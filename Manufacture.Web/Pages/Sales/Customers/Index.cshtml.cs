using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.Customers
{
    public class IndexModel : PageModel
    {
        private readonly ISalesService _salesService;

        public IndexModel(ISalesService salesService)
        {
            _salesService = salesService;
        }

        public List<Customer> Customers { get; set; } = new();

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            Customers = await _salesService.GetCustomersAsync(cancellationToken: cancellationToken);
        }
    }
}
