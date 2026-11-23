using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
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
        public string UserRole { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            if (!string.Equals(UserRole, "Customer", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "The Pre-Order Schedule is available to customer portal accounts only.";
                return RedirectToPage("/Sales/Orders/Index");
            }

            var userId = HttpContext.Session.GetInt32("UserId");
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
