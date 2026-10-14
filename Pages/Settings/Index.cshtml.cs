using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.Entities;
using Manufacture.Services;

namespace Manufacture.Pages.Settings
{
    public class IndexModel : PageModel
    {
        private readonly MockSettingsService _settingsService;
        private readonly MockRbacService _rbacService;

        public IndexModel(MockSettingsService settingsService, MockRbacService rbacService)
        {
            _settingsService = settingsService;
            _rbacService = rbacService;
        }

        public TreasuryPolicySettings TreasuryPolicy { get; set; } = new();
        public string UserRole { get; set; } = string.Empty;
        public bool CanAccessSettings { get; set; }

        public IActionResult OnGet()
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            CanAccessSettings = _rbacService.HasAccess(UserRole, "Settings");

            if (!CanAccessSettings)
            {
                TempData["ErrorMessage"] = "Access Denied: You do not have permission to view System Settings.";
                return RedirectToPage("/Dashboard/Index");
            }

            TreasuryPolicy = _settingsService.GetTreasuryPolicy();
            return Page();
        }
    }
}
