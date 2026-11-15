using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class FleetDispatch
{
    public int Id { get; set; }

    public string DispatchRef { get; set; } = null!;

    public DateOnly DispatchDate { get; set; }

    public int VehicleId { get; set; }

    public int? DriverEmployeeId { get; set; }

    public string DriverName { get; set; } = null!;

    public int Purpose { get; set; }

    public int Status { get; set; }

    public decimal? OdometerStart { get; set; }

    public decimal? OdometerEnd { get; set; }

    public string Notes { get; set; } = null!;

    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual User? CreatedByUser { get; set; }

    public virtual Employee? DriverEmployee { get; set; }

    public virtual ICollection<FleetDispatchStop> FleetDispatchStops { get; set; } = new List<FleetDispatchStop>();

    public virtual ICollection<FuelLog> FuelLogs { get; set; } = new List<FuelLog>();

    public virtual Vehicle Vehicle { get; set; } = null!;
}
