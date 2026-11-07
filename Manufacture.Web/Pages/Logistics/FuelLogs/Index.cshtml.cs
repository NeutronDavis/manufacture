using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.FuelLogs
{
    public class IndexModel : PageModel
    {
        private readonly IFleetService _fleetService;

        public IndexModel(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        public List<FuelLogDto> FuelLogs { get; set; } = new();

        public async Task OnGetAsync()
        {
            FuelLogs = await _fleetService.GetFuelLogsAsync();
        }
    }
}
