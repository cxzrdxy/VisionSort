using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using DobotArm.Ipc;
using DobotArmBridge.Native;

namespace DobotArmBridge
{
    /// <summary>
    /// IPC 命令分发：每条命令翻译成一次官方 DobotDll.dll 调用，再把结果翻回一行 KV。
    ///
    /// 由 BridgeServer **单线程串行**调用 —— 官方 DLL 是有共享状态的原生库，
    /// 并发调用会错乱；串行也天然满足"同一时刻只有一个请求在飞"。
    /// </summary>
    internal sealed class BridgeCommands : IDisposable
    {
        /// <summary>桥接进程自身的版本（客户端用来发现"旧 exe 配新客户端"）。</summary>
        internal const string BridgeVersion = "1.0.0";

        private readonly Action<string> _log;
        private readonly List<string> _noisy = new List<string> { BridgeCmd.Pose, BridgeCmd.Ping };

        private bool _connected;
        private string _port = string.Empty;
        private string _fw = string.Empty;
        private string _ver = string.Empty;

        internal BridgeCommands(Action<string> log)
        {
            _log = log ?? delegate { };
        }

        /// <summary>收到 shutdown 后置位；BridgeServer 回完包就退出主循环。</summary>
        internal bool ShutdownRequested { get; private set; }

        internal BridgeMessage Execute(BridgeMessage request)
        {
            BridgeMessage response = new BridgeMessage()
                .Set(BridgeKey.Seq, request.Seq)
                .Set(BridgeKey.Ok, true);

            string cmd = request.Command;
            try
            {
                if (!_noisy.Contains(cmd)) _log("← " + request.ToLine());

                switch (cmd)
                {
                    case BridgeCmd.Ping: return Ping(response);
                    case BridgeCmd.Search: return Search(response);
                    case BridgeCmd.Connect: return Connect(request, response);
                    case BridgeCmd.Disconnect: return Disconnect(response);
                    case BridgeCmd.Pose: return PoseCommand(response);
                    case BridgeCmd.Alarms: return Alarms(response);
                    case BridgeCmd.ClearAlarms: return ClearAlarms(response);
                    case BridgeCmd.Home: return Home(response);
                    case BridgeCmd.MoveJ: return MoveJ(request, response);
                    case BridgeCmd.Jog: return Jog(request, response);
                    case BridgeCmd.Suction: return Suction(request, response);
                    case BridgeCmd.Shutdown: return Shutdown(response);
                    default:
                        return Fail(response, BridgeError.BadCommand,
                            cmd.Length == 0 ? "请求里缺少 cmd 字段" : "未知命令：" + cmd);
                }
            }
            catch (DllNotFoundException ex)
            {
                // 这三类异常是"桥接进程本身没配好"，中文说清楚比原始英文有用得多
                return Fail(response, BridgeError.DllMissing,
                    "找不到 DobotDll.dll 或其依赖（exe 同目录的 6 个原生 DLL 是否齐全？）：" + ex.Message);
            }
            catch (BadImageFormatException ex)
            {
                return Fail(response, BridgeError.Arch,
                    "位数不符：桥接进程与 DobotDll.dll 都必须是 x86：" + ex.Message);
            }
            catch (EntryPointNotFoundException ex)
            {
                return Fail(response, BridgeError.EntryPointMissing,
                    "DobotDll.dll 里没有这个导出函数（版本不符？）：" + ex.Message);
            }
            catch (Exception ex)
            {
                return Fail(response, BridgeError.BadCommand, ex.GetType().Name + "：" + ex.Message);
            }
        }

        // ── 状态类 ──────────────────────────────────────────────────────

        private BridgeMessage Ping(BridgeMessage response)
        {
            return response
                .Set(BridgeKey.Connected, _connected)
                .Set(BridgeKey.Port, _port)
                .Set(BridgeKey.Fw, _fw)
                .Set(BridgeKey.Ver, _ver)
                .Set(BridgeKey.Bridge, BridgeVersion);
        }

        private BridgeMessage Search(BridgeMessage response)
        {
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports, StringComparer.OrdinalIgnoreCase);
            return response.Set(BridgeKey.Ports, string.Join(",", ports));
        }

