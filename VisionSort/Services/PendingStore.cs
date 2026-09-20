using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace VisionSort.Services
{
    /// <summary>
    /// 断网补写的本地暂存（Q8=A）：一行一条 JSON 的 JSONL 文件，位置
    /// &lt;exe&gt;\config\pending_results.jsonl。
    ///
    /// 设计取舍：
    ///   · 用 JSONL 而不是"一条大 JSON"：崩溃时最多丢最后半行，前面的都还能读；
    ///     写完一条 flush 一条，DB 恢复后按行补写，成功后整文件重写。
    ///   · 手写 JSON 读写而不是引第三方序列化库：字段只有 8 个且都是简单类型，
    ///     与项目"零 NuGet 依赖、libs 本地引用"的约定一致（见 数据库方案.md §四）。
    ///   · 本类不做去重/排序，也不判断 DB 是否可达——那是 <see cref="ResultRepository"/> 的事。
    /// </summary>
    public sealed class PendingStore
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private readonly object _sync = new object();

        /// <summary>暂存文件绝对路径。</summary>
        public string FilePath { get; }

        /// <summary>当前待补写条数（内存计数，构造时读一次文件）。</summary>
        public int Count { get; private set; }

        public PendingStore()
            : this(Path.Combine(Application.StartupPath, "config", "pending_results.jsonl"))
        {
        }

        /// <summary>可指定路径的构造（探针用）。</summary>
        public PendingStore(string filePath)
        {
            FilePath = filePath;
            lock (_sync)
            {
                Count = ReadAllLocked().Count;
            }
        }

        /// <summary>追加一条（立即落盘）。失败不抛——补写是"尽力而为"，不能反过来打断生产。</summary>
        public void Append(ResultRecord record)
        {
            if (record == null) return;
            lock (_sync)
            {
                try
                {
                    string dir = Path.GetDirectoryName(FilePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.AppendAllText(FilePath, ToLine(record) + "\r\n", Utf8NoBom);
                    Count++;
                }
                catch (Exception)
                {
                    // 磁盘满 / 无权限：这里吞掉。调用方（补写线程）无力回天，
                    // 更不该因为"备份写不进去"而把已经发生的检测结果再抛异常出去。
                }
            }
        }

        /// <summary>批量追加。</summary>
        public void AppendRange(IEnumerable<ResultRecord> records)
        {
            if (records == null) return;
            foreach (ResultRecord r in records) Append(r);
        }

        /// <summary>读出全部待补写记录（解析不了的行直接跳过，不让一行坏数据卡死补写）。</summary>
        public List<ResultRecord> ReadAll()
        {
            lock (_sync)
            {
                return ReadAllLocked();
            }
        }

        /// <summary>用给定集合覆盖整个文件（补写成功后调用）。空集合＝删除文件。</summary>
        public void Rewrite(IEnumerable<ResultRecord> records)
        {
            lock (_sync)
            {
                try
                {
                    List<ResultRecord> list = new List<ResultRecord>(records ?? new ResultRecord[0]);
                    if (list.Count == 0)
                    {
                        if (File.Exists(FilePath)) File.Delete(FilePath);
                        Count = 0;
                        return;
                    }

                    string dir = Path.GetDirectoryName(FilePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    string temp = FilePath + ".tmp";
                    StringBuilder sb = new StringBuilder();
                    foreach (ResultRecord r in list) sb.Append(ToLine(r)).Append("\r\n");
                    File.WriteAllText(temp, sb.ToString(), Utf8NoBom);

                    // 用 Copy+Delete 而不是 File.Replace：本机实测 File.Replace 会抛
                    // UnauthorizedAccessException（图像保存那期踩过，见 图像保存配置方案.md §10.3）。
                    File.Copy(temp, FilePath, true);
                    File.Delete(temp);
                    Count = list.Count;
                }
                catch (Exception)
                {
                    // 同上：重写失败时保留原文件（至少数据还在），不抛。
                }
            }
        }

        private List<ResultRecord> ReadAllLocked()
        {
            List<ResultRecord> list = new List<ResultRecord>();
            try
            {
                if (!File.Exists(FilePath)) return list;
                foreach (string line in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    ResultRecord r = TryParse(line);
                    if (r != null) list.Add(r);
                }
            }
            catch (Exception)
            {
                // 读不动就当空（补写线程下一轮会再试）
            }
            return list;
        }

        // ------------------------------------------------------------------
        // 单行 JSON：写与读
        // ------------------------------------------------------------------

        private static string ToLine(ResultRecord r)
        {
            StringBuilder sb = new StringBuilder(256);
            sb.Append('{');
            sb.Append("\"uuid\":\"").Append(Escape(r.Uuid)).Append("\",");
            sb.Append("\"ts\":\"").Append(r.Time.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)).Append("\",");
            sb.Append("\"no\":\"").Append(Escape(r.WorkpieceNo)).Append("\",");
            sb.Append("\"recipe\":").Append(r.RecipeID.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"ok\":").Append(r.IsOk ? "1" : "0").Append(',');
            sb.Append("\"score\":").Append(r.Score.HasValue
                ? r.Score.Value.ToString("0.#####", CultureInfo.InvariantCulture)
                : "null").Append(',');
            sb.Append("\"path\":\"").Append(Escape(r.ImagePath)).Append("\",");
            sb.Append("\"note\":\"").Append(Escape(r.Note)).Append('"');
            sb.Append('}');
            return sb.ToString();
        }

        private static readonly Regex KeyPattern = new Regex("\"(?<k>uuid|ts|no|recipe|ok|score|path|note)\"\\s*:\\s*", RegexOptions.Compiled);

        /// <summary>解析一行；格式不对返回 null（调用方跳过）。</summary>
        internal static ResultRecord TryParse(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;
            string text = line.Trim();
            if (text.Length < 2 || text[0] != '{') return null;

            ResultRecord r = new ResultRecord();
            bool sawAny = false;

            foreach (Match m in KeyPattern.Matches(text))
            {
                int start = m.Index + m.Length;
                if (start >= text.Length) continue;

                string key = m.Groups["k"].Value;
                if (text[start] == '"')
                {
                    string raw;
                    int end = start + 1;
                    if (!ReadString(text, ref end, out raw)) continue;
                    switch (key)
                    {
                        case "uuid": r.Uuid = raw; break;
                        case "ts":
                            DateTime t;
                            if (DateTime.TryParseExact(raw, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture,
                                    DateTimeStyles.None, out t))
                                r.Time = t;
                            break;
                        case "no": r.WorkpieceNo = raw; break;
                        case "path": r.ImagePath = raw; break;
                        case "note": r.Note = raw; break;
                    }
                }
                else
                {
                    string raw = ReadLiteral(text, start);
                    switch (key)
                    {
                        case "recipe":
                            int recipe;
                            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out recipe)) r.RecipeID = recipe;
                            break;
                        case "ok": r.IsOk = raw == "1"; break;
                        case "score":
                            double score;
                            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out score)) r.Score = score;
                            break;
                    }
                }
                sawAny = true;
            }

            return sawAny ? r : null;
        }

        /// <summary>text[start] 是开引号；出来后 end 指向闭引号之后。</summary>
        private static bool ReadString(string text, ref int end, out string value)
        {
            StringBuilder sb = new StringBuilder();
            value = null;
            // 进函数时 end 已由调用方置为 start+1（跳过开引号）
            for (int i = end; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\\')
                {
                    if (i + 1 >= text.Length) return false;
                    char e = text[++i];
                    switch (e)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'u':
                            if (i + 4 >= text.Length) return false;
                            string hex = text.Substring(i + 1, 4);
                            ushort code;
                            if (!ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code)) return false;
                            sb.Append((char)code);
                            i += 4;
                            break;
                        default: sb.Append(e); break;
                    }
                }
                else if (c == '"')
                {
                    value = sb.ToString();
                    end = i + 1;
                    return true;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return false;
        }

        /// <summary>读一个非字符串字面量（数字 / true / false / null），到 , 或 } 为止。</summary>
        private static string ReadLiteral(string text, int start)
        {
            int i = start;
            while (i < text.Length && text[i] != ',' && text[i] != '}') i++;
            return text.Substring(start, i - start).Trim();
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            StringBuilder sb = new StringBuilder(value.Length + 8);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
