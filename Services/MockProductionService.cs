using Manufacture.Models.Entities;
using Manufacture.Models.DTOs;

namespace Manufacture.Services
{
    public class MockProductionService
    {
        private readonly List<Ingredient> _ingredients = new();
        private readonly List<Recipe> _recipes = new();
        private readonly List<ProductionBatch> _batches = new();

        public MockProductionService()
        {
            SeedInitialData();
        }

        private void SeedInitialData()
        {
            // Seed Ingredients
            _ingredients.AddRange(new[]
            {
                new Ingredient { Id = 1, Name = "Premium Wheat Flour", Unit = "kg", UnitCost = 1100, CurrentStock = 450 },
                new Ingredient { Id = 2, Name = "Refined White Sugar", Unit = "kg", UnitCost = 1450, CurrentStock = 120 },
                new Ingredient { Id = 3, Name = "Instant Dry Yeast", Unit = "kg", UnitCost = 3800, CurrentStock = 35 },
                new Ingredient { Id = 4, Name = "Vegetable Bakery Shortening", Unit = "kg", UnitCost = 2200, CurrentStock = 80 },
                new Ingredient { Id = 5, Name = "Iodized Salt", Unit = "kg", UnitCost = 400, CurrentStock = 50 },
                new Ingredient { Id = 6, Name = "Calcium Propionate (Preservative)", Unit = "kg", UnitCost = 4500, CurrentStock = 15 },
                new Ingredient { Id = 7, Name = "Purified Process Water", Unit = "litres", UnitCost = 10, CurrentStock = 2000 }
            });

            // Seed Recipes (e.g. Jumbo Bread, Medium Loaf, 61 Bread)
            var jumbo = new Recipe
            {
                Id = 1,
                Name = "Jumbo Family Bread",
                Code = "BRD-JMB",
                Description = "900g premium sweet loaf with golden crust",
                PackagingCost = 65, // printed nylon bag + clip
                LaborAndOverheadPerUnit = 85,
                SellingPrice = 1400,
                IsActive = true,
                Ingredients = new List<RecipeIngredient>
                {
                    new() { Id = 1, RecipeId = 1, IngredientId = 1, IngredientName = "Premium Wheat Flour", Unit = "kg", QuantityPerUnit = 0.52m, IngredientUnitCost = 1100 },
                    new() { Id = 2, RecipeId = 1, IngredientId = 2, IngredientName = "Refined White Sugar", Unit = "kg", QuantityPerUnit = 0.08m, IngredientUnitCost = 1450 },
                    new() { Id = 3, RecipeId = 1, IngredientId = 3, IngredientName = "Instant Dry Yeast", Unit = "kg", QuantityPerUnit = 0.012m, IngredientUnitCost = 3800 },
                    new() { Id = 4, RecipeId = 1, IngredientId = 4, IngredientName = "Vegetable Bakery Shortening", Unit = "kg", QuantityPerUnit = 0.035m, IngredientUnitCost = 2200 },
                    new() { Id = 5, RecipeId = 1, IngredientId = 5, IngredientName = "Iodized Salt", Unit = "kg", QuantityPerUnit = 0.008m, IngredientUnitCost = 400 },
                    new() { Id = 6, RecipeId = 1, IngredientId = 7, IngredientName = "Purified Process Water", Unit = "litres", QuantityPerUnit = 0.28m, IngredientUnitCost = 10 }
                }
            };

            var medium = new Recipe
            {
                Id = 2,
                Name = "Medium Loaf Bread",
                Code = "BRD-MED",
                Description = "500g family table bread",
                PackagingCost = 45,
                LaborAndOverheadPerUnit = 60,
                SellingPrice = 900,
                IsActive = true,
                Ingredients = new List<RecipeIngredient>
                {
                    new() { Id = 7, RecipeId = 2, IngredientId = 1, IngredientName = "Premium Wheat Flour", Unit = "kg", QuantityPerUnit = 0.32m, IngredientUnitCost = 1100 },
                    new() { Id = 8, RecipeId = 2, IngredientId = 2, IngredientName = "Refined White Sugar", Unit = "kg", QuantityPerUnit = 0.05m, IngredientUnitCost = 1450 },
                    new() { Id = 9, RecipeId = 2, IngredientId = 3, IngredientName = "Instant Dry Yeast", Unit = "kg", QuantityPerUnit = 0.008m, IngredientUnitCost = 3800 },
                    new() { Id = 10, RecipeId = 2, IngredientId = 4, IngredientName = "Vegetable Bakery Shortening", Unit = "kg", QuantityPerUnit = 0.022m, IngredientUnitCost = 2200 }
                }
            };

            var special61 = new Recipe
            {
                Id = 3,
                Name = "Special 61 Butter Bread",
                Code = "BRD-61",
                Description = "Rich buttery traditional 61 bread loaf",
                PackagingCost = 55,
                LaborAndOverheadPerUnit = 70,
                SellingPrice = 1100,
                IsActive = true,
                Ingredients = new List<RecipeIngredient>
                {
                    new() { Id = 11, RecipeId = 3, IngredientId = 1, IngredientName = "Premium Wheat Flour", Unit = "kg", QuantityPerUnit = 0.40m, IngredientUnitCost = 1100 },
                    new() { Id = 12, RecipeId = 3, IngredientId = 2, IngredientName = "Refined White Sugar", Unit = "kg", QuantityPerUnit = 0.065m, IngredientUnitCost = 1450 },
                    new() { Id = 13, RecipeId = 3, IngredientId = 4, IngredientName = "Vegetable Bakery Shortening", Unit = "kg", QuantityPerUnit = 0.04m, IngredientUnitCost = 2200 }
                }
            };

            _recipes.AddRange(new[] { jumbo, medium, special61 });

            // Seed Batches
            _batches.AddRange(new[]
            {
                new ProductionBatch
                {
                    Id = 1,
                    BatchNumber = "BATCH-2026-0819-01",
                    RecipeId = 1,
                    RecipeName = "Jumbo Family Bread",
                    TargetQuantity = 400,
                    ActualQuantity = 404,
                    Shift = "Morning Shift",
                    BakerInCharge = "Emeka Obi",
                    FlourBagsUsed = 4.2m,
                    Status = BatchStatus.Completed,
                    ProductionDate = DateTime.UtcNow.Date,
                    Notes = "Good rise and color; +4 extra loaves produced from dough yield"
                },
                new ProductionBatch
                {
                    Id = 2,
                    BatchNumber = "BATCH-2026-0819-02",
                    RecipeId = 2,
                    RecipeName = "Medium Loaf Bread",
                    TargetQuantity = 250,
                    ActualQuantity = 248,
                    Shift = "Morning Shift",
                    BakerInCharge = "Emeka Obi",
                    FlourBagsUsed = 1.6m,
                    Status = BatchStatus.Completed,
                    ProductionDate = DateTime.UtcNow.Date,
                    Notes = "-2 loaves due to oven side burn"
                },
                new ProductionBatch
                {
                    Id = 3,
                    BatchNumber = "BATCH-2026-0819-03",
                    RecipeId = 3,
                    RecipeName = "Special 61 Butter Bread",
                    TargetQuantity = 180,
                    ActualQuantity = 0,
                    Shift = "Afternoon Shift",
                    BakerInCharge = "Emeka Obi",
                    FlourBagsUsed = 1.5m,
                    Status = BatchStatus.Baking,
                    ProductionDate = DateTime.UtcNow.Date,
                    Notes = "Currently in second proofing / oven stage"
                }
            });
        }

