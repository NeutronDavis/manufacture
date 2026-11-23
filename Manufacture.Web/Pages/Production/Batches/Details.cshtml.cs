using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Manufacture.Models.Domain;

namespace Manufacture.Pages.Production.Batches
{
    public class DetailsModel : PageModel
    {
        private readonly IProductionService _productionService;

        public DetailsModel(IProductionService productionService)
        {
            _productionService = productionService;
        }

        public ProductionBatch Batch { get; set; } = null!;
        public Recipe? Recipe { get; set; }
        public CategoryLabelsDto CategoryLabels { get; set; } = new();
        public int RepDemand { get; set; }
        public List<ProductionDefectDto> BatchDefects { get; set; } = new();
        public int TotalBatchDefects => BatchDefects.Sum(defect => defect.Quantity);

        [BindProperty]
        public ActualOutputInputModel OutputInput { get; set; } = new();

        [BindProperty]
        public DefectInputModel DefectInput { get; set; } = new();

        public class ActualOutputInputModel
        {
            public int BatchId { get; set; }
            public int ActualQuantity { get; set; }
            public BatchStatus Status { get; set; } = BatchStatus.Completed;
            public string? ShiftNotes { get; set; }
            public string? BakerInCharge { get; set; }
        }

        public class DefectInputModel
        {
            public int BatchId { get; set; }
            public int Quantity { get; set; }
            public string? Reason { get; set; }
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
            RepDemand = _productionService.GetDemandForRecipe(batch.RecipeId);
            BatchDefects = _productionService.GetBatchDefects(batch.Id);

            DefectInput.BatchId = batch.Id;
            DefectInput.Quantity = TotalBatchDefects;
            DefectInput.Reason = BatchDefects.FirstOrDefault()?.DefectReason ?? string.Empty;

            OutputInput.BatchId = batch.Id;
            OutputInput.ActualQuantity = batch.ActualQuantity > 0 ? batch.ActualQuantity : batch.TargetQuantity;
            OutputInput.Status = BatchStatus.Completed;
            OutputInput.BakerInCharge = batch.BakerInCharge;
            return Page();
        }

        public IActionResult OnGetCalculation(int batchId, int actualQuantity)
        {
            var batch = _productionService.GetBatchById(batchId);
            if (batch == null) return new JsonResult(new { success = false });

            var recipe = _productionService.GetRecipeById(batch.RecipeId);
            var inputQty = batch.BatchInputQuantity > 0 ? batch.BatchInputQuantity : (batch.FlourBagsUsed > 0 ? batch.FlourBagsUsed : 1m);
            var materialCost = batch.TotalBatchCost > 0 ? batch.TotalBatchCost : _productionService.CalculateBatchMaterialCost(batch.RecipeId, inputQty);
            var actualUnitCost = actualQuantity > 0 ? Math.Round(materialCost / actualQuantity, 2) : 0m;
            var plannedYield = batch.TargetQuantity > 0 ? batch.TargetQuantity : (int)((recipe?.ExpectedYield ?? 100m) * inputQty);
            var expectedUnitCost = plannedYield > 0 ? Math.Round(materialCost / plannedYield, 2) : 0m;
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
                ModelState.AddModelError("OutputInput.ActualQuantity", "Actual output quantity cannot be negative.");

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
                RepDemand = _productionService.GetDemandForRecipe(batch.RecipeId);
                BatchDefects = _productionService.GetBatchDefects(batch.Id);
                DefectInput.BatchId = batch.Id;
                DefectInput.Quantity = TotalBatchDefects;
                DefectInput.Reason = BatchDefects.FirstOrDefault()?.DefectReason ?? string.Empty;
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

        public IActionResult OnPostLogDefects()
        {
            if (DefectInput.BatchId <= 0)
            {
                TempData["ErrorMessage"] = "Select a valid batch before logging defects.";
                return RedirectToPage("/Production/Batches/Index");
            }

            if (DefectInput.Quantity < 0)
            {
                TempData["ErrorMessage"] = "Defect quantity cannot be negative.";
                return RedirectToPage(new { id = DefectInput.BatchId });
            }

            var batch = _productionService.GetBatchById(DefectInput.BatchId);
            if (batch == null)
            {
                TempData["ErrorMessage"] = "Batch was not found.";
                return RedirectToPage("/Production/Batches/Index");
            }

            var recordedBy = HttpContext.Session.GetString("UserName") ?? "Production Manager";
            _productionService.SaveBatchDefects(
                batch.Id,
                DefectInput.Quantity,
                DefectInput.Reason,
                recordedBy);

            TempData["SuccessMessage"] = DefectInput.Quantity == 0
                ? $"Defect log cleared for batch {batch.BatchNumber}."
                : $"Logged {DefectInput.Quantity:N0} defected {batch.OutputUnitLabel} for batch {batch.BatchNumber}.";

            return RedirectToPage(new { id = batch.Id });
        }
    }
}
