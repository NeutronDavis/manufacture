using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Manufacture.Services;

namespace Manufacture.Pages.Logistics.Maintenance
{
    public class CreateModel : PageModel
    {
        private readonly MockLogisticsService _logisticsService;

        public CreateModel(MockLogisticsService logisticsService)
        {
            _logisticsService = logisticsService;
        }

        [BindProperty]
        public MaintenanceLogDto MaintenanceInput { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public List<Vehicle> AllVehicles { get; set; } = new();

        public void OnGet(int? vehicleId, string? returnUrl)
        {
            ReturnUrl = returnUrl;
            AllVehicles = _logisticsService.GetAllVehicles();

            if (vehicleId.HasValue && vehicleId.Value > 0)
            {
                MaintenanceInput.VehicleId = vehicleId.Value;
            }
            else if (AllVehicles.Any())
            {
                MaintenanceInput.VehicleId = AllVehicles.First().Id;
            }

            MaintenanceInput.ServiceDate = DateTime.UtcNow;
            MaintenanceInput.Cost = 45000;
            MaintenanceInput.Description = "Scheduled Engine Oil & Filter Service";
            MaintenanceInput.WorkshopOrVendor = "Mandilas Motors Lagos";
        }

        public IActionResult OnPost()
        {
            if (MaintenanceInput.VehicleId <= 0 ||
                MaintenanceInput.Cost <= 0 ||
                string.IsNullOrWhiteSpace(MaintenanceInput.Description))
            {
                ModelState.AddModelError("", "Please fill in all required maintenance fields (Vehicle, Service Description, and Valid Cost).");
                AllVehicles = _logisticsService.GetAllVehicles();
                return Page();
            }

            var created = _logisticsService.CreateMaintenanceLog(MaintenanceInput);
            TempData["SuccessMessage"] = $"Maintenance service cost of ₦{created.Cost:N0} recorded for {created.VehicleRegNumber}.";

            if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToPage("/Logistics/WeeklyRunningCosts/Index", new { WeekDate = created.ServiceDate.ToString("yyyy-MM-dd") });
        }
    }
}
