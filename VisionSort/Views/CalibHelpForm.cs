using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace VisionSort.Views
{
    /// <summary>
    /// 「操作指引」弹窗（「九点标定联机分拣方案.md」§4.10.1）。
    ///
    /// 显示的是**嵌入资源**里的《九点标定操作指引.md》原文 —— 单一真相源，不是代码里另抄的一份。
    /// 为什么不做富文本渲染：少一个依赖、少一处出错；Markdown 原文用等宽字体读起来也是清楚的，
    /// 而且现场要的是"能照着做"，不是"好看"。
    ///
    /// 这个窗体用手写代码搭、不建 .Designer.cs：它是一个只读文本查看器，控件就三五个，
    /// 用设计器反而多一份要同步的样板（与 MainRunView 运行时建 CogRecordDisplay 同一个理由）。
    /// </summary>
    internal sealed class CalibHelpForm : Form
    {
        /// <summary>嵌入资源名（与 VisionSort.csproj 里的 LogicalName 一致）。</summary>
        private const string ResourceName = "VisionSort.九点标定操作指引.md";

        /// <summary>磁盘上的文件名（用于「打开所在文件夹」）。</summary>
        private const string FileName = "九点标定操作指引.md";

        /// <summary>向上找文件的层数上限（exe 在 bin\x64\Debug\net48\ 下，要爬 5 层到 04-WinForms）。</summary>
        private const int MaxParentLevels = 8;

        private readonly string _text;

        private CalibHelpForm(string text)
        {
            _text = text ?? string.Empty;

            Text = "九点标定操作指引";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(940, 720);
            MinimumSize = new Size(520, 360);
            ShowIcon = false;
            MinimizeBox = false;

            TextBox view = new TextBox();
            view.Multiline = true;
            view.ReadOnly = true;
            // 自动换行：**开着**。实测第一版关了它（怕表格被拆），结果整篇指引变成一条长龙，
            // 只能靠横向滚动条读 —— 指引是给人从头读到尾的，可读性比表格对齐重要。
            // 窗口默认 940 宽、Consolas 10pt 一行约 100 字符，正文基本不会折行，只有表格会。
            view.WordWrap = true;
            view.ScrollBars = ScrollBars.Vertical;
            view.Dock = DockStyle.Fill;
            view.BackColor = Color.White;
            view.Font = new Font("Consolas", 10F);   // 等宽：表格对齐靠它
            view.Text = _text;
            view.Select(0, 0);
            // Ctrl+A / Ctrl+C 在只读多行框里默认就可用，不再额外处理

            Panel bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 48;

            Button copy = new Button();
            copy.Text = "复制到剪贴板";
            copy.Size = new Size(130, 30);
            copy.Location = new Point(12, 9);
            copy.Click += Copy_Click;

            Button folder = new Button();
            folder.Text = "打开所在文件夹";
            folder.Size = new Size(140, 30);
            folder.Location = new Point(150, 9);
            folder.Click += OpenFolder_Click;

            Button close = new Button();
            close.Text = "关闭";
            close.Size = new Size(90, 30);
            close.Location = new Point(ClientSize.Width - 104, 9);
            close.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            close.DialogResult = DialogResult.OK;
            close.Click += delegate { Close(); };

            Label hint = new Label();
            hint.AutoSize = true;
            hint.ForeColor = Color.DimGray;
            hint.Location = new Point(304, 17);
            hint.Text = "内容来自《九点标定操作指引.md》（编译进程序，改了文档重新编译即可）";

            bottom.Controls.Add(copy);
            bottom.Controls.Add(folder);
            bottom.Controls.Add(close);
            bottom.Controls.Add(hint);

            Controls.Add(view);
            Controls.Add(bottom);

            AcceptButton = close;
            CancelButton = close;
        }

        /// <summary>弹出指引（读不到嵌入资源时也给得出中文原因，不静默空白）。</summary>
        public static void ShowHelp(IWin32Window owner)
        {
            string text;
            try
            {
                text = ReadEmbeddedText();
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner,
                    "读不到操作指引（嵌入资源 " + ResourceName + "）：\r\n" + ex.Message
                    + "\r\n\r\n程序可能不是正常编译出来的，请重新生成解决方案。",
                    "操作指引", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (CalibHelpForm form = new CalibHelpForm(text))
                form.ShowDialog(owner);
        }

        /// <summary>把嵌入资源读成字符串。</summary>
        private static string ReadEmbeddedText()
        {
            Assembly assembly = typeof(CalibHelpForm).Assembly;
            using (Stream stream = assembly.GetManifestResourceStream(ResourceName))
            {
                if (stream == null)
                {
                    string[] names = assembly.GetManifestResourceNames();
                    throw new InvalidOperationException(
                        "程序里没有这个名字的资源。现有资源：" +
                        (names.Length == 0 ? "（一个都没有）" : string.Join("、", names)));
                }

                // 文件是 UTF-8（无 BOM）；用检测 BOM 的 StreamReader，两种都能读。
                using (StreamReader reader = new StreamReader(stream, new UTF8Encoding(false), true))
                    return reader.ReadToEnd();
            }
        }

        private void Copy_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(_text);
                MessageBox.Show(this, "操作指引全文已复制到剪贴板（" + _text.Length + " 个字符）。",
                    "操作指引", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "复制失败：" + ex.Message, "操作指引",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenFolder_Click(object sender, EventArgs e)
        {
            try
            {
                string path = FindFileOnDisk();
                if (path == null)
                {
                    MessageBox.Show(this,
                        "在程序目录往上 " + MaxParentLevels + " 层里没找到《" + FileName + "》。\r\n"
                        + "它属于项目文档，正常在 20260914-资料\\04-WinForms\\ 下。\r\n"
                        + "（不影响阅读：上面框里显示的是编译进程序的同一份文字）",
                        "操作指引", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Process.Start("explorer.exe", "/select,\"" + path + "\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "打开失败：" + ex.Message, "操作指引",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>在磁盘上找那份 .md：先在 exe 同目录找，再一层层往上找。</summary>
        private static string FindFileOnDisk()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;

            string same = Path.Combine(dir, FileName);
            if (File.Exists(same)) return same;

            DirectoryInfo info = new DirectoryInfo(dir);
            for (int i = 0; i < MaxParentLevels && info != null && info.Parent != null; i++)
            {
                info = info.Parent;
                string candidate = Path.Combine(info.FullName, FileName);
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }
    }
}
