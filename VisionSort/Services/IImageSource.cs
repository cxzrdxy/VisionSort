using Cognex.VisionPro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace VisionSort.Services
{
    /// <summary>
    /// 取像来源抽象（「主运行页方案.md」Q12=A）。
    ///
    /// 存在的理由很实际：本机（开发机）**没有相机**，没有这层抽象，主运行页的主流程一行都跑不起来、
    /// 只能等到了现场才发现问题。有了它，自测可以用真 VPP + 真图把整条流程跑通（含"取不到图"这种异常用例），
    /// 现场调试也能用"图片回放"复现某一批工件。
    ///
    /// 生产用 <see cref="CameraImageSource"/>；自测/回放用 <see cref="FileImageSource"/>。
    /// </summary>
    public interface IImageSource
    {
        /// <summary>显示名（写进界面提示）。</summary>
        string Name { get; }

        /// <summary>准备来源（连相机 / 列图片）。失败时给中文原因。</summary>
        bool Prepare(out string message);

        /// <summary>
        /// 取一帧。返回 null 表示这次没取到（原因在 <paramref name="reason"/>）；
        /// **不抛异常**——生产循环靠返回值与原因来决定"继续等"还是"判故障"。
        /// </summary>
        ICogImage Grab(int timeoutMs, out string reason);

        /// <summary>收尾（不关相机——相机是 CameraService 的，谁连谁负责）。</summary>
        void Release();
    }

    /// <summary>生产用：走共享的 <see cref="CameraService"/>。</summary>
    public sealed class CameraImageSource : IImageSource
    {
        public string Name
        {
            get { return "相机"; }
        }

        public bool Prepare(out string message)
        {
            return CameraService.Shared.EnsureConnected(out message);
        }

        public ICogImage Grab(int timeoutMs, out string reason)
        {
            return CameraService.Shared.GrabOne(timeoutMs, out reason);
        }

        public void Release()
        {
            // 故意不断开相机：相机由视觉配置页负责连接/断开，
            // 主运行页停了再开也不用重连（"停止检测"不该把相机也关了）。
        }
    }

    /// <summary>
    /// 自测 / 现场回放用：按顺序轮播一批本地图片。
    /// 两个可调项都是为了测异常路径：<see cref="CycleDelayMs"/> 模拟节拍、<see cref="MissingEveryNth"/> 模拟"没取到图"。
    /// </summary>
    public sealed class FileImageSource : IImageSource
    {
        private readonly List<string> _files = new List<string>();
        private int _index;

        /// <summary>每次取图前的等待（毫秒），模拟生产节拍。</summary>
        public int CycleDelayMs { get; set; }

        /// <summary>每隔 N 次返回一次"取不到图"（0 = 不模拟）。用来测"取像超时不算故障"这条路。</summary>
        public int MissingEveryNth { get; set; }

        public string Name
        {
            get { return "本地图片回放（" + _files.Count + " 张）"; }
        }

        /// <summary>用目录里的图片建来源（按文件名排序，循环播放）。</summary>
        public FileImageSource(string folderOrFile)
        {
            if (string.IsNullOrEmpty(folderOrFile)) return;

            if (File.Exists(folderOrFile))
            {
                _files.Add(folderOrFile);
                return;
            }
            if (!Directory.Exists(folderOrFile)) return;

            List<string> found = new List<string>();
            foreach (string pattern in new[] { "*.bmp", "*.jpg", "*.jpeg", "*.png", "*.tif", "*.tiff" })
            {
                found.AddRange(Directory.GetFiles(folderOrFile, pattern));
            }
            found.Sort(StringComparer.OrdinalIgnoreCase);
            _files.AddRange(found);
        }

        public bool Prepare(out string message)
        {
            if (_files.Count == 0)
            {
                message = "图片目录里没有可用图片（支持 bmp/jpg/png/tif）。";
                return false;
            }
            message = "图片回放已就绪：" + _files.Count + " 张。";
            return true;
        }

        public ICogImage Grab(int timeoutMs, out string reason)
        {
            reason = null;

            if (CycleDelayMs > 0) Thread.Sleep(CycleDelayMs);

            if (_files.Count == 0)
            {
                reason = "没有可用图片。";
                return null;
            }

            int call = _index++;
            if (MissingEveryNth > 0 && (call + 1) % MissingEveryNth == 0)
            {
                reason = "（自测）本次故意不给图。";
                return null;
            }

            string path = _files[call % _files.Count];
            try
            {
                ICogImage image = PmaTemplateTrainer.LoadImageFromFile(path);
                if (image == null) reason = "图片读不出来：" + path;
                return image;
            }
            catch (Exception ex)
            {
                reason = "读图失败：" + ex.Message;
                return null;
            }
        }

        public void Release()
        {
        }
    }
}
