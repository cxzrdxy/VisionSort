using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using DobotArm.Client;
using VisionSort.Services;

namespace VisionSort.Views
{
    /// <summary>
    /// 设备调试页 · 机械臂控制（grpRobot）。
    ///
    /// 连接方式：**独立项目 DobotArm** → **x86 桥接进程 DobotArmBridge.exe**（宿主官方 DobotDll.dll）
    ///   → USB 串口连机械臂。桥接进程的生命周期、命令往返、超时重发都在类库里，本页只管界面与流程。
    ///   （官方原生 DLL 只有 32 位版本，而 VisionPro 是 x64，必须靠独立 x86 进程隔离，
    ///     见 `机械臂TCP控制方案.md` §一；"独立类库就能避开冲突"是行不通的。）
    /// 点动（Q2=A）：**按住 = 连续 JOG**（MouseDown 启动 / MouseUp 停止），**单击 = 按步距走一步**（相对 MovJ）。
    ///   "按住"用 200ms 定时器判定：按下后 200ms 内松手算单击（走一步），超过才算按住（发 JOG），避免两种语义打架。
    /// 队列（Q3=B）：**软件端队列**——步骤表逐条下发，每步用位姿轮询等到位；不依赖机械臂固件队列能力，
    ///   所以延时（抓取/复位）想插哪插哪、停止可控、断线也清楚停在第几步。
    /// 执行器（Q5=B）：只做吸盘；选「气爪夹」会提示并退回「吸盘吸」。真空反馈本次显示 "--"（无反馈源）。
    ///
    /// 生命周期：Init/Shutdown 由 DeviceDebugView.Conveyor.cs 的 OnLoad/Disposed 调用（那个 partial 已占用这两个入口）。
    /// </summary>
    public partial class DeviceDebugView
    {
        /// <summary>排队步骤：本次只有"走到某个点"一种（结构上留了扩展位）。</summary>
        private sealed class RobotStep
        {
            public double X;
            public double Y;
            public double Z;
        }

        private const int JogHoldThresholdMs = 200;   // 按下多久算"按住"

        /// <summary>
        /// 机械臂客户端 ＝ <see cref="RobotArmService.Shared"/> 里那**一个**（§17.4 E 已补）。
        ///
        /// 原来这里是 `private readonly DobotArmClient _arm = new DobotArmClient();` ——
        /// 那一份只在调试页手里，主运行页分拣又用共享单例，于是**两个客户端各发各的命令**；
        /// 而 v3.0 只有一个桥接进程、一条回环 UDP，命令会互相插队，
        /// 现场表现就是"机械臂偶尔乱走 / 报超时"（很难复现的那种）。
        /// 改成属性代理之后，本页所有 `_arm.xxx` 调用一行都不用改。
        ///
        /// 注意两个语义变化（都是想要的）：
        ///   · 本页『断开机械臂』断的就是共享连接 —— 生产在跑时那两个按钮被互锁置灰了（见 DeviceDebugView.Lock.cs），
        ///     所以不会出现"生产中被调试页断开"。
        ///   · `ShutdownRobot()` 里的 `Dispose()` 现在释放的是共享实例；这一页只在程序退出时才 Dispose，
        ///     语义与改动前一致（关掉我们自己拉起的桥接进程）。
        /// </summary>
        private DobotArmClient _arm
        {
            get { return RobotArmService.Shared.Client; }
        }

        private readonly List<RobotStep> _robotQueue = new List<RobotStep>();
        private int _robotDoneSteps;
        private bool _queueRunning;
        private bool _queueStopRequested;
        private bool _robotBusy;

        private System.Windows.Forms.Timer _robotTimer;     // 500ms 位姿刷新
        private System.Windows.Forms.Timer _jogHoldTimer;   // 200ms 按住判定
        private char _jogAxis;
        private int _jogSign;
        private bool _jogActive;                            // 已发出 JOG 启动、需要补一次停止
        private bool _robotPanelReady;                      // InitRobotPanel 跑完前，控件事件（如 Designer 设 SelectedIndex）不动作
        private bool _robotUiSyncing;                       // 程序改控件时抑制事件（回退下拉不会被当成"用户选了吸盘"）

