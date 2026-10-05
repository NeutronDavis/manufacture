namespace Manufacture.Models.Entities
{
    /// <summary>
    /// Rolling windows offered by the reports engine. "Week" always follows the
    /// plant operating calendar: Sunday through Saturday.
    /// </summary>
    public enum ReportTimeframe
    {
        Daily = 0,
        Week = 1,
        Monthly = 2,
        Yearly = 3,
        Custom = 4
    }

    /// <summary>
    /// The three departmental reconciliation modules surfaced on /reports.
    /// </summary>
    public enum AnalyticsModule
    {
        /// <summary>Requested vs produced vs loaded vs sold vs returned vs damaged.</summary>
        Operational = 0,

        /// <summary>Expected revenue vs cash/transfer/POS collected vs outstanding balances.</summary>
        Financial = 1,

        /// <summary>Fuel, maintenance, trip expenses and delivery efficiency.</summary>
        Logistics = 2
    }

    /// <summary>
    /// Granularity used to slice the reconciliation table and the lifecycle chart.
    /// </summary>
    public enum ReportGroupBy
    {
        ProductLine = 0,
        Sku = 1,
        SalesRep = 2
    }

    /// <summary>
    /// The six stages of the daily operational lifecycle, in execution order.
    /// </summary>
    public enum LifecycleMetric
    {
        OrdersPlaced = 0,
        Produced = 1,
        Loaded = 2,
        Sold = 3,
        Returned = 4,
        Damaged = 5
    }
}
