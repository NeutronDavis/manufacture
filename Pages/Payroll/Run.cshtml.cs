using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Payroll
{
    public class RunModel : PageModel
    {
        private readonly MockPayrollService _payrollService;
        private readonly MockEmployeeService _employeeService;

        public RunModel(MockPayrollService payrollService, MockEmployeeService employeeService)
        {
            _payrollService = payrollService;
            _employeeService = employeeService;
        }

        [BindProperty]
        public ProcessPayrollDto ProcessInput { get; set; } = new();

        public List<Employee> Employees { get; set; } = new();
        public List<SalaryAdvance> ActiveAdvances { get; set; } = new();

        public void OnGet(int? month, int? year)
        {
            if (month.HasValue) ProcessInput.Month = month.Value;
            if (year.HasValue) ProcessInput.Year = year.Value;

            Employees = _employeeService.GetAll(includeInactive: false);
            ActiveAdvances = _payrollService.GetAllAdvances().Where(a => !a.IsFullyRepaid).ToList();
        }

        public IActionResult OnPost()
        {
            Employees = _employeeService.GetAll(includeInactive: false);
            ActiveAdvances = _payrollService.GetAllAdvances().Where(a => !a.IsFullyRepaid).ToList();

            _payrollService.ProcessMonthlyPayroll(ProcessInput.Month, ProcessInput.Year, ProcessInput.Adjustments);
            TempData["SuccessMessage"] = $"Payroll for {System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(ProcessInput.Month)} {ProcessInput.Year} processed successfully with automated loan deductions.";
            return RedirectToPage("/Payroll/Index", new { month = ProcessInput.Month, year = ProcessInput.Year });
        }
    }
}
