using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.Orders
{
    public class DetailsModel : PageModel
    {
        private readonly ISalesService _salesService;

        public DetailsModel(ISalesService salesService)
        {
            _salesService = salesService;
        }

        public Order Order { get; set; } = new();
        public List<SalesRepProfile> SalesReps { get; set; } = new();

        [BindProperty]
        public OrderPaymentInputDto PaymentInput { get; set; } = new() { PaymentMethod = "Cash" };

        [BindProperty]
        public int? SelectedSalesRepId { get; set; }

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken = default)
        {
            var order = await _salesService.GetOrderAsync(id, cancellationToken);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToPage("/Sales/Orders/Index");
            }

            Order = order;
            PaymentInput.OrderId = order.Id;
            PaymentInput.Amount = Math.Max(0m, order.BalanceDue);
            SalesReps = await _salesService.GetSalesRepsAsync(cancellationToken: cancellationToken);
            SelectedSalesRepId = order.SalesRepId;
            return Page();
        }

        public async Task<IActionResult> OnPostAssignRepAsync(int id, int? selectedSalesRepId, CancellationToken cancellationToken = default)
        {
            try
            {
                var assigned = await _salesService.AssignOrderSalesRepAsync(id, selectedSalesRepId, cancellationToken);
                TempData[assigned ? "SuccessMessage" : "ErrorMessage"] = assigned
                    ? "Sales representative assigned to this order."
                    : "Order could not be assigned.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage(new { id });
        }

        public Task<IActionResult> OnPostAsync()
        {
            return OnPostApplyPaymentAsync();
        }

        public async Task<IActionResult> OnPostApplyPaymentAsync(CancellationToken cancellationToken = default)
        {
            PaymentInput.ReceivedBy = HttpContext.Session.GetString("UserName") ?? "Sales";

            try
            {
                var applied = await _salesService.ApplyOrderPaymentAsync(PaymentInput, cancellationToken);
                TempData[applied ? "SuccessMessage" : "ErrorMessage"] = applied
                    ? $"Payment of ₦{PaymentInput.Amount:N2} applied to order #{PaymentInput.OrderId}."
                    : "Payment could not be applied. The order may already be fully settled.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage(new { id = PaymentInput.OrderId });
        }
    }
}
