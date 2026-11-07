using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.UserManagement
{
    public class IndexModel : PageModel
    {
        private readonly IUserService _userService;

        public IndexModel(IUserService userService)
        {
            _userService = userService;
        }

        public List<UserDto> Users { get; set; } = new();

        public async Task OnGetAsync()
        {
            Users = await _userService.GetAllAsync(includeInactive: true);
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User account not found.";
                return RedirectToPage();
            }

            try
            {
                await _userService.SetActiveAsync(id, !user.IsActive);
                TempData["SuccessMessage"] = user.IsActive
                    ? $"User '{user.Name}' deactivated. Historical records are preserved."
                    : $"User '{user.Name}' activated.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var deleted = await _userService.DeleteAsync(id);
                TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted
                    ? "User account deleted."
                    : "User account not found.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }
    }
}
