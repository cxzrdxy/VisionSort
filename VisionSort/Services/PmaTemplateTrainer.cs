using Cognex.VisionPro;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.ImageProcessing;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.ToolGroup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VisionSort.Services
{
    /// <summary>
    /// 训练区域形状（PMA 的 TrainRegion）。
    /// </summary>
    public enum PmaTrainRegionShape
    {
        /// <summary>矩形（CogRectangleAffine）：工件占满视野时最常用。</summary>
        RectangleAffine,

        /// <summary>圆环扇形（CogCircularAnnulusSection）：回转体/圆形工件只取外圈特征。</summary>
        CircularAnnulusSection
    }

    /// <summary>
    /// 粒度预设（对应界面“粒度”下拉，直接影响 PatMax 的特征尺寸范围）。
    /// VisionPro 9.0 的 CogPMAlignPattern 没有公开的“金字塔层数”成员，
    /// 所以界面语义就是 API 的粒度限制 GrainLimitCoarse / GrainLimitFine，不做层数换算。
    /// </summary>
    public enum PmaGrainPreset
    {
        /// <summary>自动：GrainLimitAutoSelect = true，由 VisionPro 在训练时自选。</summary>
        Auto,

        /// <summary>粗：只用较大特征（6.1 / 1.5，官方示例值），最快、抗噪。</summary>
        Coarse,

        /// <summary>标准：VisionPro 默认量级（4.0 / 1.0）。</summary>
        Standard,

        /// <summary>细：允许更小特征参与（2.0 / 1.0），最稳、最慢。</summary>
        Fine
    }

    /// <summary>
    /// PMA 模板训练参数：与“视觉配置页 → PMA模板训练”分组里的控件一一对应。
    /// 采集训练图 / 训练模板两个动作都会先按界面当前值刷新本对象，再交给 PmaTemplateTrainer。
    /// 注意：这里只放“训练条件”，生产期的识别参数（缩放搜索等）不在这里。
    /// </summary>
    public sealed class PmaTrainSettings
    {
        /// <summary>训练区域形状（界面暂未放该下拉，先固定矩形；改这一个字段即可切换）。</summary>
        public PmaTrainRegionShape RegionShape = PmaTrainRegionShape.RectangleAffine;

        /// <summary>训练区域占图像的比例（1.0 = 整幅图）。默认 0.8，留一点边，避免把图像边缘/背景算进模板。</summary>
        public double RegionScale = 0.8;

        /// <summary>角度搜索下限（度），对应界面“角度范围”（±180°/±90°/±30°/±10°）。</summary>
        public double AngleLowDeg = -180.0;

        /// <summary>角度搜索上限（度）。</summary>
        public double AngleHighDeg = 180.0;

        /// <summary>匹配阈值（0~1），对应界面“匹配阈值”（可选值：下拉选中即用）。</summary>
        public double AcceptThreshold = 0.70;

        /// <summary>一幅图里预计找到几个工件：分拣线单件流固定 1。</summary>
        public int ApproximateNumberToFind = 1;

        /// <summary>粒度预设，对应界面“粒度”下拉，默认“自动”。</summary>
        public PmaGrainPreset GrainPreset = PmaGrainPreset.Auto;
    }

    /// <summary>
    /// 方案里的一把 PMA 工具（供界面下拉列表使用）：
    /// 直接持有工具实例，避免用名字反查带来的歧义（重名 / 换方案后名字失效）。
    /// </summary>
    public sealed class PmaToolRef
    {
        public PmaToolRef(CogPMAlignTool tool, string display)
        {
            Tool = tool;
            Display = display;
        }

        /// <summary>工具实例。</summary>
        public CogPMAlignTool Tool { get; }

        /// <summary>下拉里显示的文字：顶层工具是工具名；子容器里的工具是“容器名/工具名”。</summary>
        public string Display { get; }

        public override string ToString()
        {
            return Display;
        }
    }

    /// <summary>
    /// 一次训练/自检的结果，供界面刷新“验证分数”和弹窗摘要。
    /// </summary>
    public sealed class PmaTrainResult
    {
        /// <summary>PMA 工具名，例如 CogPMAlignTool1。</summary>
        public string ToolName { get; set; } = string.Empty;

        /// <summary>本次结束后模式是否处于已训练状态。</summary>
        public bool Trained { get; set; }

        /// <summary>自检时 PMA 找到的匹配个数。</summary>
        public int MatchCount { get; set; }

        /// <summary>自检时分数达到匹配阈值（Accepted）的个数。</summary>
        public int AcceptedCount { get; set; }

        /// <summary>最高匹配分数（0~1）。</summary>
        public double BestScore { get; set; }

        /// <summary>最高分结果的 X（像素）。</summary>
        public double BestX { get; set; }

        /// <summary>最高分结果的 Y（像素）。</summary>
        public double BestY { get; set; }

        /// <summary>最高分结果的旋转角（度）。</summary>
        public double BestAngle { get; set; }

        /// <summary>VisionPro 训练诊断信息（Pattern.GetInfoStrings 拼成一行），可能为空。</summary>
        public string Diagnostics { get; set; } = string.Empty;

        /// <summary>一句话摘要，可直接丢给 MessageBox 或状态栏。</summary>
        public string Summary
        {
            get
            {
                string head = Trained ? "模板训练完成。" : "模板未训练成功。";

                if (MatchCount == 0)
                {
                    return head + "\r\n自检：训练图里没有找到匹配（区域选得不对，或对比度/阈值不合适）。";
                }

                string text = string.Format(
                    "{0}\r\n自检：找到 {1} 个匹配（Accepted {2} 个），最高分 {3:F3}，位置 ({4:F1}, {5:F1})，角度 {6:F2}°\r\n模板工具：{7}",
                    head, MatchCount, AcceptedCount, BestScore, BestX, BestY, BestAngle, ToolName);

                if (!string.IsNullOrEmpty(Diagnostics))
                {
                    text += "\r\n训练诊断：" + Diagnostics;
                }
                return text;
            }
        }

        /// <summary>界面 lblTplScore 用的一行文本。</summary>
        public string ScoreText
        {
            get
            {
                if (!Trained) return "验证分数：--（模板未训练成功）";
                if (MatchCount == 0) return "验证分数：--（自检没有找到匹配）";
                return string.Format("验证分数：{0:F3}（自检，角度 {1:F2}°）", BestScore, BestAngle);
            }
        }
    }

    /// <summary>
    /// PMA（PatMax）模板训练器：把《联合编程01.md》§八 的二次开发步骤封成可复用的类。
    /// 纯逻辑、不依赖任何界面控件，视觉配置页和以后的主运行页都可以直接用。
    ///
    /// 职责边界（重要）：
    ///   • 本类只负责“训练”——把训练条件写进模式、提取特征、生成/保存 CogPMAlignPattern；
    ///   • 训练完成后会做**一次自检**（Verify：用训练图跑一遍拿分数），除此之外不做识别；
    ///     生产期的识别（找工件、判 OK/NG、坐标输出、通讯）不在本类。
    ///
    /// 典型用法：
    ///   PmaTemplateTrainer trainer = new PmaTemplateTrainer(pmaTool);
    ///   trainer.SetInputImage(image);                        // 喂图（自动转 8 位灰度；不作废旧模板）
    ///   trainer.Train(settings);                             // 训练 + 自检（区域：沿用 → 无则生成）
    ///   trainer.SavePattern(@"...\PMA模板.pat");             // 模板单独存档
    ///   trainer.Verify();                                    // 需要时也可单独再验一次
    ///
    /// 约定：所有“会改动模式”的动作（TrainImage/TrainRegion/Origin/GrainLimit）都会让上一次训练失效，
    ///       顺序必须是：先设图 → 再设区域/参数 → 最后 Train()（VisionPro 的既定行为）。
    /// </summary>
    public sealed class PmaTemplateTrainer
    {
        /// <summary>没有名字的 PMA 工具在界面上的兜底显示名（与 VisionPro QuickBuild 默认命名一致）。</summary>
        public const string DefaultToolName = "CogPMAlignTool1";

        public PmaTemplateTrainer(CogPMAlignTool tool)
        {
            Tool = tool ?? throw new ArgumentNullException(nameof(tool));
        }

        /// <summary>被训练的 PMA 工具：一定是方案里已有的那把。</summary>
        public CogPMAlignTool Tool { get; }

        /// <summary>本页设置的训练图（一定是 8 位灰度）；为 null 表示还没采集过。</summary>
        public ICogImage TrainImage { get; private set; }

        /// <summary>本页设置过的训练区域；为 null 表示还没设过。</summary>
        public ICogRegion TrainRegion { get; private set; }

        /// <summary>
        /// 当前生效的训练区域：本页设过的优先，否则取方案里模式自带的（可能为 null）。
        /// A 方案（沿用方案已有区域）就是靠这个属性实现的。
        /// </summary>
        public ICogRegion CurrentTrainRegion
        {
            get { return TrainRegion ?? (Tool.Pattern != null ? Tool.Pattern.TrainRegion : null); }
        }

        /// <summary>最近一次训练/自检结果。</summary>
        public PmaTrainResult LastResult { get; private set; }

        /// <summary>模式是否已经训练成功。</summary>
        public bool IsTrained
        {
            get { return Tool.Pattern != null && Tool.Pattern.Trained; }
        }

        /// <summary>是否已经有训练图（本页采集的，或方案里模板自带的）。</summary>
        public bool HasTrainImage
        {
            get { return TrainImage != null || (Tool.Pattern != null && Tool.Pattern.TrainImage != null); }
        }

        // ------------------------------------------------------------------
        // 工具查找（D3 / D4）
        // ------------------------------------------------------------------

        /// <summary>
        /// 在工具集合（CogToolBlock.Tools / CogToolGroup.Tools）里找 PMA 工具：
        /// 先扫本层（名字匹配优先，否则第一个 PMA），本层没有再看子容器；
        /// nestDepth 限定了往下找的层数（默认 1 = 只额外找一层子容器，D4）。
        /// </summary>
        public static CogPMAlignTool FindTool(System.Collections.IEnumerable tools, string preferredName = null, int nestDepth = 1)
        {
            if (tools == null) return null;

            CogPMAlignTool first = null;
            List<System.Collections.IEnumerable> nested = null;

            foreach (object element in tools)
            {
                ICogTool item = element as ICogTool;
                if (item == null) continue;

                CogPMAlignTool pma = item as CogPMAlignTool;
                if (pma != null)
                {
                    if (first == null) first = pma;
                    if (!string.IsNullOrEmpty(preferredName) &&
                        string.Equals(item.Name, preferredName, StringComparison.OrdinalIgnoreCase))
                    {
                        return pma;
                    }
                    continue;
                }

                if (nestDepth > 0)
                {
                    System.Collections.IEnumerable child = GetChildTools(item);
                    if (child != null)
                    {
                        if (nested == null) nested = new List<System.Collections.IEnumerable>();
                        nested.Add(child);
                    }
                }
            }

            if (nested != null)
            {
                foreach (System.Collections.IEnumerable child in nested)
                {
                    CogPMAlignTool found = FindTool(child, preferredName, nestDepth - 1);
                    if (found != null) return found;
                }
            }

            return first;
        }

        /// <summary>
        /// 列出方案里所有 PMA 工具（供界面下拉，D3）：
        /// 顶层工具显示为“工具名”，子容器（一层，D4）里的显示为“容器名/工具名”，重名自动加序号。
        /// </summary>
        public static List<PmaToolRef> ListPmaTools(System.Collections.IEnumerable tools, int nestDepth = 1)
        {
            List<PmaToolRef> result = new List<PmaToolRef>();
            CollectPmaTools(tools, null, nestDepth, result);
            return result;
        }

        private static void CollectPmaTools(System.Collections.IEnumerable tools, string prefix, int nestDepth, List<PmaToolRef> result)
        {
            if (tools == null) return;

            List<KeyValuePair<ICogTool, System.Collections.IEnumerable>> nested = null;

            foreach (object element in tools)
            {
                ICogTool item = element as ICogTool;
                if (item == null) continue;

                CogPMAlignTool pma = item as CogPMAlignTool;
                if (pma != null)
                {
                    string name = string.IsNullOrEmpty(item.Name) ? DefaultToolName : item.Name;
                    string display = string.IsNullOrEmpty(prefix) ? name : prefix + "/" + name;
                    result.Add(new PmaToolRef(pma, MakeUniqueName(display, result)));
                    continue;
                }

                if (nestDepth > 0)
                {
                    System.Collections.IEnumerable child = GetChildTools(item);
                    if (child != null)
                    {
                        if (nested == null) nested = new List<KeyValuePair<ICogTool, System.Collections.IEnumerable>>();
                        nested.Add(new KeyValuePair<ICogTool, System.Collections.IEnumerable>(item, child));
                    }
                }
            }

            if (nested == null) return;

            foreach (KeyValuePair<ICogTool, System.Collections.IEnumerable> pair in nested)
            {
                string containerName = string.IsNullOrEmpty(pair.Key.Name) ? pair.Key.GetType().Name : pair.Key.Name;
                string childPrefix = string.IsNullOrEmpty(prefix) ? containerName : prefix + "/" + containerName;
                CollectPmaTools(pair.Value, childPrefix, nestDepth - 1, result);
            }
        }

        /// <summary>取出子容器的工具集合（CogToolGroup / CogToolBlock 都暴露 Tools）。</summary>
        private static System.Collections.IEnumerable GetChildTools(ICogTool tool)
        {
            CogToolGroup group = tool as CogToolGroup;
            if (group != null) return group.Tools;

            CogToolBlock block = tool as CogToolBlock;
            if (block != null) return block.Tools;

            return null;
        }

        private static string MakeUniqueName(string display, List<PmaToolRef> existing)
        {
            string name = display;
            int suffix = 2;
            while (ContainsDisplay(existing, name))
            {
                name = display + " (" + suffix + ")";
                suffix++;
            }
            return name;
        }

        private static bool ContainsDisplay(List<PmaToolRef> existing, string display)
        {
            for (int i = 0; i < existing.Count; i++)
            {
                if (string.Equals(existing[i].Display, display, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------
        // 训练图 / 区域 / 参数
        // ------------------------------------------------------------------

        /// <summary>
        /// 只把图喂给工具（供预览 / 可选的自检使用），**不动 `Pattern.TrainImage`**。
        /// 所以“采集一张新图”不会让已有模板失效——要换训练图得点『训练模板』（Train 里才写 TrainImage）。
        /// 非 8 位灰度（彩色/16 位）会先转灰度，对照 visionPro09 笔记里“PMA 前面挂 CogImageConvertTool1”。
        /// </summary>
        /// <returns>真正喂给工具的灰度图。</returns>
        public ICogImage SetInputImage(ICogImage image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            ICogImage grey = EnsureGreyImage(image);
            Tool.InputImage = grey;   // 只喂图：不作废旧模板
            TrainImage = grey;        // 记为“当前待训练图”，Train() 时才写进 Pattern
            LastResult = null;
            return grey;
        }

        /// <summary>
        /// 直接把图设为训练图（**会作废旧模板**，VisionPro 既定行为）。
        /// 只有在确实要立刻重训时才用它；界面的『采集训练图』走 SetInputImage。
        /// </summary>
        /// <returns>真正写进模式的灰度图。</returns>
        public ICogImage SetTrainImage(ICogImage image)
        {
            ICogImage grey = SetInputImage(image);
            Tool.Pattern.TrainImage = grey;
            return grey;
        }

        /// <summary>把任意图像转成 PMA 能训练的 8 位灰度图（本来就是 CogImage8Grey 则原样返回）。</summary>
        public static ICogImage EnsureGreyImage(ICogImage image)
        {
            if (image == null) return null;
            if (image is CogImage8Grey) return image;

            CogImageConvertTool convert = new CogImageConvertTool();
            convert.InputImage = image;
            convert.RunParams.RunMode = CogImageConvertRunModeConstants.Intensity; // 24 位彩色 → 8 位灰度
            convert.Run();
            return convert.OutputImage;
        }

        /// <summary>
        /// 按参数生成默认训练区域（居中）——只在“方案里没有区域”时才调用：
        ///   • 矩形：FitToImage(image, scale, scale)，占图像 scale 比例；
        ///   • 圆环：同样居中，再把角度补满 360°（FitToImage 之后只覆盖 270°）。
        /// 两种区域都打开 GraphicDOFEnable=All / Interactive，挂到 CogRecordDisplay 上即可拖拽微调。
        /// </summary>
        public ICogRegion BuildTrainRegion(PmaTrainSettings settings)
        {
            if (TrainImage == null && (Tool.Pattern == null || Tool.Pattern.TrainImage == null))
            {
                throw new InvalidOperationException("还没有训练图，不能生成训练区域。");
            }

            ICogImage baseImage = TrainImage ?? Tool.Pattern.TrainImage;
            if (settings == null) settings = new PmaTrainSettings();

            double scale = settings.RegionScale;
            if (scale <= 0.0 || scale > 1.0) scale = 0.8;   // FitToImage 只接受 (0, 1]

            if (settings.RegionShape == PmaTrainRegionShape.CircularAnnulusSection)
            {
                CogCircularAnnulusSection ring = new CogCircularAnnulusSection();
                ring.FitToImage(baseImage, scale, scale);
                ring.AngleStart = 0.0;      // FitToImage 之后是 270°，模板要整圈
                ring.AngleSpan = 360.0;
                ring.GraphicDOFEnable = CogCircularAnnulusSectionDOFConstants.All;
                ring.Interactive = true;
                return ring;
            }

            CogRectangleAffine rect = new CogRectangleAffine();
            rect.FitToImage(baseImage, scale, scale);
            rect.GraphicDOFEnable = CogRectangleAffineDOFConstants.All;
            rect.Interactive = true;
            return rect;
        }

        /// <summary>
        /// 把训练区域写进模式。resetOrigin=true 时同时把模式原点挪到区域中心
        /// （《联合编程01》§八.3：原点决定结果坐标的原点，取区域中心最直观）。
        /// </summary>
        public void ApplyTrainRegion(ICogRegion region, bool resetOrigin)
        {
            Tool.Pattern.TrainRegion = region;
            Tool.Pattern.TrainRegionMode = CogRegionModeConstants.PixelAlignedBoundingBox; // 用区域外接矩形裁训练图
            TrainRegion = region;
            if (resetOrigin) ApplyOriginFromRegion(region);
        }

        /// <summary>把模式原点设成训练区域的中心（矩形/圆环两种区域）。</summary>
        public void ApplyOriginFromRegion(ICogRegion region)
        {
            double centerX;
            double centerY;

            CogRectangleAffine rect = region as CogRectangleAffine;
            CogCircularAnnulusSection ring = region as CogCircularAnnulusSection;
            if (rect != null)
            {
                centerX = rect.CenterX;
                centerY = rect.CenterY;
            }
            else if (ring != null)
            {
                centerX = ring.CenterX;
                centerY = ring.CenterY;
            }
            else
            {
                return;   // 其它形状（多边形等）不自动设原点，保留方案里已有的原点
            }

            CogTransform2DLinear origin = new CogTransform2DLinear();
            origin.TranslationX = centerX;
            origin.TranslationY = centerY;
            Tool.Pattern.Origin = origin;
        }

        /// <summary>
        /// 把界面参数写进 PMA 运行参数：匹配阈值、角度范围、预计个数、粒度。
        /// ZoneScale（缩放搜索）故意不动：本工序相机固定、工件同规格，缩放搜索只会拖慢节拍；
        /// 将来要兼容多规格工件，在这里补 ZoneScale 即可。
        /// </summary>
        public void ApplyRunParams(PmaTrainSettings settings)
        {
            if (settings == null) settings = new PmaTrainSettings();

            CogPMAlignRunParams runParams = Tool.RunParams;
            runParams.RunAlgorithm = CogPMAlignRunAlgorithmConstants.PatMax;   // 训练时同时练 PatMax/PatQuick，运行取精度最高的 PatMax
            runParams.AcceptThreshold = settings.AcceptThreshold;
            runParams.ApproximateNumberToFind = settings.ApproximateNumberToFind;
            runParams.ZoneAngle.Configuration = CogPMAlignZoneConstants.LowHigh;
            runParams.ZoneAngle.Low = settings.AngleLowDeg;
            runParams.ZoneAngle.High = settings.AngleHighDeg;

            ApplyGrainSelection(Tool.Pattern, settings.GrainPreset);
        }

        /// <summary>
        /// 训练模板并立即自检（A 方案：保留复检）：
        ///   1) 写运行参数（阈值/角度/个数/粒度）；
        ///   2) 区域：方案里已有 → 沿用（A）；没有 → 生成居中默认区域（A2）；
        ///   3) Pattern.Train()（已训练会先取消再重训）；
        ///   4) 用训练图 Run() 一次，拿最高分当“验证分数”（自检，不是生产识别）。
        /// 训练失败（区域里没有特征等）会抛 VisionPro 异常，由调用方提示。
        /// </summary>
        public PmaTrainResult Train(PmaTrainSettings settings)
        {
            if (Tool.Pattern == null) throw new InvalidOperationException("PMA 工具没有模式对象。");
            if (!HasTrainImage) throw new InvalidOperationException("还没有训练图，请先『采集训练图』。");

            if (TrainImage != null) Tool.Pattern.TrainImage = TrainImage;   // 到这一步才写训练图（会作废旧模板）

            ApplyRunParams(settings);

            ICogRegion region = CurrentTrainRegion;                 // A1：方案里已有的区域
            bool generated = region == null;                        // A2：没有才生成
            if (generated) region = BuildTrainRegion(settings);
            ApplyTrainRegion(region, resetOrigin: generated);       // 只有自动生成时才重设原点

            Tool.Pattern.TrainAlgorithm = CogPMAlignTrainAlgorithmConstants.PatMaxAndPatQuick;
            Tool.Pattern.TrainMode = CogPMAlignTrainModeConstants.Image;    // 用整幅训练图训练
            Tool.Pattern.Train();

            LastResult = Verify();   // 训练完立刻自检，给出量化分数
            return LastResult;
        }

        /// <summary>
        /// 训练后的自检：用训练图跑一次 PMA 并汇总结果，给出量化分数。
        /// 由 Train() 自动调用一次（操作员需要立刻知道模板能不能用）；
        /// 生产期的识别（判定、坐标、通讯）不在这里。
        /// </summary>
        public PmaTrainResult Verify()
        {
            PmaTrainResult result = new PmaTrainResult();
            result.ToolName = Tool.Name ?? string.Empty;
            result.Trained = IsTrained;

            // 自检前把输入图钉死成训练图：
            // 方案里的 PMA 常常被 ToolBlock 的数据绑定喂图（Image Source → Convert → PMA），
            // 一旦块内跑过一次，Tool.InputImage 就会被绑定值覆盖，自检就会“找不到匹配”。
            ICogImage checkImage = TrainImage ?? (Tool.Pattern != null ? Tool.Pattern.TrainImage : null);
            if (checkImage != null) Tool.InputImage = checkImage;

            Tool.Run();

            CogPMAlignResults results = Tool.Results;
            if (results != null)
            {
                result.MatchCount = results.Count;
                for (int i = 0; i < results.Count; i++)
                {
                    CogPMAlignResult item = results[i];
                    if (item == null) continue;

                    if (i == 0 || item.Score > result.BestScore)
                    {
                        var pose = item.GetPose();   // 用 var：GetPose 返回类型在各版本里名字不一致
                        result.BestScore = item.Score;
                        result.BestX = pose.TranslationX;
                        result.BestY = pose.TranslationY;
                        result.BestAngle = pose.Rotation;
                    }
                    if (item.Accepted) result.AcceptedCount++;
                }
            }

            result.Diagnostics = ReadDiagnostics(Tool.Pattern);
            LastResult = result;
            return result;
        }

        /// <summary>
        /// 保存训练好的模式（.pat）：
        /// 存进去的是 CogPMAlignPattern 对象本身，读回来直接赋给 Tool.Pattern 就能用
        /// （CogSerializer.LoadObjectFromFile(路径) as CogPMAlignPattern）。
        /// 想让生产方案用上这个模板，还要把 VPP 一起写回（视觉配置页的『保存方案』按钮）。
        /// </summary>
        public void SavePattern(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (!IsTrained) throw new InvalidOperationException("模板还没有训练成功，不能保存。");

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            CogSerializer.SaveObjectToFile(Tool.Pattern, path);
        }

        /// <summary>
        /// 从图片文件读一张 VisionPro 能处理的图（bmp/jpg/png/tif/idb…）。
        /// 对照 visionPro09 笔记：普通图片必须过 CogImageFileTool 才能给方案用。
        /// </summary>
        public static ICogImage LoadImageFromFile(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            CogImageFileTool fileTool = new CogImageFileTool();
            fileTool.Operator.Open(path, CogImageFileModeConstants.Read);
            fileTool.Run();

            ICogImage image = fileTool.OutputImage;   // 先把图的引用取出来，再关文件
            try { fileTool.Operator.Close(); }
            catch (Exception) { /* 关不掉不影响已经取到的图 */ }
            return image;
        }

        /// <summary>
        /// 粒度预设 → 模式上的粒度限制。
        /// 自动：GrainLimitAutoSelect = true，交给 VisionPro；
        /// 粗/标准/细：关掉自动，按预设写 Coarse/Fine。
        /// </summary>
        private static void ApplyGrainSelection(CogPMAlignPattern pattern, PmaGrainPreset preset)
        {
            if (pattern == null) return;

            if (preset == PmaGrainPreset.Auto)
            {
                pattern.GrainLimitAutoSelect = true;
                return;
            }

            double coarse;
            double fine;
            ResolveGrainLimits(preset, out coarse, out fine);

            pattern.GrainLimitAutoSelect = false;
            pattern.GrainLimitCoarse = coarse;   // 必须先写粗粒度：VisionPro 会自动把小于粗粒度的细粒度抬上去
            pattern.GrainLimitFine = fine;
        }

        /// <summary>
        /// 粒度预设对应的 (粗粒度, 细粒度)：数值取自官方默认值与官方示例，不是估算。
        ///   粗   = 6.1 / 1.5（VisionPro 官方示例用的这一组）
        ///   标准 = 4.0 / 1.0（CogPMAlignPattern 的 XML 默认值）
        ///   细   = 2.0 / 1.0（允许更小特征参与）
        /// </summary>
        private static void ResolveGrainLimits(PmaGrainPreset preset, out double coarse, out double fine)
        {
            switch (preset)
            {
                case PmaGrainPreset.Coarse:
                    coarse = 6.1; fine = 1.5; break;
                case PmaGrainPreset.Fine:
                    coarse = 2.0; fine = 1.0; break;
                default:
                    coarse = 4.0; fine = 1.0; break;
            }
        }

        /// <summary>把 VisionPro 的训练诊断信息（GetInfoStrings）拼成一行文字，取不到就返回空串。</summary>
        private static string ReadDiagnostics(CogPMAlignPattern pattern)
        {
            if (pattern == null) return string.Empty;

            try
            {
                CogStringCollection messages = pattern.GetInfoStrings();
                if (messages == null || messages.Count == 0) return string.Empty;

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < messages.Count; i++)
                {
                    if (builder.Length > 0) builder.Append("；");
                    builder.Append(messages[i]);
                }
                return builder.ToString();
            }
            catch (Exception)
            {
                return string.Empty;   // 诊断信息只是锦上添花，取不到不影响训练结果
            }
        }
    }
}
