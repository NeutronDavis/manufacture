using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Production.Batches
{
    public class DetailsModel : PageModel
    {
        private readonly MockProductionService _productionService;
        private readonly MockSalesService _salesService;
        private readonly RecipeCostingEngine _engine;

        public DetailsModel(
            MockProductionService productionService,
            MockSalesService salesService,
            RecipeCostingEngine engine)
        {
            _productionService = productionService;
            _salesService = salesService;
            _engine = engine;
        }

        public ProductionBatch Batch { get; set; } = null!;
        public Recipe? Recipe { get; set; }
        public CategoryLabelsDto CategoryLabels { get; set; } = new();
        public int RepDemand { get; set; }

        [BindProperty]
        public ActualOutputInputModel OutputInput { get; set; } = new();

        public class ActualOutputInputModel
        {
            public int BatchId { get; set; }
            public int ActualQuantity { get; set; }
            public BatchStatus Status { get; set; } = BatchStatus.Completed;
            public string? ShiftNotes { get; set; }
            public string? BakerInCharge { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var batch = _productionService.GetBatchById(id);
            if (batch == null)
            {
                TempData["ErrorMessage"] = $"Batch #{id} was not found.";
                return RedirectToPage("/Production/Batches/Index");
            }

            Batch = batch;
            Recipe = _productionService.GetRecipeById(batch.RecipeId);
            CategoryLabels = _productionService.GetCategoryLabels(batch.ProductCategory);
            RepDemand = _salesService.GetDemandForRecipe(batch.RecipeId);

            OutputInput.BatchId = batch.Id;
            OutputInput.ActualQuantity = batch.ActualQuantity > 0 ? batch.ActualQuantity : batch.TargetQuantity;
            OutputInput.Status = batch.Status == BatchStatus.Completed ? BatchStatus.Completed : BatchStatus.Completed;
            OutputInput.BakerInCharge = batch.BakerInCharge;

            return Page();
        }

        /// <summary>
        /// Live calculation endpoint for the Actual Output entry card on the batch details page.
        /// </summary>
        public IActionResult OnGetCalculation(int batchId, int actualQuantity)
        {
            var batch = _productionService.GetBatchById(batchId);
            if (batch == null)
                return new JsonResult(new { success = false });

            var recipe = _productionService.GetRecipeById(batch.RecipeId);
            var inputQty = batch.BatchInputQuantity > 0 ? batch.BatchInputQuantity : (batch.FlourBagsUsed > 0 ? batch.FlourBagsUsed : 1m);
            var materialCost = batch.TotalBatchCost > 0 ? batch.TotalBatchCost : _productionService.CalculateBatchMaterialCost(batch.RecipeId, inputQty);

            var overhead = (recipe?.PackagingCost ?? 0m) + (recipe?.LaborAndOverheadPerUnit ?? 0m);
            var actualUnitCost = actualQuantity > 0 ? Math.Round(_productionService.CalculateRunUnitCost(materialCost, actualQuantity) + overhead, 2) : 0m;
            var plannedYield = batch.TargetQuantity > 0 ? batch.TargetQuantity : (int)((recipe?.ExpectedYield ?? 100m) * inputQty);
            var expectedUnitCost = plannedYield > 0 ? Math.Round((materialCost / plannedYield) + overhead, 2) : 0m;
            var varianceUnits = actualQuantity - batch.TargetQuantity;
            var compliancePercent = batch.TargetQuantity > 0 ? Math.Round(((decimal)actualQuantity / batch.TargetQuantity) * 100m, 1) : 100m;
            var costVariancePerUnit = actualQuantity > 0 ? actualUnitCost - expectedUnitCost : 0m;

            return new JsonResult(new
            {
                success = true,
                materialCost,
                actualUnitCost,
                expectedUnitCost,
                costVariancePerUnit,
                varianceUnits,
                compliancePercent,
                outputUnit = batch.OutputUnitLabel,
                inventoryEffect = $"+{actualQuantity:N0} {batch.OutputUnitLabel} to POS"
            });
        }

        public IActionResult OnPostRecordActual()
        {
            if (OutputInput.ActualQuantity < 0)
            {
                ModelState.AddModelError("OutputInput.ActualQuantity", "Actual output quantity cannot be negative.");
            }

            var batch = _productionService.GetBatchById(OutputInput.BatchId);
            if (batch == null)
            {
                TempData["ErrorMessage"] = "Batch was not found.";
                return RedirectToPage("/Production/Batches/Index");
            }

            if (!ModelState.IsValid)
            {
                Batch = batch;
                Recipe = _productionService.GetRecipeById(batch.RecipeId);
                CategoryLabels = _productionService.GetCategoryLabels(batch.ProductCategory);
                RepDemand = _salesService.GetDemandForRecipe(batch.RecipeId);
                return Page();
            }

            var updated = _productionService.RecordBatchActualOutput(
                OutputInput.BatchId,
                OutputInput.ActualQuantity,
                OutputInput.Status,
                OutputInput.ShiftNotes,
                OutputInput.BakerInCharge);

            if (updated != null)
            {
                TempData["SuccessMessage"] =
                    $"Actual output recorded for '{updated.BatchNumber}': {updated.ActualQuantity:N0} {updated.OutputUnitLabel} produced. " +
                    $"Unit production cost calculated: ₦{updated.CalculatedUnitCost:N2} (Yield Variance: {updated.Variance:+0;-0;0} {updated.OutputUnitLabel}).";
            }

            return RedirectToPage("/Production/Batches/Details", new { id = OutputInput.BatchId });
        }
    }
}
