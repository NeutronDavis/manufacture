using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Inventory.Items
{
    public class DetailsModel : PageModel
    {
        private readonly IInventoryService _inventoryService;

        public DetailsModel(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        public InventoryItemDto Item { get; set; } = default!;
        public List<StoreRequestDto> ItemRequests { get; set; } = new();
        public List<InventoryRecipeUsageDto> AssociatedRecipes { get; set; } = new();
        public decimal TotalStockValuation => Item != null ? Item.QuantityInStock * Item.UnitCostPrice : 0m;

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _inventoryService.GetItemAsync(id, cancellationToken);
            if (item == null)
            {
                TempData["ErrorMessage"] = "Warehouse inventory item not found.";
                return RedirectToPage("/Inventory/Index");
            }

            Item = item;
            ItemRequests = await _inventoryService.GetRequestsByItemAsync(id, cancellationToken);
            AssociatedRecipes = await _inventoryService.GetRecipeUsageAsync(id, cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostRestockAsync(
            int id,
            decimal addedQuantity,
            CancellationToken cancellationToken = default)
        {
            if (addedQuantity <= 0)
            {
                TempData["ErrorMessage"] = "Restock quantity must be greater than zero.";
                return RedirectToPage(new { id });
            }

            var item = await _inventoryService.GetItemAsync(id, cancellationToken);
            if (item == null)
            {
                TempData["ErrorMessage"] = "Item not found.";
                return RedirectToPage("/Inventory/Index");
            }

            var userName = HttpContext.Session.GetString("UserName") ?? "Store Manager";
            var restocked = await _inventoryService.RestockItemAsync(id, addedQuantity, userName, cancellationToken);
            if (!restocked)
            {
                TempData["ErrorMessage"] = "The inventory item could not be restocked.";
                return RedirectToPage(new { id });
            }

            var updated = await _inventoryService.GetItemAsync(id, cancellationToken);
            TempData["SuccessMessage"] = $"Stock replenished: Added +{addedQuantity:N0} {item.Unit} to '{item.Name}'. New balance: {updated?.QuantityInStock ?? item.QuantityInStock:N0} {item.Unit}.";
            return RedirectToPage(new { id });
        }
    }
}
