using Manufacture.Models.Entities;
using Manufacture.Models.DTOs;

namespace Manufacture.Services
{
    /// <summary>
    /// In-memory production data store (no database — mock data, per project
    /// constraint). Seeded with the baseline-batch model described in
    /// doc/recipe-costing.md: bread from a single 50kg flour bag, water from one
    /// 18.9L dispenser run, and popcorn from one standard cooking pot.
    /// </summary>
    public class MockProductionService
    {
        private readonly MockInventoryService _inventoryService;
        private readonly List<Ingredient> _ingredients = new();
        private readonly List<Recipe> _recipes = new();
        private readonly List<ProductionBatch> _batches = new();

        public MockProductionService(MockInventoryService? inventoryService = null)
        {
            _inventoryService = inventoryService ?? new MockInventoryService();
            SeedInitialData();
        }

        // =============================================================
        // Seed data
        // =============================================================

        private void SeedInitialData()
        {
            SeedIngredients();
            SeedRecipes();
            SeedBatches();
        }

        public static (string baseUnit, decimal multiplier) ParsePackageUnit(string unitDescription)
        {
            if (string.IsNullOrWhiteSpace(unitDescription)) return ("kg", 1m);
            var lower = unitDescription.ToLowerInvariant();

            var matchKg = System.Text.RegularExpressions.Regex.Match(lower, @"\(\s*([\d\.]+)\s*kg\s*\)");
            if (matchKg.Success && decimal.TryParse(matchKg.Groups[1].Value, out var kgVal))
                return ("kg", kgVal);

            var matchL = System.Text.RegularExpressions.Regex.Match(lower, @"\(\s*([\d\.]+)\s*l\s*\)");
            if (matchL.Success && decimal.TryParse(matchL.Groups[1].Value, out var lVal))
                return ("litres", lVal);

            var matchPcs = System.Text.RegularExpressions.Regex.Match(lower, @"\(\s*([\d,\.]+)\s*pcs\s*\)");
            if (matchPcs.Success)
            {
                var clean = matchPcs.Groups[1].Value.Replace(",", "");
                if (decimal.TryParse(clean, out var pcsVal)) return ("pcs", pcsVal);
            }

            if (lower.Contains("litre") || lower.Contains("liter")) return ("litres", 1m);
            if (lower.Contains("piece") || lower.Contains("pcs")) return ("pcs", 1m);
            if (lower.Contains("kg")) return ("kg", 1m);

            return ("pcs", 1m);
        }

        private void SeedIngredients()
        {
            _ingredients.Clear();
            var inventoryItems = _inventoryService.GetAllItems()
                .Where(i => i.Category != "Fuel & Utility")
                .ToList();

            foreach (var item in inventoryItems)
            {
                var (baseUnit, multiplier) = ParsePackageUnit(item.Unit);
                var costPerBaseUnit = multiplier > 0 ? Math.Round(item.UnitCostPrice / multiplier, 4) : item.UnitCostPrice;
                var stockInBaseUnit = item.QuantityInStock * multiplier;

                _ingredients.Add(new Ingredient
                {
                    Id = item.Id,
                    ItemCode = item.ItemCode,
                    Name = item.Name,
                    Unit = baseUnit,
                    UnitCost = costPerBaseUnit,
                    CurrentStock = stockInBaseUnit,
                    Category = item.Category,
                    PackageUnit = item.Unit,
                    PackageCostPrice = item.UnitCostPrice,
                    PackageStockOnHand = item.QuantityInStock,
                    SupplierName = item.PreferredSupplierName,
                    UpdatedAt = item.LastRestockedDate
                });
            }
        }

