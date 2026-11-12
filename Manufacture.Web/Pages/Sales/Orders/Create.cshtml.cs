using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.Orders
{
    public class CreateModel : PageModel
    {
        private readonly ISalesService _salesService;

        public CreateModel(ISalesService salesService)
        {
            _salesService = salesService;
        }

        [BindProperty]
        public CreateOrderDto OrderInput { get; set; } = new();

        public List<Customer> Customers { get; set; } = new();
        public List<PosTerminal> Terminals { get; set; } = new();
        public List<Recipe> Recipes { get; set; } = new();
        public List<SalesRepProfile> SalesReps { get; set; } = new();
        public List<BonusRule> BonusRules { get; set; } = new();

        public bool IsCustomer { get; set; }
        public bool IsSalesRep { get; set; }
        public Customer? Customer { get; set; }
        public SalesRepProfile? CurrentSalesRep { get; set; }
        public string UserRole { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(string? orderType, int? customerId, CancellationToken cancellationToken = default)
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            IsCustomer = string.Equals(UserRole, "Customer", StringComparison.OrdinalIgnoreCase);
            IsSalesRep = string.Equals(UserRole, "SalesRep", StringComparison.OrdinalIgnoreCase);

            if (IsCustomer)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    Customer = await _salesService.GetCustomerByUserIdAsync(userId.Value, cancellationToken);
                }

                if (Customer == null)
                {
                    TempData["ErrorMessage"] = "This customer login is not linked to a customer account. Contact an administrator.";
                    return RedirectToPage("/Dashboard/Index");
                }

                await LoadVendorDataAsync(cancellationToken);
                OrderInput.OrderType = "Supermarket / Partner Order";
                OrderInput.CustomerId = Customer.Id;
                OrderInput.CustomerName = Customer.Name;
                OrderInput.CustomerCategory = Customer.Category.ToString();
                OrderInput.PaymentMethod = "CreditOwing";
                OrderInput.AmountPaid = 0;
                OrderInput.SalesRepId = null;
                OrderInput.PosTerminalCode = string.Empty;
                return Page();
            }

            if (IsSalesRep)
            {
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (currentUserId.HasValue)
                {
                    CurrentSalesRep = await _salesService.GetSalesRepByUserIdAsync(currentUserId.Value, cancellationToken);
                }

                if (CurrentSalesRep == null)
                {
                    TempData["ErrorMessage"] = "Your sales rep login is not linked to an active sales representative profile.";
                    return RedirectToPage("/Sales/Index");
                }
            }

            await LoadDataAsync(cancellationToken);

            if (!string.IsNullOrEmpty(orderType))
            {
                OrderInput.OrderType = orderType;
            }

            if (customerId.HasValue)
            {
                OrderInput.CustomerId = customerId.Value;
                var customer = Customers.FirstOrDefault(c => c.Id == customerId.Value);
                if (customer != null)
                {
                    OrderInput.CustomerName = customer.Name;
                    OrderInput.CustomerCategory = customer.Category.ToString();
                }
            }
            else
            {
                var defaultCustomer = Customers.FirstOrDefault(c => c.Category == CustomerCategory.WalkIn)
                    ?? Customers.FirstOrDefault();
                if (defaultCustomer != null)
                {
                    OrderInput.CustomerId = defaultCustomer.Id;
                    OrderInput.CustomerName = defaultCustomer.Name;
                    OrderInput.CustomerCategory = defaultCustomer.Category.ToString();
                }
                else
                {
                    OrderInput.CustomerId = 0;
                    OrderInput.CustomerName = "Unregistered Customer";
                }
            }

            if (IsSalesRep && CurrentSalesRep != null)
            {
                OrderInput.SalesRepId = CurrentSalesRep.Id;
            }
            else
            {
                OrderInput.SalesRepId ??= SalesReps.FirstOrDefault()?.Id;
            }
            if (IsSalesRep)
            {
                OrderInput.PosTerminalCode = Terminals.FirstOrDefault()?.TerminalCode ?? string.Empty;
            }
            else
            {
                OrderInput.PosTerminalCode ??= Terminals.FirstOrDefault()?.TerminalCode ?? "POS-01";
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            IsCustomer = string.Equals(UserRole, "Customer", StringComparison.OrdinalIgnoreCase);
            IsSalesRep = string.Equals(UserRole, "SalesRep", StringComparison.OrdinalIgnoreCase);

            if (IsCustomer)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    Customer = await _salesService.GetCustomerByUserIdAsync(userId.Value, cancellationToken);
                }

                if (Customer == null)
                {
                    ModelState.AddModelError(string.Empty, "This customer login is not linked to a customer account.");
                    await LoadVendorDataAsync(cancellationToken);
                    return Page();
                }

                OrderInput.OrderType = "Supermarket / Partner Order";
                OrderInput.CustomerId = Customer.Id;
                OrderInput.CustomerName = Customer.Name;
                OrderInput.CustomerCategory = Customer.Category.ToString();
                OrderInput.PaymentMethod = "CreditOwing";
                OrderInput.AmountPaid = 0;
                OrderInput.Discount = 0;
                OrderInput.SalesRepId = null;
                OrderInput.PosTerminalCode = string.Empty;
            }

            if (IsSalesRep)
            {
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (currentUserId.HasValue)
                {
                    CurrentSalesRep = await _salesService.GetSalesRepByUserIdAsync(currentUserId.Value, cancellationToken);
                }

                if (CurrentSalesRep == null)
                {
                    ModelState.AddModelError(string.Empty, "Your sales rep login is not linked to an active sales representative profile.");
                    await LoadDataAsync(cancellationToken);
                    return Page();
                }

                // A signed-in sales rep can only book orders under their own rep profile.
                OrderInput.SalesRepId = CurrentSalesRep.Id;

                var assignedTerminals = (await _salesService.GetTerminalsAsync(cancellationToken: cancellationToken))
                    .Where(t => CurrentSalesRep.EmployeeId.HasValue && t.AssignedEmployeeId == CurrentSalesRep.EmployeeId)
                    .ToList();
                var selectedTerminal = assignedTerminals.FirstOrDefault(t => t.TerminalCode == OrderInput.PosTerminalCode)
                    ?? assignedTerminals.FirstOrDefault();

                if (selectedTerminal == null)
                {
                    ModelState.AddModelError(string.Empty, "No POS terminal is assigned to your employee record.");
                }
                else
                {
                    OrderInput.PosTerminalCode = selectedTerminal.TerminalCode;
                }
            }

            if (!ModelState.IsValid)
            {
                if (IsCustomer) await LoadVendorDataAsync(cancellationToken);
                else await LoadDataAsync(cancellationToken);
                return Page();
            }

            OrderInput.Items = OrderInput.Items.Where(i => i.Quantity > 0).ToList();
            if (!OrderInput.Items.Any())
            {
                ModelState.AddModelError(string.Empty, "Please specify a quantity for at least one product.");
                if (IsCustomer) await LoadVendorDataAsync(cancellationToken);
                else await LoadDataAsync(cancellationToken);
                return Page();
            }

            var userIdValue = HttpContext.Session.GetInt32("UserId");
            var userName = HttpContext.Session.GetString("UserName") ?? "Sales Rep";

            try
            {
                var order = await _salesService.CreateOrderAsync(
                    OrderInput,
                    userIdValue,
                    userName,
                    UserRole,
                    cancellationToken);

                TempData["SuccessMessage"] = $"Order '{order.OrderNumber}' created successfully. Gross total: ₦{order.TotalAmount:N0}.";
                return IsCustomer
                    ? RedirectToPage("/Sales/PreOrderSchedule/Index")
                    : RedirectToPage("/Sales/Orders/Receipt", new { id = order.Id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                if (IsCustomer) await LoadVendorDataAsync(cancellationToken);
                else await LoadDataAsync(cancellationToken);
                return Page();
            }
        }

        private async Task LoadDataAsync(CancellationToken cancellationToken)
        {
            Customers = await _salesService.GetCustomersAsync(cancellationToken: cancellationToken);
            Terminals = await _salesService.GetTerminalsAsync(cancellationToken: cancellationToken);
            if (IsSalesRep)
            {
                Terminals = CurrentSalesRep?.EmployeeId is int employeeId
                    ? Terminals.Where(t => t.AssignedEmployeeId == employeeId).ToList()
                    : new List<PosTerminal>();
            }
            Recipes = await _salesService.GetSellableProductsAsync(cancellationToken);
            SalesReps = await _salesService.GetSalesRepsAsync(cancellationToken: cancellationToken);
            BonusRules = await _salesService.GetBonusRulesAsync(cancellationToken: cancellationToken);
        }

        private async Task LoadVendorDataAsync(CancellationToken cancellationToken)
        {
            Recipes = await _salesService.GetSellableProductsAsync(cancellationToken);
            BonusRules = await _salesService.GetBonusRulesAsync(cancellationToken: cancellationToken);
        }
    }
}