        // ------------------------------------------------------------------
        // 初始化 / 收尾（由 Conveyor partial 调用）
        // ------------------------------------------------------------------

        private void InitRobotPanel()
        {
            _robotTimer = new System.Windows.Forms.Timer();
            _robotTimer.Interval = 500;
            _robotTimer.Tick += RobotTimer_Tick;

            _jogHoldTimer = new System.Windows.Forms.Timer();
            _jogHoldTimer.Interval = JogHoldThresholdMs;
            _jogHoldTimer.Tick += JogHoldTimer_Tick;

            if (cmbStep.Items.Count > 0 && cmbStep.SelectedIndex < 0) cmbStep.SelectedIndex = 1;   // 默认 5mm
            lblVac.Text = "真空反馈：--";   // Q5=B：没有反馈源
            _robotPanelReady = true;        // 之后控件事件才真正动作
            RefreshRobotUi();
        }

        private void ShutdownRobot()
        {
            _queueStopRequested = true;
            _jogHoldTimer.Stop();
            _robotTimer.Stop();
            // 用 Dispose 而不是 Disconnect：关程序时要顺带关掉**我们自己拉起的**桥接进程，
            // 否则它会继续占着机械臂串口（Disconnect 只释放串口、保留进程，那是"断开连接"的语义）。
            try { _arm.Dispose(); } catch (Exception) { }
        }

        // ------------------------------------------------------------------
        // 连接 / 断开
        // ------------------------------------------------------------------

