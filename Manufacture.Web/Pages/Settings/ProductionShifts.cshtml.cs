using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Services;

namespace Manufacture.Pages.Settings
{
    public class ProductionShiftsModel : PageModel
    {
        private readonly IProductionService _productionService;
        private readonly RbacCache _rbacService;

        public ProductionShiftsModel(IProductionService productionService, RbacCache rbacService)
        {
            _productionService = productionService;
            _rbacService = rbacService;
        }

        public List<ProductionShift> Shifts { get; set; } = new();

        [BindProperty]
        public ProductionShift ShiftInput { get; set; } = new() { IsActive = true };

        public IActionResult OnGet(int? editId)
        {
            var roleId = HttpContext.Session.GetRoleId();
            if (!_rbacService.HasAccess(roleId, "Settings"))
                return RedirectToPage("/Dashboard/Index");

            Load();
            if (editId.HasValue)
            {
                ShiftInput = Shifts.FirstOrDefault(s => s.Id == editId.Value) ?? ShiftInput;
            }
            return Page();
        }

        public IActionResult OnPostSave()
        {
            if (!ModelState.IsValid)
            {
                Load();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(ShiftInput.Name))
            {
                ModelState.AddModelError("ShiftInput.Name", "Shift name is required.");
                Load();
                return Page();
            }

            try
            {
                _productionService.SaveShift(ShiftInput);
                TempData["SuccessMessage"] = "Production shift saved successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public IActionResult OnPostToggle(int id)
        {
            _productionService.ToggleShiftStatus(id);
            return RedirectToPage();
        }

        private void Load()
        {
            Shifts = _productionService.GetShifts(includeInactive: true);
        }
    }
}
