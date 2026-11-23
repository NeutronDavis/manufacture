using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Manufacture.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Rates
{
    public class EditModel : PageModel
    {
        private readonly ICharterQuoteService _quotes;
        private readonly IFleetService _fleetService;
        private readonly RbacCache _rbac;

        public EditModel(ICharterQuoteService quotes, IFleetService fleetService, RbacCache rbac)
        {
            _quotes = quotes;
            _fleetService = fleetService;
            _rbac = rbac;
        }

        [BindProperty]
        public CharterQuoteDto QuoteInput { get; set; } = new();

        public CharterQuoteDto? Quote { get; set; }
        public List<VehicleDto> AvailableVehicles { get; set; } = new();
        public string ActiveRole { get; set; } = "SuperAdmin";
        public int UserId { get; set; }

        public bool CanApprove => string.Equals(ActiveRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase)
            || _rbac.CanApprove(ActiveRole, "Fleet");

        public List<string> VehicleTypes { get; } = new()
        {
            "30-Ton Haulage Truck",
            "15-Ton Box Truck",
            "Delivery Van",
            "VIP Escort Patrol"
        };

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
        {
            await LoadAsync(id, cancellationToken);
            return Quote is null ? RedirectToPage("/Logistics/Rates/Index") : Page();
        }

        public async Task<IActionResult> OnPostSaveAsync(int id, CancellationToken cancellationToken)
        {
            NormalizeLegs();
            if (!ModelState.IsValid)
            {
                await LoadAsync(id, cancellationToken);
                return Page();
            }

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                await _quotes.UpdateCharterQuoteAsync(QuoteInput, userId, cancellationToken);
                TempData["SuccessMessage"] = "Quote saved as draft.";
                return Redirect($"/Fleet/Quotes/Edit/{id}");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadAsync(id, cancellationToken);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostSubmitAsync(int id, CancellationToken cancellationToken)
        {
            try
            {
                var q = await _quotes.GetCharterQuoteAsync(id, cancellationToken);
                if (q is null) return RedirectToPage("/Logistics/Rates/Index");

                if (q.IsEditable)
                {
                    NormalizeLegs();
                    var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    await _quotes.UpdateCharterQuoteAsync(QuoteInput, userId, cancellationToken);
                }

                await _quotes.SubmitCharterQuoteForApprovalAsync(id, cancellationToken);
                TempData["SuccessMessage"] = "Quote submitted for approval.";
                return Redirect($"/Fleet/Quotes/Edit/{id}");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadAsync(id, cancellationToken);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostApproveAsync(int id, CancellationToken cancellationToken)
        {
            if (!CanApprove)
            {
                TempData["ErrorMessage"] = "You do not have permission to approve fleet quotes.";
                return Redirect($"/Fleet/Quotes/Edit/{id}");
            }

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var quote = await _quotes.GetCharterQuoteAsync(id, cancellationToken);
                if (quote is not null
                    && quote.RequestedByUserId == userId
                    && !string.Equals(ActiveRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("You cannot approve a quote you prepared yourself.");
                }

                await _quotes.ApproveCharterQuoteAsync(id, userId, cancellationToken);
                TempData["SuccessMessage"] = "Quote approved and ready for booking.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return Redirect($"/Fleet/Quotes/Edit/{id}");
        }

        public async Task<IActionResult> OnPostRejectAsync(int id, string? reason, CancellationToken cancellationToken)
        {
            if (!CanApprove)
            {
                TempData["ErrorMessage"] = "You do not have permission to reject fleet quotes.";
                return Redirect($"/Fleet/Quotes/Edit/{id}");
            }

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                await _quotes.RejectCharterQuoteAsync(id, reason ?? string.Empty, userId, cancellationToken);
                TempData["SuccessMessage"] = "Quote rejected. It can be edited and resubmitted.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return Redirect($"/Fleet/Quotes/Edit/{id}");
        }

        public async Task<IActionResult> OnPostCloneAsync(int id, CancellationToken cancellationToken)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var clone = await _quotes.CloneCharterQuoteAsync(id, userId, cancellationToken);
                TempData["SuccessMessage"] = $"Quote cloned as {clone.QuoteRef}.";
                return Redirect($"/Fleet/Quotes/Edit/{clone.Id}");
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return Redirect($"/Fleet/Quotes/Edit/{id}");
            }
        }

        private async Task LoadAsync(int id, CancellationToken cancellationToken)
        {
            ActiveRole = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            UserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            AvailableVehicles = await _fleetService.GetVehiclesAsync(cancellationToken);
            Quote = await _quotes.GetCharterQuoteAsync(id, cancellationToken);
            if (Quote is null)
            {
                TempData["ErrorMessage"] = "Quote not found.";
                return;
            }

            if (QuoteInput.Id == 0)
            {
                QuoteInput = Quote;
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
