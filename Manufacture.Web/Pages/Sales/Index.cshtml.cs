using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Microsoft.AspNetCore.Mvc;
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

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? string.Empty;

            // Sales reps do not use the Sales hub; they work from the order register.
            if (string.Equals(UserRole, "SalesRep", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToPage("/Sales/Orders/Index");
            }

            Terminals = await _salesService.GetTerminalsAsync(cancellationToken: cancellationToken);
            var orders = await _salesService.GetOrdersAsync(cancellationToken);
            RecentOrders = orders.Take(8).ToList();
            return Page();
        }
    }
}