        private BridgeMessage Connect(BridgeMessage request, BridgeMessage response)
        {
            string port = request.GetString(BridgeKey.Port, "COM3");
            int timeout = request.GetInt(BridgeKey.Timeout, 3000);
            bool motion = request.GetBool(BridgeKey.Motion, false);

            // port 留空或 "auto" → 走官方 demo 的连接方式 ConnectDobot("")：让 DLL 自己搜串口设备。
            // 这与"显式给串口号"的返回码语义不同（实测）：
            //   显式串口号 → 打不开就返回 Occupied(2)（串口不存在或被占用都算）
            //   自动搜索   → 搜不到设备才返回 NotFound(1)，现场"机械臂没插/没开机"看得最清楚
            bool auto = port.Length == 0 || string.Equals(port, "auto", StringComparison.OrdinalIgnoreCase);
            string label = auto ? "自动搜索" : port;
            string key = auto ? "auto" : port;

            // 幂等：已经连着同一个口就直接复用。
            // 少了这一步，VisionSort 第二次连接会因为"自己占着串口"收到 Occupied。
            if (_connected && string.Equals(_port, key, StringComparison.OrdinalIgnoreCase))
            {
                _log("connect：已连接 " + _port + "，直接复用");
                return ConnectedInfo(response);
            }
            if (_connected)
            {
                _log("connect：连接目标变化（" + _port + " → " + key + "），先断开");
                DisconnectCore();
            }

            StringBuilder fw = new StringBuilder(64);
            StringBuilder ver = new StringBuilder(64);
            int ret = DobotDll.ConnectDobot(auto ? string.Empty : port, DobotDll.BaudRate, fw, ver);
            if (ret != (int)ConnectResult.NoError)
            {
                _log("connect：" + label + " 失败（官方返回码 " + ret + "）");
                return Fail(response, ConnectErrorOf(ret), ConnectMessageOf(ret, label));
            }

            _connected = true;
            _port = key;
            _fw = fw.ToString();
            _ver = ver.ToString();

            DobotDll.SetCmdTimeout((uint)timeout);
            // 官方 demo 的连接时序：清空并启动机械臂内部队列。
            // 此后 isQueued=true 的运动才会被执行，不做这一步只会入队不动。
            DobotDll.SetQueuedCmdClear();
            DobotDll.SetQueuedCmdStartExec();
            if (motion) ApplyMotionParams(request);

            _log("connect：成功 " + port + "（固件 " + _fw + " / 版本 " + _ver + "）"
                + (motion ? "，已下发运动参数" : "，未改动机械臂原有运动参数"));
            return ConnectedInfo(response);
        }

        private BridgeMessage Disconnect(BridgeMessage response)
        {
            if (!_connected)
            {
                _log("disconnect：本来就没连接，忽略");
                return response;
            }
            DisconnectCore();
            _log("disconnect：已断开");
            return response;
        }

        private BridgeMessage Shutdown(BridgeMessage response)
        {
            _log("shutdown：准备退出");
            if (_connected) DisconnectCore();
            ShutdownRequested = true;
            return response;
        }

        private BridgeMessage ConnectedInfo(BridgeMessage response)
        {
            return response
                .Set(BridgeKey.Connected, true)
                .Set(BridgeKey.Port, _port)
                .Set(BridgeKey.Fw, _fw)
                .Set(BridgeKey.Ver, _ver);
        }

        // ── 读取类 ──────────────────────────────────────────────────────

        private BridgeMessage PoseCommand(BridgeMessage response)
        {
            if (!_connected) return NotConnected(response);

            Pose pose = new Pose();
            pose.jointAngle = new float[4];      // ByValArray 是内联数组，必须自己分配
            int ret = DobotDll.GetPose(ref pose);
            if (ret != (int)CommunicateResult.NoError) return Fail(response, CommErrorOf(ret), CommMessageOf(ret));

            return response
                .Set(BridgeKey.X, pose.x)
                .Set(BridgeKey.Y, pose.y)
                .Set(BridgeKey.Z, pose.z)
                .Set(BridgeKey.R, pose.rHead)
                .Set(BridgeKey.J1, pose.jointAngle[0])
                .Set(BridgeKey.J2, pose.jointAngle[1])
                .Set(BridgeKey.J3, pose.jointAngle[2])
                .Set(BridgeKey.J4, pose.jointAngle[3]);
        }

        private BridgeMessage Alarms(BridgeMessage response)
        {
            if (!_connected) return NotConnected(response);

            byte[] buffer = new byte[16];
            uint len = (uint)buffer.Length;
            int ret = DobotDll.GetAlarmsState(buffer, ref len, buffer.Length);
            if (ret != (int)CommunicateResult.NoError) return Fail(response, CommErrorOf(ret), CommMessageOf(ret));

            StringBuilder hex = new StringBuilder(buffer.Length * 2);
            bool active = false;
            for (int i = 0; i < buffer.Length; i++)
            {
                hex.Append(buffer[i].ToString("X2"));
                if (buffer[i] != 0) active = true;
            }
            return response.Set(BridgeKey.Alarms, hex.ToString()).Set(BridgeKey.Active, active);
        }

