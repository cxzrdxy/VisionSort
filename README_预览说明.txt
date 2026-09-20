工业视觉分拣系统 · WinForms 界面（对照 04-资料草稿图）
==================================================

一、怎么在 Visual Studio 里打开预览（VS2019 及以上，x64）
1. 双击 VisionSort.sln。
2. 解决方案资源管理器里：
   - MainForm.cs → 右键“视图设计器”（Shift+F7）：看顶部标题 + 5 页 Tab 导航。
   - Views/MainRunView.cs      → 主运行页（对照 01主运行页.png）
   - Views/DataLogView.cs      → 数据日志页（对照 02工业视觉分拣系统.png）
   - Views/SysConfigView.cs    → 系统设置页（对照 03系统设置页.png）
   - Views/VisionConfigView.cs → 视觉配置页（对照 04视觉配置页.png）
   - Views/DeviceDebugView.cs  → 设备调试页（对照 05设备调试页.png）
3. 生成 → 生成解决方案（平台须选 x64，目标 net48）。
   注意：界面层是纯 WinForms，零第三方引用，在没装 VisionPro 的机器上
   也能正常预览和编译。

二、和草稿图的对应关系与修正（均已在界面里改掉，草稿原样不动）
- 01主运行页：导航高亮错位不管；“检测NG+工件编号”改为“工件编号”配方下拉；
  状态按钮改为 LED 语义；新增“急停/报警复位”。
- 02数据日志：表头重复（时间/时间/图像路径/图像路径）→ 改为
  时间/工件编号/配方/检测结果/匹配分数/图像路径/备注；查询补“结果”过滤。
- 03系统设置：全部乱码 placeholder 已填真值（IP/端口/寄存器/延时/阈值），
  默认值与《技术补充_可施工设计文档_v1.0.md》§8 一致。
- 04视觉配置：双“增益”合并为 曝光(ms)+增益(dB)；延迟级别/包大小/触发模式
  只读显示；阈值显示配方值；模板预览用黑底 PictureBox（别再用医学图）。
- 05设备调试：jog 区“+/--”乱文本删除；双“当前Y”改为 X/Y/Z 各一行；
  点动补步距下拉；执行器/队列/真空反馈补齐；Modbus 补故障复位。

三、联调时换 Cognex 控件的位置（共 3 处，按名找）
1. MainRunView.pnlDisplay  → 删掉 lblDisplayTip，把 CogRecordDisplay Dock=Fill 放进来。
2. VisionConfigView.pnlVppHost → 放 CogToolBlockEdit（Subject = ToolBlock）。
3. VisionConfigView.picTemplate → 训练模板预览（PMA.CreateCurrentRecord 叠加显示）。
VPP 加载/Run/存图代码写法见 20260914-笔记/联合编程01.md（§三/§四/§八）。

四、目录
VisionSort.sln
VisionSort/
  VisionSort.csproj / App.config / Program.cs
  MainForm.cs(+.Designer.cs/.resx)
  Views/
    MainRunView / DeviceDebugView / VisionConfigView / DataLogView / SysConfigView
    （各 .cs + .Designer.cs + .resx，VS 设计器逐个可看）
