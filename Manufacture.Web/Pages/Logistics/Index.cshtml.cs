using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics
{
    public class IndexModel : PageModel
    {
        private readonly IFleetService _fleetService;
        private readonly ICharterQuoteService _quoteService;
        private readonly IFleetDispatchService _dispatchService;

        public IndexModel(
            IFleetService fleetService,
            ICharterQuoteService quoteService,
            IFleetDispatchService dispatchService)
        {
            _fleetService = fleetService;
            _quoteService = quoteService;
            _dispatchService = dispatchService;
        }

        public List<VehicleDto> Vehicles { get; set; } = new();
        public List<FuelLogDto> FuelLogs { get; set; } = new();
        public List<CharterBookingDto> CharterBookings { get; set; } = new();
        public List<CharterQuoteDto> Quotes { get; set; } = new();
        public List<FleetDispatchDto> Dispatches { get; set; } = new();
        public List<FleetDriverDto> Drivers { get; set; } = new();
        public List<MaintenanceLogDto> MaintenanceLogs { get; set; } = new();

        public int ActiveVehicleCount => Vehicles.Count(v => v.Status == "Active");
        public int InTransitVehicleCount => Vehicles.Count(v => v.Status == "InTransit");
        public int InMaintenanceVehicleCount => Vehicles.Count(v => v.Status == "InMaintenance");
        public int GroundedVehicleCount => Vehicles.Count(v => v.Status == "Grounded");
        public int OpenCharterCount => CharterBookings.Count(b => b.TripStatus == Manufacture.Models.Domain.CharterTripStatus.Booked);
        public int CharterInTransitCount => CharterBookings.Count(b => b.TripStatus == Manufacture.Models.Domain.CharterTripStatus.InTransit);
        public int PendingQuoteCount => Quotes.Count(q => q.Status == CharterQuoteStatus.PendingApproval);
        public int ApprovedQuoteCount => Quotes.Count(q => q.Status == CharterQuoteStatus.Approved);
        public int PlannedDispatchCount => Dispatches.Count(d => d.Status == FleetDispatchStatus.Planned);
        public int DispatchInTransitCount => Dispatches.Count(d => d.Status == FleetDispatchStatus.InTransit);
        public int CompletedDispatchCount => Dispatches.Count(d => d.Status == FleetDispatchStatus.Completed);
        public decimal TotalFuelLitres => FuelLogs.Sum(log => log.LitresDispensed);
        public int FuelRecordCount => FuelLogs.Count;
        public int MaintenanceEventCount => MaintenanceLogs.Count;

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            Vehicles = await _fleetService.GetVehiclesAsync(cancellationToken);
            FuelLogs = await _fleetService.GetFuelLogsAsync(cancellationToken);
            CharterBookings = await _fleetService.GetCharterBookingsAsync(cancellationToken: cancellationToken);
            Quotes = await _quoteService.GetCharterQuotesAsync(null, null, cancellationToken);
            Dispatches = await _dispatchService.GetDispatchesAsync(cancellationToken: cancellationToken);
            Drivers = await _fleetService.GetDriversAsync(cancellationToken);
            MaintenanceLogs = await _fleetService.GetMaintenanceLogsAsync(cancellationToken);
        }
    }
}
