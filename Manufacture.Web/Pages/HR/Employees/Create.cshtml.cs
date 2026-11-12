using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.HR.Employees
{
    public class CreateModel : PageModel
    {
        private readonly IEmployeeService _employeeService;
        private readonly IImageUploadService _imageUploadService;
        private readonly IOrganizationService _organizationService;

        public CreateModel(
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

        public async Task OnGetAsync()
        {
            EmployeeInput.BaseSalary = 90000;
            EmployeeInput.EmploymentStatus = "Active";
            EmployeeInput.IsActive = true;
            EmployeeInput.EmploymentDate = DateTime.UtcNow.Date;
            await LoadLookupsAsync();
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

                var employee = await _employeeService.CreateAsync(EmployeeInput);
                TempData["SuccessMessage"] = $"Employee '{employee.FullName}' ({employee.StaffCode}) registered successfully.";
                return RedirectToPage("/HR/Index", new { employeeId = employee.Id });
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
