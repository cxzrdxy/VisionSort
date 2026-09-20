namespace VisionSort
{
    partial class MainForm
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
            this.lblTitle = new System.Windows.Forms.Label();
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabPageRun = new System.Windows.Forms.TabPage();
            this.tabPageDevice = new System.Windows.Forms.TabPage();
            this.tabPageVision = new System.Windows.Forms.TabPage();
            this.tabPageLog = new System.Windows.Forms.TabPage();
            this.tabPageSetup = new System.Windows.Forms.TabPage();
            this.statusMain = new System.Windows.Forms.StatusStrip();
            this.tslStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.tslHint = new System.Windows.Forms.ToolStripStatusLabel();
            this.tabMain.SuspendLayout();
            this.statusMain.SuspendLayout();
            this.SuspendLayout();
            //
            // lblTitle
            //
            this.lblTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(80)))), ((int)(((byte)(180)))));
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitle.Font = new System.Drawing.Font("微软雅黑", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.lblTitle.Size = new System.Drawing.Size(1280, 52);
            this.lblTitle.TabIndex = 1;
            this.lblTitle.Text = "工业视觉分拣系统";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // tabMain
            //
            this.tabMain.Controls.Add(this.tabPageRun);
            this.tabMain.Controls.Add(this.tabPageDevice);
            this.tabMain.Controls.Add(this.tabPageVision);
            this.tabMain.Controls.Add(this.tabPageLog);
            this.tabMain.Controls.Add(this.tabPageSetup);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tabMain.Location = new System.Drawing.Point(0, 52);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(1280, 696);
            this.tabMain.TabIndex = 0;
            //
            // tabPageRun
            //
            this.tabPageRun.Location = new System.Drawing.Point(4, 27);
            this.tabPageRun.Name = "tabPageRun";
            this.tabPageRun.Padding = new System.Windows.Forms.Padding(6);
            this.tabPageRun.Size = new System.Drawing.Size(1272, 665);
            this.tabPageRun.TabIndex = 0;
            this.tabPageRun.Text = "主运行页";
            this.tabPageRun.UseVisualStyleBackColor = true;
            //
            // tabPageDevice
            //
            this.tabPageDevice.Location = new System.Drawing.Point(4, 27);
            this.tabPageDevice.Name = "tabPageDevice";
            this.tabPageDevice.Padding = new System.Windows.Forms.Padding(6);
            this.tabPageDevice.Size = new System.Drawing.Size(1272, 665);
            this.tabPageDevice.TabIndex = 1;
            this.tabPageDevice.Text = "设备调试";
            this.tabPageDevice.UseVisualStyleBackColor = true;
            //
            // tabPageVision
            //
            this.tabPageVision.Location = new System.Drawing.Point(4, 27);
            this.tabPageVision.Name = "tabPageVision";
            this.tabPageVision.Padding = new System.Windows.Forms.Padding(6);
            this.tabPageVision.Size = new System.Drawing.Size(1272, 665);
            this.tabPageVision.TabIndex = 2;
            this.tabPageVision.Text = "视觉配置";
            this.tabPageVision.UseVisualStyleBackColor = true;
            //
            // tabPageLog
            //
            this.tabPageLog.Location = new System.Drawing.Point(4, 27);
            this.tabPageLog.Name = "tabPageLog";
            this.tabPageLog.Padding = new System.Windows.Forms.Padding(6);
            this.tabPageLog.Size = new System.Drawing.Size(1272, 665);
            this.tabPageLog.TabIndex = 3;
            this.tabPageLog.Text = "数据日志";
            this.tabPageLog.UseVisualStyleBackColor = true;
            //
            // tabPageSetup
            //
            this.tabPageSetup.Location = new System.Drawing.Point(4, 27);
            this.tabPageSetup.Name = "tabPageSetup";
            this.tabPageSetup.Padding = new System.Windows.Forms.Padding(6);
            this.tabPageSetup.Size = new System.Drawing.Size(1272, 665);
            this.tabPageSetup.TabIndex = 4;
            this.tabPageSetup.Text = "系统设置";
            this.tabPageSetup.UseVisualStyleBackColor = true;
            //
            // statusMain
            //
            this.statusMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tslStatus,
            this.tslHint});
            this.statusMain.Location = new System.Drawing.Point(0, 748);
            this.statusMain.Name = "statusMain";
            this.statusMain.Size = new System.Drawing.Size(1280, 26);
            this.statusMain.TabIndex = 2;
            //
            // tslStatus
            //
            this.tslStatus.Name = "tslStatus";
            this.tslStatus.Size = new System.Drawing.Size(108, 21);
            this.tslStatus.Text = "就绪 · 等待工件";
            //
            // tslHint
            //
            this.tslHint.Name = "tslHint";
            this.tslHint.Size = new System.Drawing.Size(332, 21);
            this.tslHint.Text = "设计预览：Views 下 5 个视图均可单独打开设计器";
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1280, 774);
            this.Controls.Add(this.tabMain);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.statusMain);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.MinimumSize = new System.Drawing.Size(1100, 700);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "工业视觉分拣系统";
            this.tabMain.ResumeLayout(false);
            this.statusMain.ResumeLayout(false);
            this.statusMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabPageRun;
        private System.Windows.Forms.TabPage tabPageDevice;
        private System.Windows.Forms.TabPage tabPageVision;
        private System.Windows.Forms.TabPage tabPageLog;
        private System.Windows.Forms.TabPage tabPageSetup;
        private System.Windows.Forms.StatusStrip statusMain;
        private System.Windows.Forms.ToolStripStatusLabel tslStatus;
        private System.Windows.Forms.ToolStripStatusLabel tslHint;
    }
}
