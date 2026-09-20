using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DobotArm.Ipc;
using DobotArm.Transport;

namespace DobotArm.Client
{
    /// <summary>
    /// 机械臂客户端。**不直接碰机械臂**，而是通过 x86 桥接进程（DobotArmBridge.exe，
    /// 宿主官方 DobotDll.dll）指挥它——原生 DLL 只有 32 位版本，必须进程隔离（见方案 §一）。
    /// 通信参数见 <see cref="DobotArmSettings"/>。
    ///
    /// 通信模型（与 v2.1 相同，只是"对机械臂"变成"对桥接进程"）：
    ///   ① **一问一答串行化**——同一时刻只允许一个请求在飞（`SemaphoreSlim(1,1)`）；
    ///   ② **只有"没应答"才重发**，且只对幂等命令（读取/探活/断开）；
    ///      动作命令（回零/点动/定点/吸盘）不盲目重发，报错交给上层（安全优先）；
    ///      桥接进程明确回了 `ok=0`（未连接/串口打不开/参数非法）是业务性失败，重发没意义，直接抛；
    ///   ③ **迟到包丢弃**——每条请求带递增 `seq`，应答 `seq` 不符就丢掉（UDP 上迟到包很常见）；
    ///   ④ **断线即标记**——已连接状态下的请求彻底失败 → `IsConnected=false` 并触发一次 `ConnectionLost`。
    ///
    /// 同步方法内部阻塞收发；带 Async 后缀的只是 Task.Run 包装，方便 WinForms await。
    /// </summary>
    public sealed class DobotArmClient : IDisposable
    {
        /// <summary>探活超时：本地回环，500ms 足够；桥接进程没起来时也不想等太久。</summary>
        private const int PingTimeoutMs = 500;

        /// <summary>下发给桥接进程的官方 SetCmdTimeout（官方 demo 用 3000）。</summary>
        private const int BridgeCommandTimeoutMs = 3000;

        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly BridgeProcess _bridge;

        private UdpBridgeTransport _transport;
        private int _transportPort;
        private int _seq;
        private bool _connected;

        /// <summary>通信参数（连接前可改）。</summary>
        public DobotArmSettings Settings { get; private set; }

        /// <summary>日志回调（可空）：把每条命令与应答写出来，便于现场排查。</summary>
        public Action<string> Log { get; set; }

        /// <summary>是否已连接（桥接进程连上了机械臂，且最近一次请求没失败）。</summary>
        public bool IsConnected
        {
            get { return _connected && _transport != null && _transport.IsOpen; }
        }

        /// <summary>断线事件（已连接状态下重试耗尽后的第一次失败触发一次）。</summary>
        public event Action<string> ConnectionLost;

        public DobotArmClient() : this(new DobotArmSettings())
        {
        }

        public DobotArmClient(DobotArmSettings settings)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            Settings = settings;
            _bridge = new BridgeProcess(settings, Write);
        }

        // ------------------------------------------------------------------
        // 连接 / 断开
        // ------------------------------------------------------------------

        /// <summary>
        /// 连接：确保桥接进程在跑 → 让桥接进程连串口 → `GetPose` 验活。任一步失败都会自己清理。
        /// </summary>
        public void Connect()
        {
            Disconnect();
            EnsureBridgeRunning();

            BridgeMessage reply;
            try
            {
                reply = Request(BridgeCmd.Connect, Settings.ConnectTimeoutMs, false, FillConnectRequest);
            }
            catch (Exception)
            {
                // 实测：官方 DLL 连一个不是机械臂的串口也要 6 秒以上，我们的等待可能先超时，
                // 而桥接进程随后仍然把串口连上了。所以失败后必须让它对账（发 disconnect），
                // 否则会出现"界面显示未连接、串口却被占着"的状态分歧。
                ResyncAfterConnectFailure();
                throw;
            }

            _connected = true;
            Write("已连接机械臂：串口 " + Settings.SerialPortName
                + "，固件 " + reply.GetString(BridgeKey.Fw, "?")
                + " / 版本 " + reply.GetString(BridgeKey.Ver, "?"));

            try
            {
                // 验活：connect 成功**不代表**机械臂在那儿（官方 DLL 只验证串口能打开，
                // 实测连 COM1 也会返回成功），必须真读到位姿才算连上。
                ArmPose pose = GetPose();
                Write("验活成功：" + pose);
            }
            catch (Exception)
            {
                Disconnect();
                throw;
            }
        }

