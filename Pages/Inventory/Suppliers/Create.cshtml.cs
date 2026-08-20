using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;

namespace Manufacture.Pages.Inventory.Suppliers
{
    public class CreateModel : PageModel
    {
        private readonly MockInventoryService _inventoryService;

        public CreateModel(MockInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [BindProperty]
        public SupplierDto SupplierInput { get; set; } = new();

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();

            var sup = _inventoryService.CreateSupplier(SupplierInput);
            TempData["SuccessMessage"] = $"Supplier '{sup.Name}' added to directory.";
            return RedirectToPage("/Inventory/Suppliers/Index");
        }
    }
}
