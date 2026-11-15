namespace Manufacture.Models.DTOs
{
    public enum ReportColumnType
    {
        Text = 0,
        Integer = 1,
        Number = 2,
        Money = 3,
        Percent = 4,
        Date = 5
    }

    public enum ReportTotalMode
    {
        None = 0,
        Sum = 1,
        Average = 2
    }

    public sealed class ReportViewColumnDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public ReportColumnType Type { get; set; } = ReportColumnType.Text;
        public ReportTotalMode TotalMode { get; set; } = ReportTotalMode.None;
        public string Align { get; set; } = "left";

        public static ReportViewColumnDto Text(string key, string label, string align = "left") =>
            new() { Key = key, Label = label, Type = ReportColumnType.Text, Align = align };

        public static ReportViewColumnDto Integer(string key, string label, bool total = false) =>
            new()
            {
                Key = key,
                Label = label,
                Type = ReportColumnType.Integer,
                Align = "right",
                TotalMode = total ? ReportTotalMode.Sum : ReportTotalMode.None
            };

        public static ReportViewColumnDto Number(string key, string label, bool total = false) =>
            new()
            {
                Key = key,
                Label = label,
                Type = ReportColumnType.Number,
                Align = "right",
                TotalMode = total ? ReportTotalMode.Sum : ReportTotalMode.None
            };

        public static ReportViewColumnDto Money(string key, string label, bool total = false) =>
            new()
            {
                Key = key,
                Label = label,
                Type = ReportColumnType.Money,
                Align = "right",
                TotalMode = total ? ReportTotalMode.Sum : ReportTotalMode.None
            };

        public static ReportViewColumnDto Percent(string key, string label, bool average = false) =>
            new()
            {
                Key = key,
                Label = label,
                Type = ReportColumnType.Percent,
                Align = "right",
                TotalMode = average ? ReportTotalMode.Average : ReportTotalMode.None
            };

        public static ReportViewColumnDto Date(string key, string label) =>
            new() { Key = key, Label = label, Type = ReportColumnType.Date, Align = "left" };
    }

    public sealed class ReportDefinitionDto
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string IconClass { get; set; } = "fa-solid fa-chart-column";
        public List<string> AllowedRoles { get; set; } = new();
        public bool SupportsCategoryFilter { get; set; }
        public bool SupportsRepFilter { get; set; }
        public bool SupportsStatusFilter { get; set; }
        public bool SupportsSearch { get; set; } = true;
        public int DefaultDays { get; set; } = 30;
        public bool SupportsCsv { get; set; } = true;
        public bool SupportsPdf { get; set; } = true;
    }

    public sealed class ReportFilterDto
    {
        public DateTime StartDate { get; set; } = DateTime.UtcNow.Date.AddDays(-29);
        public DateTime EndDate { get; set; } = DateTime.UtcNow.Date;
        public int? CategoryId { get; set; }
        public int? RecipeId { get; set; }
        public int? SalesRepId { get; set; }
        public string? Status { get; set; }
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public sealed class ReportQueryContext
    {
        public required ReportDefinitionDto Definition { get; init; }
        public required ReportFilterDto Filters { get; init; }
        public required string Role { get; init; }
        public int UserId { get; init; }
        public int? CurrentSalesRepId { get; init; }
        public int? CurrentCustomerId { get; init; }
    }

    public sealed class ReportViewResultDto
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string DateRangeLabel { get; set; } = string.Empty;
        public List<ReportViewColumnDto> Columns { get; set; } = new();
        public List<Dictionary<string, object?>> Rows { get; set; } = new();
        public Dictionary<string, object?> Totals { get; set; } = new();
        public int TotalRows { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalRows / (double)PageSize));
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
