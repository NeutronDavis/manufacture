using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Sales.Orders
{
    public class IndexModel : PageModel
    {
        private readonly MockSalesService _salesService;

        public IndexModel(MockSalesService salesService)
        {
            _salesService = salesService;
        }

        public List<Order> Orders { get; set; } = new();

        public void OnGet()
        {
            Orders = _salesService.GetAllOrders();
        }
    }
}
