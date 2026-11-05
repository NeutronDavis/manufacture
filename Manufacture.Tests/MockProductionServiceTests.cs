using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Manufacture.Services;
using Xunit;

namespace Manufacture.Tests
{
    /// <summary>
    /// Tests for the in-memory production store: builder input normalisation,
    /// the seeded 50kg-bag baseline dataset, and daily production logging.
    /// </summary>
    public class MockProductionServiceTests
    {
        private readonly MockProductionService _production;
        private readonly RecipeCostingEngine _engine;

        public MockProductionServiceTests()
        {
            _production = new MockProductionService();
            _engine = new RecipeCostingEngine(_production);
        }

        /// <summary>
        /// Fetches a seeded ingredient by id, failing with a clear message if the
        /// seed data ever changes and the row is no longer there.
        /// </summary>
        private Ingredient SeedIngredient(int id)
        {
            var found = _production.GetIngredientById(id);
            Assert.True(found != null, $"No seeded ingredient with id {id}.");
            return found!;
        }

        /// <summary>Fetches a seeded recipe by id, asserting that it exists.</summary>
        private Recipe SeedRecipe(int id)
        {
            var found = _production.GetRecipeById(id);
            Assert.True(found != null, $"No seeded recipe with id {id}.");
            return found!;
        }

        // =============================================================
        // Seeded baseline dataset
        // =============================================================

        [Fact]
        public void Seed_CreatesABreadWaterAndPopcornRecipe()
        {
            var recipes = _production.GetAllRecipes();

            Assert.Contains(recipes, r => r.ProductType == ProductType.Bread);
            Assert.Contains(recipes, r => r.ProductType == ProductType.Water);
            Assert.Contains(recipes, r => r.ProductType == ProductType.Popcorn);
        }

        [Fact]
        public void Seed_EveryBreadRecipeConsumesExactlyOne50KgBagOfFlour()
        {
            var flour = SeedIngredient(1); // Premium Wheat Flour, per kg

            var breadRecipes = _production.GetAllRecipes().Where(r => r.ProductType == ProductType.Bread);

            Assert.NotEmpty(breadRecipes);
            Assert.All(breadRecipes, r =>
            {
                var flourLine = r.Ingredients.Single(i => i.IngredientId == flour.Id);
                Assert.Equal(50m, flourLine.QuantityPerBatch);
                Assert.Equal(BaselineUnit.FlourBag50Kg, r.BaselineUnit);
            });
        }

        [Fact]
        public void Seed_EveryRecipeHasUsableYieldAndPricing()
        {
            Assert.All(_production.GetAllRecipes(), r =>
            {
                Assert.True(r.ExpectedYield > 0, $"{r.Name} has a non-positive expected yield");
                Assert.True(r.SellingPrice > 0, $"{r.Name} has no selling price");
                Assert.NotEmpty(r.Ingredients);
                Assert.False(string.IsNullOrWhiteSpace(r.OutputUnit));
            });
        }

        [Fact]
        public void Seed_IngredientLineYieldsAreSyncedToTheRecipeYield()
        {
            Assert.All(_production.GetAllRecipes(), r =>
                Assert.All(r.Ingredients, line =>
                    Assert.Equal(r.ExpectedYield, line.BaselineYield)));
        }

        [Fact]
        public void Seed_AllSeededRecipesArePricedAboveCost()
        {
            // The demo dataset should not open with any product selling at a loss.
            var losing = _engine.BuildProfitabilityReport(includeInactive: true)
                .Rows.Where(r => r.HasNegativeMargin)
                .ToList();

            Assert.Empty(losing);
        }

        [Fact]
        public void Seed_WaterRecipeIsPricedPerBottleFromOneRun()
        {
            var water = _production.GetAllRecipes().Single(r => r.ProductType == ProductType.Water);

            Assert.Equal(BaselineUnit.WaterRun18_9L, water.BaselineUnit);
            Assert.Equal("bottles", water.OutputUnit);

            // 250 bottles are filled per run.
            var bottles = water.Ingredients.Single(i => i.IngredientName.Contains("Dispenser"));
            Assert.Equal(250m, bottles.QuantityPerBatch);
        }

