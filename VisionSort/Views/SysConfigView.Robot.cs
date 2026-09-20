using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using VisionSort.Services;

namespace VisionSort.Views
{
    /// <summary>
    /// 系统设置页 · 机械臂配置（grpRobotCfg）。
    /// 5 个字段的失焦/勾选校验 + 即时写 <see cref="RobotArmSettings.Shared"/>；
    /// 落盘由 Modbus 那个 partial 的「保存参数/加载参数」负责（sysparam.json 的 robot 组）。
    /// 注意：不重写 OnLoad（已被 SysConfigView.Modbus.cs 占用）；校验用的 WarnAndRevert 也复用那边的。
    ///
    /// 说明：v3.0 起这里不再有"机械臂 IP"——通信目标是本机的 x86 桥接进程（回环），
    /// 机械臂只认 USB 串口（官方 DLL 没有网络通道），所以界面填的是**串口号**。
    /// </summary>
    public partial class SysConfigView
    {
        /// <summary>串口号形如 COM3（不区分大小写）。</summary>
        private static readonly Regex ComPortPattern = new Regex(@"^COM\d+$", RegexOptions.IgnoreCase);

        /// <summary>合法串口号：COMx，或留空/auto（＝让官方 DLL 自动搜索设备）。</summary>
        private static bool IsValidComPort(string value)
        {
            return value.Length == 0
                || string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase)
                || ComPortPattern.IsMatch(value);
        }

        private void txtRbCom_Leave(object sender, EventArgs e)
        {
            string value = txtRbCom.Text.Trim();
            if (!IsValidComPort(value))
            {
                WarnAndRevert(txtRbCom, RobotArmSettings.Shared.SerialPortName,
                    "串口号请填 COM3 这样的形式，或填 auto（让官方 DLL 自动搜索设备）。");
                return;
            }
            RobotArmSettings.Shared.SerialPortName = value;
        }

        private void txtRbPort_Leave(object sender, EventArgs e)
        {
            int port;
            if (!int.TryParse(txtRbPort.Text.Trim(), out port) || port < 1 || port > 65535)
            {
                WarnAndRevert(txtRbPort, RobotArmSettings.Shared.Port.ToString(),
                    "桥接端口请输入 1–65535（默认 18899，一般不用改）。");
                return;
            }
            RobotArmSettings.Shared.Port = port;
        }

        private void chkAutoBridge_CheckedChanged(object sender, EventArgs e)
        {
            RobotArmSettings.Shared.AutoStartBridge = chkAutoBridge.Checked;
        }

        private void txtPickDelay_Leave(object sender, EventArgs e)
        {
            int value;
            if (!TryParseDelayMs(txtPickDelay.Text, out value))
            {
                WarnAndRevert(txtPickDelay, RobotArmSettings.Shared.PickDelayMs.ToString(),
                    "抓取延时请输入 0–60000 的整数（毫秒）。");
                return;
            }
            RobotArmSettings.Shared.PickDelayMs = value;
        }

        private void txtHomeDelay_Leave(object sender, EventArgs e)
        {
            int value;
            if (!TryParseDelayMs(txtHomeDelay.Text, out value))
            {
                WarnAndRevert(txtHomeDelay, RobotArmSettings.Shared.ResetDelayMs.ToString(),
                    "复位延时请输入 0–60000 的整数（毫秒）。");
                return;
            }
            RobotArmSettings.Shared.ResetDelayMs = value;
        }

        private static bool TryParseDelayMs(string text, out int value)
        {
            return int.TryParse(text.Trim(), out value) && value >= 0 && value <= 60000;
        }

        /// <summary>把 Shared 里的生效值推到界面。</summary>
        private void PushRobotToUi()
        {
            RobotArmSettings r = RobotArmSettings.Shared;
            txtRbCom.Text = r.SerialPortName;
            txtRbPort.Text = r.Port.ToString();
            txtPickDelay.Text = r.PickDelayMs.ToString();
            txtHomeDelay.Text = r.ResetDelayMs.ToString();
            chkAutoBridge.Checked = r.AutoStartBridge;
        }

        /// <summary>把界面机械臂组的值收进 Shared；非法值跳过（随后由 PushRobotToUi 回显生效值）。</summary>
        private void PushRobotUiToShared()
        {
            RobotArmSettings r = RobotArmSettings.Shared;
            int number;

            if (IsValidComPort(txtRbCom.Text.Trim())) r.SerialPortName = txtRbCom.Text.Trim();
            if (int.TryParse(txtRbPort.Text.Trim(), out number) && number >= 1 && number <= 65535) r.Port = number;
            if (TryParseDelayMs(txtPickDelay.Text, out number)) r.PickDelayMs = number;
            if (TryParseDelayMs(txtHomeDelay.Text, out number)) r.ResetDelayMs = number;
            r.AutoStartBridge = chkAutoBridge.Checked;

            PushRobotToUi();
        }
    }
}
