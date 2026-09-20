using Cognex.VisionPro;
using System;
using System.Drawing;
using System.Windows.Forms;
using VisionSort.Services;

namespace VisionSort.Views
{
    /// <summary>
    /// 主运行页（「主运行页方案.md」第 2 步）：把 ProductionRunner 的状态与结果**显示**出来，
    /// 并把按钮意图**转达**给 runner。这里不做任何设备判断——判断全在 Services 里。
    ///
    /// 三个约定：
    ///   ① runner 的事件都在**后台线程**触发，这里一律 `BeginInvoke` 回 UI 线程再碰控件；
    ///   ② `btnShot / btnOK / btnNG` 是**灯**（草稿里就是状态指示），不接 Click；
    ///   ③ 预览用运行时创建的 `CogRecordDisplay` 填进 `pnlDisplay`（不动 Designer）。
    /// </summary>
    public partial class MainRunView
    {
        /// <summary>状态链文案与阶段一一对应（草稿原文案）。</summary>
        private static readonly string[] FlowSteps =
        {
            "等待工件", "光电触发", "停止传送带", "拍照检测", "机械臂分拣", "恢复传送带"
        };

        private static readonly Color LampOnGreen = Color.MediumSeaGreen;
        private static readonly Color LampOnRed = Color.Crimson;
        private static readonly Color LampOnAmber = Color.Goldenrod;
        private static readonly Color LampOff = Color.Gainsboro;
        private static readonly Color LampOffText = Color.DimGray;

        private CogRecordDisplay _display;
        private bool _wired;
        private ToolTip _readbackTip;

        /// <summary>开机那次"真连一次"数据库的结果；null = 还没回来（那时显示"连接中…"）。</summary>
        private DbProbeResult _dbProbe;

        /// <summary>发起探测那一刻的累计写入数：之后若真写成功过，说明有新证据胜过这次探测。</summary>
        private long _dbProbeWritten;

        /// <summary>库这一格现在算不算"坏"（决定状态行颜色）。</summary>
        private bool _dbLampBad;

        /// <summary>把 Designer 里没绑的按钮接上，并建预览控件（构造期调用一次）。</summary>
        private void WireRunEvents()
        {
            if (_wired) return;
            _wired = true;

            btnStart.Click += btnStart_Click;
            btnStop.Click += btnStop_Click;
            btnSingle.Click += btnSingle_Click;
            btnCont.Click += btnCont_Click;
            btnEstop.Click += btnEstop_Click;
            btnReset.Click += btnReset_Click;

            BuildDisplay();

            _readbackTip = new ToolTip();   // 状态行全文（面板放不下时的兜底）

            // 切回本页时重新读一遍"当前生效值"（原因见 MainRunView_VisibleChanged 的注释）
            VisibleChanged += MainRunView_VisibleChanged;

            ProductionRunner runner = ProductionRunner.Shared;
            runner.PhaseChanged += Runner_PhaseChanged;
            runner.StatisticsChanged += Runner_StatisticsChanged;
            runner.CycleCompleted += Runner_CycleCompleted;
            runner.FrameReady += Runner_FrameReady;
            runner.Info += Runner_Info;
            runner.Faulted += Runner_Faulted;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // 生产取像来源＝相机（自测/回放时把它换成 FileImageSource，见方案 §七）
            ProductionRunner.Shared.Source = new CameraImageSource();
            ProductionRunner.Shared.WorkpieceNo = cmbRecipe.Text;

            ResetLamps();
            SetStatistics(0, 0, 0);
            SetFlow(null);
            RefreshRunButtons();
            UpdateReadback();
            lblDisplayTip.Visible = true;
            lblDisplayTip.Text = "等待开始。开始检测后会连相机、加载视觉方案，并显示实时画面。";

            CheckDatabaseOnce();   // 开机真连一次库（见方法注释：以前那句"正常"是假的）

            // 九点标定也在开机时读一次：这份文件决定"能不能抓得准"，而它跟"库"一样是
            // 界面上要显眼告诉人的事实。不在这里读的话，只有进过设备调试页才会被载入，
            // 主运行页那句「标定 …」就会撒谎（文件明明在，却显示"无"）。
            string calibMessage;
            CalibrationService.Shared.EnsureLoaded(out calibMessage);
            UpdateReadback();
            if (!CalibrationService.Shared.IsCalibrated) Runner_Info("九点标定：" + calibMessage);
        }

