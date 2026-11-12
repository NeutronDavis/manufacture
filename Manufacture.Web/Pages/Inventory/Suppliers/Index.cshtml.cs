using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Inventory.Suppliers
{
    public class IndexModel : PageModel
    {
        private readonly IInventoryService _inventoryService;

        public IndexModel(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        public List<SupplierDto> Suppliers { get; set; } = new();

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            Suppliers = await _inventoryService.GetSuppliersAsync(cancellationToken: cancellationToken);
        }
    }
}