        public Task ConnectAsync()
        {
            return Task.Run((Action)Connect);
        }

        /// <summary>断开机械臂（释放串口）。**不关桥接进程**——它是常驻服务，下次连接更快。</summary>
        public void Disconnect()
        {
            if (_connected && _transport != null && _transport.IsOpen)
            {
                try { Request(BridgeCmd.Disconnect, Settings.TimeoutMs, false, null); }
                catch (Exception ex) { Write("断开机械臂时出错（忽略）：" + ex.Message); }
            }

            _connected = false;
            CloseTransport();
        }

        /// <summary>确保桥接进程可达：先探活，不通就按配置拉起，再轮询探活。</summary>
        private void EnsureBridgeRunning()
        {
            OpenTransport();

            if (TryPing() != null)
            {
                Write("桥接进程已在运行（" + _transport.Name + "）");
                return;
            }

            if (!Settings.AutoStartBridge)
            {
                throw new ArmException("桥接进程没在运行（" + _transport.Name + "）。请手动运行 "
                    + Settings.BridgeExePath + "，或在系统设置页勾选「自动启动桥接进程」。");
            }

            _bridge.StartIfNeeded();

            Stopwatch watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < Settings.BridgeStartTimeoutMs)
            {
                if (TryPing() != null)
                {
                    Write("桥接进程已就绪（" + _transport.Name + "）");
                    return;
                }
                Thread.Sleep(200);
            }

