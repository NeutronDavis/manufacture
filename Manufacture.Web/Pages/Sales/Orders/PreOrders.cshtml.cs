using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.Orders
{
    public class PreOrdersModel : PageModel
    {
        public IActionResult OnGet()
        {
            return RedirectToPage("/Sales/PreOrderSchedule/Index");
        }
    }
}