        private void SeedRecipes()
        {
            var flour = _ingredients.First(i => i.ItemCode == "RAW-FLR-01" || i.Name.Contains("Flour"));
            var sugar = _ingredients.First(i => i.ItemCode == "RAW-SGR-01" || i.Name.Contains("Sugar"));
            var yeast = _ingredients.First(i => i.ItemCode == "RAW-YST-01" || i.Name.Contains("Yeast"));
            var shortening = _ingredients.First(i => i.ItemCode == "RAW-FAT-01" || i.Name.Contains("Shortening") || i.Name.Contains("Margarine"));
            var salt = _ingredients.First(i => i.ItemCode == "RAW-SLT-01" || i.Name.Contains("Salt"));
            var preservative = _ingredients.First(i => i.ItemCode == "RAW-PRV-01" || i.Name.Contains("Preservative"));
            var milkFlavour = _ingredients.First(i => i.ItemCode == "RAW-FLV-01" || i.Name.Contains("Flavour"));
            var water = _ingredients.First(i => i.ItemCode == "RAW-WTR-01" || i.Name.Contains("Water"));
            var jumboWrap = _ingredients.First(i => i.ItemCode == "PKG-NYL-JMB" || i.Name.Contains("Jumbo"));
            var mediumWrap = _ingredients.First(i => i.ItemCode == "PKG-NYL-MED" || i.Name.Contains("Medium"));
            var waterBottle = _ingredients.First(i => i.ItemCode == "PKG-BTL-189" || i.Name.Contains("18.9L") || i.Name.Contains("Bottle"));
            var popcornBag = _ingredients.First(i => i.ItemCode == "PKG-BAG-POP" || i.Name.Contains("Popcorn Small Bag"));
            var maize = _ingredients.First(i => i.ItemCode == "RAW-CRN-01" || i.Name.Contains("Maize"));
            var fryingOil = _ingredients.First(i => i.ItemCode == "RAW-OIL-01" || i.Name.Contains("Oil"));
            var seasoning = _ingredients.First(i => i.ItemCode == "RAW-SEA-01" || i.Name.Contains("Seasoning"));

            // ---- Bread: every recipe is defined against ONE 50kg bag of flour ----

            var jumbo = NewRecipe(1, "Jumbo Family Bread", "BRD-JMB",
                "900g premium sweet loaf with golden crust",
                ProductType.Bread, BaselineUnit.FlourBag50Kg, 96m, "loaves",
                packaging: 0m, labor: 85m, sellingPrice: 1400m,
                Lines((flour, 50m), (sugar, 5m), (yeast, 0.6m), (shortening, 2m),
                      (salt, 0.4m), (water, 28m), (preservative, 0.1m),
                      (milkFlavour, 0.3m), (jumboWrap, 96m)));

            var medium = NewRecipe(2, "Medium Loaf Bread", "BRD-MED",
                "500g family table bread",
                ProductType.Bread, BaselineUnit.FlourBag50Kg, 156m, "loaves",
                packaging: 0m, labor: 60m, sellingPrice: 900m,
                Lines((flour, 50m), (sugar, 4m), (yeast, 0.5m), (shortening, 1.5m),
                      (salt, 0.4m), (water, 30m), (preservative, 0.1m),
                      (mediumWrap, 156m)));

            var special61 = NewRecipe(3, "Special 61 Butter Bread", "BRD-61",
                "Rich buttery traditional 61 bread loaf",
                ProductType.Bread, BaselineUnit.FlourBag50Kg, 125m, "loaves",
                packaging: 0m, labor: 70m, sellingPrice: 1100m,
                Lines((flour, 50m), (sugar, 5.5m), (shortening, 3m), (salt, 0.4m),
                      (water, 30m), (milkFlavour, 0.5m), (jumboWrap, 125m)));

            var round = NewRecipe(4, "Round Family Loaf", "BRD-RND",
                "Soft round table loaf for everyday family consumption",
                ProductType.Bread, BaselineUnit.FlourBag50Kg, 166m, "loaves",
                packaging: 0m, labor: 58m, sellingPrice: 850m,
                Lines((flour, 50m), (sugar, 4m), (yeast, 0.55m), (shortening, 1.8m),
                      (salt, 0.4m), (water, 30m), (mediumWrap, 166m)));

            var sixInOne = NewRecipe(5, "6-in-1 Multigrain Loaf", "BRD-6IN1",
                "Six-seed multigrain health loaf",
                ProductType.Bread, BaselineUnit.FlourBag50Kg, 192m, "loaves",
                packaging: 0m, labor: 55m, sellingPrice: 780m,
                Lines((flour, 50m), (sugar, 4.5m), (yeast, 0.6m), (shortening, 1.6m),
                      (salt, 0.45m), (water, 30m), (milkFlavour, 0.4m),
                      (mediumWrap, 192m)));

            // ---- Water: baseline is one full dispenser run ----

            var waterRun = NewRecipe(6, "Purified Water 18.9L", "WTR-189",
                "Purified drinking water filled into 18.9L dispenser bottles",
                ProductType.Water, BaselineUnit.WaterRun18_9L, 250m, "bottles",
                packaging: 0m, labor: 15m, sellingPrice: 250m,
                Lines((water, 500m), (waterBottle, 250m)));

            // ---- Popcorn: baseline is one standard cooking pot ----

            var popcorn = NewRecipe(7, "Popcorn 40g Bag", "POP-40",
                "Salted popcorn packed in 40g bags",
                ProductType.Popcorn, BaselineUnit.PopcornPot, 60m, "bags",
                packaging: 0m, labor: 20m, sellingPrice: 200m,
                Lines((maize, 2m), (fryingOil, 0.3m), (seasoning, 0.15m),
                      (popcornBag, 60m)));

            _recipes.AddRange(new[] { jumbo, medium, special61, round, sixInOne, waterRun, popcorn });
        }

