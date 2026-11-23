using System.ComponentModel.DataAnnotations;
using Manufacture.Models.Domain;

namespace Manufacture.Models.DTOs
{
    public enum CharterQuoteStatus
    {
        Draft = 0,
        PendingApproval = 1,
        Approved = 2,
        Rejected = 3,
        Expired = 4,
        Converted = 5
    }

    public enum CharterQuoteTripType
    {
        OneWay = 0,
        Return = 1,
        MultiStop = 2
    }

    public class CharterQuoteLegDto
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Leg number must be greater than zero.")]
        public int LegNumber { get; set; } = 1;

        [Required(ErrorMessage = "Origin address is required.")]
        [StringLength(300)]
        public string OriginAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Origin city/state is required.")]
        [StringLength(120)]
        public string OriginCityState { get; set; } = string.Empty;

        [Required(ErrorMessage = "Destination address is required.")]
        [StringLength(300)]
        public string DestinationAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Destination city/state is required.")]
        [StringLength(120)]
        public string DestinationCityState { get; set; } = string.Empty;

        public decimal? DistanceKm { get; set; }

        public FuelOption FuelOption { get; set; } = FuelOption.WithFuel;

        [Range(0, 100_000_000, ErrorMessage = "Quoted leg amount cannot be negative.")]
        public decimal QuotedLegAmount { get; set; }