        // Ingredients methods
        public List<Ingredient> GetAllIngredients() => _ingredients.ToList();
        public Ingredient? GetIngredientById(int id) => _ingredients.FirstOrDefault(i => i.Id == id);

        // Recipes methods
        public List<Recipe> GetAllRecipes(bool includeInactive = false) =>
            includeInactive ? _recipes.ToList() : _recipes.Where(r => r.IsActive).ToList();

        public Recipe? GetRecipeById(int id) => _recipes.FirstOrDefault(r => r.Id == id);

        public Recipe CreateRecipe(RecipeDto dto)
        {
            var recipe = new Recipe
            {
                Id = _recipes.Any() ? _recipes.Max(r => r.Id) + 1 : 1,
                Name = dto.Name,
                Code = dto.Code,
                Description = dto.Description,
                PackagingCost = dto.PackagingCost,
                LaborAndOverheadPerUnit = dto.LaborAndOverheadPerUnit,
                SellingPrice = dto.SellingPrice,
                IsActive = true,
                Ingredients = dto.Ingredients.Select((ing, idx) => new RecipeIngredient
                {
                    Id = idx + 1,
                    IngredientId = ing.IngredientId,
                    IngredientName = ing.IngredientName,
                    Unit = ing.Unit,
                    QuantityPerUnit = ing.QuantityPerUnit,
                    IngredientUnitCost = ing.IngredientUnitCost
                }).ToList()
            };
            _recipes.Add(recipe);
            return recipe;
        }

