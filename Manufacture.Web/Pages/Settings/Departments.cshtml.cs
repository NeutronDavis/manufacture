using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Settings
{
    public class DepartmentsModel : PageModel
    {
        private readonly IOrganizationService _organizationService;

        public DepartmentsModel(IOrganizationService organizationService)
        {
            _organizationService = organizationService;
        }

        public List<DepartmentDto> Departments { get; set; } = new();

        [BindProperty]
        public DepartmentDto Input { get; set; } = new() { IsActive = true };

        public async Task OnGetAsync(int? editId, CancellationToken cancellationToken = default)
        {
            Departments = await _organizationService.GetDepartmentsAsync(includeInactive: true, cancellationToken);
            if (editId.HasValue)
            {
                var department = Departments.FirstOrDefault(d => d.Id == editId.Value);
                if (department != null)
                {
                    Input = department;
                }
            }
        }

        public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                Departments = await _organizationService.GetDepartmentsAsync(true, cancellationToken);
                return Page();
            }

            try
            {
                await _organizationService.SaveDepartmentAsync(Input, cancellationToken);
                TempData["SuccessMessage"] = "Department saved successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleAsync(int id, CancellationToken cancellationToken = default)
        {
            await _organizationService.ToggleDepartmentStatusAsync(id, cancellationToken);
            return RedirectToPage();
        }
    }
}
