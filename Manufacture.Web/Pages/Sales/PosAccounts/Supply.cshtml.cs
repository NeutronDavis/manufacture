using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Manufacture.Services;

namespace Manufacture.Pages.Sales.PosAccounts
{
    public class SupplyModel : PageModel
    {
        private readonly MockSalesService _salesService;

        public SupplyModel(MockSalesService salesService)
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

        public void OnGet(int? id)
        {
            LoadData(id ?? Id);
        }

        public IActionResult OnPost(int accountId, int repId, int recipeId, int quantity, string? notes)
        {
            if (accountId <= 0 || recipeId <= 0 || quantity <= 0)
            {
                ErrorMessage = "Please select a supermarket account, product SKU, and enter a valid quantity.";
                LoadData(accountId);
                return Page();
            }

            var skus = _salesService.GetCatalogSkus();
            var sku = skus.FirstOrDefault(s => s.Id == recipeId);
            if (sku == default)
            {
                ErrorMessage = "Selected product SKU was not found in catalog.";
                LoadData(accountId);
                return Page();
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
                var account = _salesService.GetSupermarketAccountById(accountId);
                SuccessMessage = $"Supermarket supply recorded successfully! {quantity}x {sku.Name} charged to {account?.AccountName ?? "account"}.";
                return RedirectToPage("/Sales/PosAccounts/Edit", new { id = accountId });
            }

            ErrorMessage = "Could not fulfill direct supply order. Please verify account details.";
            LoadData(accountId);
            return Page();
        }

        private void LoadData(int? accountId)
        {
            Accounts = _salesService.GetAllSupermarketAccounts().Where(a => a.IsActive).ToList();
            CatalogSkus = _salesService.GetCatalogSkus();
            SalesReps = _salesService.GetAllReps();

            if (accountId.HasValue && accountId.Value > 0)
            {
                SelectedAccount = _salesService.GetSupermarketAccountById(accountId.Value);
            }
            else if (Accounts.Any())
            {
                SelectedAccount = Accounts.First();
            }
        }
    }
}
