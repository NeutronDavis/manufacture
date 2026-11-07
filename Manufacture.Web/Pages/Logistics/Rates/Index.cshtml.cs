using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Rates
{
    public class IndexModel : PageModel
    {
        private readonly IFleetService _fleetService;

        public IndexModel(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        public List<RouteRateDto> Rates { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? VehicleFilter { get; set; }

        public int TotalRoutesCount => Rates.Count;
        public decimal AvgRateWithFuel => Rates.Any() ? Rates.Average(r => r.RateWithFuel) : 0;
        public decimal AvgRateWithoutFuel => Rates.Any() ? Rates.Average(r => r.RateWithoutFuel) : 0;
        public int ActiveLanesCount => Rates.Count(r => r.IsActive);

        public List<string> AvailableVehicleTypes { get; set; } = new()
        {
            "30-Ton Haulage Truck",
            "15-Ton Box Truck",
            "Delivery Van",
            "VIP Escort Patrol"
        };

        public async Task OnGetAsync()
        {
            Rates = await _fleetService.GetRouteRatesAsync(Search, VehicleFilter);
        }

        public async Task<IActionResult> OnPostDeleteRateAsync(int id)
        {
            var success = await _fleetService.DeleteRouteRateAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Route rate deleted successfully."
                : "Route rate could not be found.";
            return RedirectToPage(new { Search, VehicleFilter });
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            var rate = await _fleetService.GetRouteRateAsync(id);
            if (rate != null)
            {
                await _fleetService.SetRouteRateActiveAsync(id, !rate.IsActive);
                TempData["SuccessMessage"] = $"Route {rate.OriginState} → {rate.DestinationState} status set to {(!rate.IsActive ? "Active" : "Inactive")}.";
            }
            return RedirectToPage(new { Search, VehicleFilter });
        }
    }
}
