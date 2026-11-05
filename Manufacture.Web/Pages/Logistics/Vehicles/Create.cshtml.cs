using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Logistics.Vehicles
{
    public class CreateModel : PageModel
    {
        private readonly MockLogisticsService _logisticsService;
        private readonly MockEmployeeService _employeeService;

        public CreateModel(MockLogisticsService logisticsService, MockEmployeeService employeeService)
        {
            _logisticsService = logisticsService;
            _employeeService = employeeService;
        }

        [BindProperty]
        public VehicleDto VehicleInput { get; set; } = new();

        public List<Employee> FleetDrivers { get; set; } = new();

        public void OnGet()
        {
            LoadFleetDrivers();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                LoadFleetDrivers();
                return Page();
            }

            var v = _logisticsService.CreateVehicle(VehicleInput);
            TempData["SuccessMessage"] = $"Vehicle '{v.RegistrationNumber}' added to fleet.";
            return RedirectToPage("/Logistics/Vehicles/Index");
        }

        private void LoadFleetDrivers()
        {
            FleetDrivers = _employeeService.GetFleetDrivers();
        }
    }
}
