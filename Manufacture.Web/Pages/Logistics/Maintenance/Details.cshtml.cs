using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Maintenance
{
    public class DetailsModel : PageModel
    {
        private readonly IFleetService _fleetService;

        public DetailsModel(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        public MaintenanceLogDto Log { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var log = await _fleetService.GetMaintenanceLogAsync(id);
            if (log == null)
            {
                TempData["ErrorMessage"] = "Maintenance log not found.";
                return RedirectToPage("/Logistics/Maintenance/Index");
            }

            Log = log;
            return Page();
        }
    }
}
