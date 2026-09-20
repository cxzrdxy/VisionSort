using System;
using System.Globalization;
using System.Text;
using System.Threading;

namespace DobotArmBridge
{
    /// <summary>
    /// 桥接进程入口：x86 控制台程序，唯一职责是"把 IPC 命令翻译成官方 DobotDll.dll 调用"。
    ///
    /// 为什么必须是独立进程（见 机械臂TCP控制方案.md §一）：
    ///   官方 DobotDll.dll 只有 32 位版本，而 VisionSort/VisionPro 是 x64。
    ///   把 P/Invoke 写进类库并不能隔离——类库还是跑在 VisionSort.exe 进程里，
    ///   真正起作用的是"独立进程"本身。
    /// </summary>
    internal static class Program
    {
        /// <summary>单实例互斥体：同时跑两个会抢同一个串口。</summary>
        private const string MutexName = "Global\\DobotArmBridge";

        private const int DefaultPort = 18899;

        private static int Main(string[] args)
        {
            int port = ParsePort(args);
            try { Console.OutputEncoding = Encoding.UTF8; }
            catch (Exception) { /* 无控制台（被重定向/服务方式启动）时忽略 */ }

            BridgeLog log = new BridgeLog(AppDomain.CurrentDomain.BaseDirectory);
            log.Write("DobotArmBridge " + BridgeCommands.BridgeVersion
                + " 启动：端口 " + port + "，进程位数 " + (Environment.Is64BitProcess ? "x64" : "x86"));

            bool createdNew;
            using (Mutex mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    log.Write("已有一个桥接进程在运行，本实例退出（退出码 3）。");
                    return 3;
                }

                AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
                {
                    log.Write("未处理异常（进程即将结束）：" + e.ExceptionObject);
                };

                using (BridgeCommands commands = new BridgeCommands(log.Write))
                using (BridgeServer server = new BridgeServer(port, commands, log.Write))
                {
                    Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e)
                    {
                        e.Cancel = true;                 // 不直接杀进程，走收尾
                        log.Write("收到 Ctrl+C，准备退出");
                        server.Stop();
                    };

                    try
                    {
                        server.Run();
                    }
                    catch (Exception ex)
                    {
                        log.Write("主循环异常终止：" + ex);
                        return 1;
                    }
                }

                log.Write("桥接进程退出。");
                return 0;
            }
        }

        private static int ParsePort(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], "--port", StringComparison.OrdinalIgnoreCase)) continue;

                int port;
                if (int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out port)
                    && port >= 1 && port <= 65535)
                {
                    return port;
                }
            }
            return DefaultPort;
        }
    }
}
