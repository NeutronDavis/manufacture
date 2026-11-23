using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.WeeklyRunningCosts
{
    public class IndexModel : PageModel
    {
        private readonly IFleetService _fleetService;

        public IndexModel(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        [BindProperty(SupportsGet = true)]
        public string? WeekDate { get; set; }

        public DateTime SelectedDate { get; set; } = DateTime.UtcNow;
        public DateTime OperatingWeekStart { get; set; }
        public DateTime OperatingWeekEnd { get; set; }

        public List<WeeklyFleetExpenseDto> FleetExpenses { get; set; } = new();
        public List<VehicleDto> AllVehicles { get; set; } = new();

        public decimal TotalWeeklyFuel => FleetExpenses.Sum(e => e.FuelExpenseTotal);
        public decimal TotalWeeklyMaintenance => FleetExpenses.Sum(e => e.MaintenanceExpenseTotal);
        public decimal TotalWeeklyIncidentals => FleetExpenses.Sum(e => e.IncidentalsTotal);
        public decimal GrandTotalWeeklyCost => FleetExpenses.Sum(e => e.TotalWeeklyRunningCost);

        public async Task OnGetAsync()
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

            OperatingWeekStart = GetOperatingWeekStart(SelectedDate);
            OperatingWeekEnd = OperatingWeekStart.AddDays(6);

            FleetExpenses = await _fleetService.GetWeeklyFleetExpensesAsync(OperatingWeekStart, OperatingWeekEnd);
            AllVehicles = await _fleetService.GetVehiclesAsync();
        }

        private static DateTime GetOperatingWeekStart(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Sunday)) % 7;
            return date.Date.AddDays(-diff);
        }
    }
}
