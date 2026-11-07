using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics
{
    public class IndexModel : PageModel
    {
        private readonly IFleetService _fleetService;

        public IndexModel(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        public List<VehicleDto> Vehicles { get; set; } = new();
        public List<FuelLogDto> FuelLogs { get; set; } = new();
        public List<CharterBookingDto> CharterBookings { get; set; } = new();
        public List<RouteRateDto> RouteRates { get; set; } = new();
        public List<WeeklyFleetExpenseDto> CurrentWeekExpenses { get; set; } = new();

        public async Task OnGetAsync()
        {
            Vehicles = await _fleetService.GetVehiclesAsync();
            FuelLogs = await _fleetService.GetFuelLogsAsync();
            CharterBookings = await _fleetService.GetCharterBookingsAsync();
            RouteRates = await _fleetService.GetRouteRatesAsync();
            CurrentWeekExpenses = await _fleetService.GetWeeklyFleetExpensesAsync(
                GetOperatingWeekStart(DateTime.UtcNow),
                GetOperatingWeekStart(DateTime.UtcNow).AddDays(6));
        }

        private static DateTime GetOperatingWeekStart(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Sunday)) % 7;
            return date.Date.AddDays(-diff);
        }
    }
}