        private BridgeMessage ClearAlarms(BridgeMessage response)
        {
            if (!_connected) return NotConnected(response);

            int ret = DobotDll.ClearAllAlarmsState();
            if (ret != (int)CommunicateResult.NoError) return Fail(response, CommErrorOf(ret), CommMessageOf(ret));
            return response;
        }

        // ── 动作类 ──────────────────────────────────────────────────────

        private BridgeMessage Home(BridgeMessage response)
        {
            if (!_connected) return NotConnected(response);

            HOMECmd cmd = new HOMECmd();
            cmd.temp = 0;
            ulong index = 0;
            // 运动类 isQueued=true（机械臂内部队列已由 connect 启动），返回队列索引便于判到位
            int ret = DobotDll.SetHOMECmd(ref cmd, true, ref index);
            if (ret != (int)CommunicateResult.NoError) return Fail(response, CommErrorOf(ret), CommMessageOf(ret));
            return response.Set(BridgeKey.Index, index);
        }

        private BridgeMessage MoveJ(BridgeMessage request, BridgeMessage response)
        {
            if (!_connected) return NotConnected(response);

            PtpCmd cmd = new PtpCmd();
            cmd.ptpMode = (byte)PtpMode.MovJXYZ;     // 官方枚举 MOVJ=1（JUMP 才是 0，别写反）
            cmd.x = request.GetFloat(BridgeKey.X, 0f);
            cmd.y = request.GetFloat(BridgeKey.Y, 0f);
            cmd.z = request.GetFloat(BridgeKey.Z, 0f);
            cmd.rHead = request.GetFloat(BridgeKey.R, 0f);

            ulong index = 0;
            int ret = DobotDll.SetPTPCmd(ref cmd, true, ref index);
            if (ret != (int)CommunicateResult.NoError) return Fail(response, CommErrorOf(ret), CommMessageOf(ret));
            return response.Set(BridgeKey.Index, index);
        }

        private BridgeMessage Jog(BridgeMessage request, BridgeMessage response)
        {
            if (!_connected) return NotConnected(response);

            int code = request.GetInt(BridgeKey.Dir, JogCmdCode.Idle);
            if (code < 0 || code > JogCmdCode.Max)
            {
                return Fail(response, BridgeError.InvalidParams,
                    "点动方向码 dir 必须是 0–8（0=停，1/2=A±，3/4=B±，5/6=C±，7/8=D±）");
            }

            JogCmd cmd = new JogCmd();
            cmd.isJoint = (byte)(request.GetBool(BridgeKey.Joint, false) ? 1 : 0);
            cmd.cmd = (byte)code;

            ulong index = 0;
            // 点动是立即执行（官方 demo：isQueued=false），队列索引在这里没有意义，不回给客户端
            int ret = DobotDll.SetJOGCmd(ref cmd, false, ref index);
            if (ret != (int)CommunicateResult.NoError) return Fail(response, CommErrorOf(ret), CommMessageOf(ret));
            return response;
        }

        private BridgeMessage Suction(BridgeMessage request, BridgeMessage response)
        {
            if (!_connected) return NotConnected(response);

            bool enable = request.GetBool(BridgeKey.Enable, true);
            bool on = request.GetBool(BridgeKey.On, false);

            ulong index = 0;
            // 吸盘是立即执行（官方 demo：isQueued=false）
            int ret = DobotDll.SetEndEffectorSuctionCup(enable, on, false, ref index);
            if (ret != (int)CommunicateResult.NoError) return Fail(response, CommErrorOf(ret), CommMessageOf(ret));
            return response;
        }

        // ── 运动参数（默认不下发） ───────────────────────────────────────

        /// <summary>
        /// 只在 connect 的 motion=1 时下发，顺序照 v2.1 已实测的 72 → 81 → 83。
        ///
        /// **刻意不下发 70/71（JOG 关节/坐标的每轴绝对速度）**：那是绝对值，会覆盖现场已调好的每轴速度；
        /// 而 72 只是倍率（20% 这种），作用在原有速度之上、影响面小得多。
        ///
        /// 代价必须清楚：若机械臂里 71（坐标每轴）的速度是 0，光发倍率救不回点动（20% × 0 = 0）。
        /// 官方 demo 的 SetParam() 是发 70 的（JOG 关节 200/200），但它不做坐标点动，所以够用；
        /// 我们做坐标点动，要救"点不动"就得把 71 一起下发（见 机械臂TCP控制方案.md §12.4）。
        /// </summary>
        private void ApplyMotionParams(BridgeMessage request)
        {
            ulong index = 0;

            JogCommonParams jog = new JogCommonParams();
            jog.velocityRatio = request.GetFloat(BridgeKey.JogRatio, 20f);
            jog.accelerationRatio = request.GetFloat(BridgeKey.JogAccRatio, 20f);
            DobotDll.SetJOGCommonParams(ref jog, false, ref index);

            PtpCoordinateParams ptp = new PtpCoordinateParams();
            ptp.xyzVelocity = request.GetFloat(BridgeKey.PtpVel, 100f);
            ptp.rVelocity = request.GetFloat(BridgeKey.PtpVel, 100f);
            ptp.xyzAcceleration = request.GetFloat(BridgeKey.PtpAcc, 100f);
            ptp.rAcceleration = request.GetFloat(BridgeKey.PtpAcc, 100f);
            DobotDll.SetPTPCoordinateParams(ref ptp, false, ref index);

            PtpCommonParams common = new PtpCommonParams();
            common.velocityRatio = request.GetFloat(BridgeKey.PtpRatio, 50f);
            common.accelerationRatio = request.GetFloat(BridgeKey.PtpAccRatio, 50f);
            DobotDll.SetPTPCommonParams(ref common, false, ref index);
        }

