using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Charter
{
    public class DetailsModel : PageModel
    {
        private readonly IFleetService _fleetService;

        public DetailsModel(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        public CharterBookingDto? Booking { get; set; }
        public VehicleDto? Vehicle { get; set; }
        public List<FuelLogDto> FuelLogs { get; set; } = new();
        public decimal TotalFuelCost => FuelLogs.Sum(log => log.TotalCost);

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken = default)
        {
            Booking = await _fleetService.GetCharterBookingAsync(id, cancellationToken);
            if (Booking == null)
            {
                TempData["ErrorMessage"] = "Charter booking not found.";
                return RedirectToPage("/Logistics/Charter/Index");
            }

            Vehicle = await _fleetService.GetVehicleAsync(Booking.VehicleId, cancellationToken);
            FuelLogs = await _fleetService.GetFuelLogsForCharterAsync(id, cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id, CharterTripStatus newStatus)
        {
            var success = await _fleetService.UpdateCharterStatusAsync(id, newStatus);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? $"Trip status updated to {newStatus}."
                : "Could not update trip status.";

            return RedirectToPage(new { id });
        }
    }
}
