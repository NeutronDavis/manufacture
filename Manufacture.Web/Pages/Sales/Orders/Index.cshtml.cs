using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.Orders
{
    public class IndexModel : PageModel
    {
        private readonly ISalesService _salesService;

        public IndexModel(ISalesService salesService)
        {
            _salesService = salesService;
        }

        public List<Order> Orders { get; set; } = new();
        public bool IsSalesRep { get; set; }
        public SalesRepProfile? CurrentSalesRep { get; set; }
        public string UserRole { get; set; } = string.Empty;

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            IsSalesRep = string.Equals(UserRole, "SalesRep", StringComparison.OrdinalIgnoreCase);

            if (IsSalesRep)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    CurrentSalesRep = await _salesService.GetSalesRepByUserIdAsync(userId.Value, cancellationToken);
                }
            }

            var orders = await _salesService.GetOrdersAsync(cancellationToken);
            Orders = IsSalesRep
                ? orders.Where(o => o.SalesRepId == null || o.SalesRepId == CurrentSalesRep?.Id).ToList()
                : orders;
        }
    }
}
