using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.Terminals
{
    public class CreateModel : PageModel
    {
        private readonly ISalesService _salesService;
        private readonly IEmployeeService _employeeService;

        public CreateModel(ISalesService salesService, IEmployeeService employeeService)
        {
            _salesService = salesService;
            _employeeService = employeeService;
        }

        [BindProperty]
        public PosTerminal Terminal { get; set; } = new()
        {
            TerminalCode = "POS-04",
            TerminalName = "Retail Checkout Register 4",
            CashAccountName = "Bakery Cash Till #4",
            TransferAccountName = "FirstBank PLC - 3021948291 (Bakery Ops)",
            PosBankAccountName = "Moniepoint POS #04 - 9028129",
            IsActive = true
        };

        public List<EmployeeDto> EligibleEmployees { get; set; } = new();

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            await LoadEmployeesAsync(cancellationToken);
            var all = await _salesService.GetTerminalsAsync(includeInactive: true, cancellationToken);
            var nextNum = all.Any() ? all.Max(t => t.Id) + 1 : 1;
            Terminal.TerminalCode = $"POS-{nextNum:00}";
            Terminal.TerminalName = $"Checkout Register #{nextNum}";
            Terminal.CashAccountName = $"Cash Till #{nextNum}";
            Terminal.PosBankAccountName = $"Moniepoint POS #{nextNum:00} - 9028129";
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
                var created = await _salesService.CreateTerminalAsync(Terminal, cancellationToken);
                TempData["SuccessMessage"] = $"POS Terminal '{created.TerminalName}' ({created.TerminalCode}) registered successfully.";
                return RedirectToPage("/Sales/Terminals/Index");
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
