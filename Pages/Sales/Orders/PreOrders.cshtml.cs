using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Sales.Orders
{
    public class PreOrdersModel : PageModel
    {
        private readonly MockSalesService _salesService;

        public PreOrdersModel(MockSalesService salesService)
        {
            _salesService = salesService;
        }

        public List<Order> PreOrders { get; set; } = new();

        public void OnGet()
        {
            PreOrders = _salesService.GetPreOrders();
        }
    }
}
