using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Never create an implicit session. A user must authenticate first.
            if (!HttpContext.Session.GetInt32("UserId").HasValue)
            {
                return RedirectToPage("/Login");
            }

            return RedirectToPage("/Dashboard/Index");
        }
    }
}
