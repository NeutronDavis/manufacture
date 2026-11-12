using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class SalesRep
{
    public int Id { get; set; }

    public string RepCode { get; set; } = null!;

    public int? EmployeeId { get; set; }

    public int? UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string RouteName { get; set; } = null!;

    public int? PosTerminalId { get; set; }

    public int? VehicleId { get; set; }

    public string ProductFocus { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<CustomerReturn> CustomerReturns { get; set; } = new List<CustomerReturn>();

    public virtual ICollection<DailyRepRouteSheet> DailyRepRouteSheets { get; set; } = new List<DailyRepRouteSheet>();

    public virtual Employee? Employee { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual PosTerminal? PosTerminal { get; set; }

    public virtual ICollection<SupermarketAccount> SupermarketAccounts { get; set; } = new List<SupermarketAccount>();

    public virtual User? User { get; set; }

    public virtual Vehicle? Vehicle { get; set; }
}
