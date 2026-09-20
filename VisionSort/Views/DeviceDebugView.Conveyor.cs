using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisionSort.Services;

namespace VisionSort.Views
{
    /// <summary>
    /// 设备调试页 · 传送带 Modbus 控制（grpConveyor）。
    /// 连接/断开/故障复位/正转/反转/停止/调速 共 7 个动作 + 状态显示（通讯、运行状态、当前速度、绿灯）。
    ///
    /// 配置来源：<see cref="ConveyorSettings.Shared"/>（与系统设置页同一实例，那边改这里立刻生效，不用点保存）。
    /// 按钮可点规则与相机页一致：连接/断开互斥置灰（连接状态指示），其余按钮永远可点、前置不满足弹提示。
    /// 状态轮询：连上后 500ms 读一次（速度 + 运行状态）；读失败即停轮询并提示，不刷屏。
    /// 调速：滑杆拖动即时更新显示，停手 250ms 后才真正写设备（避免拖动过程中刷几十条串口指令）。
    ///
    /// 本文件只管传送带；右侧机械臂控制（29999 端口那套）是另一个功能，不在本次范围。
    /// </summary>
    public partial class DeviceDebugView
    {
        // 全名限定：本项目开了 ImplicitUsings，System.Threading.Timer 也在作用域内，不限定会 CS0104
        private readonly System.Windows.Forms.Timer _stateTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _speedDebounce = new System.Windows.Forms.Timer();
        private bool _busy;   // 防重入：串口动作串行化

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            _stateTimer.Interval = 500;
            _stateTimer.Tick += StateTimer_Tick;

            _speedDebounce.Interval = 250;
            _speedDebounce.Tick += SpeedDebounce_Tick;

            this.Disposed += DeviceDebugView_Disposed;

            txtCurSpeed.Text = FormatSpeed(trkSpeed.Value);   // 滑杆 300 → 30.0%
            RefreshConveyorUi();
            InitRobotPanel();   // 机械臂那半区（实现在 DeviceDebugView.Robot.cs）
            InitCalibPanel();   // 九点标定输入区（实现在 DeviceDebugView.Calib.cs）
            HookProductionLock();   // 生产互锁：订阅生产状态，按需把本页置灰（DeviceDebugView.Lock.cs）
        }

        private void DeviceDebugView_Disposed(object sender, EventArgs e)
        {
            _stateTimer.Stop();
            _speedDebounce.Stop();
            _stateTimer.Dispose();
            _speedDebounce.Dispose();
            UnhookProductionLock();
            ConveyorSettings.Shared.Disconnect();
            ShutdownRobot();   // 停队列 + 停 JOG + 断机械臂连接（实现在 DeviceDebugView.Robot.cs）
        }

        // ------------------------------------------------------------------
        // 连接 / 断开 / 复位
        // ------------------------------------------------------------------

