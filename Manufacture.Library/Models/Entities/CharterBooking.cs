using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class CharterBooking
{
    public int Id { get; set; }

    public string BookingRef { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string CustomerPhone { get; set; } = null!;

    public int VehicleId { get; set; }

    public int? DriverEmployeeId { get; set; }

    public string DriverName { get; set; } = null!;

    public decimal DriverAllowance { get; set; }

    public decimal SecurityEscortFee { get; set; }

    public decimal TollsAndRoadLevies { get; set; }

    public decimal? TotalTripIncidentals { get; set; }

    public decimal GrossCharterFee { get; set; }

    public decimal? NetCharterRevenue { get; set; }

    public int TripStatus { get; set; }

    public DateOnly DepartureDate { get; set; }

    public DateOnly? ReturnDate { get; set; }

    public string Notes { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Employee? DriverEmployee { get; set; }

    public virtual ICollection<TripLeg> TripLegs { get; set; } = new List<TripLeg>();

    public virtual Vehicle Vehicle { get; set; } = null!;
}
