namespace Manufacture.Models.Entities
{
    public enum FuelOption
    {
        WithFuel,
        WithoutFuel
    }

    public enum CharterTripStatus
    {
        Booked,
        InTransit,
        Completed,
        Cancelled
    }

    public class RouteRate
    {
        public int Id { get; set; }
        public string OriginState { get; set; } = string.Empty; // e.g. "Lagos"
        public string DestinationState { get; set; } = string.Empty; // e.g. "Rivers (Port Harcourt)"
        public string VehicleTypeRequired { get; set; } = "30-Ton Haulage Truck"; // 30-Ton Haulage Truck, 15-Ton Box Truck, Delivery Van, VIP Escort
        public decimal RateWithFuel { get; set; } // Standard rate including company fuel
        public decimal RateWithoutFuel { get; set; } // Discounted freight-only rate when client fuels
        public int EstimatedHours { get; set; } // Estimated transit duration in hours
        public bool IsActive { get; set; } = true;
    }

    public class TripLeg
    {
        public int Id { get; set; }
        public int CharterBookingId { get; set; }
        public int LegNumber { get; set; } = 1;
        public string OriginState { get; set; } = string.Empty;
        public string DestinationState { get; set; } = string.Empty;
        public FuelOption FuelOption { get; set; } = FuelOption.WithFuel;
        public decimal LegCost { get; set; }
    }

    public class CharterBooking
    {
        public int Id { get; set; }
        public string BookingRef { get; set; } = string.Empty; // e.g. LOG-20261005-01
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public string VehicleRegNumber { get; set; } = string.Empty;
        public int DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;

        // Route & Multi-Leg Layover Details
        public bool IsMultiLeg => Legs.Count > 1;
        public List<TripLeg> Legs { get; set; } = new();

        // Trip Incidentals
        public decimal DriverAllowance { get; set; } // Daily/trip driver stipend
        public decimal SecurityEscortFee { get; set; } // Security/police escort costs
        public decimal TollsAndRoadLevies { get; set; } // Checkpoints, LGA levies, union dues, toll fees

        // Computed Financials
        public decimal GrossCharterFee { get; set; } // SUM(LegCost)
        public decimal TotalTripIncidentals => DriverAllowance + SecurityEscortFee + TollsAndRoadLevies;
        public decimal NetCharterRevenue => GrossCharterFee - TotalTripIncidentals;

        public CharterTripStatus TripStatus { get; set; } = CharterTripStatus.Booked;
        public DateTime DepartureDate { get; set; } = DateTime.UtcNow.Date;
        public DateTime? ReturnDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Notes { get; set; } = string.Empty;
    }

    public class MaintenanceLog
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string VehicleRegNumber { get; set; } = string.Empty;
        public DateTime ServiceDate { get; set; } = DateTime.UtcNow.Date;
        public string Description { get; set; } = string.Empty; // e.g. "Engine oil change, oil filter, rear brake pads"
        public decimal Cost { get; set; }
        public string WorkshopOrVendor { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
    }

    public class WeeklyFleetExpense
    {
        public int Id { get; set; }
        public DateTime OperatingWeekStart { get; set; } // Sunday
        public DateTime OperatingWeekEnd { get; set; } // Saturday
        public int VehicleId { get; set; }
        public string VehicleRegNumber { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public decimal FuelExpenseTotal { get; set; }
        public decimal MaintenanceExpenseTotal { get; set; }
        public decimal IncidentalsTotal { get; set; }
        public decimal TotalWeeklyRunningCost => FuelExpenseTotal + MaintenanceExpenseTotal + IncidentalsTotal;
    }
}
