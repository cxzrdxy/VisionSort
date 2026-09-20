using Cognex.VisionPro;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.QuickBuild;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.ToolGroup;
using System;
using System.Collections;
using System.Diagnostics;

namespace VisionSort.Services
{
    /// <summary>一次生产判定的结果。</summary>
    public sealed class InspectionResult
    {
        /// <summary>工具跑完了没有（false＝抛异常，原因在 <see cref="Message"/>）。</summary>
        public bool Ran;

        /// <summary>跑是跑完了，但超过了「检测超时」。</summary>
        public bool TimedOut;

        /// <summary>是否达标（＝工具的 Accepted，判定阀值由调用方每轮写进 RunParams）。</summary>
        public bool Accepted;

        /// <summary>最高匹配分数（没有匹配时为 0）。</summary>
        public double Score;

        /// <summary>方案里的 PMA 工具名（写进提示，便于确认跑的是哪把）。</summary>
        public string ToolName = string.Empty;

        /// <summary>
        /// 最高分那条结果的**像素坐标**（＝模板原点在图像里的位置）。
        /// 九点标定就是把这个 (u,v) 换算成机械臂 mm（见 九点标定联机分拣方案.md §4.9）。
        /// 一个结果都没有时 <see cref="HasPixel"/> = false，分拣那边据此拒绝动作。
        /// </summary>
        public bool HasPixel;

        /// <summary>像素 X（图像列）。</summary>
        public double PixelX;

        /// <summary>像素 Y（图像行）。</summary>
        public double PixelY;

        /// <summary>中文说明（成功时空串）。</summary>
        public string Message = string.Empty;

        /// <summary>本次检测耗时（毫秒）。</summary>
        public long ElapsedMs;
    }

    /// <summary>
    /// PMA 模板原点检查结果（「九点标定联机分拣方案.md」§4.10.2 N1 #1/#3）。
    ///
    /// 为什么要有这个检查：PMA 报出来的"位置"指的是**模板原点**在图像里的位置，
    /// 而模板原点的**默认值是 (0,0)** —— 也就是**图像左上角**，不是工件。
    /// 实测：`Pattern.Train()` 之后 `Origin` 平移 = (0.0000, 0.0000)；设成 (630,530) 后
    /// 结果位姿才变成 (629.9839, 529.9862)。所以**判定 OK/NG 不受影响，但拿它去驱动机械臂会全错，
    /// 而且不报任何错**。人眼从数字上看不出这件事，只能由程序拦。
    /// </summary>
    public sealed class PatternOriginCheck
    {
        /// <summary>能不能拿这个模板去做标定（原点已设 + 模板已训练）。</summary>
        public bool Ok;

        /// <summary>模板是否已训练。</summary>
        public bool Trained;

        /// <summary>原点是否还是 (0,0)（＝没设过）。</summary>
        public bool OriginUnset;

        /// <summary>模板原点（像素）。</summary>
        public double OriginX;
        public double OriginY;

        /// <summary>能不能取到训练区域中心（区域形状不认识时取不到，只影响那句"离中心多远"的提示）。</summary>
        public bool HasRegionCenter;
        public double RegionCenterX;
        public double RegionCenterY;

        /// <summary>中文说明（可以直接显示给操作工）。</summary>
        public string Message = string.Empty;
    }

    /// <summary>
    /// 生产用的视觉方案（「主运行页方案.md」Q3/Q4=A）。
    ///
    /// 两件事：
    ///   ① <see cref="Load"/>：每次「开始检测」重新加载 VPP 并取出里面的 PMA 工具。
    ///      这样"改了模板/保存了方案，点一次开始就生效"，不会拿着过期对象跑
    ///      （代价是开始时要等一两秒，值）。
    ///   ② <see cref="Inspect"/>：把生产阈值写进工具、喂图、跑一次、读分数。
    ///      **不用** <see cref="PmaTemplateTrainer.Verify"/>——那是训练后自检，会按设置重建训练区域、
    ///      可能改工具状态，拿来跑生产既慢又危险。
    ///
    /// 阈值职责（沿用 视觉参数配置方案.md Q2=A）：判定阈值只认 <see cref="VisionRunSettings.MatchThreshold"/>
    /// （系统设置页那份），视觉配置页那份只管训练/自检，两者互不写穿。
    /// </summary>
    public sealed class VisionScheme
    {
        /// <summary>唯一实例。</summary>
        public static VisionScheme Shared { get; } = new VisionScheme();

        private object _root;          // 保住加载出来的对象树，别被 GC 收走
        private CogPMAlignTool _tool;

        private VisionScheme()
        {
        }

        /// <summary>当前视觉方案路径（视觉配置页加载/另存时写入）。</summary>
        public string VppPath { get; set; } = string.Empty;

