using Microsoft.Extensions.Primitives;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Manufacture.Services;

namespace Manufacture.Api
{
    /// <summary>
    /// Read-only analytics endpoints over <see cref="MockAnalyticsService"/>.
    ///
    /// The Razor Pages UI is the primary surface; these endpoints exist so the
    /// dashboard and reports data can also be consumed by a future mobile
    /// client or BI tool. Both routes return exactly the payload the pages use,
    /// so the two can never diverge.
    /// </summary>
    public static class AnalyticsApi
    {
        public static void MapAnalyticsApi(this WebApplication app)
        {
            app.MapGet("/api/dashboard/overview", (HttpRequest request, MockAnalyticsService analytics) =>
            {
                var date = ParseDate(request.Query["date"]);
                var timeframe = request.Query["timeframe"].ToString();
                return Results.Ok(analytics.GetDashboardOverviewApi(timeframe, date));
            });

            app.MapGet("/api/dashboard/summary", (HttpRequest request, MockAnalyticsService analytics) =>
            {
                var date = ParseDate(request.Query["date"]);
                return Results.Ok(analytics.GetDashboardOverview(date));
            });

            app.MapGet("/api/reports/query", (HttpRequest request, MockAnalyticsService analytics) =>
            {
                var query = BuildQuery(request);
                return Results.Ok(analytics.QueryReports(query));
            });
        }

        internal static ReportsQueryDto BuildQuery(HttpRequest request)
        {
            var q = request.Query;
            return new ReportsQueryDto
            {
                Timeframe = Enum.TryParse<ReportTimeframe>(q["timeframe"], ignoreCase: true, out var tf)
                    ? tf
                    : ReportTimeframe.Week,
                AnchorDate = ParseDate(q["date"]) ?? DateTime.UtcNow.Date,
                StartDate = ParseDate(q["start_date"]) ?? default,
                EndDate = ParseDate(q["end_date"]) ?? default,
                Lines = ParseEnumList<ProductType>(q["category"]),
                ProductIds = ParseIntList(q["product_id"]),
                RepIds = ParseIntList(q["rep_id"]),
                Route = q["route"].ToString(),
                Module = Enum.TryParse<AnalyticsModule>(q["module"], ignoreCase: true, out var module)
                    ? module
                    : AnalyticsModule.Operational,
                GroupBy = Enum.TryParse<ReportGroupBy>(q["group_by"], ignoreCase: true, out var group)
                    ? group
                    : ReportGroupBy.ProductLine,
                ChartGroupByRep = string.Equals(q["group_by"].ToString(), "rep", StringComparison.OrdinalIgnoreCase),
                Sort = q["sort"].ToString(),
                Direction = q["dir"].ToString(),
                Page = ParseInt(q["page"]) ?? 1,
                Limit = ParseInt(q["limit"]) ?? 12
            };
        }

        private static DateTime? ParseDate(StringValues value)
        {
            var raw = value.ToString().Trim();
            if (string.IsNullOrEmpty(raw)) return null;
            return DateTime.TryParse(raw, out var parsed) ? parsed.Date : null;
        }

        private static int? ParseInt(StringValues value)
        {
            var raw = value.ToString().Trim();
            return int.TryParse(raw, out var parsed) ? parsed : null;
        }

        /// <summary>Accepts both <c>?id=1&amp;id=2</c> and <c>?id=1,2</c>.</summary>
        private static List<int> ParseIntList(StringValues values) =>
            Split(values)
                .Select(v => int.TryParse(v, out var parsed) ? parsed : (int?)null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .Distinct()
                .ToList();

        private static List<T> ParseEnumList<T>(StringValues values) where T : struct, Enum =>
            Split(values)
                .Select(v => Enum.TryParse<T>(v, ignoreCase: true, out var parsed) ? parsed : (T?)null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .Distinct()
                .ToList();

        private static IEnumerable<string> Split(StringValues values) =>
            values
                .SelectMany(v => (v ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(v => v.Length > 0);
    }
}
