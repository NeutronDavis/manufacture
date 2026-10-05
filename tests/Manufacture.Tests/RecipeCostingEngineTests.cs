using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Manufacture.Services;
using Xunit;

namespace Manufacture.Tests
{
    /// <summary>
    /// Tests for the yield-based unit cost calculation and the dynamic price
    /// updating logic required by doc/recipe-costing.md section 5.
    /// <para>
    /// A controlled recipe is built on top of the seeded service so every figure
    /// below is exact:
    /// <list type="bullet">
    ///   <item>Flour: 10 kg @ ₦100/kg</item>
    ///   <item>Sugar: 4 kg @ ₦50/kg</item>
    ///   <item>Batch cost = 1000 + 200 = ₦1,200</item>
    ///   <item>Expected yield 100 -> materials ₦12.00/unit</item>
    ///   <item>Packaging ₦3 + Labor ₦5 -> total unit cost ₦20.00</item>
    ///   <item>Selling ₦30 -> margin ₦10.00 (33.33%)</item>
    /// </list>
    /// </para>
    /// </summary>
    public class RecipeCostingEngineTests
    {
        private const decimal FlourPrice = 100m;
        private const decimal SugarPrice = 50m;
        private const decimal ExpectedBatchCost = 1200m; // 10*100 + 4*50
        private const decimal ExpectedYield = 100m;
        private const decimal Overhead = 8m;   // packaging 3 + labor 5
        private const decimal SellingPrice = 30m;

        private readonly MockProductionService _production;
        private readonly RecipeCostingEngine _engine;
        private readonly Ingredient _flour;
        private readonly Ingredient _sugar;
        private readonly Recipe _recipe;

        public RecipeCostingEngineTests()
        {
            _production = new MockProductionService();
            _engine = new RecipeCostingEngine(_production);

            _flour = _production.CreateIngredient(new Ingredient
            {
                Name = "Test Wheat Flour",
                Unit = "kg",
                UnitCost = FlourPrice,
                CurrentStock = 500m,
                Category = "Test"
            });

            _sugar = _production.CreateIngredient(new Ingredient
            {
                Name = "Test White Sugar",
                Unit = "kg",
                UnitCost = SugarPrice,
                CurrentStock = 200m,
                Category = "Test"
            });

            _recipe = _production.CreateRecipe(new RecipeBuilderDto
            {
                Name = "Test Loaf",
                Code = "TST-001",
                ProductType = nameof(ProductType.Bread),
                BaselineUnit = nameof(BaselineUnit.FlourBag50Kg),
                ExpectedYield = ExpectedYield,
                OutputUnit = "loaves",
                PackagingCost = 3m,
                LaborAndOverheadPerUnit = 5m,
                SellingPrice = SellingPrice,
                Ingredients = new List<RecipeIngredientInputDto>
                {
                    new() { IngredientId = _flour.Id, QuantityUnit = "kg", QuantityPerBatch = 10m },
                    new() { IngredientId = _sugar.Id, QuantityUnit = "kg", QuantityPerBatch = 4m }
                }
            });
        }

        // =============================================================
        // Total batch cost
        // =============================================================

        [Fact]
        public void Cost_TotalBatchCost_SumsQuantityTimesLiveUnitPrice()
        {
            // Spec: TotalBatchCost = SUM(ingredient batch qty * current unit price)
            var result = _engine.Cost(_recipe);

            Assert.Equal(ExpectedBatchCost, result.TotalBatchCost);
        }

        [Fact]
        public void Cost_Lines_ExposeBothBatchAndPerUnitFigures()
        {
            var result = _engine.Cost(_recipe);

            Assert.Equal(2, result.Lines.Count);

            var flour = result.Lines.Single(l => l.IngredientId == _flour.Id);
            Assert.Equal(10m, flour.QuantityPerBatch);
            Assert.Equal(0.1m, flour.QuantityPerUnit);           // 10 / 100
            Assert.Equal(FlourPrice, flour.UnitCost);
            Assert.Equal(1000m, flour.BatchLineCost);
            Assert.Equal(10m, flour.UnitLineCost);
        }

