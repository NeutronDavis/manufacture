using System.Text;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Services
{
    /// <summary>
    /// Renders a report result to CSV.
    ///
    /// The column list comes from <see cref="ReportColumnCatalog"/> — the same
    /// metadata the on-screen table uses — so an export can never drift out of
    /// sync with the report the user is looking at.
    /// </summary>
    public static class ReportExportService
    {
        /// <summary>UTF-8 byte order mark so Excel detects ₦ and accented names correctly.</summary>
        private const string Bom = "\uFEFF";

        public static byte[] BuildCsv(ReportResultDto result)
        {
            var query = result.Query;
            var sb = new StringBuilder();

            sb.AppendLine(Line(new[]
            {
                "BakeryFlow ERP",
                result.ModuleLabel,
                query.Timeframe.ToString(),
                query.RangeLabel
            }));

            if (result.ActiveFilterLabels.Count > 0)
            {
                sb.AppendLine(Line(new[] { "Filters", string.Join(" | ", result.ActiveFilterLabels) }));
            }

            sb.AppendLine(Line(new[]
            {
                "Grouped by",
                result.GroupByLabel,
                "Generated (UTC)",
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")
            }));
            sb.AppendLine();

            var columns = result.Columns;
            sb.AppendLine(Line(columns.Select(c => c.Label).Prepend("Range")));

            var rows = result.AllRows.Count > 0 ? result.AllRows : result.Rows;
            foreach (var row in rows)
            {
                var values = columns
                    .Select(c => ReportColumnCatalog.FormatValue(row, c.Key, c))
                    .Prepend(query.RangeLabel);
                sb.AppendLine(Line(values));
            }

            sb.AppendLine();
            sb.AppendLine(TotalsLine(result));

            return Encoding.UTF8.GetBytes(Bom + sb.ToString());
        }

        public static string SuggestedFileName(ReportResultDto result)
        {
            var module = result.Module.ToString().ToLowerInvariant();
            var stamp = result.Query.AnchorDate.ToString("yyyyMMdd");
            var scope = result.Query.ChartGroupByRep ? "reps" : result.Query.GroupBy.ToString().ToLowerInvariant();
            return $"bakeryflow-{module}-{scope}-{stamp}.csv";
        }

        /// <summary>
        /// Builds the closing TOTAL row by walking the same column list the data
        /// rows use, so a figure can never end up under the wrong heading.
        /// </summary>
        private static string TotalsLine(ReportResultDto result)
        {
            var totals = ReportTotals.For(result);
            var values = result.Columns
                .Select(c => totals.TryGetValue(c.Key, out var value) ? value : string.Empty)
                .Prepend("TOTAL");

            return Line(values);
        }

        private static string Line(IEnumerable<string> values) =>
            string.Join(",", values.Select(Escape));

        private static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var needsQuotes = value.Contains(',') || value.Contains('"') ||
                             value.Contains('\n') || value.Contains('\r');

            return needsQuotes
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }
    }
}
