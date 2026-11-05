using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class TripLeg
{
    public int Id { get; set; }

    public int CharterBookingId { get; set; }

    public int LegNumber { get; set; }

    public string OriginState { get; set; } = null!;

    public string DestinationState { get; set; } = null!;

    public int FuelOption { get; set; }

    public decimal LegCost { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual CharterBooking CharterBooking { get; set; } = null!;
}
