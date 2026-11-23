using System;

namespace Manufacture.Models.Entities;

public partial class RouteSheetSupermarket
{
    public int Id { get; set; }

    public int RouteSheetId { get; set; }

    public int SupermarketAccountId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual DailyRepRouteSheet RouteSheet { get; set; } = null!;

    public virtual SupermarketAccount SupermarketAccount { get; set; } = null!;
}
