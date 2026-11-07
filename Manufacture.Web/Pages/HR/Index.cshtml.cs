using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.HR
{
    public class IndexModel : PageModel
    {
        private readonly IEmployeeService _employeeService;
        private readonly IPayrollService _payrollService;

        public IndexModel(IEmployeeService employeeService, IPayrollService payrollService)
        {
            _employeeService = employeeService;
            _payrollService = payrollService;
        }

        public List<EmployeeDto> Employees { get; set; } = new();
        public List<SalaryAdvanceListDto> ActiveAdvances { get; set; } = new();
        public List<PayrollRecordDto> CurrentPayrollRecords { get; set; } = new();

        public EmployeeDto? SelectedEmployee { get; set; }
        public PayrollRecordDto? SelectedPayroll { get; set; }
        public List<PayrollRecordDto> SelectedEmployeePayroll { get; set; } = new();
        public List<SalaryAdvanceListDto> SelectedEmployeeAdvances { get; set; } = new();

        public int SelectedMonth { get; set; }
        public int SelectedYear { get; set; }
        public int ActiveEmployeesCount { get; set; }
        public decimal TotalPayrollAmount { get; set; }
        public decimal PendingAdvancesTotal { get; set; }
        public int ActiveLoanCount { get; set; }

        public string SelectedMonthLabel =>
            new DateTime(SelectedYear, SelectedMonth, 1).ToString("MMMM yyyy");

        public async Task OnGetAsync(int? employeeId, int? month, int? year)
        {
            SelectedMonth = month ?? DateTime.UtcNow.Month;
            SelectedYear = year ?? DateTime.UtcNow.Year;

            Employees = await _employeeService.GetAllAsync(includeInactive: true);
            ActiveEmployeesCount = Employees.Count(e => e.IsActive && e.EmploymentStatus != "Inactive");

            ActiveAdvances = (await _payrollService.GetAdvancesAsync())
                .Where(a => a.Status == "Active" && a.RemainingBalance > 0)
                .ToList();
            ActiveLoanCount = ActiveAdvances.Count;
            PendingAdvancesTotal = ActiveAdvances.Sum(a => a.RemainingBalance);

            CurrentPayrollRecords = await _payrollService.GetPayrollRecordsAsync(SelectedMonth, SelectedYear);
            TotalPayrollAmount = CurrentPayrollRecords.Sum(p => p.NetSalary);

            SelectedEmployee = employeeId.HasValue
                ? Employees.FirstOrDefault(e => e.Id == employeeId.Value)
                : Employees.FirstOrDefault();

            if (SelectedEmployee != null)
            {
                SelectedPayroll = CurrentPayrollRecords.FirstOrDefault(p => p.EmployeeId == SelectedEmployee.Id)
                    ?? await _payrollService.GetPayrollRecordAsync(SelectedEmployee.Id, SelectedMonth, SelectedYear);
                SelectedEmployeeAdvances = await _payrollService.GetAdvancesForEmployeeAsync(SelectedEmployee.Id);
                SelectedEmployeePayroll = (await _payrollService.GetAllPayrollRecordsAsync())
                    .Where(p => p.EmployeeId == SelectedEmployee.Id)
                    .ToList();
            }
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            var employee = await _employeeService.GetByIdAsync(id);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee record not found.";
                return RedirectToPage();
            }

            await _employeeService.SetActiveAsync(id, !employee.IsActive);
            TempData["SuccessMessage"] = $"Employee '{employee.FullName}' status updated.";
            return RedirectToPage();
        }

    }
}