        [Fact]
        public void Cost_LineShares_SumToOneHundredPercent()
        {
            var result = _engine.Cost(_recipe);

            Assert.Equal(100m, result.Lines.Sum(l => l.ShareOfBatchPercent));
        }

        // =============================================================
        // Yield-based unit cost
        // =============================================================

        [Fact]
        public void Cost_UnitCost_IsBatchCostDividedByExpectedYield()
        {
            // Spec: ingredient per unit = batch qty / expected batch yield
            var result = _engine.Cost(_recipe);

            Assert.Equal(12m, result.IngredientsCostPerUnit);
            Assert.Equal(20m, result.TotalUnitCost);
        }

        [Fact]
        public void Cost_AtExpectedYield_IsTheDefaultResult()
        {
            var atDefault = _engine.Cost(_recipe);
            var atExplicit = _engine.CostAtActualYield(_recipe, ExpectedYield);

            Assert.Equal(atDefault.TotalUnitCost, atExplicit.TotalUnitCost);
            Assert.True(atExplicit.UsesActualYield);
            Assert.False(atDefault.UsesActualYield);
        }

        [Fact]
        public void CostAtActualYield_LowerYield_RaisesUnitCost()
        {
            // Spec 1.C: actual unit cost = total batch cost / actual batch yield
            // 1200 / 60 = 20 + 8 overhead = 28
            var result = _engine.CostAtActualYield(_recipe, 60m);

            Assert.Equal(60m, result.EffectiveYield);
            Assert.Equal(20m, result.IngredientsCostPerUnit);
            Assert.Equal(28m, result.TotalUnitCost);
        }

        [Fact]
        public void CostAtActualYield_HigherYield_LowersUnitCost()
        {
            // 1200 / 150 = 8 + 8 overhead = 16
            var result = _engine.CostAtActualYield(_recipe, 150m);

            Assert.Equal(8m, result.IngredientsCostPerUnit);
            Assert.Equal(16m, result.TotalUnitCost);
        }

        [Fact]
        public void CostAtActualYield_ZeroYield_FallsBackToExpectedYield()
        {
            var result = _engine.CostAtActualYield(_recipe, 0m);

            Assert.Equal(ExpectedYield, result.EffectiveYield);
            Assert.Equal(20m, result.TotalUnitCost);
        }

        [Fact]
        public void Cost_ReportsYieldVarianceAgainstPlan()
        {
            var result = _engine.CostAtActualYield(_recipe, 90m);

            Assert.Equal(-10m, result.BatchYieldVarianceUnits);
            Assert.Equal(-10m, result.BatchYieldVariancePercent);
        }

        // =============================================================
        // Profitability
        // =============================================================

        [Theory]
        [InlineData("loaves", "loaf")]
        [InlineData("bottles", "bottle")]
        [InlineData("bags", "bag")]
        [InlineData("racks", "rack")]
        [InlineData("knives", "knife")]
        [InlineData("loaf", "loaf")]          // already singular, left alone
        [InlineData("unit", "unit")]
        [InlineData("", "unit")]
        [InlineData(null, "unit")]
        public void Singularize_ProducesUsablePerUnitLabels(string? plural, string expected)
        {
            Assert.Equal(expected, RecipeCostingResultDto.Singularize(plural));
        }

        [Fact]
        public void Cost_SingularOutputUnit_IsDerivedFromTheOutputUnit()
        {
            var result = _engine.Cost(_recipe);

            Assert.Equal("loaf", result.SingularOutputUnit);
        }

        [Fact]
        public void Cost_Margin_IsSellingPriceLessTotalUnitCost()
        {
            var result = _engine.Cost(_recipe);

            Assert.Equal(SellingPrice, result.SellingPrice);
            Assert.Equal(10m, result.MarginAmount);
            Assert.Equal(33.33m, result.MarginPercentage);
            Assert.False(result.HasNegativeMargin);
            Assert.False(result.HasThinMargin);
        }

        [Fact]
        public void Cost_NegativeMargin_IsFlagged()
        {
            _recipe.SellingPrice = 15m; // below the ₦20 unit cost

            var result = _engine.Cost(_recipe);

            Assert.Equal(-5m, result.MarginAmount);
            Assert.True(result.HasNegativeMargin);
        }