        private void SeedBatches()
        {
            // Seeded with realistic sample production runs across Bread, Water, and Popcorn
            // with accurate material costs and unit costs calculated against live master inventory.
            _batches.AddRange(new[]
            {
                new ProductionBatch
                {
                    Id = 1,
                    BatchNumber = "BATCH-2026-0819-01",
                    RecipeId = 1,
                    RecipeName = "Jumbo Family Bread",
                    ProductCategory = ProductType.Bread,
                    BatchInputQuantity = 1m,
                    BatchInputUnitLabel = "Flour Bags (50kg)",
                    OutputUnitLabel = "loaves",
                    TargetQuantity = 96,
                    TargetOrderQuantity = 80,
                    LinkedOrderId = 3,
                    LinkedOrderNumber = "ORD-20260819-003",
                    CustomerName = "Goodness Supermarket Ikeja",
                    ActualQuantity = 98,
                    Shift = "Morning Shift",
                    BakerInCharge = "Emeka Obi",
                    FlourBagsUsed = 1m,
                    Status = BatchStatus.Completed,
                    ProductionDate = DateTime.UtcNow.Date,
                    LoggedBy = "Chidinma Okoro",
                    Notes = "Good rise and color; +2 extra loaves from dough yield",
                    TotalBatchCost = 73822m,
                    ExpectedUnitCost = 768.98m,
                    CalculatedUnitCost = 753.29m
                },
                new ProductionBatch
                {
                    Id = 2,
                    BatchNumber = "BATCH-2026-0819-02",
                    RecipeId = 2,
                    RecipeName = "Medium Loaf Bread",
                    ProductCategory = ProductType.Bread,
                    BatchInputQuantity = 1m,
                    BatchInputUnitLabel = "Flour Bags (50kg)",
                    OutputUnitLabel = "loaves",
                    TargetQuantity = 156,
                    TargetOrderQuantity = 160,
                    LinkedOrderId = 4,
                    LinkedOrderNumber = "ORD-20260819-004",
                    CustomerName = "Madam Ngozi Wholesalers",
                    ActualQuantity = 152,
                    Shift = "Morning Shift",
                    BakerInCharge = "Emeka Obi",
                    FlourBagsUsed = 1m,
                    Status = BatchStatus.Completed,
                    ProductionDate = DateTime.UtcNow.Date,
                    LoggedBy = "Chidinma Okoro",
                    Notes = "-4 loaves due to oven side burn",
                    TotalBatchCost = 68158m,
                    ExpectedUnitCost = 436.91m,
                    CalculatedUnitCost = 448.41m
                },
                new ProductionBatch
                {
                    Id = 3,
                    BatchNumber = "BATCH-2026-0819-03",
                    RecipeId = 3,
                    RecipeName = "Special 61 Butter Bread",
                    ProductCategory = ProductType.Bread,
                    BatchInputQuantity = 1m,
                    BatchInputUnitLabel = "Flour Bags (50kg)",
                    OutputUnitLabel = "loaves",
                    TargetQuantity = 125,
                    TargetOrderQuantity = 125,
                    LinkedOrderId = null,
                    LinkedOrderNumber = null,
                    CustomerName = null, // Make-to-Stock / Floor Inventory
                    ActualQuantity = 0,
                    Shift = "Afternoon Shift",
                    BakerInCharge = "Emeka Obi",
                    FlourBagsUsed = 1m,
                    Status = BatchStatus.Baking,
                    ProductionDate = DateTime.UtcNow.Date,
                    LoggedBy = "Chidinma Okoro",
                    Notes = "Currently in second proofing / oven stage",
                    TotalBatchCost = 76285m,
                    ExpectedUnitCost = 610.28m,
                    CalculatedUnitCost = 0m
                },
                new ProductionBatch
                {
                    Id = 4,
                    BatchNumber = "BATCH-2026-0819-04",
                    RecipeId = 6,
                    RecipeName = "Purified Water 18.9L",
                    ProductCategory = ProductType.Water,
                    BatchInputQuantity = 1m,
                    BatchInputUnitLabel = "Production Runs",
                    OutputUnitLabel = "bottles",
                    TargetQuantity = 250,
                    TargetOrderQuantity = 250,
                    LinkedOrderId = 5,
                    LinkedOrderNumber = "ORD-20260819-005",
                    CustomerName = "Grand Square Supermarket VI",
                    ActualQuantity = 244,
                    Shift = "Morning Shift",
                    BakerInCharge = "Fatima Bello",
                    FlourBagsUsed = 0m,
                    Status = BatchStatus.Completed,
                    ProductionDate = DateTime.UtcNow.Date,
                    LoggedBy = "Chidinma Okoro",
                    Notes = "6 bottles lost to filling line downtime",
                    TotalBatchCost = 28750m,
                    ExpectedUnitCost = 115.00m,
                    CalculatedUnitCost = 117.83m
                },
                new ProductionBatch
                {
                    Id = 5,
                    BatchNumber = "BATCH-2026-0819-05",
                    RecipeId = 7,
                    RecipeName = "Popcorn 40g Bag",
                    ProductCategory = ProductType.Popcorn,
                    BatchInputQuantity = 2m,
                    BatchInputUnitLabel = "Cooking Pots (~2kg)",
                    OutputUnitLabel = "bags",
                    TargetQuantity = 120,
                    TargetOrderQuantity = 120,
                    LinkedOrderId = 6,
                    LinkedOrderNumber = "ORD-20260819-006",
                    CustomerName = "Goodness Supermarket Ikeja",
                    ActualQuantity = 116,
                    Shift = "Morning Shift",
                    BakerInCharge = "Blessing Etim",
                    FlourBagsUsed = 0m,
                    Status = BatchStatus.Completed,
                    ProductionDate = DateTime.UtcNow.Date,
                    LoggedBy = "Chidinma Okoro",
                    Notes = "Crisp pop, sweet buttery aroma; 4 bags damaged during bag sealing",
                    TotalBatchCost = 10620m,
                    ExpectedUnitCost = 88.50m,
                    CalculatedUnitCost = 91.55m
                }
            });
        }

        // =============================================================
        // Recipe construction helpers
        // =============================================================

