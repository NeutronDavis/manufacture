using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Inventory.Items
{
    public class CreateModel : PageModel
    {
        private readonly IInventoryService _inventoryService;

        public CreateModel(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [BindProperty]
        public InventoryItemDto ItemInput { get; set; } = new();

        public List<SupplierDto> Suppliers { get; set; } = new();

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            await LoadSuppliersAsync(cancellationToken);
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                await LoadSuppliersAsync(cancellationToken);
                return Page();
            }

            try
            {
                var userName = HttpContext.Session.GetString("UserName") ?? "Store Manager";
                var item = await _inventoryService.CreateItemAsync(ItemInput, userName, cancellationToken);
                TempData["SuccessMessage"] = $"Stock item '{item.Name}' added to warehouse inventory with code {item.ItemCode}.";
                return RedirectToPage("/Inventory/Index");
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
