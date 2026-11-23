using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class CharterQuoteRequest
{
    public int Id { get; set; }

    public string QuoteRef { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string CustomerPhone { get; set; } = null!;

    public string? CustomerEmail { get; set; }

    public DateTime RequestDate { get; set; }

    public int TripType { get; set; }

    public string VehicleTypeRequired { get; set; } = null!;

    public int? VehicleId { get; set; }

    public decimal TotalDistanceKm { get; set; }

    public decimal QuotedAmount { get; set; }

    public decimal DriverAllowance { get; set; }

    public decimal SecurityEscortFee { get; set; }

    public decimal TollsAndRoadLevies { get; set; }

    public decimal? TotalTripIncidentals { get; set; }

    public decimal? NetEstimate { get; set; }

    public int Status { get; set; }

    public DateOnly? ValidUntil { get; set; }

    public string Notes { get; set; } = null!;

    public int? RequestedByUserId { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public int? ApprovedByUserId { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual User? ApprovedByUser { get; set; }

    public virtual ICollection<CharterBooking> CharterBookings { get; set; } = new List<CharterBooking>();

    public virtual ICollection<CharterQuoteLeg> CharterQuoteLegs { get; set; } = new List<CharterQuoteLeg>();

    public virtual User? RequestedByUser { get; set; }

    public virtual Vehicle? Vehicle { get; set; }
}
