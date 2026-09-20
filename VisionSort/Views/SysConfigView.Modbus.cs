using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using VisionSort.Services;

namespace VisionSort.Views
{
    /// <summary>
    /// 系统设置页 · Modbus 传送带配置（grpModbus）+ 底部「保存参数 / 加载参数」。
    /// 本文件只做三件事：
    ///   ① 串口号/波特率/站号/三个寄存器的失焦校验（非法值警告并恢复 Shared 里的旧值）；
    ///   ② 改动即时写进 <see cref="ConveyorSettings.Shared"/>，调试页永远看到最新值，不必先点保存；
    ///   ③ 保存/加载参数：整页 4 组存 config\sysparam.json（Q3=A）。
    /// 注意：本 partial 不重写 Dispose（Designer 已占用，同名会 CS0111）；OnLoad 未被占用，可用。
    /// </summary>
    public partial class SysConfigView
    {
        private const string SysParamFileName = "sysparam.json";

        /// <summary>首次显示时把 Shared（唯一真相源）推给界面。</summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            PushModbusToUi();
            PushVisionToUi();   // 视觉参数配置组同理（实现在 SysConfigView.Vision.cs）
            PushRobotToUi();    // 机械臂配置组（实现在 SysConfigView.Robot.cs）
            PushDbToUi();       // 数据库配置组（实现在 SysConfigView.Db.cs）
        }

        // ------------------------------------------------------------------
        // Modbus 输入框：失焦校验 + 即时写入 Shared
        // ------------------------------------------------------------------

        private void txtComPort_Leave(object sender, EventArgs e)
        {
            string value = txtComPort.Text.Trim();
            if (value.Length == 0)
            {
                WarnAndRevert(txtComPort, ConveyorSettings.Shared.PortName, "串口号不能为空（如 COM5）。");
                return;
            }
            ConveyorSettings.Shared.PortName = value;
        }

        private void txtBaud_Leave(object sender, EventArgs e)
        {
            int baud;
            if (!int.TryParse(txtBaud.Text.Trim(), out baud) || baud <= 0)
            {
                WarnAndRevert(txtBaud, ConveyorSettings.Shared.BaudRate.ToString(), "波特率请输入正整数（如 9600）。");
                return;
            }
            ConveyorSettings.Shared.BaudRate = baud;
        }

