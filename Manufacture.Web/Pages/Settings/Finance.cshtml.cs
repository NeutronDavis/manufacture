using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Manufacture.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Settings
{
    public class FinanceModel : PageModel
    {
        private readonly ISettingsService _settingsService;
        private readonly RbacCache _rbacService;

        public FinanceModel(ISettingsService settingsService, RbacCache rbacService)
        {
            _settingsService = settingsService;
            _rbacService = rbacService;
        }

        public PayrollSettingsDto Payroll { get; set; } = new();

        public bool CanEdit { get; set; }
        public string UserRole { get; set; } = string.Empty;

        [BindProperty]
        public PayrollSettingsDto PayrollInput { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!await AuthorizeAsync())
            {
                return RedirectToPage("/Dashboard/Index");
            }

            await LoadAsync();
            PayrollInput = Payroll;
            return Page();
        }

        public async Task<IActionResult> OnPostSavePayrollAsync()
        {
            if (!await AuthorizeAsync()) return RedirectToPage("/Dashboard/Index");
            if (!CanEdit) return Forbid();

            if (!ValidatePayrollSettings(PayrollInput))
            {
                await LoadAsync();
                return Page();
            }

            await _settingsService.UpdatePayrollSettingsAsync(PayrollInput, UserName());
            TempData["SuccessMessage"] =
                $"Payroll & loan policy updated. Monthly loan recovery is now {PayrollInput.LoanMonthlyRepaymentPercent:0.#}%.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostResetPayrollAsync()
        {
            if (!await AuthorizeAsync()) return RedirectToPage("/Dashboard/Index");
            if (UserRole != "SuperAdmin")
            {
                TempData["ErrorMessage"] = "Only a SuperAdmin can reset the payroll policy.";
                return RedirectToPage();
            }

            await _settingsService.ResetPayrollSettingsAsync(UserName());
            TempData["SuccessMessage"] = "Payroll & loan policy restored to default.";
            return RedirectToPage();
        }

        private async Task<bool> AuthorizeAsync()
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            if (!_rbacService.HasAccess(UserRole, "Settings"))
            {
                TempData["ErrorMessage"] = "Access Denied: You do not have permission to view Finance & Payroll Settings.";
                return false;
            }

            CanEdit = UserRole == "SuperAdmin" || _rbacService.CanEdit(UserRole, "Settings");
            return true;
        }

        private async Task LoadAsync()
        {
            Payroll = await _settingsService.GetPayrollSettingsAsync();
        }

        private bool ValidatePayrollSettings(PayrollSettingsDto settings)
        {
            var values = new (string Name, decimal Value)[]
            {
                (nameof(settings.LoanMonthlyRepaymentPercent), settings.LoanMonthlyRepaymentPercent),
                (nameof(settings.MaxLoanPercentOfSalary), settings.MaxLoanPercentOfSalary),
                (nameof(settings.HousingAllowancePercent), settings.HousingAllowancePercent),
                (nameof(settings.TransportAllowancePercent), settings.TransportAllowancePercent),
                (nameof(settings.PayeTaxPercent), settings.PayeTaxPercent),
                (nameof(settings.PensionPercent), settings.PensionPercent),
                (nameof(settings.HealthInsurancePercent), settings.HealthInsurancePercent)
            };

            foreach (var (name, value) in values)
            {
                if (value < 0m || value > 100m)
                {
                    ModelState.AddModelError($"PayrollInput.{name}", "Percentage must be between 0 and 100.");
                }
            }

            if (settings.LoanMonthlyRepaymentPercent <= 0m)
            {
                ModelState.AddModelError(
                    "PayrollInput.LoanMonthlyRepaymentPercent",
                    "Monthly loan repayment percentage must be greater than zero.");
            }

            if (settings.MaxLoanPercentOfSalary <= 0m)
            {
                ModelState.AddModelError(
                    "PayrollInput.MaxLoanPercentOfSalary",
                    "Maximum loan percentage must be greater than zero.");
            }

            return ModelState.IsValid;
        }

        private string UserName() =>
            $"{HttpContext.Session.GetString("UserName") ?? "Administrator"} ({UserRole})";
    }
}