        [Fact]
        public void Seed_PopcornRecipeUsesOneCookingPotOfTwoKgCorn()
        {
            var popcorn = _production.GetAllRecipes().Single(r => r.ProductType == ProductType.Popcorn);

            Assert.Equal(BaselineUnit.PopcornPot, popcorn.BaselineUnit);

            var corn = popcorn.Ingredients.Single(i => i.IngredientName.Contains("Maize"));
            Assert.Equal(2m, corn.QuantityPerBatch);
        }

        // =============================================================
        // Builder input normalisation
        // =============================================================

        [Fact]
        public void CreateRecipe_NormalisesGramsIntoTheKilogramPricingUnit()
        {
            var yeast = SeedIngredient(3); // Instant Dry Yeast, per kg

            var recipe = _production.CreateRecipe(new RecipeBuilderDto
            {
                Name = "Gram Entry Loaf",
                Code = "TST-GRM",
                ProductType = nameof(ProductType.Bread),
                BaselineUnit = nameof(BaselineUnit.FlourBag50Kg),
                ExpectedYield = 100m,
                OutputUnit = "loaves",
                SellingPrice = 100m,
                Ingredients = new List<RecipeIngredientInputDto>
                {
                    new() { IngredientId = yeast.Id, QuantityUnit = "g", QuantityPerBatch = 500m }
                }
            });

            var line = recipe.Ingredients.Single();
            Assert.Equal(0.5m, line.QuantityPerBatch);       // 500 g -> 0.5 kg
            Assert.Equal("g", line.QuantityUnit);            // the entered unit is retained
            Assert.Equal(1900m, line.BatchLineCost);         // 0.5 * 3800
        }

        [Fact]
        public void CreateRecipe_DropsLinesWithNoQuantity()
        {
            var flour = SeedIngredient(1);
            var sugar = SeedIngredient(2);

            var recipe = _production.CreateRecipe(new RecipeBuilderDto
            {
                Name = "Sparse Loaf",
                Code = "TST-SPC",
                ProductType = nameof(ProductType.Bread),
                BaselineUnit = nameof(BaselineUnit.FlourBag50Kg),
                ExpectedYield = 100m,
                OutputUnit = "loaves",
                SellingPrice = 100m,
                Ingredients = new List<RecipeIngredientInputDto>
                {
                    new() { IngredientId = flour.Id, QuantityUnit = "kg", QuantityPerBatch = 50m },
                    new() { IngredientId = sugar.Id, QuantityUnit = "kg", QuantityPerBatch = 0m },
                    new() { IngredientId = 0, QuantityUnit = "kg", QuantityPerBatch = 5m }
                }
            });

            Assert.Single(recipe.Ingredients);
            Assert.Equal(flour.Id, recipe.Ingredients[0].IngredientId);
        }

        [Fact]
        public void CreateRecipe_AppliesTheBaselineDefaultOutputUnit()
        {
            var flour = SeedIngredient(1);

            var recipe = _production.CreateRecipe(new RecipeBuilderDto
            {
                Name = "Popcorn Probe",
                Code = "TST-POP",
                ProductType = nameof(ProductType.Popcorn),
                BaselineUnit = nameof(BaselineUnit.PopcornPot),
                OutputUnit = "",                               // left blank on purpose
                ExpectedYield = 60m,
                SellingPrice = 200m,
                Ingredients = new List<RecipeIngredientInputDto>
                {
                    new() { IngredientId = flour.Id, QuantityUnit = "kg", QuantityPerBatch = 0.1m }
                }
            });

            Assert.Equal("bags", recipe.OutputUnit);
        }

