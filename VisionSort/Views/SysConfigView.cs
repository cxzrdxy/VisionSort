using System.Windows.Forms;

namespace VisionSort.Views
{
    /// <summary>
    /// 对照草稿：04-资料/03系统设置页.png
    /// Modbus传送带配置 / 机械臂配置 / 视觉参数配置 / 数据库配置 + 保存/加载参数。
    /// 已修正草稿 bug：全部输入框补全名、单位、默认值（原 placeholder 为乱码）。
    /// 默认值与《技术补充_可施工设计文档_v1.0.md》§8 AppSettings 保持一致。
    /// </summary>
    public partial class SysConfigView : UserControl
    {
        public SysConfigView()
        {
            InitializeComponent();
        }
    }
}
