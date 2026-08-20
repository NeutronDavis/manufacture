using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Sales.Customers
{
    public class IndexModel : PageModel
    {
        private readonly MockSalesService _salesService;

        public IndexModel(MockSalesService salesService)
        {
            _salesService = salesService;
        }

        public List<Customer> Customers { get; set; } = new();

        public void OnGet()
        {
            Customers = _salesService.GetAllCustomers();
        }
    }
}
