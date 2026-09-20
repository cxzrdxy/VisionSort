using NModbus.IO;
using System.IO.Ports;

namespace VisionSort.Services
{
    /// <summary>
    /// NModbus 3.x 删掉了 2.x 的串口直连（ModbusSerialMaster.CreateRtu 已不存在，反射确认），
    /// 必须自写这个适配器：包 SerialPort.BaseStream，实现 IStreamResource。
    /// Dispose 负责关串口；生命周期由 ConveyorSettings 统一管（它同时持有 master 与本对象）。
    /// </summary>
    internal sealed class SerialPortStreamResource : IStreamResource
    {
        private readonly SerialPort _port;

        public SerialPortStreamResource(SerialPort port)
        {
            _port = port;
        }

        public int InfiniteTimeout
        {
            get { return SerialPort.InfiniteTimeout; }
        }

        public int ReadTimeout
        {
            get { return _port.ReadTimeout; }
            set { _port.ReadTimeout = value; }
        }

        public int WriteTimeout
        {
            get { return _port.WriteTimeout; }
            set { _port.WriteTimeout = value; }
        }

        public void DiscardInBuffer()
        {
            _port.DiscardInBuffer();
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return _port.BaseStream.Read(buffer, offset, count);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            _port.BaseStream.Write(buffer, offset, count);
        }

        public void Dispose()
        {
            try
            {
                if (_port.IsOpen) _port.Close();
            }
            catch (System.Exception)
            {
                // 关闭路径不抛异常
            }
            _port.Dispose();
        }
    }
}
