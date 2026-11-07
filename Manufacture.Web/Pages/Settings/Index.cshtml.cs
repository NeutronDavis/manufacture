using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.Domain;
using Manufacture.Services;

namespace Manufacture.Pages.Settings
{
    public class IndexModel : PageModel
    {
        private readonly SettingsCache _settingsCache;
        private readonly RbacCache _rbacService;

        public IndexModel(SettingsCache settingsCache, RbacCache rbacService)
        {
            _settingsCache = settingsCache;
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

            TreasuryPolicy = _settingsCache.TreasuryPolicy;
            return Page();
        }
    }
}
