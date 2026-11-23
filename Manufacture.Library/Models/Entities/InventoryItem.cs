using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class InventoryItem
{
    public int Id { get; set; }

    public string ItemCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public decimal QuantityInStock { get; set; }

    public decimal ReorderThreshold { get; set; }

    public decimal UnitCostPrice { get; set; }

    public decimal PackageSize { get; set; } = 1m;

    public string PackageUnitOfMeasure { get; set; } = "kg";

    public decimal? DensityKgPerLitre { get; set; }

    public int? MeasurementDensityId { get; set; }

    public int? PreferredSupplierId { get; set; }

    public DateOnly LastRestockedDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Supplier? PreferredSupplier { get; set; }

    public virtual MeasurementDensity? MeasurementDensity { get; set; }

    public virtual ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ICollection<StoreRequestItem> StoreRequestItems { get; set; } = new List<StoreRequestItem>();
}