        [Fact]
        public void Cost_ThinMargin_IsFlaggedBelowFifteenPercent()
        {
            _recipe.SellingPrice = 22m; // ₦2 margin on ₦22 = 9.09%

            var result = _engine.Cost(_recipe);

            Assert.Equal(2m, result.MarginAmount);
            Assert.True(result.HasThinMargin);
            Assert.False(result.HasNegativeMargin);
        }

        [Fact]
        public void Cost_ZeroSellingPrice_DoesNotDivideByZero()
        {
            _recipe.SellingPrice = 0m;

            var result = _engine.Cost(_recipe);

            Assert.Equal(0m, result.MarginPercentage);
        }

        [Fact]
        public void Cost_NullRecipe_ReturnsEmptyResult()
        {
            var result = _engine.Cost(null);

            Assert.Equal(0m, result.TotalBatchCost);
            Assert.Empty(result.Lines);
        }

        // =============================================================
        // Dynamic price updating
        // =============================================================

        [Fact]
        public void UpdateIngredientPrice_RecalculatesBatchAndUnitCost()
        {
            // Flour ₦100 -> ₦200/kg. New batch = 2000 + 200 = 2200.
            var applied = _engine.UpdateIngredientPrice(_flour.Id, 200m);

            Assert.True(applied);

            var result = _engine.Cost(_recipe);
            Assert.Equal(2200m, result.TotalBatchCost);
            Assert.Equal(22m, result.IngredientsCostPerUnit);
            Assert.Equal(30m, result.TotalUnitCost);
            Assert.Equal(0m, result.MarginAmount);
        }

        [Fact]
        public void UpdateIngredientPrice_CascadesToEveryRecipeUsingIt()
        {
            // A second recipe consuming the same flour.
            _production.CreateRecipe(new RecipeBuilderDto
            {
                Name = "Second Test Loaf",
                Code = "TST-002",
                ProductType = nameof(ProductType.Bread),
                BaselineUnit = nameof(BaselineUnit.FlourBag50Kg),
                ExpectedYield = 50m,
                OutputUnit = "loaves",
                SellingPrice = 100m,
                Ingredients = new List<RecipeIngredientInputDto>
                {
                    new() { IngredientId = _flour.Id, QuantityUnit = "kg", QuantityPerBatch = 20m }
                }
            });

            _engine.UpdateIngredientPrice(_flour.Id, 200m);

            var all = _engine.CostAll(includeInactive: true);
            Assert.Equal(2200m, all.Single(r => r.RecipeCode == "TST-001").TotalBatchCost);
            Assert.Equal(4000m, all.Single(r => r.RecipeCode == "TST-002").TotalBatchCost);
        }

        [Fact]
        public void UpdateIngredientPrice_RefreshesTheStoredLineSnapshot()
        {
            _engine.UpdateIngredientPrice(_flour.Id, 275m);

            var line = _production.GetRecipeById(_recipe.Id)!
                .Ingredients.Single(l => l.IngredientId == _flour.Id);

            Assert.Equal(275m, line.IngredientUnitCost);
        }

        [Fact]
        public void UpdateIngredientPrice_StampsUpdatedAt()
        {
            var before = _flour.UpdatedAt;
            System.Threading.Thread.Sleep(20);

            _engine.UpdateIngredientPrice(_flour.Id, 111m);

            Assert.True(_flour.UpdatedAt > before);
        }

        [Fact]
        public void UpdateIngredientPrice_NegativePrice_IsRejected()
        {
            Assert.False(_engine.UpdateIngredientPrice(_flour.Id, -1m));
            Assert.Equal(FlourPrice, _flour.UnitCost);
        }

        [Fact]
        public void UpdateIngredientPrice_UnknownIngredient_ReturnsFalse()
        {
            Assert.False(_engine.UpdateIngredientPrice(999_999, 50m));
        }

