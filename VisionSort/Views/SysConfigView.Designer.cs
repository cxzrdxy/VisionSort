namespace VisionSort.Views
{
    partial class SysConfigView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SysConfigView));
            this.tlpMain = new System.Windows.Forms.TableLayoutPanel();
            this.grpModbus = new System.Windows.Forms.GroupBox();
            this.tlpMb = new System.Windows.Forms.TableLayoutPanel();
            this.lblComPort = new System.Windows.Forms.Label();
            this.txtComPort = new System.Windows.Forms.TextBox();
            this.lblBaud = new System.Windows.Forms.Label();
            this.txtBaud = new System.Windows.Forms.TextBox();
            this.lblSlaveId = new System.Windows.Forms.Label();
            this.txtSlaveId = new System.Windows.Forms.TextBox();
            this.lblRegFwd = new System.Windows.Forms.Label();
            this.txtRegFwd = new System.Windows.Forms.TextBox();
            this.lblRegRev = new System.Windows.Forms.Label();
            this.txtRegRev = new System.Windows.Forms.TextBox();
            this.lblRegSpeed = new System.Windows.Forms.Label();
            this.txtRegSpeed = new System.Windows.Forms.TextBox();
            this.grpRobotCfg = new System.Windows.Forms.GroupBox();
            this.tlpRb = new System.Windows.Forms.TableLayoutPanel();
            this.lblRbCom = new System.Windows.Forms.Label();
            this.txtRbCom = new System.Windows.Forms.TextBox();
            this.lblRbPort = new System.Windows.Forms.Label();
            this.txtRbPort = new System.Windows.Forms.TextBox();
            this.lblPickD = new System.Windows.Forms.Label();
            this.txtPickDelay = new System.Windows.Forms.TextBox();
            this.lblHomeD = new System.Windows.Forms.Label();
            this.txtHomeDelay = new System.Windows.Forms.TextBox();
            this.lblAutoBridge = new System.Windows.Forms.Label();
            this.chkAutoBridge = new System.Windows.Forms.CheckBox();
            this.grpVisionP = new System.Windows.Forms.GroupBox();
            this.tlpVp = new System.Windows.Forms.TableLayoutPanel();
            this.lblThr = new System.Windows.Forms.Label();
            this.txtThreshold = new System.Windows.Forms.TextBox();
            this.lblStopD = new System.Windows.Forms.Label();
            this.txtStopDelay = new System.Windows.Forms.TextBox();
            this.lblPhotoW = new System.Windows.Forms.Label();
            this.txtPhotoWait = new System.Windows.Forms.TextBox();
            this.lblTimeout = new System.Windows.Forms.Label();
            this.txtTimeout = new System.Windows.Forms.TextBox();
            this.grpDb = new System.Windows.Forms.GroupBox();
            this.tlpDb = new System.Windows.Forms.TableLayoutPanel();
            this.lblDbType = new System.Windows.Forms.Label();
            this.cmbDbType = new System.Windows.Forms.ComboBox();
            this.lblConn = new System.Windows.Forms.Label();
            this.txtConnStr = new System.Windows.Forms.TextBox();
            this.flpDb = new System.Windows.Forms.FlowLayoutPanel();
            this.btnTestConn = new System.Windows.Forms.Button();
            this.btnInitDb = new System.Windows.Forms.Button();
            this.tlpBottom = new System.Windows.Forms.TableLayoutPanel();
            this.flpBtns = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSaveParam = new System.Windows.Forms.Button();
            this.btnLoadParam = new System.Windows.Forms.Button();
            this.tlpMain.SuspendLayout();
            this.grpModbus.SuspendLayout();
            this.tlpMb.SuspendLayout();
            this.grpRobotCfg.SuspendLayout();
            this.tlpRb.SuspendLayout();
            this.grpVisionP.SuspendLayout();
            this.tlpVp.SuspendLayout();
            this.grpDb.SuspendLayout();
            this.tlpDb.SuspendLayout();
            this.flpDb.SuspendLayout();
            this.tlpBottom.SuspendLayout();
            this.flpBtns.SuspendLayout();
            this.SuspendLayout();
            // 
            // tlpMain
            // 
            this.tlpMain.ColumnCount = 2;
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpMain.Controls.Add(this.grpModbus, 0, 0);
            this.tlpMain.Controls.Add(this.grpRobotCfg, 1, 0);
            this.tlpMain.Controls.Add(this.grpVisionP, 0, 1);
            this.tlpMain.Controls.Add(this.grpDb, 1, 1);
            this.tlpMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpMain.Location = new System.Drawing.Point(0, 0);
            this.tlpMain.Name = "tlpMain";
            this.tlpMain.RowCount = 2;
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpMain.Size = new System.Drawing.Size(1100, 578);
            this.tlpMain.TabIndex = 0;
            // 
            // grpModbus
            // 
            this.grpModbus.Controls.Add(this.tlpMb);
            this.grpModbus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpModbus.Location = new System.Drawing.Point(4, 4);
            this.grpModbus.Margin = new System.Windows.Forms.Padding(4);
            this.grpModbus.Name = "grpModbus";
            this.grpModbus.Padding = new System.Windows.Forms.Padding(8);
            this.grpModbus.Size = new System.Drawing.Size(542, 281);
            this.grpModbus.TabIndex = 0;
            this.grpModbus.TabStop = false;
            this.grpModbus.Text = "◆ Modbus传送带配置";
            // 
            // tlpMb
            // 
            this.tlpMb.ColumnCount = 2;
            this.tlpMb.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tlpMb.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpMb.Controls.Add(this.lblComPort, 0, 0);
            this.tlpMb.Controls.Add(this.txtComPort, 1, 0);
            this.tlpMb.Controls.Add(this.lblBaud, 0, 1);
            this.tlpMb.Controls.Add(this.txtBaud, 1, 1);
            this.tlpMb.Controls.Add(this.lblSlaveId, 0, 2);
            this.tlpMb.Controls.Add(this.txtSlaveId, 1, 2);
            this.tlpMb.Controls.Add(this.lblRegFwd, 0, 3);
            this.tlpMb.Controls.Add(this.txtRegFwd, 1, 3);
            this.tlpMb.Controls.Add(this.lblRegRev, 0, 4);
            this.tlpMb.Controls.Add(this.txtRegRev, 1, 4);
            this.tlpMb.Controls.Add(this.lblRegSpeed, 0, 5);
            this.tlpMb.Controls.Add(this.txtRegSpeed, 1, 5);
            this.tlpMb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpMb.Location = new System.Drawing.Point(8, 36);
            this.tlpMb.Name = "tlpMb";
            this.tlpMb.RowCount = 6;
            this.tlpMb.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpMb.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpMb.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpMb.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpMb.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpMb.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpMb.Size = new System.Drawing.Size(526, 237);
            this.tlpMb.TabIndex = 0;
            // 
            // lblComPort
            // 
            this.lblComPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblComPort.Location = new System.Drawing.Point(3, 0);
            this.lblComPort.Name = "lblComPort";
            this.lblComPort.Size = new System.Drawing.Size(144, 36);
            this.lblComPort.TabIndex = 0;
            this.lblComPort.Text = "串口号";
            this.lblComPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtComPort
            // 
            this.txtComPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtComPort.Location = new System.Drawing.Point(153, 6);
            this.txtComPort.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.txtComPort.Name = "txtComPort";
            this.txtComPort.Size = new System.Drawing.Size(370, 35);
            this.txtComPort.TabIndex = 1;
            this.txtComPort.Text = "COM5";
            this.txtComPort.Leave += new System.EventHandler(this.txtComPort_Leave);
            // 
            // lblBaud
            // 
            this.lblBaud.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBaud.Location = new System.Drawing.Point(3, 36);
            this.lblBaud.Name = "lblBaud";
            this.lblBaud.Size = new System.Drawing.Size(144, 36);
            this.lblBaud.TabIndex = 2;
            this.lblBaud.Text = "波特率";
            this.lblBaud.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtBaud
            // 
            this.txtBaud.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtBaud.Location = new System.Drawing.Point(153, 42);
            this.txtBaud.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.txtBaud.Name = "txtBaud";
            this.txtBaud.Size = new System.Drawing.Size(370, 35);
            this.txtBaud.TabIndex = 3;
            this.txtBaud.Text = "9600";
            this.txtBaud.Leave += new System.EventHandler(this.txtBaud_Leave);
            // 
            // lblSlaveId
            // 
            this.lblSlaveId.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSlaveId.Location = new System.Drawing.Point(3, 72);
            this.lblSlaveId.Name = "lblSlaveId";
            this.lblSlaveId.Size = new System.Drawing.Size(144, 36);
            this.lblSlaveId.TabIndex = 4;
            this.lblSlaveId.Text = "站号";
            this.lblSlaveId.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtSlaveId
            // 
            this.txtSlaveId.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSlaveId.Location = new System.Drawing.Point(153, 78);
            this.txtSlaveId.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.txtSlaveId.Name = "txtSlaveId";
            this.txtSlaveId.Size = new System.Drawing.Size(370, 35);
            this.txtSlaveId.TabIndex = 5;
            this.txtSlaveId.Text = "2";
            this.txtSlaveId.Leave += new System.EventHandler(this.txtSlaveId_Leave);
            // 
            // lblRegFwd
            // 
            this.lblRegFwd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRegFwd.Location = new System.Drawing.Point(3, 108);
            this.lblRegFwd.Name = "lblRegFwd";
            this.lblRegFwd.Size = new System.Drawing.Size(144, 36);
            this.lblRegFwd.TabIndex = 6;
            this.lblRegFwd.Text = "正转寄存器";
            this.lblRegFwd.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtRegFwd
            // 
            this.txtRegFwd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtRegFwd.Location = new System.Drawing.Point(153, 114);
            this.txtRegFwd.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.txtRegFwd.Name = "txtRegFwd";
            this.txtRegFwd.Size = new System.Drawing.Size(370, 35);
            this.txtRegFwd.TabIndex = 7;
            this.txtRegFwd.Text = "100";
            this.txtRegFwd.Leave += new System.EventHandler(this.txtRegFwd_Leave);
            // 
            // lblRegRev
            // 
            this.lblRegRev.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRegRev.Location = new System.Drawing.Point(3, 144);
            this.lblRegRev.Name = "lblRegRev";
            this.lblRegRev.Size = new System.Drawing.Size(144, 36);
            this.lblRegRev.TabIndex = 8;
            this.lblRegRev.Text = "反转寄存器";
            this.lblRegRev.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtRegRev
            // 
            this.txtRegRev.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtRegRev.Location = new System.Drawing.Point(153, 150);
            this.txtRegRev.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.txtRegRev.Name = "txtRegRev";
            this.txtRegRev.Size = new System.Drawing.Size(370, 35);
            this.txtRegRev.TabIndex = 9;
            this.txtRegRev.Text = "101";
            this.txtRegRev.Leave += new System.EventHandler(this.txtRegRev_Leave);
            // 
            // lblRegSpeed
            // 
            this.lblRegSpeed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRegSpeed.Location = new System.Drawing.Point(3, 180);
            this.lblRegSpeed.Name = "lblRegSpeed";
            this.lblRegSpeed.Size = new System.Drawing.Size(144, 57);
            this.lblRegSpeed.TabIndex = 10;
            this.lblRegSpeed.Text = "速度寄存器";
            this.lblRegSpeed.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtRegSpeed
            // 
            this.txtRegSpeed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtRegSpeed.Location = new System.Drawing.Point(153, 186);
            this.txtRegSpeed.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.txtRegSpeed.Name = "txtRegSpeed";
            this.txtRegSpeed.Size = new System.Drawing.Size(370, 35);
            this.txtRegSpeed.TabIndex = 11;
            this.txtRegSpeed.Text = "102";
            this.txtRegSpeed.Leave += new System.EventHandler(this.txtRegSpeed_Leave);
            // 
            // grpRobotCfg
            // 
            this.grpRobotCfg.Controls.Add(this.tlpRb);
            this.grpRobotCfg.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpRobotCfg.Location = new System.Drawing.Point(554, 4);
            this.grpRobotCfg.Margin = new System.Windows.Forms.Padding(4);
            this.grpRobotCfg.Name = "grpRobotCfg";
            this.grpRobotCfg.Padding = new System.Windows.Forms.Padding(8);
            this.grpRobotCfg.Size = new System.Drawing.Size(542, 281);
            this.grpRobotCfg.TabIndex = 1;
            this.grpRobotCfg.TabStop = false;
            this.grpRobotCfg.Text = "◆ 机械臂配置";
            // 
            // tlpRb
            // 
            this.tlpRb.ColumnCount = 2;
            this.tlpRb.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tlpRb.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpRb.Controls.Add(this.lblRbCom, 0, 0);
            this.tlpRb.Controls.Add(this.txtRbCom, 1, 0);
            this.tlpRb.Controls.Add(this.lblRbPort, 0, 1);
            this.tlpRb.Controls.Add(this.txtRbPort, 1, 1);
            this.tlpRb.Controls.Add(this.lblPickD, 0, 2);
            this.tlpRb.Controls.Add(this.txtPickDelay, 1, 2);
            this.tlpRb.Controls.Add(this.lblHomeD, 0, 3);
            this.tlpRb.Controls.Add(this.txtHomeDelay, 1, 3);
            this.tlpRb.Controls.Add(this.lblAutoBridge, 0, 4);
            this.tlpRb.Controls.Add(this.chkAutoBridge, 1, 4);
            this.tlpRb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpRb.Location = new System.Drawing.Point(8, 36);
            this.tlpRb.Name = "tlpRb";
            this.tlpRb.RowCount = 5;
            this.tlpRb.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpRb.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpRb.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpRb.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpRb.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpRb.Size = new System.Drawing.Size(526, 237);
            this.tlpRb.TabIndex = 0;
            // 
            // lblRbCom
            // 
            this.lblRbCom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRbCom.Location = new System.Drawing.Point(3, 0);
            this.lblRbCom.Name = "lblRbCom";
            this.lblRbCom.Size = new System.Drawing.Size(144, 36);
            this.lblRbCom.TabIndex = 0;
            this.lblRbCom.Text = "串口号";
            this.lblRbCom.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtRbCom
            // 
            this.txtRbCom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtRbCom.Location = new System.Drawing.Point(153, 6);
            this.txtRbCom.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.txtRbCom.Name = "txtRbCom";
            this.txtRbCom.Size = new System.Drawing.Size(370, 35);
            this.txtRbCom.TabIndex = 1;
            this.txtRbCom.Text = "COM3";
            this.txtRbCom.Leave += new System.EventHandler(this.txtRbCom_Leave);
            // 
            // lblRbPort
            // 
            this.lblRbPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRbPort.Location = new System.Drawing.Point(3, 36);
            this.lblRbPort.Name = "lblRbPort";
            this.lblRbPort.Size = new System.Drawing.Size(144, 36);
            this.lblRbPort.TabIndex = 2;
            this.lblRbPort.Text = "桥接端口";
            this.lblRbPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtRbPort
            // 
            this.txtRbPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtRbPort.Location = new System.Drawing.Point(153, 42);
            this.txtRbPort.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.txtRbPort.Name = "txtRbPort";
            this.txtRbPort.Size = new System.Drawing.Size(370, 35);
            this.txtRbPort.TabIndex = 3;
            this.txtRbPort.Text = "18899";
            this.txtRbPort.Leave += new System.EventHandler(this.txtRbPort_Leave);
            // 
            // lblPickD
            // 
            this.lblPickD.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickD.Location = new System.Drawing.Point(3, 72);
            this.lblPickD.Name = "lblPickD";
            this.lblPickD.Size = new System.Drawing.Size(144, 36);
            this.lblPickD.TabIndex = 4;
            this.lblPickD.Text = "抓取延时(ms)";
            this.lblPickD.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtPickDelay
            // 
            this.txtPickDelay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPickDelay.Location = new System.Drawing.Point(153, 78);
            this.txtPickDelay.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.txtPickDelay.Name = "txtPickDelay";
            this.txtPickDelay.Size = new System.Drawing.Size(370, 35);
            this.txtPickDelay.TabIndex = 5;
            this.txtPickDelay.Text = "500";
            this.txtPickDelay.Leave += new System.EventHandler(this.txtPickDelay_Leave);
            // 
            // lblHomeD
            // 
            this.lblHomeD.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHomeD.Location = new System.Drawing.Point(3, 108);
            this.lblHomeD.Name = "lblHomeD";
            this.lblHomeD.Size = new System.Drawing.Size(144, 36);
            this.lblHomeD.TabIndex = 6;
            this.lblHomeD.Text = "复位延时(ms)";
            this.lblHomeD.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtHomeDelay
            // 
            this.txtHomeDelay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtHomeDelay.Location = new System.Drawing.Point(153, 114);
            this.txtHomeDelay.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.txtHomeDelay.Name = "txtHomeDelay";
            this.txtHomeDelay.Size = new System.Drawing.Size(370, 35);
            this.txtHomeDelay.TabIndex = 7;
            this.txtHomeDelay.Text = "800";
            this.txtHomeDelay.Leave += new System.EventHandler(this.txtHomeDelay_Leave);
            // 
            // lblAutoBridge
            // 
            this.lblAutoBridge.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAutoBridge.Location = new System.Drawing.Point(3, 144);
            this.lblAutoBridge.Name = "lblAutoBridge";
            this.lblAutoBridge.Size = new System.Drawing.Size(144, 93);
            this.lblAutoBridge.TabIndex = 8;
            this.lblAutoBridge.Text = "自动启动桥接进程";
            this.lblAutoBridge.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // chkAutoBridge
            // 
            this.chkAutoBridge.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkAutoBridge.Location = new System.Drawing.Point(153, 150);
            this.chkAutoBridge.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.chkAutoBridge.Name = "chkAutoBridge";
            this.chkAutoBridge.Size = new System.Drawing.Size(370, 81);
            this.chkAutoBridge.TabIndex = 9;
            this.chkAutoBridge.Text = "ArmBridge\\DobotArmBridge.exe";
            this.chkAutoBridge.UseVisualStyleBackColor = true;
            this.chkAutoBridge.CheckedChanged += new System.EventHandler(this.chkAutoBridge_CheckedChanged);
            // 
            // grpVisionP
            // 
            this.grpVisionP.Controls.Add(this.tlpVp);
            this.grpVisionP.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpVisionP.Location = new System.Drawing.Point(4, 293);
            this.grpVisionP.Margin = new System.Windows.Forms.Padding(4);
            this.grpVisionP.Name = "grpVisionP";
            this.grpVisionP.Padding = new System.Windows.Forms.Padding(8);
            this.grpVisionP.Size = new System.Drawing.Size(542, 281);
            this.grpVisionP.TabIndex = 2;
            this.grpVisionP.TabStop = false;
            this.grpVisionP.Text = "◆ 视觉参数配置";
            // 
            // tlpVp
            // 
            this.tlpVp.ColumnCount = 2;
            this.tlpVp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.tlpVp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpVp.Controls.Add(this.lblThr, 0, 0);
            this.tlpVp.Controls.Add(this.txtThreshold, 1, 0);
            this.tlpVp.Controls.Add(this.lblStopD, 0, 1);
            this.tlpVp.Controls.Add(this.txtStopDelay, 1, 1);
            this.tlpVp.Controls.Add(this.lblPhotoW, 0, 2);
            this.tlpVp.Controls.Add(this.txtPhotoWait, 1, 2);
            this.tlpVp.Controls.Add(this.lblTimeout, 0, 3);
            this.tlpVp.Controls.Add(this.txtTimeout, 1, 3);
            this.tlpVp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpVp.Location = new System.Drawing.Point(8, 36);
            this.tlpVp.Name = "tlpVp";
            this.tlpVp.RowCount = 4;
            this.tlpVp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpVp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpVp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpVp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpVp.Size = new System.Drawing.Size(526, 237);
            this.tlpVp.TabIndex = 0;
            // 
            // lblThr
            // 
            this.lblThr.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblThr.Location = new System.Drawing.Point(3, 0);
            this.lblThr.Name = "lblThr";
            this.lblThr.Size = new System.Drawing.Size(174, 36);
            this.lblThr.TabIndex = 0;
            this.lblThr.Text = "匹配分数阈值";
            this.lblThr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtThreshold
            // 
            this.txtThreshold.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtThreshold.Location = new System.Drawing.Point(183, 6);
            this.txtThreshold.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.txtThreshold.Name = "txtThreshold";
            this.txtThreshold.Size = new System.Drawing.Size(340, 35);
            this.txtThreshold.TabIndex = 1;
            this.txtThreshold.Text = "0.70";
            this.txtThreshold.Leave += new System.EventHandler(this.txtThreshold_Leave);
            // 
            // lblStopD
            // 
            this.lblStopD.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStopD.Location = new System.Drawing.Point(3, 36);
            this.lblStopD.Name = "lblStopD";
            this.lblStopD.Size = new System.Drawing.Size(174, 36);
            this.lblStopD.TabIndex = 2;
            this.lblStopD.Text = "光电触发后停止延时(ms)";
            this.lblStopD.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtStopDelay
            // 
            this.txtStopDelay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtStopDelay.Location = new System.Drawing.Point(183, 42);
            this.txtStopDelay.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.txtStopDelay.Name = "txtStopDelay";
            this.txtStopDelay.Size = new System.Drawing.Size(340, 35);
            this.txtStopDelay.TabIndex = 3;
            this.txtStopDelay.Text = "300";
            this.txtStopDelay.Leave += new System.EventHandler(this.txtStopDelay_Leave);
            // 
            // lblPhotoW
            // 
            this.lblPhotoW.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPhotoW.Location = new System.Drawing.Point(3, 72);
            this.lblPhotoW.Name = "lblPhotoW";
            this.lblPhotoW.Size = new System.Drawing.Size(174, 36);
            this.lblPhotoW.TabIndex = 4;
            this.lblPhotoW.Text = "拍照等待时间(ms)";
            this.lblPhotoW.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtPhotoWait
            // 
            this.txtPhotoWait.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPhotoWait.Location = new System.Drawing.Point(183, 78);
            this.txtPhotoWait.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.txtPhotoWait.Name = "txtPhotoWait";
            this.txtPhotoWait.Size = new System.Drawing.Size(340, 35);
            this.txtPhotoWait.TabIndex = 5;
            this.txtPhotoWait.Text = "100";
            this.txtPhotoWait.Leave += new System.EventHandler(this.txtPhotoWait_Leave);
            // 
            // lblTimeout
            // 
            this.lblTimeout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTimeout.Location = new System.Drawing.Point(3, 108);
            this.lblTimeout.Name = "lblTimeout";
            this.lblTimeout.Size = new System.Drawing.Size(174, 129);
            this.lblTimeout.TabIndex = 6;
            this.lblTimeout.Text = "检测超时(ms)";
            this.lblTimeout.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtTimeout
            // 
            this.txtTimeout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtTimeout.Location = new System.Drawing.Point(183, 114);
            this.txtTimeout.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.txtTimeout.Name = "txtTimeout";
            this.txtTimeout.Size = new System.Drawing.Size(340, 35);
            this.txtTimeout.TabIndex = 7;
            this.txtTimeout.Text = "2000";
            this.txtTimeout.Leave += new System.EventHandler(this.txtTimeout_Leave);
            // 
            // grpDb
            // 
            this.grpDb.Controls.Add(this.tlpDb);
            this.grpDb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpDb.Location = new System.Drawing.Point(554, 293);
            this.grpDb.Margin = new System.Windows.Forms.Padding(4);
            this.grpDb.Name = "grpDb";
            this.grpDb.Padding = new System.Windows.Forms.Padding(8);
            this.grpDb.Size = new System.Drawing.Size(542, 281);
            this.grpDb.TabIndex = 3;
            this.grpDb.TabStop = false;
            this.grpDb.Text = "◆ 数据库配置";
            // 
            // tlpDb
            // 
            this.tlpDb.ColumnCount = 2;
            this.tlpDb.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tlpDb.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpDb.Controls.Add(this.lblDbType, 0, 0);
            this.tlpDb.Controls.Add(this.cmbDbType, 1, 0);
            this.tlpDb.Controls.Add(this.lblConn, 0, 1);
            this.tlpDb.Controls.Add(this.txtConnStr, 1, 1);
            this.tlpDb.Controls.Add(this.flpDb, 1, 2);
            this.tlpDb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpDb.Location = new System.Drawing.Point(8, 36);
            this.tlpDb.Name = "tlpDb";
            this.tlpDb.RowCount = 3;
            this.tlpDb.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpDb.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpDb.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tlpDb.Size = new System.Drawing.Size(526, 237);
            this.tlpDb.TabIndex = 0;
            // 
            // lblDbType
            // 
            this.lblDbType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDbType.Location = new System.Drawing.Point(3, 0);
            this.lblDbType.Name = "lblDbType";
            this.lblDbType.Size = new System.Drawing.Size(144, 36);
            this.lblDbType.TabIndex = 0;
            this.lblDbType.Text = "数据库类型";
            this.lblDbType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cmbDbType
            // 
            this.cmbDbType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbDbType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDbType.FormattingEnabled = true;
            this.cmbDbType.Items.AddRange(new object[] {
            "MySQL"});
            this.cmbDbType.Location = new System.Drawing.Point(153, 6);
            this.cmbDbType.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.cmbDbType.Name = "cmbDbType";
            this.cmbDbType.Size = new System.Drawing.Size(370, 36);
            this.cmbDbType.TabIndex = 1;
            // 
            // lblConn
            // 
            this.lblConn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblConn.Location = new System.Drawing.Point(3, 36);
            this.lblConn.Name = "lblConn";
            this.lblConn.Size = new System.Drawing.Size(144, 36);
            this.lblConn.TabIndex = 2;
            this.lblConn.Text = "连接字符串";
            this.lblConn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtConnStr
            // 
            this.txtConnStr.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtConnStr.Location = new System.Drawing.Point(153, 42);
            this.txtConnStr.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.txtConnStr.Name = "txtConnStr";
            this.txtConnStr.Size = new System.Drawing.Size(370, 35);
            this.txtConnStr.TabIndex = 3;
            this.txtConnStr.Text = resources.GetString("txtConnStr.Text");
            this.txtConnStr.Leave += new System.EventHandler(this.txtConnStr_Leave);
            // 
            // flpDb
            // 
            this.flpDb.Controls.Add(this.btnTestConn);
            this.flpDb.Controls.Add(this.btnInitDb);
            this.flpDb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpDb.Location = new System.Drawing.Point(150, 72);
            this.flpDb.Margin = new System.Windows.Forms.Padding(0);
            this.flpDb.Name = "flpDb";
            this.flpDb.Size = new System.Drawing.Size(376, 165);
            this.flpDb.TabIndex = 4;
            // 
            // btnTestConn
            // 
            this.btnTestConn.Location = new System.Drawing.Point(3, 6);
            this.btnTestConn.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.btnTestConn.Name = "btnTestConn";
            this.btnTestConn.Size = new System.Drawing.Size(110, 30);
            this.btnTestConn.TabIndex = 0;
            this.btnTestConn.Text = "测试连接";
            this.btnTestConn.UseVisualStyleBackColor = true;
            this.btnTestConn.Click += new System.EventHandler(this.btnTestConn_Click);
            // 
            // btnInitDb
            // 
            this.btnInitDb.Location = new System.Drawing.Point(119, 6);
            this.btnInitDb.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.btnInitDb.Name = "btnInitDb";
            this.btnInitDb.Size = new System.Drawing.Size(110, 30);
            this.btnInitDb.TabIndex = 1;
            this.btnInitDb.Text = "初始化建表";
            this.btnInitDb.UseVisualStyleBackColor = true;
            this.btnInitDb.Click += new System.EventHandler(this.btnInitDb_Click);
            // 
            // tlpBottom
            // 
            this.tlpBottom.ColumnCount = 3;
            this.tlpBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpBottom.Controls.Add(this.flpBtns, 1, 0);
            this.tlpBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tlpBottom.Location = new System.Drawing.Point(0, 578);
            this.tlpBottom.Name = "tlpBottom";
            this.tlpBottom.RowCount = 1;
            this.tlpBottom.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpBottom.Size = new System.Drawing.Size(1100, 62);
            this.tlpBottom.TabIndex = 1;
            // 
            // flpBtns
            // 
            this.flpBtns.Controls.Add(this.btnSaveParam);
            this.flpBtns.Controls.Add(this.btnLoadParam);
            this.flpBtns.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpBtns.Location = new System.Drawing.Point(410, 3);
            this.flpBtns.Name = "flpBtns";
            this.flpBtns.Size = new System.Drawing.Size(280, 56);
            this.flpBtns.TabIndex = 0;
            // 
            // btnSaveParam
            // 
            this.btnSaveParam.BackColor = System.Drawing.Color.DodgerBlue;
            this.btnSaveParam.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveParam.ForeColor = System.Drawing.Color.White;
            this.btnSaveParam.Location = new System.Drawing.Point(8, 8);
            this.btnSaveParam.Margin = new System.Windows.Forms.Padding(8);
            this.btnSaveParam.Name = "btnSaveParam";
            this.btnSaveParam.Size = new System.Drawing.Size(120, 36);
            this.btnSaveParam.TabIndex = 0;
            this.btnSaveParam.Text = "保存参数";
            this.btnSaveParam.UseVisualStyleBackColor = false;
            this.btnSaveParam.Click += new System.EventHandler(this.btnSaveParam_Click);
            // 
            // btnLoadParam
            // 
            this.btnLoadParam.Location = new System.Drawing.Point(144, 8);
            this.btnLoadParam.Margin = new System.Windows.Forms.Padding(8);
            this.btnLoadParam.Name = "btnLoadParam";
            this.btnLoadParam.Size = new System.Drawing.Size(120, 36);
            this.btnLoadParam.TabIndex = 1;
            this.btnLoadParam.Text = "加载参数";
            this.btnLoadParam.UseVisualStyleBackColor = true;
            this.btnLoadParam.Click += new System.EventHandler(this.btnLoadParam_Click);
            // 
            // SysConfigView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 28F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tlpMain);
            this.Controls.Add(this.tlpBottom);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Name = "SysConfigView";
            this.Size = new System.Drawing.Size(1100, 640);
            this.tlpMain.ResumeLayout(false);
            this.grpModbus.ResumeLayout(false);
            this.tlpMb.ResumeLayout(false);
            this.tlpMb.PerformLayout();
            this.grpRobotCfg.ResumeLayout(false);
            this.tlpRb.ResumeLayout(false);
            this.tlpRb.PerformLayout();
            this.grpVisionP.ResumeLayout(false);
            this.tlpVp.ResumeLayout(false);
            this.tlpVp.PerformLayout();
            this.grpDb.ResumeLayout(false);
            this.tlpDb.ResumeLayout(false);
            this.tlpDb.PerformLayout();
            this.flpDb.ResumeLayout(false);
            this.tlpBottom.ResumeLayout(false);
            this.flpBtns.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tlpMain;
        private System.Windows.Forms.GroupBox grpModbus;
        private System.Windows.Forms.TableLayoutPanel tlpMb;
        private System.Windows.Forms.Label lblComPort;
        private System.Windows.Forms.TextBox txtComPort;
        private System.Windows.Forms.Label lblBaud;
        private System.Windows.Forms.TextBox txtBaud;
        private System.Windows.Forms.Label lblSlaveId;
        private System.Windows.Forms.TextBox txtSlaveId;
        private System.Windows.Forms.Label lblRegFwd;
        private System.Windows.Forms.TextBox txtRegFwd;
        private System.Windows.Forms.Label lblRegRev;
        private System.Windows.Forms.TextBox txtRegRev;
        private System.Windows.Forms.Label lblRegSpeed;
        private System.Windows.Forms.TextBox txtRegSpeed;
        private System.Windows.Forms.GroupBox grpRobotCfg;
        private System.Windows.Forms.TableLayoutPanel tlpRb;
        private System.Windows.Forms.Label lblRbCom;
        private System.Windows.Forms.TextBox txtRbCom;
        private System.Windows.Forms.Label lblRbPort;
        private System.Windows.Forms.TextBox txtRbPort;
        private System.Windows.Forms.Label lblPickD;
        private System.Windows.Forms.TextBox txtPickDelay;
        private System.Windows.Forms.Label lblHomeD;
        private System.Windows.Forms.TextBox txtHomeDelay;
        private System.Windows.Forms.Label lblAutoBridge;
        private System.Windows.Forms.CheckBox chkAutoBridge;
        private System.Windows.Forms.GroupBox grpVisionP;
        private System.Windows.Forms.TableLayoutPanel tlpVp;
        private System.Windows.Forms.Label lblThr;
        private System.Windows.Forms.TextBox txtThreshold;
        private System.Windows.Forms.Label lblStopD;
        private System.Windows.Forms.TextBox txtStopDelay;
        private System.Windows.Forms.Label lblPhotoW;
        private System.Windows.Forms.TextBox txtPhotoWait;
        private System.Windows.Forms.Label lblTimeout;
        private System.Windows.Forms.TextBox txtTimeout;
        private System.Windows.Forms.GroupBox grpDb;
        private System.Windows.Forms.TableLayoutPanel tlpDb;
        private System.Windows.Forms.Label lblDbType;
        private System.Windows.Forms.ComboBox cmbDbType;
        private System.Windows.Forms.Label lblConn;
        private System.Windows.Forms.TextBox txtConnStr;
        private System.Windows.Forms.FlowLayoutPanel flpDb;
        private System.Windows.Forms.Button btnTestConn;
        private System.Windows.Forms.Button btnInitDb;
        private System.Windows.Forms.TableLayoutPanel tlpBottom;
        private System.Windows.Forms.FlowLayoutPanel flpBtns;
        private System.Windows.Forms.Button btnSaveParam;
        private System.Windows.Forms.Button btnLoadParam;
    }
}

