using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Services;

namespace Manufacture.Pages.Settings
{
    public class ProductionModel : PageModel
    {
        private readonly IProductionService _productionService;
        private readonly RbacCache _rbacService;

        public ProductionModel(IProductionService productionService, RbacCache rbacService)
        {
            _productionService = productionService;
            _rbacService = rbacService;
        }

        public List<ProductionCategory> Categories { get; set; } = new();
        public List<ProductionProductType> ProductTypes { get; set; } = new();

        [BindProperty]
        public ProductionCategory CategoryInput { get; set; } = new() { IsActive = true };

        [BindProperty]
        public ProductionProductType ProductTypeInput { get; set; } = new() { IsActive = true };

        public IActionResult OnGet(int? editCategoryId, int? editProductTypeId)
        {
            var roleId = HttpContext.Session.GetRoleId();
            if (!_rbacService.HasAccess(roleId, "Settings"))
                return RedirectToPage("/Dashboard/Index");

            Load();
            if (editCategoryId.HasValue)
            {
                CategoryInput = Categories.FirstOrDefault(c => c.Id == editCategoryId.Value) ?? CategoryInput;
            }
            if (editProductTypeId.HasValue)
            {
                ProductTypeInput = ProductTypes.FirstOrDefault(p => p.Id == editProductTypeId.Value) ?? ProductTypeInput;
            }
            return Page();
        }

        public IActionResult OnPostSaveCategory()
        {
            if (!ModelState.IsValid)
            {
                Load();
                return Page();
            }

            try
            {
                _productionService.SaveCategory(CategoryInput);
                TempData["SuccessMessage"] = "Production category saved successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public IActionResult OnPostToggleCategory(int id)
        {
            _productionService.ToggleCategoryStatus(id);
            return RedirectToPage();
        }

        public IActionResult OnPostSaveProductType()
        {
            if (!ModelState.IsValid)
            {
                Load();
                return Page();
            }

            try
            {
                _productionService.SaveProductType(ProductTypeInput);
                TempData["SuccessMessage"] = "Production product type saved successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public IActionResult OnPostToggleProductType(int id)
        {
            _productionService.ToggleProductTypeStatus(id);
            return RedirectToPage();
        }

        private void Load()
        {
            Categories = _productionService.GetCategories(includeInactive: true);
            ProductTypes = _productionService.GetProductTypes(includeInactive: true);
        }
    }
}
