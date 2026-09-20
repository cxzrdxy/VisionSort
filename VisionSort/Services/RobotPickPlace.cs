using System;
using System.Threading;
using System.Threading.Tasks;
using DobotArm.Client;

namespace VisionSort.Services
{
    /// <summary>
    /// NG 件分拣动作（「主运行页方案.md」Q5/Q6=A ＋「九点标定联机分拣方案.md」§4.8）。
    ///
    /// 顺序：**换算出来的取料点** → 吸 → 等 PickDelayMs → 放料点 → 放 → 等 ResetDelayMs → 回待机位。
    /// 每一步都用 <c>WaitArriveAsync</c> 等到位（容差/轮询/超时在 <see cref="RobotArmSettings"/> 里）。
    ///
    /// ⚠ 与 v1.0 的区别（这一版的重点）：
    ///   · 取料点**不再写死**，而是由视觉给出的像素坐标经九点标定换算而来 —— 工件停在哪儿都能抓。
    ///   · 换算失败 / 没标定 / 越过工作范围 → **一律拒动**（返回中文原因，让调用方写进库的备注）。
    ///     标定错一行或配成镜像时，机械臂会往墙上走；宁可不动，等人处理。
    ///   · 放料点与 Z 仍然是示教固定值；`rHead` 恒 0（Q6=甲）。
    ///   · 放完回的是**示教基准点** `PickX/PickY/PickZ`（原来叫"取料点"，现在它的语义是
    ///     "待机位"：一个不进相机视野、不会压到带面的安全落点）。
    ///
    /// 为什么不在这里自己读 vpp / 跑 PMA：视觉那半边的职责在 <see cref="VisionScheme"/>，
    /// 这里只收"一个像素坐标 + 一个能不能用的标记"。
    /// </summary>
    public sealed class RobotPickPlace
    {
        /// <summary>唯一实例。</summary>
        public static RobotPickPlace Shared { get; } = new RobotPickPlace();

        private RobotPickPlace()
        {
        }

        /// <summary>
        /// 执行一次分拣。返回 null＝成功，否则是中文原因（调用方把它写进那条记录的备注，**不丢事件**）。
        /// 取消（急停/停止）会抛 <see cref="OperationCanceledException"/>，由主循环接住。
        /// </summary>
        /// <param name="hasPixel">视觉有没有给出像素坐标（PMA 一个结果都没有时为 false）。</param>
        /// <param name="pixelX">工件（准确说是模板原点）在图像里的 X。</param>
        /// <param name="pixelY">工件（准确说是模板原点）在图像里的 Y。</param>
        public async Task<string> ExecuteAsync(bool hasPixel, double pixelX, double pixelY, CancellationToken token)
        {
            RobotArmSettings s = RobotArmSettings.Shared;
            DobotArmClient arm = RobotArmService.Shared.Client;

            try
            {
                token.ThrowIfCancellationRequested();

                // ---------- ① 像素 → mm ----------
                if (!hasPixel)
                {
                    return "视觉没有给出工件位置（PMA 一个结果都没有），不动机械臂。"
                        + "（检查：模板训练、光照、工件是否在视野内。）";
                }

                string loadMessage;
                CalibrationService.Shared.EnsureLoaded(out loadMessage);

                double pickX, pickY;
                string convertMessage;
                if (!CalibrationService.Shared.PixelToRobot(pixelX, pixelY, out pickX, out pickY, out convertMessage))
                {
                    // Q5=甲：没标定就拒绝分拣（不悄悄退回示教点 —— 那会让人误以为"系统正常"）
                    return convertMessage;
                }

                // ---------- ② 工作范围校验（保命） ----------
                string areaMessage;
                if (!WithinPickArea(pickX, pickY, out areaMessage)) return areaMessage;

                // ---------- ③ 取放料 ----------
                await arm.MoveJAsync(pickX, pickY, s.PickZ, 0);
                await arm.WaitArriveAsync(pickX, pickY, s.PickZ);

                token.ThrowIfCancellationRequested();
                await arm.SuctionAsync(true);
                if (s.PickDelayMs > 0) await Task.Delay(s.PickDelayMs, token);

                await arm.MoveJAsync(s.PlaceX, s.PlaceY, s.PlaceZ, 0);
                await arm.WaitArriveAsync(s.PlaceX, s.PlaceY, s.PlaceZ);

                token.ThrowIfCancellationRequested();
                await arm.SuctionAsync(false);
                if (s.ResetDelayMs > 0) await Task.Delay(s.ResetDelayMs, token);

                // 回待机位：放料区一般不在相机视野里，但待机位要能保证
                // 下一件工件进视野时机械臂不挡着它。
                // ⚠ 是 **StandbyX/StandbyY**，不是取料点 —— 取料点是上面换算出来的 (pickX, pickY)。
                await arm.MoveJAsync(s.StandbyX, s.StandbyY, s.PickZ, 0);
                await arm.WaitArriveAsync(s.StandbyX, s.StandbyY, s.PickZ);
                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// 换算出来的取料点必须在工作范围里。
        /// 为什么默认范围给得那么窄（示教点 ±80mm）：九点标定错一行、或者配成镜像时，
        /// 机械臂会往墙上走。**宁可先拒动让人来调范围，也不要先撞一次。**
        /// 现场按工件实际可能出现的位置，到设备调试页的「抓取几何」里放大。
        /// </summary>
        private static bool WithinPickArea(double x, double y, out string message)
        {
            RobotArmSettings s = RobotArmSettings.Shared;

            if (x < s.PickAreaMinX || x > s.PickAreaMaxX || y < s.PickAreaMinY || y > s.PickAreaMaxY)
            {
                message = "标定换算出来的取料点超出工作范围，已拒绝动作（机械臂没动）。\n"
                    + "  算出来的点：X=" + x.ToString("F1") + "，Y=" + y.ToString("F1") + " mm\n"
                    + "  允许范围：X " + s.PickAreaMinX.ToString("F1") + "~" + s.PickAreaMaxX.ToString("F1")
                    + "，Y " + s.PickAreaMinY.ToString("F1") + "~" + s.PickAreaMaxY.ToString("F1") + " mm\n"
                    + "  先查：① 模板原点设了没（设备调试页『核对 PMA 原点』）；"
                    + "② 标定对不对（重新载入后看手性是不是 ✓）；③ 范围要不要放大（标定区的『抓取几何』）。";
                return false;
            }

            message = string.Empty;
            return true;
        }
    }
}
