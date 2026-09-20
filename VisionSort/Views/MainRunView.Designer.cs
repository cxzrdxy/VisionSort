namespace VisionSort.Views
{
    partial class MainRunView
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
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.grpPreview = new System.Windows.Forms.GroupBox();
            this.pnlDisplay = new System.Windows.Forms.Panel();
            this.lblDisplayTip = new System.Windows.Forms.Label();
            this.lblFlow = new System.Windows.Forms.Label();
            this.grpControl = new System.Windows.Forms.GroupBox();
            this.pnlEstop = new System.Windows.Forms.Panel();
            this.btnEstop = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.pnlRecipe = new System.Windows.Forms.Panel();
            this.lblRecipe = new System.Windows.Forms.Label();
            this.cmbRecipe = new System.Windows.Forms.ComboBox();
            this.pnlLights2 = new System.Windows.Forms.Panel();
            this.btnShot = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnNG = new System.Windows.Forms.Button();
            this.pnlLights1 = new System.Windows.Forms.Panel();
            this.lblSysReady = new System.Windows.Forms.Label();
            this.lblTrigger = new System.Windows.Forms.Label();
            this.flpRun = new System.Windows.Forms.FlowLayoutPanel();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnSingle = new System.Windows.Forms.Button();
            this.btnCont = new System.Windows.Forms.Button();
            this.grpStats = new System.Windows.Forms.GroupBox();
            this.lblRate = new System.Windows.Forms.Label();
            this.lblNGCount = new System.Windows.Forms.Label();
            this.lblOKCount = new System.Windows.Forms.Label();
            this.lblTotal = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.grpPreview.SuspendLayout();
            this.pnlDisplay.SuspendLayout();
            this.grpControl.SuspendLayout();
            this.pnlEstop.SuspendLayout();
            this.pnlRecipe.SuspendLayout();
            this.pnlLights2.SuspendLayout();
            this.pnlLights1.SuspendLayout();
            this.flpRun.SuspendLayout();
            this.grpStats.SuspendLayout();
            this.SuspendLayout();
            //
            // splitMain
            //
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Location = new System.Drawing.Point(0, 0);
            this.splitMain.Name = "splitMain";
            this.splitMain.Orientation = System.Windows.Forms.Orientation.Vertical;
            //
            // splitMain.Panel1
            //
            this.splitMain.Panel1.Controls.Add(this.grpPreview);
            this.splitMain.Panel1.Padding = new System.Windows.Forms.Padding(0, 0, 4, 0);
            //
            // splitMain.Panel2
            //
            this.splitMain.Panel2.Controls.Add(this.grpStats);
            this.splitMain.Panel2.Controls.Add(this.grpControl);
            this.splitMain.Panel2.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.splitMain.Size = new System.Drawing.Size(1100, 640);
            this.splitMain.SplitterDistance = 700;
            this.splitMain.TabIndex = 0;
            //
            // grpPreview
            //
            this.grpPreview.Controls.Add(this.pnlDisplay);
            this.grpPreview.Controls.Add(this.lblFlow);
            this.grpPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPreview.Location = new System.Drawing.Point(0, 0);
            this.grpPreview.Name = "grpPreview";
            this.grpPreview.Size = new System.Drawing.Size(696, 640);
            this.grpPreview.TabIndex = 0;
            this.grpPreview.TabStop = false;
            this.grpPreview.Text = "图像预览区";
            //
            // pnlDisplay
            //
            this.pnlDisplay.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(37)))), ((int)(((byte)(48)))));
            this.pnlDisplay.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlDisplay.Controls.Add(this.lblDisplayTip);
            this.pnlDisplay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlDisplay.Location = new System.Drawing.Point(3, 19);
            this.pnlDisplay.Name = "pnlDisplay";
            this.pnlDisplay.Size = new System.Drawing.Size(690, 575);
            this.pnlDisplay.TabIndex = 0;
            //
            // lblDisplayTip
            //
            this.lblDisplayTip.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDisplayTip.ForeColor = System.Drawing.Color.White;
            this.lblDisplayTip.Location = new System.Drawing.Point(0, 0);
            this.lblDisplayTip.Name = "lblDisplayTip";
            this.lblDisplayTip.Size = new System.Drawing.Size(688, 573);
            this.lblDisplayTip.TabIndex = 0;
            this.lblDisplayTip.Text = "CogDisplay 占位 — 相机画面 + 检测框\r\n联调时将此 Panel 替换为 CogRecordDisplay";
            this.lblDisplayTip.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblFlow
            //
            this.lblFlow.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblFlow.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblFlow.Location = new System.Drawing.Point(3, 594);
            this.lblFlow.Name = "lblFlow";
            this.lblFlow.Size = new System.Drawing.Size(690, 43);
            this.lblFlow.TabIndex = 1;
            this.lblFlow.Text = "状态链：等待工件 → 光电触发 → 停止传送带 → 拍照检测 → 机械臂分拣 → 恢复传送带";
            this.lblFlow.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // grpControl
            //
            this.grpControl.Controls.Add(this.pnlEstop);
            this.grpControl.Controls.Add(this.pnlRecipe);
            this.grpControl.Controls.Add(this.pnlLights2);
            this.grpControl.Controls.Add(this.pnlLights1);
            this.grpControl.Controls.Add(this.flpRun);
            this.grpControl.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpControl.Location = new System.Drawing.Point(4, 0);
            this.grpControl.Name = "grpControl";
            this.grpControl.Size = new System.Drawing.Size(392, 264);
            this.grpControl.TabIndex = 0;
            this.grpControl.TabStop = false;
            this.grpControl.Text = "运行控制";
            //
            // pnlEstop
            //
            this.pnlEstop.Controls.Add(this.btnEstop);
            this.pnlEstop.Controls.Add(this.btnReset);
            this.pnlEstop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlEstop.Location = new System.Drawing.Point(3, 213);
            this.pnlEstop.Name = "pnlEstop";
            this.pnlEstop.Size = new System.Drawing.Size(386, 42);
            this.pnlEstop.TabIndex = 4;
            //
            // btnEstop
            //
            this.btnEstop.BackColor = System.Drawing.Color.Crimson;
            this.btnEstop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEstop.ForeColor = System.Drawing.Color.White;
            this.btnEstop.Location = new System.Drawing.Point(8, 6);
            this.btnEstop.Name = "btnEstop";
            this.btnEstop.Size = new System.Drawing.Size(96, 30);
            this.btnEstop.TabIndex = 0;
            this.btnEstop.Text = "急停";
            this.btnEstop.UseVisualStyleBackColor = false;
            //
            // btnReset
            //
            this.btnReset.Location = new System.Drawing.Point(112, 6);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(96, 30);
            this.btnReset.TabIndex = 1;
            this.btnReset.Text = "报警复位";
            this.btnReset.UseVisualStyleBackColor = true;
            //
            // pnlRecipe
            //
            this.pnlRecipe.Controls.Add(this.lblRecipe);
            this.pnlRecipe.Controls.Add(this.cmbRecipe);
            this.pnlRecipe.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlRecipe.Location = new System.Drawing.Point(3, 175);
            this.pnlRecipe.Name = "pnlRecipe";
            this.pnlRecipe.Size = new System.Drawing.Size(386, 38);
            this.pnlRecipe.TabIndex = 3;
            //
            // lblRecipe
            //
            this.lblRecipe.AutoSize = true;
            this.lblRecipe.Location = new System.Drawing.Point(8, 11);
            this.lblRecipe.Name = "lblRecipe";
            this.lblRecipe.Size = new System.Drawing.Size(56, 17);
            this.lblRecipe.TabIndex = 0;
            this.lblRecipe.Text = "工件编号";
            //
            // cmbRecipe
            //
            // 工件编号（Q8=A：可输入）——现场输批次号/件号，将来扫码枪也喂这个框。
            // ⚠ 这里原来有草稿截图里的示例文字「A001-通用件 / A002-缺料检测」，**已删除**：
            //    它们既不来自配置文件也不来自数据库，留着就会被当成真实工件编号
            //    写进 vision_result.workpiece_no（操作工没输过，库里却有值，追溯时是假数据）。
            this.cmbRecipe.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this.cmbRecipe.FormattingEnabled = true;
            this.cmbRecipe.Text = "";
            this.cmbRecipe.Location = new System.Drawing.Point(96, 7);
            this.cmbRecipe.Name = "cmbRecipe";
            this.cmbRecipe.Size = new System.Drawing.Size(220, 25);
            this.cmbRecipe.TabIndex = 1;
            //
            // pnlLights2
            //
            this.pnlLights2.Controls.Add(this.btnShot);
            this.pnlLights2.Controls.Add(this.btnOK);
            this.pnlLights2.Controls.Add(this.btnNG);
            this.pnlLights2.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlLights2.Location = new System.Drawing.Point(3, 133);
            this.pnlLights2.Name = "pnlLights2";
            this.pnlLights2.Size = new System.Drawing.Size(386, 42);
            this.pnlLights2.TabIndex = 2;
            //
            // btnShot
            //
            this.btnShot.Enabled = false;
            this.btnShot.Location = new System.Drawing.Point(8, 6);
            this.btnShot.Name = "btnShot";
            this.btnShot.Size = new System.Drawing.Size(96, 30);
            this.btnShot.TabIndex = 0;
            this.btnShot.Text = "拍照中";
            this.btnShot.UseVisualStyleBackColor = true;
            //
            // btnOK
            //
            this.btnOK.Location = new System.Drawing.Point(112, 6);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(96, 30);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "检测OK";
            this.btnOK.UseVisualStyleBackColor = true;
            //
            // btnNG
            //
            this.btnNG.Location = new System.Drawing.Point(216, 6);
            this.btnNG.Name = "btnNG";
            this.btnNG.Size = new System.Drawing.Size(96, 30);
            this.btnNG.TabIndex = 2;
            this.btnNG.Text = "检测NG";
            this.btnNG.UseVisualStyleBackColor = true;
            //
            // pnlLights1
            //
            this.pnlLights1.Controls.Add(this.lblSysReady);
            this.pnlLights1.Controls.Add(this.lblTrigger);
            this.pnlLights1.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlLights1.Location = new System.Drawing.Point(3, 99);
            this.pnlLights1.Name = "pnlLights1";
            this.pnlLights1.Size = new System.Drawing.Size(386, 34);
            this.pnlLights1.TabIndex = 1;
            //
            // lblSysReady
            //
            this.lblSysReady.AutoSize = true;
            this.lblSysReady.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblSysReady.ForeColor = System.Drawing.Color.DarkOrange;
            this.lblSysReady.Location = new System.Drawing.Point(8, 7);
            this.lblSysReady.Name = "lblSysReady";
            this.lblSysReady.Size = new System.Drawing.Size(93, 19);
            this.lblSysReady.TabIndex = 0;
            this.lblSysReady.Text = "● 系统就绪";
            //
            // lblTrigger
            //
            this.lblTrigger.AutoSize = true;
            this.lblTrigger.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblTrigger.ForeColor = System.Drawing.Color.ForestGreen;
            this.lblTrigger.Location = new System.Drawing.Point(150, 7);
            this.lblTrigger.Name = "lblTrigger";
            this.lblTrigger.Size = new System.Drawing.Size(93, 19);
            this.lblTrigger.TabIndex = 1;
            this.lblTrigger.Text = "● 光电触发";
            //
            // flpRun
            //
            this.flpRun.Controls.Add(this.btnStart);
            this.flpRun.Controls.Add(this.btnStop);
            this.flpRun.Controls.Add(this.btnSingle);
            this.flpRun.Controls.Add(this.btnCont);
            this.flpRun.Dock = System.Windows.Forms.DockStyle.Top;
            this.flpRun.Location = new System.Drawing.Point(3, 19);
            this.flpRun.Name = "flpRun";
            this.flpRun.Padding = new System.Windows.Forms.Padding(4);
            this.flpRun.Size = new System.Drawing.Size(386, 80);
            this.flpRun.TabIndex = 0;
            //
            // btnStart
            //
            this.btnStart.BackColor = System.Drawing.Color.DodgerBlue;
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.ForeColor = System.Drawing.Color.White;
            this.btnStart.Location = new System.Drawing.Point(7, 7);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(100, 32);
            this.btnStart.TabIndex = 0;
            this.btnStart.Text = "开始检测";
            this.btnStart.UseVisualStyleBackColor = false;
            //
            // btnStop
            //
            this.btnStop.Location = new System.Drawing.Point(113, 7);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(100, 32);
            this.btnStop.TabIndex = 1;
            this.btnStop.Text = "停止检测";
            this.btnStop.UseVisualStyleBackColor = true;
            //
            // btnSingle
            //
            this.btnSingle.Location = new System.Drawing.Point(219, 7);
            this.btnSingle.Name = "btnSingle";
            this.btnSingle.Size = new System.Drawing.Size(100, 32);
            this.btnSingle.TabIndex = 2;
            this.btnSingle.Text = "单次运行";
            this.btnSingle.UseVisualStyleBackColor = true;
            //
            // btnCont
            //
            this.btnCont.Location = new System.Drawing.Point(7, 45);
            this.btnCont.Name = "btnCont";
            this.btnCont.Size = new System.Drawing.Size(100, 32);
            this.btnCont.TabIndex = 3;
            this.btnCont.Text = "连续运行";
            this.btnCont.UseVisualStyleBackColor = true;
            //
            // grpStats
            //
            this.grpStats.Controls.Add(this.lblRate);
            this.grpStats.Controls.Add(this.lblNGCount);
            this.grpStats.Controls.Add(this.lblOKCount);
            this.grpStats.Controls.Add(this.lblTotal);
            this.grpStats.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpStats.Location = new System.Drawing.Point(4, 264);
            this.grpStats.Name = "grpStats";
            this.grpStats.Size = new System.Drawing.Size(392, 376);
            this.grpStats.TabIndex = 1;
            this.grpStats.TabStop = false;
            this.grpStats.Text = "检测统计";
            //
            // lblRate
            //
            this.lblRate.AutoSize = true;
            this.lblRate.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblRate.Location = new System.Drawing.Point(40, 128);
            this.lblRate.Name = "lblRate";
            this.lblRate.Size = new System.Drawing.Size(122, 20);
            this.lblRate.TabIndex = 3;
            this.lblRate.Text = "合格率：0.00%";
            //
            // lblNGCount
            //
            this.lblNGCount.AutoSize = true;
            this.lblNGCount.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblNGCount.Location = new System.Drawing.Point(40, 98);
            this.lblNGCount.Name = "lblNGCount";
            this.lblNGCount.Size = new System.Drawing.Size(92, 20);
            this.lblNGCount.TabIndex = 2;
            this.lblNGCount.Text = "瑕疵数量：0";
            //
            // lblOKCount
            //
            this.lblOKCount.AutoSize = true;
            this.lblOKCount.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblOKCount.Location = new System.Drawing.Point(40, 68);
            this.lblOKCount.Name = "lblOKCount";
            this.lblOKCount.Size = new System.Drawing.Size(92, 20);
            this.lblOKCount.TabIndex = 1;
            this.lblOKCount.Text = "合格数量：0";
            //
            // lblTotal
            //
            this.lblTotal.AutoSize = true;
            this.lblTotal.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblTotal.Location = new System.Drawing.Point(40, 38);
            this.lblTotal.Name = "lblTotal";
            this.lblTotal.Size = new System.Drawing.Size(107, 20);
            this.lblTotal.TabIndex = 0;
            this.lblTotal.Text = "总检测数量：0";
            //
            // MainRunView
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitMain);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Name = "MainRunView";
            this.Size = new System.Drawing.Size(1100, 640);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            this.splitMain.ResumeLayout(false);
            this.grpPreview.ResumeLayout(false);
            this.pnlDisplay.ResumeLayout(false);
            this.grpControl.ResumeLayout(false);
            this.pnlEstop.ResumeLayout(false);
            this.pnlRecipe.ResumeLayout(false);
            this.pnlLights2.ResumeLayout(false);
            this.pnlLights1.ResumeLayout(false);
            this.flpRun.ResumeLayout(false);
            this.grpStats.ResumeLayout(false);
            this.grpStats.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.GroupBox grpPreview;
        private System.Windows.Forms.Panel pnlDisplay;
        private System.Windows.Forms.Label lblDisplayTip;
        private System.Windows.Forms.Label lblFlow;
        private System.Windows.Forms.GroupBox grpControl;
        private System.Windows.Forms.FlowLayoutPanel flpRun;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnSingle;
        private System.Windows.Forms.Button btnCont;
        private System.Windows.Forms.Panel pnlLights1;
        private System.Windows.Forms.Label lblSysReady;
        private System.Windows.Forms.Label lblTrigger;
        private System.Windows.Forms.Panel pnlLights2;
        private System.Windows.Forms.Button btnShot;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnNG;
        private System.Windows.Forms.Panel pnlRecipe;
        private System.Windows.Forms.Label lblRecipe;
        private System.Windows.Forms.ComboBox cmbRecipe;
        private System.Windows.Forms.Panel pnlEstop;
        private System.Windows.Forms.Button btnEstop;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.GroupBox grpStats;
        private System.Windows.Forms.Label lblTotal;
        private System.Windows.Forms.Label lblOKCount;
        private System.Windows.Forms.Label lblNGCount;
        private System.Windows.Forms.Label lblRate;
    }
}
