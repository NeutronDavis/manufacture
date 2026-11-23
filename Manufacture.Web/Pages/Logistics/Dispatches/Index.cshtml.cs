using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Dispatches
{
    public class IndexModel : PageModel
    {
        private readonly IFleetDispatchService _dispatches;
        private readonly IFleetService _fleet;

        public IndexModel(IFleetDispatchService dispatches, IFleetService fleet)
        {
            _dispatches = dispatches;
            _fleet = fleet;
        }

        public List<FleetDispatchDto> Dispatches { get; set; } = new();
        public List<VehicleDto> Vehicles { get; set; } = new();
        public List<FleetDriverDto> Drivers { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; } = "All";

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? VehicleId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? DriverId { get; set; }

        public int PlannedCount => Dispatches.Count(d => d.Status == FleetDispatchStatus.Planned);
        public int InTransitCount => Dispatches.Count(d => d.Status == FleetDispatchStatus.InTransit);
        public int CompletedCount => Dispatches.Count(d => d.Status == FleetDispatchStatus.Completed);
        public decimal TotalFuelCost => Dispatches.Sum(d => d.TotalFuelCost);

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            Vehicles = await _fleet.GetVehiclesAsync(cancellationToken);
            Drivers = await _fleet.GetDriversAsync(cancellationToken);
            Dispatches = await _dispatches.GetDispatchesAsync(StatusFilter, Search, FromDate, ToDate, VehicleId, DriverId, cancellationToken);
        }

        public static string StatusBadge(FleetDispatchStatus status) => status switch
        {
            FleetDispatchStatus.Planned => "badge-ghost",
            FleetDispatchStatus.Loading => "badge-warning",
            FleetDispatchStatus.InTransit => "badge-info",
            FleetDispatchStatus.Delivered => "badge-primary",
            FleetDispatchStatus.Completed => "badge-success",
            FleetDispatchStatus.Cancelled => "badge-error",
            _ => "badge-ghost"
        };
    }
}
