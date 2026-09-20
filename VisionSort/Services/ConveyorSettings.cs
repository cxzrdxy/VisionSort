using NModbus;
using NModbus.IO;
using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace VisionSort.Services
{
    /// <summary>
    /// 传送带 Modbus RTU 配置 + 共享客户端（本方案的“唯一真相源”）。
    /// MainForm 只 new 视图、不做接线，所以三页共用静态单例 <see cref="Shared"/>：
    /// SysConfigView 改配置，DeviceDebugView 做控制，主运行页将来跑流程。
    ///
    /// 寄存器语义（**以现场给的示例工程 `day30\03-code\MyModBus\...\BeltControl.cs` 为准，2026-09-18 更正**）：
    ///   站号 2；**100=启停（写 1=启动 / 写 0=停止）**；**101=方向（写 0=反转 / 写 1=正转）**；102=速度（ushort 直写）。
    ///   早先这里写的"100=正转启停、101=反转启停、停止=两边都写 0、正反转互锁"**是错的**，已按上面更正。
    ///   **换向不需要"先停另一边"**——这台传送带没这么高级：动作只写自己那一条寄存器（你 2026-09-18 确认）。
    ///   故障复位：示例代码里没有这个寄存器，暂定=停止（写 100=0）；现场有专用复位寄存器时改 ResetAsync 一处。
    ///
    /// 串口固定 9600/None/8/1（示例值），读写超时 3 秒；全部 Async（示例风格，超时不卡 UI）。
    /// 本类不读不写配置文件；参数落盘是 SysConfigView 的事（config\sysparam.json）。
    /// </summary>
    public sealed class ConveyorSettings
    {
        /// <summary>三页共享的唯一实例。</summary>
        public static ConveyorSettings Shared { get; } = new ConveyorSettings();

        // ---- 配置（与 SysConfigView 界面双向同步，初值与 Designer 默认值对齐） ----
        public string PortName = "COM5";
        public int BaudRate = 9600;
        public byte SlaveId = 2;
        public ushort RegFwd = 100;
        public ushort RegRev = 101;
        public ushort RegSpeed = 102;

        private IModbusMaster _master;
        private IStreamResource _resource;        // 与 _master 同生命周期，断开时一起释放

        /// <summary>
        /// 串口是单通道，NModbus 主站不是线程安全的：界面 500ms 状态轮询 + 按钮动作 + 将来主运行页
        /// 会并发进来。实测（自测探针）并发读写会串包——问的响应被另一次调用吃掉，表现为"刚点正转
        /// 但回读还是停止"。所有对 _master 的访问都必须过这道闸。
        /// </summary>
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

        private ConveyorSettings()
        {
        }

        /// <summary>是否已连接。</summary>
        public bool IsConnected
        {
            get { return _master != null; }
        }

        /// <summary>
        /// 连接：建传输层 → 建 RTU 主站 → 空读一次速度寄存器验活。
        /// 失败时自己清理干净再抛（调用方只管弹提示，不用管资源）。
        /// </summary>
        public async Task ConnectAsync()
        {
            if (IsConnected) return;

            IStreamResource resource = CreateResource();
            try
            {
                ModbusFactory factory = new ModbusFactory();
                IModbusSerialTransport transport = factory.CreateRtuTransport(resource);
                _master = factory.CreateMaster(transport);
                _resource = resource;

                await ReadSpeedAsync().ConfigureAwait(false);   // 验活：不通这里就抛
            }
            catch (Exception)
            {
                DisposeConnection();
                throw;
            }
        }

        /// <summary>断开：释放主站（连带传输层）与串口。从不抛异常。</summary>
        public void Disconnect()
        {
            DisposeConnection();
        }

        /// <summary>正转：写启停寄存器 <c>100=1</c>。</summary>
        public Task ForwardAsync()
        {
            EnsureConnected();
            return RunLocked(delegate
            {
                return _master.WriteSingleRegisterAsync(SlaveId, RegFwd, 1);
            });
        }

        /// <summary>反转：写方向寄存器 <c>101=0</c>（0 = 反转）。</summary>
        public Task ReverseAsync()
        {
            EnsureConnected();
            return RunLocked(delegate
            {
                return _master.WriteSingleRegisterAsync(SlaveId, RegRev, 0);
            });
        }

        /// <summary>停止：只写启停寄存器 <c>100=0</c>。</summary>
        public Task StopAsync()
        {
            EnsureConnected();
            return RunLocked(delegate
            {
                return _master.WriteSingleRegisterAsync(SlaveId, RegFwd, 0);
            });
        }

        /// <summary>故障复位：暂定=停止（示例代码无此寄存器，见类注释）。</summary>
        public Task ResetAsync()
        {
            return StopAsync();
        }

        /// <summary>设速度：写速度寄存器（0–1000 对应 0–100%，滑杆旁证）。</summary>
        public Task SetSpeedAsync(ushort value)
        {
            EnsureConnected();
            return RunLocked(delegate
            {
                return _master.WriteSingleRegisterAsync(SlaveId, RegSpeed, value);
            });
        }

        /// <summary>读速度寄存器原始值。</summary>
        public async Task<ushort> ReadSpeedAsync()
        {
            ushort[] values = await ReadHoldingAsync(RegSpeed, 1).ConfigureAwait(false);
            return values[0];
        }

        /// <summary>读运行状态：正转 / 反转 / 停止（一次读 100、101 两个寄存器，同一把锁内完成）。</summary>
        public async Task<string> ReadRunStateAsync()
        {
            ushort[] values = await ReadHoldingAsync(RegFwd, 2).ConfigureAwait(false);
            if (values[0] != 0) return "正转";
            if (values[1] != 0) return "反转";
            return "停止";
        }

        /// <summary>上锁执行一串写操作。</summary>
        private async Task RunLocked(Func<Task> work)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                await work().ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>上锁读保持寄存器。</summary>
        private async Task<ushort[]> ReadHoldingAsync(ushort start, ushort count)
        {
            EnsureConnected();
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                return await _master.ReadHoldingRegistersAsync(SlaveId, start, count).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        private IStreamResource CreateResource()
        {
            SerialPort port = new SerialPort(PortName, BaudRate, Parity.None, 8, StopBits.One);
            port.ReadTimeout = 3000;
            port.WriteTimeout = 3000;
            try
            {
                port.Open();
            }
            catch (Exception)
            {
                port.Dispose();
                throw;
            }
            return new SerialPortStreamResource(port);
        }

        private void DisposeConnection()
        {
            try
            {
                IDisposable master = _master as IDisposable;
                if (master != null) master.Dispose();
            }
            catch (Exception)
            {
            }

            try
            {
                if (_resource != null) _resource.Dispose();
            }
            catch (Exception)
            {
            }
            finally
            {
                _master = null;
                _resource = null;
            }
        }

        private void EnsureConnected()
        {
            if (!IsConnected) throw new InvalidOperationException("Modbus 未连接。");
        }
    }
}
