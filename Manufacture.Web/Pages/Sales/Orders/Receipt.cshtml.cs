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
        public bool AutoPrint { get; set; }

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken = default)
        {
            var order = await _salesService.GetOrderAsync(id, cancellationToken);
            if (order == null)
            {
                return RedirectToPage("/Sales/Orders/Index");
            }

            Order = order;
            AutoPrint = TempData["AutoPrint"] != null;
            return Page();
        }

        public async Task<IActionResult> OnPostPrintAndDeductAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = HttpContext.Session.GetString("UserName") ?? "Sales";
                var success = await _salesService.DeductAndCompleteOrderAsync(id, actor, cancellationToken);
                if (!success)
                {
                    TempData["ErrorMessage"] = "Order was not found.";
                    return RedirectToPage(new { id });
                }

                TempData["SuccessMessage"] = "Receipt printed. Stock was deducted from the POS repository.";
                TempData["AutoPrint"] = true;
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage(new { id });
        }
    }
}