        /// <summary>
        /// 切回本页时重新读一遍"当前生效值"（2026-09-20 补，联动缺口 2）。
        ///
        /// 为什么需要：状态行上的**阈值 / 标定 / 方案 / 工具**都是在**别的页**改的
        /// （阈值在系统设置页、标定在设备调试页、方案在视觉配置页），而本页原来只在
        /// `OnLoad` 与"阶段变化"时刷新 —— 于是改完阈值切回来，界面还显示旧值，
        /// 而阈值其实**下一轮就已经生效**了。这正好违反这条状态行自己立的原则：
        /// "把当前生效值显式写出来，改了不生效是现场最常见的困惑"（§九），
        /// 以及「库」那格同款的"界面不许撒谎"。
        ///
        /// 为什么用 `VisibleChanged` 而不是给 TabControl 挂 `SelectedIndexChanged`：
        /// 本页不需要知道、也不该知道"自己被谁装着"（`MainForm` 里现在没有任何 Tab 事件），
        /// 而切页时父级可见性变化会自然传到这一层，零耦合。
        ///
        /// 重复触发无害：`UpdateReadback` 只读 Shared 的现值，不写任何状态、不触发设备动作。
        /// </summary>
        private void MainRunView_VisibleChanged(object sender, EventArgs e)
        {
            if (!Visible || IsDisposed || !IsHandleCreated) return;   // 只在"变成可见"且控件可用时刷
            UpdateReadback();
        }

        // ------------------------------------------------------------------
        // 开机数据库自检
        // ------------------------------------------------------------------

        /// <summary>
        /// 开机后台**真连一次**数据库，结果写进状态行的"库"这一格。
        ///
        /// 为什么要有这个：原来那里写的是 `LastError 为空 → "正常"`，那只是"本进程内最近一次写库有没有报错"，
        /// 新进程一次库操作都没做过时**恒显示"正常"**——库其实连不上它也说正常（2026-09-17 GUI 实测踩到，
        /// 见 数据库方案.md §二十）。改成开机主动连一次，把"未知"和"已知通"区分开。
        ///
        /// 不阻塞界面：连接超时 3 秒（连接串里的 ConnectionTimeout），且跑在任务线程上。
        /// </summary>
        private async void CheckDatabaseOnce()
        {
            _dbProbeWritten = ResultRepository.Shared.Written;

            DbProbeResult result;
            try
            {
                result = await ResultRepository.Shared.TestConnectionAsync();
            }
            catch (Exception ex)
            {
                // TestConnectionAsync 自身不抛；这里只为"连自检都不能崩界面"兜底
                result = new DbProbeResult { Ok = false, Message = "开机自检异常：" + ex.Message };
            }

            OnUi(delegate
            {
                _dbProbe = result;
                UpdateReadback();

                lblDisplayTip.Visible = true;
                if (result.Ok && result.TableExists)
                {
                    lblDisplayTip.Text = "数据库已连接（" + result.ServerVersion + "／" + result.DatabaseName +
                        "／结果表 " + DbSettings.Shared.TableName + " 共 " + result.RowCount + " 行）。" +
                        "\r\n\r\n等待开始：开始检测后会连相机、加载视觉方案，并显示实时画面。";
                }
                else if (result.Ok)
                {
                    lblDisplayTip.Text = "数据库已连接（" + result.ServerVersion + "／" + result.DatabaseName +
                        "），但结果表 " + DbSettings.Shared.TableName + " 不存在。" +
                        "\r\n\r\n请到「系统设置」页点『初始化建表』，否则检测结果会先落到本机补写文件。";
                }
                else
                {
                    lblDisplayTip.Text = "数据库连不上：" + result.Message +
                        "\r\n\r\n生产不受影响：检测结果会先写进本机补写文件，数据库恢复后自动补写。";
                }
            });
        }

        // ------------------------------------------------------------------
        // 按钮
        // ------------------------------------------------------------------

        private void btnStart_Click(object sender, EventArgs e)
        {
            StartRun(true);
        }

        private void btnSingle_Click(object sender, EventArgs e)
        {
            StartRun(false);
        }

        private void btnCont_Click(object sender, EventArgs e)
        {
            StartRun(true);
        }

