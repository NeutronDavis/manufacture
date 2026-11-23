using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.PreOrderSchedule
{
    public class DetailsModel : PageModel
    {
        private readonly ISalesService _salesService;

        public DetailsModel(ISalesService salesService)
        {
            _salesService = salesService;
        }

        public Order Order { get; set; } = new();
        public Customer? PortalCustomer { get; set; }

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken = default)
        {
            var role = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            if (!string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Pre-order details are available to customer portal accounts only.";
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
                TempData["ErrorMessage"] = "This customer login is not linked to a customer account.";
                return RedirectToPage("/Sales/PreOrderSchedule/Index");
            }

            var order = await _salesService.GetOrderAsync(id, cancellationToken);
            if (order == null || order.CustomerId != PortalCustomer.Id || order.Type != OrderType.PreOrder)
            {
                TempData["ErrorMessage"] = "That pre-order was not found for your customer account.";
                return RedirectToPage("/Sales/PreOrderSchedule/Index");
            }

            Order = order;
            return Page();
        }
    }
}
