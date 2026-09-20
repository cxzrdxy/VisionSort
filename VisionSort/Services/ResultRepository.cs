using MySqlConnector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VisionSort.Services
{
    /// <summary>「测试连接」的结果：给设置页显示中文一句话，不把英文异常甩给操作工。</summary>
    public sealed class DbProbeResult
    {
        public bool Ok;
        public string ServerVersion = string.Empty;
        public string DatabaseName = string.Empty;
        public bool TableExists;
        public long RowCount;
        public string Message = string.Empty;
    }

    /// <summary>日志页四张统计卡（与列表用同一个 WHERE，保证卡片和表格永远对得上）。</summary>
    public sealed class LogSummary
    {
        public long Total;
        public long Ok;
        public long Ng;

        /// <summary>合格率 0~1；没有任何记录时返回 0（界面自己显示 0.00%）。</summary>
        public double Rate
        {
            get { return Total == 0 ? 0d : (double)Ok / Total; }
        }
    }

    /// <summary>
    /// MySQL 数据层（Q1=A MySqlConnector；Q8=A 后台队列 + 断网补写）。
    ///
    /// 用法（主运行页）：
    ///     ResultRepository.Shared.Enqueue(record);   // 立即返回，不阻塞节拍；首调用自动起后台泵
    ///
    /// 线程模型（承接 Modbus 那次的教训——那次是"非线程安全的主站被并发调用"）：
    ///   · <see cref="MySqlConnection"/> 不是线程安全的 → **每次操作各自 new 一个**，靠驱动自带连接池兜底；
    ///     界面查询、后台补写两条线永不共用连接对象。
    ///   · 队列用 <see cref="ConcurrentQueue{T}"/>；后台泵用 <see cref="Interlocked"/> 防重入。
    ///
    /// 失败策略：写库失败绝不打断生产。队列里的记录落到 <see cref="PendingStore"/>（JSONL），
    /// DB 恢复后自动补齐；唯一键 uuid 保证重试不产生重复行（INSERT IGNORE）。
    /// 本类不弹窗、不读界面；提示文案由调用方决定（见 数据库方案.md §七）。
    /// </summary>
    public sealed class ResultRepository
    {
        /// <summary>全局唯一实例（与 ConveyorSettings / VisionRunSettings / RobotArmSettings 同风格）。</summary>
        public static ResultRepository Shared { get; } = new ResultRepository();

        private const int BatchSize = 200;          // 一次最多写多少行
        private const int PumpIntervalMs = 1000;    // 后台泵节拍

        private readonly ConcurrentQueue<ResultRecord> _queue = new ConcurrentQueue<ResultRecord>();
        private readonly PendingStore _pending;
        private readonly object _pumpSync = new object();

        private System.Threading.Timer _pump;
        private int _pumping;
        private DateTime _nextRetryUtc = DateTime.MinValue;

        /// <summary>最近一次失败的简短原因（中文）；成功后清空。</summary>
        public string LastError { get; private set; } = string.Empty;

        /// <summary>累计成功写入数据库的行数（状态显示与自测用）。</summary>
        public long Written { get; private set; }

        /// <summary>当前排队中（尚未落库）的行数。</summary>
        public int QueuedCount
        {
            get { return _queue.Count; }
        }

        /// <summary>断网补写的暂存文件。</summary>
        public PendingStore Pending
        {
            get { return _pending; }
        }

        private ResultRepository()
        {
            string path = DbSettings.Shared.PendingFilePath;
            _pending = string.IsNullOrEmpty(path) ? new PendingStore() : new PendingStore(path);
        }

        // ==================================================================
        // 一、连接 / 建表 / 自检
        // ==================================================================

        /// <summary>
        /// 测试连接：连上 → 取服务端版本 → 看库/表在不在 → 有表就数行数。
        /// 不抛异常，全部结果塞进 <see cref="DbProbeResult"/>（设置页直接显示 Message）。
        /// </summary>
        public async Task<DbProbeResult> TestConnectionAsync(string connectionString = null)
        {
            DbProbeResult result = new DbProbeResult();
            string cs = string.IsNullOrWhiteSpace(connectionString) ? DbSettings.Shared.ConnectionString : connectionString;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(cs))
                {
                    await conn.OpenAsync().ConfigureAwait(false);
                    result.ServerVersion = conn.ServerVersion;
                    result.DatabaseName = conn.Database;
                    result.TableExists = await TableExistsAsync(conn).ConfigureAwait(false);

                    if (result.TableExists)
                    {
                        result.RowCount = await CountAsync(conn, DateTime.MinValue, DateTime.MaxValue, null).ConfigureAwait(false);
                        result.Message = string.Format(CultureInfo.InvariantCulture,
                            "连接成功。\r\n服务器版本：{0}\r\n数据库：{1}\r\n结果表：{2}（{3} 行）",
                            result.ServerVersion, result.DatabaseName, DbSettings.Shared.TableName, result.RowCount);
                    }
                    else
                    {
                        result.Message = string.Format(CultureInfo.InvariantCulture,
                            "连接成功，但结果表还不存在。\r\n服务器版本：{0}\r\n数据库：{1}\r\n请点「初始化建表」创建 {2}。",
                            result.ServerVersion, result.DatabaseName, DbSettings.Shared.TableName);
                    }

                    result.Ok = true;
                }
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.Message = Describe(ex, cs);
            }

            return result;
        }

        /// <summary>
        /// 建表（Q9=A：软件只建表，库由现场手工建好）。幂等，可反复点。
        /// 表名先校验，避免把配置里的怪字符拼进 DDL。
        /// </summary>
        public async Task EnsureSchemaAsync()
        {
            string table = DbSettings.Shared.TableName;
            if (!Regex.IsMatch(table, "^[A-Za-z_][A-Za-z0-9_]*$"))
                throw new InvalidOperationException("表名不合法（只允许字母/数字/下划线）：" + table);

            string ddl =
                "CREATE TABLE IF NOT EXISTS `" + table + "` (" +
                "  id           BIGINT UNSIGNED NOT NULL AUTO_INCREMENT," +
                "  uuid         CHAR(36)        NOT NULL                COMMENT '客户端幂等键：断网补写不重复'," +
                "  ts           DATETIME(3)     NOT NULL                COMMENT '检测时刻（客户端本地时间）'," +
                "  workpiece_no VARCHAR(64)     NOT NULL DEFAULT ''     COMMENT '工件编号 / 批次（主运行页「工件编号」输入框）'," +
                "  recipe_id    INT             NOT NULL DEFAULT 0      COMMENT '配方号，本期恒 0'," +
                "  result       TINYINT         NOT NULL                COMMENT '1=OK，0=NG'," +
                "  score        DECIMAL(6,5)        NULL                COMMENT 'PMA 匹配分数 0~1，未检出为 NULL'," +
                "  image_path   VARCHAR(260)    NOT NULL DEFAULT ''     COMMENT '图像文件，相对图像根目录'," +
                "  note         VARCHAR(255)    NOT NULL DEFAULT ''     COMMENT '备注：机械臂异常 / 检测超时 / 存图失败 / 补写标记'," +
                "  PRIMARY KEY (id)," +
                "  UNIQUE KEY uk_uuid (uuid)," +
                "  KEY ix_ts (ts)," +
                "  KEY ix_result_ts (result, ts)" +
                ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='检测结果'";

            using (MySqlConnection conn = await OpenAsync(null).ConfigureAwait(false))
            using (MySqlCommand cmd = new MySqlCommand(ddl, conn))
            {
                cmd.CommandTimeout = DbSettings.Shared.CommandTimeoutSec;
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            DbSettings.Shared.SchemaChecked = true;
        }

        // ==================================================================
        // 二、写：入队 / 直插 / 批量 / 补写
        // ==================================================================

        /// <summary>
        /// 主运行页唯一入口：把一行结果排进队列后立即返回（首调用自动起后台泵）。
        /// 不抛异常——生产流程不该因为"记录结果"这件事中断。
        /// </summary>
        public void Enqueue(ResultRecord record)
        {
            if (record == null) return;
            _queue.Enqueue(record);
            EnsurePump();
        }

        /// <summary>单行直插（幂等：同 uuid 再插为无操作）。返回受影响行数（0=已存在，1=新写入）。</summary>
        public async Task<int> InsertAsync(ResultRecord record, string connectionString = null)
        {
            if (record == null) return 0;
            using (MySqlConnection conn = await OpenAsync(connectionString).ConfigureAwait(false))
            {
                return await InsertAsync(conn, null, record).ConfigureAwait(false);
            }
        }

        /// <summary>批量直插（一个事务里逐行 INSERT IGNORE）。返回真正新写入的行数。</summary>
        public async Task<int> InsertBatchAsync(IList<ResultRecord> records)
        {
            if (records == null || records.Count == 0) return 0;
            using (MySqlConnection conn = await OpenAsync(null).ConfigureAwait(false))
            using (MySqlTransaction tx = await conn.BeginTransactionAsync().ConfigureAwait(false))
            {
                int written = 0;
                foreach (ResultRecord r in records)
                {
                    written += await InsertAsync(conn, tx, r).ConfigureAwait(false);
                }
                await tx.CommitAsync().ConfigureAwait(false);
                return written;
            }
        }

        /// <summary>
        /// 跑一轮后台泵：先把队列里的写出，再尝试补写 pending 文件。返回本轮写进行数。
        /// 定时器与探针都调它，保证"测到的"就是"线上跑的"。
        /// </summary>
        public async Task<int> PumpOnceAsync()
        {
            int written = 0;

            // ① 队列 → 数据库（失败就整批转 pending，交给下一轮补写）
            List<ResultRecord> batch = DequeueBatch(BatchSize);
            if (batch.Count > 0)
            {
                try
                {
                    written += await InsertBatchAsync(batch).ConfigureAwait(false);
                    LastError = string.Empty;
                }
                catch (Exception ex)
                {
                    LastError = Describe(ex, DbSettings.Shared.ConnectionString);
                    _pending.AppendRange(batch);
                    _nextRetryUtc = DateTime.UtcNow.AddSeconds(DbSettings.Shared.RetryIntervalSec);
                }
            }

            // ② pending 文件 → 数据库（间隔到了才试，避免连不上时每秒锤一次）
            if (_pending.Count > 0 && DateTime.UtcNow >= _nextRetryUtc)
            {
                try
                {
                    written += await FlushPendingAsync().ConfigureAwait(false);
                    LastError = string.Empty;
                }
                catch (Exception ex)
                {
                    LastError = Describe(ex, DbSettings.Shared.ConnectionString);
                    _nextRetryUtc = DateTime.UtcNow.AddSeconds(DbSettings.Shared.RetryIntervalSec);
                }
            }

            Written += written;
            return written;
        }

        /// <summary>
        /// 把 pending 文件里的记录补写进库：成功的删掉、失败的留下（不丢）。
        /// 幂等由 uuid + INSERT IGNORE 保证，所以"补写时重复"是安全的。
        /// </summary>
        public async Task<int> FlushPendingAsync()
        {
            List<ResultRecord> all = _pending.ReadAll();
            if (all.Count == 0) return 0;

            int written = 0;
            List<ResultRecord> failed = new List<ResultRecord>();

            using (MySqlConnection conn = await OpenAsync(null).ConfigureAwait(false))
            {
                for (int offset = 0; offset < all.Count; offset += BatchSize)
                {
                    int count = Math.Min(BatchSize, all.Count - offset);
                    List<ResultRecord> slice = all.GetRange(offset, count);
                    try
                    {
                        using (MySqlTransaction tx = await conn.BeginTransactionAsync().ConfigureAwait(false))
                        {
                            foreach (ResultRecord r in slice)
                            {
                                written += await InsertAsync(conn, tx, r).ConfigureAwait(false);
                            }
                            await tx.CommitAsync().ConfigureAwait(false);
                        }
                    }
                    catch (Exception)
                    {
                        // 这一段失败：本段及之后的都留着（下一轮重试）；已成功的段不回滚（靠 uuid 幂等）
                        for (int i = offset; i < all.Count; i++) failed.Add(all[i]);
                        break;
                    }
                }
            }

            _pending.Rewrite(failed);
            return written;
        }

        /// <summary>起后台泵（幂等；<see cref="Enqueue"/> 会自动调用）。</summary>
        public void EnsurePump()
        {
            if (_pump != null) return;
            lock (_pumpSync)
            {
                if (_pump == null)
                {
                    _pump = new System.Threading.Timer(PumpCallback, null, PumpIntervalMs, PumpIntervalMs);
                }
            }
        }

        /// <summary>停后台泵（退出/自测收尾用）。</summary>
        public void StopPump()
        {
            lock (_pumpSync)
            {
                if (_pump != null)
                {
                    _pump.Dispose();
                    _pump = null;
                }
            }
        }

        private async void PumpCallback(object state)
        {
            if (Interlocked.Exchange(ref _pumping, 1) == 1) return;
            try
            {
                if (_queue.IsEmpty && _pending.Count == 0) return;
                await PumpOnceAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LastError = Describe(ex, DbSettings.Shared.ConnectionString);
            }
            finally
            {
                Interlocked.Exchange(ref _pumping, 0);
            }
        }

        // ==================================================================
        // 三、读：查询 / 计数 / 统计（日志页用）
        // ==================================================================

        /// <summary>
        /// 分页查询。<paramref name="resultFilter"/>：null/空/"全部" = 不过滤，"OK"/"NG" = 过滤结果。
        /// 按 ts 倒序（同一毫秒内再按 id 倒序，保证分页稳定不跳行）。
        /// </summary>
        public async Task<List<ResultRecord>> QueryAsync(DateTime from, DateTime to, string resultFilter, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 200;

            string sql =
                "SELECT uuid, ts, workpiece_no, recipe_id, result, score, image_path, note FROM `" + DbSettings.Shared.TableName + "` " +
                "WHERE ts >= @from AND ts <= @to" + FilterSql(resultFilter) +
                " ORDER BY ts DESC, id DESC LIMIT @size OFFSET @offset";

            List<ResultRecord> rows = new List<ResultRecord>();
            using (MySqlConnection conn = await OpenAsync(null).ConfigureAwait(false))
            using (MySqlCommand cmd = new MySqlCommand(sql, conn))
            {
                cmd.CommandTimeout = DbSettings.Shared.CommandTimeoutSec;
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@to", to);
                AddFilterParameter(cmd, resultFilter);
                cmd.Parameters.AddWithValue("@size", pageSize);
                cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

                using (MySqlDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        rows.Add(ReadRecord(reader));
                    }
                }
            }
            return rows;
        }

        /// <summary>符合条件的总行数（分页控件用）。</summary>
        public async Task<long> CountAsync(DateTime from, DateTime to, string resultFilter)
        {
            using (MySqlConnection conn = await OpenAsync(null).ConfigureAwait(false))
            {
                return await CountAsync(conn, from, to, resultFilter).ConfigureAwait(false);
            }
        }

        /// <summary>四张统计卡：一条 SQL 出三个数，与列表同一个 WHERE。</summary>
        public async Task<LogSummary> SummaryAsync(DateTime from, DateTime to, string resultFilter)
        {
            LogSummary summary = new LogSummary();
            string sql =
                "SELECT COUNT(*), COALESCE(SUM(result = 1), 0), COALESCE(SUM(result = 0), 0) FROM `" +
                DbSettings.Shared.TableName + "` WHERE ts >= @from AND ts <= @to" + FilterSql(resultFilter);

            using (MySqlConnection conn = await OpenAsync(null).ConfigureAwait(false))
            using (MySqlCommand cmd = new MySqlCommand(sql, conn))
            {
                cmd.CommandTimeout = DbSettings.Shared.CommandTimeoutSec;
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@to", to);
                AddFilterParameter(cmd, resultFilter);

                using (MySqlDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    if (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        summary.Total = reader.GetInt64(0);
                        summary.Ok = reader.GetInt64(1);
                        summary.Ng = reader.GetInt64(2);
                    }
                }
            }
            return summary;
        }

        /// <summary>
        /// 流式逐行读（导出用）：不把结果集整块读进内存，读到一行回调一行。
        /// 回调在读取线程上同步执行，所以要快（写 zip 条目正合适）；返回总行数。
        /// 导出顺序用 ts 升序 + id 升序（与界面倒序相反：报表按时间从头看）。
        /// </summary>
        public async Task<long> WriteRowsAsync(DateTime from, DateTime to, string resultFilter, Action<ResultRecord> onRow)
        {
            if (onRow == null) throw new ArgumentNullException("onRow");

            string sql =
                "SELECT uuid, ts, workpiece_no, recipe_id, result, score, image_path, note FROM `" + DbSettings.Shared.TableName + "` " +
                "WHERE ts >= @from AND ts <= @to" + FilterSql(resultFilter) +
                " ORDER BY ts, id";

            long rows = 0;
            using (MySqlConnection conn = await OpenAsync(null).ConfigureAwait(false))
            using (MySqlCommand cmd = new MySqlCommand(sql, conn))
            {
                cmd.CommandTimeout = Math.Max(DbSettings.Shared.CommandTimeoutSec, 60);   // 导出可能很久，别被 5 秒掐断
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@to", to);
                AddFilterParameter(cmd, resultFilter);

                using (MySqlDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        onRow(ReadRecord(reader));
                        rows++;
                    }
                }
            }
            return rows;
        }

        // ==================================================================
        // 四、内部
        // ==================================================================

        private async Task<MySqlConnection> OpenAsync(string connectionString)
        {
            string cs = string.IsNullOrWhiteSpace(connectionString) ? DbSettings.Shared.ConnectionString : connectionString;
            MySqlConnection conn = new MySqlConnection(cs);
            try
            {
                await conn.OpenAsync().ConfigureAwait(false);
                return conn;
            }
            catch
            {
                conn.Dispose();
                throw;
            }
        }

        private async Task<bool> TableExistsAsync(MySqlConnection conn)
        {
            const string sql =
                "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @t";
            using (MySqlCommand cmd = new MySqlCommand(sql, conn))
            {
                cmd.CommandTimeout = DbSettings.Shared.CommandTimeoutSec;
                cmd.Parameters.AddWithValue("@t", DbSettings.Shared.TableName);
                return Convert.ToInt64(await cmd.ExecuteScalarAsync().ConfigureAwait(false)) > 0;
            }
        }

        private async Task<long> CountAsync(MySqlConnection conn, DateTime from, DateTime to, string resultFilter)
        {
            string sql = "SELECT COUNT(*) FROM `" + DbSettings.Shared.TableName + "` WHERE ts >= @from AND ts <= @to"
                         + FilterSql(resultFilter);
            using (MySqlCommand cmd = new MySqlCommand(sql, conn))
            {
                cmd.CommandTimeout = DbSettings.Shared.CommandTimeoutSec;
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@to", to);
                AddFilterParameter(cmd, resultFilter);
                return Convert.ToInt64(await cmd.ExecuteScalarAsync().ConfigureAwait(false));
            }
        }

        /// <summary>
        /// 单行写入。用 INSERT IGNORE 而不是 INSERT：uuid 撞车（补写重试、同一行重复提交）
        /// 时安静跳过，不抛异常——这是"重试安全"的全部依据。
        /// </summary>
        private async Task<int> InsertAsync(MySqlConnection conn, MySqlTransaction tx, ResultRecord r)
        {
            string sql =
                "INSERT IGNORE INTO `" + DbSettings.Shared.TableName + "` " +
                "(uuid, ts, workpiece_no, recipe_id, result, score, image_path, note) " +
                "VALUES (@uuid, @ts, @no, @recipe, @result, @score, @path, @note)";

            using (MySqlCommand cmd = new MySqlCommand(sql, conn, tx))
            {
                cmd.CommandTimeout = DbSettings.Shared.CommandTimeoutSec;
                cmd.Parameters.AddWithValue("@uuid", Trunc(r.Uuid, 36));
                cmd.Parameters.AddWithValue("@ts", r.Time);
                cmd.Parameters.AddWithValue("@no", Trunc(r.WorkpieceNo, 64));
                cmd.Parameters.AddWithValue("@recipe", r.RecipeID);
                cmd.Parameters.AddWithValue("@result", r.IsOk ? 1 : 0);
                cmd.Parameters.AddWithValue("@score", r.Score.HasValue
                    ? (object)Math.Round(Math.Max(0d, Math.Min(1d, r.Score.Value)), 5)
                    : DBNull.Value);
                cmd.Parameters.AddWithValue("@path", Trunc(r.ImagePath, 260));
                cmd.Parameters.AddWithValue("@note", Trunc(r.Note, 255));
                return await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        private static ResultRecord ReadRecord(MySqlDataReader reader)
        {
            // uuid 列是 CHAR(36)，MySqlConnector 默认会把它当 Guid 取出来（GuidFormat=Char36），
            // 直接 GetString 会抛 InvalidCastException（自测 T5d 实测踩到）。
            // 这里统一按 object 取再转字符串：是 Guid 就 ToString("D")，是 string 原样用。
            object uuidValue = reader.GetValue(0);

            ResultRecord r = new ResultRecord
            {
                Uuid = uuidValue is string ? (string)uuidValue : uuidValue.ToString(),
                Time = reader.GetDateTime(1),
                WorkpieceNo = reader.GetString(2),
                RecipeID = reader.GetInt32(3),
                IsOk = reader.GetInt32(4) == 1,
                Score = reader.IsDBNull(5) ? (double?)null : (double)reader.GetDecimal(5),
                ImagePath = reader.GetString(6),
                Note = reader.GetString(7)
            };
            return r;
        }

        private List<ResultRecord> DequeueBatch(int max)
        {
            List<ResultRecord> batch = new List<ResultRecord>(max);
            ResultRecord item;
            while (batch.Count < max && _queue.TryDequeue(out item))
            {
                batch.Add(item);
            }
            return batch;
        }

        private static string FilterSql(string resultFilter)
        {
            return NormalizeFilter(resultFilter) == null ? string.Empty : " AND result = @result";
        }

        private static void AddFilterParameter(MySqlCommand cmd, string resultFilter)
        {
            int? filter = NormalizeFilter(resultFilter);
            if (filter.HasValue) cmd.Parameters.AddWithValue("@result", filter.Value);
        }

        /// <summary>"全部"/null/空 → null；"OK" → 1；"NG" → 0。</summary>
        private static int? NormalizeFilter(string resultFilter)
        {
            if (string.IsNullOrWhiteSpace(resultFilter)) return null;
            string f = resultFilter.Trim();
            if (f.Equals("OK", StringComparison.OrdinalIgnoreCase)) return 1;
            if (f.Equals("NG", StringComparison.OrdinalIgnoreCase)) return 0;
            return null;
        }

        /// <summary>超长字段先截断再入库：宁可少几个字，也不要让整行被 MySQL 严格模式拒掉。</summary>
        private static string Trunc(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= max ? value : value.Substring(0, max);
        }

        /// <summary>把英文异常翻成"操作工能照着做"的中文一句话。</summary>
        internal static string Describe(Exception ex, string connectionString)
        {
            if (ex == null) return string.Empty;

            string masked = DbSettings.MaskPassword(connectionString);
            string all = ex.ToString();

            // TLS/schannel 这类失败必须单独给建议：异常类型是 MySqlException 包着 Win32Exception，
            // 光看类型会被归到"连不上服务器"，而真正要做的是改连接串（自测实测踩到）。
            if (all.Contains("SslProtocolType") || all.Contains("安全包中没有可用的凭证")
                || all.Contains("SEC_E_NO_CREDENTIALS") || all.Contains("SSL connection could not be established"))
            {
                return "TLS 协商失败：把连接串的 SslMode 改成 None（局域网内常见做法，配 AllowPublicKeyRetrieval=True），" +
                       "或保留 SslMode=Preferred 并加 TlsVersion=Tls12。";
            }

            // 连不上 / 连接超时：MySqlConnector 对这两种情况的 ErrorCode 不稳定（有时 0、有时 1042），
            // 按报文文本判更可靠（自测 T7a/T8 实测踩到——原来落到"数据库错误："那条兜底，操作工看不出该干什么）。
            if (all.Contains("Unable to connect to any of the specified MySQL hosts")
                || all.Contains("Connect Timeout expired")
                || all.Contains("Timeout expired"))
            {
                return "连不上数据库服务器：确认服务已启动、端口正确、防火墙放行。（" + masked + "）";
            }

            MySqlException mysql = ex as MySqlException;
            if (mysql != null)
            {
                switch ((int)mysql.ErrorCode)
                {
                    case 1045:
                        return "认证失败：账号或口令不对。（" + masked + "）";
                    // 1049=库不存在；1044=账号没有该库权限。**两者对操作工是同一句话**：
                    // 低权限账号连不存在的库时，MySQL 8 返回的是 1044 而不是 1049（避免泄露库是否存在），
                    // 所以不能把 1044 单独说成"权限问题"——现场最常见的原因其实是库名写错（自测 T9b 实测踩到）。
                    case 1044:
                    case 1049:
                        return "打不开数据库「" + ExtractDatabase(connectionString) + "」：库不存在（先按 数据库方案.md §6.1 建库），" +
                               "或账号没有该库的权限（让管理员授权）。";
                    case 1146:
                        return "结果表不存在：请先点「初始化建表」。";
                }

                if (mysql.InnerException is SocketException || mysql.Number == 0)
                {
                    return "连不上数据库服务器：确认服务已启动、端口正确、防火墙放行。（" + masked + "）";
                }
                return "数据库错误：" + mysql.Message;
            }

            if (ex is SocketException || ex.InnerException is SocketException)
            {
                return "连不上数据库服务器：确认服务已启动、端口正确、防火墙放行。（" + masked + "）";
            }
            if (ex is TimeoutException)
            {
                return "数据库操作超时（" + DbSettings.Shared.CommandTimeoutSec + " 秒）：服务器忙或网络不通。";
            }

            return ex.Message;
        }

        /// <summary>从连接串里取库名（只用于中文提示，取不到就返回 ?）。</summary>
        private static string ExtractDatabase(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) return "?";
            foreach (string part in connectionString.Split(';'))
            {
                int eq = part.IndexOf('=');
                if (eq <= 0) continue;
                string key = part.Substring(0, eq).Trim();
                if (key.Equals("Database", StringComparison.OrdinalIgnoreCase)
                    || key.Equals("Initial Catalog", StringComparison.OrdinalIgnoreCase))
                {
                    string value = part.Substring(eq + 1).Trim();
                    return value.Length == 0 ? "?" : value;
                }
            }
            return "?";
        }
    }
}