        private static Recipe NewRecipe(
            int id, string name, string code, string description,
            ProductType productType, BaselineUnit baselineUnit,
            decimal expectedYield, string outputUnit,
            decimal packaging, decimal labor, decimal sellingPrice,
            List<RecipeIngredient> lines)
        {
            var recipe = new Recipe
            {
                Id = id,
                Name = name,
                Code = code,
                Description = description,
                ProductType = productType,
                BaselineUnit = baselineUnit,
                ExpectedYield = expectedYield,
                OutputUnit = outputUnit,
                // Wrappers and bottles are consumed as recipe materials in the
                // baseline model, so per-unit packaging is a handling cost only.
                PackagingCost = packaging,
                LaborAndOverheadPerUnit = labor,
                SellingPrice = sellingPrice,
                Ingredients = lines,
                IsActive = true
            };
            recipe.SyncIngredientYield();
            return recipe;
        }

        /// <summary>Builds recipe lines from (ingredient, quantity) pairs.</summary>
        private static List<RecipeIngredient> Lines(params (Ingredient ing, decimal qty)[] pairs)
        {
            var result = new List<RecipeIngredient>();
            var id = 1;
            foreach (var (ing, qty) in pairs)
            {
                result.Add(new RecipeIngredient
                {
                    Id = id++,
                    IngredientId = ing.Id,
                    IngredientName = ing.Name,
                    Unit = ing.Unit,
                    QuantityUnit = ing.Unit,
                    QuantityPerBatch = qty,
                    IngredientUnitCost = ing.UnitCost,
                    BaselineYield = 1m
                });
            }
            return result;
        }

        // =============================================================
        // Ingredients
        // =============================================================

        public void SyncIngredientsWithInventory()
        {
            var inventoryItems = _inventoryService.GetAllItems()
                .Where(i => i.Category != "Fuel & Utility")
                .ToList();

            foreach (var item in inventoryItems)
            {
                var (baseUnit, multiplier) = ParsePackageUnit(item.Unit);
                var costPerBaseUnit = multiplier > 0 ? Math.Round(item.UnitCostPrice / multiplier, 4) : item.UnitCostPrice;
                var stockInBaseUnit = item.QuantityInStock * multiplier;

                var existing = _ingredients.FirstOrDefault(i => i.Id == item.Id || i.ItemCode == item.ItemCode);
                if (existing != null)
                {
                    existing.ItemCode = item.ItemCode;
                    existing.Name = item.Name;
                    existing.Category = item.Category;
                    existing.PackageUnit = item.Unit;
                    existing.PackageCostPrice = item.UnitCostPrice;
                    existing.PackageStockOnHand = item.QuantityInStock;
                    existing.SupplierName = item.PreferredSupplierName;
                    existing.Unit = baseUnit;
                    existing.UnitCost = costPerBaseUnit;
                    existing.CurrentStock = stockInBaseUnit;
                    existing.UpdatedAt = item.LastRestockedDate;
                }
                else
                {
                    _ingredients.Add(new Ingredient
                    {
                        Id = item.Id,
                        ItemCode = item.ItemCode,
                        Name = item.Name,
                        Unit = baseUnit,
                        UnitCost = costPerBaseUnit,
                        CurrentStock = stockInBaseUnit,
                        Category = item.Category,
                        PackageUnit = item.Unit,
                        PackageCostPrice = item.UnitCostPrice,
                        PackageStockOnHand = item.QuantityInStock,
                        SupplierName = item.PreferredSupplierName,
                        UpdatedAt = item.LastRestockedDate
                    });
                }
            }
        }

        public List<Ingredient> GetAllIngredients()
        {
            SyncIngredientsWithInventory();
            return _ingredients.OrderBy(i => i.Name).ToList();
        }

        public Ingredient? GetIngredientById(int id)
        {
            SyncIngredientsWithInventory();
            return _ingredients.FirstOrDefault(i => i.Id == id);
        }

        public Ingredient CreateIngredient(Ingredient ingredient)
        {
            ingredient.Id = _ingredients.Any() ? _ingredients.Max(i => i.Id) + 1 : 1;
            ingredient.UpdatedAt = DateTime.UtcNow;
            _ingredients.Add(ingredient);
            return ingredient;
        }

        // =============================================================
        // Recipes
        // =============================================================

        public List<Recipe> GetAllRecipes(bool includeInactive = false)
        {
            var list = includeInactive
                ? _recipes.ToList()
                : _recipes.Where(r => r.IsActive).ToList();

            // Ensure every line's stored price snapshot reflects the live master price.
            foreach (var recipe in list)
            {
                foreach (var line in recipe.Ingredients)
                {
                    var ingredient = _ingredients.FirstOrDefault(i => i.Id == line.IngredientId);
                    if (ingredient != null) line.IngredientUnitCost = ingredient.UnitCost;
                }
            }

            return list;
        }

        public Recipe? GetRecipeById(int id) => GetAllRecipes(includeInactive: true).FirstOrDefault(r => r.Id == id);

        /// <summary>
        /// Creates a recipe from the builder form, normalising each entered
        /// quantity to its ingredient's pricing unit and syncing the yield.
        /// </summary>
        public Recipe CreateRecipe(RecipeBuilderDto dto)
        {
            var baseline = RecipeCatalog.ParseBaseline(dto.BaselineUnit);
            var productType = RecipeCatalog.ParseProductType(dto.ProductType);

            var recipe = new Recipe
            {
                Id = _recipes.Any() ? _recipes.Max(r => r.Id) + 1 : 1,
                Name = dto.Name.Trim(),
                Code = dto.Code.Trim(),
                Description = dto.Description?.Trim() ?? string.Empty,
                ProductType = productType,
                BaselineUnit = baseline,
                ExpectedYield = dto.ExpectedYield,
                OutputUnit = string.IsNullOrWhiteSpace(dto.OutputUnit)
                    ? RecipeCatalog.OutputUnitFor(baseline)
                    : dto.OutputUnit.Trim(),
                PackagingCost = dto.PackagingCost,
                LaborAndOverheadPerUnit = dto.LaborAndOverheadPerUnit,
                SellingPrice = dto.SellingPrice,
                IsActive = true
            };

            recipe.Ingredients = BuildLines(dto.Ingredients, recipe.Id, recipe.ExpectedYield);
            _recipes.Add(recipe);
            return recipe;
        }

