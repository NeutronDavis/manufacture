using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Maintenance
{
    public class CreateModel : PageModel
    {
        private readonly IFleetService _fleetService;

        public CreateModel(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        [BindProperty]
        public MaintenanceLogDto MaintenanceInput { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public List<VehicleDto> AllVehicles { get; set; } = new();

        public async Task OnGetAsync(int? vehicleId, string? returnUrl)
        {
            ReturnUrl = returnUrl;
            AllVehicles = await _fleetService.GetVehiclesAsync();

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

        public async Task<IActionResult> OnPostAsync()
        {
            AllVehicles = await _fleetService.GetVehiclesAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var created = await _fleetService.CreateMaintenanceLogAsync(MaintenanceInput);
                TempData["SuccessMessage"] =
                    $"Maintenance service cost of ₦{created.Cost:N0} recorded for {created.VehicleRegNumber}.";

                if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                {
                    return Redirect(ReturnUrl);
                }

                return RedirectToPage("/Logistics/WeeklyRunningCosts/Index", new { WeekDate = created.ServiceDate.ToString("yyyy-MM-dd") });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }
}
