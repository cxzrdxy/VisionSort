using System.Windows.Forms;

namespace VisionSort.Views
{
    /// <summary>
    /// 对照草稿：04-资料/02工业视觉分拣系统.png（数据日志页）
    /// 已修正草稿 bug：表头重复（时间/时间/图像路径/图像路径）→ 7 列标准表头；
    /// 查询补结果过滤，统计改四卡片，导出按钮保留。
    ///
    /// 逻辑拆分（「数据库方案.md」第 3 步）：
    ///   · DataLogView.Query.cs  —— 查询 / 分页 / 统计卡片 / 结果着色
    ///   · DataLogView.Export.cs —— 手写 xlsx 后台导出
    /// 本文件只负责把 Designer 里没绑的事件接上（草稿的按钮在设计器里都没有 Click 处理）。
    /// </summary>
    public partial class DataLogView : UserControl
    {
        public DataLogView()
        {
            InitializeComponent();
            WireLogEvents();
        }
    }
}
