namespace VisionSort.Services
{
    /// <summary>
    /// 图像保存设置（视觉配置页「图像保存配置」组的共享副本）——主运行页存图时读它。
    ///
    /// 注意（你当初的决定）：**图像配置不落盘**，只存出来的图片。
    /// 所以这里没有"加载/保存配置文件"的逻辑：默认值 = Designer 里的默认值，
    /// 用户在视觉配置页改一次，本进程内主运行页立刻跟着变（同 ConveyorSettings 的"改动即时进 Shared"套路）。
    /// </summary>
    public sealed class SaveSettings
    {
        /// <summary>唯一实例。</summary>
        public static SaveSettings Shared { get; } = new SaveSettings();

        /// <summary>图像根目录（与 Designer 的 txtSavePath 默认值一致）。</summary>
        public string RootPath = @"D:\VisionData";

        /// <summary>存图格式：BMP / JPG / PNG（对应 Designer 的 cmbFormat）。</summary>
        public string Format = "BMP";

        /// <summary>保存 OK 图。</summary>
        public bool SaveOk = true;

        /// <summary>保存 NG 图。</summary>
        public bool SaveNg = true;

        private SaveSettings()
        {
        }

        /// <summary>按判定结果决定这一轮要不要存图。</summary>
        public bool ShouldSave(bool isOk)
        {
            return isOk ? SaveOk : SaveNg;
        }

        /// <summary>扩展名（含点），未知格式按 .bmp。</summary>
        public string Extension
        {
            get
            {
                string format = (Format ?? string.Empty).Trim().ToUpperInvariant();
                if (format == "JPG" || format == "JPEG") return ".jpg";
                if (format == "PNG") return ".png";
                return ".bmp";
            }
        }
    }
}
