using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DobotArm.Client;
using VisionSort.Services;

namespace VisionSort.Views
{
    /// <summary>
    /// 设备调试页 · 九点标定输入区（grpCalib）（「九点标定联机分拣方案.md」§4.5 / §4.10）。
    ///
    /// 分工（Q1=丙 拍板）：九个点在 **VisionPro 的 CogCalibNPointToNPointTool 里人工点**，
    /// 像素坐标由人抄/粘到这里；机械臂坐标**一律由程序读**（`GetPoseAsync`），不许手抄 ——
    /// 手抄错一个数整张标定就歪，而程序读是免费的。
    ///
    /// 为什么控件用代码建、不写进 .Designer.cs：
    /// ① 这里是"表格 + 一排按钮 + 几个框"的机械布局，设计器反而要维护一堆样板；
    /// ② 停靠顺序（Fill 必须先加）这类事写在代码里能带注释解释；
    /// ③ 与 `MainRunView.Run.cs` 运行时建 `CogRecordDisplay`、`CalibHelpForm` 手写搭窗同一个理由。
    /// Designer 里只留一个空的 `grpCalib` 占位。
    ///
    /// 尺寸按**运行时实测**摆（MainForm 1280x774 → 本组 798x286，客户区 792x279）：
    /// 左边表格、右边三行按钮 + 拟合指标 + 抓取几何 + 状态，最底边 272 &lt; 279。
    /// </summary>
    public partial class DeviceDebugView
    {
        private const int CalibDefaultRows = 9;

        private DataGridView _calibGrid;
        private Button _calibReadPose, _calibAddRow, _calibDelRow, _calibClearRows, _calibPaste;
        private Button _calibCalc, _calibSave, _calibReload, _calibCheckOrigin, _calibHelp;
        private Label _calibMetrics, _calibStatus, _calibGeomTitle;
        private Label _calibGeomZLabel, _calibGeomXLabel, _calibGeomYLabel, _calibGeomTilde1, _calibGeomTilde2;
        private Label _calibGeomStandbyLabel, _calibGeomSxLabel, _calibGeomSyLabel;
        private Label _calibGeomPlaceLabel, _calibGeomPxLabel, _calibGeomPyLabel, _calibGeomPzLabel;
        private TextBox _calibPickZ, _calibMinX, _calibMaxX, _calibMinY, _calibMaxY;
        private TextBox _calibStandbyX, _calibStandbyY, _calibPlaceX, _calibPlaceY, _calibPlaceZ;

        private bool _calibReady;     // InitCalibPanel 跑完前，控件事件不动作（Designer 可能提前触发）
        private bool _calibBusy;      // 防重入（读位姿/核对要等机械臂）

        // ------------------------------------------------------------------
        // 初始化
        // ------------------------------------------------------------------

        /// <summary>由 DeviceDebugView.Conveyor.cs 的 OnLoad 调用（与 InitRobotPanel 并列）。</summary>
        private void InitCalibPanel()
        {
            BuildCalibControls();

            _calibReady = true;

            // 先把磁盘上的标定读进来（读不到不算错误，只是"还没标定"）
            string loadMessage;
            CalibrationService.Shared.EnsureLoaded(out loadMessage);

            RefreshCalibGridFromService();
            RefreshCalibGeometryFromSettings();
            RefreshCalibMetrics();
            RefreshCalibStatus(loadMessage);
            RefreshCalibUi();
        }

