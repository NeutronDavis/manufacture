using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Vehicles
{
    public class IndexModel : PageModel
    {
        private readonly IFleetService _fleetService;

        public IndexModel(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        public List<VehicleDto> Vehicles { get; set; } = new();

        public async Task OnGetAsync()
        {
            Vehicles = await _fleetService.GetVehiclesAsync();
        }
    }
}
