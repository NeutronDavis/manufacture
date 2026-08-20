using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.HR.Employees
{
    public class DetailsModel : PageModel
    {
        private readonly MockEmployeeService _employeeService;
        private readonly MockPayrollService _payrollService;

        public DetailsModel(MockEmployeeService employeeService, MockPayrollService payrollService)
        {
            _employeeService = employeeService;
            _payrollService = payrollService;
        }

        public Employee Employee { get; set; } = new();
        public List<SalaryAdvance> Advances { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var emp = _employeeService.GetById(id);
            if (emp == null) return RedirectToPage("/HR/Index");

            Employee = emp;
            Advances = _payrollService.GetAllAdvances().Where(a => a.EmployeeId == id).ToList();
            return Page();
        }
    }
}