        private void txtSlaveId_Leave(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(txtSlaveId.Text.Trim(), out id) || id < 1 || id > 247)
            {
                WarnAndRevert(txtSlaveId, ConveyorSettings.Shared.SlaveId.ToString(), "站号请输入 1–247。");
                return;
            }
            ConveyorSettings.Shared.SlaveId = (byte)id;
        }

        private void txtRegFwd_Leave(object sender, EventArgs e)
        {
            ushort addr;
            if (!ushort.TryParse(txtRegFwd.Text.Trim(), out addr))
            {
                WarnAndRevert(txtRegFwd, ConveyorSettings.Shared.RegFwd.ToString(), "寄存器地址请输入 0–65535。");
                return;
            }
            ConveyorSettings.Shared.RegFwd = addr;
        }

        private void txtRegRev_Leave(object sender, EventArgs e)
        {
            ushort addr;
            if (!ushort.TryParse(txtRegRev.Text.Trim(), out addr))
            {
                WarnAndRevert(txtRegRev, ConveyorSettings.Shared.RegRev.ToString(), "寄存器地址请输入 0–65535。");
                return;
            }
            ConveyorSettings.Shared.RegRev = addr;
        }

        private void txtRegSpeed_Leave(object sender, EventArgs e)
        {
            ushort addr;
            if (!ushort.TryParse(txtRegSpeed.Text.Trim(), out addr))
            {
                WarnAndRevert(txtRegSpeed, ConveyorSettings.Shared.RegSpeed.ToString(), "寄存器地址请输入 0–65535。");
                return;
            }
            ConveyorSettings.Shared.RegSpeed = addr;
        }

        private void WarnAndRevert(TextBox box, string goodValue, string message)
        {
            MessageBox.Show(this, message, "系统设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            box.Text = goodValue;
        }

        private void PushModbusToUi()
        {
            ConveyorSettings s = ConveyorSettings.Shared;
            txtComPort.Text = s.PortName;
            txtBaud.Text = s.BaudRate.ToString();
            txtSlaveId.Text = s.SlaveId.ToString();
            txtRegFwd.Text = s.RegFwd.ToString();
            txtRegRev.Text = s.RegRev.ToString();
            txtRegSpeed.Text = s.RegSpeed.ToString();
        }

        // ------------------------------------------------------------------
        // 保存参数 / 加载参数（整页 4 组）
        // ------------------------------------------------------------------

        private static string SysParamPath
        {
            get { return Path.Combine(Application.StartupPath, "config", SysParamFileName); }
        }

        private void btnSaveParam_Click(object sender, EventArgs e)
        {
            try
            {
                this.ActiveControl = null;   // 逼出正在编辑的框的 Leave，保证 Shared 是最新值

                Dictionary<string, Dictionary<string, string>> all =
                    new Dictionary<string, Dictionary<string, string>>();

                Dictionary<string, string> modbus = new Dictionary<string, string>();
                modbus["txtComPort"] = txtComPort.Text.Trim();
                modbus["txtBaud"] = txtBaud.Text.Trim();
                modbus["txtSlaveId"] = txtSlaveId.Text.Trim();
                modbus["txtRegFwd"] = txtRegFwd.Text.Trim();
                modbus["txtRegRev"] = txtRegRev.Text.Trim();
                modbus["txtRegSpeed"] = txtRegSpeed.Text.Trim();
                all["modbus"] = modbus;

                Dictionary<string, string> robot = new Dictionary<string, string>();
                robot["txtRbCom"] = txtRbCom.Text.Trim();
                robot["txtRbPort"] = txtRbPort.Text.Trim();
                robot["txtPickDelay"] = txtPickDelay.Text.Trim();
                robot["txtHomeDelay"] = txtHomeDelay.Text.Trim();
                robot["chkAutoBridge"] = chkAutoBridge.Checked ? "1" : "0";
                all["robot"] = robot;

                Dictionary<string, string> vision = new Dictionary<string, string>();
                vision["txtThreshold"] = txtThreshold.Text.Trim();
                vision["txtStopDelay"] = txtStopDelay.Text.Trim();
                vision["txtPhotoWait"] = txtPhotoWait.Text.Trim();
                vision["txtTimeout"] = txtTimeout.Text.Trim();
                all["vision"] = vision;

                Dictionary<string, string> db = new Dictionary<string, string>();
                // 存**类型名**而不是下标：下标会随下拉项变化而改变含义（把 SQLite 那三项换成 MySQL 这一项时
                // 老的 "0" 就换了意思），名字则永远自解释，人打开 sysparam.json 也能看懂。
                db["cmbDbType"] = cmbDbType.SelectedItem == null ? DbSettings.Shared.DbType : cmbDbType.SelectedItem.ToString();
                db["txtConnStr"] = txtConnStr.Text.Trim();
                all["db"] = db;

                string path = SysParamPath;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(path, ToJson(all), Encoding.UTF8);

                MessageBox.Show(this, "参数已保存：\r\n" + path, "系统设置",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "保存参数失败：\r\n" + ex.Message, "系统设置",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnLoadParam_Click(object sender, EventArgs e)
        {
            try
            {
                string path = SysParamPath;
                if (!File.Exists(path))
                {
                    MessageBox.Show(this, "还没有保存过的参数文件：\r\n" + path, "系统设置",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Dictionary<string, Dictionary<string, string>> all = FromJson(File.ReadAllText(path, Encoding.UTF8));

                RestoreGroup(all, "modbus");
                RestoreGroup(all, "robot");
                RestoreGroup(all, "vision");

                Dictionary<string, string> db;
                if (all.TryGetValue("db", out db))
                {
                    string raw;
                    if (db.TryGetValue("cmbDbType", out raw) && raw.Length > 0)
                    {
                        int index = cmbDbType.Items.IndexOf(raw);   // 新格式：类型名
                        if (index < 0)
                        {
                            int legacy;                              // 老格式：下标（兼容已有 sysparam.json）
                            if (int.TryParse(raw, out legacy) && legacy >= 0 && legacy < cmbDbType.Items.Count)
                                index = legacy;
                        }
                        if (index >= 0) cmbDbType.SelectedIndex = index;
                    }
                    if (db.TryGetValue("txtConnStr", out raw)) txtConnStr.Text = raw;
                }

                PushUiToShared();   // Modbus 组回读后收进 Shared（非法值保留旧值）
                MessageBox.Show(this, "参数已加载。", "系统设置",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "加载参数失败（文件损坏？）：\r\n" + ex.Message, "系统设置",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>按控件名把一组值写回界面（认 TextBox 与 CheckBox）。</summary>
        private void RestoreGroup(Dictionary<string, Dictionary<string, string>> all, string group)
        {
            Dictionary<string, string> dict;
            if (!all.TryGetValue(group, out dict)) return;

            foreach (KeyValuePair<string, string> kv in dict)
            {
                Control[] found = this.Controls.Find(kv.Key, true);
                if (found.Length == 0) continue;

                TextBox textBox = found[0] as TextBox;
                if (textBox != null) { textBox.Text = kv.Value; continue; }

                CheckBox checkBox = found[0] as CheckBox;
                if (checkBox != null) checkBox.Checked = kv.Value == "1";
            }
        }

        /// <summary>把界面 Modbus 组的值收进 Shared；非法值跳过，随后界面恢复显示合法值。</summary>
        private void PushUiToShared()
        {
            ConveyorSettings s = ConveyorSettings.Shared;
            int number;
            ushort addr;

            if (txtComPort.Text.Trim().Length > 0) s.PortName = txtComPort.Text.Trim();
            if (int.TryParse(txtBaud.Text.Trim(), out number) && number > 0) s.BaudRate = number;
            if (int.TryParse(txtSlaveId.Text.Trim(), out number) && number >= 1 && number <= 247) s.SlaveId = (byte)number;
            if (ushort.TryParse(txtRegFwd.Text.Trim(), out addr)) s.RegFwd = addr;
            if (ushort.TryParse(txtRegRev.Text.Trim(), out addr)) s.RegRev = addr;
            if (ushort.TryParse(txtRegSpeed.Text.Trim(), out addr)) s.RegSpeed = addr;

            PushModbusToUi();
            PushVisionUiToShared();   // 视觉组同样"校验后回显"（实现在 SysConfigView.Vision.cs）
            PushRobotUiToShared();    // 机械臂组同样（实现在 SysConfigView.Robot.cs）
            PushDbUiToShared();       // 数据库组同样（实现在 SysConfigView.Db.cs）
        }

        // ---- 极简 JSON：只处理本页写出的 {"组":{"键":"值"}} 扁平结构 ----

        private static string JsonEscape(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string ToJson(Dictionary<string, Dictionary<string, string>> all)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{\r\n");
            bool firstGroup = true;
            foreach (KeyValuePair<string, Dictionary<string, string>> group in all)
            {
                if (!firstGroup) sb.Append(",\r\n");
                firstGroup = false;
                sb.Append("  \"").Append(group.Key).Append("\": {\r\n");

                bool first = true;
                foreach (KeyValuePair<string, string> kv in group.Value)
                {
                    if (!first) sb.Append(",\r\n");
                    first = false;
                    sb.Append("    \"").Append(kv.Key).Append("\": \"").Append(JsonEscape(kv.Value)).Append("\"");
                }
                sb.Append("\r\n  }");
            }
            sb.Append("\r\n}\r\n");
            return sb.ToString();
        }

        private static Dictionary<string, Dictionary<string, string>> FromJson(string text)
        {
            Dictionary<string, Dictionary<string, string>> all =
                new Dictionary<string, Dictionary<string, string>>();

            int i = 0;
            SkipWs(text, ref i);
            Expect(text, ref i, '{');

            while (true)
            {
                SkipWs(text, ref i);
                if (Peek(text, i) == '}')
                {
                    i++;
                    break;
                }

                string group = ReadString(text, ref i);
                SkipWs(text, ref i);
                Expect(text, ref i, ':');
                SkipWs(text, ref i);
                Expect(text, ref i, '{');

                Dictionary<string, string> dict = new Dictionary<string, string>();
                while (true)
                {
                    SkipWs(text, ref i);
                    if (Peek(text, i) == '}')
                    {
                        i++;
                        break;
                    }

                    string key = ReadString(text, ref i);
                    SkipWs(text, ref i);
                    Expect(text, ref i, ':');
                    SkipWs(text, ref i);
                    dict[key] = ReadString(text, ref i);
                    SkipWs(text, ref i);
                    if (Peek(text, i) == ',') i++;
                }

                all[group] = dict;
                SkipWs(text, ref i);
                if (Peek(text, i) == ',') i++;
            }

            return all;
        }

        private static void SkipWs(string text, ref int i)
        {
            while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
        }

        private static char Peek(string text, int i)
        {
            return i < text.Length ? text[i] : '\0';
        }

        private static void Expect(string text, ref int i, char expected)
        {
            if (Peek(text, i) != expected) throw new FormatException("JSON 格式错误（位置 " + i + "）。");
            i++;
        }

        private static string ReadString(string text, ref int i)
        {
            Expect(text, ref i, '"');
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                if (i >= text.Length) throw new FormatException("JSON 字符串未闭合。");
                char c = text[i++];
                if (c == '"') break;

                if (c == '\\')
                {
                    if (i >= text.Length) throw new FormatException("JSON 转义未闭合。");
                    char escaped = text[i++];
                    if (escaped == '"') sb.Append('"');
                    else if (escaped == '\\') sb.Append('\\');
                    else sb.Append(escaped);
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