        [Fact]
        public void CreateRecipe_DefaultsUnknownEnumsToBreadFlourBag()
        {
            var flour = SeedIngredient(1);

            var recipe = _production.CreateRecipe(new RecipeBuilderDto
            {
                Name = "Garbage Enum Loaf",
                Code = "TST-ENM",
                ProductType = "NotAProductType",
                BaselineUnit = "NotABaseline",
                ExpectedYield = 100m,
                OutputUnit = "loaves",
                SellingPrice = 100m,
                Ingredients = new List<RecipeIngredientInputDto>
                {
                    new() { IngredientId = flour.Id, QuantityUnit = "kg", QuantityPerBatch = 50m }
                }
            });

            Assert.Equal(ProductType.Bread, recipe.ProductType);
            Assert.Equal(BaselineUnit.FlourBag50Kg, recipe.BaselineUnit);
        }

        [Fact]
        public void CreateRecipe_AssignsSequentialIds()
        {
            var flour = SeedIngredient(1);
            var highest = _production.GetAllRecipes(includeInactive: true).Max(r => r.Id);

            var recipe = _production.CreateRecipe(new RecipeBuilderDto
            {
                Name = "Id Probe",
                Code = "TST-ID",
                ProductType = nameof(ProductType.Bread),
                BaselineUnit = nameof(BaselineUnit.FlourBag50Kg),
                ExpectedYield = 100m,
                OutputUnit = "loaves",
                SellingPrice = 100m,
                Ingredients = new List<RecipeIngredientInputDto>
                {
                    new() { IngredientId = flour.Id, QuantityUnit = "kg", QuantityPerBatch = 50m }
                }
            });

            Assert.Equal(highest + 1, recipe.Id);
        }

        // =============================================================
        // Update
        // =============================================================

        [Fact]
        public void UpdateRecipe_ResyncsLineYieldsWhenExpectedYieldChanges()
        {
            var recipe = SeedRecipe(1);
            var originalYield = recipe.ExpectedYield;

            var updated = _production.UpdateRecipe(new RecipeBuilderDto
            {
                Id = recipe.Id,
                Name = recipe.Name,
                Code = recipe.Code,
                Description = recipe.Description,
                ProductType = recipe.ProductType.ToString(),
                BaselineUnit = recipe.BaselineUnit.ToString(),
                ExpectedYield = originalYield * 2,
                OutputUnit = recipe.OutputUnit,
                PackagingCost = recipe.PackagingCost,
                LaborAndOverheadPerUnit = recipe.LaborAndOverheadPerUnit,
                SellingPrice = recipe.SellingPrice,
                Ingredients = recipe.Ingredients.Select(i => new RecipeIngredientInputDto
                {
                    IngredientId = i.IngredientId,
                    QuantityUnit = i.QuantityUnit,
                    QuantityPerBatch = i.QuantityPerBatch
                }).ToList()
            });

            Assert.True(updated);

            var reloaded = _production.GetRecipeById(recipe.Id)!;
            Assert.Equal(originalYield * 2, reloaded.ExpectedYield);
            Assert.All(reloaded.Ingredients, line =>
                Assert.Equal(reloaded.ExpectedYield, line.BaselineYield));
        }

        [Fact]
        public void UpdateRecipe_UnknownId_ReturnsFalse()
        {
            var updated = _production.UpdateRecipe(new RecipeBuilderDto
            {
                Id = 999_999,
                Name = "Ghost",
                Code = "TST-GST",
                ExpectedYield = 10m,
                SellingPrice = 10m
            });

            Assert.False(updated);
        }

        [Fact]
        public void SoftDeleteRecipe_TogglesTheActiveFlag()
        {
            var recipe = SeedRecipe(1);
            Assert.True(recipe.IsActive);

            _production.SoftDeleteRecipe(recipe.Id);
            Assert.False(_production.GetRecipeById(recipe.Id)!.IsActive);
            Assert.DoesNotContain(recipe.Id, _production.GetAllRecipes().Select(r => r.Id));

            _production.SoftDeleteRecipe(recipe.Id);
            Assert.True(_production.GetRecipeById(recipe.Id)!.IsActive);
        }

