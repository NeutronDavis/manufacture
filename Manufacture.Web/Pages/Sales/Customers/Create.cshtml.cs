using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.Customers
{
    public class CreateModel : PageModel
    {
        private readonly ISalesService _salesService;

        public CreateModel(ISalesService salesService)
        {
            _salesService = salesService;
        }

        [BindProperty]
        public CustomerDto CustomerInput { get; set; } = new();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var customer = await _salesService.CreateCustomerAsync(CustomerInput, cancellationToken);
                TempData["SuccessMessage"] = $"Customer '{customer.Name}' registered successfully with code {customer.MemberCode}.";
                return RedirectToPage("/Sales/Customers/Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }
}
