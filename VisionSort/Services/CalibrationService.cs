using Cognex.VisionPro;
using Cognex.VisionPro.CalibFix;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace VisionSort.Services
{
    /// <summary>
    /// 一对标定点：像素（图像坐标）↔ 机械臂 mm。
    /// 为什么是 class 而不是 struct：界面表格要直接改字段，引用语义少一层拷贝出错。
    /// </summary>
    public sealed class CalibPair
    {
        /// <summary>像素 X（图像列，向右为正）。</summary>
        public double PixelX;

        /// <summary>像素 Y（图像行，向下为正）。</summary>
        public double PixelY;

        /// <summary>机械臂 X（mm）。</summary>
        public double RobotX;

        /// <summary>机械臂 Y（mm）。</summary>
        public double RobotY;

        public CalibPair()
        {
        }

        public CalibPair(double pixelX, double pixelY, double robotX, double robotY)
        {
            PixelX = pixelX;
            PixelY = pixelY;
            RobotX = robotX;
            RobotY = robotY;
        }

        /// <summary>四个数都是有限数（不是 NaN / 不是无穷）。</summary>
        public bool IsFinite
        {
            get
            {
                return IsNumber(PixelX) && IsNumber(PixelY) && IsNumber(RobotX) && IsNumber(RobotY);
            }
        }

        private static bool IsNumber(double v)
        {
            return !double.IsNaN(v) && !double.IsInfinity(v);
        }
    }

    /// <summary>
    /// 九点标定（「九点标定联机分拣方案.md」§4）。职责只有四个：
    ///   ① 收点对（人工从 VisionPro 抄回来的九个点）；② 算变换；③ 存/读 config\calib.json；④ 像素 → mm。
    ///
    /// ⚠ 三条实测事实决定了这个类的写法（探针证据见方案 §3.1，改这个文件前先读一遍）：
    ///   ① **API 给的方向是反的**：CogCalibNPointToNPoint 的两个 GetComputed...Transform() 返回的都是
    ///      「未校正 ← 已校正」（像素 ← mm），要「像素 → mm」**必须 Invert()**。
    ///   ② **反变换的 Scaling 就是 mm/px**（实测：正变换 Scaling=5.200000 px/mm 时，反变换 =0.192308 mm/px）。
    ///      正反用错一个，显示的"精度"就差了 27 倍。
    ///   ③ **反变换的 MatrixDeterminant > 0 ＝ 两个坐标系同手性**（实测 0.036982）。
    ///      为负说明 X 或 Y 有一个轴配反了 —— 这种标定会让机械臂往反方向走，必须**拒绝保存**。
    ///      （人眼从九个数字里看不出镜像，所以这个检查只能由程序做。）
    ///
    /// 为什么存"点对"而不是存矩阵：矩阵是派生数据。存点对则每次启动重算，参数一改就自动一致，
    /// 而且人打开 calib.json 能一个数一个数地核对（现场排查就靠这个）。
    /// </summary>
    public sealed class CalibrationService
    {
        private const string ConfigFolder = "config";
        private const string ConfigFileName = "calib.json";

        /// <summary>反变换行列式的绝对下限：比这还小就是退化（九个点共线之类）。</summary>
        private const double MinDeterminant = 1e-12;

        /// <summary>唯一实例。</summary>
        public static CalibrationService Shared { get; } = new CalibrationService();

        private readonly object _sync = new object();
        private readonly List<CalibPair> _pairs = new List<CalibPair>();

        /// <summary>保住算变换用的那个对象，别被 GC 收走（与 VisionScheme._root 同一个理由）。</summary>
        private CogCalibNPointToNPoint _calibRoot;

        /// <summary>像素 → mm。没标定时是 null。</summary>
        private CogTransform2DLinear _pixelToRobot;

        private bool _calibrated;
        private double _rmsError;
        private double _mmPerPixel;
        private double _rotationDeg;
        private bool _handednessOk;
        private string _savedAt = string.Empty;
        private string _lastError = string.Empty;
        private bool _loaded;

        private CalibrationService()
        {
        }

        // ------------------------------------------------------------------
        // 读（生产线程也会读，全部走锁）
        // ------------------------------------------------------------------

        /// <summary>
        /// 是否**可用**：算过、且通过了手性检查。
        /// 注意"算过但是镜像"时这里返回 false（虽然那四个指标还留着给界面显示）——
        /// 主运行页的 Q5甲"没标定就拒绝分拣"就是读这个属性，镜像必须被当成"不能used"。
        /// </summary>
        public bool IsCalibrated
        {
            get { lock (_sync) { return _calibrated && _handednessOk; } }
        }

        /// <summary>当前点对数。</summary>
        public int PairCount
        {
            get { lock (_sync) { return _pairs.Count; } }
        }

        /// <summary>拟合误差（mm），越小越好。现场目标 &lt; 0.5mm。</summary>
        public double RmsError
        {
            get { lock (_sync) { return _rmsError; } }
        }

        /// <summary>反变换的 Scaling：一像素等于多少毫米。</summary>
        public double MmPerPixel
        {
            get { lock (_sync) { return _mmPerPixel; } }
        }

        /// <summary>相机坐标轴相对机械臂坐标轴的夹角（度）。</summary>
        public double RotationDeg
        {
            get { lock (_sync) { return _rotationDeg; } }
        }

        /// <summary>手性检查：true ＝ 两坐标系同手性（没镜像）。为 false 时拒绝保存。</summary>
        public bool HandednessOk
        {
            get { lock (_sync) { return _handednessOk; } }
        }

        /// <summary>标定保存时间（"2026-09-17 14:05:22"；没保存过就是空串）。</summary>
        public string SavedAt
        {
            get { lock (_sync) { return _savedAt; } }
        }

        /// <summary>最近一次失败的中文原因（成功后清空）。</summary>
        public string LastError
        {
            get { lock (_sync) { return _lastError; } }
        }

        /// <summary>给界面用的一句话（主运行页状态行、标定区状态行都读它）。</summary>
        public string Describe()
        {
            lock (_sync)
            {
                if (!_calibrated) return "无";
                if (!_handednessOk) return "镜像 ✗（不可用）";
                return _mmPerPixel.ToString("F4", CultureInfo.InvariantCulture) + "mm/px·RMSE "
                    + _rmsError.ToString("F3", CultureInfo.InvariantCulture);
            }
        }

        /// <summary>当前点对的拷贝（界面拿去显示/编辑，改它不影响服务）。</summary>
        public List<CalibPair> SnapshotPairs()
        {
            lock (_sync)
            {
                List<CalibPair> copy = new List<CalibPair>(_pairs.Count);
                foreach (CalibPair pair in _pairs)
                    copy.Add(new CalibPair(pair.PixelX, pair.PixelY, pair.RobotX, pair.RobotY));
                return copy;
            }
        }

        // ------------------------------------------------------------------
        // 编辑
        // ------------------------------------------------------------------

        /// <summary>清空点对与标定结果（不动磁盘上的文件）。</summary>
        public void Clear()
        {
            lock (_sync)
            {
                _pairs.Clear();
                _calibRoot = null;
                _pixelToRobot = null;
                _calibrated = false;
                _rmsError = 0;
                _mmPerPixel = 0;
                _rotationDeg = 0;
                _handednessOk = false;
                _lastError = string.Empty;
            }
        }

        /// <summary>
        /// 加一对点（四个数都必须是有限数）。返回 false 时 <paramref name="message"/> 是中文原因。
        /// </summary>
        public bool AddPair(double pixelX, double pixelY, double robotX, double robotY, out string message)
        {
            CalibPair pair = new CalibPair(pixelX, pixelY, robotX, robotY);
            if (!pair.IsFinite)
            {
                message = "这一行有非数字或无穷大的值，请检查（像素和机械臂坐标都必须是数字）。";
                return false;
            }

            lock (_sync)
            {
                // 同一对点重复喂进去会让拟合被它"加权"，这是手抄时最容易发生的事。
                for (int i = 0; i < _pairs.Count; i++)
                {
                    if (Math.Abs(_pairs[i].PixelX - pixelX) < 1e-9 && Math.Abs(_pairs[i].PixelY - pixelY) < 1e-9
                        && Math.Abs(_pairs[i].RobotX - robotX) < 1e-9 && Math.Abs(_pairs[i].RobotY - robotY) < 1e-9)
                    {
                        message = "第 " + (i + 1) + " 行和这一行完全一样（重复点），已忽略这一行。";
                        return false;
                    }
                }
                _pairs.Add(pair);
            }

            message = string.Empty;
            return true;
        }

        /// <summary>用当前点对重新算标定。返回 false 时 <paramref name="message"/> 是中文原因。</summary>
        public bool Calibrate(out string message)
        {
            lock (_sync)
            {
                _lastError = string.Empty;

                if (_pairs.Count < 3)
                {
                    message = "至少要 3 对点（推荐 9 对），现在只有 " + _pairs.Count + " 对。";
                    return Fail(message);
                }

                for (int i = 0; i < _pairs.Count; i++)
                {
                    if (!_pairs[i].IsFinite)
                    {
                        message = "第 " + (i + 1) + " 行不是有效数字。";
                        return Fail(message);
                    }
                }

                CogCalibNPointToNPoint calib = new CogCalibNPointToNPoint();
                try
                {
                    // 默认参数实测值（写死成显式赋值，不靠默认值 —— 以后有人改了默认值我们要知道）：
                    //   DOFsToCompute = ScalingAspectRotationSkewAndTranslation
                    //   ComputationMode = Linear
                    //   CalibratedOriginSpace = RawCalibrated，CalibratedOriginX/Y = 0
                    //   SwapCalibratedHandedness = false
                    calib.DOFsToCompute = CogNPointToNPointDOFConstants.ScalingAspectRotationSkewAndTranslation;
                    calib.ComputationMode = CogCalibFixComputationModeConstants.Linear;
                    calib.CalibratedOriginSpace = CogCalibNPointAdjustmentSpaceConstants.RawCalibrated;
                    calib.CalibratedOriginX = 0;
                    calib.CalibratedOriginY = 0;
                    calib.SwapCalibratedHandedness = false;

                    foreach (CalibPair pair in _pairs)
                        calib.AddPointPair(pair.PixelX, pair.PixelY, pair.RobotX, pair.RobotY);

                    calib.Calibrate();
                }
                catch (Exception ex)
                {
                    message = "标定算不出来：" + ex.Message
                        + "（最常见的原因：九个点差不多在一条直线上，或者像素/机械臂两列填串了）";
                    return Fail(message);
                }

                if (!calib.Calibrated)
                {
                    message = "标定没有完成（Calibrated=false）。请检查九个点是否分布太集中或共线。";
                    return Fail(message);
                }

                // ① 方向：取 RawCalibrated 那一支（语义 ＝ 回到人工填的 mm），再取反得到 像素→mm。
                ICogTransform2D raw = calib.GetComputedUncalibratedFromRawCalibratedTransform();
                CogTransform2DLinear fromRaw = raw as CogTransform2DLinear;
                if (fromRaw == null)
                {
                    message = "标定变换不是线性变换（ComputationMode 不是 Linear？），本期只支持 Linear。";
                    return Fail(message);
                }

                CogTransform2DLinear pixelToRobot = fromRaw.Invert();
                if (pixelToRobot == null || !IsUsable(pixelToRobot))
                {
                    message = "标定结果无效（变换里出现了 NaN / 无穷大，或行列式为 0）。";
                    return Fail(message);
                }

                // ② 自检：默认调整参数下，RawCalibrated 与 Calibrated 两条取法必须完全一致。
                //    不一致说明有人动了 CalibratedOriginX/Y / CalibratedXAxisRotation 之类的调整参数 ——
                //    那时候我们的"像素→mm"就不再是回到人工填的 mm 了，必须让上层知道，不能悄悄算。
                ICogTransform2D adjustable = calib.GetComputedUncalibratedFromCalibratedTransform();
                CogTransform2DLinear fromCalibrated = adjustable as CogTransform2DLinear;
                if (fromCalibrated != null)
                {
                    double dx, dy;
                    fromRaw.Invert().MapPoint(0, 0, out dx, out dy);
                    double ax, ay;
                    fromCalibrated.Invert().MapPoint(0, 0, out ax, out ay);
                    if (Math.Abs(ax - dx) > 1e-6 || Math.Abs(ay - dy) > 1e-6)
                    {
                        message = "标定的调整参数被改动过（Calibrated 与 RawCalibrated 不一致），"
                            + "本程序按 RawCalibrated 解释坐标。请把 CalibratedOriginX/Y、CalibratedXAxisRotation、"
                            + "SwapCalibratedHandedness 恢复成默认（0/0/0/false）后重算。";
                        return Fail(message);
                    }
                }

                double determinant = pixelToRobot.MatrixDeterminant;
                bool handednessOk = determinant > 0;

                _calibRoot = calib;                 // 保住对象树，别被 GC 收走
                _pixelToRobot = pixelToRobot;
                _calibrated = true;
                _rmsError = calib.ComputedRMSError;
                _mmPerPixel = pixelToRobot.Scaling;
                _rotationDeg = pixelToRobot.Rotation * 180.0 / Math.PI;
                _handednessOk = handednessOk;

                if (!handednessOk)
                {
                    // 保留结果供界面显示（能看到手性 ✗ 和那四个数），但**不许保存** —— Save 会再拦一次。
                    message = "标定出现镜像（行列式 " + determinant.ToString("G6", CultureInfo.InvariantCulture)
                        + " < 0）：X 或 Y 有一个轴配反了。请检查像素列与机械臂列是不是把轴对错了。"
                        + "（这种标定会让机械臂往反方向走，已拒绝保存）";
                    _lastError = message;
                    return false;
                }

                message = "标定完成：" + Describe() + "，旋转 "
                    + _rotationDeg.ToString("F2", CultureInfo.InvariantCulture) + "°，手性 ✓";
                return true;
            }
        }

        /// <summary>像素 → 机械臂 mm。没有标定时返回 false，<paramref name="message"/> 是中文原因。</summary>
        public bool PixelToRobot(double pixelX, double pixelY, out double robotX, out double robotY, out string message)
        {
            robotX = 0;
            robotY = 0;

            // 先兜一次懒加载：调用方（如生产循环）可能没显式 Load 过，
            // 漏了的话文件明明在也会被误报成"还没标定"。
            string ensureMessage;
            EnsureLoaded(out ensureMessage);

            CogTransform2DLinear transform;
            lock (_sync)
            {
                if (!_calibrated || _pixelToRobot == null)
                {
                    message = "还没标定（config\\calib.json 不存在或没算过），不能把像素换算成机械臂坐标。";
                    _lastError = message;
                    return false;
                }
                if (!_handednessOk)
                {
                    message = "当前标定是镜像的（手性 ✗），拒绝用它驱动机械臂。";
                    _lastError = message;
                    return false;
                }
                transform = _pixelToRobot;
            }

            try
            {
                // MapPoint 是只读的，放在锁外面调用（免得一个异常把锁带住）。
                transform.MapPoint(pixelX, pixelY, out robotX, out robotY);
            }
            catch (Exception ex)
            {
                message = "像素换算失败：" + ex.Message;
                lock (_sync) { _lastError = message; }
                return false;
            }

            if (double.IsNaN(robotX) || double.IsInfinity(robotX) || double.IsNaN(robotY) || double.IsInfinity(robotY))
            {
                message = "像素换算出来不是有效数字（NaN/无穷大），标定文件可能被改坏了。";
                lock (_sync) { _lastError = message; }
                return false;
            }

            message = string.Empty;
            return true;
        }

        // ------------------------------------------------------------------
        // 存取
        // ------------------------------------------------------------------

        /// <summary>标定文件全路径：&lt;程序目录&gt;\config\calib.json。</summary>
        public static string ConfigPath
        {
            get
            {
                return Path.Combine(System.Windows.Forms.Application.StartupPath, ConfigFolder, ConfigFileName);
            }
        }

        /// <summary>
        /// 懒加载一次（生产路径和界面都可以直接调，重复调没副作用）。
        /// 返回 false 时 <paramref name="message"/> 是中文原因（文件不存在**不算**失败，只是没标定）。
        ///
        /// ⚠ 只在"内存里**什么都没有**"时才去读磁盘。
        /// 为什么不能只看 <c>_loaded</c>：`Load()` 是"清掉内存里的点对、按磁盘重读"，
        /// 而 `EnsureLoaded` 是被 `PixelToRobot` 顺手调用的兜底。如果有人先点了『计算』
        /// （算出内存标定、**还没保存**），再走到这里，`_loaded` 若是 false 就会把他刚算的东西抹掉。
        /// —— 自测里真踩到过：探针先 `Calibrate` 再 `PixelToRobot`，换算回了"还没标定"。
        /// （正常点界面流程下 `MainRunView.OnLoad` 已经在开机时把 `_loaded` 置上了，所以很难碰到；
        ///   但"很难碰到"不等于"不会碰到"。）
        /// </summary>
        public bool EnsureLoaded(out string message)
        {
            lock (_sync)
            {
                if (_loaded || _calibrated || _pairs.Count > 0)
                {
                    message = string.Empty;
                    return true;
                }
            }
            return Load(out message);
        }

        /// <summary>
        /// 从 config\calib.json 读点对并重算标定。
        /// 文件不存在 → 返回 true + 提示"还没标定"（不是错误）；
        /// 文件存在但坏了 → 返回 false + 中文原因。
        /// </summary>
        public bool Load(out string message)
        {
            string path = ConfigPath;
            if (!File.Exists(path))
            {
                lock (_sync)
                {
                    _loaded = true;
                    _pairs.Clear();
                    _calibrated = false;
                    _pixelToRobot = null;
                    _savedAt = string.Empty;
                    _lastError = string.Empty;
                }
                message = "还没标定过（找不到 " + path + "）。请按「九点标定操作指引」做一次标定。";
                return true;
            }

            string text;
            try
            {
                text = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                message = "读标定文件失败：" + ex.Message;
                return false;
            }

            List<CalibPair> pairs;
            string savedAt;
            double pickZ, standbyX, standbyY, placeX, placeY, placeZ;
            double minX, maxX, minY, maxY;
            try
            {
                object root = MiniJson.Parse(text);
                Dictionary<string, object> rootObject = root as Dictionary<string, object>;
                if (rootObject == null) throw new FormatException("文件最外层不是一个 JSON 对象");

                savedAt = MiniJson.GetString(rootObject, "savedAt", string.Empty);
                pickZ = MiniJson.GetDouble(rootObject, "pickZ", double.NaN);
                standbyX = MiniJson.GetDouble(rootObject, "standbyX", double.NaN);
                standbyY = MiniJson.GetDouble(rootObject, "standbyY", double.NaN);
                placeX = MiniJson.GetDouble(rootObject, "placeX", double.NaN);
                placeY = MiniJson.GetDouble(rootObject, "placeY", double.NaN);
                placeZ = MiniJson.GetDouble(rootObject, "placeZ", double.NaN);
                minX = MiniJson.GetDouble(rootObject, "pickAreaMinX", double.NaN);
                maxX = MiniJson.GetDouble(rootObject, "pickAreaMaxX", double.NaN);
                minY = MiniJson.GetDouble(rootObject, "pickAreaMinY", double.NaN);
                maxY = MiniJson.GetDouble(rootObject, "pickAreaMaxY", double.NaN);

                object rawPairs = MiniJson.Get(rootObject, "pairs");
                List<object> list = rawPairs as List<object>;
                if (list == null) throw new FormatException("缺少 pairs 数组");

                pairs = new List<CalibPair>(list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    Dictionary<string, object> item = list[i] as Dictionary<string, object>;
                    if (item == null) throw new FormatException("pairs 里第 " + (i + 1) + " 项不是对象");

                    CalibPair pair = new CalibPair(
                        MiniJson.GetDouble(item, "px", double.NaN),
                        MiniJson.GetDouble(item, "py", double.NaN),
                        MiniJson.GetDouble(item, "mx", double.NaN),
                        MiniJson.GetDouble(item, "my", double.NaN));

                    // 逐项查有限性：只说"至少 3 对点"会把人往错的方向引（真正的问题是某个数不是数字）。
                    if (!pair.IsFinite)
                        throw new FormatException("pairs 里第 " + (i + 1) + " 项的 px/py/mx/my 有一个不是数字");

                    pairs.Add(pair);
                }
            }
            catch (Exception ex)
            {
                message = "标定文件坏了（" + path + "）：" + ex.Message + "。请按「九点标定操作指引」重做一次标定。";
                return false;
            }

            lock (_sync)
            {
                _pairs.Clear();
                _pairs.AddRange(pairs);
                _savedAt = savedAt;
            }

            string calibrateMessage;
            if (!Calibrate(out calibrateMessage))
            {
                message = "标定文件里的点对算不出有效标定：" + calibrateMessage;
                return false;
            }

            // 抓取几何（抓取 Z + 待机位 + 放料点 + 工作范围）跟着标定一起重放：
            // 它们是同一批现场数据，分开存迟早对不上。
            // 唯一真相源仍然是 RobotArmSettings.Shared（RobotPickPlace 读它），这里只是灌一次值。
            // 缺字段就保留当前值（老 calib.json 没有 standbyX/placeX 这几个键，不能因此清零）。
            RobotArmSettings robot = RobotArmSettings.Shared;
            bool restored = false;
            if (!double.IsNaN(pickZ)) { robot.PickZ = pickZ; restored = true; }
            if (!double.IsNaN(standbyX)) { robot.StandbyX = standbyX; restored = true; }
            if (!double.IsNaN(standbyY)) { robot.StandbyY = standbyY; restored = true; }
            if (!double.IsNaN(placeX)) { robot.PlaceX = placeX; restored = true; }
            if (!double.IsNaN(placeY)) { robot.PlaceY = placeY; restored = true; }
            if (!double.IsNaN(placeZ)) { robot.PlaceZ = placeZ; restored = true; }
            if (!double.IsNaN(minX)) { robot.PickAreaMinX = minX; restored = true; }
            if (!double.IsNaN(maxX)) { robot.PickAreaMaxX = maxX; restored = true; }
            if (!double.IsNaN(minY)) { robot.PickAreaMinY = minY; restored = true; }
            if (!double.IsNaN(maxY)) { robot.PickAreaMaxY = maxY; restored = true; }

            lock (_sync) { _loaded = true; }

            message = "已载入标定：" + Describe() + "（" + PairCount + " 对点，标定于 "
                + (string.IsNullOrEmpty(SavedAt) ? "未知时间" : SavedAt) + "）"
                + (restored ? "；抓取 Z 与工作范围已一并恢复" : string.Empty);
            return true;
        }

        /// <summary>
        /// 把当前点对与抓取几何写进 config\calib.json。
        /// **手性不通过时拒绝保存** —— 镜像标定会让机械臂往反方向走，这种事不能留在磁盘上。
        /// </summary>
        public bool Save(out string message)
        {
            List<CalibPair> pairs;
            double rmsError, mmPerPixel, rotationDeg;
            bool calibrated, handednessOk;

            lock (_sync)
            {
                if (!_calibrated || _pixelToRobot == null)
                {
                    message = "还没算出标定，先点『计算』。";
                    return false;
                }
                if (!_handednessOk)
                {
                    message = "当前标定是镜像的（手性 ✗），拒绝保存。请检查 X/Y 有没有配错轴。";
                    return false;
                }
                if (_pairs.Count < 3)
                {
                    message = "点对太少（" + _pairs.Count + " 对），拒绝保存。";
                    return false;
                }

                pairs = new List<CalibPair>(_pairs);
                rmsError = _rmsError;
                mmPerPixel = _mmPerPixel;
                rotationDeg = _rotationDeg;
                calibrated = _calibrated;
                handednessOk = _handednessOk;
            }

            if (!calibrated || !handednessOk)
            {
                message = "标定状态不允许保存。";
                return false;
            }

            string savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            RobotArmSettings robot = RobotArmSettings.Shared;

            StringBuilder sb = new StringBuilder();
            sb.Append("{\r\n");
            sb.Append("  \"savedAt\": \"").Append(savedAt).Append("\",\r\n");
            // 派生指标给人看（6 位小数够读）；下面的点对是算标定用的原始数据，用 G17 保证原样往返。
            sb.Append("  \"rmsError\": ").Append(Report(rmsError)).Append(",\r\n");
            sb.Append("  \"mmPerPixel\": ").Append(Report(mmPerPixel)).Append(",\r\n");
            sb.Append("  \"rotationDeg\": ").Append(Report(rotationDeg)).Append(",\r\n");
            sb.Append("  \"handednessOk\": ").Append(handednessOk ? "true" : "false").Append(",\r\n");
            sb.Append("  \"pickZ\": ").Append(Exact(robot.PickZ)).Append(",\r\n");
            sb.Append("  \"standbyX\": ").Append(Exact(robot.StandbyX)).Append(",\r\n");
            sb.Append("  \"standbyY\": ").Append(Exact(robot.StandbyY)).Append(",\r\n");
            sb.Append("  \"placeX\": ").Append(Exact(robot.PlaceX)).Append(",\r\n");
            sb.Append("  \"placeY\": ").Append(Exact(robot.PlaceY)).Append(",\r\n");
            sb.Append("  \"placeZ\": ").Append(Exact(robot.PlaceZ)).Append(",\r\n");
            sb.Append("  \"pickAreaMinX\": ").Append(Exact(robot.PickAreaMinX)).Append(",\r\n");
            sb.Append("  \"pickAreaMaxX\": ").Append(Exact(robot.PickAreaMaxX)).Append(",\r\n");
            sb.Append("  \"pickAreaMinY\": ").Append(Exact(robot.PickAreaMinY)).Append(",\r\n");
            sb.Append("  \"pickAreaMaxY\": ").Append(Exact(robot.PickAreaMaxY)).Append(",\r\n");
            sb.Append("  \"pairs\": [\r\n");
            for (int i = 0; i < pairs.Count; i++)
            {
                CalibPair p = pairs[i];
                sb.Append("    { \"px\": ").Append(Exact(p.PixelX))
                  .Append(", \"py\": ").Append(Exact(p.PixelY))
                  .Append(", \"mx\": ").Append(Exact(p.RobotX))
                  .Append(", \"my\": ").Append(Exact(p.RobotY)).Append(" }");
                sb.Append(i < pairs.Count - 1 ? ",\r\n" : "\r\n");
            }
            sb.Append("  ]\r\n");
            sb.Append("}\r\n");

            string path = ConfigPath;
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                message = "写标定文件失败：" + ex.Message + "（" + path + "）";
                return false;
            }

            lock (_sync)
            {
                _savedAt = savedAt;
                _loaded = true;
                _lastError = string.Empty;
            }

            message = "已保存标定：" + path + "（" + pairs.Count + " 对点，" + Describe() + "）";
            return true;
        }

        // ------------------------------------------------------------------
        // 内部
        // ------------------------------------------------------------------

        /// <summary>用 <see cref="_pairs"/> 重算（Load 与界面『计算』都走它；调用方负责先装好点）。</summary>
        private bool Fail(string message)
        {
            _calibrated = false;
            _pixelToRobot = null;
            _lastError = message;
            return false;
        }

        /// <summary>变换能不能用：六个数都有限、行列式不退化。</summary>
        private static bool IsUsable(CogTransform2DLinear transform)
        {
            double[] values = new double[6];
            values[0] = transform.GetMatrixElement(0, 0);
            values[1] = transform.GetMatrixElement(0, 1);
            values[2] = transform.GetMatrixElement(1, 0);
            values[3] = transform.GetMatrixElement(1, 1);
            values[4] = transform.TranslationX;
            values[5] = transform.TranslationY;

            foreach (double v in values)
                if (double.IsNaN(v) || double.IsInfinity(v)) return false;

            return Math.Abs(transform.MatrixDeterminant) > MinDeterminant;
        }

        /// <summary>给人看的数（6 位小数）。</summary>
        private static string Report(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "0";
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        /// <summary>原样往返的数（G17；.NET Framework 的 "R" 不保证往返，别用）。</summary>
        private static string Exact(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "0";
            return value.ToString("G17", CultureInfo.InvariantCulture);
        }

        // ------------------------------------------------------------------
        // 极简 JSON（只为本文件服务：一个对象 + 一组点对）
        // 为什么不用 DataContractJsonSerializer：① 不引新引用；② 文件是给人核对的，
        // 出错时我们要能报"第 5 项的 mx 不是数字"这种中文原因。
        // ------------------------------------------------------------------

        private static class MiniJson
        {
            public static object Parse(string text)
            {
                Parser parser = new Parser(text);
                object value = parser.ParseValue();
                parser.SkipWhite();
                if (!parser.AtEnd) throw new FormatException("第 " + parser.Position + " 个字符后面还有多余内容");
                return value;
            }

            public static object Get(Dictionary<string, object> obj, string key)
            {
                object value;
                if (!obj.TryGetValue(key, out value)) return null;
                return value;
            }

            public static string GetString(Dictionary<string, object> obj, string key, string fallback)
            {
                object value = Get(obj, key);
                string text = value as string;
                return text == null ? fallback : text;
            }

            public static double GetDouble(Dictionary<string, object> obj, string key, double fallback)
            {
                object value = Get(obj, key);
                if (value == null) return fallback;
                if (value is double) return (double)value;
                string text = value as string;
                double parsed;
                if (text != null && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                    return parsed;
                return fallback;
            }

            private sealed class Parser
            {
                private readonly string _text;
                private int _index;

                public Parser(string text)
                {
                    _text = text ?? string.Empty;
                    _index = 0;
                }

                public bool AtEnd { get { return _index >= _text.Length; } }

                public int Position { get { return _index; } }

                public void SkipWhite()
                {
                    while (_index < _text.Length)
                    {
                        char c = _text[_index];
                        if (c == ' ' || c == '\t' || c == '\r' || c == '\n') { _index++; continue; }
                        break;
                    }
                }

                public object ParseValue()
                {
                    SkipWhite();
                    if (AtEnd) throw new FormatException("内容意外结束");

                    char c = _text[_index];
                    switch (c)
                    {
                        case '{': return ParseObject();
                        case '[': return ParseArray();
                        case '"': return ParseString();
                        case 't': Expect("true"); return true;
                        case 'f': Expect("false"); return false;
                        case 'n': Expect("null"); return null;
                        default: return ParseNumber();
                    }
                }

                private Dictionary<string, object> ParseObject()
                {
                    Dictionary<string, object> result = new Dictionary<string, object>();
                    _index++;   // {
                    SkipWhite();
                    if (!AtEnd && _text[_index] == '}') { _index++; return result; }

                    while (true)
                    {
                        SkipWhite();
                        if (AtEnd || _text[_index] != '"') throw new FormatException("第 " + _index + " 个字符处应该是键名（带引号）");
                        string key = ParseString();

                        SkipWhite();
                        if (AtEnd || _text[_index] != ':') throw new FormatException("键 " + key + " 后面缺少冒号");
                        _index++;

                        result[key] = ParseValue();

                        SkipWhite();
                        if (AtEnd) throw new FormatException("对象没有闭合（少一个 }）");
                        if (_text[_index] == ',') { _index++; continue; }
                        if (_text[_index] == '}') { _index++; return result; }
                        throw new FormatException("第 " + _index + " 个字符处应该是 , 或 }");
                    }
                }

                private List<object> ParseArray()
                {
                    List<object> result = new List<object>();
                    _index++;   // [
                    SkipWhite();
                    if (!AtEnd && _text[_index] == ']') { _index++; return result; }

                    while (true)
                    {
                        result.Add(ParseValue());
                        SkipWhite();
                        if (AtEnd) throw new FormatException("数组没有闭合（少一个 ]）");
                        if (_text[_index] == ',') { _index++; continue; }
                        if (_text[_index] == ']') { _index++; return result; }
                        throw new FormatException("第 " + _index + " 个字符处应该是 , 或 ]");
                    }
                }

                private string ParseString()
                {
                    _index++;   // 开引号
                    StringBuilder sb = new StringBuilder();
                    while (true)
                    {
                        if (AtEnd) throw new FormatException("字符串没有闭合（少一个引号）");
                        char c = _text[_index++];
                        if (c == '"') return sb.ToString();
                        if (c != '\\') { sb.Append(c); continue; }

                        if (AtEnd) throw new FormatException("字符串以反斜杠结尾");
                        char escape = _text[_index++];
                        switch (escape)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                if (_index + 4 > _text.Length) throw new FormatException("\\u 转义不完整");
                                sb.Append((char)Convert.ToInt32(_text.Substring(_index, 4), 16));
                                _index += 4;
                                break;
                            default: throw new FormatException("不认识的转义 \\" + escape);
                        }
                    }
                }

                private double ParseNumber()
                {
                    int start = _index;
                    while (_index < _text.Length)
                    {
                        char c = _text[_index];
                        if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E') { _index++; continue; }
                        break;
                    }
                    string text = _text.Substring(start, _index - start);
                    double value;
                    if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    {
                        // 说清"在第几个字符、看到的是什么"，别只报一个下标 —— 现场是人在看这个提示。
                        string seen = text.Length == 0
                            ? (_index < _text.Length ? "'" + _text[_index] + "'" : "文件结尾")
                            : text;
                        throw new FormatException("第 " + (start + 1) + " 个字符处应该是数字或引号，实际看到 " + seen);
                    }
                    return value;
                }

                private void Expect(string literal)
                {
                    if (_index + literal.Length > _text.Length
                        || string.CompareOrdinal(_text, _index, literal, 0, literal.Length) != 0)
                        throw new FormatException("第 " + _index + " 个字符处应该是 " + literal);
                    _index += literal.Length;
                }
            }
        }
    }
}
