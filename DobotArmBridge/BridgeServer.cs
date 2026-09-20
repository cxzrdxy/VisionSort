using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using DobotArm.Ipc;

namespace DobotArmBridge
{
    /// <summary>
    /// 回环 UDP 服务：收一行 KV → 交给 BridgeCommands → 把应答回给请求方。
    /// 只绑 127.0.0.1，外部网络根本连不上（天然的安全边界）。
    /// 单线程串行处理：官方 DLL 是共享状态的原生库，不能并发调用。
    /// </summary>
    internal sealed class BridgeServer : IDisposable
    {
        /// <summary>收包超时(ms)：给"停止标志"和关闭窗口留检查点，不然会一直阻塞在 Receive。</summary>
        private const int ReceiveTimeoutMs = 400;

        private readonly UdpClient _udp;
        private readonly BridgeCommands _commands;
        private readonly Action<string> _log;
        private volatile bool _stop;

        internal BridgeServer(int port, BridgeCommands commands, Action<string> log)
        {
            _commands = commands;
            _log = log;

            _udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
            _udp.Client.ReceiveTimeout = ReceiveTimeoutMs;
        }

        internal IPEndPoint LocalEndPoint
        {
            get { return (IPEndPoint)_udp.Client.LocalEndPoint; }
        }

        internal void Run()
        {
            _log("监听 " + LocalEndPoint + "（仅回环，x86 进程）");
            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);

            while (!_stop && !_commands.ShutdownRequested)
            {
                byte[] data;
                try
                {
                    data = _udp.Receive(ref remote);
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    continue;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset
                                             || ex.SocketErrorCode == SocketError.NetworkReset)
                {
                    // 上一轮回包时对方进程已退出 → Windows 回 ICMP 端口不可达 → 这里报 WSAECONNRESET。
                    // 不是故障，继续等下一条请求。
                    continue;
                }

                BridgeMessage response = _commands.Execute(BridgeMessage.Parse(Encoding.UTF8.GetString(data)));
                if (!response.GetBool(BridgeKey.Ok, true)) _log("→ " + response.ToLine());

                byte[] reply = Encoding.UTF8.GetBytes(response.ToLine());
                try
                {
                    _udp.Send(reply, reply.Length, remote);
                }
                catch (SocketException ex)
                {
                    _log("回包失败（忽略）：" + ex.SocketErrorCode);
                }
            }

            _log("主循环结束（" + (_commands.ShutdownRequested ? "收到 shutdown" : "收到停止信号") + "）");
        }

        internal void Stop()
        {
            _stop = true;
        }

        public void Dispose()
        {
            _stop = true;
            try { _udp.Close(); }
            catch (Exception) { }
        }
    }
}
