using Cognex.VisionPro;
using Cognex.VisionPro.Display;
using Cognex.VisionPro.FGGigE;
using System;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace VisionSort.Views
{
    /// <summary>
    /// 视觉配置页 · 相机设置（grpCamera 分组）。
    /// 同一个 partial 类再拆一份：
    ///   VisionConfigView.cs          —— VPP 方案加载/保存
    ///   VisionConfigView.Pma.cs      —— PMA 模板训练
    ///   VisionConfigView.Save.cs     —— 图像保存配置的界面逻辑
    ///   本文件                        —— GigE 相机连接/参数/拍照/预览
    ///
    /// 路线：纯 VisionPro CogAcqFifoTool（FGGigE），不引第三方相机 SDK。
    /// 本页自建独占一个 FIFO，不动方案里的 Image Source；
    /// 拍到的帧通过 Pma.cs 预留的 TrainingImageProvider 共享给训练页（借图不抢相机）。
    /// **配置不落盘**：界面初值来自 Designer，连上后由相机现值覆盖（相机才是真相源）。
    ///
    /// API 依据（反射探针 + 官方 XML 逐项确认，59.2.0.0）：
    ///   枚举  CogFrameGrabberGigEs.Count / Item[int] → ICogFrameGrabber
    ///   格式  **必须**取 ICogFrameGrabber.AvailableVideoFormats（官方 XML：CreateAcqFifo 的
    ///         videoFormat"Must be one of the strings returned by AvailableVideoFormats"）。
    ///         不能用 GetAvailableVideoFormatOptions 的返回值——那是"选项组件"拼串，
    ///         文档说它的 key 才是 AvailableVideoFormats 的下标（v1.3 在这里踩过坑）。
    ///   绑定  grabber.CreateAcqFifo(videoFormat, Format8Grey, 0, true) → fifoTool.Operator
    ///   曝光  OwnedExposureParams.Exposure（毫秒，与界面一致，无需换算）
    ///   增益  无模拟增益标准接口（DigitalCameraGain 只有 bool）→ GenICam "Gain" 节点
    ///   包    OwnedGigEVisionTransportParams.PacketSize / PacketSizeMax（只读上限）/ LatencyLevel
    ///   超时  ICogAcqFifo.Timeout（毫秒，默认 10000）+ TimeoutEnabled（默认 true）
    ///   触发  OwnedTriggerParams.TriggerModel（Manual=0 / Auto=1 / Semi=2 / Slave=3 / FreeRun=4）+ TriggerEnabled
    ///         文档：TriggerModel 为 Auto 时 StartLiveDisplay 的 own 必须为 true。
    ///         下拉 cmbTrigMode 默认「自动探测」= **不写相机**，连接时按 保持原样 → FreeRun → Manual
    ///         依次试取，哪种出图用哪种（现场那台相机是"手动"才出图，所以绝不能硬写 FreeRun）；
    ///         选「强制：…」= 按所选模式写相机，仍试取一帧验证，取不到就还原相机原配置并明确报错；
    ///         从强制切回「自动探测」时，把相机还原成连接时记下的原始触发配置。
    ///         Auto 模式下『单次拍照』的软件触发取不到图，已在按钮里先提示、不让用户干等超时。
    ///   预览  CogDisplay.StartLiveDisplay(object, bool) / StopLiveDisplay()
    ///         own 参数由 LiveDisplayOwnsFifo 决定：TriggerModel=Auto 时按文档必须为 true。
    ///   状态  lblTrigMode 显示读回来的真实触发状态 + 是"原配置"还是"已切换"。
    ///
    /// 现场实测（Hikrobot MV-CS060-10GC，DB1711685/DB1711521）：
    ///   视频格式 4 选 1：Generic GigEVision (Bayer Color / Mono / RGB8 / YUV422 Packed) → 用 Bayer Color；
    ///   VisionPro 自家可用配置：曝光 5 ms、包大小 **1508**、延迟级别 3、传输超时 2000 ms。
    ///   v1.3 曾写死包 9000 → 网卡没开巨帧 → 一帧不来 → 预览只有空白（"蓝屏"）。
    /// </summary>
    public partial class VisionConfigView
    {
        private const double DefaultExposureMs = 8.0;   // 与 Designer 的 txtExposure.Text 对齐（未连接时的占位值）
        private const int DefaultPacketSize = 1508;     // 标准 1500 MTU 的 GigE 包大小（VisionPro 现场实测值）
        private const string GainNodeName = "Gain";     // GenICam 标准增益节点（float, dB）
        private const string PreferredFormatTag = "Bayer";   // 优先挑的视频格式关键字（现场用 Bayer Color）
        private const int ProbeTimeoutMs = 2000;        // 每次试取一帧的超时（与现场"传输超时 2000 毫秒"一致）

        /// <summary>
        /// 取像 FIFO。**设备对象已归 <c>CameraService.Shared</c> 共享**（主运行页要用同一台相机，
        /// 一台 GigE 相机只能被一个 FIFO 打开，见 主运行页方案.md Q1=A）。
        /// 这里保留同名的属性代理，本文件原有的连接/参数/触发逻辑**一行都不用改**。
        /// </summary>
        private CogAcqFifoTool _camFifo
        {
            get { return Services.CameraService.Shared.Fifo; }
            set { Services.CameraService.Shared.Fifo = value; }
        }
        private string _camName = "";           // 已连接相机的显示名（Name/SerialNumber）
        private string _camVideoFormat = "";    // 实际生效的视频格式（读回，写进小窗标题）
        private ICogImage _lastFrame;           // 最近一次单帧
        private Form _previewBox;               // 预览小窗单例（null = 没打开）
        private CogDisplay _previewDisplay;     // 小窗里的显示控件
        private bool _previewLive;              // 连续预览进行中
        private bool _camBusy;                  // 防重入（同步取图时）
        private bool _cleanupHooked;            // Disposed 钩子只挂一次
        private double _lastValidExposure = DefaultExposureMs;

        // 参数策略用的状态：界面值 vs 相机真值
        private int _camPacketSize;             // 相机当前包大小（读回/写成功后更新）
        private int _camLatencyLevel;           // 相机当前延迟级别 0-3（读回/写成功后更新）
        private bool _exposureEdited;           // 用户在界面上改过曝光 → 连接时按界面值写入
        private bool _packetEdited;             // 同上，包大小
        private bool _latencyEdited;            // 同上，延迟级别
        private bool _uiSyncing;                // 程序改下拉时抑制事件（读回填界面不会被当成用户改动写回相机）
        private bool _triggerKeptAsIs;          // 触发用的是相机原配置（true）还是本页试出来的（false）
        private bool _triggerModeEdited;        // 用户在下拉里选过"强制"模式 → 连接时按它写相机；否则保持相机原配置并自动试取
        private bool _hasOriginalTrigger;       // 是否已记下相机原始触发配置（切回"自动探测"时还原用）
        private bool _originalTriggerEnabled;   // 相机原始触发使能
        private CogAcqTriggerModelConstants _originalTriggerModel;   // 相机原始触发模式

        // 注意：不新增 OnLoad 重写（Save.cs 已占用，同名会 CS0111）；
        // 下拉默认选中全部放 Designer，曝光初值与 DefaultExposureMs 对齐即可。

        // ------------------------------------------------------------------
        // 连接 / 断开
        // ------------------------------------------------------------------

        /// <summary>『连接相机』：枚举 → 取第一台 → 建 FIFO → 应用参数 → 共享取帧钩子。</summary>
        private void btnCamConn_Click(object sender, EventArgs e)
        {
            if (_camFifo != null)
            {
                MessageBox.Show(this, "相机已连接，如需重连请先断开。", "相机设置",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ICogFrameGrabber grabber = null;
            int found = 0;
            try
            {
                CogFrameGrabberGigEs giges = new CogFrameGrabberGigEs();
                found = giges.Count;
                if (found > 0) grabber = giges[0];
            }
            catch (Exception ex)
            {
                ShowCamError("枚举相机", ex);
                return;
            }

            if (grabber == null)
            {
                MessageBox.Show(this, "未发现 GigE 相机，请检查网线/供电/网卡巨帧/防火墙。",
                    "相机设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CogAcqFifoTool fifo = null;
            try
            {
                // 视频格式：必须取自 AvailableVideoFormats（官方 XML 规定），优先挑 Bayer Color
                string videoFormat = PickVideoFormat(grabber);
                if (videoFormat == null)
                    throw new InvalidOperationException("该相机没有可用视频格式。");

                fifo = new CogAcqFifoTool();
                fifo.Operator = grabber.CreateAcqFifo(
                    videoFormat, CogAcqFifoPixelFormatConstants.Format8Grey, 0, true);

                // 参数：界面改过的按界面值写，没改过的读回相机现值（现场配方不会被本页改坏）
                ApplyOrAdoptParams(fifo.Operator);

                // 试取一帧：触发配置在这里定。
                //  下拉=自动探测（默认）→ 保持相机原配置并依次试（原配置→FreeRun→Manual），哪种出图用哪种；
                //  下拉=强制某种   → 就按它写，仍试取一帧验证，取不到就明确报错并还原相机原配置。
                ICogImage probe = _triggerModeEdited
                    ? ProbeWithForcedTrigger(fifo, videoFormat, SelectedTriggerChoice())
                    : ProbeOneFrame(fifo, videoFormat);

                _camFifo = fifo;
                fifo = null;   // 已接管，finally 不释放
                _camName = grabber.Name + " / " + grabber.SerialNumber;
                _camVideoFormat = videoFormat;
                _lastFrame = probe;   // 顺手留着这一帧：PMA『采集训练图』立刻可用
                TrainingImageProvider = ProvideCameraFrame;
                EnsureCleanupHook();
                RefreshCamButtons();
                RefreshTriggerLabel();

                if (found > 1)
                {
                    MessageBox.Show(this, "发现 " + found + " 台相机，已连接第一台：\r\n" + _camName,
                        "相机设置", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                // 成功静默（按钮状态即反馈；多台是例外，必须让用户知道连的是哪台）
            }
            catch (Exception ex)
            {
                if (fifo != null) fifo.Dispose();
                ShowCamError("连接相机", ex);
            }
        }

        /// <summary>
        /// 挑视频格式：只用官方文档认的 AvailableVideoFormats；
        /// 优先含 "Bayer" 的那项（现场 Hikrobot 是 "Generic GigEVision (Bayer Color)"），否则第一项。
        /// </summary>
        private static string PickVideoFormat(ICogFrameGrabber grabber)
        {
            CogStringCollection formats = grabber.AvailableVideoFormats;
            if (formats == null || formats.Count == 0) return null;

            for (int i = 0; i < formats.Count; i++)
            {
                if (formats[i].IndexOf(PreferredFormatTag, StringComparison.OrdinalIgnoreCase) >= 0)
                    return formats[i];
            }
            return formats[0];
        }

        /// <summary>
        /// 连接后试取一帧，并顺带把触发配置定下来。
        /// 现场那台相机在 VisionPro 里是"手动"触发才出图，盲目改成 FreeRun 反而一帧不来，
        /// 所以按 ① 保持原样 → ② FreeRun+关触发 → ③ Manual+开触发 依次试，哪种出图就用哪种。
        /// 全都不出图 → 抛带诊断信息的异常（含 VisionPro 的 RunStatus.Message/Exception）。
        /// 每次试取限时 ProbeTimeoutMs（默认 10000ms 会让连接干等很久），试完恢复原超时。
        /// </summary>
        private ICogImage ProbeOneFrame(CogAcqFifoTool fifo, string videoFormat)
        {
            ICogAcqFifo op = fifo.Operator;
            ICogAcqTrigger trigger = op.OwnedTriggerParams;

            // 相机进来的原始触发状态：第①轮不改它，全失败时还原，别把相机留在半路状态
            bool originalEnabled = SafeBool(delegate { return trigger.TriggerEnabled; });
            CogAcqTriggerModelConstants originalModel = SafeTriggerModel(trigger);
            RememberOriginalTrigger(trigger);   // 也留给"切回自动探测"用

            string[] attempts = { "保持相机原触发配置", "自由运行(FreeRun/触发关)", "手动软件触发(Manual/触发开)" };
            StringBuilder log = new StringBuilder();
            ICogImage image = null;
            int winner = -1;

            for (int i = 0; i < attempts.Length && image == null; i++)
            {
                string reason;
                try
                {
                    ApplyTriggerCandidate(trigger, i);
                }
                catch (Exception ex)
                {
                    log.AppendLine("  " + (i + 1) + ") " + attempts[i] + "：设置触发失败（" + ex.Message + "）");
                    continue;
                }

                image = TryRunOnce(fifo, out reason);
                log.AppendLine("  " + (i + 1) + ") " + attempts[i] + "：" +
                    (image != null ? "取到图 ✓" : "没取到（" + reason + "）"));
                if (image != null) winner = i;
            }

            if (image == null)
            {
                try
                {
                    trigger.TriggerEnabled = originalEnabled;
                    trigger.TriggerModel = originalModel;
                }
                catch (Exception) { /* 还原失败不改变结论 */ }
                throw new InvalidOperationException(
                    BuildNoFrameMessage(op, videoFormat, log.ToString()));
            }

            if (winner > 0)
            {
                MessageBox.Show(this, "相机原触发配置在本程序里取不到图，已自动切到「" + attempts[winner] +
                    "」继续。\r\n\r\n其它程序的取像设置不受影响。", "相机设置",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            _triggerKeptAsIs = (winner == 0);
            return image;
        }

        /// <summary>第 i 种触发候选：0=保持原样（不写）1=FreeRun+关触发 2=Manual+开触发（软件触发）。</summary>
        private static void ApplyTriggerCandidate(ICogAcqTrigger trigger, int index)
        {
            ApplyTriggerChoice(trigger, index == 1 ? TriggerChoice.FreeRun
                                 : index == 2 ? TriggerChoice.Manual
                                 : TriggerChoice.KeepAsIs);
        }

        // ------------------------------------------------------------------
        // 触发方式下拉（0=自动探测，1=强制自动，2=强制自由运行，3=强制手动）
        // ------------------------------------------------------------------

        /// <summary>触发方式选项。KeepAsIs = 不动相机（保持现场原配方）。</summary>
        private enum TriggerChoice
        {
            KeepAsIs = 0,
            Auto = 1,
            FreeRun = 2,
            Manual = 3
        }

        private TriggerChoice SelectedTriggerChoice()
        {
            switch (cmbTrigMode.SelectedIndex)
            {
                case 1: return TriggerChoice.Auto;
                case 2: return TriggerChoice.FreeRun;
                case 3: return TriggerChoice.Manual;
                default: return TriggerChoice.KeepAsIs;
            }
        }

        /// <summary>把触发方式写进相机。KeepAsIs 什么都不写（保护相机原配置）。</summary>
        private static void ApplyTriggerChoice(ICogAcqTrigger trigger, TriggerChoice choice)
        {
            switch (choice)
            {
                case TriggerChoice.Auto:
                    trigger.TriggerModel = CogAcqTriggerModelConstants.Auto;
                    trigger.TriggerEnabled = true;
                    break;
                case TriggerChoice.FreeRun:
                    trigger.TriggerModel = CogAcqTriggerModelConstants.FreeRun;
                    trigger.TriggerEnabled = false;
                    break;
                case TriggerChoice.Manual:
                    trigger.TriggerModel = CogAcqTriggerModelConstants.Manual;
                    trigger.TriggerEnabled = true;
                    break;
                // KeepAsIs: 故意不写
            }
        }

        private static string TriggerChoiceName(TriggerChoice choice)
        {
            switch (choice)
            {
                case TriggerChoice.Auto: return "自动触发（外部硬触发）";
                case TriggerChoice.FreeRun: return "自由运行";
                case TriggerChoice.Manual: return "手动（软件触发）";
                default: return "自动探测";
            }
        }

        /// <summary>记下相机原始触发配置（只在首次记录；断开后清空，下次连接重新读）。</summary>
        private void RememberOriginalTrigger(ICogAcqTrigger trigger)
        {
            if (_hasOriginalTrigger) return;
            try
            {
                _originalTriggerEnabled = SafeBool(delegate { return trigger.TriggerEnabled; });
                _originalTriggerModel = SafeTriggerModel(trigger);
                _hasOriginalTrigger = true;
            }
            catch (Exception)
            {
                // 读不到就不记，切回自动探测时也不还原（不猜）
            }
        }

        /// <summary>还原相机原始触发配置（切回"自动探测"时用）。</summary>
        private void RestoreOriginalTrigger(ICogAcqTrigger trigger)
        {
            if (!_hasOriginalTrigger) return;
            trigger.TriggerModel = _originalTriggerModel;
            trigger.TriggerEnabled = _originalTriggerEnabled;
        }

        /// <summary>下拉变化：未连接只记界面值；已连接立即写相机（强制模式）或还原原配置（自动探测）。</summary>
        private void cmbTrigMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_uiSyncing) return;

            _triggerModeEdited = (SelectedTriggerChoice() != TriggerChoice.KeepAsIs);
            if (_camFifo == null)
            {
                RefreshTriggerLabel();   // 未连接：只记界面值，连接时按它应用
                return;
            }

            try
            {
                _camBusy = true;
                ICogAcqTrigger trigger = _camFifo.Operator.OwnedTriggerParams;
                if (_triggerModeEdited) ApplyTriggerChoice(trigger, SelectedTriggerChoice());
                else RestoreOriginalTrigger(trigger);   // 切回自动探测 → 把相机还原成原配置
            }
            catch (Exception ex)
            {
                ShowCamError("切换触发方式", ex);
            }
            finally
            {
                _camBusy = false;
                RefreshTriggerLabel();
            }
        }

        /// <summary>
        /// 按用户强制的触发方式连相机：写模式 → 试取一帧验证；取不到就还原相机原配置并抛错
        /// （不悄悄退回自动试取——用户明确指定了方式，失败要说清楚）。
        /// </summary>
        private ICogImage ProbeWithForcedTrigger(CogAcqFifoTool fifo, string videoFormat, TriggerChoice choice)
        {
            ICogAcqFifo op = fifo.Operator;
            ICogAcqTrigger trigger = op.OwnedTriggerParams;
            RememberOriginalTrigger(trigger);

            try
            {
                ApplyTriggerChoice(trigger, choice);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("按所选触发方式「" + TriggerChoiceName(choice) +
                    "」写相机失败：" + ex.Message, ex);
            }

            string reason;
            ICogImage image = TryRunOnce(fifo, out reason);
            if (image != null)
            {
                _triggerKeptAsIs = false;
                return image;
            }

            try { RestoreOriginalTrigger(trigger); }
            catch (Exception) { /* 还原失败不改变结论 */ }

            throw new InvalidOperationException(
                "已按所选触发方式「" + TriggerChoiceName(choice) + "」写相机，但取不到图（" + reason + "）。\r\n" +
                "可把触发方式切回「自动探测（保持相机原配置）」再连一次。\r\n\r\n" +
                BuildNoFrameMessage(op, videoFormat, ""));
        }

        /// <summary>
        /// 限时试取一帧：拿到图返回图；否则返回 null 并把原因写进 reason。
        /// 注意 Run() 可能"不抛异常但也不给图"——这时去读 RunStatus（Result/Message/Exception），
        /// 那才是 VisionPro 真正想说的话。
        /// </summary>
        private ICogImage TryRunOnce(CogAcqFifoTool fifo, out string reason)
        {
            ICogAcqFifo op = fifo.Operator;
            bool oldEnabled = op.TimeoutEnabled;
            double oldTimeout = op.Timeout;
            reason = null;

            try
            {
                op.Timeout = ProbeTimeoutMs;
                op.TimeoutEnabled = true;
                fifo.Run();

                ICogImage image = fifo.OutputImage;
                if (image != null) return image;

                reason = "取像返回空图";
                ICogRunStatus status = fifo.RunStatus;
                if (status != null)
                {
                    reason += "（Result=" + status.Result;
                    if (!string.IsNullOrEmpty(status.Message)) reason += "，Message=" + status.Message;
                    if (status.Exception != null) reason += "，Exception=" + status.Exception.Message;
                    reason += "）";
                }
                return null;
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return null;
            }
            finally
            {
                try { op.Timeout = oldTimeout; op.TimeoutEnabled = oldEnabled; }
                catch (Exception) { /* 恢复失败不影响结论 */ }
            }
        }

        /// <summary>
        /// 取不到图时，把现场排查要用的信息一次说全（视频格式/像素格式/包大小/触发/曝光 + 试过什么）。
        /// </summary>
        private string BuildNoFrameMessage(ICogAcqFifo op, string videoFormat, string attempts)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("已连上相机，但一帧图都取不到。三种触发配置都试过了：");
            sb.Append(attempts);
            sb.AppendLine();
            sb.AppendLine("视频格式：" + Safe(delegate { return op.VideoFormat; }) + "（请求 " + videoFormat + "）");
            sb.AppendLine("实际像素格式：" + Safe(delegate { return op.AcquiredPixelFormat(); }));
            sb.AppendLine("曝光：" + Safe(delegate { return op.OwnedExposureParams.Exposure; }) + " ms");
            sb.AppendLine("包大小：" + Safe(delegate { return op.OwnedGigEVisionTransportParams.PacketSize; }) +
                " 字节（相机上限 " + Safe(delegate { return op.OwnedGigEVisionTransportParams.PacketSizeMax; }) + "）");
            sb.AppendLine("延迟级别：" + Safe(delegate { return op.OwnedGigEVisionTransportParams.LatencyLevel; }));
            sb.AppendLine("触发模式：" + Safe(delegate { return op.OwnedTriggerParams.TriggerModel; }) +
                "，触发使能：" + Safe(delegate { return op.OwnedTriggerParams.TriggerEnabled; }));
            sb.AppendLine();
            sb.Append("请把这一整框内容发回给我。");
            return sb.ToString();
        }

        /// <summary>读相机参数用于报错文案：读不到就显示 "?"，不让诊断本身再抛异常。</summary>
        private static string Safe(Func<object> getter)
        {
            try
            {
                object value = getter();
                return value == null ? "?" : value.ToString();
            }
            catch (Exception) { return "?"; }
        }

        private static bool SafeBool(Func<object> getter)
        {
            try { return Convert.ToBoolean(getter()); }
            catch (Exception) { return false; }
        }

        private static CogAcqTriggerModelConstants SafeTriggerModel(ICogAcqTrigger trigger)
        {
            try { return trigger.TriggerModel; }
            catch (Exception) { return CogAcqTriggerModelConstants.FreeRun; }
        }

        /// <summary>『断开相机』：停预览 → 关窗 → 撤钩子 → 释放 FIFO。</summary>
        private void btnCamDis_Click(object sender, EventArgs e)
        {
            if (_camFifo == null)
            {
                MessageBox.Show(this, "当前未连接相机。", "相机设置",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                ClosePreview();
                TrainingImageProvider = null;
                _camFifo.Dispose();
            }
            catch (Exception ex)
            {
                ShowCamError("断开相机", ex);
            }
            finally
            {
                _camFifo = null;
                _camName = "";
                _camVideoFormat = "";
                _lastFrame = null;
                _hasOriginalTrigger = false;   // 下次连接重新记录相机原始触发配置
                RefreshCamButtons();   // 成功静默
                RefreshTriggerLabel();
            }
        }

        /// <summary>连接/断开互斥置灰（连接状态的唯一指示）；拍照/预览按钮永远可点，不管。</summary>
        private void RefreshCamButtons()
        {
            btnCamConn.Enabled = (_camFifo == null);
            btnCamDis.Enabled = (_camFifo != null);
        }

        /// <summary>
        /// 把当前触发状态写到 lblTrigMode。原来那句「触发模式：Semi 半自动（停带拍照）」是 Designer
        /// 写死的静态文字，跟实际状态毫无关系（等于骗人）；现在显示读回来的真实值 + 是否被本页改过。
        /// </summary>
        private void RefreshTriggerLabel()
        {
            if (_camFifo == null)
            {
                lblTrigMode.Text = "实际：未连接";
                return;
            }

            ICogAcqTrigger trigger = _camFifo.Operator.OwnedTriggerParams;
            CogAcqTriggerModelConstants model = SafeTriggerModel(trigger);
            bool enabled = SafeBool(delegate { return trigger.TriggerEnabled; });

            lblTrigMode.Text = "实际：" + TriggerModelName(model) +
                (enabled ? " · 触发开" : " · 触发关") +
                (_triggerKeptAsIs ? " · 原配置" : " · 已切换");
        }

        /// <summary>触发模式枚举 → 人话（枚举值实测：Manual=0 / Auto=1 / Semi=2 / Slave=3 / FreeRun=4）。</summary>
        private static string TriggerModelName(CogAcqTriggerModelConstants model)
        {
            switch (model)
            {
                case CogAcqTriggerModelConstants.FreeRun: return "自由运行";
                case CogAcqTriggerModelConstants.Manual: return "手动（软件触发）";
                case CogAcqTriggerModelConstants.Auto: return "自动（外部硬触发）";
                case CogAcqTriggerModelConstants.Semi: return "半自动（停带触发）";
                case CogAcqTriggerModelConstants.Slave: return "从站（跟随主站）";
                default: return model.ToString();
            }
        }

        /// <summary>
        /// 实时预览的 own 参数：官方文档规定 TriggerModel 为 Auto 时 own 必须为 true，
        /// 否则实时流起不来；其余模式传 false（FIFO 归本页管，预览中直接读显示控件的帧）。
        /// own=true 期间不允许再用 FIFO 取图 —— btnSnap_Click 已按这条改过，不再调 Run()。
        /// </summary>
        private bool LiveDisplayOwnsFifo
        {
            get
            {
                if (_camFifo == null) return false;
                return SafeTriggerModel(_camFifo.Operator.OwnedTriggerParams) ==
                    CogAcqTriggerModelConstants.Auto;
            }
        }

        /// <summary>懒挂 Disposed 钩子：界面销毁时关窗并释放 FIFO，不碰构造函数/VisionConfigView.cs。</summary>
        private void EnsureCleanupHook()
        {
            if (_cleanupHooked) return;
            _cleanupHooked = true;
            this.Disposed += delegate
            {
                try
                {
                    ClosePreview();
                    if (_camFifo != null) _camFifo.Dispose();
                }
                catch (Exception) { /* 析构路径不弹框 */ }
                finally { _camFifo = null; }
            };
        }

        // ------------------------------------------------------------------
        // 拍照 / 预览
        // ------------------------------------------------------------------

        /// <summary>『单次拍照』：软件触发取一帧 → 小窗显示。预览中点它只刷新共享帧，不碰 live 画面。</summary>
        private void btnSnap_Click(object sender, EventArgs e)
        {
            if (_camFifo == null)
            {
                MessageBox.Show(this, "相机未连接，请先点『连接相机』。", "相机设置",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_camBusy) return;

            // Auto（外部硬触发）模式下软件 Run() 取不到图（还可能干等超时）：先提示，别让用户白等
            if (!_previewLive && SafeTriggerModel(_camFifo.Operator.OwnedTriggerParams) ==
                CogAcqTriggerModelConstants.Auto)
            {
                MessageBox.Show(this,
                    "相机当前是「自动（外部硬触发）」模式：本页『单次拍照』走的是软件触发，取不到图。\r\n\r\n" +
                    "要么用硬件触发（光电传感器），要么把上方触发方式切到「强制：手动（软件触发）」或「强制：自由运行」。",
                    "相机设置", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                _camBusy = true;
                Cursor.Current = Cursors.WaitCursor;

                if (_previewLive)
                {
                    // 预览中：只读显示控件的当前帧，**绝不调 Run()**——
                    // own=true 时（TriggerModel=Auto）文档明确禁止边预览边用 FIFO 取图；
                    // 画面还没出来就什么都不做，不要退化成 Run()。
                    _lastFrame = _previewDisplay != null ? _previewDisplay.Image : null;
                    if (_lastFrame == null)
                        MessageBox.Show(this, "预览画面还没出来，等画面出来再点『单次拍照』。",
                            "相机设置", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    _camFifo.Run();
                    _lastFrame = _camFifo.OutputImage;
                    ShowPreview(_lastFrame, false);
                }
            }
            catch (Exception ex)
            {
                ShowCamError("单次拍照", ex);
            }
            finally
            {
                _camBusy = false;
                Cursor.Current = Cursors.Default;
            }
        }

        /// <summary>『实时预览』：开小窗 → StartLiveDisplay 全速显示。</summary>
        private void btnLive_Click(object sender, EventArgs e)
        {
            if (_camFifo == null)
            {
                MessageBox.Show(this, "相机未连接，请先点『连接相机』。", "相机设置",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_previewLive)
            {
                if (_previewBox != null) _previewBox.BringToFront();   // 不叠窗
                return;
            }

            try
            {
                ShowPreview(null, true);
                _previewDisplay.StartLiveDisplay(_camFifo.Operator, LiveDisplayOwnsFifo);
                _previewLive = true;
            }
            catch (Exception ex)
            {
                _previewLive = false;
                ClosePreview();   // 失败不留空窗
                ShowCamError("实时预览", ex);
            }
        }

        /// <summary>『停止预览』：停流 + 关窗。</summary>
        private void btnStopLive_Click(object sender, EventArgs e)
        {
            if (!_previewLive)
            {
                MessageBox.Show(this, "当前没有预览。", "相机设置",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ClosePreview();   // 成功静默
        }

        /// <summary>
        /// 预览小窗单例。live 模式只建窗不塞图（流由 StartLiveDisplay 推）；
        /// 拍照模式把静帧塞进去。标题带相机名，现场确认连的是哪台。
        /// </summary>
        private void ShowPreview(ICogImage frame, bool forLive)
        {
            if (_previewBox == null || _previewBox.IsDisposed)
            {
                _previewBox = new Form();
                // 标题带相机名 + 实际生效的视频格式：现场一眼确认跑在什么格式上
                _previewBox.Text = "相机预览" + (_camName.Length > 0 ? " - " + _camName : "") +
                    (_camVideoFormat.Length > 0 ? " · " + FormatTagOf(_camVideoFormat) : "");
                _previewBox.Width = 800;
                _previewBox.Height = 600;
                _previewBox.StartPosition = FormStartPosition.CenterParent;
                _previewDisplay = new CogDisplay();
                _previewDisplay.Dock = DockStyle.Fill;
                _previewBox.Controls.Add(_previewDisplay);
                _previewBox.FormClosed += delegate
                {
                    try { if (_previewLive && _previewDisplay != null) _previewDisplay.StopLiveDisplay(); }
                    catch (Exception) { /* 关窗路径不弹框 */ }
                    finally { _previewLive = false; _previewDisplay = null; _previewBox = null; }
                };
                _previewBox.Show(this);
            }
            else
            {
                _previewBox.BringToFront();
            }

            if (!forLive && frame != null)
            {
                _previewDisplay.Image = frame;
                _previewDisplay.Fit(true);
            }
        }

        /// <summary>关小窗（预览中则先停流）。关窗/按钮/断开/销毁四处复用。</summary>
        private void ClosePreview()
        {
            try
            {
                if (_previewLive && _previewDisplay != null) _previewDisplay.StopLiveDisplay();
            }
            catch (Exception) { /* 停流失败不阻塞关窗 */ }
            finally
            {
                _previewLive = false;
                if (_previewBox != null && !_previewBox.IsDisposed) _previewBox.Close();
                _previewDisplay = null;
                _previewBox = null;
            }
        }

        /// <summary>
        /// PMA 训练页借帧规则：预览中拿显示控件当前帧，否则拿最近单次拍照帧；
        /// 都没有返回 null（PMA 现有逻辑会提示“还没有训练图”，不用改）。
        /// </summary>
        private ICogImage ProvideCameraFrame()
        {
            if (_previewLive && _previewDisplay != null && _previewDisplay.Image != null)
                return _previewDisplay.Image;
            return _lastFrame;
        }

        // ------------------------------------------------------------------
        // 参数：读回优先 + 单项写入
        // ------------------------------------------------------------------
        // 策略（v1.4 现场实测后定）：
        //   • 连上后先**读回**相机现值填界面，不主动写 → 现场配方（曝光 5 / 包 1508 / 延迟 3）不会被改坏；
        //   • 界面上显式改过的项，连接时按界面值写入（用户意图优先）；
        //   • 已连接时改一项只写这一项（旧写法会把四项一起重写，改延迟会顺带把曝光改掉）；
        //   • 写失败/被驱动夹断 → 提示 + 界面回退到相机真值，界面永远显示"实际生效值"。

        /// <summary>
        /// 曝光失焦：没改不打扰；数字且 &gt;0 生效（上限不卡，驱动夹断后读回显示）；非法恢复上次有效值。
        /// 未连接只记界面值并标记"用户改过"（连接时按它写入）。
        /// </summary>
        private void txtExposure_Leave(object sender, EventArgs e)
        {
            double value;
            if (!double.TryParse(txtExposure.Text.Trim(), out value))
                value = double.NaN;

            if (!double.IsNaN(value) && Math.Abs(value - _lastValidExposure) < 1e-9) return;  // 没改

            if (double.IsNaN(value) || value <= 0)
            {
                MessageBox.Show(this, "曝光请输入大于 0 的数字（毫秒）。", "相机设置",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtExposure.Text = FormatDouble(_lastValidExposure);
                return;
            }

            double old = _lastValidExposure;
            _lastValidExposure = value;
            _exposureEdited = true;   // 用户显式改过 → 连接时按界面值写入
            if (_camFifo == null) return;   // 未连接：只记界面值

            try
            {
                WriteExposure(_camFifo.Operator, value);
                _lastValidExposure = _camFifo.Operator.OwnedExposureParams.Exposure;   // 读回（驱动可能夹断）
                txtExposure.Text = FormatDouble(_lastValidExposure);
            }
            catch (Exception ex)
            {
                _lastValidExposure = old;
                txtExposure.Text = FormatDouble(old);
                ShowCamError("应用曝光", ex);
            }
        }

        /// <summary>增益：已连接才写（GenICam 节点名因固件而异，失败只警告不断连）；连接时不主动写。</summary>
        private void cmbGain_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_uiSyncing) return;
            if (_camFifo == null) return;   // 未连接：只记界面值

            try
            {
                ApplyGainNode(_camFifo.Operator, SelectedGainDb());
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "增益未能确认生效（" + ex.Message +
                    "），请在预览中目视确认亮度变化。", "相机设置",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>包大小：改了就热应用；超相机上限夹到上限并提示（不再让连接失败）。</summary>
        private void cmbPacket_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_uiSyncing) return;
            _packetEdited = true;
            if (_camFifo == null) return;   // 未连接：只记界面值

            int want = SelectedPacketSize();
            if (want == _camPacketSize) return;   // 与相机现值相同，不用写

            try
            {
                WritePacketSize(_camFifo.Operator, want);
                SyncPacketUi(_camPacketSize);
                if (_camPacketSize != want)
                    MessageBox.Show(this, "包大小 " + want + " 超过相机上限，已按 " + _camPacketSize +
                        " 字节应用。", "相机设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                SyncPacketUi(_camPacketSize);
                MessageBox.Show(this, "包大小应用失败：" + ex.Message + "\r\n已恢复到相机当前值 " +
                    _camPacketSize + "。", "相机设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>延迟级别：界面 0-3 与相机值 1:1，改了就热应用，失败回退到相机真值。</summary>
        private void cmbLatency_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_uiSyncing) return;
            _latencyEdited = true;
            if (_camFifo == null) return;   // 未连接：只记界面值

            int want = cmbLatency.SelectedIndex;
            if (want < 0 || want == _camLatencyLevel) return;

            try
            {
                WriteLatencyLevel(_camFifo.Operator, want);
                SyncLatencyUi(_camLatencyLevel);
            }
            catch (Exception ex)
            {
                SyncLatencyUi(_camLatencyLevel);
                MessageBox.Show(this, "延迟级别应用失败：" + ex.Message + "\r\n已恢复到相机当前值 " +
                    _camLatencyLevel + "。", "相机设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>连接时：界面改过的写入，没改过的读回；增益不读不写（读不到相机现值）。</summary>
        private void ApplyOrAdoptParams(ICogAcqFifo op)
        {
            if (_exposureEdited)
            {
                try { WriteExposure(op, _lastValidExposure); }
                catch (Exception ex) { WarnParamAtConnect("曝光", ex); }
            }
            AdoptExposure(op);

            if (_packetEdited)
            {
                try { WritePacketSize(op, SelectedPacketSize()); }
                catch (Exception ex) { WarnParamAtConnect("包大小", ex); }
            }
            AdoptPacketSize(op);

            if (_latencyEdited)
            {
                try { WriteLatencyLevel(op, cmbLatency.SelectedIndex); }
                catch (Exception ex) { WarnParamAtConnect("延迟级别", ex); }
            }
            AdoptLatencyLevel(op);
        }

        private void AdoptExposure(ICogAcqFifo op)
        {
            try
            {
                double ms = op.OwnedExposureParams.Exposure;
                if (ms > 0)
                {
                    _lastValidExposure = ms;
                    txtExposure.Text = FormatDouble(ms);
                }
            }
            catch (Exception) { /* 读不到就保持界面值 */ }
        }

        private void AdoptPacketSize(ICogAcqFifo op)
        {
            try
            {
                int size = op.OwnedGigEVisionTransportParams.PacketSize;
                if (size > 0) { _camPacketSize = size; SyncPacketUi(size); }
            }
            catch (Exception) { /* 读不到就保持界面值 */ }
        }

        private void AdoptLatencyLevel(ICogAcqFifo op)
        {
            try
            {
                int level = op.OwnedGigEVisionTransportParams.LatencyLevel;
                _camLatencyLevel = level;
                SyncLatencyUi(level);
            }
            catch (Exception) { /* 读不到就保持界面值 */ }
        }

        /// <summary>某一项没写进去：告诉用户，但不让整次连接失败（连接本身是好的）。</summary>
        private void WarnParamAtConnect(string what, Exception ex)
        {
            MessageBox.Show(this, what + "未能应用（" + ex.Message + "），已按相机当前值显示。",
                "相机设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // ---- 单项写入（失败抛异常，调用方决定怎么提示）----

        /// <summary>曝光（毫秒，无需换算）。</summary>
        private static void WriteExposure(ICogAcqFifo op, double ms)
        {
            op.OwnedExposureParams.Exposure = ms;
        }

        /// <summary>包大小：超相机上限就夹到上限，写完读回真值。</summary>
        private void WritePacketSize(ICogAcqFifo op, int want)
        {
            ICogAcqGigEVisionTransport transport = op.OwnedGigEVisionTransportParams;
            int max = transport.PacketSizeMax;
            transport.PacketSize = (max > 0 && want > max) ? max : want;
            _camPacketSize = transport.PacketSize;   // 读回
        }

        /// <summary>延迟级别（界面 0-3 与相机值 1:1），写完读回真值。</summary>
        private void WriteLatencyLevel(ICogAcqFifo op, int level)
        {
            ICogAcqGigEVisionTransport transport = op.OwnedGigEVisionTransportParams;
            transport.LatencyLevel = level;
            _camLatencyLevel = transport.LatencyLevel;   // 读回
        }

        // ---- 界面同步（程序改界面时抑制事件，避免"读回填界面"又被当成用户改动写回相机）----

        /// <summary>把相机包大小写进下拉；不在候选里就补一项，保证界面显示的是相机真值。</summary>
        private void SyncPacketUi(int bytes)
        {
            string text = bytes.ToString(CultureInfo.InvariantCulture);
            _uiSyncing = true;
            try
            {
                int index = cmbPacket.Items.IndexOf(text);
                if (index < 0)
                {
                    cmbPacket.Items.Add(text);
                    index = cmbPacket.Items.Count - 1;
                }
                cmbPacket.SelectedIndex = index;
            }
            finally { _uiSyncing = false; }
        }

        /// <summary>把相机延迟级别写进下拉（界面 0-3 与相机值 1:1）。</summary>
        private void SyncLatencyUi(int level)
        {
            if (level < 0 || level >= cmbLatency.Items.Count) return;
            _uiSyncing = true;
            try { cmbLatency.SelectedIndex = level; }
            finally { _uiSyncing = false; }
        }

        /// <summary>增益下拉文本 → dB；认不出回 0。</summary>
        private double SelectedGainDb()
        {
            double db;
            if (double.TryParse(cmbGain.Text.Trim(), NumberStyles.Float,
                CultureInfo.InvariantCulture, out db))
                return db;
            return 0;
        }

        /// <summary>包大小下拉文本 → int；认不出回默认值。</summary>
        private int SelectedPacketSize()
        {
            int size;
            if (int.TryParse(cmbPacket.Text.Trim(), out size) && size > 0)
                return size;
            return DefaultPacketSize;
        }

        /// <summary>小数的界面格式：短、不带科学计数、不受当前区域影响。</summary>
        private static string FormatDouble(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        /// <summary>视频格式取括号里的短标签（"Generic GigEVision (Bayer Color)" → "Bayer Color"）。</summary>
        private static string FormatTagOf(string videoFormat)
        {
            int open = videoFormat.IndexOf('(');
            int close = videoFormat.LastIndexOf(')');
            if (open >= 0 && close > open + 1)
                return videoFormat.Substring(open + 1, close - open - 1);
            return videoFormat;
        }

        /// <summary>写 GenICam "Gain" 节点：有则更新，无则添加（float, dB）。</summary>
        private static void ApplyGainNode(ICogAcqFifo op, double db)
        {
            ICogAcqCustomProperties custom = op.OwnedCustomPropertiesParams;
            if (custom == null)
                throw new InvalidOperationException("该相机不支持自定义属性。");

            string text = db.ToString(CultureInfo.InvariantCulture);
            foreach (CogAcqCustomProperty prop in custom.CustomProps)
            {
                if (string.Equals(prop.Name, GainNodeName, StringComparison.OrdinalIgnoreCase))
                {
                    prop.Value = text;
                    prop.Type = CogCustomPropertyTypeConstants.TypeFloat;
                    return;
                }
            }
            custom.CustomProps.Add(new CogAcqCustomProperty(GainNodeName, text,
                CogCustomPropertyTypeConstants.TypeFloat));
        }

        private void ShowCamError(string action, Exception ex)
        {
            MessageBox.Show(this, action + "失败：\r\n" + ex.Message, "相机设置",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
