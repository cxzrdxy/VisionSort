namespace VisionSort.Views
{
    partial class DeviceDebugView
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
            this.grpConveyor = new System.Windows.Forms.GroupBox();
            this.pnlComm = new System.Windows.Forms.Panel();
            this.lblCommT = new System.Windows.Forms.Label();
            this.txtComm = new System.Windows.Forms.TextBox();
            this.lblRunT = new System.Windows.Forms.Label();
            this.txtRunState = new System.Windows.Forms.TextBox();
            this.pnlCur = new System.Windows.Forms.Panel();
            this.lblCurT = new System.Windows.Forms.Label();
            this.txtCurSpeed = new System.Windows.Forms.TextBox();
            this.lblUnit = new System.Windows.Forms.Label();
            this.ledMb = new System.Windows.Forms.Label();
            this.pnlSpeed = new System.Windows.Forms.Panel();
            this.lblSpeedT = new System.Windows.Forms.Label();
            this.trkSpeed = new System.Windows.Forms.TrackBar();
            this.flpMove = new System.Windows.Forms.FlowLayoutPanel();
            this.btnFwd = new System.Windows.Forms.Button();
            this.btnRev = new System.Windows.Forms.Button();
            this.btnStopMove = new System.Windows.Forms.Button();
            this.flpConn = new System.Windows.Forms.FlowLayoutPanel();
            this.btnMbConn = new System.Windows.Forms.Button();
            this.btnMbDis = new System.Windows.Forms.Button();
            this.btnMbReset = new System.Windows.Forms.Button();
            this.grpRobot = new System.Windows.Forms.GroupBox();
            this.pnlPos = new System.Windows.Forms.Panel();
            this.lblPX = new System.Windows.Forms.Label();
            this.lblPY = new System.Windows.Forms.Label();
            this.lblPZ = new System.Windows.Forms.Label();
            this.lblVac = new System.Windows.Forms.Label();
            this.pnlQ = new System.Windows.Forms.Panel();
            this.btnRunQ = new System.Windows.Forms.Button();
            this.lblQCount = new System.Windows.Forms.Label();
            this.pnlExec = new System.Windows.Forms.Panel();
            this.lblExec = new System.Windows.Forms.Label();
            this.cmbExec = new System.Windows.Forms.ComboBox();
            this.btnRelease = new System.Windows.Forms.Button();
            this.btnAddQ = new System.Windows.Forms.Button();
            this.pnlTarget = new System.Windows.Forms.Panel();
            this.lblTX = new System.Windows.Forms.Label();
            this.txtTX = new System.Windows.Forms.TextBox();
            this.lblTY = new System.Windows.Forms.Label();
            this.txtTY = new System.Windows.Forms.TextBox();
            this.lblTZ = new System.Windows.Forms.Label();
            this.txtTZ = new System.Windows.Forms.TextBox();
            this.btnMoveTo = new System.Windows.Forms.Button();
            this.grpJog = new System.Windows.Forms.GroupBox();
            this.btnXp = new System.Windows.Forms.Button();
            this.btnXm = new System.Windows.Forms.Button();
            this.btnYp = new System.Windows.Forms.Button();
            this.btnYm = new System.Windows.Forms.Button();
            this.btnZp = new System.Windows.Forms.Button();
            this.btnZm = new System.Windows.Forms.Button();
            this.lblStep = new System.Windows.Forms.Label();
            this.cmbStep = new System.Windows.Forms.ComboBox();
            this.flpRb = new System.Windows.Forms.FlowLayoutPanel();
            this.btnRbConn = new System.Windows.Forms.Button();
            this.btnRbDis = new System.Windows.Forms.Button();
            this.btnHome = new System.Windows.Forms.Button();
            this.btnClearAlarm = new System.Windows.Forms.Button();
            this.btnClearQ = new System.Windows.Forms.Button();
            this.grpCalib = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.grpConveyor.SuspendLayout();
            this.pnlComm.SuspendLayout();
            this.pnlCur.SuspendLayout();
            this.pnlSpeed.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkSpeed)).BeginInit();
            this.flpMove.SuspendLayout();
            this.flpConn.SuspendLayout();
            this.grpRobot.SuspendLayout();
            this.pnlPos.SuspendLayout();
            this.pnlQ.SuspendLayout();
            this.pnlExec.SuspendLayout();
            this.pnlTarget.SuspendLayout();
            this.grpJog.SuspendLayout();
            this.flpRb.SuspendLayout();
            this.grpCalib.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitMain
            // 
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Location = new System.Drawing.Point(0, 0);
            this.splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.grpConveyor);
            this.splitMain.Panel1.Padding = new System.Windows.Forms.Padding(0, 0, 4, 0);
            // 
            // splitMain.Panel2
            // 
            // 顺序要紧：**先加 grpCalib（Fill）再加 grpRobot（Top）**。停靠是"下标大的先靠边"，
            // 所以 Fill 的那个必须是先加进去的（下标 0）；反过来标定区会跑到机械臂上面去。
            this.splitMain.Panel2.Controls.Add(this.grpCalib);
            this.splitMain.Panel2.Controls.Add(this.grpRobot);
            this.splitMain.Panel2.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.splitMain.Size = new System.Drawing.Size(1100, 640);
            this.splitMain.SplitterDistance = 400;
            this.splitMain.TabIndex = 0;
            // 
            // grpConveyor
            // 
            this.grpConveyor.Controls.Add(this.pnlComm);
            this.grpConveyor.Controls.Add(this.pnlCur);
            this.grpConveyor.Controls.Add(this.pnlSpeed);
            this.grpConveyor.Controls.Add(this.flpMove);
            this.grpConveyor.Controls.Add(this.flpConn);
            this.grpConveyor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpConveyor.Location = new System.Drawing.Point(0, 0);
            this.grpConveyor.Name = "grpConveyor";
            this.grpConveyor.Size = new System.Drawing.Size(396, 640);
            this.grpConveyor.TabIndex = 0;
            this.grpConveyor.TabStop = false;
            this.grpConveyor.Text = "传送带 Modbus 控制";
            // 
            // pnlComm
            // 
            this.pnlComm.Controls.Add(this.lblCommT);
            this.pnlComm.Controls.Add(this.txtComm);
            this.pnlComm.Controls.Add(this.lblRunT);
            this.pnlComm.Controls.Add(this.txtRunState);
            this.pnlComm.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlComm.Location = new System.Drawing.Point(3, 340);
            this.pnlComm.Name = "pnlComm";
            this.pnlComm.Size = new System.Drawing.Size(390, 109);
            this.pnlComm.TabIndex = 4;
            // 
            // lblCommT
            // 
            this.lblCommT.AutoSize = true;
            this.lblCommT.Location = new System.Drawing.Point(8, 10);
            this.lblCommT.Name = "lblCommT";
            this.lblCommT.Size = new System.Drawing.Size(96, 28);
            this.lblCommT.TabIndex = 0;
            this.lblCommT.Text = "通讯状态";
            // 
            // txtComm
            // 
            this.txtComm.Location = new System.Drawing.Point(100, 7);
            this.txtComm.Name = "txtComm";
            this.txtComm.ReadOnly = true;
            this.txtComm.Size = new System.Drawing.Size(110, 35);
            this.txtComm.TabIndex = 1;
            this.txtComm.Text = "未连接";
            // 
            // lblRunT
            // 
            this.lblRunT.AutoSize = true;
            this.lblRunT.Location = new System.Drawing.Point(8, 62);
            this.lblRunT.Name = "lblRunT";
            this.lblRunT.Size = new System.Drawing.Size(96, 28);
            this.lblRunT.TabIndex = 2;
            this.lblRunT.Text = "运行状态";
            // 
            // txtRunState
            // 
            this.txtRunState.Location = new System.Drawing.Point(100, 59);
            this.txtRunState.Name = "txtRunState";
            this.txtRunState.ReadOnly = true;
            this.txtRunState.Size = new System.Drawing.Size(110, 35);
            this.txtRunState.TabIndex = 3;
            this.txtRunState.Text = "停止";
            // 
            // pnlCur
            // 
            this.pnlCur.Controls.Add(this.lblCurT);
            this.pnlCur.Controls.Add(this.txtCurSpeed);
            this.pnlCur.Controls.Add(this.lblUnit);
            this.pnlCur.Controls.Add(this.ledMb);
            this.pnlCur.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlCur.Location = new System.Drawing.Point(3, 278);
            this.pnlCur.Name = "pnlCur";
            this.pnlCur.Size = new System.Drawing.Size(390, 62);
            this.pnlCur.TabIndex = 3;
            // 
            // lblCurT
            // 
            this.lblCurT.AutoSize = true;
            this.lblCurT.Location = new System.Drawing.Point(8, 10);
            this.lblCurT.Name = "lblCurT";
            this.lblCurT.Size = new System.Drawing.Size(96, 28);
            this.lblCurT.TabIndex = 0;
            this.lblCurT.Text = "当前速度";
            // 
            // txtCurSpeed
            // 
            this.txtCurSpeed.Location = new System.Drawing.Point(100, 7);
            this.txtCurSpeed.Name = "txtCurSpeed";
            this.txtCurSpeed.ReadOnly = true;
            this.txtCurSpeed.Size = new System.Drawing.Size(110, 35);
            this.txtCurSpeed.TabIndex = 1;
            this.txtCurSpeed.Text = "30.0";
            // 
            // lblUnit
            // 
            this.lblUnit.AutoSize = true;
            this.lblUnit.Location = new System.Drawing.Point(216, 10);
            this.lblUnit.Name = "lblUnit";
            this.lblUnit.Size = new System.Drawing.Size(31, 28);
            this.lblUnit.TabIndex = 2;
            this.lblUnit.Text = "%";
            // 
            // ledMb
            // 
            this.ledMb.AutoSize = true;
            this.ledMb.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ledMb.ForeColor = System.Drawing.Color.ForestGreen;
            this.ledMb.Location = new System.Drawing.Point(250, 6);
            this.ledMb.Name = "ledMb";
            this.ledMb.Size = new System.Drawing.Size(35, 37);
            this.ledMb.TabIndex = 3;
            this.ledMb.Text = "●";
            // 
            // pnlSpeed
            // 
            this.pnlSpeed.Controls.Add(this.lblSpeedT);
            this.pnlSpeed.Controls.Add(this.trkSpeed);
            this.pnlSpeed.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlSpeed.Location = new System.Drawing.Point(3, 197);
            this.pnlSpeed.Name = "pnlSpeed";
            this.pnlSpeed.Size = new System.Drawing.Size(390, 81);
            this.pnlSpeed.TabIndex = 2;
            // 
            // lblSpeedT
            // 
            this.lblSpeedT.AutoSize = true;
            this.lblSpeedT.Location = new System.Drawing.Point(8, 19);
            this.lblSpeedT.Name = "lblSpeedT";
            this.lblSpeedT.Size = new System.Drawing.Size(96, 28);
            this.lblSpeedT.TabIndex = 0;
            this.lblSpeedT.Text = "速度调节";
            // 
            // trkSpeed
            // 
            this.trkSpeed.LargeChange = 100;
            this.trkSpeed.Location = new System.Drawing.Point(100, 8);
            this.trkSpeed.Maximum = 1000;
            this.trkSpeed.Name = "trkSpeed";
            this.trkSpeed.Size = new System.Drawing.Size(220, 80);
            this.trkSpeed.SmallChange = 10;
            this.trkSpeed.TabIndex = 1;
            this.trkSpeed.TickFrequency = 100;
            this.trkSpeed.Value = 300;
            this.trkSpeed.ValueChanged += new System.EventHandler(this.trkSpeed_ValueChanged);
            // 
            // flpMove
            // 
            this.flpMove.Controls.Add(this.btnFwd);
            this.flpMove.Controls.Add(this.btnRev);
            this.flpMove.Controls.Add(this.btnStopMove);
            this.flpMove.Dock = System.Windows.Forms.DockStyle.Top;
            this.flpMove.Location = new System.Drawing.Point(3, 147);
            this.flpMove.Name = "flpMove";
            this.flpMove.Padding = new System.Windows.Forms.Padding(4);
            this.flpMove.Size = new System.Drawing.Size(390, 50);
            this.flpMove.TabIndex = 1;
            // 
            // btnFwd
            // 
            this.btnFwd.Location = new System.Drawing.Point(7, 7);
            this.btnFwd.Name = "btnFwd";
            this.btnFwd.Size = new System.Drawing.Size(90, 32);
            this.btnFwd.TabIndex = 0;
            this.btnFwd.Text = "正转";
            this.btnFwd.UseVisualStyleBackColor = true;
            this.btnFwd.Click += new System.EventHandler(this.btnFwd_Click);
            // 
            // btnRev
            // 
            this.btnRev.Location = new System.Drawing.Point(103, 7);
            this.btnRev.Name = "btnRev";
            this.btnRev.Size = new System.Drawing.Size(90, 32);
            this.btnRev.TabIndex = 1;
            this.btnRev.Text = "反转";
            this.btnRev.UseVisualStyleBackColor = true;
            this.btnRev.Click += new System.EventHandler(this.btnRev_Click);
            // 
            // btnStopMove
            // 
            this.btnStopMove.Location = new System.Drawing.Point(199, 7);
            this.btnStopMove.Name = "btnStopMove";
            this.btnStopMove.Size = new System.Drawing.Size(90, 32);
            this.btnStopMove.TabIndex = 2;
            this.btnStopMove.Text = "停止";
            this.btnStopMove.UseVisualStyleBackColor = true;
            this.btnStopMove.Click += new System.EventHandler(this.btnStopMove_Click);
            // 
            // flpConn
            // 
            this.flpConn.Controls.Add(this.btnMbConn);
            this.flpConn.Controls.Add(this.btnMbDis);
            this.flpConn.Controls.Add(this.btnMbReset);
            this.flpConn.Dock = System.Windows.Forms.DockStyle.Top;
            this.flpConn.Location = new System.Drawing.Point(3, 31);
            this.flpConn.Name = "flpConn";
            this.flpConn.Padding = new System.Windows.Forms.Padding(4);
            this.flpConn.Size = new System.Drawing.Size(390, 116);
            this.flpConn.TabIndex = 0;
            // 
            // btnMbConn
            // 
            this.btnMbConn.Location = new System.Drawing.Point(7, 7);
            this.btnMbConn.Name = "btnMbConn";
            this.btnMbConn.Size = new System.Drawing.Size(120, 32);
            this.btnMbConn.TabIndex = 0;
            this.btnMbConn.Text = "连接Modbus";
            this.btnMbConn.UseVisualStyleBackColor = true;
            this.btnMbConn.Click += new System.EventHandler(this.btnMbConn_Click);
            // 
            // btnMbDis
            // 
            this.btnMbDis.Enabled = false;
            this.btnMbDis.Location = new System.Drawing.Point(133, 7);
            this.btnMbDis.Name = "btnMbDis";
            this.btnMbDis.Size = new System.Drawing.Size(120, 32);
            this.btnMbDis.TabIndex = 1;
            this.btnMbDis.Text = "断开Modbus";
            this.btnMbDis.UseVisualStyleBackColor = true;
            this.btnMbDis.Click += new System.EventHandler(this.btnMbDis_Click);
            // 
            // btnMbReset
            // 
            this.btnMbReset.Location = new System.Drawing.Point(259, 7);
            this.btnMbReset.Name = "btnMbReset";
            this.btnMbReset.Size = new System.Drawing.Size(120, 32);
            this.btnMbReset.TabIndex = 2;
            this.btnMbReset.Text = "故障复位";
            this.btnMbReset.UseVisualStyleBackColor = true;
            this.btnMbReset.Click += new System.EventHandler(this.btnMbReset_Click);
            // 
            // grpRobot
            // 
            this.grpRobot.Controls.Add(this.pnlPos);
            this.grpRobot.Controls.Add(this.pnlQ);
            this.grpRobot.Controls.Add(this.pnlExec);
            this.grpRobot.Controls.Add(this.pnlTarget);
            this.grpRobot.Controls.Add(this.grpJog);
            this.grpRobot.Controls.Add(this.flpRb);
            this.grpRobot.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpRobot.Location = new System.Drawing.Point(4, 0);
            this.grpRobot.Name = "grpRobot";
            this.grpRobot.Size = new System.Drawing.Size(794, 370);
            this.grpRobot.TabIndex = 0;
            this.grpRobot.TabStop = false;
            this.grpRobot.Text = "机械臂控制";
            // 
            // pnlPos
            // 
            this.pnlPos.Controls.Add(this.lblPX);
            this.pnlPos.Controls.Add(this.lblPY);
            this.pnlPos.Controls.Add(this.lblPZ);
            this.pnlPos.Controls.Add(this.lblVac);
            this.pnlPos.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPos.Location = new System.Drawing.Point(3, 344);
            this.pnlPos.Name = "pnlPos";
            this.pnlPos.Size = new System.Drawing.Size(686, 293);
            this.pnlPos.TabIndex = 5;
            // 
            // lblPX
            // 
            this.lblPX.AutoSize = true;
            this.lblPX.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblPX.Location = new System.Drawing.Point(12, 14);
            this.lblPX.Name = "lblPX";
            this.lblPX.Size = new System.Drawing.Size(132, 35);
            this.lblPX.TabIndex = 0;
            this.lblPX.Text = "当前X：--";
            // 
            // lblPY
            // 
            this.lblPY.AutoSize = true;
            this.lblPY.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblPY.Location = new System.Drawing.Point(12, 42);
            this.lblPY.Name = "lblPY";
            this.lblPY.Size = new System.Drawing.Size(131, 35);
            this.lblPY.TabIndex = 1;
            this.lblPY.Text = "当前Y：--";
            // 
            // lblPZ
            // 
            this.lblPZ.AutoSize = true;
            this.lblPZ.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblPZ.Location = new System.Drawing.Point(12, 70);
            this.lblPZ.Name = "lblPZ";
            this.lblPZ.Size = new System.Drawing.Size(131, 35);
            this.lblPZ.TabIndex = 2;
            this.lblPZ.Text = "当前Z：--";
            // 
            // lblVac
            // 
            this.lblVac.AutoSize = true;
            this.lblVac.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblVac.Location = new System.Drawing.Point(12, 98);
            this.lblVac.Name = "lblVac";
            this.lblVac.Size = new System.Drawing.Size(171, 35);
            this.lblVac.TabIndex = 3;
            this.lblVac.Text = "真空反馈：无";
            // 
            // pnlQ
            // 
            this.pnlQ.Controls.Add(this.btnRunQ);
            this.pnlQ.Controls.Add(this.lblQCount);
            this.pnlQ.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlQ.Location = new System.Drawing.Point(3, 306);
            this.pnlQ.Name = "pnlQ";
            this.pnlQ.Size = new System.Drawing.Size(686, 38);
            this.pnlQ.TabIndex = 4;
            // 
            // btnRunQ
            // 
            this.btnRunQ.Location = new System.Drawing.Point(8, 4);
            this.btnRunQ.Name = "btnRunQ";
            this.btnRunQ.Size = new System.Drawing.Size(130, 30);
            this.btnRunQ.TabIndex = 0;
            this.btnRunQ.Text = "启动队列运行";
            this.btnRunQ.UseVisualStyleBackColor = true;
            this.btnRunQ.Click += new System.EventHandler(this.btnRunQ_Click);
            // 
            // lblQCount
            // 
            this.lblQCount.AutoSize = true;
            this.lblQCount.Location = new System.Drawing.Point(150, 10);
            this.lblQCount.Name = "lblQCount";
            this.lblQCount.Size = new System.Drawing.Size(87, 28);
            this.lblQCount.TabIndex = 1;
            this.lblQCount.Text = "队列：0";
            // 
            // pnlExec
            // 
            this.pnlExec.Controls.Add(this.lblExec);
            this.pnlExec.Controls.Add(this.cmbExec);
            this.pnlExec.Controls.Add(this.btnRelease);
            this.pnlExec.Controls.Add(this.btnAddQ);
            this.pnlExec.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlExec.Location = new System.Drawing.Point(3, 268);
            this.pnlExec.Name = "pnlExec";
            this.pnlExec.Size = new System.Drawing.Size(686, 38);
            this.pnlExec.TabIndex = 3;
            // 
            // lblExec
            // 
            this.lblExec.AutoSize = true;
            this.lblExec.Location = new System.Drawing.Point(8, 10);
            this.lblExec.Name = "lblExec";
            this.lblExec.Size = new System.Drawing.Size(75, 28);
            this.lblExec.TabIndex = 0;
            this.lblExec.Text = "执行器";
            // 
            // cmbExec
            // 
            this.cmbExec.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbExec.FormattingEnabled = true;
            this.cmbExec.Items.AddRange(new object[] {
            "吸盘吸",
            "气爪夹"});
            this.cmbExec.Location = new System.Drawing.Point(66, 6);
            this.cmbExec.Name = "cmbExec";
            this.cmbExec.Size = new System.Drawing.Size(110, 36);
            this.cmbExec.TabIndex = 1;
            this.cmbExec.SelectedIndexChanged += new System.EventHandler(this.cmbExec_SelectedIndexChanged);
            // 
            // btnRelease
            // 
            this.btnRelease.Location = new System.Drawing.Point(186, 4);
            this.btnRelease.Name = "btnRelease";
            this.btnRelease.Size = new System.Drawing.Size(90, 30);
            this.btnRelease.TabIndex = 2;
            this.btnRelease.Text = "吸盘放";
            this.btnRelease.UseVisualStyleBackColor = true;
            this.btnRelease.Click += new System.EventHandler(this.btnRelease_Click);
            // 
            // btnAddQ
            // 
            this.btnAddQ.Location = new System.Drawing.Point(286, 4);
            this.btnAddQ.Name = "btnAddQ";
            this.btnAddQ.Size = new System.Drawing.Size(90, 30);
            this.btnAddQ.TabIndex = 3;
            this.btnAddQ.Text = "添加队列";
            this.btnAddQ.UseVisualStyleBackColor = true;
            this.btnAddQ.Click += new System.EventHandler(this.btnAddQ_Click);
            // 
            // pnlTarget
            // 
            this.pnlTarget.Controls.Add(this.lblTX);
            this.pnlTarget.Controls.Add(this.txtTX);
            this.pnlTarget.Controls.Add(this.lblTY);
            this.pnlTarget.Controls.Add(this.txtTY);
            this.pnlTarget.Controls.Add(this.lblTZ);
            this.pnlTarget.Controls.Add(this.txtTZ);
            this.pnlTarget.Controls.Add(this.btnMoveTo);
            this.pnlTarget.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTarget.Location = new System.Drawing.Point(3, 230);
            this.pnlTarget.Name = "pnlTarget";
            this.pnlTarget.Size = new System.Drawing.Size(686, 38);
            this.pnlTarget.TabIndex = 2;
            // 
            // lblTX
            // 
            this.lblTX.AutoSize = true;
            this.lblTX.Location = new System.Drawing.Point(8, 10);
            this.lblTX.Name = "lblTX";
            this.lblTX.Size = new System.Drawing.Size(68, 28);
            this.lblTX.TabIndex = 0;
            this.lblTX.Text = "目标X";
            // 
            // txtTX
            // 
            this.txtTX.Location = new System.Drawing.Point(66, 7);
            this.txtTX.Name = "txtTX";
            this.txtTX.Size = new System.Drawing.Size(80, 35);
            this.txtTX.TabIndex = 1;
            this.txtTX.Text = "200";
            // 
            // lblTY
            // 
            this.lblTY.AutoSize = true;
            this.lblTY.Location = new System.Drawing.Point(156, 10);
            this.lblTY.Name = "lblTY";
            this.lblTY.Size = new System.Drawing.Size(67, 28);
            this.lblTY.TabIndex = 2;
            this.lblTY.Text = "目标Y";
            // 
            // txtTY
            // 
            this.txtTY.Location = new System.Drawing.Point(214, 7);
            this.txtTY.Name = "txtTY";
            this.txtTY.Size = new System.Drawing.Size(80, 35);
            this.txtTY.TabIndex = 3;
            this.txtTY.Text = "0";
            // 
            // lblTZ
            // 
            this.lblTZ.AutoSize = true;
            this.lblTZ.Location = new System.Drawing.Point(304, 10);
            this.lblTZ.Name = "lblTZ";
            this.lblTZ.Size = new System.Drawing.Size(67, 28);
            this.lblTZ.TabIndex = 4;
            this.lblTZ.Text = "目标Z";
            // 
            // txtTZ
            // 
            this.txtTZ.Location = new System.Drawing.Point(362, 7);
            this.txtTZ.Name = "txtTZ";
            this.txtTZ.Size = new System.Drawing.Size(80, 35);
            this.txtTZ.TabIndex = 5;
            this.txtTZ.Text = "-50";
            // 
            // btnMoveTo
            // 
            this.btnMoveTo.Location = new System.Drawing.Point(452, 4);
            this.btnMoveTo.Name = "btnMoveTo";
            this.btnMoveTo.Size = new System.Drawing.Size(100, 30);
            this.btnMoveTo.TabIndex = 6;
            this.btnMoveTo.Text = "定点运动";
            this.btnMoveTo.UseVisualStyleBackColor = true;
            this.btnMoveTo.Click += new System.EventHandler(this.btnMoveTo_Click);
            // 
            // grpJog
            // 
            this.grpJog.Controls.Add(this.btnXp);
            this.grpJog.Controls.Add(this.btnXm);
            this.grpJog.Controls.Add(this.btnYp);
            this.grpJog.Controls.Add(this.btnYm);
            this.grpJog.Controls.Add(this.btnZp);
            this.grpJog.Controls.Add(this.btnZm);
            this.grpJog.Controls.Add(this.lblStep);
            this.grpJog.Controls.Add(this.cmbStep);
            this.grpJog.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpJog.Location = new System.Drawing.Point(3, 166);
            this.grpJog.Name = "grpJog";
            this.grpJog.Size = new System.Drawing.Size(686, 64);
            this.grpJog.TabIndex = 1;
            this.grpJog.TabStop = false;
            this.grpJog.Text = "点动";
            // 
            // btnXp
            // 
            this.btnXp.Location = new System.Drawing.Point(10, 24);
            this.btnXp.Name = "btnXp";
            this.btnXp.Size = new System.Drawing.Size(56, 30);
            this.btnXp.TabIndex = 0;
            this.btnXp.Tag = "X+";
            this.btnXp.Text = "X+";
            this.btnXp.UseVisualStyleBackColor = true;
            this.btnXp.MouseDown += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseDown);
            this.btnXp.MouseLeave += new System.EventHandler(this.JogButton_MouseLeave);
            this.btnXp.MouseUp += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseUp);
            // 
            // btnXm
            // 
            this.btnXm.Location = new System.Drawing.Point(72, 24);
            this.btnXm.Name = "btnXm";
            this.btnXm.Size = new System.Drawing.Size(56, 30);
            this.btnXm.TabIndex = 1;
            this.btnXm.Tag = "X-";
            this.btnXm.Text = "X-";
            this.btnXm.UseVisualStyleBackColor = true;
            this.btnXm.MouseDown += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseDown);
            this.btnXm.MouseLeave += new System.EventHandler(this.JogButton_MouseLeave);
            this.btnXm.MouseUp += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseUp);
            // 
            // btnYp
            // 
            this.btnYp.Location = new System.Drawing.Point(134, 24);
            this.btnYp.Name = "btnYp";
            this.btnYp.Size = new System.Drawing.Size(56, 30);
            this.btnYp.TabIndex = 2;
            this.btnYp.Tag = "Y+";
            this.btnYp.Text = "Y+";
            this.btnYp.UseVisualStyleBackColor = true;
            this.btnYp.MouseDown += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseDown);
            this.btnYp.MouseLeave += new System.EventHandler(this.JogButton_MouseLeave);
            this.btnYp.MouseUp += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseUp);
            // 
            // btnYm
            // 
            this.btnYm.Location = new System.Drawing.Point(196, 24);
            this.btnYm.Name = "btnYm";
            this.btnYm.Size = new System.Drawing.Size(56, 30);
            this.btnYm.TabIndex = 3;
            this.btnYm.Tag = "Y-";
            this.btnYm.Text = "Y-";
            this.btnYm.UseVisualStyleBackColor = true;
            this.btnYm.MouseDown += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseDown);
            this.btnYm.MouseLeave += new System.EventHandler(this.JogButton_MouseLeave);
            this.btnYm.MouseUp += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseUp);
            // 
            // btnZp
            // 
            this.btnZp.Location = new System.Drawing.Point(258, 24);
            this.btnZp.Name = "btnZp";
            this.btnZp.Size = new System.Drawing.Size(56, 30);
            this.btnZp.TabIndex = 4;
            this.btnZp.Tag = "Z+";
            this.btnZp.Text = "Z+";
            this.btnZp.UseVisualStyleBackColor = true;
            this.btnZp.MouseDown += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseDown);
            this.btnZp.MouseLeave += new System.EventHandler(this.JogButton_MouseLeave);
            this.btnZp.MouseUp += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseUp);
            // 
            // btnZm
            // 
            this.btnZm.Location = new System.Drawing.Point(320, 24);
            this.btnZm.Name = "btnZm";
            this.btnZm.Size = new System.Drawing.Size(56, 30);
            this.btnZm.TabIndex = 5;
            this.btnZm.Tag = "Z-";
            this.btnZm.Text = "Z-";
            this.btnZm.UseVisualStyleBackColor = true;
            this.btnZm.MouseDown += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseDown);
            this.btnZm.MouseLeave += new System.EventHandler(this.JogButton_MouseLeave);
            this.btnZm.MouseUp += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseUp);
            // 
            // lblStep
            // 
            this.lblStep.AutoSize = true;
            this.lblStep.Location = new System.Drawing.Point(390, 31);
            this.lblStep.Name = "lblStep";
            this.lblStep.Size = new System.Drawing.Size(54, 28);
            this.lblStep.TabIndex = 6;
            this.lblStep.Text = "步距";
            // 
            // cmbStep
            // 
            this.cmbStep.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbStep.FormattingEnabled = true;
            this.cmbStep.Items.AddRange(new object[] {
            "1mm",
            "5mm",
            "10mm"});
            this.cmbStep.Location = new System.Drawing.Point(440, 27);
            this.cmbStep.Name = "cmbStep";
            this.cmbStep.Size = new System.Drawing.Size(80, 36);
            this.cmbStep.TabIndex = 7;
            // 
            // flpRb
            // 
            this.flpRb.Controls.Add(this.btnRbConn);
            this.flpRb.Controls.Add(this.btnRbDis);
            this.flpRb.Controls.Add(this.btnHome);
            this.flpRb.Controls.Add(this.btnClearAlarm);
            this.flpRb.Controls.Add(this.btnClearQ);
            this.flpRb.Dock = System.Windows.Forms.DockStyle.Top;
            this.flpRb.Location = new System.Drawing.Point(3, 31);
            this.flpRb.Name = "flpRb";
            this.flpRb.Padding = new System.Windows.Forms.Padding(4);
            // 135 → 46：五个按钮实际只占一行（110+110+80+100+100 + 边距 ≈ 538 < 面板宽），
            // 多出来的 89px 原来是空着的。这 89px 让给了下面的九点标定区（实测过再改的）。
            this.flpRb.Size = new System.Drawing.Size(686, 46);
            this.flpRb.TabIndex = 0;
            // 
            // btnRbConn
            // 
            this.btnRbConn.Location = new System.Drawing.Point(7, 7);
            this.btnRbConn.Name = "btnRbConn";
            this.btnRbConn.Size = new System.Drawing.Size(110, 32);
            this.btnRbConn.TabIndex = 0;
            this.btnRbConn.Text = "连接机械臂";
            this.btnRbConn.UseVisualStyleBackColor = true;
            this.btnRbConn.Click += new System.EventHandler(this.btnRbConn_Click);
            // 
            // btnRbDis
            // 
            this.btnRbDis.Enabled = false;
            this.btnRbDis.Location = new System.Drawing.Point(123, 7);
            this.btnRbDis.Name = "btnRbDis";
            this.btnRbDis.Size = new System.Drawing.Size(110, 32);
            this.btnRbDis.TabIndex = 1;
            this.btnRbDis.Text = "断开机械臂";
            this.btnRbDis.UseVisualStyleBackColor = true;
            this.btnRbDis.Click += new System.EventHandler(this.btnRbDis_Click);
            // 
            // btnHome
            // 
            this.btnHome.Location = new System.Drawing.Point(239, 7);
            this.btnHome.Name = "btnHome";
            this.btnHome.Size = new System.Drawing.Size(80, 32);
            this.btnHome.TabIndex = 2;
            this.btnHome.Text = "回零";
            this.btnHome.UseVisualStyleBackColor = true;
            this.btnHome.Click += new System.EventHandler(this.btnHome_Click);
            // 
            // btnClearAlarm
            // 
            this.btnClearAlarm.Location = new System.Drawing.Point(325, 7);
            this.btnClearAlarm.Name = "btnClearAlarm";
            this.btnClearAlarm.Size = new System.Drawing.Size(100, 32);
            this.btnClearAlarm.TabIndex = 3;
            this.btnClearAlarm.Text = "消除报警";
            this.btnClearAlarm.UseVisualStyleBackColor = true;
            this.btnClearAlarm.Click += new System.EventHandler(this.btnClearAlarm_Click);
            // 
            // btnClearQ
            // 
            this.btnClearQ.Location = new System.Drawing.Point(431, 7);
            this.btnClearQ.Name = "btnClearQ";
            this.btnClearQ.Size = new System.Drawing.Size(100, 32);
            this.btnClearQ.TabIndex = 4;
            this.btnClearQ.Text = "清空队列";
            this.btnClearQ.UseVisualStyleBackColor = true;
            this.btnClearQ.Click += new System.EventHandler(this.btnClearQ_Click);
            // 
            // grpCalib
            // 
            // 只在这里"占位"：里面的控件（表格/按钮/范围框）由 DeviceDebugView.Calib.cs 在
            // InitCalibPanel() 里用代码建 —— 那个 partial 的注释里写了为什么不用设计器。
            // 尺寸 (798,286) 是按运行时实测填的（MainForm 1280x774 → Panel2=798x656，
            // grpRobot 占 370，剩下的全给标定区）。
            this.grpCalib.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpCalib.Location = new System.Drawing.Point(4, 370);
            this.grpCalib.Name = "grpCalib";
            this.grpCalib.Size = new System.Drawing.Size(798, 286);
            this.grpCalib.TabIndex = 1;
            this.grpCalib.TabStop = false;
            this.grpCalib.Text = "九点标定（在 VisionPro 里人工点九个点，把数据填到这里）";
            // 
            // DeviceDebugView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 28F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitMain);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Name = "DeviceDebugView";
            this.Size = new System.Drawing.Size(1100, 640);
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.grpConveyor.ResumeLayout(false);
            this.pnlComm.ResumeLayout(false);
            this.pnlComm.PerformLayout();
            this.pnlCur.ResumeLayout(false);
            this.pnlCur.PerformLayout();
            this.pnlSpeed.ResumeLayout(false);
            this.pnlSpeed.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkSpeed)).EndInit();
            this.flpMove.ResumeLayout(false);
            this.flpConn.ResumeLayout(false);
            this.grpRobot.ResumeLayout(false);
            this.pnlPos.ResumeLayout(false);
            this.pnlPos.PerformLayout();
            this.pnlQ.ResumeLayout(false);
            this.pnlQ.PerformLayout();
            this.pnlExec.ResumeLayout(false);
            this.pnlExec.PerformLayout();
            this.pnlTarget.ResumeLayout(false);
            this.pnlTarget.PerformLayout();
            this.grpJog.ResumeLayout(false);
            this.grpJog.PerformLayout();
            this.flpRb.ResumeLayout(false);
            this.grpCalib.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.GroupBox grpConveyor;
        private System.Windows.Forms.FlowLayoutPanel flpConn;
        private System.Windows.Forms.Button btnMbConn;
        private System.Windows.Forms.Button btnMbDis;
        private System.Windows.Forms.Button btnMbReset;
        private System.Windows.Forms.FlowLayoutPanel flpMove;
        private System.Windows.Forms.Button btnFwd;
        private System.Windows.Forms.Button btnRev;
        private System.Windows.Forms.Button btnStopMove;
        private System.Windows.Forms.Panel pnlSpeed;
        private System.Windows.Forms.Label lblSpeedT;
        private System.Windows.Forms.TrackBar trkSpeed;
        private System.Windows.Forms.Panel pnlCur;
        private System.Windows.Forms.Label lblCurT;
        private System.Windows.Forms.TextBox txtCurSpeed;
        private System.Windows.Forms.Label lblUnit;
        private System.Windows.Forms.Label ledMb;
        private System.Windows.Forms.Panel pnlComm;
        private System.Windows.Forms.Label lblCommT;
        private System.Windows.Forms.TextBox txtComm;
        private System.Windows.Forms.Label lblRunT;
        private System.Windows.Forms.TextBox txtRunState;
        private System.Windows.Forms.GroupBox grpRobot;
        private System.Windows.Forms.GroupBox grpCalib;
        private System.Windows.Forms.FlowLayoutPanel flpRb;
        private System.Windows.Forms.Button btnRbConn;
        private System.Windows.Forms.Button btnRbDis;
        private System.Windows.Forms.Button btnHome;
        private System.Windows.Forms.Button btnClearAlarm;
        private System.Windows.Forms.Button btnClearQ;
        private System.Windows.Forms.GroupBox grpJog;
        private System.Windows.Forms.Button btnXp;
        private System.Windows.Forms.Button btnXm;
        private System.Windows.Forms.Button btnYp;
        private System.Windows.Forms.Button btnYm;
        private System.Windows.Forms.Button btnZp;
        private System.Windows.Forms.Button btnZm;
        private System.Windows.Forms.Label lblStep;
        private System.Windows.Forms.ComboBox cmbStep;
        private System.Windows.Forms.Panel pnlTarget;
        private System.Windows.Forms.Label lblTX;
        private System.Windows.Forms.TextBox txtTX;
        private System.Windows.Forms.Label lblTY;
        private System.Windows.Forms.TextBox txtTY;
        private System.Windows.Forms.Label lblTZ;
        private System.Windows.Forms.TextBox txtTZ;
        private System.Windows.Forms.Button btnMoveTo;
        private System.Windows.Forms.Panel pnlExec;
        private System.Windows.Forms.Label lblExec;
        private System.Windows.Forms.ComboBox cmbExec;
        private System.Windows.Forms.Button btnRelease;
        private System.Windows.Forms.Button btnAddQ;
        private System.Windows.Forms.Panel pnlQ;
        private System.Windows.Forms.Button btnRunQ;
        private System.Windows.Forms.Label lblQCount;
        private System.Windows.Forms.Panel pnlPos;
        private System.Windows.Forms.Label lblPX;
        private System.Windows.Forms.Label lblPY;
        private System.Windows.Forms.Label lblPZ;
        private System.Windows.Forms.Label lblVac;
    }
}
