using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Vehicles
{
    public class DetailsModel : PageModel
    {
        private readonly IFleetService _fleetService;

        public DetailsModel(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        public VehicleDto Vehicle { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var vehicle = await _fleetService.GetVehicleAsync(id);
            if (vehicle == null)
            {
                TempData["ErrorMessage"] = "Vehicle not found.";
                return RedirectToPage("/Logistics/Vehicles/Index");
            }

            Vehicle = vehicle;
            return Page();
        }
    }
}
