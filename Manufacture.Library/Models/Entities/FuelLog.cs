using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class FuelLog
{
    public int Id { get; set; }

    public int VehicleId { get; set; }

    public int? EmployeeId { get; set; }

    public DateTime FuelDate { get; set; }

    public string FuelType { get; set; } = null!;

    public decimal LitresDispensed { get; set; }

    public decimal PricePerLitre { get; set; }

    public decimal? TotalCost { get; set; }

    public string PetrolStation { get; set; } = null!;

    public string RouteDescription { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual Vehicle Vehicle { get; set; } = null!;
}
