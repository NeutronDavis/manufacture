using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.Orders
{
    public class ReceiptModel : PageModel
    {
        private readonly ISalesService _salesService;

        public ReceiptModel(ISalesService salesService)
        {
            _salesService = salesService;
        }

        public Order Order { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken = default)
        {
            var order = await _salesService.GetOrderAsync(id, cancellationToken);
            if (order == null)
            {
                return RedirectToPage("/Sales/Orders/Index");
            }

            Order = order;
            return Page();
        }

        public async Task<IActionResult> OnPostMarkPrintedAsync(int id, CancellationToken cancellationToken = default)
        {
            await _salesService.MarkPrintedForLoadingAsync(id, cancellationToken);
            TempData["SuccessMessage"] = "Order slip marked as printed. Loading authorized.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDispatchOrderAsync(
            int id,
            string loaderName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(loaderName))
            {
                TempData["ErrorMessage"] = "Loader name is required to complete dispatch sign-off.";
                return RedirectToPage(new { id });
            }

            var success = await _salesService.VerifyAndDispatchAsync(id, loaderName, cancellationToken);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? $"Stock verified and dispatched by {loaderName}. Order status updated to Dispatched."
                : "Cannot dispatch order: order slip must be printed first.";

            return RedirectToPage(new { id });
        }
    }
}
