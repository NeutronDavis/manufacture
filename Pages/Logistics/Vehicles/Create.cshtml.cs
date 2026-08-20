using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;

namespace Manufacture.Pages.Logistics.Vehicles
{
    public class CreateModel : PageModel
    {
        private readonly MockLogisticsService _logisticsService;

        public CreateModel(MockLogisticsService logisticsService)
        {
            _logisticsService = logisticsService;
        }

        [BindProperty]
        public VehicleDto VehicleInput { get; set; } = new();

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();

            var v = _logisticsService.CreateVehicle(VehicleInput);
            TempData["SuccessMessage"] = $"Vehicle '{v.RegistrationNumber}' added to fleet.";
            return RedirectToPage("/Logistics/Vehicles/Index");
        }
    }
}
