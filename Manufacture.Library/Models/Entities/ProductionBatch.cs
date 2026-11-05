using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class ProductionBatch
{
    public int Id { get; set; }

    public string BatchNumber { get; set; } = null!;

    public int RecipeId { get; set; }

    public string RecipeName { get; set; } = null!;

    public int TargetQuantity { get; set; }

    public int ActualQuantity { get; set; }

    public int? Variance { get; set; }

    public string Shift { get; set; } = null!;

    public string BakerInCharge { get; set; } = null!;

    public decimal FlourBagsUsed { get; set; }

    public int ProductCategory { get; set; }

    public decimal BatchInputQuantity { get; set; }

    public string BatchInputUnitLabel { get; set; } = null!;

    public string OutputUnitLabel { get; set; } = null!;

    public int TargetOrderQuantity { get; set; }

    public int? LinkedOrderId { get; set; }

    public string? LinkedOrderNumber { get; set; }

    public string? CustomerName { get; set; }

    public int Status { get; set; }

    public DateTime ProductionDate { get; set; }

    public string Notes { get; set; } = null!;

    public decimal TotalBatchCost { get; set; }

    public decimal CalculatedUnitCost { get; set; }

    public decimal ExpectedUnitCost { get; set; }

    public string LoggedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Order? LinkedOrder { get; set; }

    public virtual ICollection<ProductionDefect> ProductionDefects { get; set; } = new List<ProductionDefect>();

    public virtual Recipe Recipe { get; set; } = null!;
}