        [Fact]
        public void ApplyPriceUpdates_AppliesEveryValidEditAndCountsThem()
        {
            var applied = _engine.ApplyPriceUpdates(new[]
            {
                new UpdateIngredientPriceDto { IngredientId = _flour.Id, NewUnitCost = 120m },
                new UpdateIngredientPriceDto { IngredientId = _sugar.Id, NewUnitCost = 60m },
                new UpdateIngredientPriceDto { IngredientId = 999_999, NewUnitCost = 10m }
            });

            Assert.Equal(2, applied);

            // 10*120 + 4*60 = 1440
            Assert.Equal(1440m, _engine.Cost(_recipe).TotalBatchCost);
        }

        [Fact]
        public void ApplyPriceUpdates_EmptyInput_AppliesNothing()
        {
            Assert.Equal(0, _engine.ApplyPriceUpdates(Array.Empty<UpdateIngredientPriceDto>()));
        }

        // =============================================================
        // Reports and previews
        // =============================================================

        [Fact]
        public void BuildProfitabilityReport_AggregatesExpectedRevenueAndMargin()
        {
            var report = _engine.BuildProfitabilityReport(includeInactive: true);

            var testRow = report.Rows.Single(r => r.RecipeCode == "TST-001");
            var expectedRevenue = testRow.SellingPrice * testRow.EffectiveYield;
            var expectedMargin = testRow.MarginAmount * testRow.EffectiveYield;

            Assert.Contains(testRow, report.Rows);
            Assert.True(report.TotalExpectedRevenue >= expectedRevenue);
            Assert.True(report.TotalExpectedMargin >= expectedMargin);
            Assert.True(report.WeightedMarginPercentage > 0);
        }

        [Fact]
        public void BuildProfitabilityReport_CountsNegativeMarginProducts()
        {
            _recipe.SellingPrice = 5m; // well below the ₦20 unit cost

            var report = _engine.BuildProfitabilityReport(includeInactive: true);

            Assert.True(report.NegativeMarginCount >= 1);
        }

        [Fact]
        public void PreviewDailyProduction_ReturnsLiveFiguresForTheGivenYield()
        {
            var preview = _engine.PreviewDailyProduction(_recipe.Id, 60m);

            Assert.True(preview.IsValid);
            Assert.Equal("Test Loaf", preview.RecipeName);
            Assert.Equal(ExpectedBatchCost, preview.TotalBatchCost);
            Assert.Equal(28m, preview.UnitCostAtActualYield);
            Assert.Equal(20m, preview.UnitCostAtExpectedYield);
            Assert.Equal(2m, preview.MarginAmount);
            Assert.Equal(-40m, preview.YieldVarianceUnits);
        }

        [Fact]
        public void PreviewDailyProduction_UnknownRecipe_IsInvalid()
        {
            var preview = _engine.PreviewDailyProduction(999_999, 100m);

            Assert.False(preview.IsValid);
        }

        [Fact]
        public void PreviewDailyProduction_DeactivatedRecipe_IsInvalid()
        {
            _production.SoftDeleteRecipe(_recipe.Id);

            var preview = _engine.PreviewDailyProduction(_recipe.Id, 100m);

            Assert.False(preview.IsValid);
        }

        [Fact]
        public void GetPriceMaster_ReportsUsageAndCategories()
        {
            var rows = _engine.GetPriceMaster();

            var flourRow = rows.Single(r => r.Id == _flour.Id);
            Assert.Equal("Test", flourRow.Category);
            Assert.Equal(1, flourRow.UsedByRecipeCount);
            Assert.False(flourRow.IsPriceStale); // seeded at construction time
        }

        [Fact]
        public void GetPriceMaster_FlagsPricesOlderThanTheStaleWindow()
        {
            _flour.UpdatedAt = DateTime.UtcNow.AddDays(-RecipeCatalog.StalePriceAfterDays - 1);

            var flourRow = _engine.GetPriceMaster().Single(r => r.Id == _flour.Id);

            Assert.True(flourRow.IsPriceStale);
        }

        // =============================================================
        // Dual-Mode (Hybrid) Recipe Costing Engine Tests
        // =============================================================

