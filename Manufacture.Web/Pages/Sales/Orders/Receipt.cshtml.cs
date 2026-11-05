using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Sales.Orders
{
    public class ReceiptModel : PageModel
    {
        private readonly MockSalesService _salesService;

        public ReceiptModel(MockSalesService salesService)
        {
            _salesService = salesService;
        }

        public Order Order { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var order = _salesService.GetOrderById(id);
            if (order == null) return RedirectToPage("/Sales/Orders/Index");

            Order = order;
            return Page();
        }

        public IActionResult OnPostMarkPrinted(int id)
        {
            _salesService.MarkPrintedForLoading(id);
            TempData["SuccessMessage"] = "Order slip marked as printed. Loading authorized.";
            return RedirectToPage(new { id });
        }

        public IActionResult OnPostDispatchOrder(int id, string loaderName)
        {
            if (string.IsNullOrWhiteSpace(loaderName))
            {
                TempData["ErrorMessage"] = "Loader name is required to complete dispatch sign-off.";
                return RedirectToPage(new { id });
            }

            var success = _salesService.VerifyAndDispatchByLoader(id, loaderName);
            if (success)
            {
                TempData["SuccessMessage"] = $"Stock verified and dispatched by {loaderName}. Order status updated to Dispatched.";
            }
            else
            {
                TempData["ErrorMessage"] = "Cannot dispatch order: order slip must be printed first.";
            }

            return RedirectToPage(new { id });
        }
    }
}
