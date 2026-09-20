namespace VisionSort.Services
{
    /// <summary>
    /// 数据库参数（系统设置页「数据库配置」的唯一真相源）。
    /// 与 <see cref="ConveyorSettings.Shared"/> / <see cref="VisionRunSettings.Shared"/> 同风格：
    /// 静态单例；系统设置页改它，数据层只读它，落盘由「保存参数」写 config\sysparam.json 的 db 组。
    ///
    /// 连接串默认值（Q12=A：现场 MySQL 装在工控机本机）里每个参数都有理由：
    ///   CharSet=utf8mb4              中文工件编号/备注，含生僻字与 emoji；
    ///   SslMode=None + AllowPublicKeyRetrieval=True
    ///                                实测（本机 MySQL 8.0.44 + caching_sha2_password）唯一能跑通的组合：
    ///                                · SslMode=Preferred 在本机直接失败——MySqlConnector 先带 TLS 1.3，
    ///                                  schannel 报"指定的值在 SslProtocolType 枚举中无效"；限死
    ///                                  TlsVersion=Tls12 后又报"安全包中没有可用的凭证"
    ///                                  （SEC_E_NO_CREDENTIALS，与 curl/Invoke-WebRequest 同一个 schannel 故障）；
    ///                                · SslMode=None 时 caching_sha2_password 仍会向服务端取 RSA 公钥
    ///                                  加密口令交换（**不是明文发口令**），所以配 AllowPublicKeyRetrieval=True；
    ///                                · 现场若强制要求 TLS：把 SslMode 改回 Preferred 并加 TlsVersion=Tls12，
    ///                                  该机器上能否协商成功要到现场验（见 数据库方案.md §十三/§十四）。
    ///   ConnectionTimeout=3           连不上就 3 秒内返回，不许把生产节拍挂在数据库上；
    ///   DefaultCommandTimeout=5       同理，单条语句最多 5 秒；
    ///   Pooling=True;MaximumPoolSize=4 驱动自带连接池：界面查询 + 补写线程够用，且不放大连接数。
    ///
    /// 口令按 Q4=A **明文**存在 sysparam.json 里（与 modbus/robot/vision 三组一致）。
    /// 任何写日志/弹提示的地方都必须用 <see cref="SafeConnectionString"/>，不要用原始串。
    /// </summary>
    public sealed class DbSettings
    {
        /// <summary>唯一实例。</summary>
        public static DbSettings Shared { get; } = new DbSettings();

        /// <summary>数据库类型。目前只支持 MySQL（Q6=A：下拉也只留这一项）。</summary>
        public string DbType = "MySQL";

        /// <summary>连接字符串（唯一一份真相，界面直接编辑它）。</summary>
        public string ConnectionString =
            "Server=127.0.0.1;Port=3306;Database=vision_sort;Uid=vision;Pwd=CHANGE_ME;" +
            "CharSet=utf8mb4;SslMode=None;AllowPublicKeyRetrieval=True;" +
            "ConnectionTimeout=3;DefaultCommandTimeout=5;Pooling=True;MaximumPoolSize=4";

        /// <summary>结果表名。只允许字母/数字/下划线（拼进 DDL 前会再校验一次）。</summary>
        public string TableName = "vision_result";

        /// <summary>
        /// 单条 SQL 的命令超时，秒（写进 <c>MySqlCommand.CommandTimeout</c>）。
        /// 连接超时不在这里——它由连接串的 <c>ConnectionTimeout=3</c> 决定，避免同一件事两个真相源。
        /// </summary>
        public int CommandTimeoutSec = 5;

        /// <summary>断网补写失败后的重试间隔，秒。</summary>
        public int RetryIntervalSec = 5;

        /// <summary>
        /// 断网补写的暂存文件路径；留空 = 默认 &lt;exe&gt;\config\pending_results.jsonl。
        /// 只有自测探针会改它（把补写文件指到临时目录），生产不填。
        /// </summary>
        public string PendingFilePath = string.Empty;

        /// <summary>本进程内是否已确认过表存在（省掉每轮的 information_schema 查询）。</summary>
        public bool SchemaChecked;

        private DbSettings()
        {
        }

        /// <summary>打码后的连接串：写日志、弹提示、记实测记录一律用它。</summary>
        public string SafeConnectionString
        {
            get { return MaskPassword(ConnectionString); }
        }

        /// <summary>把连接串里的 Pwd/Password 值换成 ***，其余原样保留（便于排错时看清主机/库名）。</summary>
        public static string MaskPassword(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) return connectionString;

            string[] parts = connectionString.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                int eq = part.IndexOf('=');
                if (eq <= 0) continue;

                string key = part.Substring(0, eq).Trim();
                if (key.Equals("Pwd", System.StringComparison.OrdinalIgnoreCase)
                    || key.Equals("Password", System.StringComparison.OrdinalIgnoreCase))
                {
                    parts[i] = key + "=***";
                }
            }
            return string.Join(";", parts);
        }
    }
}
