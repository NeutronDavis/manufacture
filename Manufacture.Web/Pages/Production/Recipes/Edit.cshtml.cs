using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Manufacture.Models.Domain;
using Manufacture.Services;

namespace Manufacture.Pages.Production.Recipes
{
    /// <summary>
    /// Edits an existing product recipe in the builder. Ingredient quantities are
    /// stored per baseline batch; per-unit costs are re-derived on save.
    /// </summary>
    public class EditModel : PageModel
    {
        private readonly IProductionService _productionService;
        private readonly RecipeCostingEngine _engine;

        public EditModel(IProductionService productionService, RecipeCostingEngine engine)
        {
            _productionService = productionService;
            _engine = engine;
        }

        [BindProperty]
        public RecipeBuilderDto RecipeInput { get; set; } = new();

        public List<Ingredient> AvailableIngredients { get; set; } = new();
        public List<ProductionProductType> ProductTypes { get; set; } = new();
        public bool Found { get; set; }

        public IActionResult OnGet(int id)
        {
            var recipe = _productionService.GetRecipeById(id);
            if (recipe == null) return RedirectToPage("/Production/Recipes/Index");

            Found = true;
            RecipeInput = MapToBuilderDto(recipe);
            LoadFormData();
            return Page();
        }

        public IActionResult OnPost()
        {
            LoadFormData();
            Found = true;

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
                        "Keep at least one raw material with a quantity greater than zero.");
                }

                var productType = ProductTypes.FirstOrDefault(p => p.Id == RecipeInput.ProductionProductTypeId);
                if (productType != null)
                {
                    RecipeInput.ProductType = productType.Name;
                    RecipeInput.BaselineUnit = productType.Baseline;
                    RecipeInput.OutputUnit = productType.OutputUnitNoun;
                }
                return Page();
            }

            try
            {
                if (!_productionService.UpdateRecipe(RecipeInput))
                    return RedirectToPage("/Production/Recipes/Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }

            var updated = _productionService.GetRecipeById(RecipeInput.Id);
            var costing = _engine.Cost(updated);

            if (costing.HasNegativeMargin)
            {
                TempData["ErrorMessage"] =
                    $"Recipe updated, but it is priced BELOW cost by " +
                    $"₦{Math.Abs(costing.MarginAmount):N2} per unit.";
            }
            else
            {
                TempData["SuccessMessage"] =
                    $"Recipe '{RecipeInput.Name}' updated. Base unit cost is now " +
                    $"₦{costing.TotalUnitCost:N2} (batch cost ₦{costing.TotalBatchCost:N0}).";
            }

            return RedirectToPage("/Production/Recipes/Details", new { id = RecipeInput.Id });
        }

        private void LoadFormData()
        {
            AvailableIngredients = _productionService.GetAllIngredients();
            ProductTypes = _productionService.GetProductTypes(includeInactive: true);
        }

        private static RecipeBuilderDto MapToBuilderDto(Recipe recipe) => new()
        {
            Id = recipe.Id,
            Name = recipe.Name,
            Code = recipe.Code,
            Description = recipe.Description,
            ProductionProductTypeId = recipe.ProductionProductTypeId ?? 0,
            ProductType = string.IsNullOrWhiteSpace(recipe.ProductTypeName) ? recipe.ProductType.ToString() : recipe.ProductTypeName,
            BaselineUnit = recipe.Baseline,
            ExpectedYield = recipe.ExpectedYield,
            OutputUnit = recipe.OutputUnit,
            SellingPrice = recipe.SellingPrice,
            Ingredients = recipe.Ingredients.Select(i =>
            {
                var displayUnit = string.IsNullOrWhiteSpace(i.QuantityUnit) ? i.Unit : i.QuantityUnit;
                var displayQty = !string.Equals(displayUnit, i.Unit, StringComparison.OrdinalIgnoreCase) && UnitConverter.CanConvert(i.Unit, displayUnit, i.IngredientName)
                    ? UnitConverter.Convert(i.QuantityPerBatch, i.Unit, displayUnit, i.IngredientName)
                    : i.QuantityPerBatch;

                return new RecipeIngredientInputDto
                {
                    IngredientId = i.IngredientId,
                    IngredientName = i.IngredientName,
                    IngredientUnit = i.Unit,
                    QuantityUnit = displayUnit,
                    QuantityPerBatch = displayQty,
                    UnitCostOverride = null
                };
            }).ToList()
        };
    }
}
