using System;
using System.IO;
using System.Text;

namespace DobotArmBridge
{
    /// <summary>
    /// 桥接进程的日志：stdout 逐行 Flush（现场可重定向）+ exe 同目录 DobotArmBridge.log 追加。
    /// 现场没人盯着控制台，所以文件日志是必需的；超过 2 MB 直接重开，避免无限增长。
    /// </summary>
    internal sealed class BridgeLog
    {
        private const long MaxBytes = 2 * 1024 * 1024;

        private readonly object _gate = new object();
        private readonly string _path;

        internal BridgeLog(string directory)
        {
            _path = Path.Combine(directory, "DobotArmBridge.log");
        }

        internal void Write(string message)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + message;
            lock (_gate)
            {
                WriteConsole(line);
                WriteFile(line);
            }
        }

        private static void WriteConsole(string line)
        {
            try
            {
                Console.WriteLine(line);
                Console.Out.Flush();
            }
            catch (Exception)
            {
                // 控制台被关掉/重定向到已关闭的管道：日志文件仍在，不因写不了控制台而中断服务
            }
        }

        private void WriteFile(string line)
        {
            try
            {
                FileInfo info = new FileInfo(_path);
                if (info.Exists && info.Length > MaxBytes) File.Delete(_path);
                File.AppendAllText(_path, line + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception)
            {
                // 目录只读/被占用：不能让日志失败拖垮桥接进程
            }
        }
    }
}
