using Cognex.VisionPro;
using Cognex.VisionPro.Display;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.ToolGroup;
using System;
using System.IO;
using System.Windows.Forms;
using VisionSort.Services;

namespace VisionSort.Views
{
    /// <summary>
    /// 视觉配置页 · PMA 模板训练（grpPma 分组）。
    /// 同一个 partial 类拆三份，跟现项目一致：
    ///   本文件                        —— 采集训练图 / 训练模板 / 保存模板 的业务逻辑
    ///
    /// 职责边界：本页只做“训练”——采集一张图 → 划区域 → 训练出 CogPMAlignPattern → 存进方案。
    /// 生产期的识别（PMA.Run 找工件、判 OK/NG、坐标输出、通讯）不在这里；
    /// 训练后调用的 PMA.Run() 只是自检（显示“验证分数”），不代表本页参与识别。
    ///
    /// 训练算法在 <see cref="PmaTemplateTrainer"/>，本文件只做“取图 → 训练 → 显示 → 存盘”的界面调度。
    /// </summary>
    public partial class VisionConfigView
    {
        /// <summary>StaticGraphics 里放精细特征用的分组名。</summary>
        private const string FineGraphicsGroup = "精细模板";

        /// <summary>InteractiveGraphics 里放训练区域用的分组名。</summary>
        private const string TrainRegionGroup = "训练区域";

        private readonly PmaTrainSettings _pmaSettings = new PmaTrainSettings();
        private PmaTemplateTrainer _pmaTrainer;
        private ICogImage _trainImage;
        private bool _pmaBusy;          // 防重入
        private bool _refreshingToolList; // 刷新下拉时抑制 SelectedIndexChanged

        /// <summary>
        /// 相机取像回调：联调时由相机层（主运行页/相机服务）注入，返回一帧图像。
        /// 为空时『采集训练图』依次退化为：方案里的 CogAcqFifoTool → 本地图片文件。
        /// 说明：训练页“借图不抢相机”——GigE 相机同一时刻基本只允许一个 FIFO 占用。
        /// </summary>
        public Func<ICogImage> TrainingImageProvider { get; set; }

        /// <summary>
        /// 方案加载完成后可调（可选一行）：刷新模板工具下拉 + 把方案里已训练的模板显示到预览框。
        /// 不调用也不影响功能——三个按钮每次点击都会先刷新下拉、再按选中项解析工具。
        /// </summary>
        public void SyncPmaFromScheme()
        {
            RefreshPmaToolList();
            _pmaTrainer = null;
            _trainImage = null;

            PmaToolRef selected = cmbPmaTool.SelectedItem as PmaToolRef;
            if (selected == null)
            {
                ClearPreview();
                lblTplScore.Text = "验证分数：--";
                return;
            }

            _pmaTrainer = new PmaTemplateTrainer(selected.Tool);
            _trainImage = selected.Tool.Pattern != null ? selected.Tool.Pattern.TrainImage : null;
            RenderPreview();
            lblTplScore.Text = _pmaTrainer.IsTrained ? "验证分数：--（方案里的模板已训练）" : "验证分数：--";
            RefreshPatternOriginHint(selected.Tool, false);
        }

