# 机械臂控制 · 实施方案（v3.0：官方 DobotDll.dll + x86 桥接进程 + UDP 回环 IPC）

> 项目：**`DobotArmBridge`**（新增，x86 控制台进程，宿主官方原生 DLL） + **`DobotArm`**（改造为 UDP 客户端类库） + `D:\visionPor\20260914-资料\04-WinForms\VisionSort`（x64 引用方）
> 页面：`Views/DeviceDebugView.cs` → `grpRobot`「机械臂控制」 + `Views/SysConfigView.cs` → `grpRobotCfg`「◆ 机械臂配置」
> 版本：v3.0（**已落盘并验证** —— 三步全部完成，见 §十 / §十一 / §十二；本机可做的验证已做完，只剩现场真机 T-live）
> 已定：机械臂 **USB 线常连** → 可用官方 DLL；架构选 **甲（桥接进程 + 官方 DLL）**。
> 沿用不改：机型 **Dobot Magician**；Q2 点动＝机械臂自身 JOG；Q3 **软件端队列**（§9 留档）；Q4 定时 `GetPose` 轮询；Q5 只做吸盘、真空反馈显示 `--`；步距＝按住连续 JOG + 单击走一步。
> 文件名 `机械臂TCP控制方案.md` 是历史名，v1.4 才是 TCP 文本，v2.1 是官方二进制协议，v3.0 是官方 DLL —— 文件名保持不变只为不打断已有引用。

---

## 一、本次最关键的结论：必须是"独立**进程**"，不是"独立**类库**"

你上一条的想法是"既然已经独立封装成项目了，就能用官方 DLL 而不与原项目冲突"。**前半句对、后半句不成立**，本机证据如下：

| 事实 | 证据 |
|---|---|
| 官方 `DobotDll.dll` **只有 x86（32 位）** | PE 头 `Machine=0x014C`（178176 字节）。官方 demo 下 4 份 + 我们入库与部署的副本合计 11 份，**全是同一份 32 位拷贝，没有任何 x64 版本**（2026-09-20 复核：`deps\DobotDll.dll` = 0x014C，`DobotArmBridge.exe` = 0x014C，`VisionSort.exe` = 0x8664） |
| 官方 demo 自己就是 **x86** | `DobotClientDemo2.0.csproj` → `<PlatformTarget>x86</PlatformTarget>` |
| 它连带 6 个原生依赖，全是 x86，共 ~6.9 MB | `DobotDll.dll` 174 KB + `Qt5Core` 4557 KB + `Qt5Network` 832 KB + `Qt5SerialPort` 58 KB + `msvcp120` 445 KB + `msvcr120` 948 KB |
| VisionSort 是 **x64** | 现有工程（Cognex VisionPro 9.0 CR2 也是 x64） |
| 官方 DLL **没有网络接口** | C# 侧只有 `SearchDobot(nameList,maxLen)` 与 `ConnectDobot(portName,baudrate,fwType,version)` 两个连接入口，**没有任何 IP/UDP 参数**；官方 demo 传的是 `ConnectDobot("", 115200, …)`（空串＝自动搜串口） |

两条推论：

1. **程序集 ≠ 进程。** 把 P/Invoke 写进 `DobotArm` 类库，`DobotArm.dll` 仍然是**加载进 `VisionSort.exe` 进程内部**运行的，它 P/Invoke 的 `DobotDll.dll` 也必然被加载进**同一个进程** —— x64 进程加载 x86 原生 DLL，第一次调用就 `BadImageFormatException`。拆项目只解决**编译期**（不必往 `VisionSort.csproj` 塞 x86 引用），**运行时位数冲突原样存在**。
2. **官方 DLL 自己不走 UDP。** 它是串口（USB COM）接口。所以"官方 DLL + UDP"唯一自洽的形态是：**UDP 用在 VisionSort ↔ 桥接进程之间**，桥接进程内部用官方 DLL 走 USB 串口连机械臂。

结论：**用官方 DLL ⇒ 机械臂必须 USB 常连（你已确认），且必须由独立的 x86 进程承载。**

---

## 二、目标架构

```
┌──────────────────────────────┐         ┌────────────────────────────────────┐
│ VisionSort.exe   (x64)       │         │ DobotArmBridge.exe      (x86)      │
│  ├ DeviceDebugView.Robot.cs  │  UDP    │  ├ Program.cs      单实例/主循环     │
│  ├ SysConfigView.Robot.cs    │ 回环    │  ├ BridgeServer.cs 收包/回包/KV 解析 │
│  └ DobotArm.dll (AnyCPU 类库)│◀───────▶│  ├ BridgeCommands.cs 12 条命令分发   │
│      ├ UdpBridgeTransport    │127.0.0.1│  ├ Native/DobotApi.cs P/Invoke       │
│      ├ BridgeLine (KV 编解码)│ :18899  │  └ deps\ DobotDll + Qt5×3 + msvc×2  │
│      └ BridgeProcess (拉起)  │         └──────────────┬─────────────────────┘
└──────────────────────────────┘                        │ USB 串口 115200-8-N-1
                                                        ▼
                                                 Dobot Magician (4 轴)
```

- **桥接进程是唯一接触原生 DLL 的地方**：x86、独立进程、崩了也带不走 VisionPro。
- **VisionSort 侧零原生依赖**：仍然只引用纯托管 `DobotArm.dll`（AnyCPU），架构与 v2.1 完全一致。
- 桥接进程还能**脱离 VisionPro 单独启动、单独手工验证**（§6 S1），这是它在现场最大的价值。

---

## 三、IPC 协议：回环 UDP + 单行 KV 文本

### 3.1 为什么不用 JSON、也不复用官方二进制帧

| 方案 | 评价 |
|---|---|
| 复用官方二进制帧（`ArmFrame`） | 桥接进程是**直接调 DLL**，不组帧；把帧再翻回函数调用是多绕一圈，且要留着已验证但已无用途的帧层 |
| JSON（`DataContractJsonSerializer`） | net48 自带可用但笨重；NuGet 不通引不进 Newtonsoft；引 `System.Web.Extensions` 只为拼几个字段不划算 |
| **单行 KV（选它）** | `cmd=moveJ;seq=3;x=200;y=30;z=-50;r=0` —— 零依赖、零转义坑、**人能读、能手打**（现场可用 PowerShell 直接发一条验证，见 §6 S1） |

### 3.2 报文格式

```
请求（VisionSort → 桥接）：cmd=<名字>[;key=value]…[;seq=<n>]
应答（桥接 → VisionSort）：ok=1[;key=value]…;seq=<n>        成功
                            ok=0;err=<错误码>;msg=<中文说明>;seq=<n>   失败
编码：ASCII/UTF-8，单包一行，无换行；字段顺序无关；缺省字段用默认值
传输：UDP，VisionSort → 127.0.0.1:18899；应答回发到请求的源端口
```

- `seq` 由客户端递增，**应答必须原样带回**（UDP 无连接，靠它丢弃迟到包）。桥接对无 `seq` 的请求也接收（便于手工测试），应答 `seq=0`。
- 单包最大 ~200 字节，回环上远小于 MTU，**不存在分片**。

### 3.3 命令表（12 条，全部有界面/生命周期调用方 —— 无死代码）

| IPC cmd | 官方 DLL 调用 | 请求字段 | 应答字段 |
|---|---|---|---|
| `ping` | — | — | `connected`(0/1), `port`, `fw`, `ver`, `bridge`(版本) |
| `search` | `SerialPort.GetPortNames()` | — | `ports=COM3,COM4` |
| `connect` | `ConnectDobot(port,115200,fw,ver)` → `SetCmdTimeout` → `SetQueuedCmdClear` → `SetQueuedCmdStartExec`（+ 可选运动参数） | `port`(COM3；留空或 `auto`＝官方自动搜索), `timeout`(3000), `motion`(0/1) + 6 个运动参数 | `fw`, `ver` |
| `disconnect` | `SetQueuedCmdStopExec` → `DisconnectDobot` | — | — |
| `pose` | `GetPose(ref pose)` | — | `x,y,z,r,j1,j2,j3,j4` |
| `alarms` | `GetAlarmsState(buf,len,16)` | — | `alarms=<32 位十六进制>`, `active`(0/1) |
| `clearAlarms` | `ClearAllAlarmsState()` | — | — |
| `home` | `SetHOMECmd(ref cmd, isQueued:true, ref idx)` | — | `index` |
| `moveJ` | `SetPTPCmd(ref cmd{PTPMOVJXYZMode,x,y,z,r}, isQueued:true, ref idx)` | `x,y,z,r` | `index` |
| `jog` | `SetJOGCmd(ref cmd{isJoint,dir}, isQueued:false, ref idx)` | `joint`(0/1), `dir`(0–8；**不能叫 cmd**，`cmd` 已经被"命令名"占用，同名会互相覆盖) | — |
| `suction` | `SetEndEffectorSuctionCup(enable,on, isQueued:false, ref idx)` | `enable`(1), `on`(0/1) | — |
| `shutdown` | `disconnect` 后退出进程 | — | `ok=1`（先回包再退出） |

