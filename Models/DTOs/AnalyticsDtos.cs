using System.Text.Json.Serialization;
using Manufacture.Models.Entities;

namespace Manufacture.Models.DTOs
{
    // =====================================================================
    // Catalog
    // =====================================================================

    /// <summary>
    /// A sellable SKU projected out of the live recipe master so that reports
    /// always describe products the plant can actually produce.
    /// </summary>
    public class ProductSkuDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public ProductType Line { get; set; }
        public string LineLabel { get; set; } = string.Empty;
        public string OutputUnit { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
        public decimal BaselineYield { get; set; }
        public decimal BaseDailyDemand { get; set; }
        public string IconClass { get; set; } = "fa-solid fa-box";
    }

    /// <summary>A sales rep surfaced as a filter / drill-down option.</summary>
    public class RepFilterOptionDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string PosTerminalCode { get; set; } = string.Empty;
        public string VehicleRegistration { get; set; } = string.Empty;
    }

    // =====================================================================
    // Lifecycle aggregation
    // =====================================================================

    /// <summary>
    /// The six stages of the operational lifecycle plus the derived ratios the
    /// dashboard and the red-flag rules are built on.
    /// </summary>
    public class LifecycleTotalsDto
    {
        public int OrdersPlaced { get; set; }
        public int Produced { get; set; }
        public int Loaded { get; set; }
        public int Sold { get; set; }
        public int Returned { get; set; }
        public int Damaged { get; set; }
        public int TicketCount { get; set; }

        public decimal ExpectedRevenue { get; set; }
        public decimal Collected { get; set; }
        public decimal OutstandingBalance { get; set; }

        /// <summary>Produced as a percentage of what reps requested.</summary>
        public decimal FillRatePercent => Ratio(Produced, OrdersPlaced);

        /// <summary>Sold as a percentage of what was physically loaded.</summary>
        public decimal SellThroughPercent => Ratio(Sold, Loaded);

        /// <summary>Damaged as a percentage of stock that came back.</summary>
        public decimal DamagePercent => Ratio(Damaged, Returned);

        /// <summary>Collected as a percentage of invoiced revenue.</summary>
        public decimal CollectionRatePercent => Ratio(Collected, ExpectedRevenue);

        public static decimal Ratio(decimal part, decimal whole) =>
            whole <= 0 ? 0m : Math.Round(part / whole * 100m, 1);
    }

    // =====================================================================
    // Executive dashboard
    // =====================================================================

    /// <summary>A cumulative KPI strip: one lifecycle snapshot for one period.</summary>
    public class KpiPeriodCardDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string DateLabel { get; set; } = string.Empty;
        public string ReportsTimeframe { get; set; } = string.Empty;
        public int RepCount { get; set; }
        public LifecycleTotalsDto Totals { get; set; } = new();
    }

    /// <summary>Bread / Water / Popcorn roll-up used for the product rows.</summary>
    public class ProductLineBreakdownDto
    {
        public ProductType Line { get; set; }
        public string LineLabel { get; set; } = string.Empty;
        public string IconClass { get; set; } = "fa-solid fa-box";
        public decimal ShareOfVolumePercent { get; set; }
        public List<string> SkuNames { get; set; } = new();
        public LifecycleTotalsDto Totals { get; set; } = new();
    }

    /// <summary>One rep's contribution to a single lifecycle metric and period.</summary>
    public class RepDrillDownRowDto
    {
        public int RepId { get; set; }
        public string RepName { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string Terminal { get; set; } = string.Empty;
        public string VehicleRegistration { get; set; } = string.Empty;
        public int TicketCount { get; set; }
        public int OrdersPlaced { get; set; }
        public int Produced { get; set; }
        public int Loaded { get; set; }
        public int Sold { get; set; }
        public int Returned { get; set; }
        public int Damaged { get; set; }
        public decimal ExpectedRevenue { get; set; }
        public decimal Collected { get; set; }

        /// <summary>The physical-cash portion of <see cref="Collected"/>, excluding transfer and POS bank.</summary>
        public decimal CashCollected { get; set; }

        public decimal OutstandingBalance { get; set; }
        public decimal FillRatePercent { get; set; }
        public decimal SellThroughPercent { get; set; }
        public decimal DamagePercent { get; set; }

        public int GetMetric(LifecycleMetric metric) => metric switch
        {
            LifecycleMetric.OrdersPlaced => OrdersPlaced,
            LifecycleMetric.Produced => Produced,
            LifecycleMetric.Loaded => Loaded,
            LifecycleMetric.Sold => Sold,
            LifecycleMetric.Returned => Returned,
            LifecycleMetric.Damaged => Damaged,
            _ => 0
        };
    }

    /// <summary>
    /// Rep-by-rep detail for one product scope (All / Bread / Water / Popcorn),
    /// keyed by period key. This is what the dashboard slide-over renders.
    /// </summary>
    public class RepDrillDownScopeDto
    {
        public string ScopeKey { get; set; } = string.Empty;
        public string ScopeLabel { get; set; } = string.Empty;
        public Dictionary<string, List<RepDrillDownRowDto>> Periods { get; set; } = new();
    }

    /// <summary>Multi-channel tender split for the current period.</summary>
    public class TenderSplitDto
    {
        public decimal Cash { get; set; }
        public decimal BankTransfer { get; set; }
        public decimal PosBank { get; set; }
        public decimal Outstanding { get; set; }
        public decimal Total => Cash + BankTransfer + PosBank;
    }

    /// <summary>
    /// Product-by-product (SKU) detail for one product scope and period.
    /// Used by the executive slide-over drawer when a KPI is clicked.
    /// </summary>
    public class ProductDrillDownRowDto
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public ProductType Line { get; set; }
        public string LineLabel { get; set; } = string.Empty;
        public string OutputUnit { get; set; } = "units";
        public decimal UnitPrice { get; set; }
        public int OrdersPlaced { get; set; }
        public int Produced { get; set; }
        public int Loaded { get; set; }
        public int Sold { get; set; }
        public int Returned { get; set; }
        public int Damaged { get; set; }
        public decimal ExpectedRevenue { get; set; }
        public decimal FillRatePercent => OrdersPlaced <= 0 ? 0m : Math.Round(Loaded / (decimal)OrdersPlaced * 100m, 1);
        public decimal SellThroughPercent => Loaded <= 0 ? 0m : Math.Round(Sold / (decimal)Loaded * 100m, 1);
        public decimal DamagePercent => Loaded <= 0 ? 0m : Math.Round(Damaged / (decimal)Loaded * 100m, 1);
    }

    /// <summary>
    /// Product SKU breakdown for one scope (All / Bread / Water / Popcorn),
    /// keyed by period key.
    /// </summary>
    public class ProductDrillDownScopeDto
    {
        public string ScopeKey { get; set; } = string.Empty;
        public string ScopeLabel { get; set; } = string.Empty;
        public Dictionary<string, List<ProductDrillDownRowDto>> Periods { get; set; } = new();
    }

    public class ExecutiveOverviewDto
    {
        public DateTime GeneratedAt { get; set; }
        public DateTime AnchorDate { get; set; }
        public string AnchorDateLabel { get; set; } = string.Empty;
        public string PlantName { get; set; } = "North Plant";

        public LifecycleTotalsDto Today { get; set; } = new();
        public TenderSplitDto Tender { get; set; } = new();
        public List<KpiPeriodCardDto> Periods { get; set; } = new();
        public List<ProductLineBreakdownDto> ProductLines { get; set; } = new();
        public List<RepDrillDownRowDto> RepToday { get; set; } = new();
        public List<RepDrillDownScopeDto> DrillDownScopes { get; set; } = new();
        public List<ProductDrillDownScopeDto> ProductDrillDownScopes { get; set; } = new();
        public List<ExecutiveAlertDto> Alerts { get; set; } = new();

        /// <summary>
        /// The same widgets pre-computed for every cumulative window, keyed by
        /// period key. The dashboard timeframe switcher swaps whole panels rather
        /// than recomputing in the browser, so a number on screen always came
        /// from the engine and never from client-side arithmetic.
        /// </summary>
        public Dictionary<string, List<ProductLineBreakdownDto>> ProductLinesByPeriod { get; set; } = new();

        public Dictionary<string, TenderSplitDto> TenderByPeriod { get; set; } = new();

        public Dictionary<string, List<ExecutiveAlertDto>> AlertsByPeriod { get; set; } = new();
    }

    public class ExecutiveAlertDto
    {
        public string Type { get; set; } = "general";
        public string Severity { get; set; } = "info";
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response payload for GET /api/dashboard/overview contract.
    /// </summary>
    public class DashboardOverviewApiResponse
    {
        [JsonPropertyName("timeframe")]
        public string Timeframe { get; set; } = "today";

        [JsonPropertyName("operating_week_range")]
        public OperatingWeekRangeDto OperatingWeekRange { get; set; } = new();

        [JsonPropertyName("lifecycle_summary")]
        public LifecycleSummaryApiDto LifecycleSummary { get; set; } = new();

        [JsonPropertyName("categories")]
        public List<CategorySummaryApiDto> Categories { get; set; } = new();

        [JsonPropertyName("tender_split")]
        public TenderSplitApiDto TenderSplit { get; set; } = new();

        [JsonPropertyName("attention_alerts")]
        public List<AttentionAlertApiDto> AttentionAlerts { get; set; } = new();
    }

    public class OperatingWeekRangeDto
    {
        [JsonPropertyName("start")]
        public string Start { get; set; } = string.Empty;

        [JsonPropertyName("end")]
        public string End { get; set; } = string.Empty;
    }

    public class LifecycleSummaryApiDto
    {
        [JsonPropertyName("orders_placed")]
        public int OrdersPlaced { get; set; }

        [JsonPropertyName("produced")]
        public int Produced { get; set; }

        [JsonPropertyName("loaded")]
        public int Loaded { get; set; }

        [JsonPropertyName("sold")]
        public int Sold { get; set; }

        [JsonPropertyName("returned")]
        public int Returned { get; set; }

        [JsonPropertyName("damaged")]
        public int Damaged { get; set; }

        [JsonPropertyName("fill_rate_pct")]
        public decimal FillRatePct { get; set; }

        [JsonPropertyName("sell_through_pct")]
        public decimal SellThroughPct { get; set; }

        [JsonPropertyName("damage_rate_pct")]
        public decimal DamageRatePct { get; set; }

        [JsonPropertyName("collection_rate_pct")]
        public decimal CollectionRatePct { get; set; }
    }

    public class CategorySummaryApiDto
    {
        [JsonPropertyName("category_id")]
        public string CategoryId { get; set; } = string.Empty;

        [JsonPropertyName("category_name")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("volume_share_pct")]
        public decimal VolumeSharePct { get; set; }

        [JsonPropertyName("total_sold_units")]
        public int TotalSoldUnits { get; set; }

        [JsonPropertyName("gross_revenue")]
        public decimal GrossRevenue { get; set; }

        [JsonPropertyName("outstanding_balance")]
        public decimal OutstandingBalance { get; set; }

        [JsonPropertyName("lifecycle")]
        public CategoryLifecycleApiDto Lifecycle { get; set; } = new();
    }

    public class CategoryLifecycleApiDto
    {
        [JsonPropertyName("orders")]
        public int Orders { get; set; }

        [JsonPropertyName("produced")]
        public int Produced { get; set; }

        [JsonPropertyName("loaded")]
        public int Loaded { get; set; }

        [JsonPropertyName("sold")]
        public int Sold { get; set; }

        [JsonPropertyName("returned")]
        public int Returned { get; set; }

        [JsonPropertyName("damaged")]
        public int Damaged { get; set; }
    }

    public class TenderSplitApiDto
    {
        [JsonPropertyName("cash")]
        public decimal Cash { get; set; }

        [JsonPropertyName("transfer")]
        public decimal Transfer { get; set; }

        [JsonPropertyName("pos")]
        public decimal Pos { get; set; }

        [JsonPropertyName("outstanding")]
        public decimal Outstanding { get; set; }
    }

    public class AttentionAlertApiDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    // =====================================================================
    // Reports query + result
    // =====================================================================

    /// <summary>Normalised, already-resolved report filters.</summary>
    public class ReportsQueryDto
    {
        public ReportTimeframe Timeframe { get; set; } = ReportTimeframe.Week;

        /// <summary>Nominal start of the selected timeframe.</summary>
        public DateTime StartDate { get; set; }

        /// <summary>Nominal end of the selected timeframe.</summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// End actually queried. Clamped to the last day with data, so a
        /// mid-week report never books shifts that have not run yet.
        /// </summary>
        public DateTime EffectiveEndDate { get; set; }

        public bool IsClamped { get; set; }
        public DateTime AnchorDate { get; set; }
        public string RangeLabel { get; set; } = string.Empty;
        public string BucketLabel { get; set; } = string.Empty;

        public List<int> ProductIds { get; set; } = new();
        public List<ProductType> Lines { get; set; } = new();
        public List<int> RepIds { get; set; } = new();
        public string Route { get; set; } = string.Empty;

        public AnalyticsModule Module { get; set; } = AnalyticsModule.Operational;
        public ReportGroupBy GroupBy { get; set; } = ReportGroupBy.ProductLine;
        public bool ChartGroupByRep { get; set; }

        public string Sort { get; set; } = string.Empty;
        public string Direction { get; set; } = "desc";
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 12;
    }

    public enum ReportColumnKind
    {
        Text = 0,
        Quantity = 1,
        Money = 2,
        Percent = 3,
        Alert = 4
    }

    public class ReportColumnDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public ReportColumnKind Kind { get; set; }
        public bool IsSortable { get; set; } = true;
        public bool IsNumeric { get; set; }
    }

    public enum ReportAlertLevel
    {
        None = 0,
        Warning = 1,
        Error = 2
    }

    /// <summary>
    /// One reconciliation line. The row is intentionally wide: the same shape
    /// backs the operational, financial and logistics modules, and the visible
    /// columns are chosen by <see cref="ReportColumnCatalog"/>.
    /// </summary>
    public class ReportRowDto
    {
        public string RowKey { get; set; } = string.Empty;
        public string EntityLabel { get; set; } = string.Empty;
        public string EntitySubLabel { get; set; } = string.Empty;

        public int? RepId { get; set; }
        public string RepName { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string Terminal { get; set; } = string.Empty;

        public int? ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductLine { get; set; } = string.Empty;
        public string OutputUnit { get; set; } = string.Empty;

        public string VehicleRegistration { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string VehicleName { get; set; } = string.Empty;

        // Operational
        public int TicketCount { get; set; }
        public int OrdersPlaced { get; set; }
        public int Produced { get; set; }
        public int Loaded { get; set; }
        public int Sold { get; set; }
        public int Returned { get; set; }
        public int Damaged { get; set; }
        public decimal FillRatePercent { get; set; }
        public decimal SellThroughPercent { get; set; }
        public decimal DamagePercent { get; set; }

        // Financial
        public decimal ExpectedRevenue { get; set; }
        public decimal CollectedCash { get; set; }
        public decimal CollectedTransfer { get; set; }
        public decimal CollectedPosBank { get; set; }
        public decimal Collected => CollectedCash + CollectedTransfer + CollectedPosBank;
        public decimal OutstandingBalance { get; set; }
        public decimal CollectionRatePercent { get; set; }
        public decimal CashVarianceAmount { get; set; }
        public decimal CashVariancePercent { get; set; }

        // Logistics
        public int Trips { get; set; }
        public decimal DistanceKm { get; set; }
        public decimal Litres { get; set; }
        public decimal FuelCost { get; set; }
        public decimal MaintenanceCost { get; set; }
        public decimal TripExpenses { get; set; }
        public decimal TotalTripCost => FuelCost + MaintenanceCost + TripExpenses;
        public decimal OnTimePercent { get; set; }
        public decimal DeliveryEfficiencyIndex { get; set; }
        public decimal CostPerUnitSold { get; set; }

        public ReportAlertLevel AlertLevel { get; set; }
        public string AlertMessage { get; set; } = string.Empty;
    }

    // =====================================================================
    // Chart payloads
    // =====================================================================

    public class ChartSeriesDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public List<decimal> Data { get; set; } = new();
    }

    public class ChartPointDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string SecondaryLabel { get; set; } = string.Empty;
        public decimal SecondaryValue { get; set; }
    }

    public class LifecycleComparisonChartDto
    {
        public string GroupByLabel { get; set; } = string.Empty;
        public string EmptyMessage { get; set; } = string.Empty;
        public List<string> Categories { get; set; } = new();
        public List<ChartSeriesDto> Series { get; set; } = new();
    }

    public class RevenueCollectionChartDto
    {
        public string BucketLabel { get; set; } = string.Empty;
        public List<ChartPointDto> Points { get; set; } = new();
    }

    public class VolumeDistributionChartDto
    {
        public string EmptyMessage { get; set; } = string.Empty;
        public List<ChartPointDto> Slices { get; set; } = new();
    }

    public class RepPerformancePointDto
    {
        public int RepId { get; set; }
        public string RepName { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;

        /// <summary>Units the rep actually sold on the route.</summary>
        public int SoldUnits { get; set; }

        /// <summary>Share of the filtered volume, used to rank the horizontal bars.</summary>
        public decimal VolumeSharePercent { get; set; }

        public decimal ExpectedRevenue { get; set; }
        public decimal Collected { get; set; }
        public decimal OutstandingBalance { get; set; }
        public decimal VarianceAmount { get; set; }
        public decimal VariancePercent { get; set; }
    }

    public class RepPerformanceChartDto
    {
        public List<RepPerformancePointDto> Rows { get; set; } = new();
    }

    public class LogisticsPointDto
    {
        public string VehicleRegistration { get; set; } = string.Empty;
        public string VehicleName { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public int RepId { get; set; }
        public int SoldVolume { get; set; }
        public int Trips { get; set; }
        public decimal DistanceKm { get; set; }
        public decimal Litres { get; set; }
        public decimal FuelCost { get; set; }
        public decimal MaintenanceCost { get; set; }
        public decimal TripExpenses { get; set; }
        public decimal OnTimePercent { get; set; }
        public decimal DeliveryEfficiencyIndex { get; set; }
        public decimal CostPerUnitSold { get; set; }
    }

    public class LogisticsChartDto
    {
        public List<LogisticsPointDto> Rows { get; set; } = new();
    }

    public class ChartBundleDto
    {
        public LifecycleComparisonChartDto Lifecycle { get; set; } = new();
        public RevenueCollectionChartDto Revenue { get; set; } = new();
        public VolumeDistributionChartDto Volume { get; set; } = new();
        public RepPerformanceChartDto RepPerformance { get; set; } = new();
        public LogisticsChartDto Logistics { get; set; } = new();
    }

    // =====================================================================
    // Report result envelope
    // =====================================================================

    public class ReportResultDto
    {
        public ReportsQueryDto Query { get; set; } = new();
        public AnalyticsModule Module { get; set; }
        public string ModuleLabel { get; set; } = string.Empty;
        public string ModuleDescription { get; set; } = string.Empty;
        public string GroupByLabel { get; set; } = string.Empty;

        public List<ReportRowDto> Rows { get; set; } = new();

        /// <summary>
        /// Every filtered row before pagination. Used by the CSV export so a
        /// download is never silently truncated to the visible page.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public List<ReportRowDto> AllRows { get; set; } = new();

        public List<ReportColumnDto> Columns { get; set; } = new();
        public LifecycleTotalsDto GrandTotals { get; set; } = new();
        public TenderSplitDto Tender { get; set; } = new();
        public LogisticsChartDto LogisticsSummary { get; set; } = new();

        public int TotalRows { get; set; }
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 12;
        public int TotalPages { get; set; }

        public ChartBundleDto Charts { get; set; } = new();

        public List<ProductSkuDto> Catalog { get; set; } = new();
        public List<RepFilterOptionDto> Reps { get; set; } = new();
        public List<string> Routes { get; set; } = new();
        public List<string> ActiveFilterLabels { get; set; } = new();
    }

    // =====================================================================
    // Shared column metadata / formatting
    // =====================================================================

    /// <summary>
    /// Single source of truth for report column metadata, value formatting,
    /// sorting and CSV escaping so the table, the chart legends and the export
    /// can never drift apart.
    /// </summary>
    public static class ReportColumnCatalog
    {
        public static List<ReportColumnDto> ForModule(AnalyticsModule module) => module switch
        {
            AnalyticsModule.Financial => new List<ReportColumnDto>
            {
                new() { Key = nameof(ReportRowDto.EntityLabel), Label = "Sales Rep / Entity", Kind = ReportColumnKind.Text },
                new() { Key = nameof(ReportRowDto.Route), Label = "Route", Kind = ReportColumnKind.Text },
                new() { Key = nameof(ReportRowDto.Terminal), Label = "Terminal", Kind = ReportColumnKind.Text },
                new() { Key = nameof(ReportRowDto.ExpectedRevenue), Label = "Expected Revenue", Kind = ReportColumnKind.Money, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.CollectedCash), Label = "Cash Collected", Kind = ReportColumnKind.Money, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.CollectedTransfer), Label = "Transfer Collected", Kind = ReportColumnKind.Money, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.CollectedPosBank), Label = "POS Bank Collected", Kind = ReportColumnKind.Money, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.CollectionRatePercent), Label = "Collection Rate", Kind = ReportColumnKind.Percent, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.OutstandingBalance), Label = "Outstanding", Kind = ReportColumnKind.Money, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.CashVarianceAmount), Label = "Cash Variance", Kind = ReportColumnKind.Money, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.AlertMessage), Label = "Flag", Kind = ReportColumnKind.Alert }
            },
            AnalyticsModule.Logistics => new List<ReportColumnDto>
            {
                new() { Key = nameof(ReportRowDto.VehicleRegistration), Label = "Vehicle", Kind = ReportColumnKind.Text },
                new() { Key = nameof(ReportRowDto.VehicleName), Label = "Model", Kind = ReportColumnKind.Text },
                new() { Key = nameof(ReportRowDto.DriverName), Label = "Driver", Kind = ReportColumnKind.Text },
                new() { Key = nameof(ReportRowDto.Trips), Label = "Trips", Kind = ReportColumnKind.Quantity, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.DistanceKm), Label = "Distance (km)", Kind = ReportColumnKind.Quantity, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.Litres), Label = "Litres", Kind = ReportColumnKind.Quantity, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.FuelCost), Label = "Fuel Cost", Kind = ReportColumnKind.Money, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.MaintenanceCost), Label = "Maintenance", Kind = ReportColumnKind.Money, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.TripExpenses), Label = "Trip Expenses", Kind = ReportColumnKind.Money, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.OnTimePercent), Label = "On-Time", Kind = ReportColumnKind.Percent, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.DeliveryEfficiencyIndex), Label = "Efficiency Index", Kind = ReportColumnKind.Quantity, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.CostPerUnitSold), Label = "Cost / Unit Sold", Kind = ReportColumnKind.Money, IsNumeric = true }
            },
            _ => new List<ReportColumnDto>
            {
                new() { Key = nameof(ReportRowDto.EntityLabel), Label = "Entity", Kind = ReportColumnKind.Text },
                new() { Key = nameof(ReportRowDto.Route), Label = "Route", Kind = ReportColumnKind.Text },
                new() { Key = nameof(ReportRowDto.OrdersPlaced), Label = "Requested", Kind = ReportColumnKind.Quantity, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.Produced), Label = "Produced", Kind = ReportColumnKind.Quantity, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.Loaded), Label = "Loaded", Kind = ReportColumnKind.Quantity, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.Sold), Label = "Sold", Kind = ReportColumnKind.Quantity, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.Returned), Label = "Returned", Kind = ReportColumnKind.Quantity, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.Damaged), Label = "Damaged", Kind = ReportColumnKind.Quantity, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.FillRatePercent), Label = "Fill Rate", Kind = ReportColumnKind.Percent, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.SellThroughPercent), Label = "Sell-Through", Kind = ReportColumnKind.Percent, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.DamagePercent), Label = "Damage %", Kind = ReportColumnKind.Percent, IsNumeric = true },
                new() { Key = nameof(ReportRowDto.AlertMessage), Label = "Flag", Kind = ReportColumnKind.Alert }
            }
        };

        public static decimal NumericValue(ReportRowDto row, string key) => key switch
        {
            nameof(ReportRowDto.OrdersPlaced) => row.OrdersPlaced,
            nameof(ReportRowDto.Produced) => row.Produced,
            nameof(ReportRowDto.Loaded) => row.Loaded,
            nameof(ReportRowDto.Sold) => row.Sold,
            nameof(ReportRowDto.Returned) => row.Returned,
            nameof(ReportRowDto.Damaged) => row.Damaged,
            nameof(ReportRowDto.TicketCount) => row.TicketCount,
            nameof(ReportRowDto.FillRatePercent) => row.FillRatePercent,
            nameof(ReportRowDto.SellThroughPercent) => row.SellThroughPercent,
            nameof(ReportRowDto.DamagePercent) => row.DamagePercent,
            nameof(ReportRowDto.ExpectedRevenue) => row.ExpectedRevenue,
            nameof(ReportRowDto.CollectedCash) => row.CollectedCash,
            nameof(ReportRowDto.CollectedTransfer) => row.CollectedTransfer,
            nameof(ReportRowDto.CollectedPosBank) => row.CollectedPosBank,
            nameof(ReportRowDto.Collected) => row.Collected,
            nameof(ReportRowDto.OutstandingBalance) => row.OutstandingBalance,
            nameof(ReportRowDto.CollectionRatePercent) => row.CollectionRatePercent,
            nameof(ReportRowDto.CashVarianceAmount) => row.CashVarianceAmount,
            nameof(ReportRowDto.CashVariancePercent) => row.CashVariancePercent,
            nameof(ReportRowDto.Trips) => row.Trips,
            nameof(ReportRowDto.DistanceKm) => row.DistanceKm,
            nameof(ReportRowDto.Litres) => row.Litres,
            nameof(ReportRowDto.FuelCost) => row.FuelCost,
            nameof(ReportRowDto.MaintenanceCost) => row.MaintenanceCost,
            nameof(ReportRowDto.TripExpenses) => row.TripExpenses,
            nameof(ReportRowDto.OnTimePercent) => row.OnTimePercent,
            nameof(ReportRowDto.DeliveryEfficiencyIndex) => row.DeliveryEfficiencyIndex,
            nameof(ReportRowDto.CostPerUnitSold) => row.CostPerUnitSold,
            _ => 0m
        };

        public static string RawValue(ReportRowDto row, string key) => key switch
        {
            nameof(ReportRowDto.EntityLabel) => row.EntityLabel,
            nameof(ReportRowDto.Route) => row.Route,
            nameof(ReportRowDto.Terminal) => row.Terminal,
            nameof(ReportRowDto.VehicleRegistration) => row.VehicleRegistration,
            nameof(ReportRowDto.VehicleName) => row.VehicleName,
            nameof(ReportRowDto.DriverName) => row.DriverName,
            nameof(ReportRowDto.AlertMessage) => row.AlertMessage,
            nameof(ReportRowDto.Collected) => FormatMoney(row.Collected),
            _ => FormatValue(row, key, new ReportColumnDto { Key = key, Kind = InferKind(key) })
        };

        public static string FormatValue(ReportRowDto row, string key, ReportColumnDto column) => column.Kind switch
        {
            ReportColumnKind.Money => FormatMoney(NumericValue(row, key)),
            ReportColumnKind.Percent => FormatPercent(NumericValue(row, key)),
            ReportColumnKind.Quantity => FormatQuantity(NumericValue(row, key)),
            ReportColumnKind.Alert => row.AlertMessage,
            _ => RawText(row, key)
        };

        private static string RawText(ReportRowDto row, string key) => key switch
        {
            nameof(ReportRowDto.EntityLabel) => row.EntityLabel,
            nameof(ReportRowDto.Route) => row.Route,
            nameof(ReportRowDto.Terminal) => row.Terminal,
            nameof(ReportRowDto.VehicleRegistration) => row.VehicleRegistration,
            nameof(ReportRowDto.VehicleName) => row.VehicleName,
            nameof(ReportRowDto.DriverName) => row.DriverName,
            nameof(ReportRowDto.AlertMessage) => row.AlertMessage,
            _ => string.Empty
        };

        private static ReportColumnKind InferKind(string key) => key switch
        {
            nameof(ReportRowDto.FillRatePercent) or nameof(ReportRowDto.SellThroughPercent)
                or nameof(ReportRowDto.DamagePercent) or nameof(ReportRowDto.CollectionRatePercent)
                or nameof(ReportRowDto.OnTimePercent) or nameof(ReportRowDto.CashVariancePercent)
                => ReportColumnKind.Percent,
            nameof(ReportRowDto.ExpectedRevenue) or nameof(ReportRowDto.CollectedCash)
                or nameof(ReportRowDto.CollectedTransfer) or nameof(ReportRowDto.CollectedPosBank)
                or nameof(ReportRowDto.OutstandingBalance) or nameof(ReportRowDto.FuelCost)
                or nameof(ReportRowDto.MaintenanceCost) or nameof(ReportRowDto.TripExpenses)
                or nameof(ReportRowDto.CostPerUnitSold) or nameof(ReportRowDto.CashVarianceAmount)
                => ReportColumnKind.Money,
            nameof(ReportRowDto.AlertMessage) => ReportColumnKind.Alert,
            _ => ReportColumnKind.Quantity
        };

        public static string FormatMoney(decimal value) => "₦" + value.ToString("N0");
        public static string FormatPercent(decimal value) => value.ToString("0.#") + "%";
        public static string FormatQuantity(decimal value) =>
            value == decimal.Truncate(value) ? value.ToString("N0") : value.ToString("N2");

        public static IOrderedEnumerable<ReportRowDto> Sort(
            IEnumerable<ReportRowDto> rows, string key, string direction)
        {
            var desc = !string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase);
            var source = rows ?? Enumerable.Empty<ReportRowDto>();

            // Text keys sort ordinally, everything else sorts on its numeric value.
            if (IsTextKey(key))
            {
                return desc
                    ? source.OrderByDescending(r => RawTextValue(r, key), StringComparer.OrdinalIgnoreCase)
                    : source.OrderBy(r => RawTextValue(r, key), StringComparer.OrdinalIgnoreCase);
            }

            return desc
                ? source.OrderByDescending(r => NumericValue(r, key))
                : source.OrderBy(r => NumericValue(r, key));
        }

        private static bool IsTextKey(string key) => key is
            nameof(ReportRowDto.EntityLabel) or nameof(ReportRowDto.Route) or
            nameof(ReportRowDto.Terminal) or nameof(ReportRowDto.VehicleRegistration) or
            nameof(ReportRowDto.VehicleName) or nameof(ReportRowDto.DriverName);

        private static string RawTextValue(ReportRowDto row, string key) => key switch
        {
            nameof(ReportRowDto.EntityLabel) => row.EntityLabel,
            nameof(ReportRowDto.Route) => row.Route,
            nameof(ReportRowDto.Terminal) => row.Terminal,
            nameof(ReportRowDto.VehicleRegistration) => row.VehicleRegistration,
            nameof(ReportRowDto.VehicleName) => row.VehicleName,
            nameof(ReportRowDto.DriverName) => row.DriverName,
            _ => string.Empty
        };
    }
}
