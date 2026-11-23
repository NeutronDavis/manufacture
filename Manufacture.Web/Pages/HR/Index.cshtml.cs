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
        public EmployeeDependencyDto? SelectedDependencies { get; set; }
        public PayrollRecordDto? SelectedPayroll { get; set; }
        public List<PayrollRecordDto> SelectedEmployeePayroll { get; set; } = new();
        public List<SalaryAdvanceListDto> SelectedEmployeeAdvances { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public bool ShowArchived { get; set; }

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

            Employees = await _employeeService.GetAllAsync(
                includeInactive: true,
                includeDeleted: ShowArchived);

            ActiveEmployeesCount = Employees.Count(e => !e.IsDeleted && e.IsActive && e.EmploymentStatus != "Inactive");

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
                SelectedDependencies = await _employeeService.GetDependenciesAsync(SelectedEmployee.Id);
                SelectedPayroll = CurrentPayrollRecords.FirstOrDefault(p => p.EmployeeId == SelectedEmployee.Id)
                    ?? await _payrollService.GetPayrollRecordAsync(SelectedEmployee.Id, SelectedMonth, SelectedYear);
                SelectedEmployeeAdvances = await _payrollService.GetAdvancesForEmployeeAsync(SelectedEmployee.Id);
                SelectedEmployeePayroll = (await _payrollService.GetAllPayrollRecordsAsync())
                    .Where(p => p.EmployeeId == SelectedEmployee.Id)
                    .ToList();
            }
        }

        public async Task<IActionResult> OnPostChangeStatusAsync(int id, string status, int? month, int? year, bool? showArchived)
        {
            var employee = await _employeeService.GetByIdAsync(id);
            if (employee == null || employee.IsDeleted)
            {
                TempData["ErrorMessage"] = "Employee record not found.";
                return RedirectToPage(new { month, year, showArchived });
            }

            if (!await _employeeService.ChangeStatusAsync(id, status))
            {
                TempData["ErrorMessage"] = "Unable to update the employee status.";
                return RedirectToEmployee(id, month, year, showArchived);
            }

            TempData["SuccessMessage"] = $"Employee '{employee.FullName}' status changed to {NormalizeStatusLabel(status)}.";
            return RedirectToEmployee(id, month, year, showArchived);
        }

        public async Task<IActionResult> OnPostArchiveAsync(int id, string? reason, int? month, int? year, bool? showArchived)
        {
            var employee = await _employeeService.GetByIdAsync(id);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee record not found.";
                return RedirectToPage(new { month, year, showArchived });
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!await _employeeService.ArchiveAsync(id, reason ?? string.Empty, userId))
            {
                TempData["ErrorMessage"] = "Unable to archive the employee.";
                return RedirectToEmployee(id, month, year, showArchived);
            }

            TempData["SuccessMessage"] = $"Employee '{employee.FullName}' was archived. Payroll and financial history were preserved.";
            return RedirectToPage(new { month, year, showArchived });
        }

        public async Task<IActionResult> OnPostRestoreAsync(int id, int? month, int? year, bool? showArchived)
        {
            try
            {
                if (!await _employeeService.RestoreAsync(id))
                {
                    TempData["ErrorMessage"] = "Employee record not found or is not archived.";
                    return RedirectToPage(new { month, year, showArchived = true });
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage(new { employeeId = id, month, year, showArchived = true });
            }

            TempData["SuccessMessage"] = "Employee restored to the active directory.";
            return RedirectToPage(new { employeeId = id, month, year, showArchived = true });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, int? month, int? year, bool? showArchived)
        {
            var employee = await _employeeService.GetByIdAsync(id);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee record not found.";
                return RedirectToPage(new { month, year, showArchived });
            }

            var dependencies = await _employeeService.GetDependenciesAsync(id);
            if (!dependencies.CanHardDelete)
            {
                TempData["ErrorMessage"] =
                    $"'{employee.FullName}' cannot be permanently deleted because linked records exist " +
                    $"({dependencies.PayrollRecords} payroll, {dependencies.SalaryAdvances} advances, " +
                    $"{dependencies.FuelLogs} fuel, {dependencies.CharterBookings} charter, " +
                    $"{dependencies.VehicleAssignments} vehicle assignments). Archive the employee instead.";
                return RedirectToEmployee(id, month, year, showArchived);
            }

            if (!await _employeeService.DeleteAsync(id))
            {
                TempData["ErrorMessage"] = "Unable to permanently delete the employee.";
                return RedirectToEmployee(id, month, year, showArchived);
            }

            TempData["SuccessMessage"] = $"Employee '{employee.FullName}' was permanently deleted.";
            return RedirectToPage(new { month, year, showArchived });
        }

        private IActionResult RedirectToEmployee(int id, int? month, int? year, bool? showArchived)
        {
            return RedirectToPage(new { employeeId = id, month, year, showArchived });
        }

        private static string NormalizeStatusLabel(string status)
        {
            return status switch
            {
                "OnLeave" => "On Leave",
                "Inactive" => "Inactive",
                _ => "Active"
            };
        }
    }
}
