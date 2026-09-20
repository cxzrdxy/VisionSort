namespace VisionSort.Views
{
    partial class DataLogView
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.grpQuery = new System.Windows.Forms.GroupBox();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnQuery = new System.Windows.Forms.Button();
            this.cmbResult = new System.Windows.Forms.ComboBox();
            this.lblRes = new System.Windows.Forms.Label();
            this.dtpTo = new System.Windows.Forms.DateTimePicker();
            this.lblTo = new System.Windows.Forms.Label();
            this.dtpFrom = new System.Windows.Forms.DateTimePicker();
            this.lblFrom = new System.Windows.Forms.Label();
            this.grpTable = new System.Windows.Forms.GroupBox();
            this.dgvLog = new System.Windows.Forms.DataGridView();
            this.colTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRecipe = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResult = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScore = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNote = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pnlBottom = new System.Windows.Forms.Panel();
            this.grpExport = new System.Windows.Forms.GroupBox();
            this.btnExport = new System.Windows.Forms.Button();
            this.lblExportTip = new System.Windows.Forms.Label();
            this.grpSummary = new System.Windows.Forms.GroupBox();
            this.flpSummary = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSumTotal = new System.Windows.Forms.Label();
            this.lblSumOK = new System.Windows.Forms.Label();
            this.lblSumNG = new System.Windows.Forms.Label();
            this.lblSumRate = new System.Windows.Forms.Label();
            this.grpQuery.SuspendLayout();
            this.grpTable.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLog)).BeginInit();
            this.pnlBottom.SuspendLayout();
            this.grpExport.SuspendLayout();
            this.grpSummary.SuspendLayout();
            this.flpSummary.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpQuery
            // 
            this.grpQuery.Controls.Add(this.btnClear);
            this.grpQuery.Controls.Add(this.btnQuery);
            this.grpQuery.Controls.Add(this.cmbResult);
            this.grpQuery.Controls.Add(this.lblRes);
            this.grpQuery.Controls.Add(this.dtpTo);
            this.grpQuery.Controls.Add(this.lblTo);
            this.grpQuery.Controls.Add(this.dtpFrom);
            this.grpQuery.Controls.Add(this.lblFrom);
            this.grpQuery.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpQuery.Location = new System.Drawing.Point(0, 0);
            this.grpQuery.Name = "grpQuery";
            this.grpQuery.Size = new System.Drawing.Size(1100, 72);
            this.grpQuery.TabIndex = 0;
            this.grpQuery.TabStop = false;
            this.grpQuery.Text = "日志查询";
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(805, 25);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(100, 30);
            this.btnClear.TabIndex = 7;
            this.btnClear.Text = "清空查询";
            this.btnClear.UseVisualStyleBackColor = true;
            // 
            // btnQuery
            // 
            this.btnQuery.BackColor = System.Drawing.Color.DodgerBlue;
            this.btnQuery.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnQuery.ForeColor = System.Drawing.Color.White;
            this.btnQuery.Location = new System.Drawing.Point(695, 25);
            this.btnQuery.Name = "btnQuery";
            this.btnQuery.Size = new System.Drawing.Size(100, 30);
            this.btnQuery.TabIndex = 6;
            this.btnQuery.Text = "查询日志";
            this.btnQuery.UseVisualStyleBackColor = false;
            // 
            // cmbResult
            // 
            this.cmbResult.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbResult.FormattingEnabled = true;
            this.cmbResult.Items.AddRange(new object[] {
            "全部",
            "OK",
            "NG"});
            this.cmbResult.Location = new System.Drawing.Point(595, 27);
            this.cmbResult.Name = "cmbResult";
            this.cmbResult.Size = new System.Drawing.Size(90, 36);
            this.cmbResult.TabIndex = 5;
            // 
            // lblRes
            // 
            this.lblRes.AutoSize = true;
            this.lblRes.Location = new System.Drawing.Point(552, 30);
            this.lblRes.Name = "lblRes";
            this.lblRes.Size = new System.Drawing.Size(54, 28);
            this.lblRes.TabIndex = 4;
            this.lblRes.Text = "结果";
            // 
            // dtpTo
            // 
            this.dtpTo.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            this.dtpTo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpTo.Location = new System.Drawing.Point(481, -41);
            this.dtpTo.Name = "dtpTo";
            this.dtpTo.ShowUpDown = true;
            this.dtpTo.Size = new System.Drawing.Size(180, 35);
            this.dtpTo.TabIndex = 3;
            // 
            // lblTo
            // 
            this.lblTo.AutoSize = true;
            this.lblTo.Location = new System.Drawing.Point(280, 30);
            this.lblTo.Name = "lblTo";
            this.lblTo.Size = new System.Drawing.Size(96, 28);
            this.lblTo.TabIndex = 2;
            this.lblTo.Text = "结束时间";
            // 
            // dtpFrom
            // 
            this.dtpFrom.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            this.dtpFrom.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpFrom.Location = new System.Drawing.Point(90, 27);
            this.dtpFrom.Name = "dtpFrom";
            this.dtpFrom.ShowUpDown = true;
            this.dtpFrom.Size = new System.Drawing.Size(180, 35);
            this.dtpFrom.TabIndex = 1;
            // 
            // lblFrom
            // 
            this.lblFrom.AutoSize = true;
            this.lblFrom.Location = new System.Drawing.Point(12, 30);
            this.lblFrom.Name = "lblFrom";
            this.lblFrom.Size = new System.Drawing.Size(96, 28);
            this.lblFrom.TabIndex = 0;
            this.lblFrom.Text = "开始时间";
            // 
            // grpTable
            // 
            this.grpTable.Controls.Add(this.dgvLog);
            this.grpTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpTable.Location = new System.Drawing.Point(0, 72);
            this.grpTable.Name = "grpTable";
            this.grpTable.Padding = new System.Windows.Forms.Padding(8);
            this.grpTable.Size = new System.Drawing.Size(1100, 440);
            this.grpTable.TabIndex = 1;
            this.grpTable.TabStop = false;
            this.grpTable.Text = "日志表格";
            // 
            // dgvLog
            // 
            this.dgvLog.AllowUserToAddRows = false;
            this.dgvLog.AllowUserToDeleteRows = false;
            this.dgvLog.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvLog.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvLog.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colTime,
            this.colNo,
            this.colRecipe,
            this.colResult,
            this.colScore,
            this.colPath,
            this.colNote});
            this.dgvLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvLog.Location = new System.Drawing.Point(8, 36);
            this.dgvLog.MultiSelect = false;
            this.dgvLog.Name = "dgvLog";
            this.dgvLog.ReadOnly = true;
            this.dgvLog.RowHeadersVisible = false;
            this.dgvLog.RowHeadersWidth = 72;
            this.dgvLog.RowTemplate.Height = 25;
            this.dgvLog.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvLog.Size = new System.Drawing.Size(1084, 396);
            this.dgvLog.TabIndex = 0;
            // 
            // colTime
            // 
            this.colTime.DataPropertyName = "Time";
            this.colTime.HeaderText = "时间";
            this.colTime.MinimumWidth = 9;
            this.colTime.Name = "colTime";
            this.colTime.ReadOnly = true;
            // 
            // colNo
            // 
            this.colNo.DataPropertyName = "WorkpieceNo";
            this.colNo.HeaderText = "工件编号";
            this.colNo.MinimumWidth = 9;
            this.colNo.Name = "colNo";
            this.colNo.ReadOnly = true;
            // 
            // colRecipe
            // 
            this.colRecipe.DataPropertyName = "RecipeID";
            this.colRecipe.HeaderText = "配方";
            this.colRecipe.MinimumWidth = 9;
            this.colRecipe.Name = "colRecipe";
            this.colRecipe.ReadOnly = true;
            // 
            // colResult
            // 
            this.colResult.DataPropertyName = "Result";
            this.colResult.HeaderText = "检测结果";
            this.colResult.MinimumWidth = 9;
            this.colResult.Name = "colResult";
            this.colResult.ReadOnly = true;
            // 
            // colScore
            // 
            this.colScore.DataPropertyName = "Score";
            this.colScore.HeaderText = "匹配分数";
            this.colScore.MinimumWidth = 9;
            this.colScore.Name = "colScore";
            this.colScore.ReadOnly = true;
            // 
            // colPath
            // 
            this.colPath.DataPropertyName = "ImagePath";
            this.colPath.HeaderText = "图像路径";
            this.colPath.MinimumWidth = 9;
            this.colPath.Name = "colPath";
            this.colPath.ReadOnly = true;
            // 
            // colNote
            // 
            this.colNote.DataPropertyName = "Note";
            this.colNote.HeaderText = "备注";
            this.colNote.MinimumWidth = 9;
            this.colNote.Name = "colNote";
            this.colNote.ReadOnly = true;
            // 
            // pnlBottom
            // 
            this.pnlBottom.Controls.Add(this.grpExport);
            this.pnlBottom.Controls.Add(this.grpSummary);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, 512);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Size = new System.Drawing.Size(1100, 128);
            this.pnlBottom.TabIndex = 2;
            // 
            // grpExport
            // 
            this.grpExport.Controls.Add(this.btnExport);
            this.grpExport.Controls.Add(this.lblExportTip);
            this.grpExport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpExport.Location = new System.Drawing.Point(568, 0);
            this.grpExport.Name = "grpExport";
            this.grpExport.Size = new System.Drawing.Size(532, 128);
            this.grpExport.TabIndex = 1;
            this.grpExport.TabStop = false;
            this.grpExport.Text = "报表导出";
            // 
            // btnExport
            // 
            this.btnExport.BackColor = System.Drawing.Color.DodgerBlue;
            this.btnExport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExport.ForeColor = System.Drawing.Color.White;
            this.btnExport.Location = new System.Drawing.Point(16, 30);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(150, 36);
            this.btnExport.TabIndex = 0;
            this.btnExport.Text = "导出Excel日志";
            this.btnExport.UseVisualStyleBackColor = false;
            // 
            // lblExportTip
            // 
            this.lblExportTip.AutoSize = true;
            this.lblExportTip.ForeColor = System.Drawing.Color.Gray;
            this.lblExportTip.Location = new System.Drawing.Point(180, 40);
            this.lblExportTip.Name = "lblExportTip";
            this.lblExportTip.Size = new System.Drawing.Size(285, 28);
            this.lblExportTip.TabIndex = 1;
            this.lblExportTip.Text = "万级记录后台导出，不锁界面";
            // 
            // grpSummary
            // 
            this.grpSummary.Controls.Add(this.flpSummary);
            this.grpSummary.Dock = System.Windows.Forms.DockStyle.Left;
            this.grpSummary.Location = new System.Drawing.Point(0, 0);
            this.grpSummary.Name = "grpSummary";
            this.grpSummary.Size = new System.Drawing.Size(568, 128);
            this.grpSummary.TabIndex = 0;
            this.grpSummary.TabStop = false;
            this.grpSummary.Text = "统计汇总";
            // 
            // flpSummary
            // 
            this.flpSummary.Controls.Add(this.lblSumTotal);
            this.flpSummary.Controls.Add(this.lblSumOK);
            this.flpSummary.Controls.Add(this.lblSumNG);
            this.flpSummary.Controls.Add(this.lblSumRate);
            this.flpSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpSummary.Location = new System.Drawing.Point(3, 31);
            this.flpSummary.Name = "flpSummary";
            this.flpSummary.Padding = new System.Windows.Forms.Padding(4);
            this.flpSummary.Size = new System.Drawing.Size(562, 94);
            this.flpSummary.TabIndex = 0;
            // 
            // lblSumTotal
            // 
            this.lblSumTotal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSumTotal.Location = new System.Drawing.Point(7, 4);
            this.lblSumTotal.Name = "lblSumTotal";
            this.lblSumTotal.Size = new System.Drawing.Size(128, 52);
            this.lblSumTotal.TabIndex = 0;
            this.lblSumTotal.Text = "总检测：0";
            this.lblSumTotal.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSumOK
            // 
            this.lblSumOK.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSumOK.Location = new System.Drawing.Point(141, 4);
            this.lblSumOK.Name = "lblSumOK";
            this.lblSumOK.Size = new System.Drawing.Size(128, 52);
            this.lblSumOK.TabIndex = 1;
            this.lblSumOK.Text = "OK：0";
            this.lblSumOK.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSumNG
            // 
            this.lblSumNG.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSumNG.Location = new System.Drawing.Point(275, 4);
            this.lblSumNG.Name = "lblSumNG";
            this.lblSumNG.Size = new System.Drawing.Size(128, 52);
            this.lblSumNG.TabIndex = 2;
            this.lblSumNG.Text = "NG：0";
            this.lblSumNG.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSumRate
            // 
            this.lblSumRate.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSumRate.Location = new System.Drawing.Point(409, 4);
            this.lblSumRate.Name = "lblSumRate";
            this.lblSumRate.Size = new System.Drawing.Size(128, 52);
            this.lblSumRate.TabIndex = 3;
            this.lblSumRate.Text = "合格率：0.00%";
            this.lblSumRate.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // DataLogView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 28F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.grpTable);
            this.Controls.Add(this.pnlBottom);
            this.Controls.Add(this.grpQuery);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Name = "DataLogView";
            this.Size = new System.Drawing.Size(1100, 640);
            this.grpQuery.ResumeLayout(false);
            this.grpQuery.PerformLayout();
            this.grpTable.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvLog)).EndInit();
            this.pnlBottom.ResumeLayout(false);
            this.grpExport.ResumeLayout(false);
            this.grpExport.PerformLayout();
            this.grpSummary.ResumeLayout(false);
            this.flpSummary.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpQuery;
        private System.Windows.Forms.Label lblFrom;
        private System.Windows.Forms.DateTimePicker dtpFrom;
        private System.Windows.Forms.Label lblTo;
        private System.Windows.Forms.DateTimePicker dtpTo;
        private System.Windows.Forms.Label lblRes;
        private System.Windows.Forms.ComboBox cmbResult;
        private System.Windows.Forms.Button btnQuery;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.GroupBox grpTable;
        private System.Windows.Forms.DataGridView dgvLog;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNo;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRecipe;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResult;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScore;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPath;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNote;
        private System.Windows.Forms.Panel pnlBottom;
        private System.Windows.Forms.GroupBox grpSummary;
        private System.Windows.Forms.FlowLayoutPanel flpSummary;
        private System.Windows.Forms.Label lblSumTotal;
        private System.Windows.Forms.Label lblSumOK;
        private System.Windows.Forms.Label lblSumNG;
        private System.Windows.Forms.Label lblSumRate;
        private System.Windows.Forms.GroupBox grpExport;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Label lblExportTip;
    }
}
