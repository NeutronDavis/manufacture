using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Logistics.FuelLogs
{
    public class IndexModel : PageModel
    {
        private readonly MockLogisticsService _logisticsService;

        public IndexModel(MockLogisticsService logisticsService)
        {
            _logisticsService = logisticsService;
        }

        public List<FuelLog> FuelLogs { get; set; } = new();

        public void OnGet()
        {
            FuelLogs = _logisticsService.GetAllFuelLogs();
        }
    }
}