        private async void btnMbConn_Click(object sender, EventArgs e)
        {
            ConveyorSettings settings = ConveyorSettings.Shared;
            if (settings.IsConnected)
            {
                MessageBox.Show(this, "Modbus 已连接，如需重连请先断开。", "传送带 Modbus",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_busy) return;

            try
            {
                _busy = true;
                Cursor.Current = Cursors.WaitCursor;
                await settings.ConnectAsync();
                await RefreshStateAsync(false);      // 连上立刻刷一次状态
                _stateTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "连接 Modbus 失败：\r\n" + ex.Message +
                    "\r\n\r\n排查顺序：① 串口号是否正确（设备管理器看 COM 号，USB 转串口会漂）；" +
                    "② 串口是否被其他软件占用；③ 波特率/站号是否与变频器一致。",
                    "传送带 Modbus", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _busy = false;
                Cursor.Current = Cursors.Default;
                RefreshConveyorUi();
            }
        }

        private void btnMbDis_Click(object sender, EventArgs e)
        {
            ConveyorSettings settings = ConveyorSettings.Shared;
            if (!settings.IsConnected)
            {
                MessageBox.Show(this, "当前未连接 Modbus。", "传送带 Modbus",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _stateTimer.Stop();
            _speedDebounce.Stop();
            settings.Disconnect();
            RefreshConveyorUi();
        }

        private async void btnMbReset_Click(object sender, EventArgs e)
        {
            // 示例代码里没有故障复位寄存器，暂定=双停（安全停一切）；现场有专用寄存器时改 ConveyorSettings.ResetAsync 一处
            await RunConveyorActionAsync("故障复位（暂定为双停：正反转都停）", delegate
            {
                return ConveyorSettings.Shared.ResetAsync();
            });
        }

        // ------------------------------------------------------------------
        // 正转 / 反转 / 停止
        // ------------------------------------------------------------------

        private async void btnFwd_Click(object sender, EventArgs e)
        {
            await RunConveyorActionAsync("正转", delegate { return ConveyorSettings.Shared.ForwardAsync(); });
        }

        private async void btnRev_Click(object sender, EventArgs e)
        {
            await RunConveyorActionAsync("反转", delegate { return ConveyorSettings.Shared.ReverseAsync(); });
        }

        private async void btnStopMove_Click(object sender, EventArgs e)
        {
            await RunConveyorActionAsync("停止", delegate { return ConveyorSettings.Shared.StopAsync(); });
        }

        /// <summary>传送带动作统一壳：未连接提示、防重入、异常提示、动作后刷状态。</summary>
        private async Task RunConveyorActionAsync(string action, Func<Task> work)
        {
            ConveyorSettings settings = ConveyorSettings.Shared;
            if (!settings.IsConnected)
            {
                MessageBox.Show(this, "Modbus 未连接，请先点『连接Modbus』。\r\n（串口号在『系统设置』页配置）",
                    "传送带 Modbus", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_busy) return;

            try
            {
                _busy = true;
                Cursor.Current = Cursors.WaitCursor;
                await work();
                await RefreshStateAsync(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, action + "失败：\r\n" + ex.Message, "传送带 Modbus",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _busy = false;
                Cursor.Current = Cursors.Default;
            }
        }

        // ------------------------------------------------------------------
        // 调速（滑杆）
        // ------------------------------------------------------------------

        private void trkSpeed_ValueChanged(object sender, EventArgs e)
        {
            txtCurSpeed.Text = FormatSpeed(trkSpeed.Value);   // 拖动即时反馈
            _speedDebounce.Stop();
            _speedDebounce.Start();                            // 停手 250ms 后再写设备
        }

        private async void SpeedDebounce_Tick(object sender, EventArgs e)
        {
            _speedDebounce.Stop();

            ConveyorSettings settings = ConveyorSettings.Shared;
            if (!settings.IsConnected || _busy) return;   // 未连接：只更新显示，不写设备

            try
            {
                _busy = true;
                await settings.SetSpeedAsync((ushort)trkSpeed.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "设置速度失败：\r\n" + ex.Message, "传送带 Modbus",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _busy = false;
            }
        }

        // ------------------------------------------------------------------
        // 状态轮询与界面刷新
        // ------------------------------------------------------------------

        private async void StateTimer_Tick(object sender, EventArgs e)
        {
            if (_busy) return;
            await RefreshStateAsync(true);
        }

        /// <summary>
        /// 读一次设备状态（速度 + 运行状态）。
        /// fromTimer=true 时，若读的过程中用户点了动作（_busy 已置位），这次结果就丢弃，
        /// 免得旧值把刚更新的状态覆盖回去（自测探针实测到过这个"点了还显示停止"的现象）。
        /// 读失败：停轮询 + 提示（只提示一次，不刷屏）。
        /// </summary>
        private async Task RefreshStateAsync(bool fromTimer)
        {
            ConveyorSettings settings = ConveyorSettings.Shared;
            if (!settings.IsConnected) return;

            try
            {
                ushort speed = await settings.ReadSpeedAsync();
                string state = await settings.ReadRunStateAsync();

                if (fromTimer && _busy) return;   // 动作优先，丢弃这次过期回读

                txtComm.Text = "已连接";
                ledMb.ForeColor = Color.ForestGreen;
                txtRunState.Text = state;
                txtCurSpeed.Text = FormatSpeed(speed);   // 连上后以设备回读值为准
            }
            catch (Exception ex)
            {
                _stateTimer.Stop();
                txtComm.Text = "通讯中断";
                ledMb.ForeColor = Color.Firebrick;
                MessageBox.Show(this, "读取传送带状态失败，已停止状态轮询：\r\n" + ex.Message,
                    "传送带 Modbus", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 连接/断开互斥置灰（连接状态指示）；其余按钮与滑杆永远可点 ——
        /// **除了生产在跑的时候**（Q13 互锁）：那时这条串口归主运行页，正转/反转/停止/调速/连接/断开/复位全部置灰。
        /// </summary>
        private void RefreshConveyorUi()
        {
            bool connected = ConveyorSettings.Shared.IsConnected;
            bool locked = _productionLock;

            btnMbConn.Enabled = !connected && !locked;
            btnMbDis.Enabled = connected && !locked;

            btnMbReset.Enabled = !locked;
            btnFwd.Enabled = !locked;
            btnRev.Enabled = !locked;
            btnStopMove.Enabled = !locked;
            trkSpeed.Enabled = !locked;

            txtComm.Text = connected ? "已连接" : "未连接";
            ledMb.ForeColor = connected ? Color.ForestGreen : Color.Gray;

            if (!connected)
            {
                txtRunState.Text = "停止";
                txtCurSpeed.Text = FormatSpeed(trkSpeed.Value);
            }
            else if (locked)
            {
                // 生产期间的带子状态由主运行页控制，这一页不要装作知道
                txtRunState.Text = "生产中（由主运行页控制）";
            }
        }

        /// <summary>滑杆 0–1000 → 显示 0.0–100.0（%），与设计稿 txtCurSpeed 的 30.0 对齐。</summary>
        private static string FormatSpeed(int trackValue)
        {
            return (trackValue / 10.0).ToString("0.0");
        }
    }
}
