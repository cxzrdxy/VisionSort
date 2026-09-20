using System;
using System.Windows.Forms;
using VisionSort.Services;

namespace VisionSort.Views
{
    /// <summary>
    /// 系统设置页 · 视觉参数配置（grpVisionP）+ 与共享设置的同步。
    /// 与 VisionConfigView.Pma.cs 的「匹配阈值」是两码事（Q2=A 职责分离）：
    ///   本页 txtThreshold = 生产判定阈值 → <see cref="VisionRunSettings.Shared"/>.MatchThreshold；
    ///   视觉配置页的阈值下拉 = 训练/自检阈值 → PmaTrainSettings。两边互不写对方。
    ///
    /// 本文件只做：4 个框失焦校验 + 合法值即时写进 Shared；落盘由 Modbus 那个 partial 的保存/加载负责。
    /// 注意：不重写 OnLoad（已被 SysConfigView.Modbus.cs 占用，同名会 CS0111）；校验用的 WarnAndRevert 也复用那边的。
    /// </summary>
    public partial class SysConfigView
    {
        private const double ThresholdMax = 1.0;
        private const int DelayMaxMs = 60000;

        // ------------------------------------------------------------------
        // 四个输入框：失焦校验 + 即时写入 Shared
        // ------------------------------------------------------------------

        private void txtThreshold_Leave(object sender, EventArgs e)
        {
            double value;
            if (!double.TryParse(txtThreshold.Text.Trim(), out value) || value <= 0 || value > ThresholdMax)
            {
                WarnAndRevert(txtThreshold, VisionRunSettings.Shared.MatchThreshold.ToString("0.00"),
                    "匹配分数阈值请输入 0–1 之间的小数（不含 0，如 0.70）。");
                return;
            }
            VisionRunSettings.Shared.MatchThreshold = value;
        }

        private void txtStopDelay_Leave(object sender, EventArgs e)
        {
            int value;
            if (!TryParseDelay(txtStopDelay.Text, out value))
            {
                WarnAndRevert(txtStopDelay, VisionRunSettings.Shared.StopDelayMs.ToString(),
                    "光电触发后停止延时请输入 0–60000 的整数（毫秒）。");
                return;
            }
            VisionRunSettings.Shared.StopDelayMs = value;
        }

        private void txtPhotoWait_Leave(object sender, EventArgs e)
        {
            int value;
            if (!TryParseDelay(txtPhotoWait.Text, out value))
            {
                WarnAndRevert(txtPhotoWait, VisionRunSettings.Shared.PhotoWaitMs.ToString(),
                    "拍照等待时间请输入 0–60000 的整数（毫秒）。");
                return;
            }
            VisionRunSettings.Shared.PhotoWaitMs = value;
        }

        private void txtTimeout_Leave(object sender, EventArgs e)
        {
            int value;
            if (!TryParseDelay(txtTimeout.Text, out value))
            {
                WarnAndRevert(txtTimeout, VisionRunSettings.Shared.DetectTimeoutMs.ToString(),
                    "检测超时请输入 0–60000 的整数（毫秒）。");
                return;
            }
            VisionRunSettings.Shared.DetectTimeoutMs = value;
        }

        /// <summary>时间参数：0–60000 的整数（毫秒）。</summary>
        private static bool TryParseDelay(string text, out int value)
        {
            return int.TryParse(text.Trim(), out value) && value >= 0 && value <= DelayMaxMs;
        }

        // ------------------------------------------------------------------
        // 界面同步（OnLoad 与「加载参数」后调用）
        // ------------------------------------------------------------------

        /// <summary>把 Shared 里的生效值推到界面（界面永远显示真正生效的值）。</summary>
        private void PushVisionToUi()
        {
            VisionRunSettings v = VisionRunSettings.Shared;
            txtThreshold.Text = v.MatchThreshold.ToString("0.00");
            txtStopDelay.Text = v.StopDelayMs.ToString();
            txtPhotoWait.Text = v.PhotoWaitMs.ToString();
            txtTimeout.Text = v.DetectTimeoutMs.ToString();
        }

        /// <summary>把界面视觉组的值收进 Shared；非法值跳过（随后由 PushVisionToUi 回显生效值）。</summary>
        private void PushVisionUiToShared()
        {
            VisionRunSettings v = VisionRunSettings.Shared;
            double threshold;
            int number;

            if (double.TryParse(txtThreshold.Text.Trim(), out threshold) && threshold > 0 && threshold <= ThresholdMax)
                v.MatchThreshold = threshold;
            if (TryParseDelay(txtStopDelay.Text, out number)) v.StopDelayMs = number;
            if (TryParseDelay(txtPhotoWait.Text, out number)) v.PhotoWaitMs = number;
            if (TryParseDelay(txtTimeout.Text, out number)) v.DetectTimeoutMs = number;

            PushVisionToUi();
        }
    }
}
