using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Vehicles
{
    public class CreateModel : PageModel
    {
        private readonly IFleetService _fleetService;

        public CreateModel(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        [BindProperty]
        public VehicleDto VehicleInput { get; set; } = new();

        public List<EmployeeDto> FleetDrivers { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadFleetDriversAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadFleetDriversAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var vehicle = await _fleetService.CreateVehicleAsync(VehicleInput);
                TempData["SuccessMessage"] = $"Vehicle '{vehicle.RegistrationNumber}' added to fleet.";
                return RedirectToPage("/Logistics/Vehicles/Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }

        private async Task LoadFleetDriversAsync()
        {
            FleetDrivers = await _fleetService.GetLogisticsDriversAsync();
        }
    }
}
