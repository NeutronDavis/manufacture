using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.UserManagement
{
    public class IndexModel : PageModel
    {
        private readonly MockUserService _userService;

        public IndexModel(MockUserService userService)
        {
            _userService = userService;
        }

        public List<User> Users { get; set; } = new();

        public void OnGet()
        {
            Users = _userService.GetAll(includeInactive: true);
        }

        public IActionResult OnPostToggleStatus(int id)
        {
            _userService.SoftDeleteToggle(id);
            TempData["SuccessMessage"] = "User status updated (soft-deleted/restored). Historical logs preserved.";
            return RedirectToPage();
        }
    }
}
