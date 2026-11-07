using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Rates
{
    public class CreateModel : PageModel
    {
        private readonly IFleetService _fleetService;

        public CreateModel(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        [BindProperty]
        public RouteRateDto RateInput { get; set; } = new();

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
            RateInput.OriginState = "Lagos";
            RateInput.VehicleTypeRequired = "30-Ton Haulage Truck";
            RateInput.RateWithFuel = 1850000;
            RateInput.RateWithoutFuel = 1150000;
            RateInput.EstimatedHours = 14;
            RateInput.IsActive = true;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var created = await _fleetService.CreateRouteRateAsync(RateInput);
                TempData["SuccessMessage"] = $"New route rate for {created.OriginState} → {created.DestinationState} added successfully.";
                return RedirectToPage("/Logistics/Rates/Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }
}
