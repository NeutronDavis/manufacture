using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.Domain;
using Manufacture.Services;

namespace Manufacture.Pages.Sales.PosAccounts
{
    public class EditModel : PageModel
    {
        private readonly MockSalesService _salesService;

        public EditModel(MockSalesService salesService)
        {
            _salesService = salesService;
        }

        [BindProperty]
        public SupermarketAccount Account { get; set; } = new();

        public List<(int Id, string Code, string Name, ProductType Category, decimal Price)> CatalogSkus { get; set; } = new();
        public List<SalesRepProfile> SalesReps { get; set; } = new();
        public SalesRepProfile? AssignedRep { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public IActionResult OnGet(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                ErrorMessage = "Supermarket account ID is required.";
                return RedirectToPage("/Sales/PosAccounts/Index");
            }

            var account = _salesService.GetSupermarketAccountById(id.Value);
            if (account == null)
            {
                ErrorMessage = $"Supermarket account #{id.Value} was not found.";
                return RedirectToPage("/Sales/PosAccounts/Index");
            }

            Account = account;
            LoadContext();

            return Page();
        }

        public IActionResult OnPostSaveAccount()
        {
            if (string.IsNullOrWhiteSpace(Account.AccountName))
            {
                ErrorMessage = "Supermarket name is required.";
                LoadContext();
                return Page();
            }

            var ok = _salesService.SaveSupermarketAccount(Account);
            if (ok)
            {
                SuccessMessage = $"Supermarket '{Account.AccountName}' details updated successfully.";
                return RedirectToPage(new { id = Account.Id });
            }

            ErrorMessage = "Failed to update supermarket details.";
            LoadContext();
            return Page();
        }

        public IActionResult OnPostSavePrices(int accountId, Dictionary<int, decimal> prices)
        {
            if (accountId <= 0)
            {
                ErrorMessage = "Invalid account specified.";
                return RedirectToPage(new { id = accountId });
            }

            var ok = _salesService.SetBatchCustomAgreedPrices(accountId, prices ?? new());
            if (ok)
            {
                SuccessMessage = "Custom agreed SKU pricing contract successfully updated.";
            }
            else
            {
                ErrorMessage = "Failed to update custom agreed prices.";
            }

            return RedirectToPage(new { id = accountId });
        }

        public IActionResult OnPostToggleStatus(int id)
        {
            if (id <= 0)
            {
                ErrorMessage = "Invalid account specified.";
                return RedirectToPage(new { id });
            }

            var ok = _salesService.ToggleSupermarketStatus(id);
            if (ok)
            {
                var account = _salesService.GetSupermarketAccountById(id);
                var statusText = account != null && account.IsActive ? "Activated" : "Suspended";
                SuccessMessage = $"Supermarket account status changed to: {statusText}.";
            }
            else
            {
                ErrorMessage = "Failed to toggle status.";
            }

            return RedirectToPage(new { id });
        }

        private void LoadContext()
        {
            CatalogSkus = _salesService.GetCatalogSkus();
            SalesReps = _salesService.GetAllReps();
            AssignedRep = SalesReps.FirstOrDefault(r => r.Id == Account.AssignedRepId);
        }
    }
}
