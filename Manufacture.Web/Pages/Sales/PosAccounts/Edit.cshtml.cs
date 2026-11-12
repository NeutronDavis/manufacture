using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.PosAccounts
{
    public class EditModel : PageModel
    {
        private readonly ISalesService _salesService;

        public EditModel(ISalesService salesService)
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

        public async Task<IActionResult> OnGetAsync(int? id, CancellationToken cancellationToken = default)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                ErrorMessage = "Supermarket account ID is required.";
                return RedirectToPage("/Sales/PosAccounts/Index");
            }

            var account = await _salesService.GetSupermarketAccountAsync(id.Value, cancellationToken);
            if (account == null)
            {
                ErrorMessage = $"Supermarket account #{id.Value} was not found.";
                return RedirectToPage("/Sales/PosAccounts/Index");
            }

            Account = account;
            await LoadContextAsync(cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAccountAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(Account.AccountName))
            {
                ErrorMessage = "Supermarket name is required.";
                await LoadContextAsync(cancellationToken);
                return Page();
            }

            var ok = await _salesService.SaveSupermarketAccountAsync(Account, cancellationToken);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? $"Supermarket '{Account.AccountName}' details updated successfully."
                : "Failed to update supermarket details.";

            return ok
                ? RedirectToPage(new { id = Account.Id })
                : Page();
        }

        public async Task<IActionResult> OnPostSavePricesAsync(
            int accountId,
            Dictionary<int, decimal> prices,
            CancellationToken cancellationToken = default)
        {
            if (accountId <= 0)
            {
                ErrorMessage = "Invalid account specified.";
                return RedirectToPage(new { id = accountId });
            }

            var ok = await _salesService.SetBatchCustomAgreedPricesAsync(accountId, prices ?? new(), cancellationToken);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? "Custom agreed SKU pricing contract successfully updated."
                : "Failed to update custom agreed prices.";

            return RedirectToPage(new { id = accountId });
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                ErrorMessage = "Invalid account specified.";
                return RedirectToPage(new { id });
            }

            var ok = await _salesService.ToggleSupermarketStatusAsync(id, cancellationToken);
            if (ok)
            {
                var account = await _salesService.GetSupermarketAccountAsync(id, cancellationToken);
                SuccessMessage = $"Supermarket account status changed to: {(account?.IsActive == true ? "Activated" : "Suspended")}.";
            }
            else
            {
                ErrorMessage = "Failed to toggle status.";
            }

            return RedirectToPage(new { id });
        }

        private async Task LoadContextAsync(CancellationToken cancellationToken)
        {
            CatalogSkus = await _salesService.GetCatalogSkusAsync(cancellationToken);
            SalesReps = await _salesService.GetSalesRepsAsync(cancellationToken: cancellationToken);
            AssignedRep = SalesReps.FirstOrDefault(r => r.Id == Account.AssignedRepId);
        }
    }
}
