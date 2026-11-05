using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;

namespace Manufacture.Pages.Sales.Customers
{
    public class CreateModel : PageModel
    {
        private readonly MockSalesService _salesService;

        public CreateModel(MockSalesService salesService)
        {
            _salesService = salesService;
        }

        [BindProperty]
        public CustomerDto CustomerInput { get; set; } = new();

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();

            var customer = _salesService.CreateCustomer(CustomerInput);
            TempData["SuccessMessage"] = $"Client '{customer.Name}' registered successfully with code {customer.MemberCode}.";
            return RedirectToPage("/Sales/Customers/Index");
        }
    }
}