        private async void btnRbConn_Click(object sender, EventArgs e)
        {
            if (_arm.IsConnected)
            {
                MessageBox.Show(this, "机械臂已连接，如需重连请先断开。", "机械臂",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_robotBusy) return;

            try
            {
                _robotBusy = true;
                Cursor.Current = Cursors.WaitCursor;

                // 界面配置（系统设置页）→ 类库通信参数
                RobotArmSettings s = RobotArmSettings.Shared;
                _arm.Settings.Port = s.Port;
                _arm.Settings.SerialPortName = s.SerialPortName;
                _arm.Settings.TimeoutMs = s.CommandTimeoutMs;
                _arm.Settings.ConnectTimeoutMs = s.ConnectTimeoutMs;
                _arm.Settings.AutoStartBridge = s.AutoStartBridge;
                _arm.Settings.MotionPollMs = s.MotionPollMs;
                _arm.Settings.ArriveToleranceMm = s.ArriveToleranceMm;
                _arm.Settings.ArriveTimeoutMs = s.ArriveTimeoutMs;

                await _arm.ConnectAsync();
                await RefreshPoseAsync();
                _robotTimer.Start();
            }
            catch (Exception ex)
            {
                RobotArmSettings s = RobotArmSettings.Shared;
                MessageBox.Show(this,
                    "连接机械臂失败：\r\n" + ex.Message +
                    "\r\n\r\n排查：① 串口号对不对（当前 " + s.SerialPortName +
                    "；不知道就填 auto，让官方 DLL 自动搜索）；" +
                    "② USB 线是否插好、机械臂是否上电；" +
                    "③ 串口是否被 DobotStudio 或官方 demo 占用（用之前要关掉它们）。",
                    "机械臂", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _robotBusy = false;
                Cursor.Current = Cursors.Default;
                RefreshRobotUi();
            }
        }

        private void btnRbDis_Click(object sender, EventArgs e)
        {
            if (!_arm.IsConnected)
            {
                MessageBox.Show(this, "当前未连接机械臂。", "机械臂",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DisconnectRobot();
        }

        private void DisconnectRobot()
        {
            _queueStopRequested = true;
            _robotTimer.Stop();
            _jogHoldTimer.Stop();
            try { _arm.Disconnect(); } catch (Exception) { }
            RefreshRobotUi();
        }

        // ------------------------------------------------------------------
        // 回零 / 消警 / 吸盘
        // ------------------------------------------------------------------

        private async void btnHome_Click(object sender, EventArgs e)
        {
            await RunRobotActionAsync("回零", delegate { return _arm.HomeAsync(); });
        }

        private async void btnClearAlarm_Click(object sender, EventArgs e)
        {
            await RunRobotActionAsync("消除报警", delegate { return _arm.ClearAlarmsAsync(); });
        }

        private async void btnRelease_Click(object sender, EventArgs e)
        {
            await RunRobotActionAsync("吸盘放", delegate { return _arm.SuctionAsync(false); });
        }

        /// <summary>
        /// 执行器下拉：本次只实现吸盘（Q5=B）。
        /// 语义＝"动作选择器"：选中即执行，执行完回到未选中，这样"吸盘吸"每次都能点出来
        /// （若默认选中"吸盘吸"，再选它不会触发事件 → 吸气路径点不出来）。
        /// </summary>
        private async void cmbExec_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_robotPanelReady || _robotUiSyncing) return;   // 初始化/程序回退时不动作

            if (cmbExec.SelectedIndex == 1)
            {
                MessageBox.Show(this, "「气爪夹」本次未实现（只做吸盘）。", "机械臂",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                ResetExecSelection();
                return;
            }
            if (cmbExec.SelectedIndex < 0) return;              // 回到未选中，不动作

            await RunRobotActionAsync("吸盘吸", delegate { return _arm.SuctionAsync(true); });
            ResetExecSelection();
        }

        /// <summary>把执行器下拉复位成"未选中"（不复位就再也点不出同一个动作）。</summary>
        private void ResetExecSelection()
        {
            _robotUiSyncing = true;
            try { cmbExec.SelectedIndex = -1; }
            finally { _robotUiSyncing = false; }
        }

        // ------------------------------------------------------------------
        // 定点运动
        // ------------------------------------------------------------------

        private async void btnMoveTo_Click(object sender, EventArgs e)
        {
            double x, y, z;
            if (!TryReadTarget(out x, out y, out z)) return;
            await RunRobotActionAsync("定点运动", delegate { return MoveToAsync(x, y, z); });
        }

        /// <summary>读目标 X/Y/Z（三个框都要合法；分开解析，别用 &amp;&amp; 短路，否则 out 参数没赋值）。</summary>
        private bool TryReadTarget(out double x, out double y, out double z)
        {
            bool okX = TryParseNumber(txtTX.Text, out x);
            bool okY = TryParseNumber(txtTY.Text, out y);
            bool okZ = TryParseNumber(txtTZ.Text, out z);

            if (!okX || !okY || !okZ)
            {
                MessageBox.Show(this, "目标 X/Y/Z 请填数字（mm）。", "机械臂",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private static bool TryParseNumber(string text, out double value)
        {
            return double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                || double.TryParse(text.Trim(), out value);
        }

        /// <summary>界面上的轴字符（'X'/'Y'/'Z'/'R'）→ 类库的 <see cref="JogAxis"/>。</summary>
        private static JogAxis AxisOf(char axis)
        {
            if (axis == 'Y') return JogAxis.Y;
            if (axis == 'Z') return JogAxis.Z;
            if (axis == 'R') return JogAxis.R;
            return JogAxis.X;
        }

        /// <summary>MovJ 到目标点并等到位（带到位超时保护）。</summary>
        private async Task MoveToAsync(double x, double y, double z)
        {
            await _arm.MoveJAsync(x, y, z, 0);
            await _arm.WaitArriveAsync(x, y, z);
        }

        // ------------------------------------------------------------------
        // 点动：按住 = 连续 JOG；单击 = 按步距走一步
        // ------------------------------------------------------------------

        private void JogButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (!EnsureConnectedForAction("点动")) return;

            string tag = Convert.ToString(((Button)sender).Tag);   // "X+" / "X-" 这样标在 Tag 上
            if (string.IsNullOrEmpty(tag) || tag.Length < 2) return;

            _jogAxis = tag[0];
            _jogSign = tag[1] == '-' ? -1 : 1;
            _jogActive = false;
            _jogHoldTimer.Stop();
            _jogHoldTimer.Start();   // 200ms 后还在按 → 认定"按住"，发 JOG
        }

        private async void JogHoldTimer_Tick(object sender, EventArgs e)
        {
            _jogHoldTimer.Stop();
            if (!_arm.IsConnected) return;

            try
            {
                await _arm.JogAsync(AxisOf(_jogAxis), _jogSign, true);
                _jogActive = true;
            }
            catch (Exception ex)
            {
                ShowRobotError("点动（启动）", ex);
            }
        }

        private async void JogButton_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            bool wasHolding = _jogActive;
            _jogHoldTimer.Stop();

            if (!_arm.IsConnected)
            {
                _jogActive = false;
                return;
            }

            try
            {
                if (wasHolding)
                {
                    await _arm.JogAsync(AxisOf(_jogAxis), _jogSign, false);   // 按住 → 松手必停
                    _jogActive = false;
                    return;
                }

                // 单击 → 按步距走一步
                double step = SelectedStepMm();
                await _arm.MoveRelativeAsync(AxisOf(_jogAxis), _jogSign * step);
            }
            catch (Exception ex)
            {
                ShowRobotError("点动（停止/步进）", ex);
            }
            finally
            {
                _jogActive = false;
            }
        }

        /// <summary>鼠标按着划出按钮也算松手，避免机械臂一直动。</summary>
        private async void JogButton_MouseLeave(object sender, EventArgs e)
        {
            if (!_jogActive || !_arm.IsConnected) return;
            try
            {
                await _arm.JogAsync(AxisOf(_jogAxis), _jogSign, false);
            }
            catch (Exception ex)
            {
                ShowRobotError("点动（离开停止）", ex);
            }
            finally
            {
                _jogHoldTimer.Stop();
                _jogActive = false;
            }
        }

        /// <summary>步距下拉（1mm/5mm/10mm）→ mm；认不出回 5。</summary>
        private double SelectedStepMm()
        {
            string text = cmbStep.Text.Replace("mm", "").Trim();
            double value;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value > 0)
                return value;
            return 5;
        }

        // ------------------------------------------------------------------
        // 软件端队列（Q3=B）
        // ------------------------------------------------------------------

        private void btnAddQ_Click(object sender, EventArgs e)
        {
            double x, y, z;
            if (!TryReadTarget(out x, out y, out z)) return;
            if (_queueRunning)
            {
                MessageBox.Show(this, "队列正在运行，先停止再改队列。", "机械臂",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _robotQueue.Add(new RobotStep { X = x, Y = y, Z = z });
            RefreshQueueLabel();
        }

        private void btnClearQ_Click(object sender, EventArgs e)
        {
            if (_queueRunning)
            {
                _queueStopRequested = true;
                MessageBox.Show(this, "已请求停止队列；停在当前步后可再点『清空队列』。", "机械臂",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _robotQueue.Clear();
            _robotDoneSteps = 0;
            RefreshQueueLabel();
        }

        private async void btnRunQ_Click(object sender, EventArgs e)
        {
            if (_queueRunning)
            {
                _queueStopRequested = true;
                btnRunQ.Enabled = false;   // 等当前步跑完
                return;
            }

            if (!EnsureConnectedForAction("启动队列")) return;
            if (_robotQueue.Count == 0)
            {
                MessageBox.Show(this, "队列是空的：先用『添加队列』把目标点加进来。", "机械臂",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _queueRunning = true;
            _queueStopRequested = false;
            _robotDoneSteps = 0;
            RefreshRobotUi();

            try
            {
                foreach (RobotStep step in _robotQueue)
                {
                    if (_queueStopRequested || !_arm.IsConnected) break;

                    await MoveToAsync(step.X, step.Y, step.Z);
                    _robotDoneSteps++;
                    RefreshQueueLabel();
                }
            }
            catch (Exception ex)
            {
                ShowRobotError("队列运行", ex);
            }
            finally
            {
                _queueRunning = false;
                RefreshRobotUi();
            }
        }

        private void RefreshQueueLabel()
        {
            lblQCount.Text = "队列：" + _robotDoneSteps + "/" + _robotQueue.Count;
        }

        // ------------------------------------------------------------------
        // 状态刷新与按钮状态
        // ------------------------------------------------------------------

        private async void RobotTimer_Tick(object sender, EventArgs e)
        {
            if (_robotBusy) return;
            await RefreshPoseAsync();
        }

        private async Task RefreshPoseAsync()
        {
            if (!_arm.IsConnected) return;

            try
            {
                ArmPose pose = await _arm.GetPoseAsync();
                lblPX.Text = "当前X：" + pose.X.ToString("0.0");
                lblPY.Text = "当前Y：" + pose.Y.ToString("0.0");
                lblPZ.Text = "当前Z：" + pose.Z.ToString("0.0");
            }
            catch (Exception ex)
            {
                _robotTimer.Stop();
                lblPX.Text = "当前X：--";
                lblPY.Text = "当前Y：--";
                lblPZ.Text = "当前Z：--";
                MessageBox.Show(this, "读取机械臂位姿失败，已停止状态轮询：\r\n" + ex.Message, "机械臂",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RefreshRobotUi();
            }
        }

        /// <summary>
        /// 按钮状态：
        ///   连接/断开互斥置灰（连接状态指示，沿用本页 Modbus 那半区与相机页的做法）；
        ///   功能按钮**未连接也可点**（点了弹提示"请先连接"，与相机页/传送带一致，不做静默置灰）；
        ///   队列运行中，或**生产正在跑**（`_productionLock`，Q13 互锁）时，
        ///   把会与它们抢设备的按钮全部置灰。
        /// </summary>
        private void RefreshRobotUi()
        {
            bool connected = _arm.IsConnected;
            // 队列在跑 或 生产在跑 → 都不能再插队抢设备
            bool hardwareLock = _queueRunning || _productionLock;

            // 生产在跑时连"连接/断开"也不许点：那条串口是生产正在用的
            btnRbConn.Enabled = !connected && !_productionLock;
            btnRbDis.Enabled = connected && !_productionLock;

            btnHome.Enabled = !hardwareLock;
            btnClearAlarm.Enabled = !hardwareLock;
            btnMoveTo.Enabled = !hardwareLock;
            btnXp.Enabled = !hardwareLock;
            btnXm.Enabled = !hardwareLock;
            btnYp.Enabled = !hardwareLock;
            btnYm.Enabled = !hardwareLock;
            btnZp.Enabled = !hardwareLock;
            btnZm.Enabled = !hardwareLock;
            btnRelease.Enabled = !hardwareLock;
            btnAddQ.Enabled = !hardwareLock;
            btnClearQ.Enabled = !hardwareLock;
            btnRunQ.Enabled = !_productionLock;   // 未连接/空队列/运行中都有明确提示或切换成"停止队列"

            btnRunQ.Text = _queueRunning ? "停止队列" : "启动队列运行";

            if (!connected || _productionLock)
            {
                lblPX.Text = "当前X：--";
                lblPY.Text = "当前Y：--";
                lblPZ.Text = "当前Z：--";
            }
            RefreshQueueLabel();
        }

        /// <summary>停掉"按住点动"（生产正好开始时，机械臂不能还在点动状态里）。</summary>
        private void StopJogIfActive()
        {
            _jogHoldTimer.Stop();
            if (!_jogActive) return;

            _jogActive = false;
            if (!_arm.IsConnected) return;
            try
            {
                // 发一次停止；这是"松手"语义，失败也不该弹框（可能正在被生产抢着用）
                _arm.JogAsync(AxisOf(_jogAxis), _jogSign, false);
            }
            catch (Exception)
            {
            }
        }

        // ------------------------------------------------------------------
        // 公共壳：连接校验 / 防重入 / 异常提示
        // ------------------------------------------------------------------

        private bool EnsureConnectedForAction(string action)
        {
            if (_arm.IsConnected) return true;
            MessageBox.Show(this, "机械臂未连接，请先点『连接机械臂』。\r\n（IP/端口在『系统设置』页配置）",
                "机械臂", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private async Task RunRobotActionAsync(string action, Func<Task> work)
        {
            if (!EnsureConnectedForAction(action)) return;
            if (_robotBusy) return;

            try
            {
                _robotBusy = true;
                Cursor.Current = Cursors.WaitCursor;
                await work();
                await RefreshPoseAsync();
            }
            catch (Exception ex)
            {
                ShowRobotError(action, ex);
            }
            finally
            {
                _robotBusy = false;
                Cursor.Current = Cursors.Default;
            }
        }

        private void ShowRobotError(string action, Exception ex)
        {
            MessageBox.Show(this, action + "失败：\r\n" + ex.Message, "机械臂",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshRobotUi();   // 超时会断开连接：按钮状态、位姿显示跟着回到"未连接"
        }
    }
}
