using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Services
{
    /// <summary>
    /// The production recipe costing engine (doc/recipe-costing.md).
    /// <para>
    /// Implements the spec formulas:
    /// <list type="bullet">
    ///   <item>Total batch cost = SUM(ingredient batch qty * current unit price)</item>
    ///   <item>Actual unit cost = total batch cost / actual batch yield</item>
    ///   <item>Profit margin = configured selling price - actual unit production cost</item>
    /// </list>
    /// Prices are ALWAYS resolved live from the ingredient master, so an admin
    /// price change cascades to every recipe that consumes the material.
    /// </para>
    /// </summary>
    public class RecipeCostingEngine
    {
        private readonly MockProductionService _productionService;

        public RecipeCostingEngine(MockProductionService productionService)
        {
            _productionService = productionService;
        }

        // =============================================================
        // Live price resolution
        // =============================================================

        /// <summary>
        /// Resolves the current unit cost for a recipe line from the ingredient
        /// master, falling back to the line's stored snapshot if the ingredient
        /// record no longer exists.
        /// </summary>
        public decimal ResolveUnitCost(RecipeIngredient line)
        {
            if (line == null) return 0m;
            var ingredient = _productionService.GetIngredientById(line.IngredientId);
            return ingredient?.UnitCost ?? line.IngredientUnitCost;
        }

        /// <summary>
        /// Updates a master ingredient price and refreshes the stored snapshot on
        /// every recipe line that consumes it, so views that read the snapshot
        /// stay in sync without a full re-cost.
        /// </summary>
        public bool UpdateIngredientPrice(int ingredientId, decimal newUnitCost)
        {
            if (newUnitCost < 0) return false;

            var ingredient = _productionService.GetIngredientById(ingredientId);
            if (ingredient == null) return false;

            ingredient.UnitCost = newUnitCost;
            ingredient.UpdatedAt = DateTime.UtcNow;
            SyncPriceSnapshot(ingredient);
            return true;
        }

        /// <summary>Applies a batch of price edits. Returns the number applied.</summary>
        public int ApplyPriceUpdates(IEnumerable<UpdateIngredientPriceDto> updates)
        {
            if (updates == null) return 0;
            var applied = 0;
            foreach (var u in updates)
            {
                if (UpdateIngredientPrice(u.IngredientId, u.NewUnitCost)) applied++;
            }
            return applied;
        }

        /// <summary>Pushes an ingredient's live price onto every recipe line using it.</summary>
        public void SyncPriceSnapshot(Ingredient ingredient)
        {
            if (ingredient == null) return;
            foreach (var recipe in _productionService.GetAllRecipes(includeInactive: true))
            {
                foreach (var line in recipe.Ingredients.Where(l => l.IngredientId == ingredient.Id))
                {
                    line.IngredientUnitCost = ingredient.UnitCost;
                }
            }
        }

        /// <summary>Refreshes every recipe line's stored price snapshot from the master.</summary>
        public int SyncAllPriceSnapshots()
        {
            var touched = 0;
            foreach (var ingredient in _productionService.GetAllIngredients())
            {
                SyncPriceSnapshot(ingredient);
                touched++;
            }
            return touched;
        }

        // =============================================================
        // Core costing
        // =============================================================

        /// <summary>Costs a recipe at its planned (expected) yield.</summary>
        public RecipeCostingResultDto Cost(Recipe? recipe)
        {
            if (recipe == null) return new RecipeCostingResultDto();
            return Cost(recipe, recipe.ExpectedYield, usesActualYield: false);
        }

        /// <summary>
        /// Costs a recipe at a real yield logged by the production manager
        /// (spec section 1.C "Actual Yield Adjustment").
        /// </summary>
        public RecipeCostingResultDto CostAtActualYield(Recipe? recipe, decimal actualYield)
        {
            if (recipe == null) return new RecipeCostingResultDto();
            return Cost(recipe, actualYield, usesActualYield: true);
        }

        /// <summary>Costs every active recipe at its expected yield.</summary>
        public List<RecipeCostingResultDto> CostAll(bool includeInactive = false)
        {
            return _productionService.GetAllRecipes(includeInactive)
                .Select(Cost)
                .ToList();
        }

        /// <summary>Builds the profitability report (spec GET /api/reports/profitability).</summary>
        public ProfitabilityReportDto BuildProfitabilityReport(bool includeInactive = false)
        {
            var rows = CostAll(includeInactive);

            var report = new ProfitabilityReportDto { Rows = rows };

            foreach (var row in rows)
            {
                report.TotalBatchCost += row.TotalBatchCost;
                report.TotalExpectedRevenue += row.SellingPrice * row.EffectiveYield;
                report.TotalExpectedMargin += row.MarginAmount * row.EffectiveYield;

                if (row.HasNegativeMargin) report.NegativeMarginCount++;
                else if (row.HasThinMargin) report.ThinMarginCount++;
                if (row.HasMissingPrice) report.MissingPriceCount++;
            }

            report.WeightedMarginPercentage = report.TotalExpectedRevenue > 0
                ? Math.Round((report.TotalExpectedMargin / report.TotalExpectedRevenue) * 100m, 2)
                : 0m;

            return report;
        }

        private RecipeCostingResultDto Cost(Recipe recipe, decimal yield, bool usesActualYield)
        {
            var effectiveYield = yield > 0 ? yield : recipe.ExpectedYield;

            var result = new RecipeCostingResultDto
            {
                RecipeId = recipe.Id,
                RecipeName = recipe.Name,
                RecipeCode = recipe.Code,
                ProductType = recipe.ProductType.ToString(),
                BaselineUnit = recipe.BaselineUnit.ToString(),
                BaselineUnitName = RecipeCatalog.BaselineDisplayName(recipe.BaselineUnit),
                OutputUnit = string.IsNullOrWhiteSpace(recipe.OutputUnit)
                    ? RecipeCatalog.OutputUnitFor(recipe.BaselineUnit)
                    : recipe.OutputUnit,
                ExpectedYield = recipe.ExpectedYield,
                EffectiveYield = effectiveYield,
                UsesActualYield = usesActualYield,
                PackagingCost = recipe.PackagingCost,
                LaborAndOverheadPerUnit = recipe.LaborAndOverheadPerUnit,
                SellingPrice = recipe.SellingPrice
            };

            // Build cost lines using live prices.
            foreach (var line in recipe.Ingredients)
            {
                var unitCost = ResolveUnitCost(line);

                // QuantityPerBatch is stored normalised to the ingredient's
                // pricing unit, so the line maths is direct.
                var batchLineCost = line.QuantityPerBatch * unitCost;
                var perUnitQty = effectiveYield > 0 ? line.QuantityPerBatch / effectiveYield : 0m;

                result.Lines.Add(new RecipeCostLineDto
                {
                    IngredientId = line.IngredientId,
                    IngredientName = line.IngredientName,
                    Unit = line.Unit,
                    QuantityPerBatch = line.QuantityPerBatch,
                    QuantityPerUnit = perUnitQty,
                    UnitCost = unitCost,
                    BatchLineCost = batchLineCost,
                    UnitLineCost = perUnitQty * unitCost
                });
            }

            // Total batch cost = SUM(ingredient batch qty * current unit price)
            result.TotalBatchCost = result.Lines.Sum(l => l.BatchLineCost);

            // Share of batch for dashboard visualisation.
            if (result.TotalBatchCost > 0)
            {
                foreach (var l in result.Lines)
                    l.ShareOfBatchPercent = Math.Round((l.BatchLineCost / result.TotalBatchCost) * 100m, 2);
            }

            // Per-unit allocation at the effective yield.
            result.IngredientsCostPerUnit = effectiveYield > 0
                ? result.TotalBatchCost / effectiveYield
                : 0m;

            result.TotalUnitCost = result.IngredientsCostPerUnit
                                   + recipe.PackagingCost
                                   + recipe.LaborAndOverheadPerUnit;

            // Profitability against the configured selling price.
            result.MarginAmount = recipe.SellingPrice - result.TotalUnitCost;
            result.MarginPercentage = recipe.SellingPrice > 0
                ? Math.Round((result.MarginAmount / recipe.SellingPrice) * 100m, 2)
                : 0m;

            // Yield variance versus the plan.
            result.BatchYieldVarianceUnits = effectiveYield - recipe.ExpectedYield;
            result.BatchYieldVariancePercent = recipe.ExpectedYield > 0
                ? Math.Round((result.BatchYieldVarianceUnits / recipe.ExpectedYield) * 100m, 2)
                : 0m;

            return result;
        }

        // =============================================================
        // Daily production preview
        // =============================================================

        /// <summary>
        /// Live preview for the daily production entry modal (spec UI #2):
        /// real-time batch cost and per-unit cost as the manager types the yield.
        /// <summary>
        /// Live preview for the daily production entry modal (spec UI #2 & plan.txt):
        /// real-time batch cost and per-unit cost as the manager types the input batch size and actual yield.
        /// </summary>
        public DailyProductionPreviewDto PreviewDailyProduction(int recipeId, decimal actualYield, decimal batchInputQuantity = 1m)
        {
            var recipe = _productionService.GetRecipeById(recipeId);
            if (recipe == null || !recipe.IsActive)
                return new DailyProductionPreviewDto { IsValid = false };

            if (batchInputQuantity <= 0) batchInputQuantity = 1m;

            var labels = _productionService.GetCategoryLabels(recipe.ProductType);
            var totalBatchMaterialCost = _productionService.CalculateBatchMaterialCost(recipeId, batchInputQuantity);
            var plannedYield = recipe.ExpectedYield * batchInputQuantity;

            var overhead = recipe.PackagingCost + recipe.LaborAndOverheadPerUnit;

            var unitCostActual = actualYield > 0
                ? Math.Round((totalBatchMaterialCost / actualYield) + overhead, 2)
                : 0m;

            var unitCostExpected = plannedYield > 0
                ? Math.Round((totalBatchMaterialCost / plannedYield) + overhead, 2)
                : 0m;

            var marginAmount = recipe.SellingPrice - unitCostActual;
            var marginPercent = recipe.SellingPrice > 0
                ? Math.Round((marginAmount / recipe.SellingPrice) * 100m, 2)
                : 0m;

            return new DailyProductionPreviewDto
            {
                IsValid = actualYield > 0,
                RecipeName = recipe.Name,
                BaselineUnitName = RecipeCatalog.BaselineDisplayName(recipe.BaselineUnit),
                OutputUnit = string.IsNullOrWhiteSpace(recipe.OutputUnit) ? labels.DefaultOutputUnit : recipe.OutputUnit,
                BatchInputQuantity = batchInputQuantity,
                BatchInputUnitLabel = labels.InputLabel,
                ExpectedYield = plannedYield,
                ActualYield = actualYield,
                TotalBatchCost = totalBatchMaterialCost,
                UnitCostAtActualYield = unitCostActual,
                UnitCostAtExpectedYield = unitCostExpected,
                SellingPrice = recipe.SellingPrice,
                MarginAmount = marginAmount,
                MarginPercentage = marginPercent,
                YieldVarianceUnits = actualYield - plannedYield
            };
        }

        // =============================================================
        // Ingredient price master
        // =============================================================

        /// <summary>Builds the admin price table rows (spec UI #3).</summary>
        public List<IngredientPriceDto> GetPriceMaster()
        {
            var cutoff = DateTime.UtcNow.AddDays(-RecipeCatalog.StalePriceAfterDays);
            var recipes = _productionService.GetAllRecipes(includeInactive: true);

            return _productionService.GetAllIngredients()
                .OrderBy(i => i.Category)
                .ThenBy(i => i.Name)
                .Select(i =>
                {
                    var usedBy = recipes.Count(r => r.Ingredients.Any(li => li.IngredientId == i.Id));

                    return new IngredientPriceDto
                    {
                        Id = i.Id,
                        Name = i.Name,
                        Unit = i.Unit,
                        Category = i.Category,
                        CurrentUnitCost = i.UnitCost,
                        CurrentStock = i.CurrentStock,
                        UpdatedAt = i.UpdatedAt,
                        UsedByRecipeCount = usedBy,
                        IsPriceStale = i.UpdatedAt < cutoff
                    };
                })
                .ToList();
        }

        // =============================================================
        // Dual-Mode (Hybrid) Recipe Costing
        // =============================================================

        /// <summary>
        /// Calculates a dual-mode recipe DTO, keeping both batch and per-unit
        /// quantities and costs synchronized regardless of entry mode:
        /// <list type="bullet">
        ///   <item><b>BatchFirst (Top-Down)</b>: BatchQty -> PerUnitQty = BatchQty / Yield</item>
        ///   <item><b>SingleUnit (Bottom-Up)</b>: PerUnitQty -> BatchQty = PerUnitQty * Yield</item>
        /// </list>
        /// </summary>
        public RecipeDto CalculateDualModeRecipe(RecipeDto? recipe, decimal? expectedBatchYieldOverride = null)
        {
            if (recipe == null) return new RecipeDto();

            var yield = expectedBatchYieldOverride.HasValue && expectedBatchYieldOverride.Value > 0
                ? expectedBatchYieldOverride.Value
                : (recipe.ExpectedBatchYield > 0 ? recipe.ExpectedBatchYield : 1m);

            recipe.ExpectedBatchYield = yield;

            foreach (var ingredient in recipe.Ingredients)
            {
                if (recipe.EntryMode == RecipeEntryMode.BatchFirst)
                {
                    // Top-Down: BatchQty is primary
                    ingredient.PerUnitQuantity = yield > 0
                        ? Math.Round(ingredient.BatchQuantity / yield, 6)
                        : 0m;
                    ingredient.BatchIngredientCost = Math.Round(ingredient.BatchQuantity * ingredient.CurrentUnitCost, 4);
                    ingredient.PerUnitIngredientCost = Math.Round(ingredient.PerUnitQuantity * ingredient.CurrentUnitCost, 4);
                }
                else
                {
                    // Bottom-Up: PerUnitQty is primary
                    ingredient.BatchQuantity = Math.Round(ingredient.PerUnitQuantity * yield, 6);
                    ingredient.PerUnitIngredientCost = Math.Round(ingredient.PerUnitQuantity * ingredient.CurrentUnitCost, 4);
                    ingredient.BatchIngredientCost = Math.Round(ingredient.BatchQuantity * ingredient.CurrentUnitCost, 4);
                }
            }

            // Aggregate totals
            recipe.TotalBatchCost = Math.Round(recipe.Ingredients.Sum(i => i.BatchIngredientCost), 2);
            recipe.CalculatedUnitProductionCost = yield > 0
                ? Math.Round(recipe.TotalBatchCost / yield, 4)
                : 0m;

            return recipe;
        }

        /// <summary>
        /// Converts an existing in-memory Recipe entity into a dual-mode RecipeDto
        /// with live ingredient pricing and computed dual quantities/costs.
        /// </summary>
        public RecipeDto ConvertEntityToRecipeDto(Recipe recipe, RecipeEntryMode mode = RecipeEntryMode.BatchFirst)
        {
            if (recipe == null) return new RecipeDto();

            var dto = new RecipeDto
            {
                Id = recipe.Id.ToString(),
                ProductName = recipe.Name,
                ProductCategory = recipe.ProductType.ToString().ToUpperInvariant(),
                BaselineUnitLabel = RecipeCatalog.BaselineDisplayName(recipe.BaselineUnit),
                EntryMode = mode,
                ExpectedBatchYield = recipe.ExpectedYield > 0 ? recipe.ExpectedYield : 1m,
                Ingredients = recipe.Ingredients.Select(line =>
                {
                    var liveCost = ResolveUnitCost(line);
                    return new RecipeIngredientDto
                    {
                        IngredientId = line.IngredientId.ToString(),
                        IngredientName = line.IngredientName,
                        UnitOfMeasure = line.Unit,
                        CurrentUnitCost = liveCost,
                        BatchQuantity = line.QuantityPerBatch,
                        PerUnitQuantity = recipe.ExpectedYield > 0
                            ? Math.Round(line.QuantityPerBatch / recipe.ExpectedYield, 6)
                            : 0m
                    };
                }).ToList()
            };

            return CalculateDualModeRecipe(dto);
        }
    }
}

