using Cognex.VisionPro;
using System;

namespace VisionSort.Services
{
    /// <summary>
    /// 相机共享服务（「主运行页方案.md」Q1=A）。
    ///
    /// **为什么需要它**：一台 GigE 相机同时只能被一个 FIFO 打开，而 FIFO 原先挂在
    /// `VisionConfigView` 的私有字段上，主运行页根本拿不到——两页各开一个会互相抢相机，
    /// 现场表现为"连不上/取不到图"，极难排查。现在设备对象归本类，两个页面共用一个 FIFO。
    ///
    /// **与相机页的分工（有意为之，见方案 §十七 偏差记录）**：
    ///   · 设备对象、取一帧、触发读写：在本类（一份实现，两个页面都用）；
    ///   · "枚举相机 → 挑视频格式 → 建 FIFO → 试取一帧定触发"这套**现场验证过的连接例程**仍留在相机页，
    ///     以 <see cref="ConnectHook"/> 的形式注册进来。理由是那 1000 行是现场返修过 6 轮的代码，
    ///     整体搬家只会带来回归风险，而收益是零。
    /// </summary>
    public sealed class CameraService
    {
        /// <summary>唯一实例。</summary>
        public static CameraService Shared { get; } = new CameraService();

        private readonly object _sync = new object();

        /// <summary>
        /// 共享的取像 FIFO。相机页的 `_camFifo` 属性直接代理到这里（它仍然可以像以前一样读写这个属性）。
        /// </summary>
        public CogAcqFifoTool Fifo { get; set; }

        /// <summary>最近一次取到的帧（主运行页用它做预览兜底）。</summary>
        public ICogImage LastFrame { get; set; }

        /// <summary>相机页注册的连接例程：返回 true 表示连上了。主运行页不自己连相机，只调用它。</summary>
        public Func<bool> ConnectHook { get; set; }

        /// <summary>是否已连接（FIFO 已建）。</summary>
        public bool IsConnected
        {
            get { return Fifo != null; }
        }

        /// <summary>
        /// 确保相机可用：没连就请相机页那套流程去连（会弹它自己的提示框，那是给操作工看的）。
        /// 主运行页在「开始检测」时调一次。
        /// </summary>
        public bool EnsureConnected(out string message)
        {
            if (IsConnected)
            {
                message = "相机已连接。";
                return true;
            }

            Func<bool> hook = ConnectHook;
            if (hook == null)
            {
                message = "相机连接例程还没注册（请先打开一次视觉配置页）。";
                return false;
            }

            try
            {
                bool ok = hook();
                message = ok ? "相机已连接。" : "相机未连接（详见刚才的提示）。";
                return ok;
            }
            catch (Exception ex)
            {
                message = "连接相机失败：" + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 取一帧（生产与相机页共用同一份实现）。
        ///
        /// 三个要点是从相机那轮的 `TryRunOnce` 直接搬过来的（都踩过）：
        ///   ① 临时把 <c>op.Timeout/TimeoutEnabled</c> 设成调用方给的值，取完恢复；
        ///   ② `fifo.Run()` 可能**不抛异常也不给图**，这时必须去读 `fifo.RunStatus`——那才是 VisionPro 想说的话；
        ///   ③ 失败时不抛异常，用 <paramref name="reason"/> 回传中文原因（生产循环不允许被取像异常打断）。
        /// </summary>
        public ICogImage GrabOne(int timeoutMs, out string reason)
        {
            reason = null;
            CogAcqFifoTool fifo = Fifo;
            if (fifo == null)
            {
                reason = "相机未连接";
                return null;
            }

            lock (_sync)
            {
                ICogAcqFifo op = null;
                bool oldEnabled = false;
                double oldTimeout = 0;
                bool timeoutChanged = false;

                try
                {
                    op = fifo.Operator;
                    if (timeoutMs > 0 && op != null)
                    {
                        oldEnabled = op.TimeoutEnabled;
                        oldTimeout = op.Timeout;
                        op.Timeout = timeoutMs;
                        op.TimeoutEnabled = true;
                        timeoutChanged = true;
                    }

                    fifo.Run();
                    ICogImage image = fifo.OutputImage;
                    if (image != null)
                    {
                        LastFrame = image;
                        return image;
                    }

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
                    if (timeoutChanged)
                    {
                        try { op.Timeout = oldTimeout; op.TimeoutEnabled = oldEnabled; }
                        catch (Exception) { /* 恢复超时失败不影响本次结果 */ }
                    }
                }
            }
        }

        /// <summary>
        /// 把相机切到生产要的触发配置：<c>TriggerModel=Auto</c>（光电硬触发）+ <c>TriggerEnabled=true</c>。
        /// 写完后**读回**并如实汇报（现场那台相机吃过"写进去没生效"的亏，这里不假装成功）。
        /// </summary>
        public bool ApplyProductionTrigger(out string message)
        {
            CogAcqFifoTool fifo = Fifo;
            if (fifo == null)
            {
                message = "相机未连接。";
                return false;
            }

            try
            {
                ICogAcqTrigger trigger = fifo.Operator.OwnedTriggerParams;
                if (trigger == null)
                {
                    message = "这台相机没有可写的触发参数，无法切到生产触发方式。";
                    return false;
                }

                trigger.TriggerEnabled = true;
                trigger.TriggerModel = CogAcqTriggerModelConstants.Auto;

                bool enabled = trigger.TriggerEnabled;
                CogAcqTriggerModelConstants model = trigger.TriggerModel;
                message = "生产触发：Model=" + model + "，TriggerEnabled=" + enabled;

                if (!enabled || model != CogAcqTriggerModelConstants.Auto)
                {
                    message += "（写入未生效，取像可能一直等不到硬触发）";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                message = "设置生产触发失败：" + ex.Message;
                return false;
            }
        }

        /// <summary>触发方式的读回文本（给界面显示"实际：…"用）。</summary>
        public string TriggerReadback()
        {
            CogAcqFifoTool fifo = Fifo;
            if (fifo == null || fifo.Operator == null) return "未连接";

            try
            {
                ICogAcqTrigger trigger = fifo.Operator.OwnedTriggerParams;
                if (trigger == null) return "无触发参数";
                return trigger.TriggerModel + (trigger.TriggerEnabled ? "（触发开）" : "（触发关）");
            }
            catch (Exception ex)
            {
                return "读取失败：" + ex.Message;
            }
        }
    }
}
