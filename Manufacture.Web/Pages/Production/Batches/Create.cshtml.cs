using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Production.Batches
{
    public class CreateModel : PageModel
    {
        private readonly MockProductionService _productionService;
        private readonly MockSalesService _salesService;
        private readonly RecipeCostingEngine _engine;

        public CreateModel(
            MockProductionService productionService,
            MockSalesService salesService,
            RecipeCostingEngine engine)
        {
            _productionService = productionService;
            _salesService = salesService;
            _engine = engine;
        }

        [BindProperty]
        public ProductionBatchDto BatchInput { get; set; } = new();

        public List<Recipe> Recipes { get; set; } = new();

        public List<Order> AvailableOrders { get; set; } = new();

        public CategoryLabelsDto CurrentCategoryLabels { get; set; } = new();

        public int CurrentRecipeRepDemand { get; set; }

        public void OnGet(int? recipeId, int? orderId)
        {
            Recipes = _productionService.GetAllRecipes(includeInactive: false);
            AvailableOrders = _salesService.GetOpenOrdersForRecipe(null);

            Order? selectedOrder = null;
            if (orderId.HasValue && orderId.Value > 0)
            {
                selectedOrder = _salesService.GetOrderById(orderId.Value);
            }

            Recipe? selectedRecipe = null;
            if (recipeId.HasValue && recipeId.Value > 0)
            {
                selectedRecipe = Recipes.FirstOrDefault(r => r.Id == recipeId.Value);
            }
            else if (selectedOrder != null && selectedOrder.Items.Any())
            {
                var firstItem = selectedOrder.Items.First();
                selectedRecipe = Recipes.FirstOrDefault(r => r.Id == firstItem.RecipeId);
            }
            selectedRecipe ??= Recipes.FirstOrDefault();

            if (selectedRecipe != null)
            {
                BatchInput.RecipeId = selectedRecipe.Id;
                BatchInput.RecipeName = selectedRecipe.Name;
                BatchInput.ProductCategory = selectedRecipe.ProductType;
                BatchInput.OutputUnitLabel = selectedRecipe.OutputUnit;

                CurrentCategoryLabels = _productionService.GetCategoryLabels(selectedRecipe.ProductType);
                BatchInput.BatchInputUnitLabel = CurrentCategoryLabels.InputLabel;

                CurrentRecipeRepDemand = _salesService.GetDemandForRecipe(selectedRecipe.Id);
                BatchInput.TargetOrderQuantity = CurrentRecipeRepDemand;

                BatchInput.BatchInputQuantity = 1.0m;
                var plannedYield = (int)selectedRecipe.ExpectedYield;
                
                if (selectedOrder != null)
                {
                    BatchInput.LinkedOrderId = selectedOrder.Id;
                    BatchInput.LinkedOrderNumber = selectedOrder.OrderNumber;
                    BatchInput.CustomerName = selectedOrder.CustomerName;
                    var orderItemQty = selectedOrder.Items.Where(i => i.RecipeId == selectedRecipe.Id).Sum(i => i.Quantity);
                    BatchInput.TargetQuantity = orderItemQty > 0 ? orderItemQty : (CurrentRecipeRepDemand > 0 ? CurrentRecipeRepDemand : plannedYield);
                }
                else
                {
                    BatchInput.TargetQuantity = CurrentRecipeRepDemand > 0 ? CurrentRecipeRepDemand : plannedYield;
                }
                BatchInput.ActualQuantity = BatchInput.TargetQuantity;
            }
            else
            {
                CurrentCategoryLabels = _productionService.GetCategoryLabels(ProductType.Bread);
                BatchInput.BatchInputQuantity = 1.0m;
                BatchInput.TargetQuantity = 100;
                BatchInput.ActualQuantity = 100;
            }

            BatchInput.BakerInCharge = HttpContext.Session.GetString("UserName") ?? "Emeka Obi";
            BatchInput.Shift = "Morning Shift";
            BatchInput.ProductionDate = DateTime.UtcNow;
            BatchInput.Status = "Completed";
        }

        /// <summary>
        /// Real-time live calculation for the batch creation form as the manager changes
        /// recipe, input batch quantity, or actual yield.
        /// </summary>
        public IActionResult OnGetCalculation(int recipeId, decimal batchInputQuantity, int actualYieldQuantity)
        {
            var recipe = _productionService.GetRecipeById(recipeId);
            if (recipe == null)
            {
                return new JsonResult(new { success = false });
            }

            var inputQty = batchInputQuantity > 0 ? batchInputQuantity : 1m;
            var categoryLabels = _productionService.GetCategoryLabels(recipe.ProductType);
            var repDemand = _salesService.GetDemandForRecipe(recipeId);
            var matchingOrders = _salesService.GetOpenOrdersForRecipe(recipeId).Select(o => new
            {
                id = o.Id,
                orderNumber = o.OrderNumber,
                customerName = o.CustomerName,
                status = o.Status.ToString(),
                targetDeliveryDate = o.TargetDeliveryDate!.ToString("yyyy-MM-dd"),
                quantity = o.Items.Where(i => i.RecipeId == recipeId).Sum(i => i.Quantity)
            }).ToList();

            var totalMaterialCost = _productionService.CalculateBatchMaterialCost(recipeId, inputQty);
            var plannedYield = (int)(recipe.ExpectedYield * inputQty);
            var actualUnitCost = _productionService.CalculateRunUnitCost(totalMaterialCost, actualYieldQuantity);
            var expectedUnitCost = plannedYield > 0 ? Math.Round(totalMaterialCost / plannedYield, 2) : 0m;
            var variancePerUnit = actualYieldQuantity > 0 ? actualUnitCost - expectedUnitCost : 0m;

            return new JsonResult(new
            {
                success = true,
                recipeId = recipe.Id,
                recipeName = recipe.Name,
                productType = recipe.ProductType.ToString(),
                outputUnit = recipe.OutputUnit,
                baselineDisplayName = RecipeCatalog.BaselineDisplayName(recipe.BaselineUnit),
                expectedYieldPerBatch = recipe.ExpectedYield,
                plannedYield,
                sellingPrice = recipe.SellingPrice,
                repDemand,
                orders = matchingOrders,
                categoryLabels = new
                {
                    inputLabel = categoryLabels.InputLabel,
                    outputLabel = categoryLabels.OutputLabel,
                    placeholderInput = categoryLabels.PlaceholderInput,
                    defaultOutputUnit = categoryLabels.DefaultOutputUnit,
                    productionStandardTip = categoryLabels.ProductionStandardTip
                },
                totalBatchMaterialCost = totalMaterialCost,
                actualUnitProductionCost = actualUnitCost,
                expectedUnitProductionCost = expectedUnitCost,
                costVariancePerUnit = variancePerUnit
            });
        }

        public IActionResult OnPost()
        {
            if (BatchInput.LinkedOrderId.HasValue && BatchInput.LinkedOrderId.Value > 0)
            {
                var order = _salesService.GetOrderById(BatchInput.LinkedOrderId.Value);
                if (order != null)
                {
                    BatchInput.LinkedOrderNumber = order.OrderNumber;
                    BatchInput.CustomerName = order.CustomerName;
                }
            }
            else
            {
                BatchInput.LinkedOrderId = null;
                BatchInput.LinkedOrderNumber = null;
                BatchInput.CustomerName = null;
            }

            if (!ModelState.IsValid)
            {
                Recipes = _productionService.GetAllRecipes(includeInactive: false);
                AvailableOrders = _salesService.GetOpenOrdersForRecipe(null);
                CurrentCategoryLabels = _productionService.GetCategoryLabels(BatchInput.ProductCategory);
                CurrentRecipeRepDemand = _salesService.GetDemandForRecipe(BatchInput.RecipeId);
                return Page();
            }

            var batch = _productionService.CreateBatch(BatchInput, _engine);
            var unitLabel = string.IsNullOrWhiteSpace(batch.OutputUnitLabel) ? "units" : batch.OutputUnitLabel;
            var orderTag = !string.IsNullOrEmpty(batch.LinkedOrderNumber) ? $" linked to Order {batch.LinkedOrderNumber} ({batch.CustomerName})" : "";
            TempData["SuccessMessage"] = batch.ActualQuantity > 0
                ? $"Production batch '{batch.BatchNumber}'{orderTag} recorded: {batch.ActualQuantity:N0} {unitLabel} (batch cost ₦{batch.TotalBatchCost:N0}, unit cost ₦{batch.CalculatedUnitCost:N2}, variance: {batch.Variance:+0;-0;0})."
                : $"Production batch '{batch.BatchNumber}'{orderTag} planned ({batch.Status}): Target {batch.TargetQuantity:N0} {unitLabel}. Actual output can be recorded on this view page upon shift completion.";

            return RedirectToPage("/Production/Batches/Details", new { id = batch.Id });
        }
    }
}
