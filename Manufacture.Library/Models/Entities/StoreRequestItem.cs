using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class StoreRequestItem
{
    public int Id { get; set; }

    public int StoreRequestId { get; set; }

    public int InventoryItemId { get; set; }

    public string ItemName { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public decimal RequestedQuantity { get; set; }

    public decimal IssuedQuantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual InventoryItem InventoryItem { get; set; } = null!;

    public virtual StoreRequest StoreRequest { get; set; } = null!;
}