        /// <summary>方案是否已加载出可用的 PMA 工具。</summary>
        public bool IsLoaded
        {
            get { return _tool != null; }
        }

        /// <summary>当前 PMA 工具名（未加载时空串）。</summary>
        public string ToolName
        {
            get { return _tool == null ? string.Empty : _tool.Name; }
        }

        /// <summary>
        /// 检查当前方案的 PMA **模板原点**（N1 #1/#3）。
        /// 判定 OK/NG 时用不到它，但**一旦要拿像素坐标去驱动机械臂，它必须是工件上的那个点**。
        /// </summary>
        public PatternOriginCheck CheckPatternOrigin()
        {
            return CheckOrigin(_tool);
        }

        /// <summary>
        /// 检查**任意一把** PMA 的模板原点（视觉配置页拿它在训练完之后当场检查，不用等生产方案加载）。
        /// 消息文案只此一份，避免两处写法不一致。
        /// </summary>
        public static PatternOriginCheck CheckOrigin(CogPMAlignTool tool)
        {
            PatternOriginCheck check = new PatternOriginCheck();

            if (tool == null)
            {
                check.Message = "还没加载视觉方案。请先到「视觉配置」页加载一次 VPP，再来核对模板原点。";
                return check;
            }

            try
            {
                CogTransform2DLinear origin = tool.Pattern.Origin;
                check.Trained = tool.Pattern.Trained;

                if (origin != null)
                {
                    check.OriginX = origin.TranslationX;
                    check.OriginY = origin.TranslationY;
                }

                // 训练区域中心（只为下面那句"离中心多远"的提示；取不到不影响判定）
                try
                {
                    CogRectangle rect = tool.Pattern.TrainRegion as CogRectangle;
                    if (rect != null) { check.HasRegionCenter = true; check.RegionCenterX = rect.CenterX; check.RegionCenterY = rect.CenterY; }
                    else
                    {
                        CogRectangleAffine affine = tool.Pattern.TrainRegion as CogRectangleAffine;
                        if (affine != null) { check.HasRegionCenter = true; check.RegionCenterX = affine.CenterX; check.RegionCenterY = affine.CenterY; }
                    }
                }
                catch (Exception)
                {
                    check.HasRegionCenter = false;
                }

                // 原点"等于 (0,0)"＝没设过。这是实测出来的默认值，不可能是有人真想要的位置。
                check.OriginUnset = Math.Abs(check.OriginX) < 0.5 && Math.Abs(check.OriginY) < 0.5;

                if (!check.Trained)
                {
                    check.Message = "PMA 模板还没训练（VisionPro：未训练）。请先点『训练模板』并保存方案，"
                        + "再回来核对模板原点 —— 模板没训练时原点是什么都说明不了。";
                    return check;
                }

                if (check.OriginUnset)
                {
                    check.Message = "★ 模板原点还在 (0,0) —— 那是**图像左上角**，不是工件。\r\n"
                        + "判定 OK/NG 不受影响，但拿这个像素坐标去做九点标定（＝驱动机械臂）会**整体偏移一大截，而且不报错**。\r\n"
                        + "处理：把模板原点设到『工件上要被吸住的那个点』（改原点不用重新训练），再保存方案。";
                    return check;
                }

                check.Ok = true;
                check.Message = "模板原点 = (" + check.OriginX.ToString("F1") + ", " + check.OriginY.ToString("F1") + ")，已设置 ✓";
                if (check.HasRegionCenter)
                {
                    double dx = check.OriginX - check.RegionCenterX;
                    double dy = check.OriginY - check.RegionCenterY;
                    if (Math.Sqrt(dx * dx + dy * dy) > 50)
                    {
                        // 不是错误：故意把原点放在工件另一处特征上是合法的。只提醒一句让人自己判断。
                        check.Message += "（比训练区域中心偏了 " + Math.Sqrt(dx * dx + dy * dy).ToString("F0")
                            + " 像素，是故意的话请忽略）";
                    }
                }
                return check;
            }
            catch (Exception ex)
            {
                check.Message = "读模板原点失败：" + ex.Message;
                return check;
            }
        }

