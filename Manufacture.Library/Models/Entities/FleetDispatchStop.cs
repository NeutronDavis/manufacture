using System;

namespace Manufacture.Models.Entities;

public partial class FleetDispatchStop
{
    public int Id { get; set; }

    public int FleetDispatchId { get; set; }

    public int StopNumber { get; set; }

    public string DeliveryPointName { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string CityState { get; set; } = null!;

    public string? ContactName { get; set; }

    public string? ContactPhone { get; set; }

    public int Status { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual FleetDispatch FleetDispatch { get; set; } = null!;
}