        public bool UpdateRecipe(RecipeBuilderDto dto)
        {
            var recipe = GetRecipeById(dto.Id);
            if (recipe == null) return false;

            recipe.Name = dto.Name.Trim();
            recipe.Code = dto.Code.Trim();
            recipe.Description = dto.Description?.Trim() ?? string.Empty;
            recipe.ProductType = RecipeCatalog.ParseProductType(dto.ProductType);
            recipe.BaselineUnit = RecipeCatalog.ParseBaseline(dto.BaselineUnit);
            recipe.ExpectedYield = dto.ExpectedYield;
            recipe.OutputUnit = string.IsNullOrWhiteSpace(dto.OutputUnit)
                ? RecipeCatalog.OutputUnitFor(recipe.BaselineUnit)
                : dto.OutputUnit.Trim();
            recipe.PackagingCost = dto.PackagingCost;
            recipe.LaborAndOverheadPerUnit = dto.LaborAndOverheadPerUnit;
            recipe.SellingPrice = dto.SellingPrice;
            recipe.Ingredients = BuildLines(dto.Ingredients, recipe.Id, recipe.ExpectedYield);
            return true;
        }

        /// <summary>
        /// Maps builder input lines onto recipe lines, dropping empty rows and
        /// converting each entered quantity into the ingredient's pricing unit
        /// (e.g. 500 g yeast priced per kg becomes 0.5).
        /// </summary>
        private List<RecipeIngredient> BuildLines(
            IEnumerable<RecipeIngredientInputDto> inputs, int recipeId, decimal yield)
        {
            var result = new List<RecipeIngredient>();
            if (inputs == null) return result;

            var id = 1;
            foreach (var input in inputs)
            {
                var ingredient = _ingredients.FirstOrDefault(i => i.Id == input.IngredientId);
                if (ingredient == null || input.QuantityPerBatch <= 0) continue;

                var enteredUnit = string.IsNullOrWhiteSpace(input.QuantityUnit)
                    ? ingredient.Unit
                    : input.QuantityUnit;

                // Normalise to the ingredient's pricing unit (g -> kg, ml -> litres).
                var normalisedQty = UnitConverter.ToPricingUnit(
                    input.QuantityPerBatch, enteredUnit, ingredient.Unit);

                // An unconvertible entry (mass priced per volume) falls back to the
                // raw value so the recipe is never silently emptied.
                if (normalisedQty <= 0) normalisedQty = input.QuantityPerBatch;

                result.Add(new RecipeIngredient
                {
                    Id = id++,
                    RecipeId = recipeId,
                    IngredientId = ingredient.Id,
                    IngredientName = ingredient.Name,
                    Unit = ingredient.Unit,
                    QuantityUnit = enteredUnit,
                    QuantityPerBatch = normalisedQty,
                    IngredientUnitCost = input.UnitCostOverride ?? ingredient.UnitCost,
                    BaselineYield = yield
                });
            }
            return result;
        }

        public bool SoftDeleteRecipe(int id)
        {
            var recipe = _recipes.FirstOrDefault(r => r.Id == id);
            if (recipe == null) return false;
            recipe.IsActive = !recipe.IsActive;
            return true;
        }

        // =============================================================
        // Batches
        // =============================================================

        public List<ProductionBatch> GetAllBatches() => _batches.OrderByDescending(b => b.Id).ToList();
        public ProductionBatch? GetBatchById(int id) => _batches.FirstOrDefault(b => b.Id == id);

        public ProductionBatch CreateBatch(ProductionBatch batch)
        {
            batch.Id = _batches.Any() ? _batches.Max(b => b.Id) + 1 : 1;
            batch.BatchNumber = $"BATCH-{DateTime.UtcNow:yyyyMMdd}-{_batches.Count + 1:00}";
            _batches.Add(batch);
            return batch;
        }

        // =============================================================
        // Dynamic Batch Production Cost Engine & Category Labels (plan.txt)
        // =============================================================

        /// <summary>
        /// Calculates the total material cost of a batch by scaling the recipe baseline
        /// ingredient quantities by the batch input quantity and multiplying by current live master inventory prices.
        /// </summary>
        public decimal CalculateBatchMaterialCost(int recipeId, decimal batchInputQuantity)
        {
            if (batchInputQuantity <= 0) batchInputQuantity = 1m;
            var recipe = GetRecipeById(recipeId);
            if (recipe == null) return 0m;

            decimal total = 0m;
            foreach (var line in recipe.Ingredients)
            {
                var ingredient = GetIngredientById(line.IngredientId);
                var unitCost = ingredient?.UnitCost ?? line.IngredientUnitCost;
                total += (line.QuantityPerBatch * batchInputQuantity) * unitCost;
            }
            return Math.Round(total, 2);
        }

