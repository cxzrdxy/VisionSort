using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisionSort.Services;

namespace VisionSort.Views
{
    /// <summary>
    /// 数据日志页 · 查询与统计（「数据库方案.md」第 3 步，Q9/Q11）。
    ///
    /// 设计要点：
    ///   · 统计四卡与列表**用同一个 WHERE**（同一次查询的日期范围+结果过滤），卡片永远和表格对得上；
    ///   · 倒序分页（ts DESC, id DESC），每页 200 行，翻页靠运行时生成的分页条；
    ///   · OnLoad 首次查询**失败不弹窗**（开机时数据库没起来不该砸操作工一脸），只把原因写进分页条；
    ///     点「查询日志」是人的主动操作，失败才弹窗。
    ///
    /// 分页条是**运行时代码构建**的（BuildPager），没有加进 Designer：
    /// ① Designer 是设计与机械臂那轮共用的文件，少动一处少一处冲突；② 分页条本身没有设计期价值。
    /// </summary>
    public partial class DataLogView
    {
        private const int PageSize = 200;

        private int _page = 1;
        private long _totalCount;
        private bool _wired;
        private bool _loading;

        private FlowLayoutPanel _pnlPager;
        private Button _btnPrev;
        private Button _btnNext;
        private Label _lblPage;

        /// <summary>把 Designer 里没绑事件的按钮接上，并建分页条。构造期调用一次。</summary>
        private void WireLogEvents()
        {
            if (_wired) return;
            _wired = true;

            btnQuery.Click += btnQuery_Click;
            btnClear.Click += btnClear_Click;
            btnExport.Click += btnExport_Click;

            dgvLog.CellFormatting += dgvLog_CellFormatting;
            dgvLog.DataBindingComplete += (s, e) => FormatColumns();

            BuildPager();
        }

        private void BuildPager()
        {
            _btnPrev = new Button { Text = "上一页", Width = 90, Height = 30, Enabled = false };
            _btnNext = new Button { Text = "下一页", Width = 90, Height = 30, Enabled = false };
            _lblPage = new Label { Text = "未查询", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };

            _btnPrev.Click += async (s, e) => await ReloadAsync(_page - 1, true);
            _btnNext.Click += async (s, e) => await ReloadAsync(_page + 1, true);

            _pnlPager = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(6, 6, 6, 6)
            };
            _lblPage.Margin = new Padding(12, 8, 12, 0);
            _pnlPager.Controls.Add(_btnPrev);
            _pnlPager.Controls.Add(_btnNext);
            _pnlPager.Controls.Add(_lblPage);

            // dgvLog 是 Fill，分页条后加 → 布局引擎先摆 Bottom 再让 Fill 填剩下的（实测验证见方案 §十四）
            grpTable.Controls.Add(_pnlPager);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            dtpFrom.Value = DateTime.Today;
            dtpTo.Value = DateTime.Today;
            cmbResult.SelectedItem = "全部";
            if (cmbResult.SelectedIndex < 0 && cmbResult.Items.Count > 0) cmbResult.SelectedIndex = 0;

            _ = ReloadAsync(1, false);   // 开机静默查一次（OnLoad 不能 async，这里显式丢弃 Task）
        }

        private async void btnQuery_Click(object sender, EventArgs e)
        {
            await ReloadAsync(1, true);
        }

        private async void btnClear_Click(object sender, EventArgs e)
        {
            dtpFrom.Value = DateTime.Today;
            dtpTo.Value = DateTime.Today;
            cmbResult.SelectedItem = "全部";
            if (cmbResult.SelectedIndex < 0 && cmbResult.Items.Count > 0) cmbResult.SelectedIndex = 0;
            await ReloadAsync(1, false);
        }

