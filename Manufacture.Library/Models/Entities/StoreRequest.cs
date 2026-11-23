using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class StoreRequest
{
    public int Id { get; set; }

    public string RequestNumber { get; set; } = null!;

    public string RequestedBy { get; set; } = null!;

    public string Department { get; set; } = null!;

    public string Purpose { get; set; } = null!;

    public int Status { get; set; }

    public DateTime RequestedAt { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ICollection<StoreRequestItem> StoreRequestItems { get; set; } = new List<StoreRequestItem>();
}
