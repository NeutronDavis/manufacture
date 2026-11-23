using System.ComponentModel.DataAnnotations;
using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(IUserService userService, ILogger<LoginModel> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [BindProperty]
        public LoginDto LoginInput { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostPasswordLoginAsync()
        {
            var email = NormalizeEmail(LoginInput.Email);
            if (!IsValidEmail(email))
            {
                ErrorMessage = "Enter a valid email address.";
                return Page();
            }

            if (LoginInput.Password is { Length: 6 } && LoginInput.Password.All(char.IsDigit))
            {
                ErrorMessage = "That is your 6-digit PIN, not your password. Use 'Sign in with POS PIN instead'.";
                return Page();
            }

            if (!IsValidPassword(LoginInput.Password))
            {
                ErrorMessage = "Password must be between 8 and 128 characters.";
                return Page();
            }

            var user = await _userService.AuthenticatePasswordAsync(email!, LoginInput.Password!);
            if (user == null)
            {
                _logger.LogWarning(
                    "Failed password login for {Email} from {RemoteIp}.",
                    email,
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

                // Deliberately generic: never reveal whether the account exists.
                ErrorMessage = "Invalid email or password.";
                return Page();
            }

            SetUserSession(user);
            return RedirectToPage("/Dashboard/Index");
        }

        public async Task<IActionResult> OnPostPinLoginAsync()
        {
            var pin = LoginInput.PinCode?.Trim();
            if (!IsValidPin(pin))
            {
                ErrorMessage = "PIN must be exactly 6 digits.";
                return Page();
            }

            var user = await _userService.AuthenticatePinAsync(pin!);
            if (user == null)
            {
                _logger.LogWarning(
                    "Failed PIN login from {RemoteIp}.",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

                ErrorMessage = "Invalid PIN.";
                return Page();
            }

            SetUserSession(user);
            return RedirectToPage("/Dashboard/Index");
        }

        private static string? NormalizeEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return email.Trim().ToLowerInvariant();
        }

        private static bool IsValidEmail(string? email)
        {
            return !string.IsNullOrWhiteSpace(email)
                && email.Length <= 256
                && !email.Any(char.IsControl)
                && new EmailAddressAttribute().IsValid(email);
        }

        private static bool IsValidPassword(string? password)
        {
            return !string.IsNullOrWhiteSpace(password)
                && password.Length is >= 8 and <= 128
                && !password.Any(char.IsControl);
        }

        private static bool IsValidPin(string? pin)
        {
            return pin is { Length: 6 } && pin.All(char.IsDigit);
        }

        private void SetUserSession(UserDto user)
        {
            HttpContext.Session.Clear();
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserRole", user.Role);
        }
    }
}
