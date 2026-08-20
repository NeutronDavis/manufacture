using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Sales.Orders
{
    public class CreateModel : PageModel
    {
        private readonly MockSalesService _salesService;
        private readonly MockProductionService _productionService;

        public CreateModel(MockSalesService salesService, MockProductionService productionService)
        {
            _salesService = salesService;
            _productionService = productionService;
        }

        [BindProperty]
        public CreateOrderDto OrderInput { get; set; } = new();

        public List<Customer> Customers { get; set; } = new();
        public List<PosTerminal> Terminals { get; set; } = new();
        public List<Recipe> Recipes { get; set; } = new();

        public void OnGet(string? orderType, int? customerId)
        {
            Customers = _salesService.GetAllCustomers();
            Terminals = _salesService.GetAllTerminals();
            Recipes = _productionService.GetAllRecipes();

            if (!string.IsNullOrEmpty(orderType)) OrderInput.OrderType = orderType;
            if (customerId.HasValue) OrderInput.CustomerId = customerId.Value;
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                Customers = _salesService.GetAllCustomers();
                Terminals = _salesService.GetAllTerminals();
                Recipes = _productionService.GetAllRecipes();
                return Page();
            }

            OrderInput.Items = OrderInput.Items.Where(i => i.Quantity > 0).ToList();
            if (!OrderInput.Items.Any())
            {
                ModelState.AddModelError(string.Empty, "Please specify a quantity for at least one bread item.");
                Customers = _salesService.GetAllCustomers();
                Terminals = _salesService.GetAllTerminals();
                Recipes = _productionService.GetAllRecipes();
                return Page();
            }

            var userId = HttpContext.Session.GetInt32("UserId") ?? 4;
            var userName = HttpContext.Session.GetString("UserName") ?? "Sales Rep";

            var order = _salesService.CreateOrder(OrderInput, userId, userName);
            TempData["SuccessMessage"] = $"Order '{order.OrderNumber}' created successfully. Total: ₦{order.TotalAmount:N0}.";
            return RedirectToPage("/Sales/Orders/Receipt", new { id = order.Id });
        }
    }
}
