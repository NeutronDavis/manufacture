using System.ComponentModel.DataAnnotations;
using Manufacture.Models.Entities;

namespace Manufacture.Models.DTOs
{
    public class RouteRateDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Origin state is required.")]
        public string OriginState { get; set; } = "Lagos";

        [Required(ErrorMessage = "Destination state is required.")]
        public string DestinationState { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vehicle type is required.")]
        public string VehicleTypeRequired { get; set; } = "30-Ton Haulage Truck";

        [Range(1000, 100_000_000, ErrorMessage = "Rate with fuel must be greater than zero.")]
        public decimal RateWithFuel { get; set; }

        [Range(1000, 100_000_000, ErrorMessage = "Rate without fuel must be greater than zero.")]
        public decimal RateWithoutFuel { get; set; }

        [Range(1, 168, ErrorMessage = "Estimated transit hours must be between 1 and 168.")]
        public int EstimatedHours { get; set; } = 12;

        public bool IsActive { get; set; } = true;
    }

    public class TripLegDto
    {
        public int LegNumber { get; set; } = 1;
        public string OriginState { get; set; } = string.Empty;
        public string DestinationState { get; set; } = string.Empty;
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

        [Range(1, int.MaxValue, ErrorMessage = "Please select a driver.")]
        public int DriverId { get; set; }
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
