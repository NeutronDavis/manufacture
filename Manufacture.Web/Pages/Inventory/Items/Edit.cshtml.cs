using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Inventory.Items
{
    public class EditModel : PageModel
    {
        private readonly MockInventoryService _inventoryService;

        public EditModel(MockInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [BindProperty]
        public InventoryItemDto ItemInput { get; set; } = new();

        public List<Supplier> Suppliers { get; set; } = new();
        public InventoryItem OriginalItem { get; set; } = default!;

        public IActionResult OnGet(int id)
        {
            var item = _inventoryService.GetItemById(id);
            if (item == null)
            {
                TempData["ErrorMessage"] = "Warehouse inventory item not found.";
                return RedirectToPage("/Inventory/Index");
            }

            OriginalItem = item;
            Suppliers = _inventoryService.GetAllSuppliers();

            ItemInput = new InventoryItemDto
            {
                Id = item.Id,
                ItemCode = item.ItemCode,
                Name = item.Name,
                Category = item.Category,
                Unit = item.Unit,
                QuantityInStock = item.QuantityInStock,
                ReorderThreshold = item.ReorderThreshold,
                UnitCostPrice = item.UnitCostPrice,
                PreferredSupplierId = item.PreferredSupplierId
            };

            return Page();
        }

        public IActionResult OnPost(int id)
        {
            if (!ModelState.IsValid)
            {
                var item = _inventoryService.GetItemById(id);
                if (item != null) OriginalItem = item;
                Suppliers = _inventoryService.GetAllSuppliers();
                return Page();
            }

            var updated = _inventoryService.UpdateItem(id, ItemInput);
            if (updated == null)
            {
                TempData["ErrorMessage"] = "Item could not be updated or was not found.";
                return RedirectToPage("/Inventory/Index");
            }

            TempData["SuccessMessage"] = $"Warehouse stock item '{updated.Name}' updated successfully.";
            return RedirectToPage("/Inventory/Items/Details", new { id = updated.Id });
        }
    }
}
