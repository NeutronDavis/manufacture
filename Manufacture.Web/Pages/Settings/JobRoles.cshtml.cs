using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Settings
{
    public class JobRolesModel : PageModel
    {
        private readonly IOrganizationService _organizationService;

        public JobRolesModel(IOrganizationService organizationService)
        {
            _organizationService = organizationService;
        }

        public List<JobRoleDto> JobRoles { get; set; } = new();
        public List<DepartmentDto> Departments { get; set; } = new();

        [BindProperty]
        public JobRoleDto Input { get; set; } = new() { IsActive = true };

        public async Task OnGetAsync(int? editId, CancellationToken cancellationToken = default)
        {
            await LoadAsync(cancellationToken);
            if (editId.HasValue)
            {
                var role = JobRoles.FirstOrDefault(r => r.Id == editId.Value);
                if (role != null)
                {
                    Input = role;
                }
            }
        }

        public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                await LoadAsync(cancellationToken);
                return Page();
            }

            try
            {
                await _organizationService.SaveJobRoleAsync(Input, cancellationToken);
                TempData["SuccessMessage"] = "Job role saved successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleAsync(int id, CancellationToken cancellationToken = default)
        {
            await _organizationService.ToggleJobRoleStatusAsync(id, cancellationToken);
            return RedirectToPage();
        }

        private async Task LoadAsync(CancellationToken cancellationToken)
        {
            JobRoles = await _organizationService.GetJobRolesAsync(includeInactive: true, cancellationToken: cancellationToken);
            Departments = await _organizationService.GetDepartmentsAsync(includeInactive: true, cancellationToken);
        }
    }
}
