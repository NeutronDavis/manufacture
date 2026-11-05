using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.HR
{
    public class IndexModel : PageModel
    {
        private readonly MockEmployeeService _employeeService;
        private readonly MockPayrollService _payrollService;

        public IndexModel(MockEmployeeService employeeService, MockPayrollService payrollService)
        {
            _employeeService = employeeService;
            _payrollService = payrollService;
        }

        public List<Employee> Employees { get; set; } = new();
        public List<SalaryAdvance> ActiveAdvances { get; set; } = new();
        public List<PayrollRecord> CurrentPayrollRecords { get; set; } = new();
        public decimal TotalPayrollAmount { get; set; }
        public decimal PendingAdvancesTotal { get; set; }

        public void OnGet()
        {
            Employees = _employeeService.GetAll(includeInactive: true);
            ActiveAdvances = _payrollService.GetAllAdvances().Where(a => a.Status == "Active").ToList();
            CurrentPayrollRecords = _payrollService.GetPayrollForMonth(DateTime.UtcNow.Month, DateTime.UtcNow.Year);
            
            TotalPayrollAmount = CurrentPayrollRecords.Sum(p => p.NetSalary);
            if (TotalPayrollAmount == 0)
            {
                TotalPayrollAmount = Employees.Where(e => e.IsActive).Sum(e => e.BaseSalary);
            }
            PendingAdvancesTotal = ActiveAdvances.Sum(a => a.RemainingBalance);
        }

        public IActionResult OnPostToggleStatus(int id)
        {
            _employeeService.SoftDeleteToggle(id);
            TempData["SuccessMessage"] = "Employee status updated.";
            return RedirectToPage();
        }

        public IActionResult OnPostApprovePayment(int id)
        {
            TempData["SuccessMessage"] = "Staff payroll disbursement approved & scheduled for bank release.";
            return RedirectToPage();
        }
    }
}
