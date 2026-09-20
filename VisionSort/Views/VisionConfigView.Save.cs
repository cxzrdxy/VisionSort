using System;
using System.IO;
using System.Windows.Forms;
using VisionSort.Services;

namespace VisionSort.Views
{
    public partial class VisionConfigView
    {
        private bool _saveUiUpdating;            // 程序改控件时抑制事件重入
        private string _lastValidSaveRoot = "";  // 内存里记的上一次校验通过的路径，供校验失败时回退

        /// <summary>启动时把界面默认路径记为“当前有效值”（不读任何配置文件）。</summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _lastValidSaveRoot = txtSavePath.Text.Trim();
            PushSaveToShared();
        }

        /// <summary>
        /// 把存图设置推进共享对象（主运行页存图时读它）。
        /// 图像配置**不落盘**（你的决定），所以这里只做进程内同步：
        /// 用户在本页改一次，主运行页立刻跟着变（与 Modbus 组"改动即时进 Shared"同套路）。
        /// </summary>
        private void PushSaveToShared()
        {
            SaveSettings.Shared.RootPath = txtSavePath.Text.Trim();
            SaveSettings.Shared.Format = cmbImgFmt.Text.Trim();
            SaveSettings.Shared.SaveOk = chkSaveOK.Checked;
            SaveSettings.Shared.SaveNg = chkSaveNG.Checked;
        }

        // ------------------------------------------------------------------
        // 界面事件
        // ------------------------------------------------------------------

        /// <summary>『选择路径』：选目录 → 校验（不存在可创建 / 可写）→ 生效。</summary>
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "选择图像保存根目录";
                dlg.ShowNewFolderButton = true;
                dlg.SelectedPath = Directory.Exists(txtSavePath.Text.Trim())
                    ? txtSavePath.Text.Trim()
                    : Application.StartupPath;

                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                if (!EnsureFolderUsable(dlg.SelectedPath)) return;   // 内部已提示

                txtSavePath.Text = dlg.SelectedPath;
                _lastValidSaveRoot = dlg.SelectedPath;
            }
        }

        /// <summary>手输路径失焦时也校验一次；不通过就回退到上次校验通过的路径。</summary>
        private void txtSavePath_Leave(object sender, EventArgs e)
        {
            if (_saveUiUpdating) return;

            string path = txtSavePath.Text.Trim();
            if (string.Equals(path, _lastValidSaveRoot, StringComparison.OrdinalIgnoreCase)) return;  // 没改就不打扰

            if (EnsureFolderUsable(path))
            {
                _lastValidSaveRoot = path;
                return;
            }
            if (_lastValidSaveRoot.Length > 0) txtSavePath.Text = _lastValidSaveRoot;   // 保留原值
        }

        /// <summary>OK/NG 至少保留一种，否则提示并恢复刚取消的那项。</summary>
        private void chkSave_CheckedChanged(object sender, EventArgs e)
        {
            if (_saveUiUpdating) return;
            if (chkSaveOK.Checked || chkSaveNG.Checked) { PushSaveToShared(); return; }

            MessageBox.Show(this, "OK 图与 NG 图至少要保存一种，否则不会存图。",
                "图像保存配置", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            _saveUiUpdating = true;
            try { ((CheckBox)sender).Checked = true; }
            finally { _saveUiUpdating = false; }
        }

        /// <summary>格式下拉改了：即时同步给主运行页（存图时按它选扩展名）。</summary>
        private void cmbImgFmt_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_saveUiUpdating) return;
            PushSaveToShared();
        }

        /// <summary>目录可用性：不存在可询问创建；不可写则提示并返回 false。</summary>
        private bool EnsureFolderUsable(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show(this, "请先填写图像保存目录。", "图像保存配置",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            try
            {
                if (!Directory.Exists(path))
                {
                    DialogResult answer = MessageBox.Show(this,
                        "目录不存在：\r\n" + path + "\r\n\r\n是否创建？", "图像保存配置",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (answer != DialogResult.Yes) return false;
                    Directory.CreateDirectory(path);
                }

                string probe = Path.Combine(path, ".writetest_" + Guid.NewGuid().ToString("N").Substring(0, 8));
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "该目录不可用（无法写入）：\r\n" + path + "\r\n\r\n" + ex.Message,
                    "图像保存配置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }
    }
}


