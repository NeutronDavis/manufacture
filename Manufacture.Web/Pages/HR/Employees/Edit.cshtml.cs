using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.HR.Employees
{
    public class EditModel : PageModel
    {
        private readonly IEmployeeService _employeeService;

        public EditModel(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [BindProperty]
        public EmployeeDto EmployeeInput { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var employee = await _employeeService.GetByIdAsync(id);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee record not found.";
                return RedirectToPage("/HR/Index");
            }

            EmployeeInput = employee;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var updated = await _employeeService.UpdateAsync(EmployeeInput);
                TempData[updated ? "SuccessMessage" : "ErrorMessage"] = updated
                    ? $"Profile for '{EmployeeInput.FullName}' updated."
                    : "Employee record not found.";

                return RedirectToPage("/HR/Index", new { employeeId = EmployeeInput.Id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }
}
