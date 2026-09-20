using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisionSort.Services;

namespace VisionSort.Views
{
    /// <summary>
    /// 数据日志页 · 导出 Excel（「数据库方案.md」Q10=A：手写 xlsx，零依赖）。
    ///
    /// 三条硬要求（对齐 Designer 里那句"万级记录后台导出，不锁界面"）：
    ///   ① 后台线程跑，界面全程可点；
    ///   ② 流式：游标读一行 → 写一行 → 丢一行，万级记录内存不涨；
    ///   ③ 导出范围＝**当前查询条件命中的全部记录**（不受分页限制）。
    ///
    /// 进度显示复用 lblExportTip（原本文案就是"万级记录后台导出，不锁界面"），不新增控件；
    /// 取消按钮没做——草稿布局里没有这个控件，加一个按钮要动 Designer，本轮先不加（方案 §十四 记为偏差）。
    /// </summary>
    public partial class DataLogView
    {
        private static readonly string[] ExportHeaders =
        {
            "时间", "工件编号", "配方", "检测结果", "匹配分数", "图像路径", "备注"
        };

        private static readonly double[] ExportWidths = { 24, 16, 8, 10, 12, 42, 30 };

        private const string ExportTipIdle = "万级记录后台导出，不锁界面";

        private long _exportRows;

        private async void btnExport_Click(object sender, EventArgs e)
        {
            DateTime from = dtpFrom.Value.Date;
            DateTime to = dtpTo.Value.Date.AddDays(1).AddMilliseconds(-1);
            if (to < from) to = from.AddDays(1).AddMilliseconds(-1);
            string filter = cmbResult.SelectedItem == null ? "全部" : cmbResult.SelectedItem.ToString();

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "导出检测日志";
                dialog.Filter = "Excel 工作簿 (*.xlsx)|*.xlsx";
                dialog.FileName = "检测日志_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xlsx";
                dialog.InitialDirectory = SaveDialogDefaultFolder();
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                string path = dialog.FileName;
                btnExport.Enabled = false;
                _exportRows = 0;
                lblExportTip.Text = "正在导出…";

                try
                {
                    long rows = await Task.Run(async () =>
                    {
                        long count = 0;
                        using (XlsxWriter writer = new XlsxWriter(path, ExportHeaders, ExportWidths))
                        {
                            await ResultRepository.Shared.WriteRowsAsync(from, to, filter, record =>
                            {
                                writer.WriteRow(
                                    record.Time,
                                    record.WorkpieceNo,
                                    record.RecipeID,
                                    record.Result,
                                    record.Score.HasValue ? (object)record.Score.Value : null,
                                    record.ImagePath,
                                    record.Note);

                                count++;
                                if (count % 500 == 0) ReportExportProgress(count);
                            });
                        }
                        return count;
                    });

                    lblExportTip.Text = "已导出 " + rows.ToString("N0", CultureInfo.InvariantCulture) + " 条";
                    MessageBox.Show(this,
                        "导出完成。\r\n\r\n共 " + rows.ToString("N0", CultureInfo.InvariantCulture) + " 条\r\n" + path,
                        "数据日志", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    lblExportTip.Text = ExportTipIdle;
                    MessageBox.Show(this,
                        "导出失败：\r\n" + ResultRepository.Describe(ex, DbSettings.Shared.ConnectionString)
                        + "\r\n\r\n（导出到一半的文件已留下，删掉即可：\r\n" + path + "）",
                        "数据日志", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                finally
                {
                    btnExport.Enabled = true;
                    if (lblExportTip.Text == "正在导出…") lblExportTip.Text = ExportTipIdle;
                }
            }
        }

        /// <summary>后台线程 → 界面线程报进度（每 500 行一次，避免频繁 Invoke 拖慢导出）。</summary>
        private void ReportExportProgress(long count)
        {
            _exportRows = count;
            try
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    BeginInvoke(new Action(() =>
                    {
                        if (!IsDisposed) lblExportTip.Text = "正在导出… " + _exportRows.ToString("N0", CultureInfo.InvariantCulture) + " 条";
                    }));
                }
            }
            catch (InvalidOperationException)
            {
                // 窗体正在销毁：忽略即可，导出本身不受影响
            }
        }

        /// <summary>
        /// 保存对话框的默认目录：桌面。
        /// （图像保存根目录没有对外暴露"当前值"——图像配置按你的决定不落盘、只活在视觉配置页的控件里，
        ///   等主运行页开工时把它提成共享设置，这里再改成默认落到图像根目录。见方案 §十四 偏差记录。）
        /// </summary>
        private static string SaveDialogDefaultFolder()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        }
    }
}
