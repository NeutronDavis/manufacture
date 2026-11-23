using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Dispatches
{
    public class CreateModel : PageModel
    {
        private readonly IFleetDispatchService _dispatches;
        private readonly IFleetService _fleet;

        public CreateModel(IFleetDispatchService dispatches, IFleetService fleet)
        {
            _dispatches = dispatches;
            _fleet = fleet;
        }

        [BindProperty]
        public FleetDispatchDto DispatchInput { get; set; } = new()
        {
            DispatchDate = DateTime.UtcNow.Date,
            Purpose = "Production Delivery",
            Stops = new List<FleetDispatchStopDto>
            {
                new() { StopNumber = 1 }
            }
        };

        public List<VehicleDto> Vehicles { get; set; } = new();
        public List<FleetDriverDto> Drivers { get; set; } = new();

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            await LoadAsync(cancellationToken);
        }

        public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken)
        {
            return await SaveAsync(start: false, cancellationToken);
        }

        public async Task<IActionResult> OnPostStartAsync(CancellationToken cancellationToken)
        {
            return await SaveAsync(start: true, cancellationToken);
        }

        private async Task<IActionResult> SaveAsync(bool start, CancellationToken cancellationToken)
        {
            NormalizeStops();
            if (!ModelState.IsValid)
            {
                await LoadAsync(cancellationToken);
                return Page();
            }

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var created = await _dispatches.CreateDispatchAsync(DispatchInput, userId, cancellationToken);
                if (start)
                {
                    await _dispatches.StartDispatchAsync(created.Id, cancellationToken);
                    TempData["SuccessMessage"] = $"Dispatch {created.DispatchRef} created and started.";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Dispatch {created.DispatchRef} saved as planned.";
                }

                return Redirect($"/Fleet/Dispatches/Details/{created.Id}");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadAsync(cancellationToken);
                return Page();
            }
        }

        private async Task LoadAsync(CancellationToken cancellationToken)
        {
            Vehicles = await _fleet.GetVehiclesAsync(cancellationToken);
            Drivers = await _fleet.GetDriversAsync(cancellationToken);
            DispatchInput.Stops ??= new List<FleetDispatchStopDto>();
            if (DispatchInput.Stops.Count == 0)
            {
                DispatchInput.Stops.Add(new FleetDispatchStopDto { StopNumber = 1 });
            }

            if (DispatchInput.VehicleId == 0 && Vehicles.Any())
            {
                DispatchInput.VehicleId = Vehicles.First().Id;
            }

            if (!DispatchInput.DriverEmployeeId.HasValue)
            {
                var vehicle = Vehicles.FirstOrDefault(v => v.Id == DispatchInput.VehicleId);
                DispatchInput.DriverEmployeeId = vehicle?.AssignedEmployeeId;
            }
        }

        private void NormalizeStops()
        {
            DispatchInput.Stops ??= new List<FleetDispatchStopDto>();
            DispatchInput.Stops = DispatchInput.Stops
                .Where(stop => !string.IsNullOrWhiteSpace(stop.DeliveryPointName)
                    || !string.IsNullOrWhiteSpace(stop.Address))
                .ToList();
            for (var i = 0; i < DispatchInput.Stops.Count; i++)
            {
                DispatchInput.Stops[i].StopNumber = i + 1;
            }
        }
    }
}