        /// <summary>按当前条件取某一页。<paramref name="showError"/>＝失败时弹窗（仅人主动查询时为 true）。</summary>
        private async Task ReloadAsync(int page, bool showError)
        {
            if (_loading) return;
            _loading = true;
            btnQuery.Enabled = false;
            btnClear.Enabled = false;

            try
            {
                if (page < 1) page = 1;

                // 日期取"整天"：dtpTo 选 9/17 就是到 9/17 23:59:59.999，否则当天数据永远查不到
                DateTime from = dtpFrom.Value.Date;
                DateTime to = dtpTo.Value.Date.AddDays(1).AddMilliseconds(-1);
                if (to < from) to = from.AddDays(1).AddMilliseconds(-1);

                string filter = cmbResult.SelectedItem == null ? "全部" : cmbResult.SelectedItem.ToString();

                ResultRepository repo = ResultRepository.Shared;
                List<ResultRecord> rows = await repo.QueryAsync(from, to, filter, page, PageSize);
                long total = await repo.CountAsync(from, to, filter);
                LogSummary summary = await repo.SummaryAsync(from, to, filter);

                long pages = total == 0 ? 1 : (total + PageSize - 1) / PageSize;
                if (page > pages)   // 越界（比如删了数据）→ 回退到最后一页
                {
                    _loading = false;
                    await ReloadAsync((int)pages, showError);
                    return;
                }

                _page = page;
                _totalCount = total;

                dgvLog.DataSource = rows;
                FormatColumns();

                lblSumTotal.Text = "总检测：" + summary.Total;
                lblSumOK.Text = "OK：" + summary.Ok;
                lblSumNG.Text = "NG：" + summary.Ng;
                lblSumRate.Text = "合格率：" + summary.Rate.ToString("P2");

                _btnPrev.Enabled = _page > 1;
                _btnNext.Enabled = _page < pages;
                _lblPage.Text = string.Format("第 {0}/{1} 页 · 共 {2} 条 · 每页 {3} 行",
                    _page, pages, total, PageSize);
            }
            catch (Exception ex)
            {
                string message = ResultRepository.Describe(ex, DbSettings.Shared.ConnectionString);
                dgvLog.DataSource = null;
                lblSumTotal.Text = "总检测：0";
                lblSumOK.Text = "OK：0";
                lblSumNG.Text = "NG：0";
                lblSumRate.Text = "合格率：0.00%";
                _btnPrev.Enabled = false;
                _btnNext.Enabled = false;
                _lblPage.Text = "查询失败";
                if (showError)
                {
                    MessageBox.Show(this, "查询日志失败：\r\n" + message, "数据日志",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    _lblPage.Text = "未连接数据库（" + message + "）";
                }
            }
            finally
            {
                _loading = false;
                btnQuery.Enabled = true;
                btnClear.Enabled = true;
            }
        }

        /// <summary>绑定完列宽与格式（Score 四位小数、时间带毫秒）。</summary>
        private void FormatColumns()
        {
            if (dgvLog.Columns.Count == 0) return;
            dgvLog.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            if (dgvLog.Columns["colScore"] != null)
                dgvLog.Columns["colScore"].DefaultCellStyle.Format = "0.0000";
            if (dgvLog.Columns["colTime"] != null)
                dgvLog.Columns["colTime"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm:ss.fff";
            if (dgvLog.Columns["colPath"] != null)
                dgvLog.Columns["colPath"].FillWeight = 160;
            if (dgvLog.Columns["colNote"] != null)
                dgvLog.Columns["colNote"].FillWeight = 120;
        }

        /// <summary>OK 绿 / NG 红（草稿里就是这么标的，扫一眼就能看出坏件分布）。</summary>
        private void dgvLog_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || dgvLog.Columns[e.ColumnIndex].Name != "colResult") return;

            string value = e.Value as string;
            if (value == "OK")
            {
                e.CellStyle.ForeColor = Color.ForestGreen;
                e.CellStyle.SelectionForeColor = Color.ForestGreen;
            }
            else if (value == "NG")
            {
                e.CellStyle.ForeColor = Color.Firebrick;
                e.CellStyle.SelectionForeColor = Color.Firebrick;
            }
        }
    }
}
