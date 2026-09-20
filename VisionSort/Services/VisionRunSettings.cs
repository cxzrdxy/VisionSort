namespace VisionSort.Services
{
    /// <summary>
    /// 主运行期的视觉参数（系统设置页「视觉参数配置」的唯一真相源）。
    /// 与 <see cref="ConveyorSettings.Shared"/> 同风格：静态单例，主运行页只读它、不直接读控件。
    ///
    /// 与「视觉配置页 → PMA 匹配阈值」的分工（Q2=A：职责分离，互不写对方）：
    ///   本类 MatchThreshold  = 生产判定阈值（**我们自己拿它比分数**，见 MatchThreshold 的注释）；
    ///   视觉配置页 cmbThreshold = 训练/自检阈值，只影响训练与自检打分。
    ///   两边各自独立，改一个不会动另一个。
    ///
    /// 参数只在本进程内有效；落盘由系统设置页的「保存参数」写 config\sysparam.json（vision 组）。
    /// </summary>
    public sealed class VisionRunSettings
    {
        /// <summary>唯一实例。</summary>
        public static VisionRunSettings Shared { get; } = new VisionRunSettings();

        /// <summary>
        /// 生产判定阈值，范围 (0, 1]。
        /// ⚠ 它**只用来做判定**（`Inspect` 里自己比 `分数 ≥ 阈值`），
        /// **不再**写进 PMA 的 `RunParams.AcceptThreshold` —— 见下面那条注释。
        /// </summary>
        public double MatchThreshold = 0.70;

        /// <summary>
        /// 「视野里到底有没有工件」的下限（判 NG 之后，值不值得让机械臂去抓）。
        ///
        /// 为什么需要它（2026-09-20 全流程实测挖出来的）：
        /// `CogPMAlignTool.RunParams.AcceptThreshold` **会把结果从 `Results` 集合里滤掉** ——
        /// 实测：同一张分数 0.9401 的图，阈值设 0.90 时 `Results.Count` 直接变成 **0**
        /// （不是"结果在、Accepted=false"，是**根本没结果**）。
        /// 后果很严重：判 NG 时拿不到像素坐标 → 机械臂不知道该去哪儿抓 → **分拣永远不动作**。
        /// 所以现在固定用 `AcceptThreshold = 0` 跑，判定和"要不要抓"都由我们自己算。
        ///
        /// 默认 0.30 是**量出来的**（同一套方案，`AcceptThreshold=0`）：
        ///   有齿轮（整幅）                       分数 **0.9401**
        ///   只有背景的 5 块 300×300 裁剪          分数 **0.0000**
        ///   全黑 / 全白 / 全灰 1280×1024          分数 **0.0000**
        /// 分得非常开，0.30 这个门槛足够安全。它的作用只是"别对着空带子空抓一次"。
        /// （界面暂时没给这个框；要现场调，先在这里改默认值，或告诉我加个输入框。）
        /// </summary>
        public double PickScoreFloor = 0.30;

        /// <summary>光电触发（相机硬触发出新帧）后到发"停带"指令的等待，毫秒（0–60000）。</summary>
        public int StopDelayMs = 300;

        /// <summary>停带后再等多久开始检测（等停稳/消抖），毫秒（0–60000）。</summary>
        public int PhotoWaitMs = 100;

        /// <summary>开始检测到出结果的看门狗，毫秒（0–60000）；超时判异常并停线。</summary>
        public int DetectTimeoutMs = 2000;

        private VisionRunSettings()
        {
        }
    }
}
