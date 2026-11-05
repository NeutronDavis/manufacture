using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Manufacture.Services;

namespace Manufacture.Pages.Logistics.WeeklyRunningCosts
{
    public class IndexModel : PageModel
    {
        private readonly MockLogisticsService _logisticsService;

        public IndexModel(MockLogisticsService logisticsService)
        {
            _logisticsService = logisticsService;
        }

        [BindProperty(SupportsGet = true)]
        public string? WeekDate { get; set; }

        public DateTime SelectedDate { get; set; } = DateTime.UtcNow;
        public DateTime OperatingWeekStart { get; set; }
        public DateTime OperatingWeekEnd { get; set; }

        public List<WeeklyFleetExpense> FleetExpenses { get; set; } = new();
        public List<Vehicle> AllVehicles { get; set; } = new();

        public decimal TotalWeeklyFuel => FleetExpenses.Sum(e => e.FuelExpenseTotal);
        public decimal TotalWeeklyMaintenance => FleetExpenses.Sum(e => e.MaintenanceExpenseTotal);
        public decimal TotalWeeklyIncidentals => FleetExpenses.Sum(e => e.IncidentalsTotal);
        public decimal GrandTotalWeeklyCost => FleetExpenses.Sum(e => e.TotalWeeklyRunningCost);

        public void OnGet()
        {
            if (!string.IsNullOrWhiteSpace(WeekDate) && DateTime.TryParse(WeekDate, out var parsed))
            {
                SelectedDate = parsed;
            }
            else
            {
                SelectedDate = DateTime.UtcNow;
                WeekDate = SelectedDate.ToString("yyyy-MM-dd");
            }

            LoadWeekData();
        }

        private void LoadWeekData()
        {
            OperatingWeekStart = MockLogisticsService.GetOperatingWeekStart(SelectedDate);
            OperatingWeekEnd = OperatingWeekStart.AddDays(6);

            FleetExpenses = _logisticsService.GetWeeklyFleetExpenses(SelectedDate);
            AllVehicles = _logisticsService.GetAllVehicles();
        }
    }
}
