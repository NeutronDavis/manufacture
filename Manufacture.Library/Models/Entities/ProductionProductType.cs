using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class ProductionProductType
{
    public int Id { get; set; }

    public int ProductionCategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string Baseline { get; set; } = null!;

    public string OutputUnitNoun { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ProductionCategory ProductionCategory { get; set; } = null!;

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

    public virtual ICollection<ProductionBatch> ProductionBatches { get; set; } = new List<ProductionBatch>();
}