        // ── 收尾 ────────────────────────────────────────────────────────

        /// <summary>
        /// 进程退出时兜底释放串口（正常路径是 shutdown 命令；
        /// Ctrl+C、控制台被关、异常退出都靠这里把 COM 口还给系统）。
        /// </summary>
        public void Dispose()
        {
            if (_connected) DisconnectCore();
        }

        private void DisconnectCore()
        {
            try { DobotDll.SetQueuedCmdStopExec(); }
            catch (Exception ex) { _log("停止队列失败（忽略）：" + ex.Message); }
            try { DobotDll.DisconnectDobot(); }
            catch (Exception ex) { _log("断开机械臂失败（忽略）：" + ex.Message); }

            _connected = false;
            _port = string.Empty;
            _fw = string.Empty;
            _ver = string.Empty;
        }

        private static BridgeMessage Fail(BridgeMessage response, string error, string message)
        {
            return response
                .Set(BridgeKey.Ok, false)
                .Set(BridgeKey.Err, error)
                .Set(BridgeKey.Msg, message);
        }

        private static BridgeMessage NotConnected(BridgeMessage response)
        {
            return Fail(response, BridgeError.NotConnected, "机械臂未连接，请先连接");
        }

        private static string ConnectErrorOf(int ret)
        {
            if (ret == (int)ConnectResult.NotFound) return BridgeError.NotFound;
            if (ret == (int)ConnectResult.Occupied) return BridgeError.Occupied;
            return "DOBOT_CONNECT_" + ret;
        }

        private static string ConnectMessageOf(int ret, string label)
        {
            if (ret == (int)ConnectResult.NotFound)
                return "没找到机械臂（" + label + "）：确认 USB 线已插、机械臂已开机";

            if (ret == (int)ConnectResult.Occupied)
            {
                // 实测（2026-09-17，本机无机械臂）：显式给串口号时，官方 DLL 对"串口打不开"
                // 一律返回 Occupied——串口被占用、串口号不存在都算，只有空串（自动搜索）
                // 才是"没搜到设备"的 NotFound。所以这里把"本机现有串口"直接列出来，现场一眼能对上。
                return "串口 " + label + " 打不开：可能被其它程序占用（请关闭 DobotStudio、官方 demo），"
                     + "也可能这个 COM 号不存在；本机现有串口：" + AvailablePortsText()
                     + "（可把串口号填 auto 让官方 DLL 自动搜索）";
            }
            return "连接机械臂失败，官方返回码 " + ret;
        }

        /// <summary>本机串口列表（给报错信息用）；枚举失败不能影响报错本身。</summary>
        private static string AvailablePortsText()
        {
            try
            {
                string[] ports = SerialPort.GetPortNames();
                return ports.Length == 0 ? "（一个都没有）" : string.Join(",", ports);
            }
            catch (Exception)
            {
                return "（枚举失败）";
            }
        }

        private static string CommErrorOf(int ret)
        {
            if (ret == (int)CommunicateResult.BufferFull) return BridgeError.BufferFull;
            if (ret == (int)CommunicateResult.Timeout) return BridgeError.Timeout;
            if (ret == (int)CommunicateResult.InvalidParams) return BridgeError.InvalidParams;
            return "DOBOT_COMM_" + ret;
        }

        private static string CommMessageOf(int ret)
        {
            if (ret == (int)CommunicateResult.BufferFull) return "机械臂命令缓冲已满";
            if (ret == (int)CommunicateResult.Timeout) return "机械臂通信超时";
            if (ret == (int)CommunicateResult.InvalidParams) return "参数非法（坐标是否超出范围？）";
            return "机械臂通信失败，官方返回码 " + ret;
        }
    }
}
