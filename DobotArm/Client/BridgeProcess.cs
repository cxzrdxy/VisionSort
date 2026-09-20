using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using DobotArm.Ipc;

namespace DobotArm.Client
{
    /// <summary>
    /// 桥接进程（<c>ArmBridge\DobotArmBridge.exe</c>，**x86**）的生命周期管理：
    /// 需要时拉起它，退出时（**只关我们自己拉起的**）发 shutdown。
    ///
    /// 它是唯一宿主官方 DobotDll.dll 的地方——原生 DLL 只有 32 位版本，
    /// 必须靠独立进程与 x64 的 VisionSort 隔离（见 机械臂TCP控制方案.md §一）。
    /// </summary>
    internal sealed class BridgeProcess : IDisposable
    {
        private readonly DobotArmSettings _settings;
        private readonly Action<string> _log;
        private Process _process;

        internal BridgeProcess(DobotArmSettings settings, Action<string> log)
        {
            _settings = settings;
            _log = log ?? delegate { };
        }

        /// <summary>拉起桥接进程（已经是我们拉起的且还活着，就什么都不做）。</summary>
        internal void StartIfNeeded()
        {
            if (_process != null && !_process.HasExited) return;

            string path = ResolvePath();
            if (!File.Exists(path))
            {
                throw new ArmException("找不到桥接进程：" + path
                    + "（构建 VisionSort 会自动把它拷到 ArmBridge\\ 子目录；"
                    + "也可以关掉「自动启动桥接进程」，手动双击运行它）");
            }

            ProcessStartInfo info = new ProcessStartInfo(path, "--port " + _settings.Port);
            info.WorkingDirectory = Path.GetDirectoryName(path);
            info.UseShellExecute = false;
            info.CreateNoWindow = true;      // 无控制台窗口；日志在同目录 DobotArmBridge.log

            _process = Process.Start(info);
            _log("已拉起桥接进程：" + path + "（PID " + _process.Id + "）");
        }

        /// <summary>
        /// 退出：只处理我们自己拉起的进程（手动启动的留给现场自己管）。
        /// 先发 shutdown（尽力而为，不依赖主传输），再等它自己退出；等不到就留着——
        /// 它是个独立服务，且下次连接会被 ping 发现并复用，不会冲突。
        /// </summary>
        internal void Shutdown()
        {
            if (_process == null) return;

            try
            {
                if (_process.HasExited) return;
            }
            catch (Exception)
            {
                return;
            }

            SendShutdown();

            try
            {
                if (_process.WaitForExit(2000))
                {
                    _log("桥接进程已退出（PID " + _process.Id + "）");
                }
                else
                {
                    _log("桥接进程 2 秒内没退出，留着它（下次连接会自动复用）");
                }
            }
            catch (Exception ex)
            {
                _log("等待桥接进程退出失败（忽略）：" + ex.Message);
            }
        }

        /// <summary>单独发一条 shutdown：不占用主传输，Dispose 时主传输可能已经关了。</summary>
        private void SendShutdown()
        {
            try
            {
                using (UdpClient udp = new UdpClient())
                {
                    udp.Connect(new IPEndPoint(IPAddress.Loopback, _settings.Port));
                    BridgeMessage message = new BridgeMessage()
                        .Set(BridgeKey.Cmd, BridgeCmd.Shutdown)
                        .Set(BridgeKey.Seq, 0);
                    byte[] bytes = Encoding.UTF8.GetBytes(message.ToLine());
                    udp.Send(bytes, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                _log("发送 shutdown 失败（忽略）：" + ex.Message);
            }
        }

        private string ResolvePath()
        {
            string configured = _settings.BridgeExePath;
            if (string.IsNullOrEmpty(configured)) configured = "DobotArmBridge.exe";

            return Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configured);
        }

        public void Dispose()
        {
            Shutdown();
        }
    }
}