        [Fact]
        public void CalculateDualModeRecipe_BatchFirst_DerivesPerUnitQuantitiesAndCostsCorrectly()
        {
            var recipe = new RecipeDto
            {
                Id = "TEST-BRD",
                ProductName = "Test Artisan Bread",
                ProductCategory = "BREAD",
                BaselineUnitLabel = "1 Bag (50kg Flour)",
                EntryMode = RecipeEntryMode.BatchFirst,
                ExpectedBatchYield = 100m,
                Ingredients = new List<RecipeIngredientDto>
                {
                    new()
                    {
                        IngredientId = "ING-FLOUR",
                        IngredientName = "Wheat Flour",
                        UnitOfMeasure = "kg",
                        CurrentUnitCost = 1000m,
                        BatchQuantity = 50m
                    },
                    new()
                    {
                        IngredientId = "ING-SWEETENER",
                        IngredientName = "Micro Sweetener",
                        UnitOfMeasure = "kg",
                        CurrentUnitCost = 5000m,
                        BatchQuantity = 0.04m // 40 grams
                    }
                }
            };

            var calculated = _engine.CalculateDualModeRecipe(recipe);

            // 1. Flour assertions
            var flour = calculated.Ingredients[0];
            Assert.Equal(50m, flour.BatchQuantity);
            Assert.Equal(0.5m, flour.PerUnitQuantity); // 50 / 100
            Assert.Equal(50_000m, flour.BatchIngredientCost); // 50 * 1000
            Assert.Equal(500m, flour.PerUnitIngredientCost); // 0.5 * 1000

            // 2. Micro-ingredient sweetener assertions (prevents rounding loss)
            var sweetener = calculated.Ingredients[1];
            Assert.Equal(0.04m, sweetener.BatchQuantity);
            Assert.Equal(0.0004m, sweetener.PerUnitQuantity); // 0.04 / 100
            Assert.Equal(200m, sweetener.BatchIngredientCost); // 0.04 * 5000
            Assert.Equal(2m, sweetener.PerUnitIngredientCost); // 0.0004 * 5000

            // 3. Aggregate totals
            Assert.Equal(50_200m, calculated.TotalBatchCost); // 50000 + 200
            Assert.Equal(502m, calculated.CalculatedUnitProductionCost); // 50200 / 100
        }

        [Fact]
        public void CalculateDualModeRecipe_SingleUnit_DerivesBatchQuantitiesAndCostsCorrectly()
        {
            var recipe = new RecipeDto
            {
                Id = "TEST-LOAF",
                ProductName = "Single Loaf Direct Calculation",
                ProductCategory = "BREAD",
                BaselineUnitLabel = "1 Loaf Direct",
                EntryMode = RecipeEntryMode.SingleUnit,
                ExpectedBatchYield = 50m,
                Ingredients = new List<RecipeIngredientDto>
                {
                    new()
                    {
                        IngredientId = "ING-FLOUR",
                        IngredientName = "Wheat Flour",
                        UnitOfMeasure = "kg",
                        CurrentUnitCost = 1000m,
                        PerUnitQuantity = 0.8m // 800g per loaf
                    },
                    new()
                    {
                        IngredientId = "ING-FLAVOR",
                        IngredientName = "Vanilla Extract",
                        UnitOfMeasure = "ml",
                        CurrentUnitCost = 10m,
                        PerUnitQuantity = 2m // 2ml per loaf
                    }
                }
            };

            var calculated = _engine.CalculateDualModeRecipe(recipe);

            // 1. Flour assertions (Bottom-Up)
            var flour = calculated.Ingredients[0];
            Assert.Equal(0.8m, flour.PerUnitQuantity);
            Assert.Equal(40m, flour.BatchQuantity); // 0.8 * 50
            Assert.Equal(800m, flour.PerUnitIngredientCost); // 0.8 * 1000
            Assert.Equal(40_000m, flour.BatchIngredientCost); // 40 * 1000

            // 2. Flavor assertions
            var flavor = calculated.Ingredients[1];
            Assert.Equal(2m, flavor.PerUnitQuantity);
            Assert.Equal(100m, flavor.BatchQuantity); // 2 * 50
            Assert.Equal(20m, flavor.PerUnitIngredientCost); // 2 * 10
            Assert.Equal(1000m, flavor.BatchIngredientCost); // 100 * 10

            // 3. Aggregate totals
            Assert.Equal(41_000m, calculated.TotalBatchCost); // 40000 + 1000
            Assert.Equal(820m, calculated.CalculatedUnitProductionCost); // 41000 / 50
        }

