using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;

namespace Manufacture.Pages.HR.Employees
{
    public class EditModel : PageModel
    {
        private readonly MockEmployeeService _employeeService;

        public EditModel(MockEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [BindProperty]
        public EmployeeDto EmployeeInput { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var emp = _employeeService.GetById(id);
            if (emp == null) return RedirectToPage("/HR/Index");

            EmployeeInput = new EmployeeDto
            {
                Id = emp.Id,
                FullName = emp.FullName,
                StaffCode = emp.StaffCode,
                Department = emp.Department,
                Designation = emp.Designation,
                PhoneNumber = emp.PhoneNumber,
                Email = emp.Email,
                Address = emp.Address,
                BaseSalary = emp.BaseSalary,
                EmergencyContactName = emp.EmergencyContactName,
                EmergencyContactPhone = emp.EmergencyContactPhone,
                EmergencyRelationship = emp.EmergencyRelationship,
                GuarantorName = emp.GuarantorName,
                GuarantorPhone = emp.GuarantorPhone,
                GuarantorAddress = emp.GuarantorAddress,
                GuarantorOccupation = emp.GuarantorOccupation,
                EmploymentDate = emp.EmploymentDate,
                IsActive = emp.IsActive
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();

            _employeeService.Update(EmployeeInput);
            TempData["SuccessMessage"] = $"Profile for '{EmployeeInput.FullName}' updated.";
            return RedirectToPage("/HR/Employees/Details", new { id = EmployeeInput.Id });
        }
    }
}
