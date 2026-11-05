using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Sales
{
    public class IndexModel : PageModel
    {
        private readonly MockSalesService _salesService;

        public IndexModel(MockSalesService salesService)
        {
            _salesService = salesService;
        }

        public List<PosTerminal> Terminals { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();

        public void OnGet()
        {
            Terminals = _salesService.GetAllTerminals();
            RecentOrders = _salesService.GetAllOrders().Take(8).ToList();
        }
    }
}