        /// <summary>
        /// Calculates the true actual unit production cost: totalBatchMaterialCost / actualYieldQuantity.
        /// Prevents division-by-zero errors when actual yield has not yet been typed in.
        /// </summary>
        public decimal CalculateRunUnitCost(decimal totalBatchMaterialCost, decimal actualYieldQuantity)
        {
            if (actualYieldQuantity <= 0) return 0m;
            return Math.Round(totalBatchMaterialCost / actualYieldQuantity, 2);
        }

        /// <summary>
        /// Returns category-specific adaptive UI labels, units, and benchmarks.
        /// </summary>
        public CategoryLabelsDto GetCategoryLabels(ProductType category)
        {
            return category switch
            {
                ProductType.Bread => new CategoryLabelsDto
                {
                    InputLabel = "Flour Bags (50kg)",
                    OutputLabel = "Loaves / Packs",
                    PlaceholderInput = "e.g., 2 bags",
                    DefaultOutputUnit = "loaves",
                    ProductionStandardTip = "Standard yield benchmark is 95 - 105 loaves per 50kg flour bag for Family Loaves, and 130 - 140 loaves for Standard Size."
                },
                ProductType.Water => new CategoryLabelsDto
                {
                    InputLabel = "Production Run Batch",
                    OutputLabel = "Filled Dispenser Bottles",
                    PlaceholderInput = "e.g., 1 run",
                    DefaultOutputUnit = "bottles",
                    ProductionStandardTip = "Water filtration benchmark is ~250 filled 18.9L bottles per daily line cycle with zero leakage."
                },
                ProductType.Popcorn => new CategoryLabelsDto
                {
                    InputLabel = "Cooking Pots (~2kg)",
                    OutputLabel = "Racks / Bags",
                    PlaceholderInput = "e.g., 2 pots",
                    DefaultOutputUnit = "bags",
                    ProductionStandardTip = "Standard popping expansion benchmark is 55 - 65 sealed snack bags per 2kg corn batch pot."
                },
                _ => new CategoryLabelsDto()
            };
        }