        [Fact]
        public void CalculateDualModeRecipe_EquivalenceBetweenModes()
        {
            const decimal yield = 80m;
            const decimal batchFlour = 40m;
            const decimal unitFlour = 0.5m; // 40 / 80
            const decimal costPerKg = 1200m;

            var batchFirst = new RecipeDto
            {
                EntryMode = RecipeEntryMode.BatchFirst,
                ExpectedBatchYield = yield,
                Ingredients = new List<RecipeIngredientDto>
                {
                    new() { BatchQuantity = batchFlour, CurrentUnitCost = costPerKg }
                }
            };

            var singleUnit = new RecipeDto
            {
                EntryMode = RecipeEntryMode.SingleUnit,
                ExpectedBatchYield = yield,
                Ingredients = new List<RecipeIngredientDto>
                {
                    new() { PerUnitQuantity = unitFlour, CurrentUnitCost = costPerKg }
                }
            };

            var resBatch = _engine.CalculateDualModeRecipe(batchFirst);
            var resSingle = _engine.CalculateDualModeRecipe(singleUnit);

            Assert.Equal(resBatch.TotalBatchCost, resSingle.TotalBatchCost);
            Assert.Equal(resBatch.CalculatedUnitProductionCost, resSingle.CalculatedUnitProductionCost);
            Assert.Equal(resBatch.Ingredients[0].BatchQuantity, resSingle.Ingredients[0].BatchQuantity);
            Assert.Equal(resBatch.Ingredients[0].PerUnitQuantity, resSingle.Ingredients[0].PerUnitQuantity);
        }

        [Fact]
        public void MockProductionService_GetRecipeDto_And_GetAllRecipeDtos_ReturnCalculatedDtos()
        {
            var dtos = _production.GetAllRecipeDtos(RecipeEntryMode.BatchFirst, _engine);

            Assert.NotEmpty(dtos);
            var first = dtos.First();
            Assert.True(first.ExpectedBatchYield > 0);
            Assert.True(first.TotalBatchCost > 0);
            Assert.True(first.CalculatedUnitProductionCost > 0);

            var retrieved = _production.GetRecipeDto(int.Parse(first.Id), RecipeEntryMode.SingleUnit, _engine);
            Assert.NotNull(retrieved);
            Assert.Equal(RecipeEntryMode.SingleUnit, retrieved.EntryMode);
            Assert.True(Math.Abs(first.TotalBatchCost - retrieved.TotalBatchCost) <= 0.05m);
        }

        [Fact]
        public void JsonSerialization_RespectsEnumValuesAndCasing()
        {
            var dto = new RecipeDto
            {
                Id = "101",
                ProductName = "Water Bottle 18.9L",
                ProductCategory = "WATER",
                BaselineUnitLabel = "1 Dispenser Run",
                EntryMode = RecipeEntryMode.BatchFirst,
                ExpectedBatchYield = 250m,
                Ingredients = new List<RecipeIngredientDto>
                {
                    new()
                    {
                        IngredientId = "CAP",
                        IngredientName = "Bottle Cap",
                        UnitOfMeasure = "pcs",
                        CurrentUnitCost = 15m,
                        BatchQuantity = 250m
                    }
                }
            };

            _engine.CalculateDualModeRecipe(dto);

            var json = System.Text.Json.JsonSerializer.Serialize(dto);
            Assert.Contains("\"entryMode\":\"BATCH_FIRST\"", json);
            Assert.Contains("\"batchQuantity\":250", json);
            Assert.Contains("\"totalBatchCost\":3750", json);

            var deserialized = System.Text.Json.JsonSerializer.Deserialize<RecipeDto>(json);
            Assert.NotNull(deserialized);
            Assert.Equal(RecipeEntryMode.BatchFirst, deserialized.EntryMode);
            Assert.Equal(250m, deserialized.ExpectedBatchYield);
            Assert.Equal(3750m, deserialized.TotalBatchCost);
        }
    }
}
