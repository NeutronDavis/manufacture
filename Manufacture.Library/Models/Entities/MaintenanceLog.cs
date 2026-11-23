using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class MaintenanceLog
{
    public int Id { get; set; }

    public int VehicleId { get; set; }

    public DateOnly ServiceDate { get; set; }

    public string Description { get; set; } = null!;

    public decimal Cost { get; set; }

    public string WorkshopOrVendor { get; set; } = null!;

    public string InvoiceNumber { get; set; } = null!;

    public string? BeforeImageUrl { get; set; }

    public string? AfterImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Vehicle Vehicle { get; set; } = null!;
}
