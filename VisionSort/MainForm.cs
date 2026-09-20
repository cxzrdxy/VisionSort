using System.Windows.Forms;
using VisionSort.Views;

namespace VisionSort
{
    /// <summary>
    /// 主窗体：顶部标题 + Tab 导航（5 页），每页挂载 Views 下的一个视图。
    /// 设计器预览本窗体只显示 Tab 结构；各页细节请单独打开 Views/*.cs [设计]。
    /// </summary>
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            this.tabPageRun.Controls.Add(new MainRunView() { Dock = DockStyle.Fill });
            this.tabPageDevice.Controls.Add(new DeviceDebugView() { Dock = DockStyle.Fill });
            this.tabPageVision.Controls.Add(new VisionConfigView() { Dock = DockStyle.Fill });
            this.tabPageLog.Controls.Add(new DataLogView() { Dock = DockStyle.Fill });
            this.tabPageSetup.Controls.Add(new SysConfigView() { Dock = DockStyle.Fill });
        }
    }
}
