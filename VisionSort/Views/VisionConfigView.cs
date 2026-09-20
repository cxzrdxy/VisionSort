using Cognex.VisionPro;
using Cognex.VisionPro.QuickBuild;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.ToolGroup;
using System;
using System.IO;
using System.Windows.Forms;

namespace VisionSort.Views
{
    /// <summary>
    /// 对照草稿：04-资料/04视觉配置页.png
    /// 相机设置 / VPP方案 / PMA模板训练 / 图像保存配置。
    /// 已修正草稿 bug：双“增益”标签合并；匹配阈值可下拉选择，选中值写入 PMA 运行参数
    /// （《技术补充_可施工设计文档_v1.0.md》§8 的配方阈值只作默认显示）。
    /// pnlVppHost 为可视化编辑器预留位（ToolBlock → CogToolBlockEditV2，ToolGroup → CogToolGroupEditV2），
    /// picTemplate 为训练模板预览（CogRecordDisplay）。
    /// PMA 模板训练的实现见 VisionConfigView.Pma.cs（本类 partial）。
    ///
    /// .vpp 的三种形态都能加载：
    ///   ① CogJobManager —— QuickBuild 工程文件（可能含多个作业，取第一个 Job）；
    ///   ② CogJob        —— 单个作业；
    ///   ③ CogToolBlock / CogToolGroup —— 直接是视觉工具。
    /// 保存时按加载时的原始形态写回，保证下次加载原样回来。
    /// </summary>
    public partial class VisionConfigView : UserControl
    {
        private CogToolBlock _toolBlock;      // 视觉工具是 ToolBlock 时
        private CogJob _job;                  // 来自 CogJob / CogJobManager 时
        private CogJobManager _jobManager;    // .vpp 根节点是 QuickBuild 工程（CogJobManager）时
        private string _vppPath;
      
        public VisionConfigView()
        {
            InitializeComponent();

            // 把本页"现场验证过 6 轮"的连接例程注册给共享服务：主运行页要连相机时（开始检测）
            // 就调它，接线与提示仍在相机页，不搬家、不重写（见 主运行页方案.md Q1=A 与 §十七 偏差记录）。
            Services.CameraService.Shared.ConnectHook = delegate
            {
                btnCamConn_Click(this, EventArgs.Empty);
                return Services.CameraService.Shared.IsConnected;
            };
        }