        /// <summary>把模板原点设到训练区域中心（只在区域是矩形类形状时可用）。返回 false 时给中文原因。</summary>
        public static bool TrySetOriginToRegionCenter(CogPMAlignTool tool, out string message)
        {
            if (tool == null || tool.Pattern == null)
            {
                message = "没有可用的 PMA 工具。";
                return false;
            }

            double cx, cy;
            try
            {
                CogRectangle rect = tool.Pattern.TrainRegion as CogRectangle;
                if (rect != null) { cx = rect.CenterX; cy = rect.CenterY; }
                else
                {
                    CogRectangleAffine affine = tool.Pattern.TrainRegion as CogRectangleAffine;
                    if (affine == null)
                    {
                        message = "训练区域不是矩形（取不到中心），请在 VisionPro 里手工把原点拖到吸取点。";
                        return false;
                    }
                    cx = affine.CenterX;
                    cy = affine.CenterY;
                }

                CogTransform2DLinear origin = new CogTransform2DLinear();
                origin.TranslationX = cx;
                origin.TranslationY = cy;
                tool.Pattern.Origin = origin;

                message = "已把模板原点设到训练区域中心 (" + cx.ToString("F1") + ", " + cy.ToString("F1") + ")。\r\n"
                    + "★ 还要点一次『保存方案』，否则只改了内存里的方案。";
                return true;
            }
            catch (Exception ex)
            {
                message = "设置模板原点失败：" + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 加载 <see cref="VppPath"/> 并取出 PMA 工具。返回 false 时 <paramref name="message"/> 是中文原因。
        /// 支持 CogJobManager（QuickBuild 工程）/ CogJob / CogToolBlock / CogToolGroup（与视觉配置页同一套判断）。
        /// </summary>
        public bool Load(out string message)
        {
            if (string.IsNullOrEmpty(VppPath))
            {
                message = "还没加载视觉方案。请先到「视觉配置」页加载一次 VPP——主运行页用方案里的 PMA 工具做判定。";
                return false;
            }
            if (!System.IO.File.Exists(VppPath))
            {
                message = "视觉方案文件不在了：" + VppPath;
                return false;
            }

            try
            {
                object loaded = CogSerializer.LoadObjectFromFile(VppPath);

                CogJobManager manager = loaded as CogJobManager;
                CogJob job = loaded as CogJob;
                if (job == null && manager != null && manager.JobCount > 0) job = manager.Job(0);

                ICogTool visionTool = job != null ? job.VisionTool as ICogTool : loaded as ICogTool;
                if (visionTool == null)
                {
                    message = "方案根节点不认识：" + loaded.GetType().Name + "（支持 CogJobManager / CogJob / CogToolBlock / CogToolGroup）。";
                    return false;
                }

                CogPMAlignTool tool = visionTool as CogPMAlignTool;
                if (tool == null)
                {
                    // ⚠ 注意：**ICogTool 本身不是 IEnumerable**（自测实测踩到：直接 as IEnumerable 拿到 null，
                    // 于是"方案里没有 PMA 工具"这种误报）。要按类型取它的工具集合——
                    // 与视觉配置页 GetSchemeTools() 的取法一致：ToolBlock → .Tools；ToolGroup → .Tools。
                    IEnumerable tools = null;
                    CogToolBlock block = visionTool as CogToolBlock;
                    if (block != null) tools = block.Tools;
                    else
                    {
                        CogToolGroup group = visionTool as CogToolGroup;
                        if (group != null) tools = group.Tools;
                    }
                    if (tools != null) tool = PmaTemplateTrainer.FindTool(tools, null, 3);
                }

                if (tool == null)
                {
                    message = "方案里没有找到 PMA 工具（CogPMAlignTool）。请确认方案里加了 PMA 并把模板训练好。";
                    return false;
                }

                _root = loaded;
                _tool = tool;
                message = "已加载方案：" + System.IO.Path.GetFileName(VppPath) + "，判定工具：" + tool.Name;
                return true;
            }
            catch (Exception ex)
            {
                _root = null;
                _tool = null;
                message = "加载视觉方案失败：" + ex.Message;
                return false;
            }
        }

        /// <summary>卸掉当前方案（不删文件）。</summary>
        public void Unload()
        {
            _tool = null;
            _root = null;
        }

        /// <summary>
        /// 一个结果都没有时给"能照着做"的中文原因：优先用 VisionPro 的 RunStatus.Message（
        /// 实测未训练时它就是"未训练"），取不到再退化成通用说法。
        /// </summary>
        private string DescribeNoResult()
        {
            string status = string.Empty;
            try
            {
                ICogRunStatus runStatus = _tool.RunStatus;
                if (runStatus != null)
                {
                    status = runStatus.Message ?? string.Empty;
                    if (status.Length == 0) status = runStatus.Result.ToString();
                    if (runStatus.Exception != null) status += "（" + runStatus.Exception.Message + "）";
                }
            }
            catch (Exception)
            {
                // RunStatus 读不到就算了，别让它把检测结果也带崩
            }

            if (status.IndexOf("未训练", StringComparison.OrdinalIgnoreCase) >= 0
                || status.IndexOf("not trained", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "PMA 模板还没训练（VisionPro：未训练）。请到「视觉配置」页点『训练模板』，再点『保存方案』。";
            }
            if (status.Length > 0) return "没有找到匹配：" + status;
            return "没有找到匹配（PMA 结果为空）。";
        }

        /// <summary>
        /// 跑一次判定。
        ///
        /// ★ **判定由我们自己算，不交给 VisionPro 的 AcceptThreshold**（2026-09-20 全流程实测改的）：
        ///   `CogPMAlignTool.RunParams.AcceptThreshold` 会把结果**从 Results 集合里滤掉** ——
        ///   实测同一张分数 0.9401 的图，阈值设 0.90 时 `Results.Count` 直接变 0（不是 Accepted=false，
        ///   是**根本没结果**）。后果是判 NG 时拿不到像素坐标 → 机械臂不知道该去哪儿抓 → 分拣永远不动作。
        ///   所以这里固定用 `AcceptThreshold = 0` 跑，拿到最佳候选后：
        ///     · `Accepted = 分数 ≥ threshold`   ← 判定（<paramref name="threshold"/> 就是系统设置页那份）
        ///     · `HasPixel = 分数 ≥ PickScoreFloor` ← "视野里到底有没有工件"，决定值不值得让机械臂去抓
        ///
        /// <paramref name="timeoutMs"/> 是看门狗：Cognex 工具一旦跑起来没法中断，所以这里只能"跑完再对表"，
        /// 超时含义是"这次检测不可信"，由调用方决定停线（见 主运行页方案.md §3.3）。
        /// </summary>
        public InspectionResult Inspect(ICogImage image, double threshold, int timeoutMs)
        {
            InspectionResult result = new InspectionResult();
            if (_tool == null)
            {
                result.Message = "没有可用的 PMA 工具（方案没加载或里面没有 PMA）。";
                return result;
            }
            if (image == null)
            {
                result.Message = "没有图可检测。";
                return result;
            }

            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                // ★ 固定 0：**不让 VisionPro 替我们滤结果**（0.90 的阈值会把 0.94 的匹配也滤成 0 条，
                //   那样判 NG 时就没有像素可用、机械臂永远不动）。判定在下面自己算。
                _tool.RunParams.AcceptThreshold = 0;
                _tool.InputImage = PmaTemplateTrainer.EnsureGreyImage(image);
                _tool.Run();
                watch.Stop();

                result.Ran = true;
                result.ToolName = _tool.Name;
                result.ElapsedMs = watch.ElapsedMilliseconds;

                CogPMAlignResults results = _tool.Results;
                double best = double.NaN;
                CogTransform2DLinear bestPose = null;
                if (results != null)
                {
                    for (int i = 0; i < results.Count; i++)
                    {
                        var item = results[i];
                        if (double.IsNaN(best) || item.Score > best)
                        {
                            best = item.Score;
                            bestPose = item.GetPose();   // 最高分那条的像素位置（分拣要用）
                        }
                    }
                }
                result.Score = double.IsNaN(best) ? 0d : best;

                // 判定：**我们自己比**（工具那边 AcceptThreshold 恒 0，它的 Accepted 已经没有意义）
                result.Accepted = result.Score >= threshold;

                // 像素坐标：实测 GetPose().TranslationX/Y ＝ 模板原点在运行图中的像素位置
                // （同图重跑差 0.016px；图整体平移 (+60,-35) 后位姿跟着走 (689.9843,494.9865)）。
                //
                // 能不能拿去抓还要过一道"视野里到底有没有工件"的门槛：
                //   实测（AcceptThreshold=0）有齿轮 0.9401，而只有背景 / 全黑 / 全白 / 全灰 全都是 0.0000。
                //   低于门槛就不给像素 —— 免得对着空带子空抓一次。
                if (bestPose != null && result.Score >= VisionRunSettings.Shared.PickScoreFloor)
                {
                    result.PixelX = bestPose.TranslationX;
                    result.PixelY = bestPose.TranslationY;
                    result.HasPixel = true;
                }

                // 一个结果都没有时，别只报"分数 0.000 → NG"——那是把人往错方向带。
                // VisionPro 自己的 RunStatus 才是真话（实测：未训练的 PMA 就是 RunStatus=Error/"未训练"，
                // 分数恒 0，看着像"检测不合格"，其实是模板根本没训练）。
                if (results == null || results.Count == 0)
                {
                    result.Message = DescribeNoResult();
                }

                if (timeoutMs > 0 && result.ElapsedMs > timeoutMs)
                {
                    result.TimedOut = true;
                    result.Message = "检测超时（耗时 " + result.ElapsedMs + "ms，限制 " + timeoutMs + "ms）";
                }
            }
            catch (Exception ex)
            {
                watch.Stop();
                result.ElapsedMs = watch.ElapsedMilliseconds;
                result.Message = "检测失败：" + ex.Message;
            }

            return result;
        }
    }
}
