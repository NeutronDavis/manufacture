using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.HR.Employees
{
    public class CreateModel : PageModel
    {
        private readonly IEmployeeService _employeeService;

        public CreateModel(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [BindProperty]
        public EmployeeDto EmployeeInput { get; set; } = new();

        public void OnGet()
        {
            EmployeeInput.BaseSalary = 90000;
            EmployeeInput.EmploymentStatus = "Active";
            EmployeeInput.IsActive = true;
            EmployeeInput.EmploymentDate = DateTime.UtcNow.Date;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var employee = await _employeeService.CreateAsync(EmployeeInput);
                TempData["SuccessMessage"] = $"Employee '{employee.FullName}' ({employee.StaffCode}) registered successfully.";
                return RedirectToPage("/HR/Index", new { employeeId = employee.Id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }
}
