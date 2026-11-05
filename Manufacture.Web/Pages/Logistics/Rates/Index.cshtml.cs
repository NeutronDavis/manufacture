using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Manufacture.Services;

namespace Manufacture.Pages.Logistics.Rates
{
    public class IndexModel : PageModel
    {
        private readonly MockLogisticsService _logisticsService;

        public IndexModel(MockLogisticsService logisticsService)
        {
            _logisticsService = logisticsService;
        }

        public List<RouteRate> Rates { get; set; } = new();

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

        public List<string> NigerianStates { get; set; } = new()
        {
            "Lagos", "Abuja FCT", "Rivers (Port Harcourt)", "Kano", "Edo (Benin City)",
            "Anambra (Onitsha)", "Delta (Warri)", "Oyo (Ibadan)", "Kaduna", "Ogun (Abeokuta)",
            "Enugu", "Ondo (Akure)", "Imo (Owerri)", "Cross River (Calabar)", "Plateau (Jos)",
            "Akwa Ibom (Uyo)", "Abia (Aba)", "Kwara (Ilorin)", "Niger (Minna)", "Benue (Makurdi)"
        };

        public void OnGet()
        {
            LoadRates();
        }

        private void LoadRates()
        {
            var allRates = _logisticsService.GetAllRouteRates();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim().ToLowerInvariant();
                allRates = allRates.Where(r =>
                    r.OriginState.ToLowerInvariant().Contains(term) ||
                    r.DestinationState.ToLowerInvariant().Contains(term) ||
                    r.VehicleTypeRequired.ToLowerInvariant().Contains(term)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(VehicleFilter))
            {
                allRates = allRates.Where(r => r.VehicleTypeRequired.Equals(VehicleFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            Rates = allRates;
        }


        public IActionResult OnPostDeleteRate(int id)
        {
            var success = _logisticsService.DeleteRouteRate(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Route rate deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Route rate could not be found.";
            }
            return RedirectToPage();
        }

        public IActionResult OnPostToggleStatus(int id)
        {
            var rate = _logisticsService.GetRouteRateById(id);
            if (rate != null)
            {
                rate.IsActive = !rate.IsActive;
                TempData["SuccessMessage"] = $"Route {rate.OriginState} → {rate.DestinationState} status set to {(rate.IsActive ? "Active" : "Inactive")}.";
            }
            return RedirectToPage();
        }
    }
}
