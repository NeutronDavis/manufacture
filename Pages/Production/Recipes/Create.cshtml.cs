using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Manufacture.Services;

namespace Manufacture.Pages.Production.Recipes
{
    /// <summary>
    /// Recipe Builder (doc/recipe-costing.md UI #1): define a product's ingredient
    /// quantities against a single baseline production unit, and see the
    /// per-unit cost breakdown derived from the expected batch yield.
    /// </summary>
    public class CreateModel : PageModel
    {
        private readonly MockProductionService _productionService;
        private readonly RecipeCostingEngine _engine;

        public CreateModel(MockProductionService productionService, RecipeCostingEngine engine)
        {
            _productionService = productionService;
            _engine = engine;
        }

        [BindProperty]
        public RecipeBuilderDto RecipeInput { get; set; } = new();

        public List<Ingredient> AvailableIngredients { get; set; } = new();
        public List<Recipe> ExistingRecipes { get; set; } = new();
        public IReadOnlyList<BaselineOptionDto> Baselines { get; set; } = RecipeCatalog.Baselines;
        public IReadOnlyList<ProductType> ProductTypes { get; set; } = RecipeCatalog.ProductTypes;

        public void OnGet()
        {
            LoadFormData();
            SeedDefaults();
        }

        public IActionResult OnPost()
        {
            LoadFormData();

            var yield = RecipeInput.ExpectedYield > 0 ? RecipeInput.ExpectedYield : 1m;
            foreach (var ing in RecipeInput.Ingredients)
            {
                if (ing.QuantityPerBatch > 0)
                {
                    ing.QuantityPerUnit = Math.Round(ing.QuantityPerBatch / yield, 6);
                }
            }

            // Drop blank ingredient rows the user left behind.
            RecipeInput.Ingredients = RecipeInput.Ingredients
                .Where(i => i.IngredientId > 0 && i.QuantityPerBatch > 0)
                .ToList();

            if (!ModelState.IsValid || RecipeInput.Ingredients.Count == 0)
            {
                if (RecipeInput.Ingredients.Count == 0 && ModelState.IsValid)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Add at least one raw material with a quantity greater than zero.");
                }

                SeedDefaults(preserveInput: true);
                return Page();
            }

            // Warn (but do not block) when the configured price cannot cover cost.
            var created = _productionService.CreateRecipe(RecipeInput);
            var costing = _engine.Cost(created);

            if (costing.HasNegativeMargin)
            {
                TempData["ErrorMessage"] =
                    $"Recipe saved, but it is priced BELOW cost: margin is " +
                    $"₦{Math.Abs(costing.MarginAmount):N2} per unit. Review the selling price.";
            }
            else if (costing.HasThinMargin)
            {
                TempData["SuccessMessage"] =
                    $"Recipe '{created.Name}' created. Base unit cost is " +
                    $"₦{costing.TotalUnitCost:N2} with a thin margin of {costing.MarginPercentage}%.";
            }

            return RedirectToPage("/Production/Recipes/Details", new { id = created.Id });
        }

        private void LoadFormData()
        {
            AvailableIngredients = _productionService.GetAllIngredients();
            ExistingRecipes = _productionService.GetAllRecipes();
        }

        private void SeedDefaults(bool preserveInput = false)
        {
            if (preserveInput)
            {
                // Re-seed only a completely empty expected yield.
                if (RecipeInput.ExpectedYield <= 0) RecipeInput.ExpectedYield = 200m;
                if (string.IsNullOrWhiteSpace(RecipeInput.OutputUnit))
                    RecipeInput.OutputUnit = RecipeCatalog.OutputUnitFor(
                        RecipeCatalog.ParseBaseline(RecipeInput.BaselineUnit));
                return;
            }

            RecipeInput.ProductType = nameof(ProductType.Bread);
            RecipeInput.BaselineUnit = nameof(BaselineUnit.FlourBag50Kg);
            RecipeInput.ExpectedYield = 200m;
            RecipeInput.OutputUnit = "loaves";
            RecipeInput.Code = "BRD-NEW";
        }
    }
}