        /// <summary>
        /// N1 #2：把模板原点显示出来（原点是个隐藏状态，不显示就等于不存在）。
        /// 只在**有问题时**才改 lblTplScore 的文案 —— 那一格只有 200px 宽，
        /// 正常时保持原样（"界面不许撒谎"不等于"每格都要刷屏"）。
        /// 要改这个标签的宽度/位置得动 Designer，本次不动（判据：问题态用短文案也够醒目）。
        /// </summary>
        private void RefreshPatternOriginHint(CogPMAlignTool tool, bool showDialogWhenBad)
        {
            PatternOriginCheck check = VisionScheme.CheckOrigin(tool);

            if (check.Ok)
            {
                if (lblTplScore.Text.StartsWith("⚠", StringComparison.Ordinal))
                    lblTplScore.Text = "验证分数：--";
                return;
            }

            if (check.OriginUnset)
            {
                lblTplScore.Text = "⚠ 模板原点(0,0)";
                lblTplScore.ForeColor = System.Drawing.Color.Firebrick;

                if (showDialogWhenBad)
                {
                    DialogResult answer = MessageBox.Show(this,
                        check.Message + "\r\n\r\n现在把模板原点设到训练区域中心吗？\r\n"
                        + "（设完还要点一次『保存方案』；如果吸取点不是区域中心，请到 VisionPro 里手工拖原点。）",
                        "模板原点没设 —— 会影响机械臂抓取",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (answer == DialogResult.Yes)
                    {
                        string message;
                        bool ok = VisionScheme.TrySetOriginToRegionCenter(tool, out message);
                        MessageBox.Show(this, message, "模板原点",
                            MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                        if (ok)
                        {
                            lblTplScore.Text = "验证分数：--";
                            lblTplScore.ForeColor = System.Drawing.SystemColors.ControlText;
                        }
                    }
                }
                return;
            }

            // 其它情况（未训练等）不在这里打扰人，交给训练流程自己报
        }

        // ------------------------------------------------------------------
        // 三个按钮
        // ------------------------------------------------------------------

        /// <summary>『采集训练图』：拿一帧图当训练图，区域沿用方案已有的（没有才生成），并显示出来。</summary>
        private void btnGrabTpl_Click(object sender, EventArgs e)
        {
            if (_pmaBusy) return;

            PmaTemplateTrainer trainer = ResolveTrainer(true);
            if (trainer == null) return;

            try
            {
                _pmaBusy = true;
                Cursor.Current = Cursors.WaitCursor;

                ICogImage image = AcquireTrainingImage();
                if (image == null) return;   // 用户取消，或取像工具没出图

                _trainImage = trainer.SetInputImage(image);   // 只喂图：顺势转 8 位灰度，且不作废旧模板
                ApplySettingsFromUi();
                EnsureTrainRegion(trainer);                   // A：方案已有区域 → 沿用；没有 → 生成默认区域

                RenderPreview();
                lblTplScore.Text = trainer.IsTrained
                    ? "已换新图（旧模板仍有效，点『训练模板』用它重训）"
                    : "验证分数：--（已采集训练图，点『训练模板』开始训练）";
            }
            catch (Exception ex)
            {
                ShowPmaError("采集训练图", ex);
            }
            finally
            {
                _pmaBusy = false;
                Cursor.Current = Cursors.Default;
            }
        }

        /// <summary>『训练模板』：写参数 → 沿用/生成区域 → 训练（只训练，不做识别），刷新状态文本。</summary>
        private void btnTrain_Click(object sender, EventArgs e)
        {
            if (_pmaBusy) return;

            PmaTemplateTrainer trainer = ResolveTrainer(true);
            if (trainer == null) return;

            if (_trainImage == null && trainer.Tool.Pattern != null)
            {
                _trainImage = trainer.Tool.Pattern.TrainImage;   // 方案里已带模板：允许直接重训
            }
            if (!trainer.HasTrainImage && _trainImage != null)
            {
                trainer.SetInputImage(_trainImage);   // 兜底：把本页已采集的图补给（可能刚重新绑定的）训练器
            }
            if (!trainer.HasTrainImage)
            {
                MessageBox.Show(this, "还没有训练图，请先点『采集训练图』。",
                    "训练模板", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _pmaBusy = true;
                Cursor.Current = Cursors.WaitCursor;

                ApplySettingsFromUi();
                PmaTrainResult result = trainer.Train(_pmaSettings);

                RenderPreview();
                lblTplScore.Text = result.ScoreText;

                // N1 #1：**坑的源头就在这一页** —— 训练完当场查模板原点。
                // 实测 `Pattern.Train()` 之后原点是 (0,0)＝图像左上角；判定 OK/NG 不受影响，
                // 但拿它去驱动机械臂会整体抓偏而且不报错。在这里拦最省钱。
                RefreshPatternOriginHint(trainer.Tool, true);

                MessageBox.Show(this, result.Summary, "训练模板",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowPmaError("训练模板", ex);
            }
            finally
            {
                _pmaBusy = false;
                Cursor.Current = Cursors.Default;
            }
        }

        /// <summary>『保存模板』：把训练好的模式单独存成 .pat；要进生产方案还得『保存方案』。</summary>
        private void btnSaveTpl_Click(object sender, EventArgs e)
        {
            PmaTemplateTrainer trainer = ResolveTrainer(true);
            if (trainer == null) return;

            if (!trainer.IsTrained)
            {
                MessageBox.Show(this, "当前还没有训练好的模板，先点『训练模板』。",
                    "保存模板", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tplDir = Path.Combine(Application.StartupPath, "templates");
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = "保存PMA模板";
                dlg.Filter = "PMA模板|*.pat|所有文件|*.*";
                dlg.InitialDirectory = Directory.Exists(tplDir)
                    ? tplDir
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dlg.FileName = BuildTemplateFileName(trainer);
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    Cursor.Current = Cursors.WaitCursor;
                    trainer.SavePattern(dlg.FileName);
                    MessageBox.Show(this,
                        "模板已保存：\r\n" + dlg.FileName +
                        "\r\n提示：要让生产方案用上这个模板，请再点一次『保存方案』把 VPP 写回。",
                        "保存模板", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    ShowPmaError("保存模板", ex);
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                }
            }
        }

        /// <summary>D3：切换模板工具后重绑训练器，并把该工具的模板显示到预览框。</summary>
        private void cmbPmaTool_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_refreshingToolList) return;

            _pmaTrainer = null;
            _trainImage = null;

            PmaToolRef selected = cmbPmaTool.SelectedItem as PmaToolRef;
            if (selected == null)
            {
                ClearPreview();
                lblTplScore.Text = "验证分数：--";
                return;
            }

            _pmaTrainer = new PmaTemplateTrainer(selected.Tool);
            _trainImage = selected.Tool.Pattern != null ? selected.Tool.Pattern.TrainImage : null;
            RenderPreview();
            lblTplScore.Text = _pmaTrainer.IsTrained ? "验证分数：--（该工具的模板已训练）" : "验证分数：--";
        }

        // ------------------------------------------------------------------
        // 工具解析 / 下拉刷新
        // ------------------------------------------------------------------

        /// <summary>
        /// 解析当前要训练的 PMA 工具：
        ///   1) 先按当前方案刷新下拉（换过 VPP 也能立刻对齐）；
        ///   2) 取下拉选中项，绑定 PmaTemplateTrainer；
        ///   3) 下拉为空 → 按原因提示（未加载方案 / 方案里没有 PMA），返回 null，不做任何修改。
        /// 只用方案里已有的 PMA，绝不新建工具。
        /// </summary>
        private PmaTemplateTrainer ResolveTrainer(bool showTip)
        {
            RefreshPmaToolList();

            PmaToolRef selected = cmbPmaTool.SelectedItem as PmaToolRef;
            if (selected == null)
            {
                if (showTip)
                {
                    string message = GetSchemeTools() == null
                        ? "还没有加载 VPP 方案，请先点『加载VPP方案』再训练模板。"
                        : "当前方案里没有 PMA 模板工具（CogPMAlignTool）。\r\n" +
                          "请在 VisionPro 的 QuickBuild 里把 PMA 工具加进方案（ToolBlock / ToolGroup，最多往下找一层），" +
                          "保存 VPP 后回到本页重新『加载VPP方案』。\r\n" +
                          "本页不新建工具：模板训练必须作用在方案里那把 PMA 上。";
                    MessageBox.Show(this, message, "PMA模板训练",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return null;
            }

            if (_pmaTrainer == null || !ReferenceEquals(_pmaTrainer.Tool, selected.Tool))
            {
                _pmaTrainer = new PmaTemplateTrainer(selected.Tool);
            }
            return _pmaTrainer;
        }

        /// <summary>
        /// 把当前方案（顶层 + 一层子容器，D4）里的 PMA 工具填进下拉，尽量保持原来的选中项。
        /// </summary>
        private void RefreshPmaToolList()
        {
            PmaToolRef previous = cmbPmaTool.SelectedItem as PmaToolRef;
            string previousDisplay = previous != null ? previous.Display : null;

            _refreshingToolList = true;
            try
            {
                cmbPmaTool.Items.Clear();

                System.Collections.IEnumerable tools = GetSchemeTools();
                if (tools != null)
                {
                    foreach (PmaToolRef item in PmaTemplateTrainer.ListPmaTools(tools))
                    {
                        cmbPmaTool.Items.Add(item);
                    }
                }

                int index = IndexOfToolDisplay(previousDisplay);
                if (index < 0) index = IndexOfToolDisplay(PmaTemplateTrainer.DefaultToolName);
                if (index < 0 && cmbPmaTool.Items.Count > 0) index = 0;
                cmbPmaTool.SelectedIndex = index;   // -1 表示当前方案里没有可用 PMA
            }
            finally
            {
                _refreshingToolList = false;
            }

            // 这里**不能**把 _pmaTrainer 置空：训练器里缓存着“本页采集的当前图”（SetInputImage 只喂图、
            // 不写 Pattern.TrainImage），一旦丢弃实例，『训练模板』就会误报“还没有训练图”。
            // 工具实例真的换了（换过方案）时，ResolveTrainer 里的 ReferenceEquals 判断会自动重建。
        }

        private int IndexOfToolDisplay(string display)
        {
            if (string.IsNullOrEmpty(display)) return -1;
            for (int i = 0; i < cmbPmaTool.Items.Count; i++)
            {
                PmaToolRef item = cmbPmaTool.Items[i] as PmaToolRef;
                if (item != null && string.Equals(item.Display, display, StringComparison.Ordinal)) return i;
            }
            return -1;
        }

        /// <summary>当前方案的工具集合：ToolBlock 优先，其次 CogJob 里的 ToolGroup。</summary>
        private System.Collections.IEnumerable GetSchemeTools()
        {
            if (_toolBlock != null) return _toolBlock.Tools;
            if (_job != null && _job.VisionTool is CogToolGroup group) return group.Tools;
            return null;
        }

        // ------------------------------------------------------------------
        // 取图
        // ------------------------------------------------------------------

        /// <summary>
        /// 取一帧训练图，优先级：
        ///   1) 相机层注入的 TrainingImageProvider（相机在 WinForms 侧时用，借图不抢相机）；
        ///   2) 方案里自带的取像工具（= QuickBuild 的 Image Source：CogAcqFifoTool）；
        ///   3) 本地图片文件（没相机也能离线训练模板）。
        /// 返回 null 表示用户取消或确实取不到图。
        /// </summary>
        private ICogImage AcquireTrainingImage()
        {
            if (TrainingImageProvider != null)
            {
                ICogImage frame = TrainingImageProvider();
                if (frame != null) return frame;
            }

            CogAcqFifoTool acq = FindAcqTool();
            if (acq != null)
            {
                ICogImage frame = null;
                string error = null;
                try
                {
                    acq.Run();
                    frame = acq.OutputImage;
                }
                catch (Exception ex)
                {
                    error = ex.Message;   // 常见：Operator 没配置 / 相机没连上
                }

                if (frame != null) return frame;

                string reason = string.IsNullOrEmpty(error)
                    ? "方案里的取像工具没有取到图。"
                    : "方案里的取像工具取像失败：\r\n" + error;
                DialogResult choice = MessageBox.Show(this,
                    reason + "\r\n\r\n是否改从本地图片文件选择训练图？",
                    "采集训练图", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (choice != DialogResult.Yes) return null;

                return AcquireTrainingImageFromFile();
            }

            return AcquireTrainingImageFromFile();
        }

        /// <summary>在已加载方案的工具里找取像工具（vpp 自带相机时用，Image Source）。</summary>
        private CogAcqFifoTool FindAcqTool()
        {
            System.Collections.IEnumerable tools = GetSchemeTools();
            if (tools == null) return null;

            foreach (object element in tools)
            {
                CogAcqFifoTool acq = element as CogAcqFifoTool;
                if (acq != null) return acq;
            }
            return null;
        }

        /// <summary>从本地图片文件取训练图（离线兜底路径）。</summary>
        private ICogImage AcquireTrainingImageFromFile()
        {
            string sampleDir = Path.Combine(Application.StartupPath, "samples");
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "选择一张训练图（没有相机时用）";
                dlg.Filter = "图片文件|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.idb|所有文件|*.*";
                dlg.InitialDirectory = Directory.Exists(sampleDir)
                    ? sampleDir
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (dlg.ShowDialog(this) != DialogResult.OK) return null;

                Cursor.Current = Cursors.WaitCursor;
                return PmaTemplateTrainer.LoadImageFromFile(dlg.FileName);
            }
        }

        // ------------------------------------------------------------------
        // 参数 / 区域 / 预览 / 提示
        // ------------------------------------------------------------------

        /// <summary>
        /// 读界面控件 → _pmaSettings：
        /// 角度范围（cmbAngle：±180°/±90°/±30°/±10°）、粒度（cmbGranularity：自动/粗/标准/细）、
        /// 匹配阈值（cmbThreshold：可选值，选中即用；解析失败保持默认 0.70）。
        /// </summary>
        private void ApplySettingsFromUi()
        {
            double low;
            double high;
            if (TryParseAngleRange(cmbAngle.Text, out low, out high))
            {
                _pmaSettings.AngleLowDeg = low;
                _pmaSettings.AngleHighDeg = high;
            }

            double threshold;
            if (double.TryParse(cmbThreshold.Text, out threshold))
            {
                _pmaSettings.AcceptThreshold = threshold;
            }

            _pmaSettings.GrainPreset = ParseGrainPreset(cmbGranularity.Text);
        }

        /// <summary>粒度下拉文本 → 预设（认不出来就按“自动”）。</summary>
        private static PmaGrainPreset ParseGrainPreset(string text)
        {
            if (string.IsNullOrEmpty(text)) return PmaGrainPreset.Auto;

            switch (text.Trim())
            {
                case "粗": return PmaGrainPreset.Coarse;
                case "标准": return PmaGrainPreset.Standard;
                case "细": return PmaGrainPreset.Fine;
                default: return PmaGrainPreset.Auto;
            }
        }

        /// <summary>把“±180°”这类文本解析成上下限；解析不了返回 false（保持原值）。</summary>
        private static bool TryParseAngleRange(string text, out double low, out double high)
        {
            low = -180.0;
            high = 180.0;
            if (string.IsNullOrEmpty(text)) return false;

            string span = text.Replace("±", string.Empty).Replace("°", string.Empty).Trim();
            double value;
            if (!double.TryParse(span, out value)) return false;

            low = -value;
            high = value;
            return true;
        }

        /// <summary>A：方案里已有训练区域 → 原样沿用（绝不覆盖）；没有 → 才生成居中默认区域并设原点。</summary>
        private void EnsureTrainRegion(PmaTemplateTrainer trainer)
        {
            if (trainer.CurrentTrainRegion != null) return;
            trainer.ApplyTrainRegion(trainer.BuildTrainRegion(_pmaSettings), resetOrigin: true);
        }

        /// <summary>默认模板文件名：PMA模板_工具名_时间戳.pat。</summary>
        private static string BuildTemplateFileName(PmaTemplateTrainer trainer)
        {
            string toolName = trainer.Tool.Name;
            if (string.IsNullOrEmpty(toolName)) toolName = PmaTemplateTrainer.DefaultToolName;
            return string.Format("PMA模板_{0}_{1:yyyyMMdd_HHmmss}.pat", toolName, DateTime.Now);
        }

        /// <summary>
        /// 刷新模板预览：底图 + 特征标注（精细特征紫、粗特征青）+ 可拖拽的训练区域。
        ///
        /// 顺序很关键：VisionPro 的显示控件在设置 Image / Record 时会重置 Static/Interactive 容器，
        /// 所以必须“先铺底图 → 再叠标注 → 最后 Fit”，否则标注会被清掉——
        /// 表现就是“训练完预览里只有原图、看不到特征”。
        /// 这里也**不使用** `CreateCurrentRecord()`：标注全部由我们自己叠加，避免两套内容互相打架。
        /// </summary>
        private void RenderPreview()
        {
            ICogImage image = _trainImage;
            if (image == null && _pmaTrainer != null && _pmaTrainer.Tool.Pattern != null)
            {
                image = _pmaTrainer.Tool.Pattern.TrainImage;
            }

            if (image == null)
            {
                ClearPreview();
                return;
            }

            // ① 底图（先清容器，再设图）
            picTemplate.StaticGraphics.Clear();
            picTemplate.InteractiveGraphics.Clear();
            picTemplate.Image = image;

            // ② 特征标注
            if (_pmaTrainer != null && _pmaTrainer.Tool.Pattern != null && _pmaTrainer.Tool.Pattern.Trained)
            {
                AddPatternGraphics(_pmaTrainer.Tool.Pattern);
            }

            // ③ 训练区域（可拖拽，放在 StaticGraphics 之后）
            AttachInteractiveTrainRegion();

            // ④ 最后适配显示：true = 连同标注一起缩放进可视范围
            picTemplate.Fit(true);
        }

        /// <summary>
        /// 把训练出来的特征叠加到预览上（精细特征轮廓）。
        /// 实测结论（用探针逐像素比对得出）：
        ///   • `CreateGraphicsFine/Coarse` 返回的 `CogGeneralContour` 轮廓在多数情况下重合，
        ///     且颜色参数对轮廓不生效（传 Purple 实际渲染成默认色），同时叠两层只会互相压色；
        ///     所以这里只叠**一层**精细特征。
        ///   • 轮廓的线宽/颜色不适合硬改，保持 VisionPro 默认渲染即可。
        /// </summary>
        private void AddPatternGraphics(CogPMAlignPattern pattern)
        {
            try
            {
                CogGraphicCollection fine = pattern.CreateGraphicsFine(CogColorConstants.Purple);
                if (fine != null && fine.Count > 0)
                {
                    picTemplate.StaticGraphics.AddList(fine, FineGraphicsGroup);
                }
            }
            catch (Exception)
            {
                // 叠加只是显示效果，取不到就只显示图像
            }
        }

        /// <summary>
        /// B：把训练区域挂到预览控件上，供鼠标拖拽微调（所见即所得）。
        /// 挂进去的必须是 Pattern.TrainRegion 那个实例，拖完点『训练模板』即重训。
        /// </summary>
        private void AttachInteractiveTrainRegion()
        {
            // 不设 Pointer 的话鼠标模式是平移，区域把手拖不动
            picTemplate.MouseMode = CogDisplayMouseModeConstants.Pointer;
            picTemplate.InteractiveGraphics.Clear();

            ICogGraphicInteractive graphic = _pmaTrainer != null
                ? _pmaTrainer.CurrentTrainRegion as ICogGraphicInteractive
                : null;
            if (graphic == null) return;

            // Add(图形, 分组名, 是否查重)：查重 false 性能更好，但同一图形只能加一次（上面已 Clear）
            picTemplate.InteractiveGraphics.Add(graphic, TrainRegionGroup, false);
        }

        /// <summary>清空预览框。</summary>
        private void ClearPreview()
        {
            picTemplate.StaticGraphics.Clear();
            picTemplate.InteractiveGraphics.Clear();
            picTemplate.Image = null;
        }

        /// <summary>统一的错误提示：对 PMA 训练失败给出排查方向（按异常类型名判断，避免多引一层异常命名空间）。</summary>
        private void ShowPmaError(string title, Exception ex)
        {
            string hint = string.Empty;
            string typeName = ex.GetType().Name;
            if (typeName.IndexOf("CanNotTrain", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                hint = "\r\n\r\n排查：训练区域里没有足够特征。请把区域放大 / 换到有轮廓的位置，" +
                       "或检查训练图是否过曝过暗（PMA 依赖边缘特征）。";
            }
            else if (typeName.IndexOf("NoTrainImage", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                hint = "\r\n\r\n排查：模式里没有训练图，请重新『采集训练图』。";
            }
            else if (typeName.IndexOf("NotTrained", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                hint = "\r\n\r\n排查：模板还没训练成功，先点『训练模板』。";
            }

            MessageBox.Show(this, title + "失败：\r\n" + ex.Message + hint,
                title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
