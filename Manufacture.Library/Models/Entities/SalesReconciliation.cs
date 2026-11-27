using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class SalesReconciliation
{
    public int Id { get; set; }

    public string ReconciliationNumber { get; set; } = null!;

    public DateOnly ReconciliationDate { get; set; }

    public int SalesRepId { get; set; }

    public int? PosTerminalId { get; set; }

    public int? ProductRepositoryId { get; set; }

    public string RouteName { get; set; } = null!;

    public string VehicleRegistration { get; set; } = null!;

    public decimal CashHandedOver { get; set; }

    public decimal PosTerminalReceipts { get; set; }

    public decimal? TotalCollected { get; set; }

    public decimal TotalExpectedRevenue { get; set; }

    public decimal? Variance { get; set; }

    public int Status { get; set; }

    public DateTime? StockPostedAt { get; set; }

    public string Notes { get; set; } = null!;

    public string? ReconciledBy { get; set; }

    public DateTime? ReconciledAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual PosTerminal? PosTerminal { get; set; }

    public virtual ProductRepository? ProductRepository { get; set; }

    public virtual SalesRep SalesRep { get; set; } = null!;

    public virtual ICollection<SalesReconciliationItem> SalesReconciliationItems { get; set; } = new List<SalesReconciliationItem>();
}