        /// <summary>
        /// 按"生产在不在跑"决定标定区哪些东西可动（Q13 互锁，见 DeviceDebugView.Lock.cs）。
        ///
        /// 生产在跑时**除了『操作指引』全部置灰**，理由：
        ///   · 『读当前位姿』要碰机械臂；
        ///   · 『计算』『保存』『重新载入』会改生产**每一轮都要读**的那份标定（`RobotPickPlace` 每轮读单例）；
        ///   · 四个几何框会改 `RobotArmSettings`，那正是分拣在用的 Z 与工作范围；
        ///   · 『核对 PMA 原点』会 `VisionScheme.Load()` 重新加载方案，等于生产跑到一半换掉工具对象。
        /// 只有『操作指引』是纯只读，留着 —— 生产时人还是可能想翻一眼。
        /// </summary>
        private void RefreshCalibUi()
        {
            if (!_calibReady) return;

            bool free = !_productionLock;

            _calibReadPose.Enabled = free;
            _calibAddRow.Enabled = free;
            _calibDelRow.Enabled = free;
            _calibClearRows.Enabled = free;
            _calibPaste.Enabled = free;
            _calibCalc.Enabled = free;
            _calibSave.Enabled = free;
            _calibReload.Enabled = free;
            _calibCheckOrigin.Enabled = free;
            // _calibHelp 永远可用（只读）

            _calibPickZ.Enabled = free;
            _calibMinX.Enabled = free;
            _calibMaxX.Enabled = free;
            _calibMinY.Enabled = free;
            _calibMaxY.Enabled = free;
            _calibStandbyX.Enabled = free;
            _calibStandbyY.Enabled = free;
            _calibPlaceX.Enabled = free;
            _calibPlaceY.Enabled = free;
            _calibPlaceZ.Enabled = free;
        }

        private void BuildCalibControls()
        {
            // ---- 左：点对表格 ----
            _calibGrid = new DataGridView();
            _calibGrid.Location = new Point(8, 22);
            _calibGrid.Size = new Size(374, 250);
            _calibGrid.Name = "calibGrid";
            _calibGrid.AllowUserToAddRows = false;
            _calibGrid.AllowUserToDeleteRows = false;
            _calibGrid.AllowUserToResizeRows = false;
            _calibGrid.RowHeadersVisible = false;
            _calibGrid.MultiSelect = false;
            _calibGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            _calibGrid.EditMode = DataGridViewEditMode.EditOnEnter;
            _calibGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _calibGrid.RowTemplate.Height = 20;

            AddCalibColumn("no", "序号", 38, true);
            AddCalibColumn("px", "像素X", 78, false);
            AddCalibColumn("py", "像素Y", 78, false);
            AddCalibColumn("mx", "机械臂X", 78, false);
            AddCalibColumn("my", "机械臂Y", 78, false);

            grpCalib.Controls.Add(_calibGrid);

            // ---- 右：按钮（三行）----
            int x = 394;
            _calibReadPose = AddCalibButton("读当前位姿 → 选中行", x, 22, 140, CalibReadPose_Click);
            _calibAddRow = AddCalibButton("加行", x + 146, 22, 72, CalibAddRow_Click);
            _calibDelRow = AddCalibButton("删末行", x + 224, 22, 78, CalibDelRow_Click);
            _calibClearRows = AddCalibButton("清空", x + 308, 22, 84, CalibClearRows_Click);

            _calibPaste = AddCalibButton("粘贴像素列", x, 56, 104, CalibPaste_Click);
            _calibCalc = AddCalibButton("计算", x + 110, 56, 76, CalibCalc_Click);
            _calibSave = AddCalibButton("保存", x + 192, 56, 76, CalibSave_Click);
            _calibReload = AddCalibButton("重新载入", x + 274, 56, 118, CalibReload_Click);

            _calibCheckOrigin = AddCalibButton("核对 PMA 原点", x, 90, 150, CalibCheckOrigin_Click);
            _calibHelp = AddCalibButton("操作指引", x + 156, 90, 110, CalibHelp_Click);

            // ---- 拟合指标 ----
            _calibMetrics = new Label();
            _calibMetrics.Location = new Point(x, 124);
            _calibMetrics.Size = new Size(392, 20);
            _calibMetrics.Name = "calibMetrics";
            _calibMetrics.Text = "拟合：－";
            grpCalib.Controls.Add(_calibMetrics);

            // ---- 抓取几何（两行；与标定一起存 calib.json）----
            // 行 1：抓取高度 Z + 工作范围（标定算出来的点必须落在这里面，否则拒动）
            // 行 2：待机位 X/Y + 放料点 X/Y/Z（这几个是"换算之外"的固定几何量，以前只能改源码重编译）
            _calibGeomTitle = AddCalibLabel("抓取几何", x, 148, 64, false);
            _calibGeomZLabel = AddCalibLabel("Z", x + 66, 150, 14, false);
            _calibPickZ = AddCalibGeomBox(x + 80, 148);
            _calibGeomXLabel = AddCalibLabel("X", x + 130, 150, 14, false);
            _calibMinX = AddCalibGeomBox(x + 144, 148);
            _calibGeomTilde1 = AddCalibLabel("~", x + 190, 150, 10, false);
            _calibMaxX = AddCalibGeomBox(x + 200, 148);
            _calibGeomYLabel = AddCalibLabel("Y", x + 250, 150, 14, false);
            _calibMinY = AddCalibGeomBox(x + 264, 148);
            _calibGeomTilde2 = AddCalibLabel("~", x + 310, 150, 10, false);
            _calibMaxY = AddCalibGeomBox(x + 320, 148);

            _calibGeomStandbyLabel = AddCalibLabel("待机", x, 176, 36, false);
            _calibGeomSxLabel = AddCalibLabel("X", x + 38, 178, 12, false);
            _calibStandbyX = AddCalibGeomBox(x + 50, 176, 44);
            _calibGeomSyLabel = AddCalibLabel("Y", x + 96, 178, 12, false);
            _calibStandbyY = AddCalibGeomBox(x + 108, 176, 44);
            _calibGeomPlaceLabel = AddCalibLabel("放料", x + 162, 176, 36, false);
            _calibGeomPxLabel = AddCalibLabel("X", x + 200, 178, 12, false);
            _calibPlaceX = AddCalibGeomBox(x + 212, 176, 44);
            _calibGeomPyLabel = AddCalibLabel("Y", x + 258, 178, 12, false);
            _calibPlaceY = AddCalibGeomBox(x + 270, 176, 44);
            _calibGeomPzLabel = AddCalibLabel("Z", x + 316, 178, 12, false);
            _calibPlaceZ = AddCalibGeomBox(x + 328, 176, 44);

            // ---- 状态 ----
            _calibStatus = new Label();
            _calibStatus.Location = new Point(x, 204);
            _calibStatus.Size = new Size(392, 68);
            _calibStatus.Name = "calibStatus";
            _calibStatus.Text = string.Empty;
            grpCalib.Controls.Add(_calibStatus);
        }

