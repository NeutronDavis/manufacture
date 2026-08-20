using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Inventory.Suppliers
{
    public class IndexModel : PageModel
    {
        private readonly MockInventoryService _inventoryService;

        public IndexModel(MockInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        public List<Supplier> Suppliers { get; set; } = new();

        public void OnGet()
        {
            Suppliers = _inventoryService.GetAllSuppliers();
        }
    }
}
