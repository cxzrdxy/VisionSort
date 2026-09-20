using System;
using System.Drawing;
using System.Windows.Forms;
using VisionSort.Services;

namespace VisionSort.Views
{
    /// <summary>
    /// 设备调试页 · **生产互锁**（「主运行页方案.md」Q13=A / §17.4 D）。
    ///
    /// 为什么必须有：传送带和机械臂各只有**一份**物理资源（一条串口 / 一条回环 UDP 上的一个桥接进程）。
    /// 主运行页在跑生产时，如果有人到这一页按「正转」或点动，两边就会各发各的命令 ——
    /// 现场表现是"机械臂偶尔乱走 / 报超时 / 带子停不下来"，而且**很难复现**。
    /// 所以只要 `ProductionRunner.IsRunning`，这一页的**一切会动设备的东西全部置灰**。
    ///
    /// 为什么不干脆禁用整页：操作工还是需要看状态、看位姿、翻「操作指引」。
    /// 灰的是"能改状态"的（含标定区的计算/保存/几何框 —— 它们会改生产正在读的那份数据），
    /// 留的是"只读"的（操作指引）。
    ///
    /// 触发时机：订阅 `ProductionRunner.PhaseChanged`（状态一变立刻刷，不用等 500ms 轮询），
    /// 加上 OnLoad 时主动刷一次（进这一页时可能生产已经在跑了）。
    /// </summary>
    public partial class DeviceDebugView
    {
        /// <summary>生产正在跑（＝本页锁定）。</summary>
        private bool _productionLock;

        /// <summary>订阅生产状态变化（由 Conveyor partial 的 OnLoad 调用）。</summary>
        private void HookProductionLock()
        {
            ProductionRunner.Shared.PhaseChanged += ProductionRunner_PhaseChanged;
            ApplyProductionLock();
        }

        /// <summary>退订（由 Conveyor partial 的 Disposed 调用）。</summary>
        private void UnhookProductionLock()
        {
            try { ProductionRunner.Shared.PhaseChanged -= ProductionRunner_PhaseChanged; }
            catch (Exception) { }
        }

        /// <summary>事件在**后台线程**触发，回 UI 线程再碰控件。</summary>
        private void ProductionRunner_PhaseChanged(RunPhase phase, string detail)
        {
            OnDeviceUi(ApplyProductionLock);
        }

        /// <summary>统一的"回 UI 线程"入口（控件已销毁时安静丢弃，与 MainRunView.OnUi 同款）。</summary>
        private void OnDeviceUi(Action action)
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
                // 控件正在销毁：丢掉这次刷新即可
            }
        }

        /// <summary>按"生产在不在跑"重新决定这一页哪些东西可动。</summary>
        private void ApplyProductionLock()
        {
            bool running = ProductionRunner.Shared.IsRunning;

            if (running && !_productionLock)
            {
                // 正在锁定：先把还可能动着的两件事停下来
                StopJogIfActive();     // 按住点动时如果正好开始生产，机械臂会一直走
                _robotTimer.Stop();    // 不再轮询位姿（省下共享客户端的往返，生产要它）
                _stateTimer.Stop();    // 不再轮询 Modbus 状态寄存器（同上）
            }

            _productionLock = running;

            RefreshConveyorUi();
            RefreshRobotUi();
            RefreshCalibUi();

            // 组标题上直接写明白 —— 光把按钮变灰，现场会以为"程序卡了"
            grpConveyor.Text = running ? "传送带 Modbus 控制（生产中，已锁定）" : "传送带 Modbus 控制";
            grpRobot.Text = running ? "机械臂控制（生产中，已锁定）" : "机械臂控制";

            if (!running)
            {
                // 解锁：把轮询还回去（原来连着才轮询）
                if (_arm.IsConnected) _robotTimer.Start();
                if (ConveyorSettings.Shared.IsConnected) _stateTimer.Start();
            }
        }
    }
}
