using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Payroll
{
    public class RunModel : PageModel
    {
        private readonly IPayrollService _payrollService;
        private readonly ISettingsService _settingsService;

        public RunModel(IPayrollService payrollService, ISettingsService settingsService)
        {
            _payrollService = payrollService;
            _settingsService = settingsService;
        }

        [BindProperty]
        public ProcessPayrollDto ProcessInput { get; set; } = new();

        public List<PayrollRunEmployeeDto> RunEmployees { get; set; } = new();
        public PayrollSettingsDto Settings { get; set; } = new();
        public decimal TotalBaseSalary { get; set; }
        public decimal EstimatedNetPayout { get; set; }

        public async Task OnGetAsync(int? month, int? year)
        {
            if (month.HasValue) ProcessInput.Month = month.Value;
            if (year.HasValue) ProcessInput.Year = year.Value;
            await LoadAsync();
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
                await _payrollService.ProcessMonthlyPayrollAsync(
                    ProcessInput.Month,
                    ProcessInput.Year,
                    ProcessInput.Adjustments,
                    userName);

                TempData["SuccessMessage"] =
                    $"Payroll for {new DateTime(ProcessInput.Year, ProcessInput.Month, 1):MMMM yyyy} processed successfully. " +
                    "Loan recoveries, deduction lines, and payroll records were generated.";

                return RedirectToPage("/Payroll/Index", new { month = ProcessInput.Month, year = ProcessInput.Year });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }

        private async Task LoadAsync()
        {
            Settings = await _settingsService.GetPayrollSettingsAsync();
            RunEmployees = await _payrollService.GetRunEmployeesAsync(ProcessInput.Month, ProcessInput.Year);

            // Preserve manually entered loan recoveries when the page is redisplayed
            // after a validation failure.
            foreach (var employee in RunEmployees)
            {
                var adjustment = ProcessInput.Adjustments.FirstOrDefault(a => a.EmployeeId == employee.EmployeeId);
                if (adjustment?.LoanRecoveryAmount is decimal overrideAmount)
                {
                    employee.MonthlyLoanRecovery = Math.Max(0m, overrideAmount);
                }
            }

            TotalBaseSalary = RunEmployees.Sum(e => e.BaseSalary);
            EstimatedNetPayout = TotalBaseSalary - RunEmployees.Sum(e => e.MonthlyLoanRecovery);
        }
    }
}
