using Cognex.VisionPro;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;

namespace VisionSort.Services
{
    /// <summary>
    /// 检测图存盘（主运行页每轮调用）。
    ///
    /// 为什么在 Services 而不是 View：当初「图像保存配置」那期把它写在 View 里、后来因为没有调用方删掉了
    /// （见 图像保存配置方案.md §3.2）。这次主运行页要用，就放在服务层——纯函数式（给图给参数 → 写文件 → 返回路径），
    /// 不依赖任何控件，探针可以直接调。
    ///
    /// 两个踩过的坑照旧：
    ///   ① <c>CogImage.ToBitmap()</c> 返回的是 8bpp 索引色，**必须先转 24bppRgb** 再存，否则 PNG/JPG 存出来是花的；
    ///   ② 文件名精确到毫秒，避免同一秒内两件互相覆盖。
    /// </summary>
    public static class ImageSaver
    {
        /// <summary>
        /// 存一张图。<paramref name="rootPath"/> 为根目录，内部按 yyyy-MM-dd 建子目录。
        /// 返回**相对根目录**的路径（数据库里存的就是这个，换盘符/换机可迁移）；失败时抛异常，由调用方决定怎么记。
        /// </summary>
        public static string Save(ICogImage image, string rootPath, string format,
            string workpieceNo, DateTime time, bool isOk)
        {
            if (image == null) throw new ArgumentNullException("image");
            if (string.IsNullOrEmpty(rootPath)) throw new ArgumentException("图像根目录为空", "rootPath");

            string extension = ExtensionOf(format);
            string dayFolder = time.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string folder = Path.Combine(rootPath, dayFolder);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string safeNo = Sanitize(workpieceNo);
            string name = safeNo + "_" + time.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture)
                          + (isOk ? "_OK" : "_NG") + extension;
            string full = Path.Combine(folder, name);

            using (Bitmap bitmap = To24bpp(image.ToBitmap()))
            {
                switch (extension)
                {
                    case ".jpg":
                        SaveJpeg(bitmap, full);
                        break;
                    case ".png":
                        bitmap.Save(full, ImageFormat.Png);
                        break;
                    default:
                        bitmap.Save(full, ImageFormat.Bmp);
                        break;
                }
            }

            // 库里存相对路径，分隔符统一成反斜杠（Windows 现场，人看着也顺眼）
            return dayFolder + "\\" + name;
        }

        /// <summary>格式名 → 扩展名（含点）。</summary>
        public static string ExtensionOf(string format)
        {
            string value = (format ?? string.Empty).Trim().ToUpperInvariant();
            if (value == "JPG" || value == "JPEG") return ".jpg";
            if (value == "PNG") return ".png";
            return ".bmp";
        }

        /// <summary>8bpp 索引色 → 24bppRgb（已在用的图直接返回新实例）。</summary>
        private static Bitmap To24bpp(Bitmap source)
        {
            if (source.PixelFormat == PixelFormat.Format24bppRgb) return source;

            Bitmap converted = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
            using (Graphics graphics = Graphics.FromImage(converted))
            {
                graphics.DrawImage(source, 0, 0, source.Width, source.Height);
            }
            source.Dispose();
            return converted;
        }

        private static void SaveJpeg(Bitmap bitmap, string path)
        {
            // 默认 JPEG 质量只有 75 左右，检测图要看得清缺陷，拉到 90
            ImageCodecInfo codec = null;
            foreach (ImageCodecInfo info in ImageCodecInfo.GetImageEncoders())
            {
                if (info.FormatID == ImageFormat.Jpeg.Guid) { codec = info; break; }
            }
            if (codec == null)
            {
                bitmap.Save(path, ImageFormat.Jpeg);
                return;
            }

            using (EncoderParameters parameters = new EncoderParameters(1))
            {
                parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L);
                bitmap.Save(path, codec, parameters);
            }
        }

        /// <summary>工件编号进文件名前先洗一遍（去掉路径分隔符与非法字符）。</summary>
        private static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "未编号";

            char[] invalid = Path.GetInvalidFileNameChars();
            System.Text.StringBuilder sb = new System.Text.StringBuilder(text.Length);
            foreach (char c in text)
            {
                bool bad = false;
                foreach (char x in invalid) { if (c == x) { bad = true; break; } }
                sb.Append(bad ? '_' : c);
            }
            string result = sb.ToString().Trim();
            return result.Length == 0 ? "未编号" : result;
        }
    }
}
