using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Inventory.Items
{
    public class EditModel : PageModel
    {
        private readonly IInventoryService _inventoryService;

        public EditModel(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [BindProperty]
        public InventoryItemDto ItemInput { get; set; } = new();

        public List<SupplierDto> Suppliers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _inventoryService.GetItemAsync(id, cancellationToken);
            if (item == null)
            {
                TempData["ErrorMessage"] = "Warehouse inventory item not found.";
                return RedirectToPage("/Inventory/Index");
            }

            ItemInput = item;
            await LoadSuppliersAsync(cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                await LoadSuppliersAsync(cancellationToken);
                return Page();
            }

            try
            {
                var updated = await _inventoryService.UpdateItemAsync(id, ItemInput, cancellationToken);
                if (updated == null)
                {
                    TempData["ErrorMessage"] = "Item could not be updated or was not found.";
                    return RedirectToPage("/Inventory/Index");
                }

                TempData["SuccessMessage"] = $"Warehouse stock item '{updated.Name}' updated successfully.";
                return RedirectToPage("/Inventory/Items/Details", new { id = updated.Id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadSuppliersAsync(cancellationToken);
                return Page();
            }
        }

        private async Task LoadSuppliersAsync(CancellationToken cancellationToken)
        {
            Suppliers = await _inventoryService.GetSuppliersAsync(cancellationToken: cancellationToken);
        }
    }
}
