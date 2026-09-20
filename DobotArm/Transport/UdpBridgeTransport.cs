using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DobotArm.Transport
{
    /// <summary>
    /// 回环 UDP 传输：往 127.0.0.1:Port 的桥接进程发一行 KV、收一行 KV（UTF-8、单包一行）。
    ///
    /// 只负责"发一行 / 收一行（带超时）"；序号匹配、超时重发、迟到包丢弃都在
    /// <see cref="DobotArm.Client.DobotArmClient"/> 里（与 v2.1 的分工一致）。
    /// 桥接进程是"收到就回"，回环上不会丢包，但仍按 UDP 的规矩做超时与重发。
    /// </summary>
    internal sealed class UdpBridgeTransport : IDisposable
    {
        private readonly int _port;
        private UdpClient _client;
        private IPEndPoint _remote;

        internal UdpBridgeTransport(int port)
        {
            _port = port;
        }

        internal string Name
        {
            get { return "127.0.0.1:" + _port; }
        }

        internal bool IsOpen
        {
            get { return _client != null; }
        }

        internal void Open()
        {
            if (_client != null) return;

            _remote = new IPEndPoint(IPAddress.Loopback, _port);
            _client = new UdpClient(0);                  // 本机端口由系统分配
            _client.Client.ReceiveTimeout = 1000;        // 兜底；实际超时用 ReceiveLine(timeoutMs)
            _client.Connect(_remote);                    // 连接后只收回环上这台进程的包
        }

        internal void Send(string line)
        {
            UdpClient client = _client;
            if (client == null) throw new InvalidOperationException("桥接传输未打开。");
            byte[] bytes = Encoding.UTF8.GetBytes(line);
            client.Send(bytes, bytes.Length);
        }

        /// <summary>在超时窗口内收一行；超时或对端不可达返回 null。</summary>
        internal string ReceiveLine(int timeoutMs)
        {
            UdpClient client = _client;
            if (client == null) return null;

            DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (true)
            {
                int remain = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
                if (remain <= 0) return null;

                try
                {
                    client.Client.ReceiveTimeout = remain;
                    IPEndPoint from = null;
                    return Encoding.UTF8.GetString(client.Receive(ref from));
                }
                catch (SocketException ex)
                {
                    // TimedOut / WouldBlock：这一轮没收到。
                    // ConnectionReset：桥接进程没在运行（Windows 收到 ICMP 端口不可达后回报），
                    // 同样按"没收到"处理，交给上层按超时/重试决定。
                    if (ex.SocketErrorCode == SocketError.TimedOut ||
                        ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.ConnectionReset)
                    {
                        return null;
                    }
                    throw;
                }
            }
        }

        internal void Close()
        {
            UdpClient client = _client;
            _client = null;
            if (client != null)
            {
                try { client.Close(); }
                catch (Exception) { }
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
