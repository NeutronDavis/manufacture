using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Manufacture.Models.Domain;
using Manufacture.Services;

namespace Manufacture.Pages.Production.Recipes
{
    /// <summary>
    /// Read-only view of a recipe's full costing breakdown.
    /// </summary>
    public class DetailsModel : PageModel
    {
        private readonly IProductionService _productionService;
        private readonly RecipeCostingEngine _engine;

        public DetailsModel(IProductionService productionService, RecipeCostingEngine engine)
        {
            _productionService = productionService;
            _engine = engine;
        }

        public Recipe Recipe { get; set; } = new();
        public RecipeCostingResultDto Costing { get; set; } = new();
        public List<ProductionBatch> RecentBatches { get; set; } = new();
        public string UserRole { get; set; } = string.Empty;

        public void OnGet(int id)
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? string.Empty;
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