        public bool UpdateRecipe(RecipeDto dto)
        {
            var recipe = GetRecipeById(dto.Id);
            if (recipe == null) return false;

            recipe.Name = dto.Name;
            recipe.Code = dto.Code;
            recipe.Description = dto.Description;
            recipe.PackagingCost = dto.PackagingCost;
            recipe.LaborAndOverheadPerUnit = dto.LaborAndOverheadPerUnit;
            recipe.SellingPrice = dto.SellingPrice;
            recipe.Ingredients = dto.Ingredients.Select((ing, idx) => new RecipeIngredient
            {
                Id = idx + 1,
                RecipeId = recipe.Id,
                IngredientId = ing.IngredientId,
                IngredientName = ing.IngredientName,
                Unit = ing.Unit,
                QuantityPerUnit = ing.QuantityPerUnit,
                IngredientUnitCost = ing.IngredientUnitCost
            }).ToList();

            return true;
        }

        public bool SoftDeleteRecipe(int id)
        {
            var recipe = GetRecipeById(id);
            if (recipe == null) return false;
            recipe.IsActive = !recipe.IsActive;
            return true;
        }

        // Batches methods
        public List<ProductionBatch> GetAllBatches() => _batches.OrderByDescending(b => b.Id).ToList();

        public ProductionBatch? GetBatchById(int id) => _batches.FirstOrDefault(b => b.Id == id);

        public ProductionBatch CreateBatch(ProductionBatchDto dto)
        {
            var recipe = GetRecipeById(dto.RecipeId);
            var batch = new ProductionBatch
            {
                Id = _batches.Any() ? _batches.Max(b => b.Id) + 1 : 1,
                BatchNumber = $"BATCH-{DateTime.UtcNow:yyyyMMdd}-{_batches.Count + 1:00}",
                RecipeId = dto.RecipeId,
                RecipeName = recipe?.Name ?? dto.RecipeName,
                TargetQuantity = dto.TargetQuantity,
                ActualQuantity = dto.ActualQuantity,
                Shift = dto.Shift,
                BakerInCharge = dto.BakerInCharge,
                FlourBagsUsed = dto.FlourBagsUsed,
                Status = Enum.TryParse<BatchStatus>(dto.Status, true, out var status) ? status : BatchStatus.Completed,
                ProductionDate = dto.ProductionDate,
                Notes = dto.Notes
            };
            _batches.Add(batch);
            return batch;
        }

        public bool UpdateBatchStatus(int id, BatchStatus status, int actualQuantity)
        {
            var batch = GetBatchById(id);
            if (batch == null) return false;
            batch.Status = status;
            if (actualQuantity > 0) batch.ActualQuantity = actualQuantity;
            return true;
        }
    }
}
