using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.PosAccounts
{
    public class SupplyModel : PageModel
    {
        private readonly ISalesService _salesService;

        public SupplyModel(ISalesService salesService)
        {
            _salesService = salesService;
        }

        public List<SupermarketAccount> Accounts { get; set; } = new();
        public List<(int Id, string Code, string Name, ProductType Category, decimal Price)> CatalogSkus { get; set; } = new();
        public List<SalesRepProfile> SalesReps { get; set; } = new();
        public SupermarketAccount? SelectedAccount { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Id { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync(int? id, CancellationToken cancellationToken = default)
        {
            await LoadDataAsync(id ?? Id, cancellationToken);
        }

        public async Task<IActionResult> OnPostAsync(
            int accountId,
            int repId,
            int recipeId,
            int quantity,
            string? notes,
            CancellationToken cancellationToken = default)
        {
            if (accountId <= 0 || recipeId <= 0 || quantity <= 0)
            {
                ErrorMessage = "Please select a supermarket account, product SKU, and enter a valid quantity.";
                await LoadDataAsync(accountId, cancellationToken);
                return Page();
            }

            var skus = await _salesService.GetCatalogSkusAsync(cancellationToken);
            var sku = skus.FirstOrDefault(s => s.Id == recipeId);
            if (sku == default)
            {
                ErrorMessage = "Selected product SKU was not found in catalog.";
                await LoadDataAsync(accountId, cancellationToken);
                return Page();
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

            if (ok)
            {
                var account = await _salesService.GetSupermarketAccountAsync(accountId, cancellationToken);
                SuccessMessage = $"Supermarket supply recorded successfully! {quantity}x {sku.Name} charged to {account?.AccountName ?? "account"}.";
                return RedirectToPage("/Sales/PosAccounts/Edit", new { id = accountId });
            }

            ErrorMessage = "Could not fulfill direct supply order. Please verify account details.";
            await LoadDataAsync(accountId, cancellationToken);
            return Page();
        }

        private async Task LoadDataAsync(int? accountId, CancellationToken cancellationToken)
        {
            Accounts = (await _salesService.GetSupermarketAccountsAsync(cancellationToken: cancellationToken))
                .Where(a => a.IsActive)
                .ToList();
            CatalogSkus = await _salesService.GetCatalogSkusAsync(cancellationToken);
            SalesReps = await _salesService.GetSalesRepsAsync(cancellationToken: cancellationToken);

            if (accountId.HasValue && accountId.Value > 0)
            {
                SelectedAccount = await _salesService.GetSupermarketAccountAsync(accountId.Value, cancellationToken);
            }
            else if (Accounts.Any())
            {
                SelectedAccount = Accounts.First();
            }
        }
    }
}
