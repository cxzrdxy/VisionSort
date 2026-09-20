using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace VisionSort.Services
{
    /// <summary>
    /// 极简 .xlsx 写出器（「数据库方案.md」Q10=A：**零第三方依赖**）。
    ///
    /// xlsx 本质是一个 zip，扁平表格最少只需要 5 个部件：
    ///     [Content_Types].xml
    ///     _rels/.rels
    ///     xl/workbook.xml
    ///     xl/_rels/workbook.xml.rels
    ///     xl/worksheets/sheet1.xml
    /// 字符串用 <c t="inlineStr"> 内联写（不需要 sharedStrings.xml），数字用 &lt;v&gt;，
    /// 时间**按文本写**（避免 Excel 序列日期要配 styles.xml；要排序筛选时再加样式，见方案 §10.2）。
    ///
    /// 用法：一次只持有一行 → 边读库边写，万级记录也不会把内存吃满。
    ///     using (var w = new XlsxWriter(path, headers, widths)) { foreach (...) w.WriteRow(...); }
    ///
    /// 本类不碰数据库、不碰界面。
    /// </summary>
    public sealed class XlsxWriter : IDisposable
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private const string Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private const string NsRel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private const string NsPkgRel = "http://schemas.openxmlformats.org/package/2006/relationships";

        private readonly ZipArchive _zip;
        private readonly StreamWriter _sheet;
        private readonly string[] _headers;
        private readonly double[] _widths;
        private readonly string _sheetName;
        private int _row;          // 已写行数（含表头那一行）
        private bool _disposed;

        /// <summary>已写的数据行数（不含表头）。</summary>
        public int RowCount
        {
            get { return _row > 0 ? _row - 1 : 0; }
        }

        /// <param name="path">目标文件（存在会被覆盖）。</param>
        /// <param name="headers">表头（中文）。</param>
        /// <param name="widths">各列宽度（Excel 字符宽），可为 null。</param>
        /// <param name="sheetName">工作表名，默认「检测日志」。</param>
        public XlsxWriter(string path, string[] headers, double[] widths, string sheetName = "检测日志")
        {
            _headers = headers ?? new string[0];
            _widths = widths;
            _sheetName = string.IsNullOrEmpty(sheetName) ? "Sheet1" : sheetName;

            FileStream file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            try
            {
                _zip = new ZipArchive(file, ZipArchiveMode.Create, false, Utf8NoBom);

                WriteEntry("[Content_Types].xml", ContentTypes());
                WriteEntry("_rels/.rels", RootRels());

                // 工作表：表头 + 列宽先写，数据行随后流式追加
                ZipArchiveEntry sheetEntry = _zip.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Optimal);
                _sheet = new StreamWriter(sheetEntry.Open(), Utf8NoBom);
                _sheet.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                _sheet.Write("<worksheet xmlns=\"" + Ns + "\">");
                _sheet.Write("<sheetViews><sheetView workbookViewId=\"0\">" +
                             "<pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/>" +
                             "</sheetView></sheetViews>");
                _sheet.Write("<sheetFormatPr defaultRowHeight=\"15\"/>");
                WriteColumns();
                _sheet.Write("<sheetData>");

                WriteRow(_headers);   // 表头也当普通行写（样式留给 Excel 默认）
            }
            catch
            {
                file.Dispose();
                throw;
            }
        }

        /// <summary>写一行。null → 空格子；string → 文本；数值 → 数字；DateTime → 毫秒文本。</summary>
        public void WriteRow(params object[] cells)
        {
            if (_disposed) throw new ObjectDisposedException("XlsxWriter");

            _row++;
            _sheet.Write("<row r=\"" + _row.ToString(CultureInfo.InvariantCulture) + "\">");

            int count = cells == null ? 0 : cells.Length;
            for (int i = 0; i < count; i++)
            {
                object value = cells[i];
                if (value == null) continue;

                string reference = ColumnName(i) + _row.ToString(CultureInfo.InvariantCulture);
                string text = value as string;

                if (text != null)
                {
                    _sheet.Write("<c r=\"" + reference + "\" t=\"inlineStr\"><is><t xml:space=\"preserve\">");
                    _sheet.Write(Escape(text));
                    _sheet.Write("</t></is></c>");
                }
                else if (value is DateTime)
                {
                    _sheet.Write("<c r=\"" + reference + "\" t=\"inlineStr\"><is><t>");
                    _sheet.Write(((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
                    _sheet.Write("</t></is></c>");
                }
                else if (value is double || value is float || value is decimal || value is int || value is long)
                {
                    double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    if (double.IsNaN(number) || double.IsInfinity(number)) continue;
                    _sheet.Write("<c r=\"" + reference + "\"><v>" +
                                 number.ToString("R", CultureInfo.InvariantCulture) + "</v></c>");
                }
                else
                {
                    _sheet.Write("<c r=\"" + reference + "\" t=\"inlineStr\"><is><t xml:space=\"preserve\">");
                    _sheet.Write(Escape(value.ToString()));
                    _sheet.Write("</t></is></c>");
                }
            }

            _sheet.Write("</row>");
        }

        /// <summary>收尾：补 dimension、关闭 sheet，再写 workbook 与关系表，最后关 zip。</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _sheet.Write("</sheetData></worksheet>");
            _sheet.Dispose();

            // dimension 必须写在 sheetData 之前，但那时还不知道行数；Excel 容忍缺失/靠后，
            // 这里索性不写 dimension —— 实测 Excel 打开正常（见 数据库方案.md §十四）。
            WriteEntry("xl/workbook.xml", Workbook());
            WriteEntry("xl/_rels/workbook.xml.rels", WorkbookRels());

            _zip.Dispose();
        }

        // ------------------------------------------------------------------

        private void WriteColumns()
        {
            if (_widths == null || _widths.Length == 0) return;
            _sheet.Write("<cols>");
            for (int i = 0; i < _widths.Length; i++)
            {
                int oneBased = i + 1;
                _sheet.Write("<col min=\"" + oneBased + "\" max=\"" + oneBased + "\" width=\"" +
                             _widths[i].ToString("0.##", CultureInfo.InvariantCulture) + "\" customWidth=\"1\"/>");
            }
            _sheet.Write("</cols>");
        }

        private void WriteEntry(string name, string content)
        {
            ZipArchiveEntry entry = _zip.CreateEntry(name, CompressionLevel.Optimal);
            using (StreamWriter writer = new StreamWriter(entry.Open(), Utf8NoBom))
            {
                writer.Write(content);
            }
        }

        private string ContentTypes()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                   "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                   "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                   "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                   "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                   "</Types>";
        }

        private static string RootRels()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<Relationships xmlns=\"" + NsPkgRel + "\">" +
                   "<Relationship Id=\"rId1\" Type=\"" + NsRel + "/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                   "</Relationships>";
        }

        private string Workbook()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<workbook xmlns=\"" + Ns + "\" xmlns:r=\"" + NsRel + "\">" +
                   "<sheets><sheet name=\"" + Escape(_sheetName) + "\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
                   "</workbook>";
        }

        private static string WorkbookRels()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<Relationships xmlns=\"" + NsPkgRel + "\">" +
                   "<Relationship Id=\"rId1\" Type=\"" + NsRel + "/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                   "</Relationships>";
        }

        /// <summary>0 → A，25 → Z，26 → AA（列数不多，够用即止）。</summary>
        internal static string ColumnName(int index)
        {
            StringBuilder sb = new StringBuilder(3);
            int value = index;
            while (true)
            {
                sb.Insert(0, (char)('A' + value % 26));
                value = value / 26 - 1;
                if (value < 0) break;
            }
            return sb.ToString();
        }

        /// <summary>XML 转义 + 去掉控制字符（Excel 遇到非法字符会报"文件已损坏"）。</summary>
        internal static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            StringBuilder sb = new StringBuilder(text.Length + 16);
            foreach (char c in text)
            {
                switch (c)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append("&quot;"); break;
                    case '\'': sb.Append("&apos;"); break;
                    case '\t': sb.Append("&#9;"); break;
                    case '\n': sb.Append("&#10;"); break;
                    case '\r': break;
                    default:
                        if (c >= ' ') sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
