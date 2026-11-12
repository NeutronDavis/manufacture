using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Maintenance
{
    public class IndexModel : PageModel
    {
        private readonly IFleetService _fleetService;

        public IndexModel(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        public List<MaintenanceLogDto> Logs { get; set; } = new();

        public async Task OnGetAsync()
        {
            Logs = await _fleetService.GetMaintenanceLogsAsync();
        }
    }
}
