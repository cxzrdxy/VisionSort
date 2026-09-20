using System.Windows.Forms;

namespace VisionSort.Views
{
    /// <summary>
    /// 对照草稿：04-资料/01主运行页.png
    /// 左侧图像预览（pnlDisplay 为 CogRecordDisplay 预留位）+ 右侧运行控制/检测统计。
    ///
    /// 逻辑拆分（「主运行页方案.md」第 2 步）：
    ///   · Services\ProductionRunner.cs —— 状态机与设备编排（本页不做任何设备判断）
    ///   · MainRunView.Run.cs           —— 按钮意图 + 状态/灯/统计/预览的显示
    /// 本文件只负责把 Designer 里没绑的事件接上（草稿的按钮在设计器里都没有 Click 处理）。
    /// </summary>
    public partial class MainRunView : UserControl
    {
        public MainRunView()
        {
            InitializeComponent();
            WireRunEvents();
        }
    }
}
