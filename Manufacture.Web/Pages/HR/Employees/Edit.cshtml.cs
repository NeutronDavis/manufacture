using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.HR.Employees
{
    public class EditModel : PageModel
    {
        private readonly IEmployeeService _employeeService;
        private readonly IImageUploadService _imageUploadService;
        private readonly IOrganizationService _organizationService;

        public EditModel(
            IEmployeeService employeeService,
            IImageUploadService imageUploadService,
            IOrganizationService organizationService)
        {
            _employeeService = employeeService;
            _imageUploadService = imageUploadService;
            _organizationService = organizationService;
        }

        [BindProperty]
        public EmployeeDto EmployeeInput { get; set; } = new();

        [BindProperty]
        public IFormFile? PassportFile { get; set; }

        public List<DepartmentDto> Departments { get; set; } = new();
        public List<JobRoleDto> JobRoles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var employee = await _employeeService.GetByIdAsync(id);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee record not found.";
                return RedirectToPage("/HR/Index");
            }

            if (employee.IsDeleted)
            {
                TempData["ErrorMessage"] = "Archived employees must be restored before editing.";
                return RedirectToPage("/HR/Index", new { employeeId = id, showArchived = true });
            }

            EmployeeInput = employee;
            await LoadLookupsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadLookupsAsync();
                return Page();
            }

            try
            {
                if (PassportFile is { Length: > 0 })
                {
                    await using var stream = PassportFile.OpenReadStream();
                    EmployeeInput.PassportUrl = await _imageUploadService.UploadAsync(
                        stream,
                        PassportFile.FileName,
                        "uploads/employees");
                }

                var updated = await _employeeService.UpdateAsync(EmployeeInput);
                TempData[updated ? "SuccessMessage" : "ErrorMessage"] = updated
                    ? $"Profile for '{EmployeeInput.FullName}' updated."
                    : "Employee record not found.";

                return RedirectToPage("/HR/Index", new { employeeId = EmployeeInput.Id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadLookupsAsync();
                return Page();
            }
        }

        private async Task LoadLookupsAsync()
        {
            Departments = await _organizationService.GetDepartmentsAsync();
            JobRoles = await _organizationService.GetJobRolesAsync(EmployeeInput.DepartmentId);
        }
    }
}