        /// <summary>
        /// Builds a full BatchProductionRunDto calculation for preview or logging.
        /// </summary>
        public BatchProductionRunDto CalculateBatchProductionRun(
            int recipeId, decimal batchInputQuantity, int actualYieldQuantity, int targetOrderQuantity = 0)
        {
            var recipe = GetRecipeById(recipeId);
            var category = recipe?.ProductType ?? ProductType.Bread;
            var labels = GetCategoryLabels(category);
            var inputQty = batchInputQuantity > 0 ? batchInputQuantity : 1m;

            var overhead = (recipe?.PackagingCost ?? 0m) + (recipe?.LaborAndOverheadPerUnit ?? 0m);
            var totalMaterialCost = CalculateBatchMaterialCost(recipeId, inputQty);
            var plannedYield = (int)((recipe?.ExpectedYield ?? 100m) * inputQty);
            var targetQty = targetOrderQuantity > 0 ? targetOrderQuantity : plannedYield;

            var actualUnitCost = actualYieldQuantity > 0 ? Math.Round(CalculateRunUnitCost(totalMaterialCost, actualYieldQuantity) + overhead, 2) : 0m;
            var expectedUnitCost = plannedYield > 0 ? Math.Round((totalMaterialCost / plannedYield) + overhead, 2) : 0m;

            return new BatchProductionRunDto
            {
                ProductCategory = category,
                RecipeId = recipeId,
                RecipeName = recipe?.Name ?? "Unknown Recipe",
                BatchInputQuantity = inputQty,
                BatchInputUnitLabel = labels.InputLabel,
                TargetOrderQuantity = targetQty,
                ActualYieldQuantity = actualYieldQuantity,
                OutputUnitLabel = recipe?.OutputUnit ?? labels.DefaultOutputUnit,
                TotalBatchMaterialCost = totalMaterialCost,
                ActualUnitProductionCost = actualUnitCost,
                ExpectedUnitProductionCost = expectedUnitCost,
                Status = "COMPLETED",
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Maps the manual batch-planning form postback onto an entity and
        /// snapshots the true batch material cost and actual unit cost.
        /// </summary>
        public ProductionBatch CreateBatch(ProductionBatchDto dto, RecipeCostingEngine engine)
        {
            var recipe = GetRecipeById(dto.RecipeId);
            var category = recipe?.ProductType ?? dto.ProductCategory;
            var labels = GetCategoryLabels(category);
            var inputQty = dto.BatchInputQuantity > 0 ? dto.BatchInputQuantity : (dto.FlourBagsUsed > 0 ? dto.FlourBagsUsed : 1m);

            var overhead = (recipe?.PackagingCost ?? 0m) + (recipe?.LaborAndOverheadPerUnit ?? 0m);
            var totalMaterialCost = CalculateBatchMaterialCost(dto.RecipeId, inputQty);
            var plannedYield = (int)((recipe?.ExpectedYield ?? 100m) * inputQty);
            var expectedUnitCost = plannedYield > 0 ? Math.Round((totalMaterialCost / plannedYield) + overhead, 2) : 0m;
            var calculatedUnitCost = dto.ActualQuantity > 0 ? Math.Round(CalculateRunUnitCost(totalMaterialCost, dto.ActualQuantity) + overhead, 2) : 0m;

            var batch = new ProductionBatch
            {
                RecipeId = dto.RecipeId,
                RecipeName = recipe?.Name ?? dto.RecipeName,
                ProductCategory = category,
                BatchInputQuantity = inputQty,
                BatchInputUnitLabel = string.IsNullOrWhiteSpace(dto.BatchInputUnitLabel) ? labels.InputLabel : dto.BatchInputUnitLabel,
                OutputUnitLabel = string.IsNullOrWhiteSpace(dto.OutputUnitLabel) ? (recipe?.OutputUnit ?? labels.DefaultOutputUnit) : dto.OutputUnitLabel,
                TargetQuantity = dto.TargetQuantity > 0 ? dto.TargetQuantity : plannedYield,
                TargetOrderQuantity = dto.TargetOrderQuantity,
                LinkedOrderId = dto.LinkedOrderId,
                LinkedOrderNumber = dto.LinkedOrderNumber,
                CustomerName = dto.CustomerName,
                ActualQuantity = dto.ActualQuantity,
                Shift = dto.Shift,
                BakerInCharge = dto.BakerInCharge,
                FlourBagsUsed = category == ProductType.Bread ? inputQty : 0m,
                Status = Enum.TryParse<BatchStatus>(dto.Status, true, out var parsed)
                    ? parsed
                    : BatchStatus.Completed,
                ProductionDate = dto.ProductionDate,
                LoggedBy = "Production Planner",
                Notes = dto.Notes,
                TotalBatchCost = totalMaterialCost,
                ExpectedUnitCost = expectedUnitCost,
                CalculatedUnitCost = calculatedUnitCost
            };

            return CreateBatch(batch);
        }

        /// <summary>
        /// Logs a daily production run from the entry modal, snapshotting the
        /// batch material cost and actual unit cost at current master prices.
        /// </summary>
        public ProductionBatch LogDailyProduction(
            DailyProductionEntryDto dto, RecipeCostingEngine engine, string loggedBy)
        {
            var recipe = GetRecipeById(dto.RecipeId);
            var category = recipe?.ProductType ?? dto.ProductCategory;
            var labels = GetCategoryLabels(category);
            var inputQty = dto.BatchInputQuantity > 0 ? dto.BatchInputQuantity : (dto.FlourBagsUsed > 0 ? dto.FlourBagsUsed : 1m);

            var overhead = (recipe?.PackagingCost ?? 0m) + (recipe?.LaborAndOverheadPerUnit ?? 0m);
            var totalMaterialCost = CalculateBatchMaterialCost(dto.RecipeId, inputQty);
            var plannedYield = (int)((recipe?.ExpectedYield ?? dto.ActualYield) * inputQty);
            var expectedUnitCost = plannedYield > 0 ? Math.Round((totalMaterialCost / plannedYield) + overhead, 2) : 0m;

            var costing = recipe != null && dto.ActualYield > 0 && engine != null
                ? engine.CostAtActualYield(recipe, dto.ActualYield)
                : null;
            var calculatedUnitCost = costing != null
                ? costing.TotalUnitCost
                : (dto.ActualYield > 0 ? (totalMaterialCost / dto.ActualYield) + overhead : 0m);

            var batch = new ProductionBatch
            {
                RecipeId = dto.RecipeId,
                RecipeName = recipe?.Name ?? "Unknown Recipe",
                ProductCategory = category,
                BatchInputQuantity = inputQty,
                BatchInputUnitLabel = string.IsNullOrWhiteSpace(dto.BatchInputUnitLabel) ? labels.InputLabel : dto.BatchInputUnitLabel,
                OutputUnitLabel = recipe?.OutputUnit ?? labels.DefaultOutputUnit,
                TargetQuantity = dto.TargetOrderQuantity > 0 ? dto.TargetOrderQuantity : plannedYield,
                TargetOrderQuantity = dto.TargetOrderQuantity,
                LinkedOrderId = dto.LinkedOrderId,
                LinkedOrderNumber = dto.LinkedOrderNumber,
                CustomerName = dto.CustomerName,
                ActualQuantity = dto.ActualYield,
                Shift = dto.Shift,
                BakerInCharge = string.IsNullOrWhiteSpace(dto.BakerInCharge) ? loggedBy : dto.BakerInCharge,
                FlourBagsUsed = category == ProductType.Bread ? inputQty : 0m,
                Status = BatchStatus.Completed,
                ProductionDate = DateTime.UtcNow,
                LoggedBy = loggedBy,
                Notes = dto.Notes,
                TotalBatchCost = totalMaterialCost,
                ExpectedUnitCost = expectedUnitCost,
                CalculatedUnitCost = calculatedUnitCost
            };

            return CreateBatch(batch);
        }

        public bool UpdateBatchStatus(int id, BatchStatus status, int actualQuantity)
        {
            var batch = GetBatchById(id);
            if (batch == null) return false;
            batch.Status = status;
            if (actualQuantity > 0) batch.ActualQuantity = actualQuantity;
            return true;
        }

        /// <summary>
        /// Records the actual yield output for a planned or in-progress batch on its view page,
        /// recalculating and snapshotting the true per-unit production cost and updating status.
        /// </summary>
        public ProductionBatch? RecordBatchActualOutput(
            int id, int actualQuantity, BatchStatus status = BatchStatus.Completed, string? notes = null, string? bakerInCharge = null)
        {
            var batch = GetBatchById(id);
            if (batch == null) return null;

            batch.ActualQuantity = actualQuantity;
            batch.Status = status;

            if (!string.IsNullOrWhiteSpace(notes))
            {
                batch.Notes = string.IsNullOrWhiteSpace(batch.Notes)
                    ? notes
                    : $"{batch.Notes} | Output Log: {notes}";
            }

            if (!string.IsNullOrWhiteSpace(bakerInCharge))
            {
                batch.BakerInCharge = bakerInCharge;
            }

            var inputQty = batch.BatchInputQuantity > 0 ? batch.BatchInputQuantity : (batch.FlourBagsUsed > 0 ? batch.FlourBagsUsed : 1m);
            if (batch.TotalBatchCost <= 0)
            {
                batch.TotalBatchCost = CalculateBatchMaterialCost(batch.RecipeId, inputQty);
            }

            var recipe = GetRecipeById(batch.RecipeId);
            var overhead = (recipe?.PackagingCost ?? 0m) + (recipe?.LaborAndOverheadPerUnit ?? 0m);

            if (batch.ExpectedUnitCost <= 0 && recipe != null)
            {
                var plannedYield = (int)(recipe.ExpectedYield * inputQty);
                batch.ExpectedUnitCost = plannedYield > 0 ? Math.Round((batch.TotalBatchCost / plannedYield) + overhead, 2) : 0m;
            }

            if (actualQuantity > 0)
            {
                batch.CalculatedUnitCost = Math.Round(CalculateRunUnitCost(batch.TotalBatchCost, actualQuantity) + overhead, 2);
            }
            else
            {
                batch.CalculatedUnitCost = 0m;
            }

            return batch;
        }

        // =============================================================
        // Dual-Mode Recipe DTO Service Operations
        // =============================================================

        /// <summary>
        /// Retrieves a recipe formatted as a dual-mode RecipeDto with synchronized
        /// Top-Down (Batch-First) and Bottom-Up (Single-Unit) metrics.
        /// </summary>
        public RecipeDto? GetRecipeDto(int id, RecipeEntryMode mode = RecipeEntryMode.BatchFirst, RecipeCostingEngine? engine = null)
        {
            var recipe = GetRecipeById(id);
            if (recipe == null) return null;
            engine ??= new RecipeCostingEngine(this);
            return engine.ConvertEntityToRecipeDto(recipe, mode);
        }

        /// <summary>
        /// Retrieves all recipes as dual-mode RecipeDtos with live ingredient costing.
        /// </summary>
        public List<RecipeDto> GetAllRecipeDtos(RecipeEntryMode mode = RecipeEntryMode.BatchFirst, RecipeCostingEngine? engine = null, bool includeInactive = false)
        {
            engine ??= new RecipeCostingEngine(this);
            return GetAllRecipes(includeInactive)
                .Select(r => engine.ConvertEntityToRecipeDto(r, mode))
                .ToList();
        }

        /// <summary>
        /// Saves or updates a recipe from a dual-mode RecipeDto, normalizing quantities
        /// based on the selected entry mode.
        /// </summary>
        public Recipe SaveRecipeDto(RecipeDto dto, RecipeCostingEngine engine)
        {
            ArgumentNullException.ThrowIfNull(dto);
            engine ??= new RecipeCostingEngine(this);

            // Re-calculate to guarantee integrity
            engine.CalculateDualModeRecipe(dto);

            int.TryParse(dto.Id, out var existingId);
            var recipe = existingId > 0 ? GetRecipeById(existingId) : null;
            var isNew = recipe == null;

            if (isNew)
            {
                recipe = new Recipe
                {
                    Id = _recipes.Any() ? _recipes.Max(r => r.Id) + 1 : 1,
                    Name = dto.ProductName,
                    Code = string.IsNullOrWhiteSpace(dto.Id) ? $"SKU-{DateTime.UtcNow.Ticks % 10000}" : dto.Id,
                    ProductType = RecipeCatalog.ParseProductType(dto.ProductCategory),
                    BaselineUnit = BaselineUnit.FlourBag50Kg,
                    ExpectedYield = dto.ExpectedBatchYield > 0 ? dto.ExpectedBatchYield : 50m,
                    OutputUnit = "units",
                    IsActive = true
                };
            }
            else
            {
                recipe!.Name = dto.ProductName;
                recipe.ExpectedYield = dto.ExpectedBatchYield > 0 ? dto.ExpectedBatchYield : recipe.ExpectedYield;
            }

            recipe.Ingredients.Clear();
            foreach (var ing in dto.Ingredients)
            {
                int.TryParse(ing.IngredientId, out var ingId);
                var masterIng = GetIngredientById(ingId);

                recipe.Ingredients.Add(new RecipeIngredient
                {
                    IngredientId = ingId,
                    IngredientName = masterIng?.Name ?? ing.IngredientName,
                    Unit = ing.UnitOfMeasure,
                    QuantityPerBatch = ing.BatchQuantity,
                    IngredientUnitCost = ing.CurrentUnitCost > 0 ? ing.CurrentUnitCost : (masterIng?.UnitCost ?? 0m)
                });
            }

            if (isNew)
            {
                _recipes.Add(recipe);
            }

            return recipe;
        }
    }
}
