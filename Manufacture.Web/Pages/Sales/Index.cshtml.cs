using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales
{
    public class IndexModel : PageModel
    {
        private readonly ISalesService _salesService;

        public IndexModel(ISalesService salesService)
        {
            _salesService = salesService;
        }

        public List<PosTerminal> Terminals { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();

        public string UserRole { get; set; } = string.Empty;

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            Terminals = await _salesService.GetTerminalsAsync(cancellationToken: cancellationToken);
            var orders = await _salesService.GetOrdersAsync(cancellationToken);
            RecentOrders = orders.Take(8).ToList();
        }
    }
}
