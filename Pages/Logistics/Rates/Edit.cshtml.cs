using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.DTOs;
using Manufacture.Services;

namespace Manufacture.Pages.Logistics.Rates
{
    public class EditModel : PageModel
    {
        private readonly MockLogisticsService _logisticsService;

        public EditModel(MockLogisticsService logisticsService)
        {
            _logisticsService = logisticsService;
        }

        [BindProperty]
        public RouteRateDto RateInput { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

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

        public IActionResult OnGet(int id)
        {
            var rate = _logisticsService.GetRouteRateById(id);
            if (rate == null)
            {
                TempData["ErrorMessage"] = $"Route rate #{id} was not found.";
                return RedirectToPage("/Logistics/Rates/Index");
            }

            Id = rate.Id;
            RateInput = new RouteRateDto
            {
                Id = rate.Id,
                OriginState = rate.OriginState,
                DestinationState = rate.DestinationState,
                VehicleTypeRequired = rate.VehicleTypeRequired,
                RateWithFuel = rate.RateWithFuel,
                RateWithoutFuel = rate.RateWithoutFuel,
                EstimatedHours = rate.EstimatedHours,
                IsActive = rate.IsActive
            };

            return Page();
        }

        public IActionResult OnPost(int id)
        {
            if (string.IsNullOrWhiteSpace(RateInput.OriginState) ||
                string.IsNullOrWhiteSpace(RateInput.DestinationState) ||
                string.IsNullOrWhiteSpace(RateInput.VehicleTypeRequired) ||
                RateInput.RateWithFuel <= 0 ||
                RateInput.RateWithoutFuel <= 0)
            {
                ModelState.AddModelError("", "Please fill in all required route rate fields with valid amounts.");
                return Page();
            }

            var updated = _logisticsService.UpdateRouteRate(id, RateInput);
            if (updated == null)
            {
                TempData["ErrorMessage"] = "Failed to update route rate: record not found.";
                return RedirectToPage("/Logistics/Rates/Index");
            }

            TempData["SuccessMessage"] = $"Route rate for {updated.OriginState} → {updated.DestinationState} updated successfully.";
            return RedirectToPage("/Logistics/Rates/Index");
        }
    }
}
