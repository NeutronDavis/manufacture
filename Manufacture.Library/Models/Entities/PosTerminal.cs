using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class PosTerminal
{
    public int Id { get; set; }

    public string TerminalCode { get; set; } = null!;

    public string TerminalName { get; set; } = null!;

    public int? AssignedEmployeeId { get; set; }

    public int? AssignedUserId { get; set; }

    public string CashAccountName { get; set; } = null!;

    public string TransferAccountName { get; set; } = null!;

    public string PosBankAccountName { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Employee? AssignedEmployee { get; set; }

    public virtual User? AssignedUser { get; set; }

    public virtual ICollection<DailyRepRouteSheet> DailyRepRouteSheets { get; set; } = new List<DailyRepRouteSheet>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<SalesRep> SalesReps { get; set; } = new List<SalesRep>();
}
