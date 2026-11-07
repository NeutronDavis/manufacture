using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Rates
{
    public class EditModel : PageModel
    {
        private readonly IFleetService _fleetService;

        public EditModel(IFleetService fleetService)
        {
            _fleetService = fleetService;
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

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var rate = await _fleetService.GetRouteRateAsync(id);
            if (rate == null)
            {
                TempData["ErrorMessage"] = $"Route rate #{id} was not found.";
                return RedirectToPage("/Logistics/Rates/Index");
            }

            Id = rate.Id;
            RateInput = rate;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            RateInput.Id = id;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var updated = await _fleetService.UpdateRouteRateAsync(RateInput);
            TempData[updated ? "SuccessMessage" : "ErrorMessage"] = updated
                ? $"Route rate for {RateInput.OriginState} → {RateInput.DestinationState} updated successfully."
                : "Failed to update route rate: record not found.";

            return RedirectToPage("/Logistics/Rates/Index");
        }
    }
}
