using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.UserManagement
{
    public class EditModel : PageModel
    {
        private readonly IUserService _userService;

        public EditModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public UserDto UserInput { get; set; } = new();

        public List<RoleDto> Roles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User account not found.";
                return RedirectToPage("/UserManagement/Index");
            }

            UserInput = user;
            // Never surface stored hashes. Blank means "keep the current value".
            UserInput.PinCode = null;
            UserInput.Password = null;

            await LoadRolesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadRolesAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(UserInput.PinCode) &&
                (UserInput.PinCode.Length != 6 || !UserInput.PinCode.All(char.IsDigit)))
            {
                ModelState.AddModelError(nameof(UserInput.PinCode), "PIN must be exactly 6 digits.");
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(UserInput.Password) && UserInput.Password.Length < 8)
            {
                ModelState.AddModelError(nameof(UserInput.Password), "Password must be at least 8 characters.");
                return Page();
            }

            try
            {
                var updated = await _userService.UpdateAsync(UserInput);
                TempData[updated ? "SuccessMessage" : "ErrorMessage"] = updated
                    ? $"User '{UserInput.Name}' updated successfully."
                    : "User account not found.";

                return RedirectToPage("/UserManagement/Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }

        private async Task LoadRolesAsync()
        {
            Roles = await _userService.GetRolesAsync();
        }
    }
}
