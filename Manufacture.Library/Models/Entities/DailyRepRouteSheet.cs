using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class DailyRepRouteSheet
{
    public int Id { get; set; }

    public string RouteSheetRef { get; set; } = null!;

    public DateOnly RouteDate { get; set; }

    public int SalesRepId { get; set; }

    public string RouteName { get; set; } = null!;

    public int? VehicleId { get; set; }

    public string? VehicleRegistration { get; set; }

    public int? PosTerminalId { get; set; }

    public string? PosTerminalCode { get; set; }

    public decimal PreviousAccountBalance { get; set; }

    public decimal TotalExpectedSalesRevenue { get; set; }

    public decimal CashCollected { get; set; }

    public decimal BankTransferCollected { get; set; }

    public decimal PosTerminalCollected { get; set; }

    public decimal? TotalPaymentsCollectedToday { get; set; }

    public decimal? NewOutstandingAccountBalance { get; set; }

    public int Status { get; set; }

    public string? SignedOffByDispatcher { get; set; }

    public DateTime? SignedOffAt { get; set; }

    public string? ReconciledBy { get; set; }

    public DateTime? ReconciledAt { get; set; }

    public string Notes { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual PosTerminal? PosTerminal { get; set; }

    public virtual ICollection<RouteSheetItem> RouteSheetItems { get; set; } = new List<RouteSheetItem>();

    public virtual SalesRep SalesRep { get; set; } = null!;

    public virtual Vehicle? Vehicle { get; set; }
}