        private void StartRun(bool continuous)
        {
            ProductionRunner.Shared.WorkpieceNo = cmbRecipe.Text;

            string message;
            if (!ProductionRunner.Shared.Start(continuous, out message))
            {
                MessageBox.Show(this, message, "主运行", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ResetLamps();
            SetStatistics(0, 0, 0);
            lblDisplayTip.Visible = true;
            lblDisplayTip.Text = "运行中…";
            RefreshRunButtons();
            UpdateReadback();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            ProductionRunner.Shared.Stop();
            lblDisplayTip.Text = "正在停止…（会先把传送带停下）";
            RefreshRunButtons();
        }

        private void btnEstop_Click(object sender, EventArgs e)
        {
            ProductionRunner.Shared.EmergencyStop();
            btnNG.BackColor = LampOnRed;
            btnNG.ForeColor = Color.White;
            RefreshRunButtons();
        }

        private async void btnReset_Click(object sender, EventArgs e)
        {
            OpResult result = await ProductionRunner.Shared.ResetAlarmAsync();
            SetLamp(btnOK, false, LampOnGreen);
            SetLamp(btnNG, false, LampOnRed);
            SetLamp(btnShot, false, LampOnAmber);
            lblDisplayTip.Visible = true;
            lblDisplayTip.Text = result.Message;
            RefreshRunButtons();
        }

        // ------------------------------------------------------------------
        // runner 事件（后台线程 → BeginInvoke 回 UI 线程）
        // ------------------------------------------------------------------

        private void Runner_PhaseChanged(RunPhase phase, string detail)
        {
            OnUi(delegate
            {
                SetFlow(phase);
                SetLampsForPhase(phase);
                lblDisplayTip.Visible = true;
                lblDisplayTip.Text = detail;

                RefreshRunButtons();
                UpdateReadback();   // 配色也由 UpdateReadback → ApplyReadyColor 统一决定
            });
        }

        private void Runner_StatisticsChanged(long total, long ok, long ng)
        {
            OnUi(delegate { SetStatistics(total, ok, ng); });
        }

        private void Runner_CycleCompleted(CycleResult result)
        {
            OnUi(delegate
            {
                SetLamp(btnShot, false, LampOnAmber);
                if (result.IsOk) SetLamp(btnOK, true, LampOnGreen);
                else SetLamp(btnNG, true, LampOnRed);
            });
        }

        private void Runner_FrameReady(ICogImage image)
        {
            OnUi(delegate
            {
                SetLamp(btnShot, true, LampOnAmber);
                lblTrigger.ForeColor = Color.MediumSeaGreen;
                try
                {
                    if (_display != null && image != null)
                    {
                        _display.Image = image;
                        _display.Fit(true);
                    }
                }
                catch (Exception)
                {
                    // 预览失败不影响生产：图已经拿到并会入库/存盘
                }
            });
        }

        private void Runner_Info(string message)
        {
            OnUi(delegate
            {
                lblDisplayTip.Visible = true;
                lblDisplayTip.Text = message;
            });
        }

        private void Runner_Faulted(string message)
        {
            OnUi(delegate
            {
                lblDisplayTip.Visible = true;
                lblDisplayTip.Text = "故障：" + message;
                RefreshRunButtons();
                UpdateReadback();
            });
        }

        /// <summary>统一的"回 UI 线程"入口（控件已销毁时安静丢弃）。</summary>
        private void OnUi(Action action)
        {
            if (action == null) return;
            try
            {
                if (IsDisposed || !IsHandleCreated) return;
                if (InvokeRequired) BeginInvoke(action);
                else action();
            }
            catch (Exception)
            {
                // 窗体正在销毁：丢掉这次刷新即可
            }
        }

        // ------------------------------------------------------------------
        // 界面刷新
        // ------------------------------------------------------------------

        private void BuildDisplay()
        {
            try
            {
                _display = new CogRecordDisplay { Dock = DockStyle.Fill, Visible = false };
                pnlDisplay.Controls.Add(_display);
            }
            catch (Exception)
            {
                _display = null;   // 拿不到显示控件也要能跑（图照常入库/存盘）
            }
        }

        private void ResetLamps()
        {
            SetLamp(btnShot, false, LampOnAmber);
            SetLamp(btnOK, false, LampOnGreen);
            SetLamp(btnNG, false, LampOnRed);
            lblSysReady.ForeColor = Color.DimGray;
            lblTrigger.ForeColor = Color.DimGray;
        }

        /// <summary>灯：亮＝上色+可用，灭＝灰。</summary>
        private static void SetLamp(Button lamp, bool on, Color onColor)
        {
            if (lamp == null) return;
            lamp.FlatStyle = FlatStyle.Flat;
            lamp.BackColor = on ? onColor : LampOff;
            lamp.ForeColor = on ? Color.White : LampOffText;
            lamp.UseVisualStyleBackColor = false;
        }

        private void SetLampsForPhase(RunPhase phase)
        {
            if (phase == RunPhase.StoppingBelt || phase == RunPhase.Inspecting) SetLamp(btnShot, true, LampOnAmber);
            else if (phase != RunPhase.WaitingTrigger) SetLamp(btnShot, false, LampOnAmber);

            if (phase == RunPhase.WaitingTrigger) lblTrigger.ForeColor = Color.DimGray;

            // 故障/急停时**故意不清预览**：保留最后一帧画面便于判断故障（状态行配色由 ApplyReadyColor 统一管）
        }

        /// <summary>状态链上给当前步加 ▶（草稿原文案，不改布局）。</summary>
        private void SetFlow(RunPhase? phase)
        {
            int current = FlowIndexOf(phase);
            System.Text.StringBuilder sb = new System.Text.StringBuilder("状态链：");
            for (int i = 0; i < FlowSteps.Length; i++)
            {
                if (i > 0) sb.Append(" → ");
                if (i == current) sb.Append("▶ ");
                sb.Append(FlowSteps[i]);
                if (i == current) sb.Append(" ◀");
            }
            lblFlow.Text = sb.ToString();
        }

        private static int FlowIndexOf(RunPhase? phase)
        {
            if (!phase.HasValue) return -1;
            switch (phase.Value)
            {
                case RunPhase.WaitingTrigger: return 0;
                case RunPhase.StoppingBelt: return 2;
                case RunPhase.Inspecting: return 3;
                case RunPhase.Sorting: return 4;
                case RunPhase.Resuming: return 5;
                default: return -1;
            }
        }

        private void SetStatistics(long total, long ok, long ng)
        {
            lblTotal.Text = "总检测数量：" + total;
            lblOKCount.Text = "合格数量：" + ok;
            lblNGCount.Text = "瑕疵数量：" + ng;
            lblRate.Text = "合格率：" + (total == 0 ? "0.00%" : ((double)ok / total).ToString("P2"));
        }

        /// <summary>按钮可用性跟着状态走（急停永远可按）。</summary>
        private void RefreshRunButtons()
        {
            ProductionRunner runner = ProductionRunner.Shared;
            bool running = runner.IsRunning;
            bool faulted = runner.Phase == RunPhase.Faulted || runner.Phase == RunPhase.Estopped;

            btnStart.Enabled = !running;
            btnSingle.Enabled = !running;
            btnCont.Enabled = !running;
            btnStop.Enabled = running;
            btnEstop.Enabled = true;
            btnReset.Enabled = faulted || !running;
        }

        /// <summary>把"当前生效值"显式写出来：改了不生效是现场最常见的困惑（方案 §九）。</summary>
        private void UpdateReadback()
        {
            VisionRunSettings vision = VisionRunSettings.Shared;
            string vpp = VisionScheme.Shared.VppPath;
            string tool = VisionScheme.Shared.IsLoaded ? VisionScheme.Shared.ToolName : "未加载";

            // 「标定」这一格是 N1 #5：生产到底用哪份标定，界面上必须说真话 ——
            // 和「库」指示灯同一条原则（界面不许撒谎）。没标定时 Q5甲 会拒绝分拣，所以这里必须显眼。
            // 放在第二位而不是末尾：这行字比面板宽，放末尾会被裁掉（「库」当初就是这么被裁过）。
            lblSysReady.Text = "● 库 " + DbLampText() +
                               "｜标定 " + CalibrationService.Shared.Describe() +
                               "｜阈值 " + vision.MatchThreshold.ToString("0.00") +
                               "｜方案 " + (string.IsNullOrEmpty(vpp) ? "未加载" : System.IO.Path.GetFileName(vpp)) +
                               "｜工具 " + tool;

            // 这行字比面板宽（默认窗口下尾部会被裁掉），挂个悬停提示保证全文随时可读
            if (_readbackTip != null) _readbackTip.SetToolTip(lblSysReady, lblSysReady.Text);

            ApplyReadyColor();
        }

        /// <summary>
        /// "库"这一格的文案。取值优先级 = 证据的新旧：排队/报错（本进程事实） &gt; 开机自检结果。
        /// 顺带把 <see cref="_dbLampBad"/> 置好给 <see cref="ApplyReadyColor"/> 用。
        /// </summary>
        private string DbLampText()
        {
            ResultRepository repo = ResultRepository.Shared;

            if (repo.Pending.Count > 0) { _dbLampBad = false; return "待补写 " + repo.Pending.Count; }  // 有兜底，不算坏
            if (!string.IsNullOrEmpty(repo.LastError)) { _dbLampBad = true; return "异常"; }
            if (_dbProbe == null) { _dbLampBad = false; return "连接中…"; }                              // 结论未出，不吓人
            if (repo.Written > _dbProbeWritten) { _dbLampBad = false; return "已连"; }                    // 之后真写成功过 → 新证据胜出
            if (!_dbProbe.Ok) { _dbLampBad = true; return "连不上"; }
            if (!_dbProbe.TableExists) { _dbLampBad = true; return "缺结果表"; }
            _dbLampBad = false; return "已连";
        }

        /// <summary>状态行配色：库坏 / 故障 / 急停 → 红；空闲 / 已停止 → 灰；其余（运行中）→ 绿。</summary>
        private void ApplyReadyColor()
        {
            RunPhase phase = ProductionRunner.Shared.Phase;
            if (_dbLampBad || phase == RunPhase.Faulted || phase == RunPhase.Estopped)
                lblSysReady.ForeColor = Color.Crimson;
            else if (phase == RunPhase.Idle || phase == RunPhase.Stopped)
                lblSysReady.ForeColor = Color.DimGray;
            else
                lblSysReady.ForeColor = Color.MediumSeaGreen;
        }
    }
}
