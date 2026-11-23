using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class Driver
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? LicenseNumber { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CharterBooking> CharterBookings { get; set; } = new List<CharterBooking>();

    public virtual ICollection<FuelLog> FuelLogs { get; set; } = new List<FuelLog>();

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
