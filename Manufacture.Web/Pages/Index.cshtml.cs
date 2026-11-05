using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole))
            {
                // Default to SuperAdmin session for frictionless preview experience or redirect to login
                HttpContext.Session.SetInt32("UserId", 1);
                HttpContext.Session.SetString("UserName", "Adekunle Johnson");
                HttpContext.Session.SetString("UserEmail", "admin@bakery.com");
                HttpContext.Session.SetString("UserRole", "SuperAdmin");
            }
            return RedirectToPage("/Dashboard/Index");
        }
    }
}
