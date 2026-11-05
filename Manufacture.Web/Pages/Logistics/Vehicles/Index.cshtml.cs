using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Logistics.Vehicles
{
    public class IndexModel : PageModel
    {
        private readonly MockLogisticsService _logisticsService;

        public IndexModel(MockLogisticsService logisticsService)
        {
            _logisticsService = logisticsService;
        }

        public List<Vehicle> Vehicles { get; set; } = new();

        public void OnGet()
        {
            Vehicles = _logisticsService.GetAllVehicles();
        }
    }
}
