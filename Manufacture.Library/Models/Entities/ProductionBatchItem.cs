using System;

namespace Manufacture.Models.Entities;

public partial class ProductionBatchItem
{
    public int Id { get; set; }

    public int ProductionBatchId { get; set; }

    public int RecipeId { get; set; }

    public string RecipeName { get; set; } = null!;

    public string ProductCode { get; set; } = null!;

    public int TargetQuantity { get; set; }

    public int ActualQuantity { get; set; }

    public int? Variance { get; set; }

    public decimal BatchInputQuantity { get; set; }

    public string BatchInputUnitLabel { get; set; } = null!;

    public string OutputUnitLabel { get; set; } = null!;

    public decimal TotalBatchCost { get; set; }

    public decimal ExpectedUnitCost { get; set; }

    public decimal CalculatedUnitCost { get; set; }

    public DateTime? ActualRecordedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ProductionBatch ProductionBatch { get; set; } = null!;

    public virtual Recipe Recipe { get; set; } = null!;
}
