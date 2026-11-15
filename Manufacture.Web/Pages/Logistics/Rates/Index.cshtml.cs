using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Manufacture.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Rates
{
    public class IndexModel : PageModel
    {
        private readonly ICharterQuoteService _quotes;
        private readonly RbacCache _rbac;

        public IndexModel(ICharterQuoteService quotes, RbacCache rbac)
        {
            _quotes = quotes;
            _rbac = rbac;
        }

        public List<CharterQuoteDto> Quotes { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; } = "All";

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        public string ActiveRole { get; set; } = "SuperAdmin";
        public bool CanApprove => string.Equals(ActiveRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase)
            || _rbac.CanApprove(ActiveRole, "Fleet");

        public int DraftCount => Quotes.Count(q => q.Status == CharterQuoteStatus.Draft);
        public int PendingCount => Quotes.Count(q => q.Status == CharterQuoteStatus.PendingApproval);
        public int ApprovedCount => Quotes.Count(q => q.Status == CharterQuoteStatus.Approved);
        public int ConvertedCount => Quotes.Count(q => q.Status == CharterQuoteStatus.Converted);
        public decimal TotalQuotedValue => Quotes.Where(q => q.Status != CharterQuoteStatus.Rejected).Sum(q => q.QuotedAmount);

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            ActiveRole = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            Quotes = await _quotes.GetCharterQuotesAsync(StatusFilter, Search, cancellationToken);
        }

        public string StatusBadge(CharterQuoteStatus status) => status switch
        {
            CharterQuoteStatus.Draft => "badge-ghost",
            CharterQuoteStatus.PendingApproval => "badge-warning",
            CharterQuoteStatus.Approved => "badge-success",
            CharterQuoteStatus.Rejected => "badge-error",
            CharterQuoteStatus.Expired => "badge-neutral",
            CharterQuoteStatus.Converted => "badge-info",
            _ => "badge-ghost"
        };
    }
}
