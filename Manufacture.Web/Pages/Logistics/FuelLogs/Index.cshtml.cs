using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
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

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SourceFilter { get; set; } = "All";

        public int CharterCount => FuelLogs.Count(log => log.CharterBookingId.HasValue);
        public int DispatchCount => FuelLogs.Count(log => log.FleetDispatchId.HasValue);
        public int GeneralCount => FuelLogs.Count(log => !log.CharterBookingId.HasValue && !log.FleetDispatchId.HasValue);

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            var logs = await _fleetService.GetFuelLogsAsync(cancellationToken);

            if (FromDate.HasValue)
            {
                logs = logs.Where(log => log.Date.Date >= FromDate.Value.Date).ToList();
            }

            if (ToDate.HasValue)
            {
                logs = logs.Where(log => log.Date.Date <= ToDate.Value.Date).ToList();
            }

            logs = (SourceFilter ?? "All").Trim().ToLowerInvariant() switch
            {
                "charter" => logs.Where(log => log.CharterBookingId.HasValue).ToList(),
                "dispatch" => logs.Where(log => log.FleetDispatchId.HasValue).ToList(),
                "general" => logs.Where(log => !log.CharterBookingId.HasValue && !log.FleetDispatchId.HasValue).ToList(),
                _ => logs
            };

            FuelLogs = logs;
        }

        public static string SourceLabel(FuelLogDto log) =>
            log.CharterBookingId.HasValue ? "Charter"
            : log.FleetDispatchId.HasValue ? "Dispatch"
            : "General";

        public static string SourceReference(FuelLogDto log) =>
            log.CharterBookingId.HasValue ? log.CharterBookingRef
            : log.FleetDispatchId.HasValue ? log.FleetDispatchRef
            : string.Empty;
    }
}
