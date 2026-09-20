using System;

namespace VisionSort.Services
{
    /// <summary>
    /// 一行检测结果：对应表 vision_result，也对应日志页 dgvLog 的七列。
    ///
    /// ⚠ 属性名必须与 DataLogView.Designer.cs 里的 DataPropertyName 逐字一致：
    ///     Time / WorkpieceNo / RecipeID / Result / Score / ImagePath / Note
    ///   改属性名要同步改 Designer，否则日志页会静默显示空白列（不报错）。
    /// </summary>
    public sealed class ResultRecord
    {
        /// <summary>
        /// 客户端幂等键：断网补写与重试靠它不产生重复行（表上有 UNIQUE KEY uk_uuid）。
        /// 默认自动生成，补写/重试时原值带着走。
        /// </summary>
        public string Uuid { get; set; } = Guid.NewGuid().ToString("D");

        /// <summary>检测时刻（客户端本地时间）。</summary>
        public DateTime Time { get; set; } = DateTime.Now;

        /// <summary>工件编号 / 批次（主运行页「工件编号」下拉里的文本），允许空串。</summary>
        public string WorkpieceNo { get; set; } = string.Empty;

        /// <summary>配方号。本期没有配方管理，恒 0（留列，将来加配方表不用改表）。</summary>
        public int RecipeID { get; set; }

        /// <summary>生产判定结果：PMA 分数 &gt;= <see cref="VisionRunSettings.MatchThreshold"/>。</summary>
        public bool IsOk { get; set; }

        /// <summary>日志页「检测结果」列（只读投影，OK / NG）。</summary>
        public string Result
        {
            get { return IsOk ? "OK" : "NG"; }
        }

        /// <summary>PMA 匹配分数 0~1；未检出 / 检测超时为 null（库里是 NULL）。</summary>
        public double? Score { get; set; }

        /// <summary>图像文件路径，**相对图像根目录**（换盘符/换机不影响）。</summary>
        public string ImagePath { get; set; } = string.Empty;

        /// <summary>备注：机械臂异常、检测超时、补写标记等；正常轮次空串。</summary>
        public string Note { get; set; } = string.Empty;
    }
}
