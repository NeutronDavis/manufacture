using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Production.Recipes
{
    public class CreateModel : PageModel
    {
        private readonly MockProductionService _productionService;

        public CreateModel(MockProductionService productionService)
        {
            _productionService = productionService;
        }

        [BindProperty]
        public RecipeDto RecipeInput { get; set; } = new();

        public List<Ingredient> AvailableIngredients { get; set; } = new();

        public void OnGet()
        {
            AvailableIngredients = _productionService.GetAllIngredients();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                AvailableIngredients = _productionService.GetAllIngredients();
                return Page();
            }

            // Filter out ingredients with 0 quantity
            RecipeInput.Ingredients = RecipeInput.Ingredients
                .Where(i => i.QuantityPerUnit > 0)
                .ToList();

            var recipe = _productionService.CreateRecipe(RecipeInput);
            TempData["SuccessMessage"] = $"Recipe '{recipe.Name}' created successfully with base unit cost ₦{recipe.TotalUnitCost:N2}.";
            return RedirectToPage("/Production/Recipes/Details", new { id = recipe.Id });
        }
    }
}