> **`isQueued` 的取值照抄官方 demo 的实测用法**（不是我猜的）：运动类 `SetPTPCmd(..., true, ...)` 入机械臂队列、点动 `SetJOGCmd(..., false, ...)` 立即、`SetEndEffectorSuctionCup(..., false, ...)` 立即（`MainWindow.xaml.cs` 第 228 / 359–415 / 548 / 570 行）。
> **连接后必须 `SetQueuedCmdClear` + `SetQueuedCmdStartExec`**（官方 demo `StartDobot()` 第 84–85 行就是这么干的）：机械臂队列处于"执行中"状态后，后续 `isQueued=true` 的运动会自动被执行；不做这一步，运动命令只会入队不动。

### 3.4 错误码与中文提示（`ok=0` 时 `err`/`msg`）

| 官方返回 | 含义 | 界面提示 |
|---|---|---|
| `DobotConnect_NoError`(0) | 成功 | — |
| `DobotConnect_NotFound`(1) | 没找到设备 | 没找到机械臂：确认 USB 线已插、机械臂已开机、串口号正确 |
| `DobotConnect_Occupied`(2) | 串口被占用 | 串口被占用：请关闭 DobotStudio / 官方 demo / 其它占用该串口的程序 |
| `DobotCommunicate_BufferFull`(1) | 命令缓冲满 | 幂等命令自动重试 3 次（官方 demo 用 `while` 无限重试，我们最多 3 次，避免界面卡死），仍失败则报错 |
| `DobotCommunicate_Timeout`(2) | 通信超时 | 机械臂通信超时 |
| `DobotCommunicate_InvalidParams`(3) | 参数非法 | 参数非法（坐标是否超范围？） |

除上表"官方返回码 → 中文提示"之外，客户端还会看到桥接进程自己产生的错误码（定义在 `Shared/BridgeProtocol.cs` 的 `BridgeError`，共 11 个）：

| 错误码 | 来源 | 说明 |
|---|---|---|
| `NOT_FOUND` / `OCCUPIED` / `TIMEOUT` / `BUFFER_FULL` / `INVALID_PARAMS` | 官方返回码映射 | 即上表 5 种 |
| `NOT_CONNECTED` | 桥接进程 | 未连接就发运动/读位姿命令 |
| `BAD_COMMAND` | 桥接进程 | 未知命令 / 请求里缺 `cmd` |
| `DLL_MISSING` | 桥接进程 | 找不到 `DobotDll.dll` 或它的依赖（`deps` 不全） |
| `ARCH` | 桥接进程 | 位数不符（`BadImageFormatException`） |
| `DLL_ENTRY` | 桥接进程 | DLL 里没有这个导出函数 |
| `BRIDGE_DOWN` | **客户端自己** | 桥接进程没在运行（不由桥接返回） |

`connect` 必须**幂等**：已连接且串口号相同 → 直接返回当前 `fw/ver`，不重复调 `ConnectDobot`（否则第二次会因自己占着串口返回 `Occupied`）。`disconnect` 同理幂等。

**官方返回码的实测语义（2026-09-17 本机无机械臂，真机复现的是同一套代码）**：

| 情形 | 官方返回 | 结论 |
|---|---|---|
| `connect;port=auto`（空串＝自动搜索）没搜到设备 | `NOT_FOUND`(1) | "机械臂没插/没开机"看得最清楚 → **现场首点亮优先用 auto** |
| `connect;port=COM5`（本机不存在该口） | `OCCUPIED`(2) | 显式给串口号时，"串口打不开"一律算 Occupied —— **串口不存在与被人占用分不开** |
| `connect;port=COM1`（本机存在、但不是机械臂） | **成功**(0)，`fw=DobotSerial` / `ver=0.0.0`，耗时 **> 6 秒** | **connect 成功不代表机械臂在那儿**：官方 DLL 只验证串口能打开，不校验设备身份 |

由此定下三条（已落到第 1 步代码里）：

1. 客户端 `ConnectAsync` **必须**紧跟 `pose` 验活，否则"连上了"是假的（方案 §5.1 本来就写了这一步，现在有了硬证据它不是可选项）；
2. 连接类命令超时给到 **10 秒**——慢的是官方 DLL 在探测设备，不是我们的 IPC；
3. 连接失败提示里带上"本机现有串口"列表，现场一眼能对上串口号。

---

## 四、桥接进程内部设计（`DobotArmBridge`）

| 项 | 设计 |
|---|---|
| 项目类型 | net48 **Console Application**，`<PlatformTarget>x86</PlatformTarget>`（**钉死**，任何 x64 构建都会 `BadImageFormatException`） |
| 无界面、无配置文件 | 所有参数（串口号、超时、运动参数开关）由 IPC 下发；命令行只认 `--port 18899`（默认 18899） |
| 单实例 | `Mutex("Global\\DobotArmBridge")`；已在运行则退出码 3（第二个实例不做任何事） |
| 并发模型 | **单线程串行**：主循环 `Receive` → 处理 → `Send`。官方 DLL 是有共享状态的原生库，必须串行；这也天然满足"单请求在飞" |
| 主循环 | `UdpClient` 绑定 `127.0.0.1:18899`；每条请求包整个处理完再收下一个（DLL 调用是同步阻塞的） |
| 异常兜底 | 每条命令 `try/catch`：`DllNotFoundException` → `err=DLL_MISSING`（提示 `deps` 缺失）；`BadImageFormatException` → `err=ARCH`（提示位数不符）；任何异常都不许让进程退出 |
| 日志 | stdout 逐行 `Flush`（现场可重定向）+ 同目录 `DobotArmBridge.log` 追加（现场没人盯控制台，必需） |
| GetPose 陷阱 | 结构体 `Pose` 里是 `[MarshalAs(ByValArray, SizeConst=4)] float[] jointAngle` —— 官方 demo 声明 `private Pose pose = new Pose();` 后**没有显式分配数组**；我们**显式 `pose.jointAngle = new float[4]` 再调用**（显式分配必然安全，不依赖 marshaler 行为） |
| 运动参数开关 | `ConfigureMotionParams` **默认关**（不覆盖现场已调好的参数）；打开时由桥接进程在 connect 内、先于任何动作命令下发 **72 → 81 → 83** 三条（JOG 倍率 + PTP 坐标绝对值 + PTP 倍率），值来自 `DobotArmSettings`。**官方 demo 的 `SetParam()` 其实发 6 组（70/72/80/81/82/83，含 JOG 每轴绝对值），我们刻意只发这 3 条**——覆盖范围与补救办法见 §12.4 |
| 进程退出 | `shutdown` 命令：先回包，再 `disconnect` 后退出；收到 Ctrl+C / 控制台关闭也走同一清理 |

**文件清单**

