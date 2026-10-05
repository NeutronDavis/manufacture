using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Manufacture.Services;

namespace Manufacture.Pages.Production.Batches
{
    /// <summary>
    /// Batch output log. Hosts the daily production entry modal
    /// (doc/recipe-costing.md UI #2) which snaps real yield against live costing.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly MockProductionService _productionService;
        private readonly MockSalesService _salesService;
        private readonly RecipeCostingEngine _engine;

        public IndexModel(
            MockProductionService productionService,
            MockSalesService salesService,
            RecipeCostingEngine engine)
        {
            _productionService = productionService;
            _salesService = salesService;
            _engine = engine;
        }

        public List<ProductionBatch> Batches { get; set; } = new();
        public List<Recipe> ActiveRecipes { get; set; } = new();
        public List<Order> AvailableOrders { get; set; } = new();

        [BindProperty]
        public DailyProductionEntryDto Entry { get; set; } = new();

        public void OnGet(int? recipeId)
        {
            Load(recipeId);
        }

        /// <summary>
        /// Live preview as the manager types an actual yield or changes input batch size,
        /// so the modal can show real-time batch and per-unit cost before committing.
        /// </summary>
        public IActionResult OnGetPreview(int recipeId, int actualYield, decimal batchInputQuantity = 1m)
        {
            var preview = _engine.PreviewDailyProduction(recipeId, actualYield, batchInputQuantity);
            var repDemand = _salesService.GetDemandForRecipe(recipeId);
            var matchingOrders = _salesService.GetOpenOrdersForRecipe(recipeId).Select(o => new
            {
                id = o.Id,
                orderNumber = o.OrderNumber,
                customerName = o.CustomerName,
                quantity = o.Items.Where(i => i.RecipeId == recipeId).Sum(i => i.Quantity)
            }).ToList();

            return new JsonResult(new
            {
                preview.IsValid,
                preview.RecipeName,
                preview.BaselineUnitName,
                preview.OutputUnit,
                preview.ExpectedYield,
                preview.ActualYield,
                preview.BatchInputQuantity,
                preview.BatchInputUnitLabel,
                preview.TotalBatchCost,
                preview.UnitCostAtActualYield,
                preview.UnitCostAtExpectedYield,
                preview.SellingPrice,
                preview.MarginAmount,
                preview.MarginPercentage,
                preview.YieldVarianceUnits,
                repDemand,
                orders = matchingOrders
            });
        }

        public IActionResult OnPostLogDailyOutput()
        {
            // Reload the table data only. Prefilling must NOT run here, or it
            // would overwrite the actual yield the manager just submitted.
            Load();

            if (Entry.LinkedOrderId.HasValue && Entry.LinkedOrderId.Value > 0)
            {
                var order = _salesService.GetOrderById(Entry.LinkedOrderId.Value);
                if (order != null)
                {
                    Entry.LinkedOrderNumber = order.OrderNumber;
                    Entry.CustomerName = order.CustomerName;
                }
            }
            else
            {
                Entry.LinkedOrderId = null;
                Entry.LinkedOrderNumber = null;
                Entry.CustomerName = null;
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] =
                    "Select a recipe and enter an actual yield of at least 1 unit.";
                return Page();
            }

            var recipe = _productionService.GetRecipeById(Entry.RecipeId);
            if (recipe == null)
            {
                TempData["ErrorMessage"] = "That recipe no longer exists.";
                return Page();
            }

            if (!recipe.IsActive)
            {
                TempData["ErrorMessage"] =
                    $"Recipe '{recipe.Name}' is deactivated. Reactivate it before logging production.";
                return Page();
            }

            var loggedBy = HttpContext.Session.GetString("UserName") ?? "Production Manager";
            var batch = _productionService.LogDailyProduction(Entry, _engine, loggedBy);

            var variance = batch.Variance;
            var varianceText = variance == 0
                ? "exactly on plan"
                : variance > 0
                    ? $"{variance} above plan"
                    : $"{Math.Abs(variance)} short of plan";

            var orderMsg = !string.IsNullOrEmpty(batch.LinkedOrderNumber) ? $" (Linked to Order {batch.LinkedOrderNumber} - {batch.CustomerName})" : "";
            TempData["SuccessMessage"] =
                $"Logged {batch.BatchNumber}{orderMsg}: {batch.ActualQuantity} {recipe.OutputUnit} " +
                $"(batch cost ₦{batch.TotalBatchCost:N0}, unit cost ₦{batch.CalculatedUnitCost:N2}, {varianceText}).";

            if (batch.CalculatedUnitCost > recipe.SellingPrice)
            {
                TempData["ErrorMessage"] =
                    $"Head's up: actual unit cost ₦{batch.CalculatedUnitCost:N2} is above the " +
                    $"₦{recipe.SellingPrice:N2} selling price for '{recipe.Name}'.";
            }

            Entry = new DailyProductionEntryDto();
            Load();
            return Page();
        }

        /// <summary>
        /// Loads the table data. Pass a recipe id only on GET to prefill the entry
        /// modal; never on POST, or the submitted values would be clobbered.
        /// </summary>
        private void Load(int? recipeId = null)
        {
            Batches = _productionService.GetAllBatches();
            ActiveRecipes = _productionService.GetAllRecipes();
            AvailableOrders = _salesService.GetOpenOrdersForRecipe(null);

            if (recipeId is > 0)
            {
                var recipe = _productionService.GetRecipeById(recipeId.Value);
                Entry.RecipeId = recipeId.Value;
                Entry.ActualYield = (int)(recipe?.ExpectedYield ?? 100m);
                Entry.Shift = "Morning Shift";
                Entry.BakerInCharge = HttpContext.Session.GetString("UserName") ?? "Production Manager";
                Entry.BatchInputQuantity = 1m;
                if (recipe != null)
                {
                    Entry.ProductCategory = recipe.ProductType;
                    var labels = _productionService.GetCategoryLabels(recipe.ProductType);
                    Entry.BatchInputUnitLabel = labels.InputLabel;
                    Entry.TargetOrderQuantity = _salesService.GetDemandForRecipe(recipe.Id);
                }
            }
        }
    }
}
