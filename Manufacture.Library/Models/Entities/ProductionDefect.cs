using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class ProductionDefect
{
    public int Id { get; set; }

    public int ProductionBatchId { get; set; }

    public int RecipeId { get; set; }

    public int Quantity { get; set; }

    public string DefectReason { get; set; } = null!;

    public DateTime RecordedAt { get; set; }

    public string RecordedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ProductionBatch ProductionBatch { get; set; } = null!;

    public virtual Recipe Recipe { get; set; } = null!;
}
