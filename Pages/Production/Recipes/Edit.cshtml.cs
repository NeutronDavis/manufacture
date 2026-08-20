using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;

namespace Manufacture.Pages.Production.Recipes
{
    public class EditModel : PageModel
    {
        private readonly MockProductionService _productionService;

        public EditModel(MockProductionService productionService)
        {
            _productionService = productionService;
        }

        [BindProperty]
        public RecipeDto RecipeInput { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var recipe = _productionService.GetRecipeById(id);
            if (recipe == null) return RedirectToPage("/Production/Recipes/Index");

            RecipeInput = new RecipeDto
            {
                Id = recipe.Id,
                Name = recipe.Name,
                Code = recipe.Code,
                Description = recipe.Description,
                PackagingCost = recipe.PackagingCost,
                LaborAndOverheadPerUnit = recipe.LaborAndOverheadPerUnit,
                SellingPrice = recipe.SellingPrice,
                Ingredients = recipe.Ingredients.Select(i => new RecipeIngredientDto
                {
                    IngredientId = i.IngredientId,
                    IngredientName = i.IngredientName,
                    Unit = i.Unit,
                    QuantityPerUnit = i.QuantityPerUnit,
                    IngredientUnitCost = i.IngredientUnitCost
                }).ToList()
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();

            _productionService.UpdateRecipe(RecipeInput);
            TempData["SuccessMessage"] = $"Recipe '{RecipeInput.Name}' updated successfully.";
            return RedirectToPage("/Production/Recipes/Details", new { id = RecipeInput.Id });
        }
    }
}
