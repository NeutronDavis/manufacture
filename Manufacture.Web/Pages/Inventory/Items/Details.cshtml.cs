using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Inventory.Items
{
    public class DetailsModel : PageModel
    {
        private readonly MockInventoryService _inventoryService;
        private readonly MockProductionService _productionService;

        public DetailsModel(MockInventoryService inventoryService, MockProductionService productionService)
        {
            _inventoryService = inventoryService;
            _productionService = productionService;
        }

        public InventoryItem Item { get; set; } = default!;
        public List<StoreRequest> ItemRequests { get; set; } = new();
        public List<Recipe> AssociatedRecipes { get; set; } = new();
        public decimal TotalStockValuation => Item != null ? Item.QuantityInStock * Item.UnitCostPrice : 0m;

        public IActionResult OnGet(int id)
        {
            var item = _inventoryService.GetItemById(id);
            if (item == null)
            {
                TempData["ErrorMessage"] = "Warehouse inventory item not found.";
                return RedirectToPage("/Inventory/Index");
            }

            Item = item;
            ItemRequests = _inventoryService.GetRequestsByItemId(id);

            // Find recipes that consume this raw material / packaging
            var allRecipes = _productionService.GetAllRecipes(includeInactive: true);
            var itemNameLower = item.Name.ToLower();
            AssociatedRecipes = allRecipes.Where(r => 
                r.Ingredients.Any(i => itemNameLower.Contains(i.IngredientName.ToLower()) || 
                                       i.IngredientName.ToLower().Contains(itemNameLower) ||
                                       (item.ItemCode.Contains("FLR") && i.IngredientName.ToLower().Contains("flour")) ||
                                       (item.ItemCode.Contains("SGR") && i.IngredientName.ToLower().Contains("sugar")) ||
                                       (item.ItemCode.Contains("YST") && i.IngredientName.ToLower().Contains("yeast")) ||
                                       (item.ItemCode.Contains("FAT") && i.IngredientName.ToLower().Contains("shortening")) ||
                                       (item.ItemCode.Contains("SLT") && i.IngredientName.ToLower().Contains("salt")) ||
                                       (item.ItemCode.Contains("JMB") && i.IngredientName.ToLower().Contains("jumbo")) ||
                                       (item.ItemCode.Contains("MED") && i.IngredientName.ToLower().Contains("medium")) ||
                                       (item.ItemCode.Contains("189") && i.IngredientName.ToLower().Contains("bottle")) ||
                                       (item.ItemCode.Contains("POP") && i.IngredientName.ToLower().Contains("popcorn")) ||
                                       (item.ItemCode.Contains("CRN") && i.IngredientName.ToLower().Contains("maize")) ||
                                       (item.ItemCode.Contains("OIL") && i.IngredientName.ToLower().Contains("oil"))
                )
            ).ToList();

            return Page();
        }

        public IActionResult OnPostRestock(int id, decimal addedQuantity)
        {
            if (addedQuantity <= 0)
            {
                TempData["ErrorMessage"] = "Restock quantity must be greater than zero.";
                return RedirectToPage(new { id });
            }

            var item = _inventoryService.GetItemById(id);
            if (item == null)
            {
                TempData["ErrorMessage"] = "Item not found.";
                return RedirectToPage("/Inventory/Index");
            }

            _inventoryService.RestockItem(id, addedQuantity);
            TempData["SuccessMessage"] = $"Stock replenished: Added +{addedQuantity:N0} {item.Unit} to '{item.Name}'. New balance: {item.QuantityInStock:N0} {item.Unit}.";
            return RedirectToPage(new { id });
        }
    }
}
