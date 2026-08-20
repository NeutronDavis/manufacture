using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;

namespace Manufacture.Pages.HR.Employees
{
    public class CreateModel : PageModel
    {
        private readonly MockEmployeeService _employeeService;

        public CreateModel(MockEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [BindProperty]
        public EmployeeDto EmployeeInput { get; set; } = new();

        public void OnGet()
        {
            EmployeeInput.BaseSalary = 90000;
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();

            var emp = _employeeService.Create(EmployeeInput);
            TempData["SuccessMessage"] = $"Employee '{emp.FullName}' ({emp.StaffCode}) registered successfully.";
            return RedirectToPage("/HR/Employees/Details", new { id = emp.Id });
        }
    }
}
