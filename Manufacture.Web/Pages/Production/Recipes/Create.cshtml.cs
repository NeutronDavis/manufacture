using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Manufacture.Models.Domain;
using Manufacture.Services;

namespace Manufacture.Pages.Production.Recipes
{
    /// <summary>
    /// Recipe Builder: define a product's ingredient quantities against a
    /// configurable baseline production unit and see the per-unit material cost.
    /// </summary>
    public class CreateModel : PageModel
    {
        private readonly IProductionService _productionService;
        private readonly RecipeCostingEngine _engine;

        public CreateModel(IProductionService productionService, RecipeCostingEngine engine)
        {
            _productionService = productionService;
            _engine = engine;
        }

        [BindProperty]
        public RecipeBuilderDto RecipeInput { get; set; } = new();

        public List<Ingredient> AvailableIngredients { get; set; } = new();
        public List<Recipe> ExistingRecipes { get; set; } = new();
        public List<ProductionProductType> ProductTypes { get; set; } = new();

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

            try
            {
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
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                SeedDefaults(preserveInput: true);
                return Page();
            }
        }

        private void LoadFormData()
        {
            AvailableIngredients = _productionService.GetAllIngredients();
            ExistingRecipes = _productionService.GetAllRecipes();
            ProductTypes = _productionService.GetProductTypes();
        }

        private void SeedDefaults(bool preserveInput = false)
        {
            if (preserveInput)
            {
                if (RecipeInput.ExpectedYield <= 0) RecipeInput.ExpectedYield = 200m;
                ApplyProductTypeDefaults();
                return;
            }

            var productType = ProductTypes.FirstOrDefault(p => p.Name == "Bread") ?? ProductTypes.FirstOrDefault();
            RecipeInput.ProductionProductTypeId = productType?.Id ?? 0;
            RecipeInput.ProductType = productType?.Name ?? "Bread";
            RecipeInput.BaselineUnit = productType?.Baseline ?? "1 Bag of Flour (50 kg)";
            RecipeInput.ExpectedYield = 200m;
            RecipeInput.OutputUnit = productType?.OutputUnitNoun ?? "loaves";
            RecipeInput.Code = "BRD-NEW";
        }

        private void ApplyProductTypeDefaults()
        {
            var productType = ProductTypes.FirstOrDefault(p => p.Id == RecipeInput.ProductionProductTypeId)
                ?? ProductTypes.FirstOrDefault();
            if (productType == null) return;

            RecipeInput.ProductionProductTypeId = productType.Id;
            RecipeInput.ProductType = productType.Name;
            RecipeInput.BaselineUnit = productType.Baseline;
            RecipeInput.OutputUnit = productType.OutputUnitNoun;
        }
    }
}
