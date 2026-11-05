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

    public int? PreferredSupplierId { get; set; }

    public DateOnly LastRestockedDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Supplier? PreferredSupplier { get; set; }

    public virtual ICollection<StoreRequestItem> StoreRequestItems { get; set; } = new List<StoreRequestItem>();
}