        [StringLength(300)]
        public string? Notes { get; set; }
    }

    public class CharterQuoteDto
    {
        public int Id { get; set; }
        public string QuoteRef { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer name is required.")]
        [StringLength(150, MinimumLength = 2)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer phone number is required.")]
        [StringLength(30)]
        public string CustomerPhone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(200)]
        public string? CustomerEmail { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public CharterQuoteTripType TripType { get; set; } = CharterQuoteTripType.OneWay;

        [Required(ErrorMessage = "Vehicle class is required.")]
        [StringLength(60)]
        public string VehicleTypeRequired { get; set; } = "30-Ton Haulage Truck";

        public int? VehicleId { get; set; }
        public string VehicleRegNumber { get; set; } = string.Empty;

        public decimal TotalDistanceKm { get; set; }
        public decimal QuotedAmount { get; set; }

        [Range(0, 10_000_000, ErrorMessage = "Driver allowance cannot be negative.")]
        public decimal DriverAllowance { get; set; }

        [Range(0, 10_000_000, ErrorMessage = "Security escort fee cannot be negative.")]
        public decimal SecurityEscortFee { get; set; }

        [Range(0, 10_000_000, ErrorMessage = "Road levies and tolls cannot be negative.")]
        public decimal TollsAndRoadLevies { get; set; }

        public decimal TotalTripIncidentals { get; set; }
        public decimal NetEstimate { get; set; }

        public CharterQuoteStatus Status { get; set; } = CharterQuoteStatus.Draft;
        public DateTime? ValidUntil { get; set; }

        [Required(ErrorMessage = "Quote notes / client requirements are required.")]
        [StringLength(1000)]
        public string Notes { get; set; } = string.Empty;

        public int? RequestedByUserId { get; set; }
        public string RequestedByName { get; set; } = string.Empty;
        public DateTime? SubmittedAt { get; set; }
        public int? ApprovedByUserId { get; set; }
        public string ApprovedByName { get; set; } = string.Empty;
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? BookingId { get; set; }
        public string BookingRef { get; set; } = string.Empty;

        public List<CharterQuoteLegDto> Legs { get; set; } = new();

        public bool IsEditable => Status is CharterQuoteStatus.Draft or CharterQuoteStatus.Rejected;
        public bool CanSubmit => IsEditable;
        public bool IsApproved => Status == CharterQuoteStatus.Approved;
        public bool IsExpired => ValidUntil.HasValue && ValidUntil.Value.Date < DateTime.UtcNow.Date
            && Status is CharterQuoteStatus.PendingApproval or CharterQuoteStatus.Approved;
    }

    public enum FleetDispatchStatus
    {
        Planned = 0,
        Loading = 1,
        InTransit = 2,
        Delivered = 3,
        Completed = 4,
        Cancelled = 5
    }

    public enum FleetDispatchStopStatus
    {
        Pending = 0,
        Delivered = 1,
        Failed = 2
    }

    public class FleetDispatchStopDto
    {
        public int Id { get; set; }
        public int FleetDispatchId { get; set; }
        public int StopNumber { get; set; }

        [Required(ErrorMessage = "Delivery point name is required.")]
        [StringLength(150)]
        public string DeliveryPointName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Delivery address is required.")]
        [StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "City/state is required.")]
        [StringLength(120)]
        public string CityState { get; set; } = string.Empty;

        [StringLength(150)]
        public string? ContactName { get; set; }

        [StringLength(30)]
        public string? ContactPhone { get; set; }

        public FleetDispatchStopStatus Status { get; set; } = FleetDispatchStopStatus.Pending;
        public DateTime? DeliveredAt { get; set; }
        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class FleetDispatchDto
    {
        public int Id { get; set; }
        public string DispatchRef { get; set; } = string.Empty;

        [Required]
        public DateTime DispatchDate { get; set; } = DateTime.UtcNow.Date;

        [Range(1, int.MaxValue, ErrorMessage = "Select a vehicle.")]
        public int VehicleId { get; set; }
        public string VehicleRegNumber { get; set; } = string.Empty;
        public int? DriverEmployeeId { get; set; }
        public string DriverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the purpose of this dispatch.")]
        [StringLength(200)]
        public string Purpose { get; set; } = string.Empty;

        public FleetDispatchStatus Status { get; set; } = FleetDispatchStatus.Planned;
        public decimal? OdometerEnd { get; set; }
        [StringLength(1000)]
        public string Notes { get; set; } = string.Empty;
        public int? CreatedByUserId { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public List<FleetDispatchStopDto> Stops { get; set; } = new();
        public List<FuelLogDto> FuelLogs { get; set; } = new();
        public decimal TotalFuelCost => FuelLogs.Sum(log => log.TotalCost);
        public int DeliveredStopCount => Stops.Count(stop => stop.Status == FleetDispatchStopStatus.Delivered);
        public int FailedStopCount => Stops.Count(stop => stop.Status == FleetDispatchStopStatus.Failed);
        public int PendingStopCount => Stops.Count(stop => stop.Status == FleetDispatchStopStatus.Pending);
    }

    public class FleetDriverDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
    }

    public class TripLegDto
    {
        public int LegNumber { get; set; } = 1;
        public string OriginState { get; set; } = string.Empty;
        public string? OriginAddress { get; set; }
        public string DestinationState { get; set; } = string.Empty;
        public string? DestinationAddress { get; set; }
        public decimal? DistanceKm { get; set; }
        public FuelOption FuelOption { get; set; } = FuelOption.WithFuel;
        public decimal LegCost { get; set; }
    }

    public class CharterBookingDto
    {
        public int Id { get; set; }
        public string BookingRef { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer name is required.")]
        [StringLength(120, MinimumLength = 2, ErrorMessage = "Customer name must be at least 2 characters.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer phone number is required.")]
        public string CustomerPhone { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Please select a vehicle.")]
        public int VehicleId { get; set; }
        public string VehicleRegNumber { get; set; } = string.Empty;

        public int? QuoteRequestId { get; set; }
        public string QuoteRef { get; set; } = string.Empty;
        public decimal TotalDistanceKm { get; set; }

        public int? DriverEmployeeId { get; set; }
        public string DriverName { get; set; } = string.Empty;

        public bool IsMultiLeg { get; set; }
        public List<TripLegDto> Legs { get; set; } = new();

        [Range(0, 10_000_000, ErrorMessage = "Driver allowance cannot be negative.")]
        public decimal DriverAllowance { get; set; }

        [Range(0, 10_000_000, ErrorMessage = "Security escort fee cannot be negative.")]
        public decimal SecurityEscortFee { get; set; }

        [Range(0, 10_000_000, ErrorMessage = "Road levies and tolls cannot be negative.")]
        public decimal TollsAndRoadLevies { get; set; }

        public decimal GrossCharterFee { get; set; }
        public decimal TotalTripIncidentals { get; set; }
        public decimal NetCharterRevenue { get; set; }

        public CharterTripStatus TripStatus { get; set; } = CharterTripStatus.Booked;

        [Required(ErrorMessage = "Departure date is required.")]
        public DateTime DepartureDate { get; set; } = DateTime.UtcNow.Date;
        public DateTime? ReturnDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class MaintenanceLogDto
    {
        public int Id { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Please select a vehicle.")]
        public int VehicleId { get; set; }
        public string VehicleRegNumber { get; set; } = string.Empty;
        public DateTime ServiceDate { get; set; } = DateTime.UtcNow.Date;
        [Required]
        public string Description { get; set; } = string.Empty;
        [Range(1, 100_000_000, ErrorMessage = "Cost must be greater than zero.")]
        public decimal Cost { get; set; }
        public string WorkshopOrVendor { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string? BeforeImageUrl { get; set; }
        public string? AfterImageUrl { get; set; }
    }

    public class WeeklyFleetExpenseDto
    {
        public int Id { get; set; }
        public DateTime OperatingWeekStart { get; set; }
        public DateTime OperatingWeekEnd { get; set; }
        public int VehicleId { get; set; }
        public string VehicleRegNumber { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public decimal FuelExpenseTotal { get; set; }
        public decimal MaintenanceExpenseTotal { get; set; }
        public decimal IncidentalsTotal { get; set; }
        public decimal TotalWeeklyRunningCost { get; set; }
    }
}
