using System;

namespace Manufacture.Models.Entities;

public partial class RouteSheetSupermarketDebt
{
    public int Id { get; set; }

    public int RouteSheetId { get; set; }

    public int SupermarketAccountId { get; set; }

    public bool IsSettled { get; set; }

    public decimal DebtAmount { get; set; }

    public decimal AppliedCredit { get; set; }

    public string RecordedBy { get; set; } = null!;

    public DateTime RecordedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual DailyRepRouteSheet RouteSheet { get; set; } = null!;

    public virtual SupermarketAccount SupermarketAccount { get; set; } = null!;
}
