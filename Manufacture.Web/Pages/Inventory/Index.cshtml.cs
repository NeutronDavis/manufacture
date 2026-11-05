using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Inventory
{
    public class IndexModel : PageModel
    {
        private readonly MockInventoryService _inventoryService;

        public IndexModel(MockInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        public List<InventoryItem> Items { get; set; } = new();
        public int LowStockCount { get; set; }
        public int PendingRequestsCount { get; set; }

        public void OnGet()
        {
            Items = _inventoryService.GetAllItems();
            LowStockCount = _inventoryService.GetLowStockItems().Count;
            PendingRequestsCount = _inventoryService.GetAllRequests().Count(r => r.Status == StoreRequestStatus.Pending);
        }

        public IActionResult OnPostRestock(int id, decimal quantity)
        {
            if (quantity > 0)
            {
                _inventoryService.RestockItem(id, quantity);
                TempData["SuccessMessage"] = $"Added +{quantity} units to inventory stock.";
            }
            return RedirectToPage();
        }
    }
}
