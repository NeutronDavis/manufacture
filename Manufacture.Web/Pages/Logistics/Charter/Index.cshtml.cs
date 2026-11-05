using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.Entities;
using Manufacture.Services;

namespace Manufacture.Pages.Logistics.Charter
{
    public class IndexModel : PageModel
    {
        private readonly MockLogisticsService _logisticsService;

        public IndexModel(MockLogisticsService logisticsService)
        {
            _logisticsService = logisticsService;
        }

        public List<CharterBooking> Bookings { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; } = "All";

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        public int TotalBookingsCount => Bookings.Count;
        public int InTransitCount => Bookings.Count(b => b.TripStatus == CharterTripStatus.InTransit);
        public decimal TotalGrossRevenue => Bookings.Where(b => b.TripStatus != CharterTripStatus.Cancelled).Sum(b => b.GrossCharterFee);
        public decimal TotalNetRevenue => Bookings.Where(b => b.TripStatus != CharterTripStatus.Cancelled).Sum(b => b.NetCharterRevenue);

        public void OnGet()
        {
            var list = _logisticsService.GetAllCharterBookings();

            if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
            {
                if (Enum.TryParse<CharterTripStatus>(StatusFilter, out var status))
                {
                    list = list.Where(b => b.TripStatus == status).ToList();
                }
            }

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim().ToLowerInvariant();
                list = list.Where(b =>
                    b.BookingRef.ToLowerInvariant().Contains(term) ||
                    b.CustomerName.ToLowerInvariant().Contains(term) ||
                    b.VehicleRegNumber.ToLowerInvariant().Contains(term) ||
                    b.DriverName.ToLowerInvariant().Contains(term) ||
                    b.Legs.Any(l => l.OriginState.ToLowerInvariant().Contains(term) || l.DestinationState.ToLowerInvariant().Contains(term))
                ).ToList();
            }

            Bookings = list;
        }

        public IActionResult OnPostUpdateStatus(int id, CharterTripStatus newStatus)
        {
            var success = _logisticsService.UpdateBookingStatus(id, newStatus);
            if (success)
            {
                TempData["SuccessMessage"] = $"Booking status successfully updated to {newStatus}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not find booking to update status.";
            }
            return RedirectToPage(new { StatusFilter, Search });
        }
    }
}
