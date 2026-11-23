using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Manufacture.Models.Domain;

namespace Manufacture.Pages.Production.Batches
{
    public class CreateModel : PageModel
    {
        private readonly IProductionService _productionService;

        public CreateModel(IProductionService productionService)
        {
            _productionService = productionService;
        }

        [BindProperty]
        public ProductionBatchDto BatchInput { get; set; } = new();

        public List<Recipe> Recipes { get; set; } = new();
        public List<ProductionShift> Shifts { get; set; } = new();

        public void OnGet(int? recipeId)
        {
            LoadFormData();

            var selectedRecipe = recipeId.HasValue && recipeId.Value > 0
                ? Recipes.FirstOrDefault(recipe => recipe.Id == recipeId.Value)
                : null;
            selectedRecipe ??= Recipes.FirstOrDefault();

            var defaultShift = Shifts.FirstOrDefault()?.Name ?? "Morning Shift";
            BatchInput.Shift = defaultShift;
            BatchInput.BakerInCharge = HttpContext.Session.GetString("UserName") ?? string.Empty;
            BatchInput.ProductionDate = DateTime.UtcNow;
            BatchInput.Status = "Completed";
            BatchInput.BatchInputQuantity = 1.0m;
            BatchInput.LinkedOrderId = null;
            BatchInput.LinkedOrderNumber = null;
            BatchInput.CustomerName = null;
            BatchInput.TargetOrderQuantity = 0;

            if (selectedRecipe != null)
            {
                BatchInput.RecipeId = selectedRecipe.Id;
                BatchInput.RecipeName = selectedRecipe.Name;
                BatchInput.ProductCategory = selectedRecipe.ProductType;
                BatchInput.BatchInputUnitLabel = selectedRecipe.Baseline;
                BatchInput.OutputUnitLabel = selectedRecipe.OutputUnit;
                BatchInput.TargetQuantity = (int)selectedRecipe.ExpectedYield;
                BatchInput.ActualQuantity = (int)selectedRecipe.ExpectedYield;
            }
            else
            {
                BatchInput.BatchInputUnitLabel = "Batch";
                BatchInput.OutputUnitLabel = "units";
                BatchInput.TargetQuantity = 100;
                BatchInput.ActualQuantity = 100;
            }
        }

        public IActionResult OnGetCalculation(int recipeId, decimal batchInputQuantity, int actualYieldQuantity)
        {
            var recipe = _productionService.GetRecipeById(recipeId);
            if (recipe == null) return new JsonResult(new { success = false });

            var inputQty = batchInputQuantity > 0 ? batchInputQuantity : 1m;
            var totalMaterialCost = _productionService.CalculateBatchMaterialCost(recipeId, inputQty);
            var plannedYield = (int)(recipe.ExpectedYield * inputQty);
            var actualUnitCost = _productionService.CalculateRunUnitCost(totalMaterialCost, actualYieldQuantity);
            var expectedUnitCost = plannedYield > 0 ? Math.Round(totalMaterialCost / plannedYield, 2) : 0m;
            var costVariancePerUnit = actualYieldQuantity > 0 ? actualUnitCost - expectedUnitCost : 0m;

            return new JsonResult(new
            {
                success = true,
                recipeId = recipe.Id,
                recipeName = recipe.Name,
                categoryName = string.IsNullOrWhiteSpace(recipe.CategoryName) ? recipe.ProductType.ToString() : recipe.CategoryName,
                baselineDisplayName = string.IsNullOrWhiteSpace(recipe.Baseline) ? "Baseline batch" : recipe.Baseline,
                expectedYieldPerBatch = recipe.ExpectedYield,
                plannedYield,
                outputUnit = recipe.OutputUnit,
                sellingPrice = recipe.SellingPrice,
                totalBatchMaterialCost = totalMaterialCost,
                actualUnitProductionCost = actualUnitCost,
                expectedUnitProductionCost = expectedUnitCost,
                costVariancePerUnit = costVariancePerUnit
            });
        }

        public IActionResult OnPost()
        {
            LoadFormData();

            // Production is planned against stock and expected demand, not linked
            // to a specific customer order.
            BatchInput.LinkedOrderId = null;
            BatchInput.LinkedOrderNumber = null;
            BatchInput.CustomerName = null;
            BatchInput.TargetOrderQuantity = 0;

            if (!ModelState.IsValid)
            {
                EnsureFormDefaults();
                return Page();
            }

            try
            {
                var loggedBy = HttpContext.Session.GetString("UserName") ?? "Production Manager";
                var batch = _productionService.CreateBatch(BatchInput, loggedBy);
                var unitLabel = string.IsNullOrWhiteSpace(batch.OutputUnitLabel) ? "units" : batch.OutputUnitLabel;
                TempData["SuccessMessage"] = batch.ActualQuantity > 0
                    ? $"Production batch '{batch.BatchNumber}' recorded: {batch.ActualQuantity:N0} {unitLabel} " +
                      $"(batch cost ₦{batch.TotalBatchCost:N0}, unit cost ₦{batch.CalculatedUnitCost:N2}, variance: {batch.Variance:+0;-0;0})."
                    : $"Production batch '{batch.BatchNumber}' planned ({batch.Status}): target {batch.TargetQuantity:N0} {unitLabel}. " +
                      "Actual output can be recorded on the batch details page.";

                return RedirectToPage("/Production/Batches/Details", new { id = batch.Id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsureFormDefaults();
                return Page();
            }
        }

        private void LoadFormData()
        {
            Recipes = _productionService.GetAllRecipes(includeInactive: false);
            Shifts = _productionService.GetShifts();
        }

        private void EnsureFormDefaults()
        {
            if (string.IsNullOrWhiteSpace(BatchInput.Shift))
                BatchInput.Shift = Shifts.FirstOrDefault()?.Name ?? "Morning Shift";
            if (string.IsNullOrWhiteSpace(BatchInput.BakerInCharge))
                BatchInput.BakerInCharge = HttpContext.Session.GetString("UserName") ?? string.Empty;
            if (BatchInput.BatchInputQuantity <= 0)
                BatchInput.BatchInputQuantity = 1m;
        }
    }
}