        // =============================================================
        // Daily production logging
        // =============================================================

        [Fact]
        public void LogDailyProduction_SnapshotsBatchAndUnitCost()
        {
            var recipe = SeedRecipe(1);
            var planned = (int)recipe.ExpectedYield;

            var batch = _production.LogDailyProduction(new DailyProductionEntryDto
            {
                RecipeId = recipe.Id,
                ActualYield = planned,
                Shift = "Morning Shift",
                BakerInCharge = "Test Baker",
                Notes = "Straight test run"
            }, _engine, "Test Manager");

            Assert.Equal(recipe.Id, batch.RecipeId);
            Assert.Equal(recipe.Name, batch.RecipeName);
            Assert.Equal(planned, batch.ActualQuantity);
            Assert.Equal(planned, batch.TargetQuantity);
            Assert.Equal(BatchStatus.Completed, batch.Status);
            Assert.Equal("Test Manager", batch.LoggedBy);
            Assert.StartsWith("BATCH-", batch.BatchNumber);

            // Snapshotted figures must be non-zero and internally consistent:
            // the unit cost must equal the batch cost divided by the actual output.
            Assert.True(batch.TotalBatchCost > 0);
            Assert.True(batch.CalculatedUnitCost > 0);
            Assert.Equal(0, batch.Variance);

            var expectedUnitCost = _engine.CostAtActualYield(recipe, planned).TotalUnitCost;
            Assert.Equal(expectedUnitCost, batch.CalculatedUnitCost);
        }

        [Fact]
        public void LogDailyProduction_LowerYield_ProducesHigherUnitCost()
        {
            var recipe = SeedRecipe(1);
            var planned = (int)recipe.ExpectedYield;

            var onPlan = _production.LogDailyProduction(new DailyProductionEntryDto
            {
                RecipeId = recipe.Id,
                ActualYield = planned
            }, _engine, "Manager");

            var shortRun = _production.LogDailyProduction(new DailyProductionEntryDto
            {
                RecipeId = recipe.Id,
                ActualYield = planned / 2
            }, _engine, "Manager");

            Assert.Equal(onPlan.TotalBatchCost, shortRun.TotalBatchCost);
            Assert.True(shortRun.CalculatedUnitCost > onPlan.CalculatedUnitCost);
            Assert.True(shortRun.Variance < 0);
        }

        [Fact]
        public void LogDailyProduction_DefaultsBakerToTheLoggedInUser()
        {
            var batch = _production.LogDailyProduction(new DailyProductionEntryDto
            {
                RecipeId = 1,
                ActualYield = 96,
                BakerInCharge = ""                    // left blank
            }, _engine, "Chidinma Okoro");

            Assert.Equal("Chidinma Okoro", batch.BakerInCharge);
            Assert.Equal("Chidinma Okoro", batch.LoggedBy);
        }

        [Fact]
        public void CreateBatch_GeneratesASequentialBatchNumber()
        {
            var before = _production.GetAllBatches().Count;

            var batch = _production.CreateBatch(new ProductionBatch
            {
                RecipeId = 1,
                RecipeName = "Manual Entry",
                TargetQuantity = 96,
                ActualQuantity = 96
            });

            Assert.Equal(before + 1, _production.GetAllBatches().Count);
            Assert.Contains(batch.BatchNumber, batch.BatchNumber);
        }

        [Fact]
        public void UpdateBatchStatus_SetsStatusAndOutput()
        {
            var batch = _production.CreateBatch(new ProductionBatch
            {
                RecipeId = 1,
                RecipeName = "Status Probe",
                TargetQuantity = 96,
                ActualQuantity = 0,
                Status = BatchStatus.Planned
            });

            var ok = _production.UpdateBatchStatus(batch.Id, BatchStatus.Baking, 40);

            Assert.True(ok);
            var reloaded = _production.GetBatchById(batch.Id)!;
            Assert.Equal(BatchStatus.Baking, reloaded.Status);
            Assert.Equal(40, reloaded.ActualQuantity);
        }

