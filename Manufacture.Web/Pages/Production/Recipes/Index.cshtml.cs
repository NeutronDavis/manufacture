using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Manufacture.Services;

namespace Manufacture.Pages.Production.Recipes
{
    /// <summary>
    /// Recipe list. Each card is costed live by the engine, so cards always
    /// reflect the current ingredient master prices.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly IProductionService _productionService;
        private readonly RecipeCostingEngine _engine;

        public IndexModel(IProductionService productionService, RecipeCostingEngine engine)
        {
            _productionService = productionService;
            _engine = engine;
        }

        public List<RecipeCostingResultDto> Recipes { get; set; } = new();
        public List<ProductionCategory> Categories { get; set; } = new();
        public int UserRoleId { get; set; }

        public void OnGet()
        {
            UserRoleId = HttpContext.Session.GetRoleId();
            Recipes = _engine.CostAll(includeInactive: true);
            Categories = _productionService.GetCategories(includeInactive: true)
                .Where(category => category.IsActive
                    || Recipes.Any(recipe => recipe.ProductionCategoryId == category.Id))
                .OrderBy(category => category.Name)
                .ToList();
        }

        public IActionResult OnPostToggleStatus(int id)
        {
            var recipe = _productionService.GetRecipeById(id);
            if (recipe == null) return RedirectToPage();

            _productionService.SoftDeleteRecipe(id);
            TempData["SuccessMessage"] = recipe.IsActive
                ? $"Recipe '{recipe.Name}' deactivated. It is hidden from new production planning."
                : $"Recipe '{recipe.Name}' reactivated.";
            return RedirectToPage();
        }
    }
}
