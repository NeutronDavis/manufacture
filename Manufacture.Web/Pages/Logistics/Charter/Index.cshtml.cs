using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Charter
{
    public class IndexModel : PageModel
    {
        private readonly IFleetService _fleetService;

        public IndexModel(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        public List<CharterBookingDto> Bookings { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; } = "All";

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        public int TotalBookingsCount => Bookings.Count;
        public int InTransitCount => Bookings.Count(b => b.TripStatus == CharterTripStatus.InTransit);
        public decimal TotalGrossRevenue => Bookings.Where(b => b.TripStatus != CharterTripStatus.Cancelled).Sum(b => b.GrossCharterFee);
        public decimal TotalNetRevenue => Bookings.Where(b => b.TripStatus != CharterTripStatus.Cancelled).Sum(b => b.NetCharterRevenue);

        public async Task OnGetAsync()
        {
            Bookings = await _fleetService.GetCharterBookingsAsync(StatusFilter, Search);

            if (FromDate.HasValue)
            {
                Bookings = Bookings
                    .Where(b => b.DepartureDate.Date >= FromDate.Value.Date)
                    .ToList();
            }

            if (ToDate.HasValue)
            {
                Bookings = Bookings
                    .Where(b => b.DepartureDate.Date <= ToDate.Value.Date)
                    .ToList();
            }
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id, CharterTripStatus newStatus)
        {
            var success = await _fleetService.UpdateCharterStatusAsync(id, newStatus);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? $"Booking status successfully updated to {newStatus}."
                : "Could not find booking to update status.";

            return RedirectToPage(new { StatusFilter, Search, FromDate, ToDate });
        }
    }
}
