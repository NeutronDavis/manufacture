using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.Entities;
using Manufacture.Services;

namespace Manufacture.Pages.Logistics.Charter
{
    public class DetailsModel : PageModel
    {
        private readonly MockLogisticsService _logisticsService;

        public DetailsModel(MockLogisticsService logisticsService)
        {
            _logisticsService = logisticsService;
        }

        public CharterBooking? Booking { get; set; }
        public Vehicle? Vehicle { get; set; }

        public IActionResult OnGet(int id)
        {
            Booking = _logisticsService.GetCharterBookingById(id);
            if (Booking == null)
            {
                TempData["ErrorMessage"] = "Charter booking not found.";
                return RedirectToPage("/Logistics/Charter/Index");
            }

            Vehicle = _logisticsService.GetVehicleById(Booking.VehicleId);
            return Page();
        }

        public IActionResult OnPostUpdateStatus(int id, CharterTripStatus newStatus)
        {
            var success = _logisticsService.UpdateBookingStatus(id, newStatus);
            if (success)
            {
                TempData["SuccessMessage"] = $"Trip status updated to {newStatus}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not update trip status.";
            }

            return RedirectToPage(new { id });
        }
    }
}
