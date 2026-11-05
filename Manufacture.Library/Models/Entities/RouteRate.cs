using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class RouteRate
{
    public int Id { get; set; }

    public string OriginState { get; set; } = null!;

    public string DestinationState { get; set; } = null!;

    public string VehicleTypeRequired { get; set; } = null!;

    public decimal RateWithFuel { get; set; }

    public decimal RateWithoutFuel { get; set; }

    public int EstimatedHours { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
