using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Logistics.FuelLogs
{
    public class CreateModel : PageModel
    {
        private readonly MockLogisticsService _logisticsService;

        public CreateModel(MockLogisticsService logisticsService)
        {
            _logisticsService = logisticsService;
        }

        [BindProperty]
        public FuelLogDto LogInput { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public List<Vehicle> Vehicles { get; set; } = new();

        public void OnGet(int? vehicleId, string? returnUrl)
        {
            ReturnUrl = returnUrl;
            Vehicles = _logisticsService.GetAllVehicles();
            if (vehicleId.HasValue) LogInput.VehicleId = vehicleId.Value;
            LogInput.LitresDispensed = 45;
            LogInput.PricePerLitre = 980;
            LogInput.PetrolStation = "NNPC Mega Station";
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                Vehicles = _logisticsService.GetAllVehicles();
                return Page();
            }

            var log = _logisticsService.CreateFuelLog(LogInput);
            TempData["SuccessMessage"] = $"Fuel expense of ₦{log.TotalCost:N0} ({log.LitresDispensed}L) logged for vehicle {log.VehicleRegNumber}.";

            if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToPage("/Logistics/FuelLogs/Index");
        }
    }
}
