using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
        public Dictionary<int, Recipe> LineRecipes { get; set; } = new();
        public List<ProductionDefectDto> BatchDefects { get; set; } = new();
        public int TotalBatchDefects => BatchDefects.Sum(defect => defect.Quantity);

        [BindProperty]
        public RecordBatchActualsDto ActualsInput { get; set; } = new();

        [BindProperty]
        public DefectInputModel DefectInput { get; set; } = new();

        public class DefectInputModel
        {
            public int BatchId { get; set; }
            public int RecipeId { get; set; }
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

            Load(batch);
            return Page();
        }

        public IActionResult OnPostRecordActuals()
        {
            var batch = _productionService.GetBatchById(ActualsInput.BatchId);
            if (batch == null)
            {
                TempData["ErrorMessage"] = "Batch was not found.";
                return RedirectToPage("/Production/Batches/Index");
            }

            if (!ModelState.IsValid)
            {
                Load(batch);
                return Page();
            }

            var actuals = (ActualsInput.Lines ?? new List<BatchActualLineDto>())
                .GroupBy(l => l.LineId)
                .ToDictionary(g => g.Key, g => g.Last().ActualQuantity);

            if (actuals.Count == 0 || actuals.Values.All(quantity => quantity <= 0))
            {
                ModelState.AddModelError(string.Empty, "Enter an actual output greater than zero for at least one product before saving.");
                Load(batch);
                return Page();
            }

            try
            {
                var updated = _productionService.RecordBatchActuals(batch.Id, actuals, HttpContext.Session.GetString("UserName") ?? "Production Manager");
                if (updated != null)
                {
                    TempData["SuccessMessage"] =
                        $"Actual output recorded for '{updated.BatchNumber}': {updated.ActualQuantity:N0} units across {updated.Lines.Count} product line(s). Stock posted to the default factory repository.";
                }

                return RedirectToPage(new { id = batch.Id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                Load(batch);
                return Page();
            }
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

            try
            {
                var recordedBy = HttpContext.Session.GetString("UserName") ?? "Production Manager";
                _productionService.SaveBatchDefects(
                    batch.Id,
                    DefectInput.RecipeId,
                    DefectInput.Quantity,
                    DefectInput.Reason,
                    recordedBy);

                TempData["SuccessMessage"] = DefectInput.Quantity == 0
                    ? $"Defect log cleared for batch {batch.BatchNumber}."
                    : $"Logged {DefectInput.Quantity:N0} defective unit(s) for batch {batch.BatchNumber}.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage(new { id = batch.Id });
        }

        private void Load(ProductionBatch batch)
        {
            Batch = batch;
            BatchDefects = _productionService.GetBatchDefects(batch.Id);
            LineRecipes = batch.Lines
                .Select(line => line.RecipeId)
                .Distinct()
                .Select(id => _productionService.GetRecipeById(id))
                .Where(r => r != null)
                .ToDictionary(r => r!.Id, r => r!);

            ActualsInput.BatchId = batch.Id;
            ActualsInput.Lines = batch.Lines.Select(line => new BatchActualLineDto
            {
                LineId = line.Id,
                ActualQuantity = line.ActualQuantity
            }).ToList();

            DefectInput.BatchId = batch.Id;
            DefectInput.RecipeId = batch.Lines.FirstOrDefault()?.RecipeId ?? 0;
            DefectInput.Quantity = 0;
            DefectInput.Reason = BatchDefects.FirstOrDefault()?.DefectReason ?? string.Empty;
        }
    }
}
