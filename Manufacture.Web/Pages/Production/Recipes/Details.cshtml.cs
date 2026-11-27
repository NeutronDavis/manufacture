using Microsoft.AspNetCore.Mvc;
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
        public int UserRoleId { get; set; }
        public bool IsSuperAdmin =>
            UserRoleId == (int)UserRole.SuperAdmin;

        public void OnGet(int id)
        {
            LoadRole();
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

        public IActionResult OnPostDelete(int id)
        {
            LoadRole();

            if (!IsSuperAdmin)
            {
                TempData["ErrorMessage"] = "Only a Super Admin can delete a recipe.";
                return RedirectToPage(new { id });
            }

            var recipe = _productionService.GetRecipeById(id);
            if (recipe == null)
            {
                TempData["ErrorMessage"] = "Recipe not found.";
                return RedirectToPage("/Production/Recipes/Index");
            }

            var blocker = _productionService.GetRecipeDeleteBlocker(id);
            if (!string.IsNullOrWhiteSpace(blocker))
            {
                TempData["ErrorMessage"] = blocker;
                return RedirectToPage(new { id });
            }

            if (!_productionService.DeleteRecipe(id))
            {
                TempData["ErrorMessage"] = $"Unable to delete recipe '{recipe.Name}'.";
                return RedirectToPage(new { id });
            }

            TempData["SuccessMessage"] = $"Recipe '{recipe.Name}' was permanently deleted.";
            return RedirectToPage("/Production/Recipes/Index");
        }

        private void LoadRole()
        {
            UserRoleId = HttpContext.Session.GetRoleId();
        }
    }
}
