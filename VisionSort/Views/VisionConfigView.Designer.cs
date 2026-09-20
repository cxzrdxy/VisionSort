namespace VisionSort.Views
{
    partial class VisionConfigView
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VisionConfigView));
            this.tlpMain = new System.Windows.Forms.TableLayoutPanel();
            this.grpCamera = new System.Windows.Forms.GroupBox();
            this.pnlTrig = new System.Windows.Forms.Panel();
            this.cmbTrigMode = new System.Windows.Forms.ComboBox();
            this.lblTrigMode = new System.Windows.Forms.Label();
            this.pnlPkt = new System.Windows.Forms.Panel();
            this.lblLatency = new System.Windows.Forms.Label();
            this.cmbLatency = new System.Windows.Forms.ComboBox();
            this.lblPacket = new System.Windows.Forms.Label();
            this.cmbPacket = new System.Windows.Forms.ComboBox();
            this.pnlExp = new System.Windows.Forms.Panel();
            this.lblExp = new System.Windows.Forms.Label();
            this.txtExposure = new System.Windows.Forms.TextBox();
            this.lblGain = new System.Windows.Forms.Label();
            this.cmbGain = new System.Windows.Forms.ComboBox();
            this.flpCam = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCamConn = new System.Windows.Forms.Button();
            this.btnCamDis = new System.Windows.Forms.Button();
            this.btnLive = new System.Windows.Forms.Button();
            this.btnStopLive = new System.Windows.Forms.Button();
            this.btnSnap = new System.Windows.Forms.Button();
            this.grpVpp = new System.Windows.Forms.GroupBox();
            this.pnlVppHost = new System.Windows.Forms.Panel();
            this.lblVppTip = new System.Windows.Forms.Label();
            this.lblVppPath = new System.Windows.Forms.Label();
            this.flpVpp = new System.Windows.Forms.FlowLayoutPanel();
            this.btnLoadVpp = new System.Windows.Forms.Button();
            this.btnSaveVpp = new System.Windows.Forms.Button();
            this.grpPma = new System.Windows.Forms.GroupBox();
            this.pnlPmaRight = new System.Windows.Forms.Panel();
            this.lblTplScore = new System.Windows.Forms.Label();
            this.pnlPyr = new System.Windows.Forms.Panel();
            this.lblPyr = new System.Windows.Forms.Label();
            this.cmbGranularity = new System.Windows.Forms.ComboBox();
            this.pnlAng = new System.Windows.Forms.Panel();
            this.lblAng = new System.Windows.Forms.Label();
            this.cmbAngle = new System.Windows.Forms.ComboBox();
            this.pnlThr = new System.Windows.Forms.Panel();
            this.lblThr = new System.Windows.Forms.Label();
            this.cmbThreshold = new System.Windows.Forms.ComboBox();
            this.pnlPmaTool = new System.Windows.Forms.Panel();
            this.lblPmaTool = new System.Windows.Forms.Label();
            this.cmbPmaTool = new System.Windows.Forms.ComboBox();
            this.flpPma = new System.Windows.Forms.FlowLayoutPanel();
            this.btnGrabTpl = new System.Windows.Forms.Button();
            this.btnTrain = new System.Windows.Forms.Button();
            this.btnSaveTpl = new System.Windows.Forms.Button();
            this.picTemplate = new Cognex.VisionPro.CogRecordDisplay();
            this.grpSave = new System.Windows.Forms.GroupBox();
            this.pnlBrowse = new System.Windows.Forms.Panel();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.pnlFmt = new System.Windows.Forms.Panel();
            this.lblFmt = new System.Windows.Forms.Label();
            this.cmbImgFmt = new System.Windows.Forms.ComboBox();
            this.lblQuota = new System.Windows.Forms.Label();
            this.txtSavePath = new System.Windows.Forms.TextBox();
            this.pnlChk = new System.Windows.Forms.Panel();
            this.chkSaveOK = new System.Windows.Forms.CheckBox();
            this.chkSaveNG = new System.Windows.Forms.CheckBox();
            this.tlpMain.SuspendLayout();
            this.grpCamera.SuspendLayout();
            this.pnlTrig.SuspendLayout();
            this.pnlPkt.SuspendLayout();
            this.pnlExp.SuspendLayout();
            this.flpCam.SuspendLayout();
            this.grpVpp.SuspendLayout();
            this.pnlVppHost.SuspendLayout();
            this.flpVpp.SuspendLayout();
            this.grpPma.SuspendLayout();
            this.pnlPmaRight.SuspendLayout();
            this.pnlPyr.SuspendLayout();
            this.pnlAng.SuspendLayout();
            this.pnlThr.SuspendLayout();
            this.pnlPmaTool.SuspendLayout();
            this.flpPma.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picTemplate)).BeginInit();
            this.grpSave.SuspendLayout();
            this.pnlBrowse.SuspendLayout();
            this.pnlFmt.SuspendLayout();
            this.pnlChk.SuspendLayout();
            this.SuspendLayout();
            // 
            // tlpMain
            // 
            this.tlpMain.ColumnCount = 2;
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpMain.Controls.Add(this.grpCamera, 0, 0);
            this.tlpMain.Controls.Add(this.grpVpp, 1, 0);
            this.tlpMain.Controls.Add(this.grpPma, 0, 1);
            this.tlpMain.Controls.Add(this.grpSave, 1, 1);
            this.tlpMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpMain.Location = new System.Drawing.Point(0, 0);
            this.tlpMain.Name = "tlpMain";
            this.tlpMain.RowCount = 2;
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpMain.Size = new System.Drawing.Size(1100, 640);
            this.tlpMain.TabIndex = 0;
            // 
            // grpCamera
            // 
            this.grpCamera.Controls.Add(this.pnlTrig);
            this.grpCamera.Controls.Add(this.pnlPkt);
            this.grpCamera.Controls.Add(this.pnlExp);
            this.grpCamera.Controls.Add(this.flpCam);
            this.grpCamera.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpCamera.Location = new System.Drawing.Point(4, 4);
            this.grpCamera.Margin = new System.Windows.Forms.Padding(4);
            this.grpCamera.Name = "grpCamera";
            this.grpCamera.Size = new System.Drawing.Size(542, 312);
            this.grpCamera.TabIndex = 0;
            this.grpCamera.TabStop = false;
            this.grpCamera.Text = "相机设置";
            // 
            // pnlTrig
            // 
            this.pnlTrig.Controls.Add(this.cmbTrigMode);
            this.pnlTrig.Controls.Add(this.lblTrigMode);
            this.pnlTrig.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTrig.Location = new System.Drawing.Point(3, 183);
            this.pnlTrig.Name = "pnlTrig";
            this.pnlTrig.Size = new System.Drawing.Size(536, 38);
            this.pnlTrig.TabIndex = 3;
            // 
            // cmbTrigMode
            // 
            this.cmbTrigMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTrigMode.FormattingEnabled = true;
            this.cmbTrigMode.Items.AddRange(new object[] {
            "自动探测（保持相机原配置）",
            "强制：自动触发（外部硬触发）",
            "强制：自由运行",
            "强制：手动（软件触发）"});
            this.cmbTrigMode.Location = new System.Drawing.Point(8, 6);
            this.cmbTrigMode.Name = "cmbTrigMode";
            this.cmbTrigMode.Size = new System.Drawing.Size(230, 36);
            this.cmbTrigMode.TabIndex = 0;
            this.cmbTrigMode.SelectedIndex = 0;
            this.cmbTrigMode.SelectedIndexChanged += new System.EventHandler(this.cmbTrigMode_SelectedIndexChanged);
            // 
            // lblTrigMode
            // 
            this.lblTrigMode.AutoSize = true;
            this.lblTrigMode.ForeColor = System.Drawing.Color.Gray;
            this.lblTrigMode.Location = new System.Drawing.Point(246, 10);
            this.lblTrigMode.Name = "lblTrigMode";
            this.lblTrigMode.Size = new System.Drawing.Size(362, 28);
            this.lblTrigMode.TabIndex = 1;
            this.lblTrigMode.Text = "实际：未连接";
            // 
            // pnlPkt
            // 
            this.pnlPkt.Controls.Add(this.lblLatency);
            this.pnlPkt.Controls.Add(this.cmbLatency);
            this.pnlPkt.Controls.Add(this.lblPacket);
            this.pnlPkt.Controls.Add(this.cmbPacket);
            this.pnlPkt.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlPkt.Location = new System.Drawing.Point(3, 153);
            this.pnlPkt.Name = "pnlPkt";
            this.pnlPkt.Size = new System.Drawing.Size(536, 38);
            this.pnlPkt.TabIndex = 2;
            // 
            // lblLatency
            // 
            this.lblLatency.AutoSize = true;
            this.lblLatency.Location = new System.Drawing.Point(8, 10);
            this.lblLatency.Name = "lblLatency";
            this.lblLatency.Size = new System.Drawing.Size(96, 28);
            this.lblLatency.TabIndex = 0;
            this.lblLatency.Text = "延迟级别";
            // 
            // cmbLatency
            // 
            this.cmbLatency.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLatency.FormattingEnabled = true;
            // 与相机 LatencyLevel 1:1（VisionPro 自家 GigE 页显示的也是 0-3 原值），不做偏移换算
            this.cmbLatency.Items.AddRange(new object[] {
            "0",
            "1",
            "2",
            "3"});
            this.cmbLatency.Location = new System.Drawing.Point(96, 6);
            this.cmbLatency.Name = "cmbLatency";
            this.cmbLatency.Size = new System.Drawing.Size(110, 36);
            this.cmbLatency.TabIndex = 1;
            this.cmbLatency.SelectedIndex = 0;
            this.cmbLatency.SelectedIndexChanged += new System.EventHandler(this.cmbLatency_SelectedIndexChanged);
            // 
            // lblPacket
            // 
            this.lblPacket.AutoSize = true;
            this.lblPacket.Location = new System.Drawing.Point(216, 10);
            this.lblPacket.Name = "lblPacket";
            this.lblPacket.Size = new System.Drawing.Size(75, 28);
            this.lblPacket.TabIndex = 2;
            this.lblPacket.Text = "包大小";
            // 
            // cmbPacket
            // 
            this.cmbPacket.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPacket.FormattingEnabled = true;
            // 首项 = 标准 1500 MTU 的 GigE 包大小（VisionPro 现场实测值 1508）；连上后会被相机现值覆盖
            this.cmbPacket.Items.AddRange(new object[] {
            "1508",
            "4096",
            "8192",
            "9000"});
            this.cmbPacket.Location = new System.Drawing.Point(300, 6);
            this.cmbPacket.Name = "cmbPacket";
            this.cmbPacket.Size = new System.Drawing.Size(120, 36);
            this.cmbPacket.TabIndex = 3;
            this.cmbPacket.SelectedIndex = 0;
            this.cmbPacket.SelectedIndexChanged += new System.EventHandler(this.cmbPacket_SelectedIndexChanged);
            // 
            // pnlExp
            // 
            this.pnlExp.Controls.Add(this.lblExp);
            this.pnlExp.Controls.Add(this.txtExposure);
            this.pnlExp.Controls.Add(this.lblGain);
            this.pnlExp.Controls.Add(this.cmbGain);
            this.pnlExp.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlExp.Location = new System.Drawing.Point(3, 115);
            this.pnlExp.Name = "pnlExp";
            this.pnlExp.Size = new System.Drawing.Size(536, 38);
            this.pnlExp.TabIndex = 1;
            // 
            // lblExp
            // 
            this.lblExp.AutoSize = true;
            this.lblExp.Location = new System.Drawing.Point(8, 10);
            this.lblExp.Name = "lblExp";
            this.lblExp.Size = new System.Drawing.Size(98, 28);
            this.lblExp.TabIndex = 0;
            this.lblExp.Text = "曝光(ms)";
            // 
            // txtExposure
            // 
            this.txtExposure.Location = new System.Drawing.Point(96, 7);
            this.txtExposure.Name = "txtExposure";
            this.txtExposure.Size = new System.Drawing.Size(110, 35);
            this.txtExposure.TabIndex = 1;
            this.txtExposure.Text = "8.0";
            this.txtExposure.Leave += new System.EventHandler(this.txtExposure_Leave);
            // 
            // lblGain
            // 
            this.lblGain.AutoSize = true;
            this.lblGain.Location = new System.Drawing.Point(216, 10);
            this.lblGain.Name = "lblGain";
            this.lblGain.Size = new System.Drawing.Size(94, 28);
            this.lblGain.TabIndex = 2;
            this.lblGain.Text = "增益(dB)";
            // 
            // cmbGain
            // 
            this.cmbGain.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbGain.FormattingEnabled = true;
            this.cmbGain.Items.AddRange(new object[] {
            "0",
            "3",
            "6",
            "12"});
            this.cmbGain.Location = new System.Drawing.Point(300, 6);
            this.cmbGain.Name = "cmbGain";
            this.cmbGain.Size = new System.Drawing.Size(120, 36);
            this.cmbGain.TabIndex = 3;
            this.cmbGain.SelectedIndex = 0;
            this.cmbGain.SelectedIndexChanged += new System.EventHandler(this.cmbGain_SelectedIndexChanged);
            // 
            // flpCam
            // 
            this.flpCam.Controls.Add(this.btnCamConn);
            this.flpCam.Controls.Add(this.btnCamDis);
            this.flpCam.Controls.Add(this.btnLive);
            this.flpCam.Controls.Add(this.btnStopLive);
            this.flpCam.Controls.Add(this.btnSnap);
            this.flpCam.Dock = System.Windows.Forms.DockStyle.Top;
            this.flpCam.Location = new System.Drawing.Point(3, 31);
            this.flpCam.Name = "flpCam";
            this.flpCam.Padding = new System.Windows.Forms.Padding(4);
            this.flpCam.Size = new System.Drawing.Size(536, 84);
            this.flpCam.TabIndex = 0;
            // 
            // btnCamConn
            // 
            this.btnCamConn.BackColor = System.Drawing.Color.DodgerBlue;
            this.btnCamConn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCamConn.ForeColor = System.Drawing.Color.White;
            this.btnCamConn.Location = new System.Drawing.Point(7, 7);
            this.btnCamConn.Name = "btnCamConn";
            this.btnCamConn.Size = new System.Drawing.Size(100, 32);
            this.btnCamConn.TabIndex = 0;
            this.btnCamConn.Text = "连接相机";
            this.btnCamConn.UseVisualStyleBackColor = false;
            this.btnCamConn.Click += new System.EventHandler(this.btnCamConn_Click);
            // 
            // btnCamDis
            // 
            this.btnCamDis.Location = new System.Drawing.Point(113, 7);
            this.btnCamDis.Name = "btnCamDis";
            this.btnCamDis.Size = new System.Drawing.Size(100, 32);
            this.btnCamDis.TabIndex = 1;
            this.btnCamDis.Text = "断开相机";
            this.btnCamDis.UseVisualStyleBackColor = true;
            this.btnCamDis.Enabled = false;
            this.btnCamDis.Click += new System.EventHandler(this.btnCamDis_Click);
            // 
            // btnLive
            // 
            this.btnLive.Location = new System.Drawing.Point(219, 7);
            this.btnLive.Name = "btnLive";
            this.btnLive.Size = new System.Drawing.Size(100, 32);
            this.btnLive.TabIndex = 2;
            this.btnLive.Text = "实时预览";
            this.btnLive.UseVisualStyleBackColor = true;
            this.btnLive.Click += new System.EventHandler(this.btnLive_Click);
            // 
            // btnStopLive
            // 
            this.btnStopLive.Location = new System.Drawing.Point(325, 7);
            this.btnStopLive.Name = "btnStopLive";
            this.btnStopLive.Size = new System.Drawing.Size(100, 32);
            this.btnStopLive.TabIndex = 3;
            this.btnStopLive.Text = "停止预览";
            this.btnStopLive.UseVisualStyleBackColor = true;
            this.btnStopLive.Click += new System.EventHandler(this.btnStopLive_Click);
            // 
            // btnSnap
            // 
            this.btnSnap.Location = new System.Drawing.Point(7, 45);
            this.btnSnap.Name = "btnSnap";
            this.btnSnap.Size = new System.Drawing.Size(100, 32);
            this.btnSnap.TabIndex = 4;
            this.btnSnap.Text = "单次拍照";
            this.btnSnap.UseVisualStyleBackColor = true;
            this.btnSnap.Click += new System.EventHandler(this.btnSnap_Click);
            // 
            // grpVpp
            // 
            this.grpVpp.Controls.Add(this.pnlVppHost);
            this.grpVpp.Controls.Add(this.lblVppPath);
            this.grpVpp.Controls.Add(this.flpVpp);
            this.grpVpp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpVpp.Location = new System.Drawing.Point(554, 4);
            this.grpVpp.Margin = new System.Windows.Forms.Padding(4);
            this.grpVpp.Name = "grpVpp";
            this.grpVpp.Size = new System.Drawing.Size(542, 312);
            this.grpVpp.TabIndex = 1;
            this.grpVpp.TabStop = false;
            this.grpVpp.Text = "VPP方案";
            // 
            // pnlVppHost
            // 
            this.pnlVppHost.BackColor = System.Drawing.Color.WhiteSmoke;
            this.pnlVppHost.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlVppHost.Controls.Add(this.lblVppTip);
            this.pnlVppHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlVppHost.Location = new System.Drawing.Point(3, 109);
            this.pnlVppHost.Name = "pnlVppHost";
            this.pnlVppHost.Size = new System.Drawing.Size(536, 200);
            this.pnlVppHost.TabIndex = 2;
            // 
            // lblVppTip
            // 
            this.lblVppTip.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVppTip.ForeColor = System.Drawing.Color.Gray;
            this.lblVppTip.Location = new System.Drawing.Point(0, 0);
            this.lblVppTip.Name = "lblVppTip";
            this.lblVppTip.Size = new System.Drawing.Size(534, 198);
            this.lblVppTip.TabIndex = 0;
            this.lblVppTip.Text = "CogToolBlockEdit 挂载位（Subject = ToolBlock）";
            this.lblVppTip.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblVppPath
            // 
            this.lblVppPath.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblVppPath.Location = new System.Drawing.Point(3, 81);
            this.lblVppPath.Name = "lblVppPath";
            this.lblVppPath.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblVppPath.Size = new System.Drawing.Size(536, 28);
            this.lblVppPath.TabIndex = 1;
            this.lblVppPath.Text = "当前VPP路径：--";
            this.lblVppPath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // flpVpp
            // 
            this.flpVpp.Controls.Add(this.btnLoadVpp);
            this.flpVpp.Controls.Add(this.btnSaveVpp);
            this.flpVpp.Dock = System.Windows.Forms.DockStyle.Top;
            this.flpVpp.Location = new System.Drawing.Point(3, 31);
            this.flpVpp.Name = "flpVpp";
            this.flpVpp.Padding = new System.Windows.Forms.Padding(4);
            this.flpVpp.Size = new System.Drawing.Size(536, 50);
            this.flpVpp.TabIndex = 0;
            // 
            // btnLoadVpp
            // 
            this.btnLoadVpp.Location = new System.Drawing.Point(7, 7);
            this.btnLoadVpp.Name = "btnLoadVpp";
            this.btnLoadVpp.Size = new System.Drawing.Size(130, 32);
            this.btnLoadVpp.TabIndex = 0;
            this.btnLoadVpp.Text = "加载VPP方案";
            this.btnLoadVpp.UseVisualStyleBackColor = true;
            this.btnLoadVpp.Click += new System.EventHandler(this.btnLoadVpp_Click);
            // 
            // btnSaveVpp
            // 
            this.btnSaveVpp.BackColor = System.Drawing.Color.DodgerBlue;
            this.btnSaveVpp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveVpp.ForeColor = System.Drawing.Color.White;
            this.btnSaveVpp.Location = new System.Drawing.Point(143, 7);
            this.btnSaveVpp.Name = "btnSaveVpp";
            this.btnSaveVpp.Size = new System.Drawing.Size(110, 32);
            this.btnSaveVpp.TabIndex = 1;
            this.btnSaveVpp.Text = "保存方案";
            this.btnSaveVpp.UseVisualStyleBackColor = false;
            this.btnSaveVpp.Click += new System.EventHandler(this.btnSaveVpp_Click);
            // 
            // grpPma
            // 
            this.grpPma.Controls.Add(this.pnlPmaRight);
            this.grpPma.Controls.Add(this.picTemplate);
            this.grpPma.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPma.Location = new System.Drawing.Point(4, 324);
            this.grpPma.Margin = new System.Windows.Forms.Padding(4);
            this.grpPma.Name = "grpPma";
            this.grpPma.Size = new System.Drawing.Size(542, 312);
            this.grpPma.TabIndex = 2;
            this.grpPma.TabStop = false;
            this.grpPma.Text = "PMA模板训练";
            // 
            // pnlPmaRight
            // 
            this.pnlPmaRight.Controls.Add(this.lblTplScore);
            this.pnlPmaRight.Controls.Add(this.pnlPyr);
            this.pnlPmaRight.Controls.Add(this.pnlAng);
            this.pnlPmaRight.Controls.Add(this.pnlThr);
            this.pnlPmaRight.Controls.Add(this.pnlPmaTool);
            this.pnlPmaRight.Controls.Add(this.flpPma);
            this.pnlPmaRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPmaRight.Location = new System.Drawing.Point(339, 31);
            this.pnlPmaRight.Name = "pnlPmaRight";
            this.pnlPmaRight.Size = new System.Drawing.Size(200, 278);
            this.pnlPmaRight.TabIndex = 1;
            // 
            // lblTplScore
            // 
            this.lblTplScore.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTplScore.Location = new System.Drawing.Point(0, 232);
            this.lblTplScore.Name = "lblTplScore";
            this.lblTplScore.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblTplScore.Size = new System.Drawing.Size(200, 28);
            this.lblTplScore.TabIndex = 4;
            this.lblTplScore.Text = "验证分数：--";
            this.lblTplScore.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlPyr
            // 
            this.pnlPyr.Controls.Add(this.lblPyr);
            this.pnlPyr.Controls.Add(this.cmbGranularity);
            this.pnlPyr.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlPyr.Location = new System.Drawing.Point(0, 196);
            this.pnlPyr.Name = "pnlPyr";
            this.pnlPyr.Size = new System.Drawing.Size(200, 36);
            this.pnlPyr.TabIndex = 3;
            // 
            // lblPyr
            // 
            this.lblPyr.AutoSize = true;
            this.lblPyr.Location = new System.Drawing.Point(8, 10);
            this.lblPyr.Name = "lblPyr";
            this.lblPyr.Size = new System.Drawing.Size(54, 28);
            this.lblPyr.TabIndex = 0;
            this.lblPyr.Text = "粒度";
            // 
            // cmbGranularity
            // 
            this.cmbGranularity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbGranularity.FormattingEnabled = true;
            this.cmbGranularity.Items.AddRange(new object[] {
            "自动",
            "粗",
            "标准",
            "细"});
            this.cmbGranularity.Location = new System.Drawing.Point(110, 6);
            this.cmbGranularity.Name = "cmbGranularity";
            this.cmbGranularity.Size = new System.Drawing.Size(160, 36);
            this.cmbGranularity.TabIndex = 1;
            this.cmbGranularity.SelectedIndex = 0;
            // 
            // pnlAng
            // 
            this.pnlAng.Controls.Add(this.lblAng);
            this.pnlAng.Controls.Add(this.cmbAngle);
            this.pnlAng.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlAng.Location = new System.Drawing.Point(0, 160);
            this.pnlAng.Name = "pnlAng";
            this.pnlAng.Size = new System.Drawing.Size(200, 36);
            this.pnlAng.TabIndex = 2;
            // 
            // lblAng
            // 
            this.lblAng.AutoSize = true;
            this.lblAng.Location = new System.Drawing.Point(8, 10);
            this.lblAng.Name = "lblAng";
            this.lblAng.Size = new System.Drawing.Size(96, 28);
            this.lblAng.TabIndex = 0;
            this.lblAng.Text = "角度范围";
            // 
            // cmbAngle
            // 
            this.cmbAngle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAngle.FormattingEnabled = true;
            this.cmbAngle.Items.AddRange(new object[] {
            "±180°",
            "±90°",
            "±30°",
            "±10°"});
            this.cmbAngle.Location = new System.Drawing.Point(110, 6);
            this.cmbAngle.Name = "cmbAngle";
            this.cmbAngle.Size = new System.Drawing.Size(160, 36);
            this.cmbAngle.TabIndex = 1;
            // 
            // pnlThr
            // 
            this.pnlThr.Controls.Add(this.lblThr);
            this.pnlThr.Controls.Add(this.cmbThreshold);
            this.pnlThr.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlThr.Location = new System.Drawing.Point(0, 124);
            this.pnlThr.Name = "pnlThr";
            this.pnlThr.Size = new System.Drawing.Size(200, 36);
            this.pnlThr.TabIndex = 1;
            // 
            // lblThr
            // 
            this.lblThr.AutoSize = true;
            this.lblThr.Location = new System.Drawing.Point(8, 10);
            this.lblThr.Name = "lblThr";
            this.lblThr.Size = new System.Drawing.Size(96, 28);
            this.lblThr.TabIndex = 0;
            this.lblThr.Text = "匹配阈值";
            // 
            // cmbThreshold
            // 
            this.cmbThreshold.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbThreshold.FormattingEnabled = true;
            this.cmbThreshold.Items.AddRange(new object[] {
            "0.50",
            "0.60",
            "0.70",
            "0.80"});
            this.cmbThreshold.Location = new System.Drawing.Point(110, 6);
            this.cmbThreshold.Name = "cmbThreshold";
            this.cmbThreshold.Size = new System.Drawing.Size(160, 36);
            this.cmbThreshold.TabIndex = 1;
            // 
            // pnlPmaTool
            // 
            this.pnlPmaTool.Controls.Add(this.lblPmaTool);
            this.pnlPmaTool.Controls.Add(this.cmbPmaTool);
            this.pnlPmaTool.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlPmaTool.Location = new System.Drawing.Point(0, 88);
            this.pnlPmaTool.Name = "pnlPmaTool";
            this.pnlPmaTool.Size = new System.Drawing.Size(200, 36);
            this.pnlPmaTool.TabIndex = 6;
            // 
            // lblPmaTool
            // 
            this.lblPmaTool.AutoSize = true;
            this.lblPmaTool.Location = new System.Drawing.Point(8, 10);
            this.lblPmaTool.Name = "lblPmaTool";
            this.lblPmaTool.Size = new System.Drawing.Size(96, 28);
            this.lblPmaTool.TabIndex = 0;
            this.lblPmaTool.Text = "模板工具";
            // 
            // cmbPmaTool
            // 
            this.cmbPmaTool.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPmaTool.FormattingEnabled = true;
            this.cmbPmaTool.Location = new System.Drawing.Point(110, 6);
            this.cmbPmaTool.Name = "cmbPmaTool";
            this.cmbPmaTool.Size = new System.Drawing.Size(160, 36);
            this.cmbPmaTool.TabIndex = 1;
            this.cmbPmaTool.SelectedIndexChanged += new System.EventHandler(this.cmbPmaTool_SelectedIndexChanged);
            // 
            // flpPma
            // 
            this.flpPma.Controls.Add(this.btnGrabTpl);
            this.flpPma.Controls.Add(this.btnTrain);
            this.flpPma.Controls.Add(this.btnSaveTpl);
            this.flpPma.Dock = System.Windows.Forms.DockStyle.Top;
            this.flpPma.Location = new System.Drawing.Point(0, 0);
            this.flpPma.Name = "flpPma";
            this.flpPma.Padding = new System.Windows.Forms.Padding(4);
            this.flpPma.Size = new System.Drawing.Size(200, 88);
            this.flpPma.TabIndex = 0;
            // 
            // btnGrabTpl
            // 
            this.btnGrabTpl.Location = new System.Drawing.Point(7, 7);
            this.btnGrabTpl.Name = "btnGrabTpl";
            this.btnGrabTpl.Size = new System.Drawing.Size(110, 32);
            this.btnGrabTpl.TabIndex = 0;
            this.btnGrabTpl.Text = "采集训练图";
            this.btnGrabTpl.UseVisualStyleBackColor = true;
            this.btnGrabTpl.Click += new System.EventHandler(this.btnGrabTpl_Click);
            // 
            // btnTrain
            // 
            this.btnTrain.Location = new System.Drawing.Point(7, 83);
            this.btnTrain.Name = "btnTrain";
            this.btnTrain.Size = new System.Drawing.Size(110, 32);
            this.btnTrain.TabIndex = 1;
            this.btnTrain.Text = "训练模板";
            this.btnTrain.UseVisualStyleBackColor = true;
            this.btnTrain.Click += new System.EventHandler(this.btnTrain_Click);
            // 
            // btnSaveTpl
            // 
            this.btnSaveTpl.Location = new System.Drawing.Point(7, 45);
            this.btnSaveTpl.Name = "btnSaveTpl";
            this.btnSaveTpl.Size = new System.Drawing.Size(110, 32);
            this.btnSaveTpl.TabIndex = 2;
            this.btnSaveTpl.Text = "保存模板";
            this.btnSaveTpl.UseVisualStyleBackColor = true;
            this.btnSaveTpl.Click += new System.EventHandler(this.btnSaveTpl_Click);
            // 
            // picTemplate
            // 
            this.picTemplate.ColorMapLowerClipColor = System.Drawing.Color.Black;
            this.picTemplate.ColorMapLowerRoiLimit = 0D;
            this.picTemplate.ColorMapPredefined = Cognex.VisionPro.Display.CogDisplayColorMapPredefinedConstants.None;
            this.picTemplate.ColorMapUpperClipColor = System.Drawing.Color.Black;
            this.picTemplate.ColorMapUpperRoiLimit = 1D;
            this.picTemplate.Dock = System.Windows.Forms.DockStyle.Left;
            this.picTemplate.DoubleTapZoomCycleLength = 2;
            this.picTemplate.DoubleTapZoomSensitivity = 2.5D;
            this.picTemplate.Location = new System.Drawing.Point(3, 31);
            this.picTemplate.MouseWheelMode = Cognex.VisionPro.Display.CogDisplayMouseWheelModeConstants.Zoom1;
            this.picTemplate.MouseWheelSensitivity = 1D;
            this.picTemplate.Name = "picTemplate";
            this.picTemplate.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("picTemplate.OcxState")));
            this.picTemplate.Size = new System.Drawing.Size(336, 278);
            this.picTemplate.TabIndex = 0;
            // 
            // grpSave
            // 
            this.grpSave.Controls.Add(this.pnlBrowse);
            this.grpSave.Controls.Add(this.pnlFmt);
            this.grpSave.Controls.Add(this.txtSavePath);
            this.grpSave.Controls.Add(this.pnlChk);
            this.grpSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSave.Location = new System.Drawing.Point(554, 324);
            this.grpSave.Margin = new System.Windows.Forms.Padding(4);
            this.grpSave.Name = "grpSave";
            this.grpSave.Size = new System.Drawing.Size(542, 312);
            this.grpSave.TabIndex = 3;
            this.grpSave.TabStop = false;
            this.grpSave.Text = "图像保存配置";
            // 
            // pnlBrowse
            // 
            this.pnlBrowse.Controls.Add(this.btnBrowse);
            this.pnlBrowse.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBrowse.Location = new System.Drawing.Point(3, 263);
            this.pnlBrowse.Name = "pnlBrowse";
            this.pnlBrowse.Size = new System.Drawing.Size(536, 46);
            this.pnlBrowse.TabIndex = 3;
            // 
            // btnBrowse
            // 
            this.btnBrowse.BackColor = System.Drawing.Color.DodgerBlue;
            this.btnBrowse.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnBrowse.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowse.ForeColor = System.Drawing.Color.White;
            this.btnBrowse.Location = new System.Drawing.Point(426, 0);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(110, 46);
            this.btnBrowse.TabIndex = 0;
            this.btnBrowse.Text = "选择路径";
            this.btnBrowse.UseVisualStyleBackColor = false;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // pnlFmt
            // 
            this.pnlFmt.Controls.Add(this.lblFmt);
            this.pnlFmt.Controls.Add(this.cmbImgFmt);
            this.pnlFmt.Controls.Add(this.lblQuota);
            this.pnlFmt.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlFmt.Location = new System.Drawing.Point(3, 102);
            this.pnlFmt.Name = "pnlFmt";
            this.pnlFmt.Size = new System.Drawing.Size(536, 36);
            this.pnlFmt.TabIndex = 2;
            // 
            // lblFmt
            // 
            this.lblFmt.AutoSize = true;
            this.lblFmt.Location = new System.Drawing.Point(8, 10);
            this.lblFmt.Name = "lblFmt";
            this.lblFmt.Size = new System.Drawing.Size(54, 28);
            this.lblFmt.TabIndex = 0;
            this.lblFmt.Text = "格式";
            // 
            // cmbImgFmt
            // 
            this.cmbImgFmt.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbImgFmt.FormattingEnabled = true;
            this.cmbImgFmt.Items.AddRange(new object[] {
            "JPG",
            "BMP",
            "PNG"});
            this.cmbImgFmt.Location = new System.Drawing.Point(96, 6);
            this.cmbImgFmt.Name = "cmbImgFmt";
            this.cmbImgFmt.Size = new System.Drawing.Size(100, 36);
            this.cmbImgFmt.TabIndex = 1;
            this.cmbImgFmt.SelectedIndex = 0;
            this.cmbImgFmt.SelectedIndexChanged += new System.EventHandler(this.cmbImgFmt_SelectedIndexChanged);
            // 
            // lblQuota
            // 
            this.lblQuota.AutoSize = true;
            this.lblQuota.ForeColor = System.Drawing.Color.Gray;
            this.lblQuota.Location = new System.Drawing.Point(210, 10);
            this.lblQuota.Name = "lblQuota";
            this.lblQuota.Size = new System.Drawing.Size(215, 28);
            this.lblQuota.TabIndex = 2;
            this.lblQuota.Text = "配额50GB / 保留30天";
            // 
            // txtSavePath
            // 
            this.txtSavePath.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtSavePath.Location = new System.Drawing.Point(3, 67);
            this.txtSavePath.Name = "txtSavePath";
            this.txtSavePath.Size = new System.Drawing.Size(536, 35);
            this.txtSavePath.TabIndex = 1;
            this.txtSavePath.Text = "D:\\VisionData";
            this.txtSavePath.Leave += new System.EventHandler(this.txtSavePath_Leave);
            // 
            // pnlChk
            // 
            this.pnlChk.Controls.Add(this.chkSaveOK);
            this.pnlChk.Controls.Add(this.chkSaveNG);
            this.pnlChk.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlChk.Location = new System.Drawing.Point(3, 31);
            this.pnlChk.Name = "pnlChk";
            this.pnlChk.Size = new System.Drawing.Size(536, 36);
            this.pnlChk.TabIndex = 0;
            // 
            // chkSaveOK
            // 
            this.chkSaveOK.AutoSize = true;
            this.chkSaveOK.Checked = true;
            this.chkSaveOK.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSaveOK.Location = new System.Drawing.Point(11, 9);
            this.chkSaveOK.Name = "chkSaveOK";
            this.chkSaveOK.Size = new System.Drawing.Size(173, 32);
            this.chkSaveOK.TabIndex = 0;
            this.chkSaveOK.Text = "自动保存OK图";
            this.chkSaveOK.UseVisualStyleBackColor = true;
            this.chkSaveOK.CheckedChanged += new System.EventHandler(this.chkSave_CheckedChanged);
            // 
            // chkSaveNG
            // 
            this.chkSaveNG.AutoSize = true;
            this.chkSaveNG.Checked = true;
            this.chkSaveNG.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSaveNG.Location = new System.Drawing.Point(190, 9);
            this.chkSaveNG.Name = "chkSaveNG";
            this.chkSaveNG.Size = new System.Drawing.Size(176, 32);
            this.chkSaveNG.TabIndex = 1;
            this.chkSaveNG.Text = "自动保存NG图";
            this.chkSaveNG.UseVisualStyleBackColor = true;
            this.chkSaveNG.CheckedChanged += new System.EventHandler(this.chkSave_CheckedChanged);
            // 
            // VisionConfigView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 28F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tlpMain);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Name = "VisionConfigView";
            this.Size = new System.Drawing.Size(1100, 640);
            this.tlpMain.ResumeLayout(false);
            this.grpCamera.ResumeLayout(false);
            this.pnlTrig.ResumeLayout(false);
            this.pnlTrig.PerformLayout();
            this.pnlPkt.ResumeLayout(false);
            this.pnlPkt.PerformLayout();
            this.pnlExp.ResumeLayout(false);
            this.pnlExp.PerformLayout();
            this.flpCam.ResumeLayout(false);
            this.grpVpp.ResumeLayout(false);
            this.pnlVppHost.ResumeLayout(false);
            this.flpVpp.ResumeLayout(false);
            this.grpPma.ResumeLayout(false);
            this.pnlPmaRight.ResumeLayout(false);
            this.pnlPyr.ResumeLayout(false);
            this.pnlPyr.PerformLayout();
            this.pnlAng.ResumeLayout(false);
            this.pnlAng.PerformLayout();
            this.pnlThr.ResumeLayout(false);
            this.pnlThr.PerformLayout();
            this.pnlPmaTool.ResumeLayout(false);
            this.pnlPmaTool.PerformLayout();
            this.flpPma.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picTemplate)).EndInit();
            this.grpSave.ResumeLayout(false);
            this.grpSave.PerformLayout();
            this.pnlBrowse.ResumeLayout(false);
            this.pnlFmt.ResumeLayout(false);
            this.pnlFmt.PerformLayout();
            this.pnlChk.ResumeLayout(false);
            this.pnlChk.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tlpMain;
        private System.Windows.Forms.GroupBox grpCamera;
        private System.Windows.Forms.FlowLayoutPanel flpCam;
        private System.Windows.Forms.Button btnCamConn;
        private System.Windows.Forms.Button btnCamDis;
        private System.Windows.Forms.Button btnLive;
        private System.Windows.Forms.Button btnStopLive;
        private System.Windows.Forms.Button btnSnap;
        private System.Windows.Forms.Panel pnlExp;
        private System.Windows.Forms.Label lblExp;
        private System.Windows.Forms.TextBox txtExposure;
        private System.Windows.Forms.Label lblGain;
        private System.Windows.Forms.ComboBox cmbGain;
        private System.Windows.Forms.Panel pnlPkt;
        private System.Windows.Forms.Label lblLatency;
        private System.Windows.Forms.ComboBox cmbLatency;
        private System.Windows.Forms.Label lblPacket;
        private System.Windows.Forms.ComboBox cmbPacket;
        private System.Windows.Forms.Panel pnlTrig;
        private System.Windows.Forms.ComboBox cmbTrigMode;
        private System.Windows.Forms.Label lblTrigMode;
        private System.Windows.Forms.GroupBox grpVpp;
        private System.Windows.Forms.FlowLayoutPanel flpVpp;
        private System.Windows.Forms.Button btnLoadVpp;
        private System.Windows.Forms.Button btnSaveVpp;
        private System.Windows.Forms.Label lblVppPath;
        private System.Windows.Forms.Panel pnlVppHost;
        private System.Windows.Forms.Label lblVppTip;
        private System.Windows.Forms.GroupBox grpPma;
        private Cognex.VisionPro.CogRecordDisplay picTemplate;
        private System.Windows.Forms.Panel pnlPmaRight;
        private System.Windows.Forms.FlowLayoutPanel flpPma;
        private System.Windows.Forms.Button btnGrabTpl;
        private System.Windows.Forms.Button btnTrain;
        private System.Windows.Forms.Button btnSaveTpl;
        private System.Windows.Forms.Panel pnlThr;
        private System.Windows.Forms.Label lblThr;
        private System.Windows.Forms.ComboBox cmbThreshold;
        private System.Windows.Forms.Panel pnlAng;
        private System.Windows.Forms.Label lblAng;
        private System.Windows.Forms.ComboBox cmbAngle;
        private System.Windows.Forms.Panel pnlPyr;
        private System.Windows.Forms.Label lblPyr;
        private System.Windows.Forms.ComboBox cmbGranularity;
        private System.Windows.Forms.Panel pnlPmaTool;
        private System.Windows.Forms.Label lblPmaTool;
        private System.Windows.Forms.ComboBox cmbPmaTool;
        private System.Windows.Forms.Label lblTplScore;
        private System.Windows.Forms.GroupBox grpSave;
        private System.Windows.Forms.Panel pnlChk;
        private System.Windows.Forms.CheckBox chkSaveOK;
        private System.Windows.Forms.CheckBox chkSaveNG;
        private System.Windows.Forms.TextBox txtSavePath;
        private System.Windows.Forms.Panel pnlFmt;
        private System.Windows.Forms.Label lblFmt;
        private System.Windows.Forms.ComboBox cmbImgFmt;
        private System.Windows.Forms.Label lblQuota;
        private System.Windows.Forms.Panel pnlBrowse;
        private System.Windows.Forms.Button btnBrowse;
    }
}

