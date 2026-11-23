using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class IngredientPriceChangeRequest
{
    public int Id { get; set; }

    public int InventoryItemId { get; set; }

    public string InventoryItemName { get; set; } = null!;

    public string RequestNumber { get; set; } = null!;

    public decimal PreviousUnitCost { get; set; }

    public decimal RequestedUnitCost { get; set; }

    public int Status { get; set; }

    public int? RequestedByUserId { get; set; }

    public string RequestedByName { get; set; } = null!;

    public DateTime RequestedAt { get; set; }

    public int? ReviewedByUserId { get; set; }

    public string? ReviewedByName { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string ReviewNotes { get; set; } = null!;

    public DateTime? AppliedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual InventoryItem InventoryItem { get; set; } = null!;

    public virtual User? RequestedByUser { get; set; }

    public virtual User? ReviewedByUser { get; set; }
}
