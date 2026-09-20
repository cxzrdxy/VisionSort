using System;
using System.Threading.Tasks;
using DobotArm.Client;

namespace VisionSort.Services
{
    /// <summary>
    /// 一次设备操作的结果（为什么不用 out 参数：异步方法不允许 out 参数，CS1988）。
    /// </summary>
    public sealed class OpResult
    {
        /// <summary>成功否。</summary>
        public bool Ok;

        /// <summary>中文说明（失败时是可直接显示给操作工的原因）。</summary>
        public string Message = string.Empty;
    }

    /// <summary>
    /// 机械臂共享服务（「主运行页方案.md」Q1 的同类问题：客户端也只能有一份）。
    ///
    /// 为什么必须共享：v3.0 是"**一个** x86 桥接进程 ↔ 一条回环 UDP"，两个客户端各发各的会把命令交错，
    /// 现场表现是"机械臂偶尔乱走/报超时"。所以调试页与主运行页共用一个 <see cref="DobotArmClient"/>，
    /// 由类库内部的锁串行化。
    ///
    /// 调试页原来的 <c>private readonly DobotArmClient _arm = new DobotArmClient();</c>
    /// 已改成代理到 <see cref="Client"/>（一行改动，页面逻辑不变）。
    /// </summary>
    public sealed class RobotArmService
    {
        /// <summary>唯一实例。</summary>
        public static RobotArmService Shared { get; } = new RobotArmService();

        /// <summary>共享客户端（调试页与主运行页共用；命令由类库内部串行化）。</summary>
        public DobotArmClient Client { get; } = new DobotArmClient();

        private RobotArmService()
        {
        }

        /// <summary>是否已连接机械臂。</summary>
        public bool IsConnected
        {
            get { return Client.IsConnected; }
        }

        /// <summary>
        /// 确保已连接：没连就按 <see cref="RobotArmSettings.Shared"/> 里的参数连一次。
        /// 参数映射与调试页保持一致（端口/串口/各类超时/是否自动拉起桥接进程）。
        /// </summary>
        public async Task<OpResult> EnsureConnectedAsync()
        {
            if (Client.IsConnected)
            {
                return new OpResult { Ok = true, Message = "机械臂已连接。" };
            }

            RobotArmSettings s = RobotArmSettings.Shared;
            try
            {
                Client.Settings.Port = s.Port;
                Client.Settings.SerialPortName = s.SerialPortName;
                Client.Settings.TimeoutMs = s.CommandTimeoutMs;
                Client.Settings.ConnectTimeoutMs = s.ConnectTimeoutMs;
                Client.Settings.AutoStartBridge = s.AutoStartBridge;
                Client.Settings.MotionPollMs = s.MotionPollMs;
                Client.Settings.ArriveToleranceMm = s.ArriveToleranceMm;
                Client.Settings.ArriveTimeoutMs = s.ArriveTimeoutMs;

                await Client.ConnectAsync();
                return new OpResult { Ok = true, Message = "机械臂已连接。" };
            }
            catch (Exception ex)
            {
                return new OpResult { Ok = false, Message = "连接机械臂失败：" + ex.Message };
            }
        }
    }
}
