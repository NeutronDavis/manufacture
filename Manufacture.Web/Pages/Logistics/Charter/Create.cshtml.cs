using System.Text.Json;
using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Charter
{
    public class CreateModel : PageModel
    {
        private readonly IFleetService _fleetService;

        public CreateModel(IFleetService fleetService)
        {
            _fleetService = fleetService;
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
                    FuelOption = FuelOption.WithFuel
                }
            },
            DriverAllowance = 75000,
            SecurityEscortFee = 150000,
            TollsAndRoadLevies = 45000
        };

        public List<VehicleDto> AvailableVehicles { get; set; } = new();
        public List<FleetDriverDto> AvailableDrivers { get; set; } = new();
        public List<RouteRateDto> ActiveRouteRates { get; set; } = new();
        public string RouteRatesJson { get; set; } = "[]";
        public string InitialLegsJson { get; set; } = "[]";

        public async Task OnGetAsync()
        {
            await LoadFormDataAsync();
        }

        private async Task LoadFormDataAsync()
        {
            AvailableVehicles = await _fleetService.GetVehiclesAsync();
            AvailableDrivers = await _fleetService.GetDriversAsync();
            ActiveRouteRates = await _fleetService.GetActiveRouteRatesAsync();

            RouteRatesJson = JsonSerializer.Serialize(
                ActiveRouteRates.Select(r => new
                {
                    r.Id,
                    r.OriginState,
                    r.DestinationState,
                    r.VehicleTypeRequired,
                    r.RateWithFuel,
                    r.RateWithoutFuel,
                    r.EstimatedHours
                }),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            Booking.Legs ??= new List<TripLegDto>();
            if (!Booking.Legs.Any())
            {
                var fallbackRate = ActiveRouteRates.FirstOrDefault(r =>
                    r.OriginState == "Lagos" && r.DestinationState.Contains("Port Harcourt"))
                    ?? ActiveRouteRates.FirstOrDefault();

                Booking.Legs.Add(new TripLegDto
                {
                    LegNumber = 1,
                    OriginState = fallbackRate?.OriginState ?? "Lagos",
                    DestinationState = fallbackRate?.DestinationState ?? string.Empty,
                    FuelOption = FuelOption.WithFuel,
                    LegCost = fallbackRate?.RateWithFuel ?? 0
                });
            }

            for (var i = 0; i < Booking.Legs.Count; i++)
            {
                var leg = Booking.Legs[i];
                leg.LegNumber = i + 1;
                var rate = ActiveRouteRates.FirstOrDefault(r =>
                    r.OriginState == leg.OriginState && r.DestinationState == leg.DestinationState);
                if (rate != null)
                {
                    leg.LegCost = leg.FuelOption == FuelOption.WithFuel ? rate.RateWithFuel : rate.RateWithoutFuel;
                }
            }

            Booking.GrossCharterFee = Booking.Legs.Sum(l => l.LegCost);

            InitialLegsJson = JsonSerializer.Serialize(
                Booking.Legs.Select(l => new
                {
                    l.LegNumber,
                    l.OriginState,
                    l.DestinationState,
                    FuelOption = l.FuelOption.ToString(),
                    l.LegCost
                }),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            if (Booking.VehicleId == 0 && AvailableVehicles.Any())
            {
                var defaultVehicle = AvailableVehicles.FirstOrDefault(v => v.Type == "Truck") ?? AvailableVehicles.First();
                Booking.VehicleId = defaultVehicle.Id;
                Booking.VehicleRegNumber = defaultVehicle.RegistrationNumber;
            }

            if (!Booking.DriverId.HasValue && AvailableDrivers.Any())
            {
                Booking.DriverId = AvailableDrivers.First().Id;
                Booking.DriverName = AvailableDrivers.First().FullName;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Booking.Legs = Booking.Legs
                ?.Where(l => !string.IsNullOrWhiteSpace(l.DestinationState))
                .ToList() ?? new List<TripLegDto>();

            if (!Booking.Legs.Any())
            {
                ModelState.AddModelError("Booking.Legs", "At least one trip leg with an origin and destination is required.");
            }

            if (!ModelState.IsValid)
            {
                await LoadFormDataAsync();
                return Page();
            }

            try
            {
                var created = await _fleetService.CreateCharterBookingAsync(Booking);
                TempData["SuccessMessage"] = $"Charter booking {created.BookingRef} booked successfully.";
                return RedirectToPage("/Logistics/Charter/Details", new { id = created.Id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadFormDataAsync();
                return Page();
            }
        }
    }
}