        private void AddCalibColumn(string name, string header, int width, bool readOnly)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.Name = name;
            column.HeaderText = header;
            column.Width = width;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            column.ReadOnly = readOnly;
            if (readOnly) column.DefaultCellStyle.BackColor = SystemColors.Control;
            _calibGrid.Columns.Add(column);
        }

        private Button AddCalibButton(string text, int x, int y, int width, EventHandler onClick)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(width, 28);
            button.UseVisualStyleBackColor = true;
            button.Click += onClick;
            grpCalib.Controls.Add(button);
            return button;
        }

        private Label AddCalibLabel(string text, int x, int y, int width, bool bold)
        {
            Label label = new Label();
            label.Text = text;
            label.Location = new Point(x, y);
            label.Size = new Size(width, 20);
            if (bold) label.Font = new Font(label.Font, FontStyle.Bold);
            grpCalib.Controls.Add(label);
            return label;
        }

        private TextBox AddCalibGeomBox(int x, int y)
        {
            return AddCalibGeomBox(x, y, 46);
        }

        private TextBox AddCalibGeomBox(int x, int y, int width)
        {
            TextBox box = new TextBox();
            box.Location = new Point(x, y);
            box.Size = new Size(width, 23);
            box.TextAlign = HorizontalAlignment.Right;
            box.Leave += CalibGeometryBox_Leave;
            box.KeyDown += CalibGeometryBox_KeyDown;
            grpCalib.Controls.Add(box);
            return box;
        }

        // ------------------------------------------------------------------
        // 表格读写
        // ------------------------------------------------------------------

        private void RefreshCalibGridFromService()
        {
            List<CalibPair> pairs = CalibrationService.Shared.SnapshotPairs();
            _calibGrid.Rows.Clear();
            for (int i = 0; i < pairs.Count; i++)
                AddCalibGridRow(pairs[i].PixelX, pairs[i].PixelY, pairs[i].RobotX, pairs[i].RobotY);

            // 不够 9 行就补空行（现场习惯是先把九个格子摆出来）
            while (_calibGrid.Rows.Count < CalibDefaultRows) AddCalibGridRow(null, null, null, null);
        }

        private void AddCalibGridRow(double? px, double? py, double? mx, double? my)
        {
            int index = _calibGrid.Rows.Add();
            DataGridViewRow row = _calibGrid.Rows[index];
            row.Cells["no"].Value = (index + 1).ToString(CultureInfo.InvariantCulture);
            row.Cells["px"].Value = Number(px);
            row.Cells["py"].Value = Number(py);
            row.Cells["mx"].Value = Number(mx);
            row.Cells["my"].Value = Number(my);
        }

        private static string Number(double? value)
        {
            if (!value.HasValue) return string.Empty;
            return value.Value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        private void RenumberCalibRows()
        {
            for (int i = 0; i < _calibGrid.Rows.Count; i++)
                _calibGrid.Rows[i].Cells["no"].Value = (i + 1).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>把表格里的四个数读出来。整行空 ＝ 跳过；半行填了 ＝ 报错（不猜）。</summary>
        private bool TryReadCalibPairs(out List<CalibPair> pairs, out string message)
        {
            pairs = new List<CalibPair>();

            for (int i = 0; i < _calibGrid.Rows.Count; i++)
            {
                DataGridViewRow row = _calibGrid.Rows[i];
                if (row.IsNewRow) continue;

                string[] texts = new string[4];
                texts[0] = Cell(row, "px");
                texts[1] = Cell(row, "py");
                texts[2] = Cell(row, "mx");
                texts[3] = Cell(row, "my");

                int filled = 0;
                foreach (string text in texts) if (text.Length > 0) filled++;
                if (filled == 0) continue;

                if (filled < 4)
                {
                    message = "第 " + (i + 1) + " 行只填了一部分：像素X/Y 和机械臂X/Y 要么四个都填，"
                        + "要么整行留空（半行没法用）。";
                    return false;
                }

                double[] values = new double[4];
                string[] names = { "像素X", "像素Y", "机械臂X", "机械臂Y" };
                for (int k = 0; k < 4; k++)
                {
                    if (!double.TryParse(texts[k], NumberStyles.Float, CultureInfo.InvariantCulture, out values[k])
                        && !double.TryParse(texts[k], out values[k]))
                    {
                        message = "第 " + (i + 1) + " 行的『" + names[k] + "』不是数字：" + texts[k];
                        return false;
                    }
                }

                pairs.Add(new CalibPair(values[0], values[1], values[2], values[3]));
            }

            if (pairs.Count == 0)
            {
                message = "表格里一对点都没有。请先把 VisionPro 里点出来的九个点的像素坐标填进『像素X/Y』列。";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private static string Cell(DataGridViewRow row, string column)
        {
            object value = row.Cells[column].Value;
            return value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture).Trim();
        }

        private int SelectedCalibRow()
        {
            if (_calibGrid.CurrentCell != null) return _calibGrid.CurrentCell.RowIndex;
            return -1;
        }

        /// <summary>把表格读进服务（先清空再逐条加，顺带报重复点）。</summary>
        private bool PushGridToService(out string note)
        {
            note = string.Empty;
            List<CalibPair> pairs;
            string message;
            if (!TryReadCalibPairs(out pairs, out message)) return false;

            CalibrationService.Shared.Clear();
            foreach (CalibPair pair in pairs)
            {
                string addMessage;
                if (!CalibrationService.Shared.AddPair(pair.PixelX, pair.PixelY, pair.RobotX, pair.RobotY, out addMessage))
                {
                    // 只有"重复点"会走到这里（数值非法在 TryReadCalibPairs 已拦下）
                    note = addMessage;
                    break;
                }
            }
            return true;
        }

        // ------------------------------------------------------------------
        // 按钮
        // ------------------------------------------------------------------

        /// <summary>
        /// 读当前位姿填到**选中行**。这一半的数字不允许手抄（手抄错一个数整张标定就歪）。
        /// 没有选中行时自动找第一行还没填机械臂坐标的空行。
        /// </summary>
        private async void CalibReadPose_Click(object sender, EventArgs e)
        {
            if (!_calibReady || _calibBusy) return;

            RobotArmService arm = RobotArmService.Shared;
            if (!arm.IsConnected)
            {
                MessageBox.Show(this, "机械臂未连接，请先点左上角『连接机械臂』。\r\n"
                    + "（标定时要一边点动机械臂、一边读位姿，所以两个区在同一页。）",
                    "九点标定", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int rowIndex = SelectedCalibRow();
            if (rowIndex < 0 || IsCalibRowFilled(rowIndex))
            {
                rowIndex = FirstEmptyCalibRow();
                if (rowIndex < 0)
                {
                    MessageBox.Show(this, "表格里没有空行了：先点『加行』再加一对点。", "九点标定",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            try
            {
                _calibBusy = true;
                Cursor.Current = Cursors.WaitCursor;

                ArmPose pose = await arm.Client.GetPoseAsync();

                _calibGrid.CurrentCell = _calibGrid.Rows[rowIndex].Cells["mx"];
                _calibGrid.Rows[rowIndex].Cells["mx"].Value = Number(pose.X);
                _calibGrid.Rows[rowIndex].Cells["my"].Value = Number(pose.Y);

                RefreshCalibStatus("已把当前位姿 X=" + Number(pose.X) + "，Y=" + Number(pose.Y)
                    + " 填到第 " + (rowIndex + 1) + " 行。");
            }
            catch (Exception ex)
            {
                RefreshCalibStatus("读位姿失败：" + ex.Message);
                MessageBox.Show(this, "读取机械臂位姿失败：\r\n" + ex.Message, "九点标定",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _calibBusy = false;
                Cursor.Current = Cursors.Default;
            }
        }

        private bool IsCalibRowFilled(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _calibGrid.Rows.Count) return false;
            DataGridViewRow row = _calibGrid.Rows[rowIndex];
            return Cell(row, "px").Length > 0 && Cell(row, "py").Length > 0
                && Cell(row, "mx").Length > 0 && Cell(row, "my").Length > 0;
        }

        private int FirstEmptyCalibRow()
        {
            for (int i = 0; i < _calibGrid.Rows.Count; i++)
            {
                DataGridViewRow row = _calibGrid.Rows[i];
                if (Cell(row, "px").Length == 0 && Cell(row, "mx").Length == 0) return i;
            }
            return -1;
        }

        private void CalibAddRow_Click(object sender, EventArgs e)
        {
            AddCalibGridRow(null, null, null, null);
            RenumberCalibRows();
        }

        private void CalibDelRow_Click(object sender, EventArgs e)
        {
            if (_calibGrid.Rows.Count == 0) return;
            _calibGrid.Rows.RemoveAt(_calibGrid.Rows.Count - 1);
            RenumberCalibRows();
        }

        private void CalibClearRows_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "清空表格里所有点对？（磁盘上的 calib.json 不会被删，只是重新填）",
                    "九点标定", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;

            _calibGrid.Rows.Clear();
            for (int i = 0; i < CalibDefaultRows; i++) AddCalibGridRow(null, null, null, null);
            CalibrationService.Shared.Clear();
            RefreshCalibMetrics();
            RefreshCalibStatus("表格已清空（磁盘上的 calib.json 还在，下次启动会重新载入）。");
        }

        /// <summary>
        /// 从剪贴板粘贴像素列。VisionPro 那个"点偶"表能不能复制我没有实测过，
        /// 所以这里做成"能粘就粘、不能粘就手填"，不依赖它。
        /// 只取前两个数当像素 X/Y；机械臂那两列一律用『读当前位姿』填。
        /// </summary>
        private void CalibPaste_Click(object sender, EventArgs e)
        {
            string text;
            try
            {
                if (!Clipboard.ContainsText())
                {
                    MessageBox.Show(this, "剪贴板里没有文本。\r\n"
                        + "如果 VisionPro 的表格复制不出来，就直接手填『像素X』『像素Y』两列 —— 手填 18 个数字是可以接受的，"
                        + "而机械臂那 18 个数字由程序读，不用抄。", "九点标定",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                text = Clipboard.GetText();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "读剪贴板失败：" + ex.Message, "九点标定",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            int row = SelectedCalibRow();
            if (row < 0) row = 0;

            int filledCount = 0;
            int fourColumnLines = 0;

            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;

                string[] parts = line.Split(new char[] { ',', '\t', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                double px, py;
                if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out px)) continue;
                if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out py)) continue;
                if (parts.Length >= 4) fourColumnLines++;

                while (row >= _calibGrid.Rows.Count) AddCalibGridRow(null, null, null, null);

                _calibGrid.Rows[row].Cells["px"].Value = Number(px);
                _calibGrid.Rows[row].Cells["py"].Value = Number(py);
                row++;
                filledCount++;
            }

            RenumberCalibRows();

            if (filledCount == 0)
            {
                RefreshCalibStatus("剪贴板里没认出坐标（每行要有两个数字：像素X 像素Y）。");
                return;
            }

            RefreshCalibStatus("已粘贴 " + filledCount + " 行像素坐标（从第 " + (SelectedCalibRow() < 0 ? 1 : SelectedCalibRow() + 1) + " 行起）。"
                + (fourColumnLines > 0
                    ? "有 " + fourColumnLines + " 行带了 4 个数，只取了前两个（像素）；机械臂坐标请用『读当前位姿』填。"
                    : string.Empty));
        }

        private void CalibCalc_Click(object sender, EventArgs e)
        {
            if (!_calibReady || _calibBusy) return;

            string message;
            bool ok = TryCalibrateFromGrid(out message);

            RefreshCalibStatus(message);
            MessageBox.Show(this, message, "九点标定",
                MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void CalibSave_Click(object sender, EventArgs e)
        {
            if (!_calibReady || _calibBusy) return;

            string message;
            bool ok = TrySaveFromGrid(out message);

            RefreshCalibStatus(message);
            MessageBox.Show(this, message, "九点标定",
                MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        /// <summary>
        /// 「计算」的全部逻辑：**强制核对模板原点** → 读表 → Calibrate。
        /// 单独抽出来是为了自测能直接调它（走按钮的话会弹 MessageBox，探针会卡住等人点）。
        /// </summary>
        private bool TryCalibrateFromGrid(out string message)
        {
            // N1 #3：这个坑错了不报错，只能靠强制检查
            string originMessage;
            if (!EnsurePatternOriginOk(out originMessage))
            {
                message = originMessage;
                return false;
            }

            string note;
            if (!PushGridToService(out note))
            {
                message = note;
                return false;
            }

            string calibrateMessage;
            bool ok = CalibrationService.Shared.Calibrate(out calibrateMessage);
            message = note.Length > 0 ? note + "\r\n" + calibrateMessage : calibrateMessage;
            RefreshCalibMetrics();   // 指标标签由这里刷，别让任何一条调用路径把它落下（自测抓到过一次）
            return ok;
        }

        /// <summary>「保存」的全部逻辑：核对 → 读表 → Calibrate → 写 calib.json。</summary>
        private bool TrySaveFromGrid(out string message)
        {
            string originMessage;
            if (!EnsurePatternOriginOk(out originMessage))
            {
                message = originMessage;
                return false;
            }

            string note;
            if (!PushGridToService(out note))
            {
                message = note;
                return false;
            }

            string calibrateMessage;
            if (!CalibrationService.Shared.Calibrate(out calibrateMessage))
            {
                message = note.Length > 0 ? note + "\r\n" + calibrateMessage : calibrateMessage;
                RefreshCalibMetrics();
                return false;
            }

            string saveMessage;
            bool ok = CalibrationService.Shared.Save(out saveMessage);
            message = note.Length > 0 ? note + "\r\n" + saveMessage : saveMessage;
            RefreshCalibMetrics();
            return ok;
        }

        private void CalibReload_Click(object sender, EventArgs e)
        {
            string message;
            bool ok = CalibrationService.Shared.Load(out message);
            RefreshCalibGridFromService();
            RefreshCalibGeometryFromSettings();
            RefreshCalibMetrics();
            RefreshCalibStatus(message);

            MessageBox.Show(this, message, "九点标定",
                MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void CalibCheckOrigin_Click(object sender, EventArgs e)
        {
            string message;
            bool ok = EnsurePatternOriginOk(out message);
            MessageBox.Show(this, message, "核对 PMA 原点",
                MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void CalibHelp_Click(object sender, EventArgs e)
        {
            CalibHelpForm.ShowHelp(this);
        }

        /// <summary>
        /// N1 #3：核对 PMA 模板原点。不通过就不许算/存 —— 因为"原点没设"这件事
        /// 在最终结果上只表现为"抓取整体偏移"，中途没有任何报错，事后极难查。
        /// </summary>
        private bool EnsurePatternOriginOk(out string message)
        {
            VisionScheme scheme = VisionScheme.Shared;

            // 方案没加载过就试着加载一次（用户在视觉配置页加载过 VPP 的话这里直接能读）
            if (!scheme.IsLoaded)
            {
                string loadMessage;
                if (!scheme.Load(out loadMessage))
                {
                    message = "核对不了模板原点：" + loadMessage
                        + "\r\n\r\n没有这一步就没法保证像素坐标是工件上的点，所以先不让你算/存标定。"
                        + "\r\n→ 请到「视觉配置」页加载一次方案（并训练好模板）再回来。";
                    RefreshCalibStatus(message);
                    return false;
                }
            }

            PatternOriginCheck check = scheme.CheckPatternOrigin();
            if (!check.Ok)
            {
                message = check.Message
                    + "\r\n\r\n（这一步是强制的：模板原点错的时候，标定数据看着全对、RMSE 也很小，"
                    + "但机械臂会整体抓偏，而且不报错。）";
                RefreshCalibStatus("★ 核对未通过：" + check.Message);
                return false;
            }

            message = "模板原点核对通过。" + check.Message;
            RefreshCalibStatus(message);
            return true;
        }

        // ------------------------------------------------------------------
        // 抓取几何（Z + 工作范围）：与 PickZ 一起存 calib.json
        // ------------------------------------------------------------------

        private void RefreshCalibGeometryFromSettings()
        {
            RobotArmSettings s = RobotArmSettings.Shared;
            _calibPickZ.Text = Number(s.PickZ);
            _calibMinX.Text = Number(s.PickAreaMinX);
            _calibMaxX.Text = Number(s.PickAreaMaxX);
            _calibMinY.Text = Number(s.PickAreaMinY);
            _calibMaxY.Text = Number(s.PickAreaMaxY);
            _calibStandbyX.Text = Number(s.StandbyX);
            _calibStandbyY.Text = Number(s.StandbyY);
            _calibPlaceX.Text = Number(s.PlaceX);
            _calibPlaceY.Text = Number(s.PlaceY);
            _calibPlaceZ.Text = Number(s.PlaceZ);
        }

        private void CalibGeometryBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 回车 ＝ 立刻生效（省得非要用鼠标点别处）
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                PushCalibGeometryToSettings();
            }
        }

        private void CalibGeometryBox_Leave(object sender, EventArgs e)
        {
            PushCalibGeometryToSettings();
        }

        private void PushCalibGeometryToSettings()
        {
            RobotArmSettings s = RobotArmSettings.Shared;

            double pickZ, minX, maxX, minY, maxY;
            double standbyX, standbyY, placeX, placeY, placeZ;
            if (!TryParseBox(_calibPickZ, "抓取高度 Z", out pickZ)) return;
            if (!TryParseBox(_calibMinX, "工作范围最小 X", out minX)) return;
            if (!TryParseBox(_calibMaxX, "工作范围最大 X", out maxX)) return;
            if (!TryParseBox(_calibMinY, "工作范围最小 Y", out minY)) return;
            if (!TryParseBox(_calibMaxY, "工作范围最大 Y", out maxY)) return;
            if (!TryParseBox(_calibStandbyX, "待机位 X", out standbyX)) return;
            if (!TryParseBox(_calibStandbyY, "待机位 Y", out standbyY)) return;
            if (!TryParseBox(_calibPlaceX, "放料点 X", out placeX)) return;
            if (!TryParseBox(_calibPlaceY, "放料点 Y", out placeY)) return;
            if (!TryParseBox(_calibPlaceZ, "放料点 Z", out placeZ)) return;

            if (minX >= maxX)
            {
                MessageBox.Show(this, "工作范围 X 的『最小』必须小于『最大』（现在 " + Number(minX) + " ≥ " + Number(maxX) + "）。",
                    "九点标定", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RefreshCalibGeometryFromSettings();
                return;
            }
            if (minY >= maxY)
            {
                MessageBox.Show(this, "工作范围 Y 的『最小』必须小于『最大』（现在 " + Number(minY) + " ≥ " + Number(maxY) + "）。",
                    "九点标定", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RefreshCalibGeometryFromSettings();
                return;
            }

            // 待机位也做个下限校验：它必须落在工作范围里（否则"回待机位"那一趟会先撞出去）
            if (standbyX < minX || standbyX > maxX || standbyY < minY || standbyY > maxY)
            {
                MessageBox.Show(this,
                    "待机位 (" + Number(standbyX) + ", " + Number(standbyY) + ") 不在工作范围里"
                    + "（X " + Number(minX) + "~" + Number(maxX) + "，Y " + Number(minY) + "~" + Number(maxY) + "）。\r\n"
                    + "放完料回到待机位那一步会先撞出去，所以这里先拦下。",
                    "九点标定", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RefreshCalibGeometryFromSettings();
                return;
            }

            s.PickZ = pickZ;
            s.PickAreaMinX = minX;
            s.PickAreaMaxX = maxX;
            s.PickAreaMinY = minY;
            s.PickAreaMaxY = maxY;
            s.StandbyX = standbyX;
            s.StandbyY = standbyY;
            s.PlaceX = placeX;
            s.PlaceY = placeY;
            s.PlaceZ = placeZ;

            RefreshCalibGeometryFromSettings();
            RefreshCalibStatus("抓取几何已更新：高度 Z=" + Number(pickZ)
                + "，范围 X " + Number(minX) + "~" + Number(maxX) + "，Y " + Number(minY) + "~" + Number(maxY)
                + "；待机 (" + Number(standbyX) + ", " + Number(standbyY) + ")"
                + "；放料 (" + Number(placeX) + ", " + Number(placeY) + ", " + Number(placeZ) + ")"
                + "（点『保存』才会写进 calib.json）。");
        }

        private bool TryParseBox(TextBox box, string name, out double value)
        {
            if (double.TryParse(box.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                || double.TryParse(box.Text.Trim(), out value))
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    MessageBox.Show(this, name + " 必须是数字。", "九点标定",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    RefreshCalibGeometryFromSettings();
                    return false;
                }
                return true;
            }

            MessageBox.Show(this, name + " 不是数字：" + box.Text.Trim(), "九点标定",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshCalibGeometryFromSettings();
            return false;
        }

        // ------------------------------------------------------------------
        // 状态刷新
        // ------------------------------------------------------------------

        private void RefreshCalibMetrics()
        {
            CalibrationService calib = CalibrationService.Shared;

            if (!calib.IsCalibrated)
            {
                _calibMetrics.Text = "拟合：－";
                _calibMetrics.ForeColor = SystemColors.ControlText;
                return;
            }

            _calibMetrics.Text = "拟合 RMSE " + calib.RmsError.ToString("F3", CultureInfo.InvariantCulture) + " mm"
                + " ｜ " + calib.MmPerPixel.ToString("F4", CultureInfo.InvariantCulture) + " mm/px"
                + " ｜ 旋转 " + calib.RotationDeg.ToString("F2", CultureInfo.InvariantCulture) + "°"
                + " ｜ 手性 " + (calib.HandednessOk ? "✓" : "✗");
            _calibMetrics.ForeColor = calib.HandednessOk ? Color.ForestGreen : Color.Firebrick;
        }

        private void RefreshCalibStatus(string message)
        {
            CalibrationService calib = CalibrationService.Shared;

            StringBuilder sb = new StringBuilder();
            if (calib.IsCalibrated)
            {
                sb.Append("标定：可用 ").Append(calib.Describe())
                  .Append("（").Append(calib.PairCount).Append(" 对点，标定于 ")
                  .Append(string.IsNullOrEmpty(calib.SavedAt) ? "未保存" : calib.SavedAt).Append("）");
                _calibStatus.ForeColor = Color.ForestGreen;
            }
            else if (calib.PairCount > 0 && !calib.HandednessOk)
            {
                sb.Append("标定：**镜像 ✗（不可用）** —— X/Y 有一个轴配反了，程序会拒绝保存和换算。");
                _calibStatus.ForeColor = Color.Firebrick;
            }
            else
            {
                sb.Append("标定：不可用（还没算过或还没填点对）");
                _calibStatus.ForeColor = Color.DimGray;
            }

            if (!string.IsNullOrEmpty(message))
            {
                sb.Append(Environment.NewLine).Append(message);
                if (sb.Length > 520) sb.Length = 520;   // 标签放不下，截断（完整内容在弹窗里）
            }

            _calibStatus.Text = sb.ToString();
        }
    }
}
