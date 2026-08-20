using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Payroll.Advances
{
    public class CreateModel : PageModel
    {
        private readonly MockPayrollService _payrollService;
        private readonly MockEmployeeService _employeeService;

        public CreateModel(MockPayrollService payrollService, MockEmployeeService employeeService)
        {
            _payrollService = payrollService;
            _employeeService = employeeService;
        }

        [BindProperty]
        public SalaryAdvanceDto AdvanceInput { get; set; } = new();

        public List<Employee> Employees { get; set; } = new();

        public void OnGet(int? employeeId)
        {
            Employees = _employeeService.GetAll(includeInactive: false);
            if (employeeId.HasValue) AdvanceInput.EmployeeId = employeeId.Value;
            AdvanceInput.PrincipalAmount = 40000;
            AdvanceInput.MonthlyDeductionAmount = 15000;
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                Employees = _employeeService.GetAll(includeInactive: false);
                return Page();
            }

            var adv = _payrollService.CreateAdvance(AdvanceInput);
            TempData["SuccessMessage"] = $"Salary advance of ₦{adv.PrincipalAmount:N0} granted to {adv.EmployeeName}.";
            return RedirectToPage("/Payroll/Advances/Index");
        }
    }
}