        private void btnLoadVpp_Click(object sender, EventArgs e)
        {
            string vppDir = Path.Combine(Application.StartupPath, "vpps");

            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "VPP文件|*.vpp";
                dlg.InitialDirectory = Directory.Exists(vppDir)
                    ? vppDir
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                if(!string.Equals(Path.GetExtension(dlg.FileName), ".vpp", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(this, "请选择有效的 VPP 文件。", "文件类型错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                btnLoadVpp.Enabled = false;
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    object loaded = CogSerializer.LoadObjectFromFile(dlg.FileName);

                    // ① QuickBuild 工程文件：根节点是 CogJobManager，视觉工具在它的 Job 里
                    CogJobManager manager = loaded as CogJobManager;
                    CogJob job = loaded as CogJob;
                    if (job == null && manager != null && manager.JobCount > 0) job = manager.Job(0);

                    // ② 单作业 / ③ 直接是视觉工具
                    ICogTool visionTool = job != null ? job.VisionTool as ICogTool : loaded as ICogTool;
                    if (visionTool == null)
                    {
                        MessageBox.Show(this,
                            "所选文件不是有效的方案文件。\r\n" +
                            "支持 CogJobManager（QuickBuild 工程）/ CogJob / CogToolBlock / CogToolGroup，当前文件根节点是：" +
                            loaded.GetType().Name,
                            "加载失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    _jobManager = manager;
                    _job = job;
                    _toolBlock = visionTool as CogToolBlock;

                    lblVppPath.Text = "当前VPP路径：" + dlg.FileName;
                    _vppPath = dlg.FileName;

                    // 告诉主运行页"该加载哪个方案"（主运行页每次「开始检测」重新加载它，见 主运行页方案.md Q3=A）
                    Services.VisionScheme.Shared.VppPath = dlg.FileName;

                    HostSchemeEditor(visionTool);

                    // 方案换过之后，PMA 模板训练页跟着刷新（模板工具下拉 + 预览），实现见 VisionConfigView.Pma.cs
                    SyncPmaFromScheme();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "VPP加载失败：\n" + ex.Message,
                        "加载VPP方案", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                    btnLoadVpp.Enabled = true;
                }
            }
        }

        /// <summary>
        /// 把方案的可视化编辑器挂到预留位：
        /// ToolBlock → CogToolBlockEditV2；ToolGroup → CogToolGroupEditV2；其它类型只提示、不挂控件。
        /// 每次换方案都重建编辑器（Subject 类型可能变化），避免叠加或类型不匹配。
        /// </summary>
        private void HostSchemeEditor(ICogTool visionTool)
        {
            for (int i = pnlVppHost.Controls.Count - 1; i >= 0; i--)
            {
                Control old = pnlVppHost.Controls[i];
                if (ReferenceEquals(old, lblVppTip)) continue;
                pnlVppHost.Controls.RemoveAt(i);
                old.Dispose();
            }

            CogToolBlock block = visionTool as CogToolBlock;
            if (block != null)
            {
                CogToolBlockEditV2 edit = new CogToolBlockEditV2();
                edit.Dock = DockStyle.Fill;
                edit.Subject = block;
                pnlVppHost.Controls.Add(edit);
                edit.BringToFront();
                lblVppTip.Visible = false;
                return;
            }

            CogToolGroup group = visionTool as CogToolGroup;
            if (group != null)
            {
                CogToolGroupEditV2 edit = new CogToolGroupEditV2();
                edit.Dock = DockStyle.Fill;
                edit.Subject = group;
                pnlVppHost.Controls.Add(edit);
                edit.BringToFront();
                lblVppTip.Visible = false;
                return;
            }

            lblVppTip.Visible = true;
            lblVppTip.Text = "该方案的视觉工具是 " + visionTool.GetType().Name +
                             "，不支持下挂编辑器；PMA 模板训练仍可正常使用。";
        }

        private void btnSaveVpp_Click(object sender, EventArgs e)
        {
            if (_toolBlock == null && _job == null && _jobManager == null)
            {
                MessageBox.Show(this, "当前没有已加载的方案，先加载一个 .vpp。",
                    "保存方案", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string vppDir = Path.Combine(Application.StartupPath, "vpps");

            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = "保存VPP方案";
                dlg.Filter = "VPP文件|*.vpp";
                dlg.InitialDirectory = Directory.Exists(vppDir)
                    ? vppDir
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dlg.FileName = string.IsNullOrEmpty(_vppPath) ? "Sort_A001.vpp" : Path.GetFileName(_vppPath);
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                btnSaveVpp.Enabled = false;
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    // 按加载时的原始形态存回：JobManager → JobManager，Job → Job，ToolBlock/ToolGroup → 本身
                    object toSave = (_jobManager != null) ? (object)_jobManager
                                  : (_job != null) ? (object)_job
                                  : (object)_toolBlock;
                    CogSerializer.SaveObjectToFile(toSave, dlg.FileName);
                    ApplySavedVppPath(dlg.FileName);
                    lblVppPath.Text = "当前VPP路径：" + dlg.FileName;
                    MessageBox.Show(this, "已保存：\n" + dlg.FileName,
                        "保存方案", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "保存失败：\n" + ex.Message,
                        "保存方案", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                    btnSaveVpp.Enabled = true;
                }
            }
        }

        /// <summary>
        /// 「保存方案」成功后，把**生产该加载哪个方案**指向刚存下的那个文件（2026-09-20 补，联动缺口 1）。
        ///
        /// `VisionScheme.VppPath` 是主运行页每次「开始检测」重新加载方案的依据（主运行页方案.md Q3=A），
        /// 而在补这个方法之前，它**全程序只有一处写入** —— 就是本页「加载VPP」那里。
        /// 于是：训练 →「保存方案」另存为新文件名 → 本页标签显示新路径，生产却仍然加载**旧那个文件**。
        /// 属于"界面和事实不一致"里最坏的一种：改也改了、存也存了，就是没生效。
        /// （覆盖保存原路径时看不出问题，所以这个坑只在"另存为"时露头。）
        ///
        /// 独立成一个方法而不是直接写在按钮里：`btnSaveVpp_Click` 中间有模态 `SaveFileDialog`，
        /// 探针没法自动化点到它；拆出来后这段"存盘后的簿记"可以被直接调用并断言（见本页实测记录）。
        /// </summary>
        private void ApplySavedVppPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            Services.VisionScheme.Shared.VppPath = path;   // 生产下次加载它
            _vppPath = path;                               // 下次「保存方案」默认就是这个新名字
        }

        // 说明：PMA 模板训练的按钮事件（btnGrabTpl_Click / btnTrain_Click / btnSaveTpl_Click）
        // 实现在 VisionConfigView.Pma.cs 里，本文件不再保留空存根（同名会 CS0111 冲突）。
    }
}
