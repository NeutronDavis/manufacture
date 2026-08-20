using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;

namespace Manufacture.Pages
{
    public class LoginModel : PageModel
    {
        private readonly MockUserService _userService;

        public LoginModel(MockUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public string? Email { get; set; }

        [BindProperty]
        public string? Password { get; set; }

        [BindProperty]
        public string? PinCode { get; set; }

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPostPasswordLogin()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please provide both Email and Password.";
                return Page();
            }

            var user = _userService.AuthenticatePassword(Email, Password);
            if (user == null)
            {
                ErrorMessage = "Invalid credentials. (Hint: admin@bakery.com / Password123!)";
                return Page();
            }

            SetUserSession(user.Id, user.Name, user.Email, user.Role);
            return RedirectToPage("/Dashboard/Index");
        }

        public IActionResult OnPostPinLogin()
        {
            if (string.IsNullOrWhiteSpace(PinCode) || PinCode.Length != 6)
            {
                ErrorMessage = "Please enter a valid 6-digit numeric PIN.";
                return Page();
            }

            var user = _userService.AuthenticatePin(PinCode);
            if (user == null)
            {
                ErrorMessage = "PIN not recognized. (Try: 111111 for Admin, 444444 for Sales Rep, 222222 for Production)";
                return Page();
            }

            SetUserSession(user.Id, user.Name, user.Email, user.Role);
            return RedirectToPage("/Dashboard/Index");
        }

        public IActionResult OnGetSwitchRole(string role)
        {
            var user = _userService.GetAll().FirstOrDefault(u => u.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
            if (user != null)
            {
                SetUserSession(user.Id, user.Name, user.Email, user.Role);
            }
            else
            {
                SetUserSession(99, $"{role} User", $"{role.ToLower()}@bakery.com", role);
            }

            TempData["SuccessMessage"] = $"Logged in as {role}";
            return RedirectToPage("/Dashboard/Index");
        }

        private void SetUserSession(int userId, string name, string email, string role)
        {
            HttpContext.Session.SetInt32("UserId", userId);
            HttpContext.Session.SetString("UserName", name);
            HttpContext.Session.SetString("UserEmail", email);
            HttpContext.Session.SetString("UserRole", role);
        }
    }
}
