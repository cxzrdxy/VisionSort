namespace VisionSort.Services
{
    /// <summary>
    /// 机械臂参数：系统设置页「机械臂配置」的唯一真相源（界面 ↔ 主运行页 ↔ DobotArm 类库）。
    ///
    /// 连接方式：**独立项目 DobotArm** → **x86 桥接进程 DobotArmBridge.exe**（宿主官方 DobotDll.dll）
    ///   → USB 串口连机械臂。官方原生 DLL 只有 32 位版本，而 VisionPro 是 x64，
    ///   必须靠独立 x86 进程隔离（见 `机械臂TCP控制方案.md` §一）。
    ///
    /// 字段分三类：
    ///   ① 界面上能改的（串口号 / 桥接端口 / 抓取延时 / 复位延时 / 自动启动桥接进程）
    ///      ——由 SysConfigView.Robot.cs 校验；
    ///   ② 通信与到位判定的（命令超时 / 连接超时 / 轮询 / 容差 / 到位超时）——先给定值，现场按需调；
    ///   ③ 运动参数开关在类库那边（<c>DobotArmSettings.ConfigureMotionParams</c>，默认关＝不覆盖机械臂已有速度参数）。
    ///
    /// 参数只在本进程内有效；落盘由系统设置页「保存参数」写 config\sysparam.json（robot 组）。
    /// </summary>
    public sealed class RobotArmSettings
    {
        /// <summary>唯一实例。</summary>
        public static RobotArmSettings Shared { get; } = new RobotArmSettings();

        // ---- ① 界面可改 ----

        /// <summary>
        /// 机械臂串口号（官方 Magician 固定 115200-8-N-1）。
        /// 留空或填 "auto" ＝让官方 DLL 自动搜索设备（现场不知道 COM 号时用）。
        /// </summary>
        public string SerialPortName = "COM3";

        /// <summary>桥接进程的监听端口（回环 UDP 127.0.0.1，一般不用改）。</summary>
        public int Port = 18899;

        /// <summary>抓取延时(ms)：吸盘吸上后再等这么久，等真空建立/机械稳定。</summary>
        public int PickDelayMs = 500;

        /// <summary>复位延时(ms)：吸盘放开后再等这么久，等物料落稳。</summary>
        public int ResetDelayMs = 800;

        /// <summary>是否由 VisionSort 自动拉起桥接进程（关掉＝现场手动运行，便于看它的控制台与日志）。</summary>
        public bool AutoStartBridge = true;

        // ---- ② 通信与到位判定 ----

        /// <summary>单条命令收发超时(ms)。一次往返＝IPC + 官方 DLL 内部调用，比原先的 300ms 宽。</summary>
        public int CommandTimeoutMs = 2000;

        /// <summary>
        /// 连接类命令超时(ms)。**必须给足**：实测官方 DLL 连接一个不是机械臂的串口也要 6 秒以上
        /// （它在探测设备版本），给短了会误报超时。
        /// </summary>
        public int ConnectTimeoutMs = 10000;

        /// <summary>到位判定轮询间隔(ms)（独立于界面的 500ms 状态刷新）。</summary>
        public int MotionPollMs = 80;

        /// <summary>到位容差(mm)：三轴都进这个范围、且连续两次满足即认为到位。</summary>
        public double ArriveToleranceMm = 0.5;

        /// <summary>到位超时(ms)：等这么久还没到位就报错。</summary>
        public int ArriveTimeoutMs = 5000;

        // ---- ③ 抓取几何（主运行页用；界面上都在设备调试页的「九点标定」区）----
        //  取料点**不再写死**：由视觉给出的像素经九点标定换算而来（标定那期改的）。
        //  这里留下的是"换算之外"的几个固定几何量，**全部与标定一起存 config\calib.json**。

        /// <summary>
        /// 待机位 X（mm）：放完料回到这里等下一件。
        /// ⚠ 它**不是**取料点（取料点由标定换算，见 <see cref="RobotPickPlace"/>）——
        /// 名字原来叫 `PickX`，跟"取料"混在一起容易看错，2026-09-20 已改名。
        /// </summary>
        public double StandbyX = 200;

        /// <summary>待机位 Y（mm）。</summary>
        public double StandbyY = 0;

        /// <summary>
        /// 抓取高度 Z（mm）：把工件吸起来的高度；**待机位也用这个高度**。
        /// ⚠ 标定时必须就是这个高度（标定物与工件同高），否则像素比例不对。
        /// </summary>
        public double PickZ = -50;

        /// <summary>放料点 X（mm）：废料区。</summary>
        public double PlaceX = 200;

        /// <summary>放料点 Y（mm）。</summary>
        public double PlaceY = -150;

        /// <summary>放料点 Z（mm）。</summary>
        public double PlaceZ = -50;

        // ---- ④ 抓取工作范围（mm）：标定换算出来的取料点必须落在这个矩形里，否则拒绝动作 ----
        //  为什么默认这么窄（＝示教点 ±80mm）：九点标定错一行、或者配成镜像时，机械臂会往墙上走。
        //  宁可先"拒动"让人来调范围，也不要先撞一次。现场按工件实际可能出现的位置放大这四个数。
        //  这四个数与 PickZ 一起存 config\calib.json（同一批"抓取几何"数据，跟着标定一起重放）。

        /// <summary>取料允许的最小 X（mm）。</summary>
        public double PickAreaMinX = 120;

        /// <summary>取料允许的最大 X（mm）。</summary>
        public double PickAreaMaxX = 280;

        /// <summary>取料允许的最小 Y（mm）。</summary>
        public double PickAreaMinY = -80;

        /// <summary>取料允许的最大 Y（mm）。</summary>
        public double PickAreaMaxY = 80;

        /// <summary>
        /// 生产取像的单次等待(ms)：连续模式下标就是"多久没工件就再等一轮"的粒度。
        /// 给短一点让急停/停止能及时响应（Auto 硬触发下这一等就是等光电）。
        /// </summary>
        public int GrabTimeoutMs = 500;

        private RobotArmSettings()
        {
        }
    }
}
