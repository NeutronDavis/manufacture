using System.Text;
using Manufacture.Models.DTOs;

namespace Manufacture.Services.Reports
{
    public static class GenericReportExportService
    {
        public static byte[] BuildCsv(ReportViewResultDto result)
        {
            var sb = new StringBuilder();
            sb.AppendLine(CsvLine(["MauRecords", result.Title, result.DateRangeLabel]));
            sb.AppendLine(CsvLine(["Generated (UTC)", result.GeneratedAt.ToString("yyyy-MM-dd HH:mm"), "Rows", result.TotalRows.ToString("N0")]));
            sb.AppendLine();
            sb.AppendLine(CsvLine(result.Columns.Select(c => c.Label)));
            foreach (var row in result.Rows)
            {
                sb.AppendLine(CsvLine(result.Columns.Select(c => ReportFormatter.Format(row.GetValueOrDefault(c.Key), c.Type))));
            }
            if (result.Totals.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(CsvLine(result.Columns.Select(c =>
                    result.Totals.TryGetValue(c.Key, out var value) ? ReportFormatter.Format(value, c.Type) : string.Empty)));
            }
            return Encoding.UTF8.GetBytes("\uFEFF" + sb);
        }

        public static string CsvFileName(ReportViewResultDto result) =>
            $"{result.Key}-{DateTime.UtcNow:yyyyMMdd}.csv";

        public static byte[] BuildPdf(ReportViewResultDto result)
        {
            const double margin = 28;
            const double width = 960;
            const double height = 680;
            const double rowHeight = 15;

            var doc = new PdfDocument();
            var page = doc.AddPage(width, height);
            var y = 28d;
            var ink = PdfColor.FromBytes(24, 24, 27);
            var muted = PdfColor.FromBytes(100, 100, 108);
            var accent = PdfColor.FromBytes(180, 83, 9);

            page.DrawText("MauRecords", PdfFont.Bold, 9, accent, margin, y);
            y += 18;
            page.DrawText(result.Title, PdfFont.Bold, 16, ink, margin, y);
            y += 20;
            page.DrawText($"{result.DateRangeLabel}  |  Generated {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC  |  {result.TotalRows:N0} rows", PdfFont.Regular, 8, muted, margin, y);
            y += 22;

            var columns = result.Columns;
            var usableWidth = width - (margin * 2);
            var columnWidth = usableWidth / Math.Max(1, columns.Count);
            foreach (var column in columns.Select((value, index) => (value, index)))
            {
                page.DrawText(column.value.Label, PdfFont.Bold, 6.5, muted, margin + (column.index * columnWidth), y);
            }
            y += 13;
            page.FillRect(margin, y, usableWidth, 0.6, muted);
            y += 6;

            foreach (var row in result.Rows)
            {
                if (y + rowHeight > height - 34)
                {
                    page = doc.AddPage(width, height);
                    y = 28;
                    foreach (var column in columns.Select((value, index) => (value, index)))
                    {
                        page.DrawText(column.value.Label, PdfFont.Bold, 6.5, muted, margin + (column.index * columnWidth), y);
                    }
                    y += 13;
                    page.FillRect(margin, y, usableWidth, 0.6, muted);
                    y += 6;
                }

                for (var i = 0; i < columns.Count; i++)
                {
                    var text = ReportFormatter.Format(row.GetValueOrDefault(columns[i].Key), columns[i].Type);
                    page.DrawText(text, PdfFont.Regular, 6.5, ink, margin + (i * columnWidth), y);
                }
                y += rowHeight;
            }

            if (result.Totals.Count > 0)
            {
                y += 4;
                page.FillRect(margin, y, usableWidth, 0.8, ink);
                y += 8;
                for (var i = 0; i < columns.Count; i++)
                {
                    if (!result.Totals.TryGetValue(columns[i].Key, out var value)) continue;
                    page.DrawText(ReportFormatter.Format(value, columns[i].Type), PdfFont.Bold, 7, ink, margin + (i * columnWidth), y);
                }
            }

            return doc.Build();
        }

        public static string PdfFileName(ReportViewResultDto result) =>
            $"{result.Key}-{DateTime.UtcNow:yyyyMMdd}.pdf";

        private static string CsvLine(IEnumerable<string> values) =>
            string.Join(",", values.Select(Escape));

        private static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }
    }
}