        [Fact]
        public void UpdateBatchStatus_UnknownId_ReturnsFalse()
        {
            Assert.False(_production.UpdateBatchStatus(999_999, BatchStatus.Completed, 10));
        }

        [Fact]
        public void CalculateBatchMaterialCost_ScalesLinearlyWithInputQuantity()
        {
            var singleCost = _production.CalculateBatchMaterialCost(1, 1.0m);
            var doubleCost = _production.CalculateBatchMaterialCost(1, 2.0m);

            Assert.True(singleCost > 0);
            Assert.Equal(singleCost * 2m, doubleCost);
        }

        [Fact]
        public void CalculateRunUnitCost_ProtectsAgainstDivisionByZero()
        {
            var zeroYieldCost = _production.CalculateRunUnitCost(50000m, 0);
            var negativeYieldCost = _production.CalculateRunUnitCost(50000m, -5);

            Assert.Equal(0m, zeroYieldCost);
            Assert.Equal(0m, negativeYieldCost);
        }

        [Fact]
        public void CalculateRunUnitCost_ComputesCorrectPerUnitCost()
        {
            var unitCost = _production.CalculateRunUnitCost(50000m, 200);
            Assert.Equal(250.00m, unitCost);
        }

        [Fact]
        public void GetCategoryLabels_ReturnsExpectedCategoryLabels()
        {
            var breadLabels = _production.GetCategoryLabels(ProductType.Bread);
            var waterLabels = _production.GetCategoryLabels(ProductType.Water);
            var popcornLabels = _production.GetCategoryLabels(ProductType.Popcorn);

            Assert.Equal("Flour Bags (50kg)", breadLabels.InputLabel);
            Assert.Equal("loaves", breadLabels.DefaultOutputUnit);

            Assert.Equal("Production Run Batch", waterLabels.InputLabel);
            Assert.Equal("Filled Dispenser Bottles", waterLabels.OutputLabel);

            Assert.Equal("Cooking Pots (~2kg)", popcornLabels.InputLabel);
            Assert.Equal("Racks / Bags", popcornLabels.OutputLabel);
        }

        [Fact]
        public void CalculateBatchProductionRun_ComputesFullDtoWithVariances()
        {
            var run = _production.CalculateBatchProductionRun(1, 1m, 100, 96);

            Assert.Equal(ProductType.Bread, run.ProductCategory);
            Assert.True(run.TotalBatchMaterialCost > 0);
            Assert.True(run.ActualUnitProductionCost > 0);
            Assert.True(run.ExpectedUnitProductionCost > 0);
            // Producing 100 loaves vs 96 expected means actual unit cost is lower than expected
            Assert.True(run.CostVariancePerUnit < 0);
        }

        [Fact]
        public void CreateBatch_PopulatesDynamicFieldsAndNonZeroCosts()
        {
            var batch = _production.CreateBatch(new ProductionBatchDto
            {
                RecipeId = 6, // Purified Water
                ProductCategory = ProductType.Water,
                BatchInputQuantity = 1.0m,
                BatchInputUnitLabel = "Production Runs",
                OutputUnitLabel = "bottles",
                TargetQuantity = 250,
                TargetOrderQuantity = 250,
                ActualQuantity = 244,
                BakerInCharge = "Fatima Bello"
            }, _engine);

            Assert.Equal(ProductType.Water, batch.ProductCategory);
            Assert.Equal("Production Runs", batch.BatchInputUnitLabel);
            Assert.Equal(28750m, batch.TotalBatchCost);
            Assert.True(batch.CalculatedUnitCost > 0);
            Assert.Equal(244 - 250, batch.Variance);
        }

