using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DobotArm.Ipc
{
    /*
     * VisionSort(x64) ↔ DobotArmBridge(x86) 之间的线协议。
     *
     *   请求：cmd=moveJ;seq=7;x=200.0000;y=30.0000;z=-50.0000;r=0.0000
     *   应答：ok=1;seq=7;index=12
     *   失败：ok=0;seq=7;err=NOT_FOUND;msg=没找到机械臂：确认 USB 线已插
     *
     * 形态：回环 UDP（127.0.0.1:18899）上一行 key=value，字段用 ';' 分隔，UTF-8 编码，
     *       单包一行、不含换行（值里的 ; = % CR LF 会被转义掉，见 Escape）。
     * 为什么不用 JSON：命令只有十来条、字段是扁平数字与短字符串，KV 零依赖、零转义坑，
     *       而且人能手打一条直接验证桥接进程（见 机械臂TCP控制方案.md §6.1 S1）。
     *
     * 本文件被 **DobotArm（客户端类库）与 DobotArmBridge（x86 桥接进程）同时编译**
     * （两边 csproj 里用 <Compile Include="..\Shared\BridgeProtocol.cs" Link="..."/> 链接），
     * 所以线协议永远只有一份定义，不会两侧各自漂移。
     */

    /// <summary>IPC 命令名（请求里的 cmd 字段）。</summary>
    public static class BridgeCmd
    {
        public const string Ping = "ping";
        public const string Search = "search";
        public const string Connect = "connect";
        public const string Disconnect = "disconnect";
        public const string Pose = "pose";
        public const string Alarms = "alarms";
        public const string ClearAlarms = "clearAlarms";
        public const string Home = "home";
        public const string MoveJ = "moveJ";
        public const string Jog = "jog";
        public const string Suction = "suction";
        public const string Shutdown = "shutdown";
    }

    /// <summary>IPC 字段名（键）。</summary>
    public static class BridgeKey
    {
        // 报文框架
        public const string Cmd = "cmd";
        public const string Seq = "seq";
        public const string Ok = "ok";
        public const string Err = "err";
        public const string Msg = "msg";

        // connect
        public const string Port = "port";
        public const string Timeout = "timeout";
        public const string Motion = "motion";
        public const string JogRatio = "jogRatio";
        public const string JogAccRatio = "jogAccRatio";
        public const string PtpVel = "ptpVel";
        public const string PtpAcc = "ptpAcc";
        public const string PtpRatio = "ptpRatio";
        public const string PtpAccRatio = "ptpAccRatio";

        // ping / connect 应答
        public const string Connected = "connected";
        public const string Fw = "fw";
        public const string Ver = "ver";
        public const string Bridge = "bridge";

        // search / pose / alarms / 队列索引
        public const string Ports = "ports";
        public const string Index = "index";
        public const string Alarms = "alarms";
        public const string Active = "active";

        // 位姿
        public const string X = "x";
        public const string Y = "y";
        public const string Z = "z";
        public const string R = "r";
        public const string J1 = "j1";
        public const string J2 = "j2";
        public const string J3 = "j3";
        public const string J4 = "j4";

        // jog / suction
        public const string Joint = "joint";
        /// <summary>
        /// 点动方向码（0=停，1/2=A±，3/4=B±，5/6=C±，7/8=D±）。
        /// **不能叫 cmd**：帧框架已经用 cmd 表示"命令名"，同名会互相覆盖
        /// （`cmd=jog;joint=0;cmd=1` 会把命令名变成 "1"）。
        /// </summary>
        public const string Dir = "dir";
        public const string Enable = "enable";
        public const string On = "on";
    }

    /// <summary>IPC 错误码（应答里的 err 字段；msg 是给人看的中文说明）。</summary>
    public static class BridgeError
    {
        /// <summary>没找到设备（串口号不对 / 机械臂没开机 / 线没插）。</summary>
        public const string NotFound = "NOT_FOUND";
        /// <summary>串口被占用（DobotStudio、官方 demo 或另一个程序占着）。</summary>
        public const string Occupied = "OCCUPIED";
        /// <summary>机械臂通信超时。</summary>
        public const string Timeout = "TIMEOUT";
        /// <summary>机械臂命令缓冲满。</summary>
        public const string BufferFull = "BUFFER_FULL";
        /// <summary>参数非法（坐标超范围等）。</summary>
        public const string InvalidParams = "INVALID_PARAMS";
        /// <summary>机械臂未连接。</summary>
        public const string NotConnected = "NOT_CONNECTED";
        /// <summary>未知命令 / 请求格式不对。</summary>
        public const string BadCommand = "BAD_COMMAND";
        /// <summary>找不到 DobotDll.dll 或它的依赖（deps 不全）。</summary>
        public const string DllMissing = "DLL_MISSING";
        /// <summary>位数不符（桥接进程或 DobotDll.dll 不是 x86）。</summary>
        public const string Arch = "ARCH";
        /// <summary>DobotDll.dll 版本里没有这个导出函数。</summary>
        public const string EntryPointMissing = "DLL_ENTRY";
        /// <summary>桥接进程没运行（客户端自己产生，不由桥接返回）。</summary>
        public const string BridgeDown = "BRIDGE_DOWN";
    }

    /// <summary>一行 KV 报文：既是请求也是应答。</summary>
    public sealed class BridgeMessage
    {
        private readonly Dictionary<string, string> _map = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<string> _order = new List<string>();

        public string Command { get { return GetString(BridgeKey.Cmd, string.Empty); } }

        public int Seq { get { return GetInt(BridgeKey.Seq, 0); } }

        public BridgeMessage Set(string key, string value)
        {
            if (!_map.ContainsKey(key)) _order.Add(key);
            _map[key] = value ?? string.Empty;
            return this;
        }

        public BridgeMessage Set(string key, int value)
        {
            return Set(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public BridgeMessage Set(string key, ulong value)
        {
            return Set(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public BridgeMessage Set(string key, bool value)
        {
            return Set(key, value ? "1" : "0");
        }

        /// <summary>float/double 一律按不变区域格式化（"F4"，与语言区域无关）。</summary>
        public BridgeMessage Set(string key, double value)
        {
            return Set(key, value.ToString("F4", CultureInfo.InvariantCulture));
        }

        public bool Has(string key)
        {
            return _map.ContainsKey(key);
        }

        public string GetString(string key, string fallback)
        {
            string value;
            return _map.TryGetValue(key, out value) ? value : fallback;
        }

        public int GetInt(string key, int fallback)
        {
            int value;
            return int.TryParse(GetString(key, string.Empty), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? value : fallback;
        }

        public ulong GetULong(string key, ulong fallback)
        {
            ulong value;
            return ulong.TryParse(GetString(key, string.Empty), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? value : fallback;
        }

        public float GetFloat(string key, float fallback)
        {
            float value;
            return float.TryParse(GetString(key, string.Empty), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                ? value : fallback;
        }

        public double GetDouble(string key, double fallback)
        {
            double value;
            return double.TryParse(GetString(key, string.Empty), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                ? value : fallback;
        }

        /// <summary>只认 "1"/"0"（以及 true/false），其它一律回退默认值。</summary>
        public bool GetBool(string key, bool fallback)
        {
            string value = GetString(key, string.Empty);
            if (value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (value == "0" || string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)) return false;
            return fallback;
        }

        /// <summary>序列化成一行（字段顺序＝设置顺序，便于人读与日志排查）。</summary>
        public string ToLine()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _order.Count; i++)
            {
                string key = _order[i];
                if (i > 0) sb.Append(';');
                sb.Append(Escape(key)).Append('=').Append(Escape(_map[key]));
            }
            return sb.ToString();
        }

        /// <summary>解析一行；坏字段跳过而不抛异常（UDP 上什么都可能收到）。</summary>
        public static BridgeMessage Parse(string line)
        {
            BridgeMessage message = new BridgeMessage();
            if (string.IsNullOrEmpty(line)) return message;

            string[] parts = line.Trim().Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.Length == 0) continue;
                int eq = part.IndexOf('=');
                if (eq <= 0) continue;
                message.Set(Unescape(part.Substring(0, eq)), Unescape(part.Substring(eq + 1)));
            }
            return message;
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            StringBuilder sb = new StringBuilder(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                switch (value[i])
                {
                    case '%': sb.Append("%25"); break;
                    case ';': sb.Append("%3B"); break;
                    case '=': sb.Append("%3D"); break;
                    case '\r': sb.Append("%0D"); break;
                    case '\n': sb.Append("%0A"); break;
                    default: sb.Append(value[i]); break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 只还原 Escape 产生的那 5 个转义（%25 %3B %3D %0D %0A），其它 '%' 原样保留。
        /// 刻意不做通用的百分号解码：我们从不生成 %XX 形式的 UTF-8 字节，
        /// 按字节解码中文只会得到乱码；原样留着，至少肉眼能看出"这里被人手改过"。
        /// 顺序：%25 必须最后替换，才能和 Escape 精确对称（"%253B" → "%3B" 而不是 ";"）。
        /// </summary>
        private static string Unescape(string value)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOf('%') < 0) return value ?? string.Empty;
            return value
                .Replace("%0D", "\r")
                .Replace("%0A", "\n")
                .Replace("%3B", ";")
                .Replace("%3D", "=")
                .Replace("%25", "%");
        }
    }
}
