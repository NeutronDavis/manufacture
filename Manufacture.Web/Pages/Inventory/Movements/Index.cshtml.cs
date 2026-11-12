using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Inventory.Movements
{
    public class IndexModel : PageModel
    {
        private readonly IInventoryService _inventoryService;

        public IndexModel(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        public List<StockMovementDto> Movements { get; set; } = new();
        public List<InventoryItemDto> Items { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? ItemId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? MovementType { get; set; }

        public decimal TotalReceipts => Movements
            .Where(m => m.MovementType == "Receipt")
            .Sum(m => m.Quantity);

        public decimal TotalIssues => Movements
            .Where(m => m.MovementType == "Issue")
            .Sum(m => m.Quantity);

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            Items = await _inventoryService.GetItemsAsync(includeInactive: true, cancellationToken);
            Movements = await _inventoryService.GetStockMovementsAsync(ItemId, MovementType, 250, cancellationToken);
        }
    }
}
