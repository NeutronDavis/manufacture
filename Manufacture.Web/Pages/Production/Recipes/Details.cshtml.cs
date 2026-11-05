using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Manufacture.Services;

namespace Manufacture.Pages.Production.Recipes
{
    /// <summary>
    /// Read-only view of a recipe's full costing breakdown: batch-level material
    /// consumption, per-unit allocation, and the margin against the configured
    /// selling price.
    /// </summary>
    public class DetailsModel : PageModel
    {
        private readonly MockProductionService _productionService;
        private readonly RecipeCostingEngine _engine;

        public DetailsModel(MockProductionService productionService, RecipeCostingEngine engine)
        {
            _productionService = productionService;
            _engine = engine;
        }

        public Recipe Recipe { get; set; } = new();
        public RecipeCostingResultDto Costing { get; set; } = new();
        public List<ProductionBatch> RecentBatches { get; set; } = new();

        public void OnGet(int id)
        {
            var recipe = _productionService.GetRecipeById(id);
            if (recipe == null)
            {
                Response.Redirect("/Production/Recipes/Index");
                return;
            }

            Recipe = recipe;
            Costing = _engine.Cost(recipe);
            RecentBatches = _productionService.GetAllBatches()
                .Where(b => b.RecipeId == recipe.Id)
                .Take(5)
                .ToList();
        }
    }
}
