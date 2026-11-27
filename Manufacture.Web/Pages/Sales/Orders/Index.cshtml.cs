using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Services;
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
        public bool IsProductionManager { get; set; }
        public bool CanCreatePosOrder => !IsProductionManager;
        public SalesRepProfile? CurrentSalesRep { get; set; }
        public int UserRoleId { get; set; }

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            UserRoleId = HttpContext.Session.GetRoleId();
            IsSalesRep = UserRoleId == (int)UserRole.SalesRep;
            IsProductionManager = UserRoleId == (int)UserRole.ProductionManager;

            if (IsSalesRep)
            {
                var userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
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
