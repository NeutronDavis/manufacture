using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.Terminals
{
    public class EditModel : PageModel
    {
        private readonly ISalesService _salesService;
        private readonly IEmployeeService _employeeService;

        public EditModel(ISalesService salesService, IEmployeeService employeeService)
        {
            _salesService = salesService;
            _employeeService = employeeService;
        }

        [BindProperty]
        public PosTerminal Terminal { get; set; } = new();

        public List<EmployeeDto> EligibleEmployees { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken = default)
        {
            var term = await _salesService.GetTerminalAsync(id, cancellationToken);
            if (term == null)
            {
                TempData["ErrorMessage"] = $"POS Terminal #{id} was not found.";
                return RedirectToPage("/Sales/Terminals/Index");
            }

            Terminal = term;
            await LoadEmployeesAsync(cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(Terminal.TerminalName))
            {
                ModelState.AddModelError("Terminal.TerminalName", "Terminal name is required.");
                await LoadEmployeesAsync(cancellationToken);
                return Page();
            }

            try
            {
                var updated = await _salesService.UpdateTerminalAsync(Terminal, cancellationToken);
                if (updated)
                {
                    TempData["SuccessMessage"] = $"POS Terminal '{Terminal.TerminalName}' ({Terminal.TerminalCode}) updated successfully.";
                    return RedirectToPage("/Sales/Terminals/Index");
                }

                TempData["ErrorMessage"] = "Could not update the specified terminal.";
                await LoadEmployeesAsync(cancellationToken);
                return Page();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadEmployeesAsync(cancellationToken);
                return Page();
            }
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
