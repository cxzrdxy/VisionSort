using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DobotArmBridge.Native
{
    /*
     * 结构体 / 枚举 / P/Invoke 声明：逐字照抄官方 demo
     *   Dobot Demo V2.3-zh\DobotDemoForCSharp-master\DobotClientDemo2.0\CPlusDll\DobotDll.cs
     *   Dobot Demo V2.3-zh\DobotDemoForCSharp-master\DobotClientDemo2.0\CPlusDll\DobotDllType.cs
     * 只保留本项目真正用到的部分；签名一个字都没改（改签名比改代码危险得多）。
     *
     * 三个必须照抄、不能"顺手优化"的地方：
     *   ① Pack=1 不能漏：JogCmd / PtpCmd / EndTypeParams 在官方声明里是 Pack=1，
     *      字节对齐错了发给机械臂的载荷就是错的；
     *   ② Pose / JOGJointParams 里的 ByValArray 是内联数组，调用前必须自己 new 出来；
     *   ③ bool 保持官方写法：C++ 的 bool 是 1 字节，但 x86 cdecl 下参数在栈上按 4 字节传递，
     *      .NET 默认的 4 字节 BOOL 正好兼容（这也是官方 demo 能跑通的原因），
     *      特意改成 byte 或 [MarshalAs(UnmanagedType.I1)] 反而没必要。
     */

    /// <summary>位姿（官方 Pose）：x/y/z/r + 4 个关节角。</summary>
    internal struct Pose
    {
        public float x;
        public float y;
        public float z;
        public float rHead;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] jointAngle;
    }

    /// <summary>回零指令（官方 HOMECmd）：temp 保留字段。</summary>
    internal struct HOMECmd
    {
        public int temp;
    }

    /// <summary>
    /// 官方 PTPMode 枚举。**注意 JUMP=0、MOVJ=1、MOVL=2**，
    /// 不是"第一个是 MOVJ"——写错会把关节运动当成门型运动执行。
    /// </summary>
    internal enum PtpMode : byte
    {
        JumpXYZ = 0,
        MovJXYZ = 1,
        MovLXYZ = 2
    }

    /// <summary>PTP 定点指令（官方 PTPCmd，Pack=1）。</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct PtpCmd
    {
        public byte ptpMode;
        public float x;
        public float y;
        public float z;
        public float rHead;
    }

    /// <summary>点动指令（官方 JogCmd，Pack=1）：isJoint=1 关节模式 / 0 坐标模式；cmd 见 JogCmdCode。</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct JogCmd
    {
        public byte isJoint;
        public byte cmd;
    }

    /// <summary>官方点动方向码（JogCmdType）：0=停，1/2=A±，3/4=B±，5/6=C±，7/8=D±。</summary>
    internal static class JogCmdCode
    {
        internal const byte Idle = 0;
        internal const byte Max = 8;
    }

    /// <summary>JOG 公共参数（官方 JOGCommonParams）：速度比 / 加速度比（%）。</summary>
    internal struct JogCommonParams
    {
        public float velocityRatio;
        public float accelerationRatio;
    }

    /// <summary>PTP 坐标参数（官方 PTPCoordinateParams）。</summary>
    internal struct PtpCoordinateParams
    {
        public float xyzVelocity;
        public float rVelocity;
        public float xyzAcceleration;
        public float rAcceleration;
    }

    /// <summary>PTP 公共参数（官方 PTPCommonParams）。</summary>
    internal struct PtpCommonParams
    {
        public float velocityRatio;
        public float accelerationRatio;
    }

    /// <summary>ConnectDobot 的返回码（官方 DobotConnect）。</summary>
    internal enum ConnectResult
    {
        NoError = 0,
        NotFound = 1,
        Occupied = 2
    }

    /// <summary>
    /// 其它命令的返回码（官方 DobotCommunicate）。
    /// **注意 1/2 与 ConnectResult 含义不同**（这里是 BufferFull/Timeout），映射时别混用。
    /// </summary>
    internal enum CommunicateResult
    {
        NoError = 0,
        BufferFull = 1,
        Timeout = 2,
        InvalidParams = 3
    }

    /// <summary>
    /// 官方 DobotDll.dll 的托管包装：只声明本项目真正用到的导出函数。
    /// 声明的“唯一正确来源”是官方 demo（见文件头），不要照着手册自己拼签名。
    /// </summary>
    internal static class DobotDll
    {
        private const string Dll = "DobotDll.dll";

        /// <summary>官方 Magician 固定波特率 115200-8-N-1。</summary>
        internal const int BaudRate = 115200;

        // ── 连接与生命周期 ────────────────────────────────────────────────
        [DllImport(Dll, EntryPoint = "ConnectDobot", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int ConnectDobot(string portName, int baudrate, StringBuilder fwType, StringBuilder version);

        [DllImport(Dll, EntryPoint = "DisconnectDobot", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DisconnectDobot();

        [DllImport(Dll, EntryPoint = "SetCmdTimeout", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetCmdTimeout(uint ms);

        // ── 机械臂内部队列 ───────────────────────────────────────────────
        // 官方 demo 连上就 SetQueuedCmdClear + SetQueuedCmdStartExec，
        // 此后 isQueued=true 的运动会被队列自动执行（不做这一步，运动只会入队不动）。
        [DllImport(Dll, EntryPoint = "SetQueuedCmdClear", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetQueuedCmdClear();

        [DllImport(Dll, EntryPoint = "SetQueuedCmdStartExec", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetQueuedCmdStartExec();

        [DllImport(Dll, EntryPoint = "SetQueuedCmdStopExec", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetQueuedCmdStopExec();

        // ── 位姿与报警 ───────────────────────────────────────────────────
        [DllImport(Dll, EntryPoint = "GetPose", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetPose(ref Pose pose);

        [DllImport(Dll, EntryPoint = "GetAlarmsState", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetAlarmsState(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] alarmsState, ref uint len, int maxLen);

        [DllImport(Dll, EntryPoint = "ClearAllAlarmsState", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int ClearAllAlarmsState();

        // ── 动作 ─────────────────────────────────────────────────────────
        [DllImport(Dll, EntryPoint = "SetHOMECmd", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetHOMECmd(ref HOMECmd homeCmd, bool isQueued, ref ulong queuedCmdIndex);

        [DllImport(Dll, EntryPoint = "SetPTPCmd", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetPTPCmd(ref PtpCmd ptpCmd, bool isQueued, ref ulong queuedCmdIndex);

        [DllImport(Dll, EntryPoint = "SetJOGCmd", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetJOGCmd(ref JogCmd jogCmd, bool isQueued, ref ulong queuedCmdIndex);

        [DllImport(Dll, EntryPoint = "SetEndEffectorSuctionCup", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetEndEffectorSuctionCup(bool enableCtrl, bool on, bool isQueued, ref ulong queuedCmdIndex);

        // ── 运动参数（默认不下发，见 BridgeCommands.ApplyMotionParams） ──
        [DllImport(Dll, EntryPoint = "SetJOGCommonParams", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetJOGCommonParams(ref JogCommonParams jogCommonParams, bool isQueued, ref ulong queuedCmdIndex);

        [DllImport(Dll, EntryPoint = "SetPTPCoordinateParams", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetPTPCoordinateParams(ref PtpCoordinateParams ptpCoordinateParams, bool isQueued, ref ulong queuedCmdIndex);

        [DllImport(Dll, EntryPoint = "SetPTPCommonParams", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetPTPCommonParams(ref PtpCommonParams ptpCommonParams, bool isQueued, ref ulong queuedCmdIndex);
    }
}
