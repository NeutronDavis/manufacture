using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.Domain;
using Manufacture.Services;

namespace Manufacture.Pages.Settings
{
    public class TreasuryModel : PageModel
    {
        private readonly MockSettingsService _settingsService;
        private readonly MockRbacService _rbacService;

        public TreasuryModel(MockSettingsService settingsService, MockRbacService rbacService)
        {
            _settingsService = settingsService;
            _rbacService = rbacService;
        }

        public TreasuryPolicySettings Policy { get; set; } = new();

        public bool CanEdit { get; set; }
        public string UserRole { get; set; } = string.Empty;

        [BindProperty]
        public decimal PlannedCashPercent { get; set; }

        [BindProperty]
        public decimal AlertTolerancePercent { get; set; }

        [BindProperty]
        public string PolicyMemo { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            if (!_rbacService.HasAccess(UserRole, "Settings"))
            {
                TempData["ErrorMessage"] = "Access Denied: You do not have permission to view Treasury Settings.";
                return RedirectToPage("/Dashboard/Index");
            }

            CanEdit = UserRole == "SuperAdmin" ||
                      _rbacService.GetPermissionLevel(UserRole, "Settings").Contains("Edit") ||
                      _rbacService.GetPermissionLevel(UserRole, "Settings") == "Full Access";

            Policy = _settingsService.GetTreasuryPolicy();
            PlannedCashPercent = Policy.PlannedCashPercent;
            AlertTolerancePercent = Policy.AlertTolerancePercent;
            PolicyMemo = Policy.PolicyMemo;

            return Page();
        }

        public IActionResult OnPostSave()
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            var userName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            if (!_rbacService.HasAccess(UserRole, "Settings"))
            {
                return Forbid();
            }

            CanEdit = UserRole == "SuperAdmin" ||
                      _rbacService.GetPermissionLevel(UserRole, "Settings").Contains("Edit") ||
                      _rbacService.GetPermissionLevel(UserRole, "Settings") == "Full Access";

            if (!CanEdit)
            {
                TempData["ErrorMessage"] = "You have View Only access to Settings. Only SuperAdmins or managers with Edit access can update the Treasury Benchmark.";
                return RedirectToPage();
            }

            if (PlannedCashPercent < 0m || PlannedCashPercent > 100m)
            {
                ModelState.AddModelError(nameof(PlannedCashPercent), "Planned cash benchmark must be between 0% and 100%.");
                Policy = _settingsService.GetTreasuryPolicy();
                return Page();
            }

            if (AlertTolerancePercent < 0m || AlertTolerancePercent > 50m)
            {
                ModelState.AddModelError(nameof(AlertTolerancePercent), "Alert tolerance threshold must be between 0% and 50%.");
                Policy = _settingsService.GetTreasuryPolicy();
                return Page();
            }

            _settingsService.UpdateTreasuryPolicy(
                PlannedCashPercent,
                AlertTolerancePercent,
                PolicyMemo,
                $"{userName} ({UserRole})");

            TempData["SuccessMessage"] = $"Treasury benchmark successfully updated to {PlannedCashPercent:0.#}% cash / {100m - PlannedCashPercent:0.#}% digital. Live cash variance calculations for sales reps have been refreshed.";
            return RedirectToPage();
        }

        public IActionResult OnPostReset()
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            var userName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            if (UserRole != "SuperAdmin")
            {
                TempData["ErrorMessage"] = "Only a SuperAdmin can reset the treasury policy to system factory defaults.";
                return RedirectToPage();
            }

            _settingsService.ResetTreasuryPolicyToDefaults($"{userName} ({UserRole})");
            TempData["SuccessMessage"] = "Treasury policy restored to system default (32.0% Cash / 68.0% Digital, 6.0% tolerance).";
            return RedirectToPage();
        }
    }
}
