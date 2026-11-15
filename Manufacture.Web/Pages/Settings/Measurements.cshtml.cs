using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Manufacture.Services;

namespace Manufacture.Pages.Settings
{
    public class MeasurementsModel : PageModel
    {
        private readonly IMeasurementDensityService _measurementService;
        private readonly RbacCache _rbacService;

        public MeasurementsModel(IMeasurementDensityService measurementService, RbacCache rbacService)
        {
            _measurementService = measurementService;
            _rbacService = rbacService;
        }

        public List<MeasurementDensityDto> Densities { get; set; } = new();

        [BindProperty]
        public MeasurementDensityDto DensityInput { get; set; } = new() { IsActive = true };

        public async Task<IActionResult> OnGetAsync(int? editId, CancellationToken cancellationToken = default)
        {
            var role = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            if (!_rbacService.HasAccess(role, "Settings"))
                return RedirectToPage("/Dashboard/Index");

            await LoadAsync(cancellationToken);
            if (editId.HasValue)
            {
                DensityInput = Densities.FirstOrDefault(d => d.Id == editId.Value) ?? DensityInput;
            }
            return Page();
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
                await _measurementService.SaveAsync(DensityInput, cancellationToken);
                TempData["SuccessMessage"] = "Measurement density saved successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleAsync(int id, CancellationToken cancellationToken = default)
        {
            await _measurementService.ToggleStatusAsync(id, cancellationToken);
            return RedirectToPage();
        }

        private async Task LoadAsync(CancellationToken cancellationToken)
        {
            Densities = await _measurementService.GetAllAsync(includeInactive: true, cancellationToken);
        }
    }
}
