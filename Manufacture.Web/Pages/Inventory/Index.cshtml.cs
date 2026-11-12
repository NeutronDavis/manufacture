using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Inventory
{
    public class IndexModel : PageModel
    {
        private readonly IInventoryService _inventoryService;

        public IndexModel(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        public List<InventoryItemDto> Items { get; set; } = new();
        public int LowStockCount { get; set; }
        public int PendingRequestsCount { get; set; }

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            Items = await _inventoryService.GetItemsAsync(cancellationToken: cancellationToken);
            LowStockCount = await _inventoryService.GetLowStockCountAsync(cancellationToken);
            PendingRequestsCount = await _inventoryService.GetPendingRequestCountAsync(cancellationToken);
        }

        public async Task<IActionResult> OnPostRestockAsync(
            int id,
            decimal quantity,
            CancellationToken cancellationToken = default)
        {
            if (quantity > 0)
            {
                var userName = HttpContext.Session.GetString("UserName") ?? "Store Manager";
                var restocked = await _inventoryService.RestockItemAsync(id, quantity, userName, cancellationToken);
                TempData[restocked ? "SuccessMessage" : "ErrorMessage"] = restocked
                    ? $"Added +{quantity:N1} units to inventory stock."
                    : "The inventory item could not be restocked.";
            }

            return RedirectToPage();
        }
    }
}
