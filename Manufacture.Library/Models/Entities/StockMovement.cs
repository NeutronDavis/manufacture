using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class StockMovement
{
    public int Id { get; set; }

    public int InventoryItemId { get; set; }

    public string MovementType { get; set; } = null!;

    public decimal Quantity { get; set; }

    public decimal BalanceAfter { get; set; }

    public int? StoreRequestId { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }

    public string PerformedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual InventoryItem InventoryItem { get; set; } = null!;

    public virtual StoreRequest? StoreRequest { get; set; }
}
