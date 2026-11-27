using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.PreOrderSchedule
{
    public class IndexModel : PageModel
    {
        private readonly ISalesService _salesService;

        public IndexModel(ISalesService salesService)
        {
            _salesService = salesService;
        }

        public List<Order> PreOrders { get; set; } = new();
        public Customer? PortalCustomer { get; set; }
        public int UserRoleId { get; set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
        {
            UserRoleId = HttpContext.Session.GetRoleId();
            if (UserRoleId != (int)UserRole.Customer)
            {
                TempData["ErrorMessage"] = "The Pre-Order Schedule is available to customer portal accounts only.";
                return RedirectToPage("/Sales/Orders/Index");
            }

            var userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!userId.HasValue)
            {
                return RedirectToPage("/Login");
            }

            PortalCustomer = await _salesService.GetCustomerByUserIdAsync(userId.Value, cancellationToken);
            if (PortalCustomer == null)
            {
                return Page();
            }

            PreOrders = (await _salesService.GetPreOrdersAsync(PortalCustomer.Id, cancellationToken))
                .Where(o => o.Status != OrderStatus.Cancelled)
                .OrderBy(o => o.TargetDeliveryDate)
                .ThenBy(o => o.Id)
                .ToList();
            return Page();
        }
    }
}
