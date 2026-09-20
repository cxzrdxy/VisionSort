using Cognex.VisionPro;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace VisionSort.Services
{
    /// <summary>主运行页的阶段（界面用它点状态链上的灯）。</summary>
    public enum RunPhase
    {
        Idle,
        Preparing,
        WaitingTrigger,
        StoppingBelt,
        Inspecting,
        Sorting,
        Recording,
        Resuming,
        Stopped,
        Faulted,
        Estopped
    }

    /// <summary>一轮跑完的产物（界面拿去点亮检测OK/NG 灯、刷统计）。</summary>
    public sealed class CycleResult
    {
        public bool IsOk;
        public double? Score;
        public string Note = string.Empty;
        public string ImagePath = string.Empty;
        public string WorkpieceNo = string.Empty;
        public DateTime Time;
        public long ElapsedMs;
    }

    /// <summary>
    /// 生产主循环（「主运行页方案.md」§三）。
    ///
    /// 为什么放在 Services 而不是 View：状态机与设备打交道，界面只负责显示。
    /// 循环跑在**后台线程**（`Task.Run`），原因很实在——Auto 硬触发下取一帧要阻塞等光电，
    /// 放在 UI 线程上"急停"按钮就按不动了。所以：
    ///   · 本类**绝不碰控件**；所有输出都走事件，由界面自己 `BeginInvoke` 回 UI 线程；
    ///   · 急停 = 取消令牌 + 停带 + 锁存，能立刻生效。
    ///
    /// 一轮：等新帧 →(停止延时)→ 停带 →(拍照等待)→ 检测 → 存图/写库 → NG 分拣 → 恢复带。
    /// 异常逐格处理见方案 §3.3（写库/存图失败**不打断节拍**；机械臂异常**仍然写库**）。
    /// </summary>
    public sealed class ProductionRunner
    {
        /// <summary>唯一实例。</summary>
        public static ProductionRunner Shared { get; } = new ProductionRunner();

        private readonly object _sync = new object();
        private CancellationTokenSource _cts;
        private volatile bool _estop;
        private int _emptyGrabs;

        private ProductionRunner()
        {
            Source = new CameraImageSource();
        }

        // ---------------- 对外状态 ----------------

        /// <summary>
        /// 取像来源。生产＝<see cref="CameraImageSource"/>（默认）；
        /// 自测/现场回放＝<see cref="FileImageSource"/>（开发机没有相机，靠它把流程跑通）。
        /// </summary>
        public IImageSource Source { get; set; }

        /// <summary>是否正在运行（调试页据此把设备按钮置灰，Q13）。</summary>
        public bool IsRunning { get; private set; }

        /// <summary>是否连续模式。</summary>
        public bool IsContinuous { get; private set; }

        /// <summary>当前工件编号（界面在开始前/切换时写进来，每轮写库时读）。</summary>
        public string WorkpieceNo { get; set; } = string.Empty;

        public long Total { get; private set; }
        public long OkCount { get; private set; }
        public long NgCount { get; private set; }

        /// <summary>最近一次失败的中文原因（成功后清空）。</summary>
        public string LastError { get; private set; } = string.Empty;

        /// <summary>当前阶段（UI 线程读，用于刷新按钮）。</summary>
        public RunPhase Phase { get; private set; } = RunPhase.Idle;

        // ---------------- 事件（都在后台线程触发，界面自己 Invoke） ----------------

        /// <summary>阶段变化：阶段枚举 + 中文细节。</summary>
        public event Action<RunPhase, string> PhaseChanged;

        /// <summary>统计变化（总/OK/NG）。</summary>
        public event Action<long, long, long> StatisticsChanged;

        /// <summary>一轮完成。</summary>
        public event Action<CycleResult> CycleCompleted;

        /// <summary>取到新帧（界面拿去显示）。</summary>
        public event Action<ICogImage> FrameReady;

        /// <summary>一般信息（触发方式回读、写库告警等）。</summary>
        public event Action<string> Info;

        /// <summary>出故障并锁存（已经停带；界面提示中文原因，等人按报警复位）。</summary>
        public event Action<string> Faulted;

        // ---------------- 启停 ----------------

        /// <summary>启动（立即返回，真正干活在后台）。失败时 <paramref name="message"/> 是中文原因。</summary>
        public bool Start(bool continuous, out string message)
        {
            lock (_sync)
            {
                if (IsRunning)
                {
                    message = "已经在运行了。";
                    return false;
                }
                if (string.IsNullOrEmpty(VisionScheme.Shared.VppPath))
                {
                    message = "还没加载视觉方案（VPP）。请先到「视觉配置」页加载一次方案。";
                    return false;
                }

                _estop = false;
                _emptyGrabs = 0;
                IsRunning = true;
                IsContinuous = continuous;
                Total = 0;
                OkCount = 0;
                NgCount = 0;
                LastError = string.Empty;

                RaiseStatistics();
                _cts = new CancellationTokenSource();
                CancellationToken token = _cts.Token;
                Task.Run(() => LoopAsync(token));
                message = continuous ? "已启动（连续运行）。" : "已启动（单次运行）。";
                return true;
            }
        }

        /// <summary>停止：取消循环，循环退出前会停带。</summary>
        public void Stop()
        {
            IsRunning = false;
            CancellationTokenSource cts = _cts;
            if (cts != null)
            {
                try { cts.Cancel(); } catch (Exception) { }
            }
        }

        /// <summary>
        /// 急停：**立刻**停带 + 取消循环（＝不再下发新动作）+ 锁存。
        /// 说明（方案 §4.7 已写进文档）：机械臂当前那一小段 MoveJ 行程**拦不住**
        /// （Dobot 文本协议没有中断运动的可用命令），它会走完；但不会再有新动作下发。
        /// 急停路径上的每个 IO 调用都吞异常——急停不允许被设备卡住。
        /// </summary>
        public void EmergencyStop()
        {
            _estop = true;
            Stop();
            TryStopBelt();
            RaisePhase(RunPhase.Estopped, "急停已按下：传送带已停、不再下发动作（机械臂当前行程会走完）");
        }

        /// <summary>报警复位：解锁存 + 清机械臂报警（**不回零**，回零是大幅动作，让人在调试页确认安全后自己做）。</summary>
        public async Task<OpResult> ResetAlarmAsync()
        {
            _estop = false;
            LastError = string.Empty;

            string robotMessage = "机械臂未连接，跳过消除报警。";
            if (RobotArmService.Shared.IsConnected)
            {
                try
                {
                    await RobotArmService.Shared.Client.ClearAlarmsAsync();
                    robotMessage = "机械臂报警已清除。";
                }
                catch (Exception ex)
                {
                    robotMessage = "消除机械臂报警失败：" + ex.Message;
                }
            }

            string detail = "报警复位完成（" + robotMessage + "）";
            RaisePhase(RunPhase.Stopped, detail);
            return new OpResult { Ok = true, Message = detail };
        }

        // ---------------- 主循环 ----------------

        private async Task LoopAsync(CancellationToken token)
        {
            try
            {
                // ---------- Preparing ----------
                RaisePhase(RunPhase.Preparing, "准备：取像来源…");
                if (!Source.Prepare(out string sourceMessage))
                {
                    Fail("取像来源不可用：" + sourceMessage);
                    return;
                }
                RaisePhase(RunPhase.Preparing, "准备：加载视觉方案…");
                if (!VisionScheme.Shared.Load(out string vppMessage))
                {
                    Fail(vppMessage);
                    return;
                }
                RaiseInfo(vppMessage + "；取像来源：" + Source.Name);

                if (Source is CameraImageSource)
                {
                    bool triggerOk = CameraService.Shared.ApplyProductionTrigger(out string triggerMessage);
                    RaiseInfo("相机触发：" + triggerMessage);
                    if (!triggerOk)
                    {
                        Fail("相机触发方式设置失败：" + triggerMessage);
                        return;
                    }
                }

                RaisePhase(RunPhase.Preparing, "准备：连接传送带…");
                try
                {
                    await ConveyorSettings.Shared.ConnectAsync();
                }
                catch (Exception ex)
                {
                    Fail("连接传送带失败：" + ConveyorSettingsFailure(ex));
                    return;
                }

                RaisePhase(RunPhase.Preparing, "准备：连接机械臂…");
                OpResult robot = await RobotArmService.Shared.EnsureConnectedAsync();
                if (!robot.Ok)
                {
                    Fail(robot.Message);
                    TryStopBelt();
                    return;
                }

                // ---------- 循环 ----------
                while (!token.IsCancellationRequested)
                {
                    RaisePhase(RunPhase.WaitingTrigger, IsContinuous ? "等工件（连续运行）…" : "等工件（单次运行）…");

                    ICogImage frame = Source.Grab(RobotArmSettings.Shared.GrabTimeoutMs, out string grabReason);
                    if (frame == null)
                    {
                        if (token.IsCancellationRequested) break;
                        _emptyGrabs++;
                        if (!IsContinuous)
                        {
                            IsRunning = false;
                            RaisePhase(RunPhase.Stopped, "单次运行：这次没取到图（" + grabReason + "）");
                            return;
                        }
                        // 连续模式：等不到工件是正常的，继续等（不算故障）
                        if (_emptyGrabs % 20 == 0) RaiseInfo("还在等工件…（" + grabReason + "）");
                        continue;
                    }
                    _emptyGrabs = 0;
                    RaiseFrame(frame);

                    // ---------- 停带 ----------
                    int stopDelay = VisionRunSettings.Shared.StopDelayMs;
                    RaisePhase(RunPhase.StoppingBelt, "已触发：" + stopDelay + "ms 后停带");
                    if (stopDelay > 0) await Task.Delay(stopDelay, token);
                    try
                    {
                        await ConveyorSettings.Shared.StopAsync();
                    }
                    catch (Exception ex)
                    {
                        Fail("停传送带失败：" + ConveyorSettingsFailure(ex));
                        return;
                    }

                    int photoWait = VisionRunSettings.Shared.PhotoWaitMs;
                    if (photoWait > 0)
                    {
                        RaisePhase(RunPhase.StoppingBelt, "等停稳：" + photoWait + "ms");
                        await Task.Delay(photoWait, token);
                    }

                    // ---------- 检测 ----------
                    RaisePhase(RunPhase.Inspecting, "检测中…");
                    InspectionResult inspection = VisionScheme.Shared.Inspect(
                        frame, VisionRunSettings.Shared.MatchThreshold, VisionRunSettings.Shared.DetectTimeoutMs);

                    if (!inspection.Ran)
                    {
                        // 检测本身失败：仍然留一行记录（不丢事件），然后停线
                        string failedNote = inspection.Message;
                        EnqueueResult(false, null, string.Empty, failedNote);
                        RaiseStatistics();
                        Fail("检测失败：" + failedNote);
                        TryStopBelt();
                        return;
                    }

                    bool isOk = inspection.Accepted;
                    string note = inspection.Message;   // 超时的话这里已经有中文原因
                    RaisePhase(RunPhase.Inspecting,
                        "分数 " + inspection.Score.ToString("0.000") + "（阈值 " +
                        VisionRunSettings.Shared.MatchThreshold.ToString("0.000") + "）→ " + (isOk ? "OK" : "NG") +
                        "，" + inspection.ElapsedMs + "ms");

                    // ---------- 记录：存图 ----------
                    RaisePhase(RunPhase.Recording, "记录：存图…");
                    string imagePath = string.Empty;
                    SaveSettings save = SaveSettings.Shared;
                    if (save.ShouldSave(isOk))
                    {
                        try
                        {
                            imagePath = ImageSaver.Save(frame, save.RootPath, save.Format, WorkpieceNo, DateTime.Now, isOk);
                        }
                        catch (Exception ex)
                        {
                            note = Append(note, "存图失败：" + ex.Message);
                        }
                    }

                    // ---------- 分拣（NG 才动机械臂） ----------
                    // 取料点由视觉给出的像素坐标经九点标定换算（见 九点标定联机分拣方案.md §4.9）。
                    // 换算失败/没标定/越界都在 RobotPickPlace 里被拒，这里只把中文原因写进备注。
                    if (!isOk)
                    {
                        RaisePhase(RunPhase.Sorting, "NG → 机械臂取走放到废料区");
                        string pickError = await RobotPickPlace.Shared.ExecuteAsync(
                            inspection.HasPixel, inspection.PixelX, inspection.PixelY, token);
                        if (pickError != null)
                        {
                            note = Append(note, "机械臂异常：" + pickError);
                        }
                    }

                    // ---------- 写库 + 统计 ----------
                    EnqueueResult(isOk, double.IsNaN(inspection.Score) ? (double?)null : inspection.Score, imagePath, note);
                    Total++;
                    if (isOk) OkCount++; else NgCount++;
                    RaiseStatistics();
                    RaiseCycle(new CycleResult
                    {
                        IsOk = isOk,
                        Score = double.IsNaN(inspection.Score) ? (double?)null : inspection.Score,
                        Note = note,
                        ImagePath = imagePath,
                        WorkpieceNo = WorkpieceNo,
                        Time = DateTime.Now,
                        ElapsedMs = inspection.ElapsedMs
                    });

                    // ---------- 检测超时：记录已留，停线等人处理 ----------
                    if (inspection.TimedOut)
                    {
                        Fail("检测超时，已停线（本轮已记录，备注里写了原因）");
                        TryStopBelt();
                        return;
                    }

                    // ---------- 恢复传送带 ----------
                    // ⚠ **只写 `100←1`，不要在这里补 `SetSpeedAsync`**（「主运行页方案.md」§4.6 / §17.6 已定）。
                    //   速度只由设备调试页的调速滑杆写入；生产只负责起停 ——
                    //   停带不碰速度寄存器（100/101），速度值保持原样，少一次写就少一个"把速度改回去"的风险。
                    //   （2026-09-20 全流程测试实测：从站只收到 写100=0 / 写100=1，与这个决定一致。
                    //     方案 §4.6 曾写成"SetSpeedAsync + ForwardAsync"，那是没实现过的旧说法，已更正。）
                    RaisePhase(RunPhase.Resuming, "恢复传送带…");
                    try
                    {
                        await ConveyorSettings.Shared.ForwardAsync();
                    }
                    catch (Exception ex)
                    {
                        Fail("恢复传送带失败：" + ConveyorSettingsFailure(ex));
                        return;
                    }

                    if (!IsContinuous)
                    {
                        IsRunning = false;
                        RaisePhase(RunPhase.Stopped, "单次运行完成：" + (isOk ? "OK" : "NG"));
                        return;
                    }
                }

                // 正常停止
                TryStopBelt();
                RaisePhase(RunPhase.Stopped, "已停止");
            }
            catch (OperationCanceledException)
            {
                TryStopBelt();
                RaisePhase(_estop ? RunPhase.Estopped : RunPhase.Stopped, _estop ? "急停" : "已停止");
            }
            catch (Exception ex)
            {
                TryStopBelt();
                Fail("运行异常：" + ex.Message);
            }
            finally
            {
                IsRunning = false;
                lock (_sync)
                {
                    if (_cts != null)
                    {
                        try { _cts.Dispose(); } catch (Exception) { }
                        _cts = null;
                    }
                }
            }
        }

        // ---------------- 小工具 ----------------

        private void EnqueueResult(bool isOk, double? score, string imagePath, string note)
        {
            try
            {
                ResultRepository.Shared.Enqueue(new ResultRecord
                {
                    Time = DateTime.Now,
                    WorkpieceNo = WorkpieceNo ?? string.Empty,
                    RecipeID = 0,                  // 无配方管理，本期恒 0（数据库方案 §6.2）
                    IsOk = isOk,
                    Score = score,
                    ImagePath = imagePath ?? string.Empty,
                    Note = note ?? string.Empty
                });

                string pendingError = ResultRepository.Shared.LastError;
                if (!string.IsNullOrEmpty(pendingError))
                {
                    RaiseInfo("写库有问题（不影响生产，恢复后自动补写）：" + pendingError +
                              "；待补写 " + ResultRepository.Shared.Pending.Count + " 条");
                }
            }
            catch (Exception ex)
            {
                // 写库这条路本身不该把生产打断
                RaiseInfo("写库入队失败（不影响生产）：" + ex.Message);
            }
        }

        private void TryStopBelt()
        {
            try
            {
                if (ConveyorSettings.Shared.IsConnected) ConveyorSettings.Shared.StopAsync().Wait(1000);
            }
            catch (Exception)
            {
                // 急停/收尾路径不弹异常
            }
        }

        private static string ConveyorSettingsFailure(Exception ex)
        {
            return ex.Message + "（确认变频器/串口；参数见「系统设置」页 Modbus 组）";
        }

        private static string Append(string note, string extra)
        {
            if (string.IsNullOrEmpty(note)) return extra;
            return note + "；" + extra;
        }

        private void Fail(string message)
        {
            // 先落 IsRunning 再发事件：否则调用方在终态事件里立刻重启会撞上"已经在运行了"
            // （自测 S1→S3 实测踩到）。
            IsRunning = false;
            LastError = message;
            RaisePhase(RunPhase.Faulted, message);
            Action<string> handler = Faulted;
            if (handler != null) handler(message);
        }

        private void RaisePhase(RunPhase phase, string detail)
        {
            Phase = phase;
            Action<RunPhase, string> handler = PhaseChanged;
            if (handler != null) handler(phase, detail);
        }

        private void RaiseStatistics()
        {
            Action<long, long, long> handler = StatisticsChanged;
            if (handler != null) handler(Total, OkCount, NgCount);
        }

        private void RaiseCycle(CycleResult result)
        {
            Action<CycleResult> handler = CycleCompleted;
            if (handler != null) handler(result);
        }

        private void RaiseFrame(ICogImage image)
        {
            Action<ICogImage> handler = FrameReady;
            if (handler != null) handler(image);
        }

        private void RaiseInfo(string message)
        {
            Action<string> handler = Info;
            if (handler != null) handler(message);
        }
    }
}

