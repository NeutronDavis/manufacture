using System;

namespace Manufacture.Models.Entities;

public partial class SalesReconciliationItem
{
    public int Id { get; set; }

    public int SalesReconciliationId { get; set; }

    public int RecipeId { get; set; }

    public string ProductName { get; set; } = null!;

    public string ProductCode { get; set; } = null!;

    public decimal UnitPrice { get; set; }

    public int OpeningStock { get; set; }

    public int QtySold { get; set; }

    public int QtyDamaged { get; set; }

    public int QtyReplacement { get; set; }

    public int? ClosingStock { get; set; }

    public decimal? ExpectedRevenue { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Recipe Recipe { get; set; } = null!;

    public virtual SalesReconciliation SalesReconciliation { get; set; } = null!;
}