        [Fact]
        public void RecordBatchActualOutput_OnPlannedBatch_UpdatesYieldCalculatesUnitCostAndCompletes()
        {
            // Batch 3 is seeded as "Baking" with ActualQuantity = 0
            var plannedBatch = _production.GetBatchById(3);
            Assert.NotNull(plannedBatch);
            Assert.Equal(BatchStatus.Baking, plannedBatch.Status);
            Assert.Equal(0, plannedBatch.ActualQuantity);
            Assert.Equal(0m, plannedBatch.CalculatedUnitCost);

            var updated = _production.RecordBatchActualOutput(
                3,
                actualQuantity: 122,
                status: BatchStatus.Completed,
                notes: "Bake completed with good crust",
                bakerInCharge: "Emeka Obi");

            Assert.NotNull(updated);
            Assert.Equal(122, updated.ActualQuantity);
            Assert.Equal(BatchStatus.Completed, updated.Status);
            Assert.True(updated.CalculatedUnitCost > 0);
            Assert.Equal(122 - updated.TargetQuantity, updated.Variance);
            Assert.Contains("Bake completed with good crust", updated.Notes);
        }

        [Fact]
        public void RecordBatchActualOutput_NonExistentBatch_ReturnsNull()
        {
            var result = _production.RecordBatchActualOutput(999_999, 100);
            Assert.Null(result);
        }

        [Fact]
        public void SeedBatches_HasLinkedOrdersForCustomerPreOrders()
        {
            var batches = _production.GetAllBatches();
            
            // Batch 1 is linked to ORD-20260819-003
            var batch1 = batches.FirstOrDefault(b => b.Id == 1);
            Assert.NotNull(batch1);
            Assert.Equal(3, batch1.LinkedOrderId);
            Assert.Equal("ORD-20260819-003", batch1.LinkedOrderNumber);
            Assert.Equal("Goodness Supermarket Ikeja", batch1.CustomerName);

            // Batch 3 is Make-to-Stock / Floor Inventory
            var batch3 = batches.FirstOrDefault(b => b.Id == 3);
            Assert.NotNull(batch3);
            Assert.Null(batch3.LinkedOrderId);
            Assert.Null(batch3.LinkedOrderNumber);
        }

        [Fact]
        public void CreateBatch_WithLinkedOrder_PreservesOrderProperties()
        {
            var batch = _production.CreateBatch(new ProductionBatchDto
            {
                RecipeId = 1,
                ProductCategory = ProductType.Bread,
                BatchInputQuantity = 1.0m,
                TargetQuantity = 80,
                ActualQuantity = 82,
                LinkedOrderId = 3,
                LinkedOrderNumber = "ORD-20260819-003",
                CustomerName = "Goodness Supermarket Ikeja",
                BakerInCharge = "Emeka Obi"
            }, _engine);

            Assert.Equal(3, batch.LinkedOrderId);
            Assert.Equal("ORD-20260819-003", batch.LinkedOrderNumber);
            Assert.Equal("Goodness Supermarket Ikeja", batch.CustomerName);
        }

        [Fact]
        public void LogDailyProduction_WithLinkedOrder_PreservesOrderProperties()
        {
            var batch = _production.LogDailyProduction(new DailyProductionEntryDto
            {
                RecipeId = 2,
                BatchInputQuantity = 1.0m,
                ActualYield = 110,
                LinkedOrderId = 4,
                LinkedOrderNumber = "ORD-20260819-004",
                CustomerName = "Madam Ngozi Wholesalers",
                BakerInCharge = "Emeka Obi"
            }, _engine, "Test Planner");

            Assert.Equal(4, batch.LinkedOrderId);
            Assert.Equal("ORD-20260819-004", batch.LinkedOrderNumber);
            Assert.Equal("Madam Ngozi Wholesalers", batch.CustomerName);
        }
    }
}
