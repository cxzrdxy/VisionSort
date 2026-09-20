using System.Windows.Forms;

namespace VisionSort.Views
{
    /// <summary>
    /// 对照草稿：04-资料/05设备调试页.png
    /// 左侧传送带 Modbus 控制，右侧机械臂控制（点动/定点/队列）。
    /// 已修正草稿 bug：jog 乱文本、双“当前Y”、缺真空反馈。
    /// </summary>
    public partial class DeviceDebugView : UserControl
    {
        public DeviceDebugView()
        {
            InitializeComponent();
        }
    }
}
