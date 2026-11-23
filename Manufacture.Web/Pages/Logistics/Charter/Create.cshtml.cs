using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Charter
{
    public class CreateModel : PageModel
    {
        private readonly IFleetService _fleetService;
        private readonly ICharterQuoteService _quotes;

        public CreateModel(IFleetService fleetService, ICharterQuoteService quotes)
        {
            _fleetService = fleetService;
            _quotes = quotes;
        }

        [BindProperty]
        public CharterBookingDto Booking { get; set; } = new()
        {
            DepartureDate = DateTime.UtcNow.Date.AddDays(1)
        };

        [BindProperty(SupportsGet = true)]
        public int? QuoteId { get; set; }

        public List<VehicleDto> AvailableVehicles { get; set; } = new();
        public List<FleetDriverDto> AvailableDrivers { get; set; } = new();
        public List<CharterQuoteDto> ApprovedQuotes { get; set; } = new();
        public CharterQuoteDto? SelectedQuote { get; set; }

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            await LoadAsync(cancellationToken);
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            if (!Booking.QuoteRequestId.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Select an approved client quote before booking.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(cancellationToken);
                return Page();
            }

            try
            {
                var created = await _fleetService.CreateCharterBookingAsync(Booking, cancellationToken);
                TempData["SuccessMessage"] = $"Charter booking {created.BookingRef} confirmed from {created.QuoteRef}.";
                return RedirectToPage("/Logistics/Charter/Details", new { id = created.Id });
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
            AvailableVehicles = await _fleetService.GetVehiclesAsync(cancellationToken);
            AvailableDrivers = await _fleetService.GetDriversAsync(cancellationToken);
            ApprovedQuotes = await _quotes.GetApprovedCharterQuotesAsync(cancellationToken);

            SelectedQuote = Booking.QuoteRequestId.HasValue
                ? ApprovedQuotes.FirstOrDefault(q => q.Id == Booking.QuoteRequestId.Value)
                : QuoteId.HasValue
                    ? ApprovedQuotes.FirstOrDefault(q => q.Id == QuoteId.Value)
                    : ApprovedQuotes.FirstOrDefault();

            if (SelectedQuote is not null)
            {
                Booking.QuoteRequestId = SelectedQuote.Id;
                Booking.QuoteRef = SelectedQuote.QuoteRef;
                Booking.CustomerName = SelectedQuote.CustomerName;
                Booking.CustomerPhone = SelectedQuote.CustomerPhone;
                Booking.TotalDistanceKm = SelectedQuote.TotalDistanceKm;
            }

            if (Booking.VehicleId == 0 && AvailableVehicles.Any())
            {
                Booking.VehicleId = AvailableVehicles.First().Id;
            }

            if (!Booking.DriverEmployeeId.HasValue)
            {
                var selectedVehicle = AvailableVehicles.FirstOrDefault(v => v.Id == Booking.VehicleId);
                Booking.DriverEmployeeId = selectedVehicle?.AssignedEmployeeId;
            }
        }
    }
}
