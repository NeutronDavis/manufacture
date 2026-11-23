using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.PosAccounts
{
    public class IndexModel : PageModel
    {
        private readonly ISalesService _salesService;

        public IndexModel(ISalesService salesService)
        {
            _salesService = salesService;
        }

        public List<SupermarketAccount> Accounts { get; set; } = new();
        public List<(int Id, string Code, string Name, ProductType Category, decimal Price)> CatalogSkus { get; set; } = new();
        public List<SalesRepProfile> SalesReps { get; set; } = new();

        public SupermarketAccount? SelectedAccount { get; set; }

        public decimal TotalCreditOutstanding => Accounts.Sum(a => a.CurrentOutstandingCredit);
        public decimal TotalCreditLimit => Accounts.Sum(a => a.CreditLimit);
        public int HighRiskCount => Accounts.Count(a => a.CreditLimit > 0 && (a.CurrentOutstandingCredit / a.CreditLimit) >= 0.8m);

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync(int? accountId, CancellationToken cancellationToken = default)
        {
            await LoadDataAsync(accountId, cancellationToken);
        }

        public async Task<IActionResult> OnPostSavePricesAsync(
            int accountId,
            Dictionary<int, decimal> prices,
            CancellationToken cancellationToken = default)
        {
            if (accountId <= 0)
            {
                ErrorMessage = "Invalid account specified.";
                return RedirectToPage(new { accountId });
            }

            var ok = await _salesService.SetBatchCustomAgreedPricesAsync(accountId, prices ?? new(), cancellationToken);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? "Custom agreed prices successfully updated for this supermarket account."
                : "Failed to update custom agreed prices.";

            return RedirectToPage(new { accountId });
        }

        public async Task<IActionResult> OnPostFulfillOrderAsync(
            int accountId,
            int repId,
            int recipeId,
            int quantity,
            string? notes,
            CancellationToken cancellationToken = default)
        {
            if (accountId <= 0 || recipeId <= 0 || quantity <= 0)
            {
                ErrorMessage = "Please select an account, product SKU, and enter a valid quantity.";
                return RedirectToPage(new { accountId });
            }

            var skus = await _salesService.GetCatalogSkusAsync(cancellationToken);
            var sku = skus.FirstOrDefault(s => s.Id == recipeId);
            if (sku == default)
            {
                ErrorMessage = "Selected product SKU was not found in catalog.";
                return RedirectToPage(new { accountId });
            }

            var items = new List<OrderItemDto>
            {
                new()
                {
                    RecipeId = sku.Id,
                    ProductName = sku.Name,
                    Quantity = quantity,
                    UnitPrice = sku.Price
                }
            };

            var ok = await _salesService.FulfillSupermarketOrderAsync(
                accountId,
                repId,
                items,
                notes ?? $"Direct store replenishment ({quantity}x {sku.Name})",
                cancellationToken);

            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? $"Supermarket order fulfilled successfully! {quantity}x {sku.Name} charged to account credit."
                : "Could not fulfill order. Please check account details.";

            return RedirectToPage(new { accountId });
        }

        public async Task<IActionResult> OnPostRecordPaymentAsync(
            int accountId,
            decimal amountPaid,
            string? paymentMethod,
            string? reference,
            CancellationToken cancellationToken = default)
        {
            if (accountId <= 0 || amountPaid <= 0)
            {
                ErrorMessage = "Please specify a valid payment amount.";
                return RedirectToPage(new { accountId });
            }

            var ok = await _salesService.RecordSupermarketPaymentAsync(
                accountId,
                amountPaid,
                paymentMethod ?? "BankTransfer",
                reference ?? "PAY-REC",
                cancellationToken);

            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? $"Payment of ₦{amountPaid:N2} credited to account and applied to the oldest open orders."
                : "Failed to process payment credit.";

            return RedirectToPage(new { accountId });
        }

        public async Task<IActionResult> OnPostSaveAccountAsync(
            [FromForm] SupermarketAccount account,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(account.AccountName))
            {
                ErrorMessage = "Account name is required.";
                return RedirectToPage();
            }

            var ok = await _salesService.SaveSupermarketAccountAsync(account, cancellationToken);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? $"Supermarket account '{account.AccountName}' saved successfully."
                : "Could not save account details.";

            return RedirectToPage(new { accountId = account.Id });
        }

        private async Task LoadDataAsync(int? accountId, CancellationToken cancellationToken)
        {
            Accounts = await _salesService.GetSupermarketAccountsAsync(cancellationToken: cancellationToken);
            CatalogSkus = await _salesService.GetCatalogSkusAsync(cancellationToken);
            SalesReps = await _salesService.GetSalesRepsAsync(cancellationToken: cancellationToken);

            if (accountId.HasValue && accountId.Value > 0)
            {
                SelectedAccount = Accounts.FirstOrDefault(a => a.Id == accountId.Value);
            }
            else if (Accounts.Any())
            {
                SelectedAccount = Accounts.First();
            }
        }
    }
}
