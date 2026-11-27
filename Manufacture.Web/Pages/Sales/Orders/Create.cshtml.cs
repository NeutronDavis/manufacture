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
        private readonly IProductRepositoryService _repositoryService;

        public CreateModel(ISalesService salesService, IProductRepositoryService repositoryService)
        {
            _salesService = salesService;
            _repositoryService = repositoryService;
        }

        [BindProperty]
        public CreateOrderDto OrderInput { get; set; } = new();

        public List<PosTerminal> Terminals { get; set; } = new();
        public List<StockProductOptionDto> Products { get; set; } = new();
        public List<BonusRule> BonusRules { get; set; } = new();
        public List<SalesRepProfile> SalesReps { get; set; } = new();

        public bool IsCustomer { get; set; }
        public bool IsSalesRep { get; set; }
        public Customer? Customer { get; set; }
        public SalesRepProfile? CurrentSalesRep { get; set; }
        public string UserRole { get; set; } = string.Empty;
        public string SelectedTerminalCode { get; set; } = string.Empty;
        public string RepositoryName { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(string? orderType, CancellationToken cancellationToken = default)
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            IsCustomer = string.Equals(UserRole, "Customer", StringComparison.OrdinalIgnoreCase);
            IsSalesRep = string.Equals(UserRole, "SalesRep", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(UserRole, "ProductionManager", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Production Managers cannot create POS orders.";
                return RedirectToPage("/Sales/Orders/Index");
            }

            if (IsCustomer)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    Customer = await _salesService.GetCustomerByUserIdAsync(userId.Value, cancellationToken);
                }

                if (Customer == null)
                {
                    TempData["ErrorMessage"] = "This customer login is not linked to a customer account.";
                    return RedirectToPage("/Dashboard/Index");
                }

                OrderInput.OrderType = "PreOrder";
                OrderInput.CustomerId = Customer.Id;
                OrderInput.CustomerName = Customer.Name;
                OrderInput.CustomerCategory = Customer.Category.ToString();
                OrderInput.PaymentMethod = "CreditOwing";
                OrderInput.AmountPaid = 0;
                Products = await GetAllProductsAsync(cancellationToken);
                BonusRules = await _salesService.GetBonusRulesAsync(cancellationToken: cancellationToken);
                return Page();
            }

            if (IsSalesRep)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    CurrentSalesRep = await _salesService.GetSalesRepByUserIdAsync(userId.Value, cancellationToken);
                }

                if (CurrentSalesRep == null)
                {
                    TempData["ErrorMessage"] = "Your sales rep login is not linked to an active sales representative profile.";
                    return RedirectToPage("/Sales/Index");
                }
            }

            await LoadDataAsync(cancellationToken);

            OrderInput.OrderType = string.IsNullOrWhiteSpace(orderType) ? "ImmediateSale" : orderType;
            OrderInput.CustomerId = 0;
            OrderInput.CustomerName = string.Empty;
            OrderInput.PosTerminalCode = Terminals.FirstOrDefault()?.TerminalCode ?? string.Empty;
            OrderInput.SalesRepId = IsSalesRep ? CurrentSalesRep?.Id : null;

            await LoadProductsForTerminalAsync(OrderInput.PosTerminalCode, cancellationToken);

            return Page();
        }

        public async Task<IActionResult> OnGetProductsAsync(int terminalId, CancellationToken cancellationToken = default)
        {
            var terminal = await _salesService.GetTerminalAsync(terminalId, cancellationToken);
            if (terminal?.ProductRepositoryId == null)
            {
                return new JsonResult(Array.Empty<object>());
            }

            var products = await _repositoryService.GetProductsForRepositoryAsync(terminal.ProductRepositoryId.Value, cancellationToken);
            return new JsonResult(products.Select(p => new
            {
                id = p.RecipeId,
                name = p.Name,
                code = p.Code,
                price = p.SellingPrice,
                available = p.AvailableQuantity,
                unit = p.OutputUnit
            }));
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            IsCustomer = string.Equals(UserRole, "Customer", StringComparison.OrdinalIgnoreCase);
            IsSalesRep = string.Equals(UserRole, "SalesRep", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(UserRole, "ProductionManager", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Production Managers cannot create POS orders.";
                return RedirectToPage("/Sales/Orders/Index");
            }

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
                    await ReloadAsync(cancellationToken);
                    return Page();
                }

                OrderInput.OrderType = "PreOrder";
                OrderInput.CustomerId = Customer.Id;
                OrderInput.CustomerName = Customer.Name;
                OrderInput.CustomerCategory = Customer.Category.ToString();
                OrderInput.PaymentMethod = "CreditOwing";
                OrderInput.AmountPaid = 0;
                OrderInput.Discount = 0;
                OrderInput.SalesRepId = null;
                OrderInput.PosTerminalCode = string.Empty;
            }
            else if (IsSalesRep)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    CurrentSalesRep = await _salesService.GetSalesRepByUserIdAsync(userId.Value, cancellationToken);
                }

                if (CurrentSalesRep == null)
                {
                    ModelState.AddModelError(string.Empty, "Your sales rep login is not linked to an active sales representative profile.");
                    await ReloadAsync(cancellationToken);
                    return Page();
                }

                OrderInput.SalesRepId = CurrentSalesRep.Id;
                var assigned = (await _salesService.GetTerminalsAsync(cancellationToken: cancellationToken))
                    .Where(t => CurrentSalesRep.EmployeeId.HasValue && t.AssignedEmployeeId == CurrentSalesRep.EmployeeId)
                    .ToList();
                var selected = assigned.FirstOrDefault(t => t.TerminalCode == OrderInput.PosTerminalCode) ?? assigned.FirstOrDefault();
                if (selected == null)
                {
                    ModelState.AddModelError(string.Empty, "No POS terminal is assigned to your employee record.");
                }
                else
                {
                    OrderInput.PosTerminalCode = selected.TerminalCode;
                }
            }
            else
            {
                OrderInput.CustomerId = 0;
                OrderInput.CustomerName = string.Empty;
            }

            if (!ModelState.IsValid)
            {
                await ReloadAsync(cancellationToken);
                return Page();
            }

            OrderInput.Items = (OrderInput.Items ?? new List<OrderItemDto>()).Where(i => i.Quantity > 0).ToList();
            if (OrderInput.Items.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Add at least one product to the basket.");
                await ReloadAsync(cancellationToken);
                return Page();
            }

            try
            {
                var order = await _salesService.CreateOrderAsync(
                    OrderInput,
                    HttpContext.Session.GetInt32("UserId"),
                    HttpContext.Session.GetString("UserName") ?? "Sales",
                    UserRole,
                    cancellationToken);

                TempData["SuccessMessage"] = IsCustomer
                    ? $"Pre-order '{order.OrderNumber}' placed successfully."
                    : $"Order '{order.OrderNumber}' saved. Review and print the receipt to deduct stock.";
                return IsCustomer
                    ? RedirectToPage("/Sales/PreOrderSchedule/Index")
                    : RedirectToPage("/Sales/Orders/Receipt", new { id = order.Id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await ReloadAsync(cancellationToken);
                return Page();
            }
        }

        private async Task LoadDataAsync(CancellationToken cancellationToken)
        {
            Terminals = await _salesService.GetTerminalsAsync(cancellationToken: cancellationToken);
            if (IsSalesRep && CurrentSalesRep?.EmployeeId is int employeeId)
            {
                Terminals = Terminals.Where(t => t.AssignedEmployeeId == employeeId).ToList();
            }

            SalesReps = await _salesService.GetSalesRepsAsync(cancellationToken: cancellationToken);
            BonusRules = await _salesService.GetBonusRulesAsync(cancellationToken: cancellationToken);
        }

        private async Task LoadProductsForTerminalAsync(string? terminalCode, CancellationToken cancellationToken)
        {
            var terminal = Terminals.FirstOrDefault(t => t.TerminalCode == terminalCode) ?? Terminals.FirstOrDefault();
            SelectedTerminalCode = terminal?.TerminalCode ?? string.Empty;
            RepositoryName = terminal?.ProductRepositoryName ?? string.Empty;
            OrderInput.PosTerminalCode = SelectedTerminalCode;

            if (terminal?.ProductRepositoryId is int repositoryId)
            {
                Products = await _repositoryService.GetProductsForRepositoryAsync(repositoryId, cancellationToken);
            }
            else
            {
                Products = new List<StockProductOptionDto>();
            }
        }

        private async Task<List<StockProductOptionDto>> GetAllProductsAsync(CancellationToken cancellationToken)
        {
            var recipes = await _salesService.GetSellableProductsAsync(cancellationToken);
            return recipes.Select(r => new StockProductOptionDto
            {
                RecipeId = r.Id,
                Name = r.Name,
                Code = r.Code,
                OutputUnit = r.OutputUnit,
                SellingPrice = r.SellingPrice,
                AvailableQuantity = 0
            }).ToList();
        }

        private async Task ReloadAsync(CancellationToken cancellationToken)
        {
            if (IsCustomer)
            {
                Products = await GetAllProductsAsync(cancellationToken);
                BonusRules = await _salesService.GetBonusRulesAsync(cancellationToken: cancellationToken);
                return;
            }

            await LoadDataAsync(cancellationToken);
            await LoadProductsForTerminalAsync(OrderInput.PosTerminalCode, cancellationToken);
        }
    }
}
