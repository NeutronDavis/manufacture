using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.Terminals
{
    public class AssignModel : PageModel
    {
        private readonly ISalesService _salesService;
        private readonly IEmployeeService _employeeService;

        public AssignModel(ISalesService salesService, IEmployeeService employeeService)
        {
            _salesService = salesService;
            _employeeService = employeeService;
        }

        public PosTerminal Terminal { get; set; } = new();
        public List<EmployeeDto> EligibleEmployees { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken = default)
        {
            var term = await _salesService.GetTerminalAsync(id, cancellationToken);
            if (term == null)
            {
                return RedirectToPage("/Sales/Terminals/Index");
            }

            Terminal = term;
            await LoadEmployeesAsync(cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id, int? selectedEmployeeId, CancellationToken cancellationToken = default)
        {
            var employee = selectedEmployeeId.HasValue
                ? (await _employeeService.GetByIdAsync(selectedEmployeeId.Value, cancellationToken))
                : null;

            var success = await _salesService.AssignTerminalEmployeeAsync(id, selectedEmployeeId, cancellationToken);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? $"Terminal assigned to {employee?.FullName ?? "Unassigned"}."
                : "Terminal assignment failed.";

            return RedirectToPage("/Sales/Terminals/Index");
        }

        private async Task LoadEmployeesAsync(CancellationToken cancellationToken)
        {
            var employees = await _employeeService.GetAllAsync(includeInactive: false, includeDeleted: false, cancellationToken);
            EligibleEmployees = employees
                .Where(e => e.Department.Contains("Sales", StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.FullName)
                .ToList();
        }
    }
}
