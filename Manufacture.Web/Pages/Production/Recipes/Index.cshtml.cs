using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
        private readonly MockProductionService _productionService;
        private readonly RecipeCostingEngine _engine;

        public IndexModel(MockProductionService productionService, RecipeCostingEngine engine)
        {
            _productionService = productionService;
            _engine = engine;
        }

        public List<RecipeCostingResultDto> Recipes { get; set; } = new();

        public void OnGet()
        {
            Recipes = _engine.CostAll(includeInactive: true);
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
