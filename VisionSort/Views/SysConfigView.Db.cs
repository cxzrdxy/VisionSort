using System;
using System.Windows.Forms;
using VisionSort.Services;

namespace VisionSort.Views
{
    /// <summary>
    /// 系统设置页 · 数据库配置（grpDb）——「数据库方案.md」第 2 步。
    ///
    /// 三件事：
    ///   ① txtConnStr 失焦：轻校验（非空 + 含 Server=）后写进 <see cref="DbSettings.Shared"/>，
    ///      非法值按本页惯例警告并恢复旧值；
    ///   ② btnTestConn：连接 → 服务端版本 → 库/表在不在 → 行数，全部中文提示；
    ///   ③ btnInitDb：CREATE TABLE IF NOT EXISTS（幂等，可反复点）。
    ///
    /// 与其它组的差别：这两个按钮要等 I/O，所以是 async void 事件处理，期间把按钮置灰改名，
    /// 避免操作工连点（连接超时最长 3 秒、建表最长 5 秒）。
    /// 口令按 Q4=A 明文存在 sysparam.json；**任何提示都用打码串**（DbSettings.MaskPassword）。
    /// </summary>
    public partial class SysConfigView
    {
        private void txtConnStr_Leave(object sender, EventArgs e)
        {
            string value = txtConnStr.Text.Trim();
            if (value.Length == 0)
            {
                WarnAndRevert(txtConnStr, DbSettings.Shared.ConnectionString, "连接字符串不能为空。");
                return;
            }
            if (value.IndexOf("Server=", StringComparison.OrdinalIgnoreCase) < 0)
            {
                WarnAndRevert(txtConnStr, DbSettings.Shared.ConnectionString,
                    "连接字符串里没有 Server=…。\r\n\r\n示例：\r\n" +
                    "Server=127.0.0.1;Port=3306;Database=vision_sort;Uid=vision;Pwd=口令;CharSet=utf8mb4;SslMode=None;AllowPublicKeyRetrieval=True");
                return;
            }
            DbSettings.Shared.ConnectionString = value;
        }

        private async void btnTestConn_Click(object sender, EventArgs e)
        {
            this.ActiveControl = null;   // 逼出 txtConnStr 的 Leave，保证用框里的最新值测试
            string oldText = btnTestConn.Text;
            btnTestConn.Enabled = false;
            btnTestConn.Text = "测试中…";
            try
            {
                DbProbeResult result = await ResultRepository.Shared.TestConnectionAsync();
                MessageBox.Show(this, result.Message,
                    result.Ok ? "数据库 · 测试连接" : "数据库 · 测试连接失败",
                    MessageBoxButtons.OK,
                    result.Ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "测试连接异常：\r\n" + ResultRepository.Describe(ex, DbSettings.Shared.ConnectionString),
                    "数据库", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                btnTestConn.Text = oldText;
                btnTestConn.Enabled = true;
            }
        }

        private async void btnInitDb_Click(object sender, EventArgs e)
        {
            this.ActiveControl = null;
            string oldText = btnInitDb.Text;
            btnInitDb.Enabled = false;
            btnInitDb.Text = "建表中…";
            try
            {
                await ResultRepository.Shared.EnsureSchemaAsync();
                DbProbeResult result = await ResultRepository.Shared.TestConnectionAsync();
                MessageBox.Show(this, "初始化建表完成。\r\n\r\n" + result.Message,
                    "数据库", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "初始化建表失败：\r\n" + ResultRepository.Describe(ex, DbSettings.Shared.ConnectionString),
                    "数据库", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                btnInitDb.Text = oldText;
                btnInitDb.Enabled = true;
            }
        }

        /// <summary>把 Shared 里的数据库配置推给界面（OnLoad 时调用）。</summary>
        private void PushDbToUi()
        {
            txtConnStr.Text = DbSettings.Shared.ConnectionString;

            int index = cmbDbType.Items.IndexOf(DbSettings.Shared.DbType);
            cmbDbType.SelectedIndex = index >= 0 ? index : (cmbDbType.Items.Count > 0 ? 0 : -1);
        }

        /// <summary>
        /// 「保存参数 / 加载参数」收尾时调用：把界面值收进 Shared。
        /// 这里**不弹窗**（沿用其它组的做法：加载/保存过程中不打断），非法值保留旧值。
        /// </summary>
        private void PushDbUiToShared()
        {
            string value = txtConnStr.Text.Trim();
            if (value.Length > 0 && value.IndexOf("Server=", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                DbSettings.Shared.ConnectionString = value;
            }
            if (cmbDbType.SelectedItem != null) DbSettings.Shared.DbType = cmbDbType.SelectedItem.ToString();

            PushDbToUi();
        }
    }
}
