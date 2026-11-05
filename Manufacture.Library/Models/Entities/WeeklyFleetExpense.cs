using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class WeeklyFleetExpense
{
    public int Id { get; set; }

    public DateOnly OperatingWeekStart { get; set; }

    public DateOnly OperatingWeekEnd { get; set; }

    public int VehicleId { get; set; }

    public string DriverName { get; set; } = null!;

    public decimal FuelExpenseTotal { get; set; }

    public decimal MaintenanceExpenseTotal { get; set; }

    public decimal IncidentalsTotal { get; set; }

    public decimal? TotalWeeklyRunningCost { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Vehicle Vehicle { get; set; } = null!;
}