            throw new ArmException("桥接进程启动后没有响应（" + _transport.Name + "，超时 "
                + Settings.BridgeStartTimeoutMs + "ms）。请检查 " + Settings.BridgeExePath
                + " 与同目录的 6 个原生 DLL 是否齐全，以及 DobotArmBridge.log。");
        }

        /// <summary>
        /// 探活：ping 不通返回 null（不抛异常，供"启动前后反复试探"用）。
        /// 容忍一次失败再下结论：桥接进程是单线程的，可能正卡在官方 ConnectDobot 里（实测要 6 秒以上），
        /// 一包不回就断言"桥接进程没在运行"会把现场排查引到错的方向。
        /// </summary>
        private BridgeMessage TryPing()
        {
            for (int attempt = 1; attempt <= 2; attempt++)
            {
                try { return Request(BridgeCmd.Ping, PingTimeoutMs, false, null); }
                catch (Exception) { }
            }
            return null;
        }

        /// <summary>connect 失败后对账：尽力让桥接进程释放串口，再 ping 一次看真实状态。</summary>
        private void ResyncAfterConnectFailure()
        {
            try
            {
                // 桥接进程是单线程串行的：如果它正卡在官方 ConnectDobot 里，这条 disconnect
                // 会排在后面、等它连完再执行——正好达到"连完立刻释放"的目的，所以这里不要求回包。
                Request(BridgeCmd.Disconnect, Settings.TimeoutMs, false, null);
            }
            catch (Exception ex)
            {
                Write("连接失败后对账未完成（忽略）：" + ex.Message);
            }
        }

        private void FillConnectRequest(BridgeMessage request)
        {
            request.Set(BridgeKey.Port, Settings.SerialPortName ?? string.Empty);
            request.Set(BridgeKey.Timeout, BridgeCommandTimeoutMs);
            request.Set(BridgeKey.Motion, Settings.ConfigureMotionParams);
            if (!Settings.ConfigureMotionParams) return;

            request.Set(BridgeKey.JogRatio, Settings.JogVelocityRatio);
            request.Set(BridgeKey.JogAccRatio, Settings.JogAccelerationRatio);
            request.Set(BridgeKey.PtpVel, Settings.PtpCoordinateVelocity);
            request.Set(BridgeKey.PtpAcc, Settings.PtpCoordinateAcceleration);
            request.Set(BridgeKey.PtpRatio, Settings.PtpVelocityRatio);
            request.Set(BridgeKey.PtpAccRatio, Settings.PtpAccelerationRatio);
        }

        // ------------------------------------------------------------------
        // 读：位姿
        // ------------------------------------------------------------------

        /// <summary>取位姿（桥接进程调官方 GetPose）。</summary>
        public ArmPose GetPose()
        {
            BridgeMessage reply = Request(BridgeCmd.Pose, Settings.TimeoutMs, true, null);

            ArmPose pose = new ArmPose();
            pose.X = Require(reply, BridgeKey.X);
            pose.Y = Require(reply, BridgeKey.Y);
            pose.Z = Require(reply, BridgeKey.Z);
            pose.R = Require(reply, BridgeKey.R);
            pose.J1 = Require(reply, BridgeKey.J1);
            pose.J2 = Require(reply, BridgeKey.J2);
            pose.J3 = Require(reply, BridgeKey.J3);
            pose.J4 = Require(reply, BridgeKey.J4);
            return pose;
        }

        public Task<ArmPose> GetPoseAsync()
        {
            return Task.Run((Func<ArmPose>)GetPose);
        }

        // ------------------------------------------------------------------
        // 动作：回零 / 清报警 / 吸盘 / 定点 / 点动
        // ------------------------------------------------------------------

        /// <summary>回零（官方 SetHOMECmd）。</summary>
        public void Home()
        {
            Request(BridgeCmd.Home, Settings.TimeoutMs, false, null);
        }

        public Task HomeAsync()
        {
            return Task.Run((Action)Home);
        }

        /// <summary>清除所有报警（官方 ClearAllAlarmsState）。</summary>
        public void ClearAlarms()
        {
            Request(BridgeCmd.ClearAlarms, Settings.TimeoutMs, true, null);
        }

        public Task ClearAlarmsAsync()
        {
            return Task.Run((Action)ClearAlarms);
        }

        /// <summary>吸盘吸/放（官方 SetEndEffectorSuctionCup）。</summary>
        public void Suction(bool on)
        {
            Request(BridgeCmd.Suction, Settings.TimeoutMs, false, delegate(BridgeMessage request)
            {
                request.Set(BridgeKey.Enable, 1);
                request.Set(BridgeKey.On, on);
            });
        }

        public Task SuctionAsync(bool on)
        {
            return Task.Run((Action)(() => Suction(on)));
        }

        /// <summary>定点运动（官方 SetPTPCmd，绝对 MOVJ 关节运动）。</summary>
        public void MoveJ(double x, double y, double z, double r)
        {
            Request(BridgeCmd.MoveJ, Settings.TimeoutMs, false, delegate(BridgeMessage request)
            {
                request.Set(BridgeKey.X, x);
                request.Set(BridgeKey.Y, y);
                request.Set(BridgeKey.Z, z);
                request.Set(BridgeKey.R, r);
            });
        }

        public Task MoveJAsync(double x, double y, double z, double r)
        {
            return Task.Run((Action)(() => MoveJ(x, y, z, r)));
        }

        /// <summary>
        /// 点动启动/停止（官方 SetJOGCmd）。**坐标模式**（isJoint=0）：dir = 轴正向码 + (反向 1)，
        /// 所以 A/B/C/D 就是 X/Y/Z/R，与界面上的 X±/Y±/Z± 标签一致；停止传 dir=0。
        /// </summary>
        public void Jog(JogAxis axis, int sign, bool start)
        {
            int dir = start ? ((int)axis + (sign >= 0 ? 0 : 1)) : 0;
            Request(BridgeCmd.Jog, Settings.TimeoutMs, false, delegate(BridgeMessage request)
            {
                request.Set(BridgeKey.Joint, false);
                request.Set(BridgeKey.Dir, dir);
            });
        }

        public Task JogAsync(JogAxis axis, int sign, bool start)
        {
            return Task.Run((Action)(() => Jog(axis, sign, start)));
        }

        /// <summary>相对走一步：读当前位置 → 绝对 MovJ 到 位置+增量（用于"单击走一步"）。</summary>
        public void MoveRelative(JogAxis axis, double delta)
        {
            ArmPose pose = GetPose();
            double x = pose.X;
            double y = pose.Y;
            double z = pose.Z;
            double r = pose.R;

            if (axis == JogAxis.X) x += delta;
            else if (axis == JogAxis.Y) y += delta;
            else if (axis == JogAxis.Z) z += delta;
            else r += delta;

            MoveJ(x, y, z, r);
        }

        public Task MoveRelativeAsync(JogAxis axis, double delta)
        {
            return Task.Run((Action)(() => MoveRelative(axis, delta)));
        }

        /// <summary>
        /// 等机械臂走到目标点：轮询位姿，三轴都进容差且连续两次满足即返回；超时抛错。
        /// （官方另有"队列索引"判据；我们沿用 v2.1 已验证的位姿容差法，见方案 §6 风险表。）
        /// </summary>
        public void WaitArrive(double x, double y, double z)
        {
            Stopwatch watch = Stopwatch.StartNew();
            int hit = 0;

            while (watch.ElapsedMilliseconds < Settings.ArriveTimeoutMs)
            {
                ArmPose pose = GetPose();
                bool inside = Math.Abs(pose.X - x) <= Settings.ArriveToleranceMm &&
                              Math.Abs(pose.Y - y) <= Settings.ArriveToleranceMm &&
                              Math.Abs(pose.Z - z) <= Settings.ArriveToleranceMm;
                hit = inside ? hit + 1 : 0;
                if (hit >= 2) return;
                Thread.Sleep(Settings.MotionPollMs);
            }

            throw new ArmException("等到位超时（" + Settings.ArriveTimeoutMs + "ms）：目标 (" +
                x.ToString("F2") + ", " + y.ToString("F2") + ", " + z.ToString("F2") + ")。");
        }

        public Task WaitArriveAsync(double x, double y, double z)
        {
            return Task.Run((Action)(() => WaitArrive(x, y, z)));
        }

        // ------------------------------------------------------------------
        // 请求核心：串行 + 超时重发 + 迟到包丢弃
        // ------------------------------------------------------------------

        /// <summary>发一条 IPC 命令并等它的应答。</summary>
        /// <param name="cmd">命令名（<see cref="BridgeCmd"/>）。</param>
        /// <param name="timeoutMs">单次等待超时。</param>
        /// <param name="allowRetry">"没应答"时是否重发（只给幂等命令传 true）。</param>
        /// <param name="fill">填充命令字段（可空）。</param>
        private BridgeMessage Request(string cmd, int timeoutMs, bool allowRetry, Action<BridgeMessage> fill)
        {
            UdpBridgeTransport transport = _transport;
            if (transport == null || !transport.IsOpen) throw new ArmException("桥接传输未打开。");

            _gate.Wait();
            try
            {
                int attempts = allowRetry ? Math.Max(1, Settings.Retries) : 1;
                ArmException last = null;

                for (int attempt = 1; attempt <= attempts; attempt++)
                {
                    int seq = ++_seq;
                    BridgeMessage request = new BridgeMessage()
                        .Set(BridgeKey.Cmd, cmd)
                        .Set(BridgeKey.Seq, seq);
                    if (fill != null) fill(request);

                    BridgeMessage reply;
                    try
                    {
                        DrainIncoming();
                        transport.Send(request.ToLine());
                        Write("→ " + request.ToLine());
                        reply = WaitReply(seq, timeoutMs);
                    }
                    catch (Exception ex)
                    {
                        last = new ArmException(cmd + " 收发失败：" + ex.Message, ex);
                        continue;
                    }

                    if (reply == null)
                    {
                        last = new ArmException(cmd + " 无应答（超时 " + timeoutMs + "ms）：桥接进程 "
                            + transport.Name + " 是否还在运行？");
                        continue;
                    }

                    Write("← " + reply.ToLine());
                    if (!reply.GetBool(BridgeKey.Ok, false))
                    {
                        // 桥接进程/机械臂明确答复了错误（未连接、串口打不开、参数非法…）：
                        // 重发没有意义，直接抛给上层；msg 已经是中文。
                        throw new ArmException(DescribeFault(cmd, reply));
                    }
                    return reply;
                }

                MarkConnectionLost(last);
                throw last ?? new ArmException(cmd + " 失败。");
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>等 seq 相符的应答；seq 不符（迟到的旧应答）丢弃。</summary>
        private BridgeMessage WaitReply(int seq, int timeoutMs)
        {
            Stopwatch watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < timeoutMs)
            {
                int remain = (int)(timeoutMs - watch.ElapsedMilliseconds);
                string line = _transport.ReceiveLine(remain <= 0 ? 1 : remain);
                if (line == null) break;

                BridgeMessage reply = BridgeMessage.Parse(line);
                if (reply.Seq == seq) return reply;
                Write("丢弃非本次应答（seq=" + reply.Seq + "，本次 " + seq + "）");
            }
            return null;
        }

        /// <summary>发新命令前把迟到的旧包排空（最多 8 个，避免死循环）。</summary>
        private void DrainIncoming()
        {
            if (_transport == null || !_transport.IsOpen) return;
            for (int i = 0; i < 8; i++)
            {
                string stale = _transport.ReceiveLine(1);
                if (stale == null) return;
                Write("重发前排空一个迟到包：" + stale);
            }
        }

        private void MarkConnectionLost(Exception error)
        {
            Write("通信失败：" + (error == null ? "未知" : error.Message));
            if (!_connected) return;        // 没连上过就谈不上"断线"（探活失败不该弹断线）

            _connected = false;
            Action<string> handler = ConnectionLost;
            if (handler != null) handler(error == null ? "通信失败" : error.Message);
        }

        private static string DescribeFault(string cmd, BridgeMessage reply)
        {
            string error = reply.GetString(BridgeKey.Err, string.Empty);
            string message = reply.GetString(BridgeKey.Msg, string.Empty);
            if (string.IsNullOrEmpty(message)) message = cmd + " 失败";
            return string.IsNullOrEmpty(error) ? message : message + "（" + error + "）";
        }

        /// <summary>取必需的数值字段；缺字段说明桥接进程应答不完整，当场报错而不是当成 0。</summary>
        private static double Require(BridgeMessage reply, string key)
        {
            if (!reply.Has(key)) throw new ArmException("位姿应答缺少字段 " + key + "。");
            return reply.GetDouble(key, 0);
        }

        private void OpenTransport()
        {
            if (_transport == null || _transportPort != Settings.Port)
            {
                CloseTransport();
                _transport = new UdpBridgeTransport(Settings.Port);
                _transportPort = Settings.Port;
            }
            _transport.Open();
        }

        private void CloseTransport()
        {
            UdpBridgeTransport transport = _transport;
            _transport = null;
            if (transport != null)
            {
                try { transport.Dispose(); }
                catch (Exception) { }
            }
        }

        private void Write(string message)
        {
            Action<string> log = Log;
            if (log != null) log(message);
        }

        /// <summary>
        /// 收尾：断开机械臂 + 关掉**我们自己拉起的**桥接进程（手动启动的留给现场）。
        /// 程序退出时请用它而不是 <see cref="Disconnect"/>，否则桥接进程会继续占着串口。
        /// </summary>
        public void Dispose()
        {
            try { Disconnect(); }
            catch (Exception) { }
            try { _bridge.Dispose(); }
            catch (Exception) { }
            _gate.Dispose();
        }
    }
}
