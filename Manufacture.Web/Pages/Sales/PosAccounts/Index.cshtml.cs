using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Manufacture.Services;

namespace Manufacture.Pages.Sales.PosAccounts
{
    public class IndexModel : PageModel
    {
        private readonly MockSalesService _salesService;

        public IndexModel(MockSalesService salesService)
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

        public void OnGet(int? accountId)
        {
            LoadData(accountId);
        }

        public IActionResult OnPostSavePrices(int accountId, Dictionary<int, decimal> prices)
        {
            if (accountId <= 0)
            {
                ErrorMessage = "Invalid account specified.";
                return RedirectToPage(new { accountId });
            }

            var ok = _salesService.SetBatchCustomAgreedPrices(accountId, prices ?? new());
            if (ok)
            {
                SuccessMessage = "Custom agreed prices successfully updated for this supermarket account.";
            }
            else
            {
                ErrorMessage = "Failed to update custom agreed prices.";
            }

            return RedirectToPage(new { accountId });
        }

        public IActionResult OnPostFulfillOrder(int accountId, int repId, int recipeId, int quantity, string? notes)
        {
            if (accountId <= 0 || recipeId <= 0 || quantity <= 0)
            {
                ErrorMessage = "Please select an account, product SKU, and enter a valid quantity.";
                return RedirectToPage(new { accountId });
            }

            var skus = _salesService.GetCatalogSkus();
            var sku = skus.FirstOrDefault(s => s.Id == recipeId);
            if (sku == default)
            {
                ErrorMessage = "Selected product SKU was not found in catalog.";
                return RedirectToPage(new { accountId });
            }

            var items = new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    RecipeId = sku.Id,
                    ProductName = sku.Name,
                    Quantity = quantity,
                    UnitPrice = sku.Price
                }
            };

            var ok = _salesService.FulfillSupermarketOrder(accountId, repId, items, notes ?? $"Direct store replenishment ({quantity}x {sku.Name})");
            if (ok)
            {
                SuccessMessage = $"Supermarket order fulfilled successfully! {quantity}x {sku.Name} charged to account credit.";
            }
            else
            {
                ErrorMessage = "Could not fulfill order. Please check account details.";
            }

            return RedirectToPage(new { accountId });
        }

        public IActionResult OnPostRecordPayment(int accountId, decimal amountPaid, string? paymentMethod, string? reference)
        {
            if (accountId <= 0 || amountPaid <= 0)
            {
                ErrorMessage = "Please specify a valid payment amount.";
                return RedirectToPage(new { accountId });
            }

            var ok = _salesService.RecordSupermarketPayment(accountId, amountPaid, paymentMethod ?? "BankTransfer", reference ?? "PAY-REC");
            if (ok)
            {
                SuccessMessage = $"Payment of ₦{amountPaid:N2} credited to account. Outstanding balance updated.";
            }
            else
            {
                ErrorMessage = "Failed to process payment credit.";
            }

            return RedirectToPage(new { accountId });
        }

        public IActionResult OnPostSaveAccount([FromForm] SupermarketAccount account)
        {
            if (string.IsNullOrWhiteSpace(account.AccountName))
            {
                ErrorMessage = "Account name is required.";
                return RedirectToPage();
            }

            var ok = _salesService.SaveSupermarketAccount(account);
            if (ok)
            {
                SuccessMessage = $"Supermarket account '{account.AccountName}' saved successfully.";
                return RedirectToPage(new { accountId = account.Id });
            }

            ErrorMessage = "Could not save account details.";
            return RedirectToPage();
        }

        private void LoadData(int? accountId)
        {
            Accounts = _salesService.GetAllSupermarketAccounts();
            CatalogSkus = _salesService.GetCatalogSkus();
            SalesReps = _salesService.GetAllReps();

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
