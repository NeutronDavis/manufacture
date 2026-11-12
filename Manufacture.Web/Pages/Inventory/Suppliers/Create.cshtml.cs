using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Inventory.Suppliers
{
    public class CreateModel : PageModel
    {
        private readonly IInventoryService _inventoryService;

        public CreateModel(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [BindProperty]
        public SupplierDto SupplierInput { get; set; } = new();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var supplier = await _inventoryService.CreateSupplierAsync(SupplierInput, cancellationToken);
                TempData["SuccessMessage"] = $"Supplier '{supplier.Name}' added to directory.";
                return RedirectToPage("/Inventory/Suppliers/Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }
}
