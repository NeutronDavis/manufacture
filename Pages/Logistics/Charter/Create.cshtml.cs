using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Manufacture.Services;
using System.Text.Json;

namespace Manufacture.Pages.Logistics.Charter
{
    public class CreateModel : PageModel
    {
        private readonly MockLogisticsService _logisticsService;

        public CreateModel(MockLogisticsService logisticsService)
        {
            _logisticsService = logisticsService;
        }

        [BindProperty]
        public CharterBookingDto Booking { get; set; } = new()
        {
            DepartureDate = DateTime.UtcNow.Date.AddDays(1),
            Legs = new List<TripLegDto>
            {
                new TripLegDto
                {
                    LegNumber = 1,
                    OriginState = "Lagos",
                    DestinationState = "Rivers (Port Harcourt)",
                    FuelOption = FuelOption.WithFuel,
                    LegCost = 2100000
                }
            },
            DriverAllowance = 75000,
            SecurityEscortFee = 150000,
            TollsAndRoadLevies = 45000,
            GrossCharterFee = 2100000
        };

        public List<Vehicle> AvailableVehicles { get; set; } = new();
        public List<RouteRate> ActiveRouteRates { get; set; } = new();
        public string RouteRatesJson { get; set; } = "[]";

        public List<string> NigerianStates { get; set; } = new()
        {
            "Lagos", "Abuja FCT", "Rivers (Port Harcourt)", "Kano", "Edo (Benin City)",
            "Anambra (Onitsha)", "Delta (Warri)", "Oyo (Ibadan)", "Kaduna", "Ogun (Abeokuta)",
            "Enugu", "Ondo (Akure)", "Imo (Owerri)", "Cross River (Calabar)", "Plateau (Jos)",
            "Akwa Ibom (Uyo)", "Abia (Aba)", "Kwara (Ilorin)", "Niger (Minna)", "Benue (Makurdi)"
        };

        public void OnGet()
        {
            LoadFormData();
        }

        private void LoadFormData()
        {
            AvailableVehicles = _logisticsService.GetAllVehicles();
            ActiveRouteRates = _logisticsService.GetAllRouteRates().Where(r => r.IsActive).ToList();

            var ratesData = ActiveRouteRates.Select(r => new
            {
                r.OriginState,
                r.DestinationState,
                r.VehicleTypeRequired,
                r.RateWithFuel,
                r.RateWithoutFuel,
                r.EstimatedHours
            });

            RouteRatesJson = JsonSerializer.Serialize(ratesData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            // Set default vehicle if none selected
            if (Booking.VehicleId == 0 && AvailableVehicles.Any())
            {
                var defaultVehicle = AvailableVehicles.FirstOrDefault(v => v.Type == "Truck") ?? AvailableVehicles.First();
                Booking.VehicleId = defaultVehicle.Id;
                Booking.VehicleRegNumber = defaultVehicle.RegistrationNumber;
                Booking.DriverId = defaultVehicle.Id;
                Booking.DriverName = defaultVehicle.AssignedDriverName;
            }
        }

        public IActionResult OnPost()
        {
            // Clean up any empty legs
            Booking.Legs = Booking.Legs?.Where(l => !string.IsNullOrWhiteSpace(l.DestinationState)).ToList() ?? new List<TripLegDto>();

            if (!Booking.Legs.Any())
            {
                ModelState.AddModelError("Booking.Legs", "At least one trip leg with an origin and destination is required.");
            }

            if (!ModelState.IsValid)
            {
                LoadFormData();
                return Page();
            }

            var vehicle = _logisticsService.GetVehicleById(Booking.VehicleId);
            if (vehicle != null)
            {
                Booking.VehicleRegNumber = vehicle.RegistrationNumber;
                Booking.DriverName = vehicle.AssignedDriverName;
            }

            // Recalculate legs costs against current route matrix
            decimal gross = 0;
            for (int i = 0; i < Booking.Legs.Count; i++)
            {
                var leg = Booking.Legs[i];
                leg.LegNumber = i + 1;
                var rate = _logisticsService.FindRate(leg.OriginState, leg.DestinationState);
                if (rate != null)
                {
                    leg.LegCost = leg.FuelOption == FuelOption.WithFuel ? rate.RateWithFuel : rate.RateWithoutFuel;
                }
                gross += leg.LegCost;
            }

            Booking.GrossCharterFee = gross > 0 ? gross : Booking.GrossCharterFee;
            var (incidentals, netRev) = _logisticsService.CalculateTripProfitability(Booking);
            Booking.TotalTripIncidentals = incidentals;
            Booking.NetCharterRevenue = netRev;

            var created = _logisticsService.CreateCharterBooking(Booking);

            TempData["SuccessMessage"] = $"Commercial charter booking {created.BookingRef} booked successfully!";
            return RedirectToPage("/Logistics/Charter/Details", new { id = created.Id });
        }
    }
}
