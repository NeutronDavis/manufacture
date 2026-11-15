using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class MeasurementDensity
{
    public int Id { get; set; }

    public string MaterialName { get; set; } = null!;

    public decimal DensityKgPerLitre { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
