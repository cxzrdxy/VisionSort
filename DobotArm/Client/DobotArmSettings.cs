using System;

namespace DobotArm.Client
{
    /// <summary>
    /// 机械臂通信参数。刻意不依赖 WinForms/Cognex，纯数据；
    /// VisionSort 侧的界面配置（RobotArmSettings）在连接前映射到这里。
    ///
    /// 与 v2.1（自实现官方协议 + UDP 直连机械臂）的区别：
    ///   不再有机械臂 IP / 传输方式 / 波特率——这些现在都归 x86 桥接进程管，
    ///   本类只管"怎么找到桥接进程"和"连接后的时序参数"。
    /// </summary>
    public sealed class DobotArmSettings
    {
        /// <summary>桥接进程的监听端口（回环 UDP 127.0.0.1）。</summary>
        public int Port = 18899;

        /// <summary>
        /// 机械臂串口号（官方 Magician 固定 115200-8-N-1，由桥接进程使用）。
        /// 留空或填 "auto" ＝让官方 DLL 自动搜索设备（现场不知道 COM 号时用）。
        /// </summary>
        public string SerialPortName = "COM3";

        /// <summary>单次 IPC 往返超时(ms)。一次往返＝IPC + 官方 DLL 内部调用，比 v2.1 的 300ms 宽。</summary>
        public int TimeoutMs = 2000;

        /// <summary>
        /// 连接类命令的超时(ms)。**必须给足**：实测官方 DLL 连接一个非机械臂的串口
        /// 也要 6 秒以上（它在探测设备版本），给短了会误报超时。
        /// </summary>
        public int ConnectTimeoutMs = 10000;

        /// <summary>幂等命令（读取、清报警、断开）无应答时的额外重发次数。</summary>
        public int Retries = 3;

        /// <summary>到位判定轮询间隔(ms)。</summary>
        public int MotionPollMs = 80;

        /// <summary>到位容差(mm)：三轴都进这个范围且连续两次满足即认为到位。</summary>
        public double ArriveToleranceMm = 0.5;

        /// <summary>到位超时(ms)。</summary>
        public int ArriveTimeoutMs = 5000;

        /// <summary>是否由本客户端自动拉起桥接进程（关掉＝现场手动运行，调试用）。</summary>
        public bool AutoStartBridge = true;

        /// <summary>桥接进程 exe 路径（相对 <see cref="AppDomain.BaseDirectory"/>）。</summary>
        public string BridgeExePath = @"ArmBridge\DobotArmBridge.exe";

        /// <summary>拉起桥接进程后的探活超时(ms)。</summary>
        public int BridgeStartTimeoutMs = 5000;

        /// <summary>
        /// 是否在**连接时把上位机的运动参数写进机械臂**（JOG 公共参数 72 / PTP 坐标参数 81 / PTP 公共参数 83）。
        ///
        /// 这些参数存在机械臂固件里（掉电保留，且与 DobotStudio 共用同一份），所以"每连接一次就写一次"
        /// ＝**用上位机的值覆盖现场配置**；写坏了 VisionSort 退出后依然留在机械臂里。
        /// 默认 false 就是"只读不写"的保守姿态：现场已经调好的速度参数不去动它。
        ///
        /// 什么时候要打开：机械臂的速度参数被写成 0 时，点动/定点命令会被**接受但机械臂不动**（不报错），
        /// 这是最难排查的现场故障；写下一套已知可用的值就能恢复。
        ///
        /// **覆盖范围有限，别指望它包治**：点动的真实速度 = 每轴绝对速度（70 关节 / 71 坐标）× 倍率（72），
        /// 而这里只发 72（倍率）+ 81（PTP 绝对值）+ 83（PTP 倍率）→ 能修"定点不动"；
        /// 但若机械臂里 71 的坐标每轴速度是 0，**点动仍然不会动**（20% × 0 = 0）。
        /// 要连点动一起救，得把 71 也加进下发清单（见 机械臂TCP控制方案.md §12.4）。
        /// </summary>
        public bool ConfigureMotionParams = false;

        /// <summary>JOG 速度比(%)（ConfigureMotionParams=true 时下发，命令 72）。</summary>
        public float JogVelocityRatio = 20f;

        /// <summary>JOG 加速度比(%)（命令 72）。</summary>
        public float JogAccelerationRatio = 20f;

        /// <summary>PTP 坐标速度(mm/s)（命令 81）。</summary>
        public float PtpCoordinateVelocity = 100f;

        /// <summary>PTP 坐标加速度(mm/s²)（命令 81）。</summary>
        public float PtpCoordinateAcceleration = 100f;

        /// <summary>PTP 速度比(%)（命令 83）。</summary>
        public float PtpVelocityRatio = 50f;

        /// <summary>PTP 加速度比(%)（命令 83）。</summary>
        public float PtpAccelerationRatio = 50f;
    }
}
