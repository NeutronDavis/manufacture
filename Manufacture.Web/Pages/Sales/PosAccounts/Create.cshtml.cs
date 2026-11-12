using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.PosAccounts
{
    public class CreateModel : PageModel
    {
        private readonly ISalesService _salesService;

        public CreateModel(ISalesService salesService)
        {
            _salesService = salesService;
        }

        [BindProperty]
        public SupermarketAccount Account { get; set; } = new()
        {
            CreditLimit = 250000m,
            PaymentTerms = "Net 15 Days",
            IsActive = true
        };

        [BindProperty]
        public Dictionary<int, decimal> InitialPrices { get; set; } = new();

        public List<SalesRepProfile> SalesReps { get; set; } = new();
        public List<(int Id, string Code, string Name, ProductType Category, decimal Price)> CatalogSkus { get; set; } = new();

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            await LoadDataAsync(cancellationToken);
            if (SalesReps.Any())
            {
                Account.AssignedRepId = SalesReps.First().Id;
            }
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(Account.AccountName))
            {
                ModelState.AddModelError("Account.AccountName", "Account or Supermarket Name is required.");
                await LoadDataAsync(cancellationToken);
                return Page();
            }

            var ok = await _salesService.SaveSupermarketAccountAsync(Account, cancellationToken);
            if (!ok)
            {
                ErrorMessage = "Failed to register supermarket account.";
                await LoadDataAsync(cancellationToken);
                return Page();
            }

            if (InitialPrices != null && InitialPrices.Any(kv => kv.Value > 0))
            {
                await _salesService.SetBatchCustomAgreedPricesAsync(Account.Id, InitialPrices, cancellationToken);
            }

            TempData["SuccessMessage"] = $"Supermarket account '{Account.AccountName}' registered successfully with ₦{Account.CreditLimit:N0} approved credit limit.";
            return RedirectToPage("/Sales/PosAccounts/Index", new { accountId = Account.Id });
        }

        private async Task LoadDataAsync(CancellationToken cancellationToken)
        {
            SalesReps = await _salesService.GetSalesRepsAsync(cancellationToken: cancellationToken);
            CatalogSkus = await _salesService.GetCatalogSkusAsync(cancellationToken);
        }
    }
}
