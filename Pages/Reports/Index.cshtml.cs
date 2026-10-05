using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Primitives;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Manufacture.Services;

namespace Manufacture.Pages.Reports
{
    public class IndexModel : PageModel
    {
        private readonly MockAnalyticsService _analytics;
        private readonly MockRbacService _rbac;

        public IndexModel(MockAnalyticsService analytics, MockRbacService rbac)
        {
            _analytics = analytics;
            _rbac = rbac;
        }

        public const string RbacModule = "Reports";

        public string ActiveRole { get; set; } = "SuperAdmin";
        public bool CanView => _rbac.HasAccess(ActiveRole, RbacModule);

        /// <summary>"table" (default) or "charts".</summary>
        public string View { get; set; } = ReportViews.Table;

        public ReportResultDto Result { get; set; } = new();

        public List<ToggleOption> ViewOptions { get; set; } = new();
        public List<ToggleOption> ModuleOptions { get; set; } = new();
        public List<ToggleOption> TimeframeOptions { get; set; } = new();
        public List<ToggleOption> GroupByOptions { get; set; } = new();

        public bool IsChartsView => string.Equals(View, ReportViews.Charts, StringComparison.OrdinalIgnoreCase);
        public bool IsCustomRange => Result.Query.Timeframe == ReportTimeframe.Custom;
        public bool HasData => Result.TotalRows > 0;

        public void OnGet()
        {
            ActiveRole = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            View = NormaliseView(Request.Query["view"].ToString());

            Result = _analytics.QueryReports(BuildQuery());
            BuildOptions();
        }

        /// <summary>
        /// Honours <c>?view=table|charts</c> for the table/charts switcher. Filter
        /// state stays in the query string, so flipping the switch never resets a
        /// selection the user has already made.
        /// </summary>
        public string ToggleViewUrl(string view)
        {
            View = NormaliseView(view);
            return ViewUrl(View);
        }

        /// <summary>Exports every filtered row, not just the visible page.</summary>
        public IActionResult OnGetExportCsv() => Export(ReportExportService.BuildCsv, "text/csv; charset=utf-8", ReportExportService.SuggestedFileName);

        /// <summary>Same row set as the CSV export, laid out as a formatted PDF.</summary>
        public IActionResult OnGetExportPdf() => Export(ReportPdfService.BuildPdf, "application/pdf", ReportPdfService.SuggestedFileName);

        private IActionResult Export(
            Func<ReportResultDto, byte[]> render,
            string contentType,
            Func<ReportResultDto, string> fileName)
        {
            ActiveRole = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            if (!_rbac.HasAccess(ActiveRole, RbacModule))
            {
                return Forbid();
            }

            var result = _analytics.QueryReports(BuildQuery());
            return File(render(result), contentType, fileName(result));
        }

        // =============================================================
        // Query binding
        // =============================================================

