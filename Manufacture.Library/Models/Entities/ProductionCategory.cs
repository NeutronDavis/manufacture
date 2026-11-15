using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class ProductionCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<ProductionProductType> ProductTypes { get; set; } = new List<ProductionProductType>();
}
