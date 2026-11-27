using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Services;
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
        public int UserRoleId { get; set; }
        public bool IsProductionManager =>
            UserRoleId == (int)UserRole.ProductionManager;
        public bool CanManageSales => !IsProductionManager;
        public bool IsSalesRep =>
            UserRoleId == (int)UserRole.SalesRep;
        public bool IsSuperAdmin =>
            UserRoleId == (int)UserRole.SuperAdmin;
        public bool IsOrderOpen => IsOpenStatus(Order.Status);
        public bool CanAcceptOrder => IsSalesRep && IsOrderOpen && Order.SalesRepId is null;
        public bool CanAssignRep => IsSuperAdmin && IsOrderOpen && Order.SalesRepId is null;

        [BindProperty]
        public OrderPaymentInputDto PaymentInput { get; set; } = new() { PaymentMethod = "Cash" };

        [BindProperty]
        public int? SelectedSalesRepId { get; set; }

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken = default)
        {
            LoadRole();

            var order = await _salesService.GetOrderAsync(id, cancellationToken);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToPage("/Sales/Orders/Index");
            }

            Order = order;
            PaymentInput.OrderId = order.Id;
            PaymentInput.Amount = Math.Max(0m, order.BalanceDue);
            SalesReps = IsSuperAdmin
                ? await _salesService.GetSalesRepsAsync(cancellationToken: cancellationToken)
                : new List<SalesRepProfile>();
            SelectedSalesRepId = order.SalesRepId;
            return Page();
        }

        public async Task<IActionResult> OnPostAssignRepAsync(int id, int? selectedSalesRepId, CancellationToken cancellationToken = default)
        {
            LoadRole();

            if (!IsSuperAdmin)
            {
                TempData["ErrorMessage"] = "Only a Super Admin can assign a sales representative.";
                return RedirectToPage(new { id });
            }

            var order = await _salesService.GetOrderAsync(id, cancellationToken);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToPage("/Sales/Orders/Index");
            }

            if (!IsOpenStatus(order.Status))
            {
                TempData["ErrorMessage"] = "This order is closed and can no longer be assigned.";
                return RedirectToPage(new { id });
            }

            if (order.SalesRepId.HasValue)
            {
                TempData["ErrorMessage"] = "This order already has an assigned sales representative.";
                return RedirectToPage(new { id });
            }

            if (!selectedSalesRepId.HasValue || selectedSalesRepId.Value <= 0)
            {
                TempData["ErrorMessage"] = "Select a sales representative to assign.";
                return RedirectToPage(new { id });
            }

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

        /// <summary>
        /// A sales rep accepts an open, unassigned order, which assigns that rep
        /// to it. The selected rep is always the signed-in rep, never posted data.
        /// </summary>
        public async Task<IActionResult> OnPostAcceptOrderAsync(int id, CancellationToken cancellationToken = default)
        {
            LoadRole();

            if (!IsSalesRep)
            {
                TempData["ErrorMessage"] = "Only a sales representative can accept an order.";
                return RedirectToPage(new { id });
            }

            var order = await _salesService.GetOrderAsync(id, cancellationToken);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToPage("/Sales/Orders/Index");
            }

            if (!IsOpenStatus(order.Status) || order.SalesRepId.HasValue)
            {
                TempData["ErrorMessage"] = "This order is no longer available to accept.";
                return RedirectToPage(new { id });
            }

            var userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            var currentRep = userId.HasValue
                ? await _salesService.GetSalesRepByUserIdAsync(userId.Value, cancellationToken)
                : null;

            if (currentRep == null)
            {
                TempData["ErrorMessage"] = "Your sales rep login is not linked to an active sales representative profile.";
                return RedirectToPage(new { id });
            }

            var accepted = await _salesService.AssignOrderSalesRepAsync(id, currentRep.Id, cancellationToken);
            TempData[accepted ? "SuccessMessage" : "ErrorMessage"] = accepted
                ? $"Order {order.OrderNumber} accepted and assigned to you."
                : "This order is no longer available to accept.";

            return RedirectToPage(new { id });
        }

        public Task<IActionResult> OnPostAsync()
        {
            return OnPostApplyPaymentAsync();
        }

        public async Task<IActionResult> OnPostApplyPaymentAsync(CancellationToken cancellationToken = default)
        {
            LoadRole();

            if (!CanManageSales)
            {
                TempData["ErrorMessage"] = "Production Managers cannot apply order payments or settlements.";
                return RedirectToPage(new { id = PaymentInput.OrderId });
            }

            PaymentInput.ReceivedBy = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Sales";

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

        private static bool IsOpenStatus(OrderStatus status) =>
            status is not (OrderStatus.Completed or OrderStatus.Cancelled);

        private void LoadRole()
        {
            UserRoleId = HttpContext.Session.GetRoleId();
        }
    }
}
