using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class Vehicle
{
    public int Id { get; set; }

    public string RegistrationNumber { get; set; } = null!;

    public string MakeAndModel { get; set; } = null!;

    public string VehicleType { get; set; } = null!;

    public string VehicleFunction { get; set; } = null!;

    public int? AssignedDriverId { get; set; }

    public string FuelType { get; set; } = null!;

    public decimal TankCapacityLitres { get; set; }

    public decimal CurrentOdometerKm { get; set; }

    public string Status { get; set; } = null!;

    public DateOnly? LastServiceDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Driver? AssignedDriver { get; set; }

    public virtual ICollection<CharterBooking> CharterBookings { get; set; } = new List<CharterBooking>();

    public virtual ICollection<DailyRepRouteSheet> DailyRepRouteSheets { get; set; } = new List<DailyRepRouteSheet>();

    public virtual ICollection<FuelLog> FuelLogs { get; set; } = new List<FuelLog>();

    public virtual ICollection<MaintenanceLog> MaintenanceLogs { get; set; } = new List<MaintenanceLog>();

    public virtual ICollection<SalesRep> SalesReps { get; set; } = new List<SalesRep>();

    public virtual ICollection<WeeklyFleetExpense> WeeklyFleetExpenses { get; set; } = new List<WeeklyFleetExpense>();
}
