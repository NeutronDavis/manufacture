using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class Ingredient
{
    public int Id { get; set; }

    public string? ItemCode { get; set; }

    public string Name { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public decimal UnitCost { get; set; }

    public decimal CurrentStock { get; set; }

    public string Category { get; set; } = null!;

    public string? PackageUnit { get; set; }

    public decimal PackageCostPrice { get; set; }

    public decimal PackageStockOnHand { get; set; }

    public int? SupplierId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

    public virtual Supplier? Supplier { get; set; }
}
