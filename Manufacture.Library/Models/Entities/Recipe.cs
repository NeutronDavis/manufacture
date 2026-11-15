using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class Recipe
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int? ProductionProductTypeId { get; set; }

    public decimal ExpectedYield { get; set; }

    public string OutputUnit { get; set; } = null!;

    public decimal SellingPrice { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<CustomerReturn> CustomerReturns { get; set; } = new List<CustomerReturn>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ProductionBatch> ProductionBatches { get; set; } = new List<ProductionBatch>();

    public virtual ProductionProductType? ProductionProductType { get; set; }

    public virtual ICollection<ProductionDefect> ProductionDefects { get; set; } = new List<ProductionDefect>();

    public virtual ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

    public virtual ICollection<RouteSheetItem> RouteSheetItems { get; set; } = new List<RouteSheetItem>();

    public virtual ICollection<SalesBonusRule> SalesBonusRules { get; set; } = new List<SalesBonusRule>();

    public virtual ICollection<SupermarketAccountPrice> SupermarketAccountPrices { get; set; } = new List<SupermarketAccountPrice>();
}
