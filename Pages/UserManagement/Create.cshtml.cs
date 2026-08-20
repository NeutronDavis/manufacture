using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;

namespace Manufacture.Pages.UserManagement
{
    public class CreateModel : PageModel
    {
        private readonly MockUserService _userService;

        public CreateModel(MockUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public UserDto UserInput { get; set; } = new();

        public void OnGet()
        {
            UserInput.PinCode = "123456";
            UserInput.Role = "SalesRep";
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();

            var user = _userService.Create(UserInput);
            TempData["SuccessMessage"] = $"User account '{user.Name}' created successfully with PIN {user.PinCode}.";
            return RedirectToPage("/UserManagement/Index");
        }
    }
}
