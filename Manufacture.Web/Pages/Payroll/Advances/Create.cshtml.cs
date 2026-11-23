using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Payroll.Advances
{
    public class CreateModel : PageModel
    {
        private readonly IPayrollService _payrollService;
        private readonly IEmployeeService _employeeService;
        private readonly ISettingsService _settingsService;

        public CreateModel(
            IPayrollService payrollService,
            IEmployeeService employeeService,
            ISettingsService settingsService)
        {
            _payrollService = payrollService;
            _employeeService = employeeService;
            _settingsService = settingsService;
        }

        [BindProperty]
        public SalaryAdvanceDto AdvanceInput { get; set; } = new();

        public List<EmployeeDto> Employees { get; set; } = new();
        public PayrollSettingsDto Settings { get; set; } = new();

        public async Task OnGetAsync(int? employeeId)
        {
            await LoadAsync();
            if (employeeId.HasValue)
            {
                AdvanceInput.EmployeeId = employeeId.Value;
            }

            AdvanceInput.PrincipalAmount = 40000;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var userName = HttpContext.Session.GetString("UserName") ?? "System";
                var advance = await _payrollService.CreateAdvanceAsync(AdvanceInput, userName);
                TempData["SuccessMessage"] =
                    $"Salary advance of ₦{advance.PrincipalAmount:N0} granted to {advance.EmployeeName}. " +
                    $"Monthly recovery: ₦{advance.MonthlyDeductionAmount:N0} ({advance.MonthlyRepaymentPercent:0.#}%).";
                return RedirectToPage("/Payroll/Advances/Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }

        private async Task LoadAsync()
        {
            Employees = await _employeeService.GetAllAsync(includeInactive: false);
            Settings = await _settingsService.GetPayrollSettingsAsync();
        }
    }
}
