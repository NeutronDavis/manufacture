using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.UserManagement
{
    public class CreateModel : PageModel
    {
        private readonly IUserService _userService;

        public CreateModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public UserDto UserInput { get; set; } = new();

        public List<RoleDto> Roles { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadRolesAsync();
            UserInput.RoleId = Roles.FirstOrDefault(r => r.Name == "SalesRep")?.Id
                ?? Roles.FirstOrDefault()?.Id
                ?? 0;
            UserInput.PinCode = "123456";
            UserInput.Password = "Password123!";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadRolesAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (string.IsNullOrWhiteSpace(UserInput.PinCode) || UserInput.PinCode.Length != 6 || !UserInput.PinCode.All(char.IsDigit))
            {
                ModelState.AddModelError(nameof(UserInput.PinCode), "PIN must be exactly 6 digits.");
                return Page();
            }

            if (string.IsNullOrWhiteSpace(UserInput.Password) || UserInput.Password.Length < 8)
            {
                ModelState.AddModelError(nameof(UserInput.Password), "Password must be at least 8 characters.");
                return Page();
            }

            try
            {
                UserInput.IsActive = true;
                var user = await _userService.CreateAsync(UserInput);
                TempData["SuccessMessage"] = $"User account '{user.Name}' created successfully for role {user.RoleDisplayName}.";
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
