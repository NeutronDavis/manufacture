using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Rates
{
    public class CreateModel : PageModel
    {
        private readonly ICharterQuoteService _quotes;
        private readonly IFleetService _fleetService;

        public CreateModel(ICharterQuoteService quotes, IFleetService fleetService)
        {
            _quotes = quotes;
            _fleetService = fleetService;
        }

        [BindProperty]
        public CharterQuoteDto QuoteInput { get; set; } = new()
        {
            RequestDate = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.Date.AddDays(14),
            VehicleTypeRequired = "VIP Escort Patrol",
            Legs = new List<CharterQuoteLegDto>
            {
                new()
                {
                    LegNumber = 1,
                    OriginCityState = "Lagos",
                    DestinationCityState = "Rivers (Port Harcourt)",
                    FuelOption = FuelOption.WithFuel
                }
            }
        };

        public List<VehicleDto> AvailableVehicles { get; set; } = new();

        public List<string> VehicleTypes { get; } = new()
        {
            "30-Ton Haulage Truck",
            "15-Ton Box Truck",
            "Delivery Van",
            "VIP Escort Patrol"
        };

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            await LoadFormDataAsync(cancellationToken);
        }

        public async Task<IActionResult> OnPostSaveDraftAsync(CancellationToken cancellationToken)
        {
            NormalizeLegs();
            if (!ModelState.IsValid)
            {
                await LoadFormDataAsync(cancellationToken);
                return Page();
            }

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var created = await _quotes.CreateCharterQuoteAsync(QuoteInput, userId, cancellationToken);
                TempData["SuccessMessage"] = $"Quote {created.QuoteRef} saved as draft.";
                return Redirect($"/Fleet/Quotes/Edit/{created.Id}");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadFormDataAsync(cancellationToken);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostSubmitAsync(CancellationToken cancellationToken)
        {
            NormalizeLegs();
            if (!ModelState.IsValid)
            {
                await LoadFormDataAsync(cancellationToken);
                return Page();
            }

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var created = await _quotes.CreateCharterQuoteAsync(QuoteInput, userId, cancellationToken);
                await _quotes.SubmitCharterQuoteForApprovalAsync(created.Id, cancellationToken);
                TempData["SuccessMessage"] = $"Quote {created.QuoteRef} submitted for approval.";
                return RedirectToPage("/Logistics/Rates/Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadFormDataAsync(cancellationToken);
                return Page();
            }
        }

        private async Task LoadFormDataAsync(CancellationToken cancellationToken)
        {
            AvailableVehicles = await _fleetService.GetVehiclesAsync(cancellationToken);
            QuoteInput.Legs ??= new List<CharterQuoteLegDto>();
            if (!QuoteInput.Legs.Any())
            {
                QuoteInput.Legs.Add(new CharterQuoteLegDto { LegNumber = 1 });
            }
        }

        private void NormalizeLegs()
        {
            QuoteInput.Legs ??= new List<CharterQuoteLegDto>();
            QuoteInput.Legs = QuoteInput.Legs
                .Where(leg => !string.IsNullOrWhiteSpace(leg.OriginAddress)
                    || !string.IsNullOrWhiteSpace(leg.DestinationAddress)
                    || leg.DistanceKm > 0
                    || leg.QuotedLegAmount > 0)
                .ToList();
            for (var i = 0; i < QuoteInput.Legs.Count; i++)
            {
                QuoteInput.Legs[i].LegNumber = i + 1;
            }
        }
    }
}