        private ReportsQueryDto BuildQuery() => new()
        {
            Timeframe = ParseEnum(Request.Query["timeframe"], ReportTimeframe.Week),
            AnchorDate = ParseDate(Request.Query["date"]) ?? DateTime.UtcNow.Date,
            StartDate = ParseDate(Request.Query["start_date"]) ?? default,
            EndDate = ParseDate(Request.Query["end_date"]) ?? default,
            Lines = ParseEnumList<ProductType>(Request.Query["category"]),
            ProductIds = ParseIntList(Request.Query["product_id"]),
            RepIds = ParseIntList(Request.Query["rep_id"]),
            Route = Request.Query["route"].ToString().Trim(),
            Module = ParseEnum(Request.Query["module"], AnalyticsModule.Operational),
            GroupBy = ParseEnum(Request.Query["group_by"], ReportGroupBy.ProductLine),
            ChartGroupByRep = string.Equals(Request.Query["group_by"].ToString(), "rep", StringComparison.OrdinalIgnoreCase),
            Sort = Request.Query["sort"].ToString().Trim(),
            Direction = string.Equals(Request.Query["dir"].ToString(), "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc",
            Page = ParseInt(Request.Query["page"]) ?? 1,
            Limit = ParseInt(Request.Query["limit"]) ?? 12
        };

        private static T ParseEnum<T>(StringValues value, T fallback) where T : struct, Enum =>
            Enum.TryParse<T>(value.ToString(), ignoreCase: true, out var parsed) ? parsed : fallback;

        private static DateTime? ParseDate(StringValues value)
        {
            var raw = value.ToString().Trim();
            return raw.Length > 0 && DateTime.TryParse(raw, out var parsed) ? parsed.Date : null;
        }

        private static int? ParseInt(StringValues value)
        {
            var raw = value.ToString().Trim();
            return int.TryParse(raw, out var parsed) ? parsed : null;
        }

        private static List<int> ParseIntList(StringValues values) => Split(values)
            .Select(v => int.TryParse(v, out var parsed) ? parsed : (int?)null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .Distinct()
            .ToList();

        private static List<T> ParseEnumList<T>(StringValues values) where T : struct, Enum => Split(values)
            .Select(v => Enum.TryParse<T>(v, ignoreCase: true, out var parsed) ? parsed : (T?)null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .Distinct()
            .ToList();

        private static IEnumerable<string> Split(StringValues values) => values
            .SelectMany(v => (v ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(v => v.Length > 0);

        private static string NormaliseView(string raw) =>
            string.Equals(raw, ReportViews.Charts, StringComparison.OrdinalIgnoreCase)
                ? ReportViews.Charts
                : ReportViews.Table;

        private void BuildOptions()
        {
            var query = Result.Query;

            ViewOptions = new List<ToggleOption>
            {
                new() { Value = ReportViews.Table,  Label = "Table View",  Icon = "fa-solid fa-table-list", Hint = "Row-by-row detail with sorting and pagination", Selected = !IsChartsView },
                new() { Value = ReportViews.Charts, Label = "Charts & Visuals", Icon = "fa-solid fa-chart-column", Hint = "Interactive dashboards built on the same filters", Selected = IsChartsView }
            };

            ModuleOptions = new List<ToggleOption>
            {
                new() { Value = nameof(AnalyticsModule.Operational), Label = "Operational Reconciliation", Icon = "fa-solid fa-industry", Selected = query.Module == AnalyticsModule.Operational },
                new() { Value = nameof(AnalyticsModule.Financial), Label = "Financial Reconciliation", Icon = "fa-solid fa-money-bill-transfer", Selected = query.Module == AnalyticsModule.Financial },
                new() { Value = nameof(AnalyticsModule.Logistics), Label = "Logistics & Fleet", Icon = "fa-solid fa-truck-moving", Selected = query.Module == AnalyticsModule.Logistics }
            };

            TimeframeOptions = new List<ToggleOption>
            {
                new() { Value = nameof(ReportTimeframe.Daily),   Label = "Daily",           Icon = "fa-solid fa-sun",            Hint = "Single operating day", Selected = query.Timeframe == ReportTimeframe.Daily },
                new() { Value = nameof(ReportTimeframe.Week),    Label = "Operational Week",Icon = "fa-solid fa-calendar-week",  Hint = "Sunday to Saturday",         Selected = query.Timeframe == ReportTimeframe.Week },
                new() { Value = nameof(ReportTimeframe.Monthly), Label = "Monthly",         Icon = "fa-solid fa-calendar-days",  Hint = "Calendar month to date",     Selected = query.Timeframe == ReportTimeframe.Monthly },
                new() { Value = nameof(ReportTimeframe.Yearly),  Label = "Yearly",          Icon = "fa-solid fa-calendar",       Hint = "Year to date",               Selected = query.Timeframe == ReportTimeframe.Yearly },
                new() { Value = nameof(ReportTimeframe.Custom),  Label = "Custom Range",    Icon = "fa-solid fa-calendar-range", Hint = "Pick your own start and end", Selected = query.Timeframe == ReportTimeframe.Custom }
            };

            GroupByOptions = new List<ToggleOption>
            {
                new() { Value = nameof(ReportGroupBy.ProductLine), Label = "Product Line", Icon = "fa-solid fa-layer-group", Selected = query.GroupBy == ReportGroupBy.ProductLine },
                new() { Value = nameof(ReportGroupBy.Sku),         Label = "Product SKU",  Icon = "fa-solid fa-box",          Selected = query.GroupBy == ReportGroupBy.Sku },
                new() { Value = nameof(ReportGroupBy.SalesRep),    Label = "Sales Rep",    Icon = "fa-solid fa-user-group",    Selected = query.GroupBy == ReportGroupBy.SalesRep }
            };
        }

        // =============================================================
        // Link builders — every navigation preserves the active filters
        // =============================================================

        public string SortUrl(string columnKey)
        {
            var current = Result.Query.Sort;
            var next = string.Equals(current, columnKey, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(Result.Query.Direction, "asc", StringComparison.OrdinalIgnoreCase)
                ? "desc"
                : "asc";
            return BuildUrl(new Dictionary<string, string?> { ["sort"] = columnKey, ["dir"] = next });
        }

        public string PageUrl(int page) => BuildUrl(
            new Dictionary<string, string?> { ["page"] = Math.Clamp(page, 1, Result.TotalPages).ToString() },
            resetPage: false);

        /// <summary>Page-size links always return to page 1, since limits shift the row offsets.</summary>
        public string PageSizeUrl(string size) => BuildUrl(
            new Dictionary<string, string?> { ["limit"] = size, ["page"] = null });

        /// <summary>Switches the reconciliation module, keeping every other filter.</summary>
        public string BuildModuleUrl(string module) => BuildUrl(
            new Dictionary<string, string?> { ["module"] = module, ["page"] = null });

        public string ExportCsvUrl => ExportUrl("ExportCsv");

        public string ExportPdfUrl => ExportUrl("ExportPdf");

        /// <summary>Exports ignore the visible page: they cover every filtered row.</summary>
        private string ExportUrl(string handler) => BuildUrl(
            new Dictionary<string, string?> { ["handler"] = handler, ["page"] = null },
            resetPage: false);

        /// <summary>
        /// Re-runs the report in a different view mode while keeping every active
        /// filter, so toggling Table/Charts never drops the user's selections.
        /// </summary>
        public string ViewUrl(string view) => BuildUrl(
            new Dictionary<string, string?> { ["view"] = view, ["page"] = null });

        public bool IsPageFirst => Result.Page <= 1;
        public bool IsPageLast => Result.Page >= Result.TotalPages;

        public IReadOnlyList<int> PageNumbers
        {
            get
            {
                var last = Result.TotalPages;
                if (last <= 1) return Array.Empty<int>();

                var first = Math.Max(1, Result.Page - 2);
                var end = Math.Min(last, first + 4);
                first = Math.Max(1, end - 4);

                var pages = new List<int>();
                for (var i = first; i <= end; i++) pages.Add(i);
                return pages;
            }
        }

        public string[] PageSizeOptions { get; } = { "12", "25", "50", "100" };

        public string CurrentPageSize => Result.Limit.ToString(CultureInfo.InvariantCulture);

        /// <summary>Product SKUs grouped by line, for the multi-select filter.</summary>
        public IEnumerable<IGrouping<ProductType, ProductSkuDto>> CatalogByLine =>
            Result.Catalog.GroupBy(s => s.Line).OrderBy(g => (int)g.Key);

        /// <summary>The lifecycle stage a table column represents, if any.</summary>
        public static LifecycleMetric? LifecycleMetricFor(string columnKey) => columnKey switch
        {
            nameof(ReportRowDto.OrdersPlaced) => LifecycleMetric.OrdersPlaced,
            nameof(ReportRowDto.Produced) => LifecycleMetric.Produced,
            nameof(ReportRowDto.Loaded) => LifecycleMetric.Loaded,
            nameof(ReportRowDto.Sold) => LifecycleMetric.Sold,
            nameof(ReportRowDto.Returned) => LifecycleMetric.Returned,
            nameof(ReportRowDto.Damaged) => LifecycleMetric.Damaged,
            _ => null
        };

        /// <summary>Deep link used by the dashboard cards into a pre-filtered report.</summary>
        public string DeepLinkUrl(AnalyticsModule module, ReportTimeframe timeframe, string? category = null, int? repId = null)
        {
            var overrides = new Dictionary<string, string?>
            {
                ["module"] = module.ToString(),
                ["timeframe"] = timeframe.ToString(),
                ["view"] = ReportViews.Table,
                ["handler"] = null
            };

            if (category != null) overrides["category"] = category;
            if (repId.HasValue) overrides["rep_id"] = repId.Value.ToString();

            return BuildUrl(overrides, basePath: "/Reports");
        }

        private string BuildUrl(IDictionary<string, string?> overrides, bool resetPage = true, string basePath = "/Reports")
        {
            var values = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in Request.Query)
            {
                values[pair.Key] = pair.Value;
            }

            foreach (var pair in overrides)
            {
                if (pair.Value == null) values.Remove(pair.Key);
                else values[pair.Key] = new StringValues(pair.Value);
            }

            if (resetPage) values.Remove("page");

            var parts = values
                .SelectMany(entry => (entry.Value.Count == 0
                        ? new[] { entry.Key }
                        : entry.Value.ToArray())
                    .Select(v => Uri.EscapeDataString(entry.Key) + "=" + Uri.EscapeDataString(v ?? string.Empty)));

            return basePath + "?" + string.Join("&", parts);
        }

        public sealed class ToggleOption
        {
            public string Value { get; init; } = string.Empty;
            public string Label { get; init; } = string.Empty;
            public string Icon { get; init; } = string.Empty;
            public string Hint { get; init; } = string.Empty;
            public bool Selected { get; init; }
        }
    }

    /// <summary>View-mode values shared by the page model and the view.</summary>
    public static class ReportViews
    {
        public const string Table = "table";
        public const string Charts = "charts";
    }
}
