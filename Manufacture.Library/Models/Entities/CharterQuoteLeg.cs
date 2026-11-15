using System;

namespace Manufacture.Models.Entities;

public partial class CharterQuoteLeg
{
    public int Id { get; set; }

    public int CharterQuoteRequestId { get; set; }

    public int LegNumber { get; set; }

    public string OriginAddress { get; set; } = null!;

    public string OriginCityState { get; set; } = null!;

    public string DestinationAddress { get; set; } = null!;

    public string DestinationCityState { get; set; } = null!;

    public decimal DistanceKm { get; set; }

    public int FuelOption { get; set; }

    public decimal QuotedLegAmount { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CharterQuoteRequest CharterQuoteRequest { get; set; } = null!;
}
