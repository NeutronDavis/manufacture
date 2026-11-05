using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class RouteSheetItem
{
    public int Id { get; set; }

    public int RouteSheetId { get; set; }

    public int RecipeId { get; set; }

    public string ProductCode { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public int Category { get; set; }

    public decimal UnitSellingPrice { get; set; }

    public int RepOrderRequestedQty { get; set; }

    public int YesterdayClosingStock { get; set; }

    public int TodayNewlyLoadedQty { get; set; }

    public int? TotalActiveInventory { get; set; }

    public int QtySold { get; set; }

    public int ClosingStockQty { get; set; }

    public int QtyDamaged { get; set; }

    public decimal? ExpectedItemRevenue { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Recipe Recipe { get; set; } = null!;

    public virtual DailyRepRouteSheet RouteSheet { get; set; } = null!;
}
