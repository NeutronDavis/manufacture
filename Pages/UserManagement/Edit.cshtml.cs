using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;

namespace Manufacture.Pages.UserManagement
{
    public class EditModel : PageModel
    {
        private readonly MockUserService _userService;

        public EditModel(MockUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public UserDto UserInput { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var user = _userService.GetById(id);
            if (user == null) return RedirectToPage("/UserManagement/Index");

            UserInput = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                PinCode = user.PinCode,
                PhoneNumber = user.PhoneNumber,
                PosTerminalId = user.PosTerminalId,
                IsActive = user.IsActive
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();

            _userService.Update(UserInput);
            TempData["SuccessMessage"] = $"User '{UserInput.Name}' updated successfully.";
            return RedirectToPage("/UserManagement/Index");
        }
    }
}
