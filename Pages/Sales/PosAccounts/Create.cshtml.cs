using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.Entities;
using Manufacture.Services;

namespace Manufacture.Pages.Sales.PosAccounts
{
    public class CreateModel : PageModel
    {
        private readonly MockSalesService _salesService;

        public CreateModel(MockSalesService salesService)
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

        public void OnGet()
        {
            SalesReps = _salesService.GetAllReps();
            CatalogSkus = _salesService.GetCatalogSkus();
            if (SalesReps.Any())
            {
                Account.AssignedRepId = SalesReps.First().Id;
            }
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Account.AccountName))
            {
                ModelState.AddModelError("Account.AccountName", "Account or Supermarket Name is required.");
                SalesReps = _salesService.GetAllReps();
                CatalogSkus = _salesService.GetCatalogSkus();
                return Page();
            }

            var ok = _salesService.SaveSupermarketAccount(Account);
            if (!ok)
            {
                ErrorMessage = "Failed to register supermarket account.";
                SalesReps = _salesService.GetAllReps();
                CatalogSkus = _salesService.GetCatalogSkus();
                return Page();
            }

            if (InitialPrices != null && InitialPrices.Any(kv => kv.Value > 0))
            {
                _salesService.SetBatchCustomAgreedPrices(Account.Id, InitialPrices);
            }

            TempData["SuccessMessage"] = $"Supermarket account '{Account.AccountName}' registered successfully with ₦{Account.CreditLimit:N0} approved credit limit.";
            return RedirectToPage("/Sales/PosAccounts/Index", new { accountId = Account.Id });
        }
    }
}
