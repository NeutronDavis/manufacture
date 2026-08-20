using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Inventory.Items
{
    public class CreateModel : PageModel
    {
        private readonly MockInventoryService _inventoryService;

        public CreateModel(MockInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [BindProperty]
        public InventoryItemDto ItemInput { get; set; } = new();

        public List<Supplier> Suppliers { get; set; } = new();

        public void OnGet()
        {
            Suppliers = _inventoryService.GetAllSuppliers();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                Suppliers = _inventoryService.GetAllSuppliers();
                return Page();
            }

            var item = _inventoryService.CreateItem(ItemInput);
            TempData["SuccessMessage"] = $"Stock item '{item.Name}' added to warehouse inventory.";
            return RedirectToPage("/Inventory/Index");
        }
    }
}