| 文件 | 职责 | 实际行数 |
|---|---|---|
| `DobotArmBridge/DobotArmBridge.csproj` | net48 + `<PlatformTarget>x86</PlatformTarget>` 钉死 + `EnsureX86` 构建期门槛 + `deps\*.dll` 作 Content 拷到输出根 | 41 |
| `DobotArmBridge/Program.cs` | 参数解析、单实例 Mutex、日志、主循环装配、退出清理 | 89 |
| `DobotArmBridge/BridgeServer.cs` | UDP 收发、KV 解析/回包、异常→错误码映射 | 91 |
| `DobotArmBridge/BridgeCommands.cs` | 12 条命令分发 + 连接时序 + 幂等 | 433 |
| `DobotArmBridge/Native/DobotApi.cs` | P/Invoke 声明（**照抄官方 demo**，取用其中 **16 个**导出函数）+ 结构体 | 187 |
| `DobotArmBridge/BridgeLog.cs` | stdout（逐行 Flush）+ `DobotArmBridge.log`（超 2 MB 重开） | 60 |
| `DobotArmBridge/deps/` | 6 个官方原生 DLL（从 `Dobot Demo V2.3-zh\...\DobotDll\` 复制） | — |

**P/Invoke 的写法（不自己猜，逐字照抄官方已验证的声明）**

来源：`Dobot Demo V2.3-zh\DobotDemoForCSharp-master\DobotClientDemo2.0\CPlusDll\DobotDll.cs`（63 个导出）与 `DobotDllType.cs`（结构体/枚举）。三条必须照抄的细节：

1. `CallingConvention.Cdecl` 一个都不能少；
2. `JogCmd` / `PTPCmd` / `EndTypeParams` 标了 `StructLayout(Pack=1)` —— 漏了字节对齐就发错载荷；
3. `Pose` / `JOGJointParams` / `JOGCoordinateParams` / `PTPJointParams` 用 `ByValArray + SizeConst` 内联数组。

> 官方 demo 里 `GetEndEffectorParams` 被复制粘贴成 5 个不同签名（第 62/67/72/77 行）——我们按需只声明真正要用的 `GetEndEffectorSuctionCup`，不继承这个错误。

---

## 五、VisionSort 侧改动

### 5.1 `DobotArm` 类库：保留 API、换掉内脏

**公开 API 保持"界面用到的那些"不变**（界面基本零改动的前提）。实际落盘后的真实签名如下——
方案初稿里写的 `DisconnectAsync` / `MoveToAsync` / `CancellationToken` 是笔误，v2.1 本来就是"同步方法 + `Async` 包装"：

```csharp
public sealed class DobotArmClient : IDisposable
{
    public DobotArmClient();                                   // 用默认 DobotArmSettings
    public DobotArmClient(DobotArmSettings settings);
    public DobotArmSettings Settings { get; }
    public Action<string> Log { get; set; }                    // 现场排查用
    public bool IsConnected { get; }
    public event Action<string> ConnectionLost;                // 断线通知
    public void Connect();                 // 确保桥接进程在跑 → connect(串口) → pose 验活
    public Task ConnectAsync();
    public void Disconnect();              // 只断串口，**桥接进程保留**（常驻服务）
    public ArmPose GetPose();                       public Task<ArmPose> GetPoseAsync();
    public void Home();                             public Task HomeAsync();
    public void ClearAlarms();                      public Task ClearAlarmsAsync();
    public void Suction(bool on);                   public Task SuctionAsync(bool on);
    public void MoveJ(double x, double y, double z, double r);
    public Task MoveJAsync(double x, double y, double z, double r);
    public void Jog(JogAxis axis, int sign, bool start);
    public Task JogAsync(JogAxis axis, int sign, bool start);
    public void MoveRelative(JogAxis axis, double delta);
    public Task MoveRelativeAsync(JogAxis axis, double delta);
    public void WaitArrive(double x, double y, double z);
    public Task WaitArriveAsync(double x, double y, double z);
    public void Dispose();                 // 断开 + 关掉**我们自己拉起的**桥接进程
}
```

**删除**（按"只保留真正活着的代码"）—— ✅ **2026-09-20 复核：下表文件与成员均已不存在**（`DobotArm` 现存仅 `Client/` 5 个文件 + `Transport/UdpBridgeTransport.cs`，`Protocol/` 目录已无）：

| 删除 | 原因 |
|---|---|
| `DobotArm/Protocol/ArmFrame.cs` | 桥接进程直接调官方 DLL，不再自己组帧/校验和 |
| `DobotArm/Protocol/ArmCommandIds.cs` | 命令号与载荷由官方 DLL 封装 |
| `DobotArm/Transport/IArmTransport.cs` | 只剩一种传输，接口没有第二个实现 |
| `DobotArm/Transport/SerialArmTransport.cs` | 串口由官方 DLL 内部处理 |
| `DobotArm/Transport/UdpArmTransport.cs` | 由 `UdpBridgeTransport` 取代（可靠性逻辑搬迁，不是重写） |
| `DobotArmClient.ReadAlarms()` / `GetDeviceVersion()` / `TransportName` | 界面从来没有调用过（v2.1 遗留）→ 按"没有调用的删了"处理。桥接进程仍保留 `alarms` 命令，现场可用一条 UDP 手工查报警位 |
| `DobotArmSettings.Ip` / `ArmTransportKind` / `SerialBaudRate` | 桥接进程固定在回环、串口由官方 DLL 管，这三个参数已无意义 |
| `DobotArm/Protocol/` 目录 | 两个文件删掉后目录空了，一并移除 |

**新增**：

| 文件 | 职责 | 实际行数 |
|---|---|---|
| `DobotArm/Transport/UdpBridgeTransport.cs` | 回环 UDP 收发一行 KV（`internal`，只负责发/收+超时） | 104 |
| `DobotArm/Client/BridgeProcess.cs` | 拉起/关闭桥接进程（`--port`、`shutdown`、只关自己拉起的） | 125 |
| `DobotArm/Client/ArmPose.cs` | `ArmPose` + `ArmException`（从 `DobotArmSettings.cs` 拆出来） | 29 |
| `DobotArm/Client/JogAxis.cs` | `JogAxis`（原先在已删除的 `ArmCommandIds.cs` 里；命名空间从 `DobotArm.Protocol` 移到 `DobotArm.Client`） | 19 |
| `04-WinForms/Shared/BridgeProtocol.cs` | **线协议唯一定义**，客户端与桥接进程各编译一份。原先计划的 `Protocol/BridgeLine.cs` 由它取代——两侧共用同一份源码，从根上避免协议漂移 | 277 |
| `DobotArm/Client/DobotArmClient.cs`（改造） | 内部全部改走 IPC；串行化/重发/迟到包/断线语义不变 | 527 |

> 实测签名与上面"保留 API"一致（2026-09-20 核对 `DobotArmClient` 公开成员）：`Settings`/`Log`/`IsConnected`/`ConnectionLost` + `Connect(Async)`/`Disconnect`/`GetPose(Async)`/`Home(Async)`/`ClearAlarms(Async)`/`Suction(Async)`/`MoveJ(Async)`/`Jog(Async)`/`MoveRelative(Async)`/`WaitArrive(Async)`/`Dispose`；
> `ReadAlarms()` / `GetDeviceVersion()` / `TransportName` 确认已删除。两个 csproj 都 `<Compile Include="..\Shared\BridgeProtocol.cs" Link="Ipc\BridgeProtocol.cs" />` 链接同一份源码。

> 协议知识不会丢：帧格式与命令号表留在**附录 B**；真要恢复代码，git 历史里有 v2.1 的完整实现。

> **（2026-09-20 修订）** 此处原有**第二张"新增"表**（列 `DobotArm/Protocol/BridgeLine.cs` 等 4 个文件、行数与上表互相矛盾）——
> 那是编辑残留：`BridgeLine.cs` **从未落盘**（它的职责由 `Shared/BridgeProtocol.cs` 承担），
> 上表才是实际清单。**该重复表已删除**，此处保留记录以免后来者以为漏读了什么。

### 5.2 设置字段（`VisionSort/Services/RobotArmSettings.cs`）

| 字段 | 变化 | 默认值 |
|---|---|---|
| `Ip` | **删除** —— 桥接固定在 `IPAddress.Loopback`（本机），不再有远端 IP 概念 | — |
| `Port` | 语义改「**桥接端口**」 | `8899` → **`18899`** |
| `SerialPortName` | 保留，语义变「机械臂串口号」（由 `connect` 下发给桥接进程） | `COM3`（不变） |
| `CommandTimeoutMs` | `300` → **`2000`**：现在一次往返＝IPC + 官方 DLL 内部调用，300ms 太紧 | 2000 |
| `ConnectTimeoutMs` | **新增**：连串口/搜设备可能要几秒 | 10000 |
| `AutoStartBridge` | **新增**：是否由 VisionSort 自动拉起桥接进程（关掉＝现场手动启动，调试用） | true |
| `PickDelayMs` / `ResetDelayMs` / `MotionPollMs` / `ArriveToleranceMm` / `ArriveTimeoutMs` | **不变** | 500 / 800 / 80 / 0.5 / 5000 |
| `JogVelocity` | **删除** —— v2.1 遗留的死字段（只声明，从没被读过）；JOG 速度改由桥接进程用 72 号参数的比例控制 | — |
| `ConfigureMotionParams` | 保留在类库（`DobotArmSettings`），默认关（不覆盖现场参数）；**界面与 sysparam.json 都还没有开关**，见 §12.4 | false |
| `BridgeExePath`（**新增**，类库 `DobotArmSettings`） | 桥接进程 exe 路径（相对 `AppDomain.BaseDirectory`）；`BridgeProcess.ResolvePath()` 读它 | `ArmBridge\DobotArmBridge.exe` |
| `BridgeStartTimeoutMs`（**新增**，类库） | 拉起桥接进程后的探活超时 | 5000 |
| `Retries`（**新增**，类库） | 幂等命令无应答时的额外重发次数（动作命令不重发） | 3 |

> 向后兼容：`sysparam.json` 里历史存过的 `robot.ip` 读到时**忽略不报错**，保存时不再写出。

> **（2026-09-20 补充）本表只覆盖"机械臂通信"这一类字段。** `RobotArmSettings` 里另有后来加入的两类，见别的文档：
> ① `GrabTimeoutMs`（生产取像单次等待，连续模式下的等待粒度）；
> ② **抓取几何** `StandbyX/StandbyY`、`PickZ`、`PlaceX/Y/Z`、`PickAreaMinX/MaxX/MinY/MaxY`
> —— 它们在九点标定期加入，与标定一起存 `config\calib.json`，见「九点标定联机分拣方案.md」。
> 其中 `PickX/PickY` 已于 2026-09-20 更名为 **`StandbyX/StandbyY`**：它本来就是"放完料回这里等下一件"的待机位，
> **不是取料点**（取料点由视觉像素经九点标定换算而来），旧名字容易看错。

### 5.3 界面改动（`grpRobotCfg`，控件名沿用现有）

现在这组是：`txtRbIp`(机械臂IP) / `txtRbPort`(端口) / `txtPickDelay` / `txtHomeDelay`。

| 控件 | 变化 |
|---|---|
| `txtRbIp` → 改名 `txtRbCom` | 标签「机械臂 IP」→「**串口号**」；校验从 IP 格式改为 `^COM\d+$`（不区分大小写）**或空/`auto`**（＝让官方 DLL 自动搜索），非法则提示并回退 `COM3` |
| `txtRbPort` | 标签「端口」→「**桥接端口**」，默认 `8899` → `18899`；校验 `1–65535` 不变 |
| `txtPickDelay` / `txtHomeDelay` | **不动** |
| `chkAutoBridge` | **新增** CheckBox「自动启动桥接进程」，绑 `AutoStartBridge` |
| 可选 | `btnScanCom`「扫描串口」→ 调 `SearchPortsAsync()` 下拉选（若你不要就删掉，默认**不做**） |

`DeviceDebugView.Robot.cs`（19 个控件、软件队列、位姿轮询、点动三事件）：**v3.0 当时一行不改**（只换协议层，界面零改动）。

> ⚠ **（2026-09-20 校正）这句话现在不再成立。** 该文件后来在**九点标定**与**E+D 互锁**两期被改过两处：
> `_arm` 从 `private readonly DobotArmClient _arm = new DobotArmClient();` 改为**属性代理** `RobotArmService.Shared.Client`
> —— 否则调试页与主运行页**各持一个客户端**，而 v3.0 只有一个桥接进程、一条回环 UDP，命令会互相插队，
> 现场表现正是"机械臂偶尔乱走 / 报超时"；另加了 `StopJogIfActive()`（互锁生效时补一次 JOG 停止）。
> 详见「主运行页方案.md」§17.4 E 与 `DeviceDebugView.Lock.cs`。

### 5.4 桥接进程的部署与生命周期

| 事项 | 做法 |
|---|---|
| 部署布局 | `VisionSort.exe` 同目录下 `ArmBridge\DobotArmBridge.exe` + `ArmBridge\DobotDll.dll` + 5 个原生依赖 |
| 构建期拷贝 | `VisionSort.csproj` 加桥接进程的 `ProjectReference`（`<ReferenceOutputAssembly>false</ReferenceOutputAssembly>`，**只借构建顺序，不引用程序集**）+ 一个 **`CopyArmBridge` 目标**（`AfterTargets="Build"`，`Copy` 到 `$(OutDir)ArmBridge\`）。**必须用目标而不是 ItemGroup**：ItemGroup 的 glob 在项目评估时展开，那时桥接进程可能还没构建过 → glob 为空 → 静默漏拷（见 §12.3 第 2 条） |
| 连接时序 | `ConnectAsync()`：`ping` 通 → 直接 `connect`；不通且 `AutoStartBridge` → `Process.Start` → 轮询 `ping`（最多 `ConnectTimeoutMs`）→ `connect` |
| 断开 | `DisconnectAsync()` 只发 `disconnect`（**保留桥接进程**，下次连接更快，也不反复占用/释放串口） |
| 退出 | `DobotArmClient.Dispose()`：**仅当桥接进程是我们拉起的**，发 `shutdown` 并等退出（最多 2s，超时则不强杀） |
| 僵尸桥接 | VisionPro 崩溃后桥接进程可能常驻并占着串口 —— 这是设计允许的（它就是可独立运行的服务）；下次运行 `ping` 得到 `connected=1` 会直接复用，不会冲突 |

---

## 六、自测与现场验证

### 6.1 S1 桥接进程手工验证（**本机无机械臂也能验，且是最大风险的本机覆盖**）

不启动 VisionPro，直接跑 `DobotArmBridge.exe`，用 PowerShell 手工发 UDP 包：

```powershell
$c = New-Object System.Net.Sockets.UdpClient
$c.Connect('127.0.0.1', 18899)
$c.Send([Text.Encoding]::ASCII.GetBytes('cmd=ping;seq=1'), 14)
```
（应答 `ok=1;connected=0;bridge=1.0.0;seq=1`）

| 步 | 发送 | 期望 | 这一步真正证明了什么 |
|---|---|---|---|
| S1-1 | `cmd=ping` | `ok=1;connected=0` | 进程起来了、回环 UDP 通 |
| S1-2 | `cmd=search` | `ports=COM3,…` | `System.IO.Ports` 可用 |
| S1-3 | `cmd=connect;port=auto` | **`ok=0;err=NOT_FOUND`**（无真机时；**实测已复现**） | **`DobotDll.dll` 被成功加载、x86 位数正确、Qt5/VC++ 依赖齐全** —— 若这里报 `DLL_MISSING`/`ARCH`，位数或依赖有问题，必须先解决 |
| S1-3b | `cmd=connect;port=COM5`（显式给不存在的口） | `ok=0;err=OCCUPIED` + 本机串口列表（**实测已复现**） | 显式串口号 vs 自动搜索的返回码语义不同（§3.4） |
| S1-4 | `cmd=ping` | `connected=0` | 失败不留脏状态 |
| S1-5 | 插真机后重做 S1-3，并紧跟 `cmd=pose` | `ok=1;fw=…;ver=…`，且 pose 返回合理数值 | 串口通路 + 固件版本；**必须 pose 成功才算真连上**（§3.4 的"假成功"） |
| S1-6 | `pose` → `clearAlarms` → `home` | 坐标合理 / 报警清零 / **真回零** | 读命令、清报警、运动命令与队列启动时序全对 |
| S1-7 | `jog;joint=1;cmd=1` → `jog;joint=1;cmd=0` | 真点动、松手必停 | JOG 映射；不动的开关 `ConfigureMotionParams` |
| S1-8 | `moveJ;x=…;y=…;z=…;r=0` → `suction;on=1` → `suction;on=0` | 真到位、真吸真放 | 运动/吸盘通路，调 `PickDelayMs`/`ResetDelayMs` |

### 6.2 S2–S4 无硬件回归（**假桥接器**，临时探针，测完删除）

| # | 场景 | 期望 |
|---|---|---|
| S2-1 | 假桥接器（同样 KV/UDP 协议，回固定位姿与"逐渐逼近目标"）↔ **真实 `DeviceDebugView`** | 连接 / 位姿刷新 / 回零 / 清报警 / 定点（**KV 字段逐个断言**）/ 执行器『气爪夹』提示未实现且复位 / 吸盘吸 / 吸盘放 / 添加队列计数 / 启动队列按序 / 跑完 N/N / 清空归零 / 断开 —— 全通（即 v2.1 的 L6 换成新协议重跑一遍） |
| S2-2 | 系统设置页 | `COM3` / `18899` / 500 / 800 回显与落盘；非法串口号、端口 0/70000 各提示 1 次并回退 |
| S3-1 | 桥接进程**未启动**且 `AutoStartBridge=false` | 提示"桥接进程未运行"，不卡死、不崩 |
| S3-2 | 假桥接器故意不回包 | `CommandTimeoutMs` 后报错；**运动类命令不重发**（只发 1 次）；幂等命令重发 3 次 |
| S3-3 | 迟到应答（先回旧 `seq`） | 被丢弃，不影响当前命令 |
| S3-4 | 跑到一半杀掉桥接进程 | `ConnectionLost` 触发、停轮询、提示"通讯中断"，**不刷屏** |
| S4-1 | 三个项目构建 | `DobotArmBridge`(**x86**)、`DobotArm`(AnyCPU)、`VisionSort`(**x64**) 全部 0 error / 0 warning |
| S4-2 | 位数与依赖核对 | PE 头核对 `DobotArmBridge.exe` = **0x014C(x86)**；`bin\...\ArmBridge\` 下 exe + 6 个原生 DLL 齐全 |

### 6.3 现场（T-live，本机无机械臂必须现场做）

1. **USB 先插上**，设备管理器确认 COM 号（官方 Magician 固定 **115200-8-N-1**）；先关闭 DobotStudio/官方 demo（否则串口被占用）；
2. 按 S1-1 → S1-8 顺序；**S1-3 是关卡**：它不过，后面全部不用试；
3. 点动 6 方向：方向不对改 `JogAsync` 的轴码映射；**点动不动**就把 `ConfigureMotionParams` 打开（下发运动参数）再试；
4. 定点 + 吸盘：把 `PickDelayMs`/`ResetDelayMs` 调到能吸稳、放净；
5. 报警 → 消除报警：看灯红→绿；
6. 软件队列：添加 3 点 → 启动 → 逐点到；**停止**只让当前动作跑完即停（不留残余动作）。

---

## 七、风险

| 风险 | 影响 | 规避 |
|---|---|---|
| 桥接进程被误建成 x64 | 一调 DLL 就 `BadImageFormatException` | csproj 里 `<PlatformTarget>x86</PlatformTarget>` 钉死 + S4-2 按 PE 头核对 |
| `deps` 里的原生 DLL 没拷全 / Qt5 缺 | `DllNotFoundException` 或静默失败 | `deps` 入库为 Content 自动拷贝；S1-3 专门验"能加载" |
| VC++2013 运行库 | 目标机没装 → 起不来 | `msvcp120/msvcr120` app-local 随包携带（不依赖系统运行库） |
| 官方 DLL 是有状态原生库 | 并发调用崩溃/错乱 | 桥接进程**单线程串行**处理所有请求 |
| 机械臂队列语义 | 忘记 `Clear+StartExec` → 运动只入队不动；乱 `Clear` → 重放/丢动作 | 照官方 demo：连接时 `Clear+StartExec` 各一次；此后只入队不 Clear |
| 串口被占用 | `connect` 报 `Occupied` | 映射成明确中文提示（关 DobotStudio/demo）；`connect` 幂等 |
| 官方 DLL 崩溃带走桥接进程 | 机械臂失控风险 | 进程隔离：VisionPro 不受影响；`ConnectionLost` → 停轮询并提示；**不做自动重连**（行为可预期） |
| 到判位定用 `GetPose` 容差 | 现场可能太慢/太急 | `MotionPollMs`/`ArriveToleranceMm`/`ArriveTimeoutMs` 可调；若仍不可靠，再用 `GetQueuedCmdCurrentIndex`（**本次不实现**，避免死代码） |
| 新协议换掉已验证的帧层 | 回归风险 | 假桥接器把 v2.1 的 L6 全部重跑一遍（S2-1）；帧层知识留附录 B |
| 机械臂只能 USB 常连 | 布线/拖链/断线 | 你已确认；断线由 `ConnectionLost` + 手动重连处理 |

---

## 八、落盘顺序（分三步，每步独立验收）

1. ✅ **桥接进程**（`DobotArmBridge` + `deps` + P/Invoke + KV 服务）→ 手工 UDP 自测 **S1-1~S1-4 已通过**（见 §10.2 V1–V11；本机可完成，验的是"原生 DLL 能不能在本机正确加载"）→ 有真机则做 S1-5~S1-8；
2. ✅ **`DobotArm` 改造**（共享线协议 + `UdpBridgeTransport` + `BridgeProcess` + 客户端内部改写）+ **删除 5 个已无用途的文件** → 假桥接器自测 S2/S3 **30 项全过**（见 §11.2）；
3. ✅ **VisionSort 接线**（设置字段、`grpRobotCfg` 三个控件改 + 一个 CheckBox、自动启动、csproj 拷贝规则）→ 构建与部署 B1–B4、界面探针 P1–P4 **21 项全过**（见 §12）→ 现场 T-live。

> 每步我都会先给你看改动清单再落盘；**第 1 步做完就能拿到"官方 DLL 在本机可用"这个结论**，后面两步都是可控的常规接线。

---

## 九、决策与状态

| 题 | 决定 |
|---|---|
| 机型 | Dobot Magician（4 轴） |
| 机械臂连接 | **USB 串口常连**，115200-8-N-1（官方 DLL 无网络通道） |
| 命令实现 | **官方 `DobotDll.dll`**（x86 原生），手写协议层废弃 |
| 架构 | **x86 桥接进程** `DobotArmBridge.exe` + `DobotArm` 客户端类库（甲） |
| IPC | **UDP 回环 127.0.0.1:18899 + 单行 KV** |
| Q2 点动 | 机械臂自身 JOG（按住持续动、松手停；单击走一步） |
| Q3 队列 | **软件端队列**（对比留档见 v2.1，结论不变） |
| Q4 状态刷新 | 定时 `GetPose` 轮询（界面 500ms + 到位判定 `MotionPollMs`） |
| Q5 执行器 | 只做吸盘；真空反馈显示 `--`；选「气爪夹」提示未实现并复位 |
| 点动模式 | **坐标模式**（`isJoint=0`）：A/B/C/D = X/Y/Z/R，与界面 X±/Y±/Z± 标签一致（v2.1 误用关节模式，X+ 实际动 Joint1） |

**小项（你已确认按推荐执行）**

- **P1 `deps` 入库**：6 个官方原生 DLL（~6.9 MB）要不要复制进 `04-WinForms\DobotArmBridge\deps\`（**推荐：要**，否则每次换机器都要去 `Dobot Demo V2.3-zh` 里找）。
- **P2 断桥行为**：`DisconnectAsync` 只断串口、**保留桥接进程**（推荐）／每次断开都杀进程。
- **P3 「停止队列」**：维持 v2.1 语义"当前动作跑完即停"（推荐）／改调 `SetQueuedCmdForceStopExec` 立即硬停（更急停，但动作突兀）。
- **P4 「扫描串口」按钮**：**不做**（推荐，串口号手填）／做（多一个按钮 + 一条 `search` 命令）。
- **P5 到位判定**：`GetPose` 容差（推荐，复用已验证逻辑）／改官方 `GetQueuedCmdCurrentIndex`（更"官方"，但要新增一条 IPC 命令）。

---

## 十、第 1 步实测记录（桥接进程 —— **已完成并验证**）

### 10.1 落盘清单

| 文件 | 动作 | 说明 |
|---|---|---|
| `04-WinForms/Shared/BridgeProtocol.cs` | 新增（277 行） | 线协议唯一定义：命令名/键名/错误码常量 + `BridgeMessage`（KV 编解码与转义）。**被客户端与桥接进程同时编译**（两边 csproj 用 `Compile Include` 链接同一份源码），不会两侧漂移 |
| `DobotArmBridge/DobotArmBridge.csproj` | 新增 | net48 控制台，`<PlatformTarget>x86</PlatformTarget>` 钉死；`deps\*.dll` 作 Content 拷到 exe 同目录；**构建期硬门槛**：`PlatformTarget≠x86` 直接编译失败 |
| `DobotArmBridge/Native/DobotApi.cs` | 新增（187 行） | 照抄官方 demo 的 P/Invoke 子集（16 个函数）+ 结构体/枚举/返回码 |
| `DobotArmBridge/BridgeCommands.cs` | 新增（433 行） | 12 条命令分发 → 官方 DLL；连接时序、幂等、错误码→中文提示 |
| `DobotArmBridge/BridgeServer.cs` | 新增（91 行） | 回环 UDP 收/回包、单线程串行、超时轮询停止标志 |
| `DobotArmBridge/Program.cs` | 新增（89 行） | 单实例 Mutex、`--port`、Ctrl+C 收尾、未处理异常兜底 |
| `DobotArmBridge/BridgeLog.cs` | 新增（60 行） | stdout（逐行 Flush）+ `DobotArmBridge.log`（超 2 MB 重开） |
| `DobotArmBridge/deps/*.dll` | 新增 | 官方 6 个原生 DLL 入库（6.85 MB），目标机不需要装 Qt 或 VC++2013 运行库 |

### 10.2 验证结果（本机无机械臂，手工 UDP 打真桥接进程）

| # | 场景 | 结果 |
|---|---|---|
| V1 | `ping` | ✅ `ok=1;connected=0;bridge=1.0.0` |
| V2 | `search` | ✅ `ports=COM1,COM2`（本机真实串口） |
| V3 | **`connect;port=auto`** | ✅ `NOT_FOUND` —— **这一条证明 `DobotDll.dll` 在 x86 进程里加载成功、Qt5/VC++ 依赖齐全**（位数或依赖有问题这里会是 `DLL_MISSING`/`ARCH`） |
| V4 | `connect;port=COM5`（不存在的口） | ✅ `OCCUPIED` + 本机串口列表（语义见 §3.4） |
| V5 | `connect;port=COM1`（存在但非机械臂） | ✅ 官方返回**成功**且耗时 > 6 s —— "假成功"，见 §3.4 的三条结论 |
| V6 | 未连接时 `pose` / 未知命令 / 缺 `cmd` 字段 | ✅ `NOT_CONNECTED` / `BAD_COMMAND` / "请求里缺少 cmd 字段"；失败后 `ping` 仍为 `connected=0`（不留脏状态） |
| V7 | 转义往返 | ✅ `port=A%3BB` → `A;B` → 回包 `%3B`；中文原样往返；`%E4%B8%AD` → `%25E4%25B8%25AD`（不静默变乱码） |
| V8 | 单实例 | ✅ 第二个实例立刻退出，退出码 3 |
| V9 | `shutdown` | ✅ 回 `ok=1` 后进程正常退出（退出码 0），无残留进程 |
| V10 | 位数核对 | ✅ exe 与 6 个原生 DLL 的 PE 头全是 `0x014C`(x86) |
| V11 | 构建 | ✅ 0 error / 0 warning |

**尚未验证（必须真机）**：`connect` 成功路径、`pose`/`alarms`/`home`/`moveJ`/`jog`/`suction` 的实际动作、`SetQueuedCmdClear+StartExec` 之后运动是否真的执行、运动参数下发效果 —— 即 §6.1 的 S1-5~S1-8。

### 10.3 自测中发现并改掉的问题

1. **`BridgeLog.Path` 属性把 `System.IO.Path` 类型遮蔽**（CS1061）→ 该属性本来就没人用，按"死代码删掉"处理；
2. **`jog` 方向码的键不能叫 `cmd`**：`cmd` 已经是"命令名"字段，`cmd=jog;...;cmd=1` 会把命令名覆盖成 `"1"` → 改用 `dir`（已写进 §3.3 命令表并留注释）；
3. **`Escape/Unescape` 从"通用百分号解码"收敛成 5 个精确替换**：原本按字节解码，手打 `%E4%B8%AD` 会静默变乱码；现在只还原我们真正产生的 5 个转义，其它 `%` 原样保留；
4. **`connect` 的官方返回码语义与预期不同**（本机实测）→ 见 §3.4：显式给串口号时一律 `Occupied`，并据此给提示补上"本机现有串口"与 `auto` 自动搜索；
5. **输出路径被平台名带偏**：`<Platforms>x86</Platforms>` 让输出落到 `bin\x86\Debug\net48\`，而 VisionSort 的 ProjectReference 会带着 `Platform=x64` 进来 → 加 `<AppendPlatformToOutputPath>false</AppendPlatformToOutputPath>` 固定为 `bin\Debug\net48\`，并加构建期 `EnsureX86` 门槛（§4）。

## 十一、第 2 步实测记录（`DobotArm` 客户端改造 —— **已完成并验证**）

文件增删见 §5.1。核心变化：客户端内部从"自组官方帧 + UDP 直连机械臂"改成"发一行 KV + 桥接进程"，
`Request(cmd, timeoutMs, allowRetry, fill)` 是唯一的收发核心，`seq` 匹配、迟到包丢弃、断线事件都在它里面。

### 11.2 验证结果（临时假桥接进程 ↔ 真 `DobotArmClient`，**30 项全过**，探针测完即删）

| # | 场景 | 结果 |
|---|---|---|
| T1 | 连接 + 各命令字段 | ✅ `connect(port=COM3;timeout=3000;motion=0)`、`moveJ` 四坐标按 F4 不变区域格式化、`suction(enable=1,on=1/0)`、回零/清报警/断开各 1 条、**`seq` 严格递增**、断开后 `IsConnected=false` |
| T1b | **点动模式** | ✅ `joint=0`（坐标模式）+ `dir=1`(启动) / `dir=0`(停止) —— 见 11.3 第 3 条 |
| T2 | 到位判定 + 相对步进 | ✅ `WaitArrive` 在仿真收敛下返回；`MoveRelative(Y,+5)` 在读到的最新位姿上加（≈35） |
| T3 | 幂等命令丢包 | ✅ 丢 2 包后 `pose` 仍成功（共发 3 次），连接状态不变 |
| T4 | 动作命令丢包 | ✅ `home` 只发 **1** 次、抛错、`ConnectionLost` 触发、`IsConnected=false` |
| T5 | `ok=0`（业务性失败） | ✅ 透传桥接进程的中文 msg + 错误码，**不重发** |
| T6 | 错 `seq` 应答 | ✅ 被丢弃（不会把假应答的 999 当成位姿），日志留痕 |
| T7 | 应答缺字段 | ✅ `pose` 少 `x` 时报"缺少字段"，不静默当 0 |
| T8 | 桥接进程静默 | ✅ 重试耗尽 → `ConnectionLost` + `IsConnected=false` |
| T9 | 桥接进程没运行 | ✅ 明确提示且 < 3 s 干脆失败 |
| T10 | `connect` 被拒 | ✅ 信息透传 + **补发 `disconnect` 对账**（避免"界面显示未连接、串口却被占着"） |
| — | 构建 | ✅ `DobotArm`（AnyCPU）+ `VisionSort`（x64）0 error / 0 warning |

### 11.3 自测中发现并改掉的问题

1. **探针自身的故障开关泄漏**（非产品问题）：假桥接器的 `DropCount` 是跨用例存活的状态，T4 只消耗 1 个，剩下的把 T5 的 ping 吞了，导致误报"桥接进程没在运行"。修法：每个用例前清零故障开关。
2. **由此暴露的真问题：`TryPing` 一包不回就断言"桥接进程没在运行"** —— 桥接进程是单线程的，可能正卡在官方 `ConnectDobot` 里（实测要 6 秒以上），此时给出"进程没在运行"会把现场排查引到错的方向。修法：探活容忍一次失败再下结论。
3. **点动的坐标/关节模式修正**：v2.1 硬编码 `isJoint=1`（关节模式），界面上的"X+"其实动的是 Joint1。官方 demo 的 `private byte isJoint = (byte)0;` 默认坐标模式，且 `SetParam()` 也不下发 71，说明坐标点动靠机械臂自身参数即可。现改为 `isJoint=0`，A/B/C/D = X/Y/Z/R，与界面标签一致（§6.1 手工 UDP 的 `jog` 示例同步为此语义）。
4. **`alarms` 命令的归属**：客户端不再暴露 `ReadAlarms()`（界面只有"消除报警"，没有读报警的需求），但桥接进程保留 `alarms` 命令——它是服务接口，现场可按 §6.1 用一条 UDP 直接看报警位，与 `search` 同理。
5. **界面里因协议更换而变错的东西一并修正**：删掉失效的 `using DobotArm.Protocol;` 与 `Settings.Transport/Ip` 两行赋值（不改就编译不过），并改写连接失败提示（原来还在教用户"检查 IP 与 Wi-Fi 端口 8899"）与类注释。

## 十二、第 3 步实测记录（VisionSort 接线 —— **已完成并验证**）

### 12.1 改了什么

| 文件 | 动作 |
|---|---|
| `VisionSort/Services/RobotArmSettings.cs` | 删 `Ip`（桥接固定回环）；`Port` 语义改**桥接端口** 8899 → **18899**；新增 `ConnectTimeoutMs=10000`、`AutoStartBridge=true`；`CommandTimeoutMs` 300 → **2000**；**删 `JogVelocity`**（v2.1 遗留死字段） |
| `VisionSort/Views/SysConfigView.Designer.cs` | `lblRbIp`/`txtRbIp` → `lblRbCom`/`txtRbCom`（默认 `COM3`）；`lblRbPort` 文本改「桥接端口」、默认 `18899`；**新增** `lblAutoBridge` + `chkAutoBridge`（`tlpRb` 第 5 行） |
| `VisionSort/Views/SysConfigView.Robot.cs` | 串口号校验（`COMx` 或空/`auto`）+ 桥接端口校验 + 勾选框即时写 Shared；`PushRobotToUi`/`PushRobotUiToShared` 同步 5 个字段 |
| `VisionSort/Views/SysConfigView.Modbus.cs` | 保存 robot 组改用 `txtRbCom`/`chkAutoBridge`；`RestoreGroup` 从"只认 TextBox"扩展到**也认 CheckBox** |
| `VisionSort/Views/DeviceDebugView.Robot.cs` | 连接前多映射 `ConnectTimeoutMs`/`AutoStartBridge`；`ShutdownRobot` 改用 **`Dispose()`** —— 关程序时顺带关掉我们自己拉起的桥接进程，别让它继续占着串口 |
| `VisionSort/VisionSort.csproj` | 桥接进程 `ProjectReference`（只借构建顺序）+ `CopyArmBridge` 目标（拷 exe + 6 个原生 DLL 到 `$(OutDir)ArmBridge\`） |
| `VisionSort.sln` | 加入 `DobotArmBridge` 项目；所有解决方案配置（含 x64）都映射到它的 `Debug\|x86`（避免被 x64 配置带错位数） |

### 12.2 验证结果

**构建与部署**

| # | 场景 | 结果 |
|---|---|---|
| B1 | 解决方案 `Debug\|x64` 构建 | ✅ 0 error / 0 warning（三个工程都编） |
| B2 | 输出目录 `ArmBridge\` | ✅ **8 个文件**：`DobotArmBridge.exe` + `.exe.config` + 6 个官方原生 DLL（6.85 MB）。2026-09-20 用 `Rebuild`（Clean+Build）复核：Debug 与 Release 都重新拷出这 8 个 —— 此前此处误记为"7 个"（exe + config + 6 DLL = 8） |
| B3 | 位数核对 | ✅ 部署进 x64 目录的桥接进程与 6 个原生 DLL **全是 x86**；`VisionSort.exe` 是 x64 |
| B4 | 真机冒烟 | ✅ `VisionSort.exe` 启动 5 秒存活（103 MB），退出后无残留进程 |

**界面探针（真 `SysConfigView` / `DeviceDebugView` + 真客户端 + 假桥接进程，21 项全过，探针测完即删）**

| # | 场景 | 结果 |
|---|---|---|
| P1 | 设置页回显 | ✅ 串口号 `COM3`、桥接端口 `18899`、抓取延时 `500`、复位延时 `800`、自动启动=勾选 |
| P2 | 校验 | ✅ 合法串口号/端口写入；`auto` 被接受；非法串口号与 `70000` 端口**提示并回退到生效值**；勾选框即时写 Shared |
| P3 | 保存/加载往返 | ✅ `sysparam.json` 写出 `txtRbCom`/`chkAutoBridge`；改乱后加载**全部恢复（含 CheckBox 状态）** |
| P4 | 连接连线 | ✅ 点『连接机械臂』→ 位姿刷到界面（当前X：123.5）→ **假桥接进程收到的 `connect` 里 `port=COM7`**（设置页的值一路传到协议层）→ 收尾后『断开』可点 → 点它发出 `disconnect` 且 `IsConnected=false` |

### 12.3 自测中发现并改掉的问题

1. **探针时序**（非产品问题）：『断开机械臂』按钮在 `_robotBusy` 收尾前是置灰的，`PerformClick()` 对不可选控件不触发 → 探针改成"等到按钮 Enabled 再点"，顺带多一条断言（连接收尾后断开按钮确实可点）。
2. **部署拷贝必须写在 `Target` 里、不能写在 `ItemGroup` 里**：ItemGroup 的 glob 在**项目评估时**展开，而那时桥接进程可能还没构建过 → glob 为空 → **静默漏拷**，直到现场运行才发现 `ArmBridge\` 不存在。改成 `AfterTargets="Build"` 的 `CopyArmBridge` 目标后，B2 验证到位。
3. **`RestoreGroup` 只认 TextBox**：新增的 `chkAutoBridge` 本来存得进 JSON、加载时却被忽略。扩展为"TextBox 与 CheckBox 都认"（P3 专门验了这条）。

### 12.4 遗留的一个小口子（**决定：2026-09-17 暂时不动**）

> **结论：保持现状（下面选项 3）** —— 开关仍只能在代码里改，覆盖范围仍是 72/81/83。
> 真机第一次联调时若真遇到"点动不动"，再回来做「界面勾选框 + 把 71 补进下发清单」这两件事（补 71 的收益/代价见下面那张表）。

`DobotArmSettings.ConfigureMotionParams`（是否下发 72/81/83 运动参数）**目前只能在代码里改**：界面没有开关，`sysparam.json` 也不存它。现场若遇到"点动不动/定点不动"，§6.3 的做法就要重新编译。三个选择：

1. 在 `grpRobotCfg` 再加一个勾选框（和「自动启动桥接进程」同款，最直观）；
2. 只把它写进 `sysparam.json`（不动界面，现场改文件）；
3. 维持现状（真机第一次联调时再看要不要加）。

**另外，现有 `motion=1` 的覆盖范围本身也偏窄，建议一起定**：它只发 72（JOG 倍率）+ 81（PTP 坐标绝对值）+ 83（PTP 倍率）。
而点动的真实速度 = **每轴绝对速度（70 关节 / 71 坐标）× 倍率（72）**，所以：

| 现场故障 | 现有 `motion=1` 能不能修 |
|---|---|
| 定点不动 / 太慢太快（PTP） | ✅ 能（81 是绝对速度，83 是倍率） |
| 点动不动，且机械臂里 **71 坐标每轴速度本身就是 0** | ❌ 不能（20% × 0 = 0）→ 必须把 **71** 也加进下发清单 |
| 点动方向不对 | 与参数无关，改 `JogAsync` 轴码 |

我们点动走的是**坐标模式**（`isJoint=0`，见 §9），所以对症的是 **71（JOG 坐标每轴）**，不是 70。
若要把这条路走通：把 `motion=1` 的载荷扩成 **71 + 72 + 81 + 83**，并给 71 加一组可配的每轴速度/加速度（默认值可参考官方 demo 的 200/200，但对坐标轴应按 mm/s 给，不能照抄关节的 °/s）。

### 12.5 真实链路取证（2026-09-20，本机，**真客户端 ↔ 真桥接进程**）

> §11.2 与 §12.2 的客户端验证用的是**临时假桥接进程**（协议对、但对面是我们自己写的探针）。
> 2026-09-20 补了一次**真 `VisionSort.exe` ↔ 真 `DobotArmBridge.exe`** 的取证（本机仍无机械臂），
> 日志取自部署目录里桥接进程自己写的 `ArmBridge\DobotArmBridge.log`：

```
2026-09-20 10:25:31.951  DobotArmBridge 1.0.0 启动：端口 18899，进程位数 x86
2026-09-20 10:25:32.047  监听 127.0.0.1:18899（仅回环，x86 进程）
2026-09-20 10:25:32.235  ← cmd=connect;seq=8;port=COM3;timeout=3000;motion=0
2026-09-20 10:25:32.310  connect：COM3 失败（官方返回码 2）
2026-09-20 10:25:32.312  → seq=8;ok=0;err=OCCUPIED;msg=串口 COM3 打不开：…本机现有串口：COM1,COM2…
2026-09-20 10:25:32.314  ← cmd=disconnect;seq=9
2026-09-20 10:25:32.314  disconnect：本来就没连接，忽略
2026-09-20 10:25:49.732  ← cmd=connect;seq=11;port=COM4;timeout=3000;motion=0
2026-09-20 10:25:49.734  → seq=11;ok=0;err=OCCUPIED;msg=…本机现有串口：COM1,COM2…
2026-09-20 10:25:49.735  ← cmd=disconnect;seq=12
2026-09-20 10:25:49.735  disconnect：本来就没连接，忽略
2026-09-20 10:26:09.085  ← cmd=shutdown;seq=0
2026-09-20 10:26:09.087  主循环结束（收到 shutdown）
```

| # | 这一步证明了什么 | 结果 |
|---|---|---|
| R1 | **自动拉起**：点『连接机械臂』时 `AutoStartBridge=true` 真把 x86 进程拉起来了 | ✅ 10:25:31 启动 |
| R2 | **真客户端 → 真桥接进程**的 KV 往返（不再是假桥接器） | ✅ `seq=8/9/11` 递增、字段完整 |
| R3 | 连接失败链路：`OCCUPIED` 错误码 + 中文提示 + **本机串口列表** 全部到位 | ✅ 两条 |
| R4 | **失败后客户端主动补发 `disconnect` 对账**（§11.2 T10 那条行为在真实链路上复现） | ✅ `seq=9` / `seq=12` |
| R5 | **退出托管**：关 VisionSort → `shutdown` → 进程自己退出，无残留 | ✅ 10:26:09 |
| R6 | 桥接进程自己记日志到 `ArmBridge\DobotArmBridge.log`（现场没人盯控制台也能查） | ✅ |

> 仍然没变的结论：**S1-5~S1-8（真机 `connect` 成功路径 / `pose` / `home` / `moveJ` / `jog` / `suction` 的实际动作）做不到就是做不到**，
> 上面这些取证不替代它们，只说明"VisionSort 这一侧到桥接进程这一侧"已经是真的了。

### 12.6 全量构建复核（2026-09-20）

| # | 内容 | 结果 |
|---|---|---|
| C1 | `MSBuild VisionSort.sln /t:Rebuild /p:Configuration=Debug /p:Platform=x64` | ✅ exit=0，**0 warning / 0 error** |
| C2 | 同上 `Configuration=Release` | ✅ exit=0，**0 warning / 0 error** |
| C3 | `CopyArmBridge` 目标在 Clean 之后能否重建部署 | ✅ Debug / Release 的 `ArmBridge\` 都重新拷出 8 个文件 |
| C4 | PE 位数复核 | ✅ `deps\DobotDll.dll`=0x014C、`DobotArmBridge.exe`=0x014C、`VisionSort.exe`=0x8664 |

> 注：Debug 的 `ArmBridge\` 里会多一个 `DobotArmBridge.log`（运行时产物）。`CopyArmBridge` 的 `Exclude` 里含 `**\*.log`，
> 所以它**不是被拷进去的**，而是上次运行留下的；`Clean` 不会删它（不是构建产物）。Release 目录没有它是正常的。

---

## 附录 A：官方 P/Invoke 来源与要点

- 函数声明：`Dobot Demo V2.3-zh\DobotDemoForCSharp-master\DobotClientDemo2.0\CPlusDll\DobotDll.cs`（63 个导出，我们实际只声明其中 **16 个**）
- 结构体/枚举/返回码：同目录 `DobotDllType.cs`（`Pose`/`PTPCmd`/`JogCmd`/`HOMECmd`/`JOG*Params`/`PTP*Params`/`EndTypeParams`、`DobotConnect`、`DobotCommunicate`、`PTPMode`、`JogCmdType`）
- 官方 demo 的连接时序（`MainWindow.xaml.cs`）：`ConnectDobot("",115200,…)` → `SetCmdTimeout(3000)` → `SetDeviceName` → `SetQueuedCmdClear()` → `SetQueuedCmdStartExec()` → 下发运动参数
- 官方 demo 的位姿轮询：`System.Timers.Timer` 600ms → `GetPose(ref pose)` → 读 `x/y/z/rHead/jointAngle[0..3]`
- 官方 demo 的重试习惯：`SetPTPCmd` 用 `while(true){ if(ret==0) break; }` —— 我们改成最多 3 次

---

## 附录 B：自实现官方二进制协议（v2.1，**已停用**，留档备查）

现场若要抓包对照"官方 DLL 到底发了什么"，用得上这张表：

```
0xAA 0xAA | Len(1B) | ID(1B) | Ctrl(1B) | Params(N B) | Checksum(1B)
Len = 2 + N；Ctrl.bit0=rw，Ctrl.bit1=isQueued；
Checksum = (256 - ((ID+Ctrl+ΣParams) mod 256)) mod 256；多字节小端
```

命令号：2 版本 / 10 取位姿(8×float32) / 20 读报警(16×uint8) / 21 清报警 / 31 回零 / 60 末端参数 / 62 吸盘(2×uint8) / 70,71 JOG 参数(8×float32) / 72 JOG 公共(2×float32) / 73 JOG 命令(2×uint8: isJoint,cmd 0=停1=A+…8=D-) / 81 PTP 坐标参数(4×float32) / 83 PTP 公共参数(2×float32) / 84 PTP 定点(1×uint8 mode 0=MOVJ + 4×float32) / 110 WAIT(uint32) / 240 开始队列 / 241 停 / 242 强停 / 245 清空 / 246 队列进度 / 150–157 Wi-Fi 配置。

依据：Dobot Magician Communication Protocol V1.1.5 + Magician API Description V1.2.2 + `AlexGustafsson/dobot-python`（第三方从零实现，用于命令号与载荷交叉核对）。

> v2.1 实测结论仍有效、可复用：L1 帧编解码与独立实现对拍逐字节一致；L2–L5 假机械臂 UDP 回环 14 项全过（重发只对幂等命令、动作命令不盲目重发、迟到包不影响后续）；L6 真机界面 ↔ 假机械臂 14 项全过；真机冒烟通过。**测出并修掉的两个真 bug**：① `TryParse` 总长误写 `3+Len`（正确 `Len+4`）；② 执行器下拉默认选中导致"吸盘吸"永远点不出来（已改为未选中＝动作选择器）。

---

## 附录 C：历史版本

| 版本 | 路线 | 状态 |
|---|---|---|
| v1.4 | TCP 文本协议 + `Services/RobotArmClient.cs` | 已废弃（v2.1 取代），记录见 v2.1 版文档 §10 |
| v2.1 | 官方二进制协议 + UDP 8899 直连 + `DobotArm` 独立类库 | **代码已落盘、自测通过，本次被 v3.0 取代**（协议层删除，界面与设置沿用） |
| v3.0 | 官方 `DobotDll.dll` + x86 桥接进程 + UDP 回环 IPC | **现行方案，已落盘并验证**（本机 V1–V11 / T1–T10 / B1–B4 / P1–P4 / R1–R6 / C1–C4 全过；只剩现场真机 T-live） |
