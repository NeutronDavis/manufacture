using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Dispatches
{
    public class DetailsModel : PageModel
    {
        private readonly IFleetDispatchService _dispatches;
        private readonly IFleetService _fleet;

        public DetailsModel(IFleetDispatchService dispatches, IFleetService fleet)
        {
            _dispatches = dispatches;
            _fleet = fleet;
        }

        public FleetDispatchDto? Dispatch { get; set; }
        public VehicleDto? Vehicle { get; set; }
        public string? VehicleStatus { get; set; }

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
        {
            await LoadAsync(id, cancellationToken);
            return Dispatch is null ? RedirectToPage("/Logistics/Dispatches/Index") : Page();
        }

        public async Task<IActionResult> OnPostStartAsync(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _dispatches.StartDispatchAsync(id, cancellationToken);
                TempData["SuccessMessage"] = "Dispatch started. Vehicle marked InTransit.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return Redirect($"/Fleet/Dispatches/Details/{id}");
        }

        public async Task<IActionResult> OnPostCompleteAsync(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _dispatches.CompleteDispatchAsync(id, cancellationToken);
                TempData["SuccessMessage"] = "Dispatch completed.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return Redirect($"/Fleet/Dispatches/Details/{id}");
        }

        public async Task<IActionResult> OnPostCancelAsync(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _dispatches.CancelDispatchAsync(id, cancellationToken);
                TempData["SuccessMessage"] = "Dispatch cancelled.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return Redirect($"/Fleet/Dispatches/Details/{id}");
        }

        public async Task<IActionResult> OnPostStopStatusAsync(
            int id,
            int stopId,
            FleetDispatchStopStatus status,
            CancellationToken cancellationToken)
        {
            try
            {
                await _dispatches.UpdateStopStatusAsync(id, stopId, status, cancellationToken);
                TempData["SuccessMessage"] = $"Delivery point status updated to {status}.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return Redirect($"/Fleet/Dispatches/Details/{id}");
        }

        public async Task<IActionResult> OnPostVehicleStatusAsync(
            int id,
            int vehicleId,
            string status,
            CancellationToken cancellationToken)
        {
            try
            {
                await _dispatches.UpdateVehicleStatusAsync(vehicleId, status, cancellationToken);
                TempData["SuccessMessage"] = $"Vehicle status updated to {status}.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return Redirect($"/Fleet/Dispatches/Details/{id}");
        }

        private async Task LoadAsync(int id, CancellationToken cancellationToken)
        {
            Dispatch = await _dispatches.GetDispatchAsync(id, cancellationToken);
            if (Dispatch is null) return;

            Vehicle = await _fleet.GetVehicleAsync(Dispatch.VehicleId, cancellationToken);
            VehicleStatus = Vehicle?.Status;
        }
    }
}
