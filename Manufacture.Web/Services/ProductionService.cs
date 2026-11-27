using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using AuditLogEntity = Manufacture.Models.Entities.AuditLog;
using IngredientPriceChangeRequestEntity = Manufacture.Models.Entities.IngredientPriceChangeRequest;
using InventoryItemEntity = Manufacture.Models.Entities.InventoryItem;
using OrderEntity = Manufacture.Models.Entities.Order;
using OrderItemEntity = Manufacture.Models.Entities.OrderItem;
using ProductionBatchEntity = Manufacture.Models.Entities.ProductionBatch;
using ProductionBatchItemEntity = Manufacture.Models.Entities.ProductionBatchItem;
using ProductionCategoryEntity = Manufacture.Models.Entities.ProductionCategory;
using ProductionDefectEntity = Manufacture.Models.Entities.ProductionDefect;
using ProductionProductTypeEntity = Manufacture.Models.Entities.ProductionProductType;
using ProductionShiftEntity = Manufacture.Models.Entities.ProductionShift;
using RecipeEntity = Manufacture.Models.Entities.Recipe;
using RecipeIngredientEntity = Manufacture.Models.Entities.RecipeIngredient;

namespace Manufacture.Services
{
    /// <summary>
    /// Database-backed production service. Replaces the in-memory production
    /// service for production pages while keeping the domain-facing shapes used
    /// by the existing costing engine and Razor pages.
    /// </summary>
    public class ProductionService : IProductionService
    {
        private static readonly string[] ExcludedIngredientCategories = { "Fuel", "Utilities" };

        private readonly ManufactureDbContext _db;
        private readonly IUnitConversionService _unitConversion;
        private readonly ProductStockLedger _stockLedger;

        public ProductionService(ManufactureDbContext db, IUnitConversionService unitConversion, ProductStockLedger stockLedger)
        {
            _db = db;
            _unitConversion = unitConversion;
            _stockLedger = stockLedger;
        }

        // =============================================================
        // Production settings
        // =============================================================

        public List<ProductionCategory> GetCategories(bool includeInactive = false)
        {
            var query = _db.ProductionCategories.AsNoTracking().AsQueryable();
            if (!includeInactive) query = query.Where(c => c.IsActive);

            return query.OrderBy(c => c.Name)
                .ToList()
                .Select(MapCategory)
                .ToList();
        }

        public ProductionCategory SaveCategory(ProductionCategory input)
        {
            var name = NormalizeRequired(input.Name, "Category name");
            if (_db.ProductionCategories.Any(c => c.Name == name && c.Id != input.Id))
            {
                throw new InvalidOperationException("A production category with that name already exists.");
            }

            ProductionCategoryEntity entity;
            if (input.Id > 0)
            {
                entity = _db.ProductionCategories.FirstOrDefault(c => c.Id == input.Id)
                    ?? throw new InvalidOperationException("Production category not found.");
            }
            else
            {
                entity = new ProductionCategoryEntity { CreatedAt = DateTime.UtcNow };
                _db.ProductionCategories.Add(entity);
            }

            entity.Name = name;
            entity.Description = input.Description?.Trim() ?? string.Empty;
            entity.IsActive = input.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            _db.SaveChanges();
            return MapCategory(entity);
        }

        public bool ToggleCategoryStatus(int id)
        {
            var entity = _db.ProductionCategories.FirstOrDefault(c => c.Id == id);
            if (entity == null) return false;
            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            _db.SaveChanges();
            return true;
        }

        public List<ProductionProductType> GetProductTypes(int? categoryId = null, bool includeInactive = false)
        {
            var query = _db.ProductionProductTypes
                .AsNoTracking()
                .Include(p => p.ProductionCategory)
                .AsQueryable();

            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(p => p.ProductionCategoryId == categoryId.Value);
            if (!includeInactive)
                query = query.Where(p => p.IsActive && p.ProductionCategory.IsActive);

            return query.OrderBy(p => p.ProductionCategory.Name)
                .ThenBy(p => p.Name)
                .ToList()
                .Select(MapProductType)
                .ToList();
        }

        public ProductionProductType SaveProductType(ProductionProductType input)
        {
            var name = NormalizeRequired(input.Name, "Product type name");
            var baseline = NormalizeRequired(input.Baseline, "Baseline");
            var outputUnit = NormalizeRequired(input.OutputUnitNoun, "Output unit noun");

            var category = _db.ProductionCategories.FirstOrDefault(c => c.Id == input.ProductionCategoryId && c.IsActive)
                ?? throw new InvalidOperationException("Select an active production category.");

            if (_db.ProductionProductTypes.Any(p => p.ProductionCategoryId == input.ProductionCategoryId && p.Name == name && p.Id != input.Id))
            {
                throw new InvalidOperationException("That product type already exists in the selected category.");
            }

            ProductionProductTypeEntity entity;
            if (input.Id > 0)
            {
                entity = _db.ProductionProductTypes.FirstOrDefault(p => p.Id == input.Id)
                    ?? throw new InvalidOperationException("Product type not found.");
            }
            else
            {
                entity = new ProductionProductTypeEntity { CreatedAt = DateTime.UtcNow };
                _db.ProductionProductTypes.Add(entity);
            }

            entity.ProductionCategoryId = category.Id;
            entity.Name = name;
            entity.Baseline = baseline;
            entity.OutputUnitNoun = outputUnit;
            entity.IsActive = input.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            _db.SaveChanges();
            entity.ProductionCategory = category;
            return MapProductType(entity);
        }

        public bool ToggleProductTypeStatus(int id)
        {
            var entity = _db.ProductionProductTypes.FirstOrDefault(p => p.Id == id);
            if (entity == null) return false;
            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            _db.SaveChanges();
            return true;
        }

        public List<ProductionShift> GetShifts(bool includeInactive = false)
        {
            var query = _db.ProductionShifts.AsNoTracking().AsQueryable();
            if (!includeInactive) query = query.Where(s => s.IsActive);

            return query.OrderBy(s => s.StartTime)
                .ThenBy(s => s.Name)
                .ToList()
                .Select(MapShift)
                .ToList();
        }

        public ProductionShift SaveShift(ProductionShift input)
        {
            var name = NormalizeRequired(input.Name, "Shift name");
            if (_db.ProductionShifts.Any(s => s.Name == name && s.Id != input.Id))
            {
                throw new InvalidOperationException("A production shift with that name already exists.");
            }

            ProductionShiftEntity entity;
            if (input.Id > 0)
            {
                entity = _db.ProductionShifts.FirstOrDefault(s => s.Id == input.Id)
                    ?? throw new InvalidOperationException("Production shift not found.");
            }
            else
            {
                entity = new ProductionShiftEntity { CreatedAt = DateTime.UtcNow };
                _db.ProductionShifts.Add(entity);
            }

            entity.Name = name;
            entity.StartTime = input.StartTime;
            entity.EndTime = input.EndTime;
            entity.IsActive = input.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            _db.SaveChanges();
            return MapShift(entity);
        }

        public bool ToggleShiftStatus(int id)
        {
            var entity = _db.ProductionShifts.FirstOrDefault(s => s.Id == id);
            if (entity == null) return false;
            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            _db.SaveChanges();
            return true;
        }

        // =============================================================
        // Raw materials (live inventory projection)
        // =============================================================

        public List<Ingredient> GetAllIngredients()
        {
            return _db.InventoryItems
                .AsNoTracking()
                .Include(i => i.PreferredSupplier)
                .Where(i => i.IsActive && !ExcludedIngredientCategories.Contains(i.Category))
                .OrderBy(i => i.Name)
                .ToList()
                .Select(MapIngredient)
                .ToList();
        }

        public Ingredient? GetIngredientById(int id)
        {
            var item = _db.InventoryItems
                .AsNoTracking()
                .Include(i => i.PreferredSupplier)
                .FirstOrDefault(i => i.Id == id && i.IsActive);

            return item == null ? null : MapIngredient(item);
        }

        // =============================================================
        // Recipes
        // =============================================================

        public List<Recipe> GetAllRecipes(bool includeInactive = false)
        {
            var query = _db.Recipes
                .AsNoTracking()
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.InventoryItem)
                .Include(r => r.ProductionProductType)
                .ThenInclude(p => p!.ProductionCategory)
                .AsQueryable();

            if (!includeInactive) query = query.Where(r => r.IsActive);

            return query.OrderBy(r => r.Name)
                .ToList()
                .Select(MapRecipe)
                .ToList();
        }

        public Recipe? GetRecipeById(int id)
        {
            var entity = _db.Recipes
                .AsNoTracking()
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.InventoryItem)
                .Include(r => r.ProductionProductType)
                .ThenInclude(p => p!.ProductionCategory)
                .FirstOrDefault(r => r.Id == id);

            return entity == null ? null : MapRecipe(entity);
        }

        public Recipe CreateRecipe(RecipeBuilderDto dto)
        {
            var productType = ResolveProductType(dto.ProductionProductTypeId, dto.ProductType);
            if (_db.Recipes.Any(r => r.Code == dto.Code.Trim()))
                throw new InvalidOperationException("A recipe with that product code already exists.");

            var recipe = new RecipeEntity
            {
                Name = dto.Name.Trim(),
                Code = dto.Code.Trim(),
                Description = dto.Description?.Trim() ?? string.Empty,
                ProductionProductTypeId = productType.Id,
                ExpectedYield = dto.ExpectedYield,
                OutputUnit = productType.OutputUnitNoun,
                SellingPrice = dto.SellingPrice,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var line in BuildRecipeLines(dto.Ingredients, recipe.Id, recipe.ExpectedYield))
            {
                recipe.RecipeIngredients.Add(line);
            }

            _db.Recipes.Add(recipe);
            _db.SaveChanges();

            recipe.ProductionProductType = productType;
            recipe.ProductionProductType.ProductionCategory = _db.ProductionCategories.First(c => c.Id == productType.ProductionCategoryId);
            return MapRecipe(recipe);
        }

        public bool UpdateRecipe(RecipeBuilderDto dto)
        {
            var recipe = _db.Recipes
                .Include(r => r.RecipeIngredients)
                .Include(r => r.ProductionProductType)
                .FirstOrDefault(r => r.Id == dto.Id);

            if (recipe == null) return false;

            var productType = ResolveProductType(dto.ProductionProductTypeId, dto.ProductType);

            recipe.Name = dto.Name.Trim();
            recipe.Code = dto.Code.Trim();
            recipe.Description = dto.Description?.Trim() ?? string.Empty;
            recipe.ProductionProductTypeId = productType.Id;
            recipe.ExpectedYield = dto.ExpectedYield;
            recipe.OutputUnit = productType.OutputUnitNoun;
            recipe.SellingPrice = dto.SellingPrice;
            recipe.UpdatedAt = DateTime.UtcNow;

            _db.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
            recipe.RecipeIngredients.Clear();
            foreach (var line in BuildRecipeLines(dto.Ingredients, recipe.Id, recipe.ExpectedYield))
            {
                recipe.RecipeIngredients.Add(line);
            }

            _db.SaveChanges();
            return true;
        }

        public bool SoftDeleteRecipe(int id)
        {
            var recipe = _db.Recipes.FirstOrDefault(r => r.Id == id);
            if (recipe == null) return false;
            recipe.IsActive = !recipe.IsActive;
            recipe.UpdatedAt = DateTime.UtcNow;
            _db.SaveChanges();
            return true;
        }

        public string GenerateRecipeCode(int productionProductTypeId, int? excludeRecipeId = null)
        {
            var productType = _db.ProductionProductTypes
                .AsNoTracking()
                .FirstOrDefault(p => p.Id == productionProductTypeId)
                ?? throw new InvalidOperationException("Select a valid product type.");

            var prefix = RecipeCodeGenerator.PrefixFor(productType.Name);
            var codePrefix = $"{prefix}-";

            var query = _db.Recipes
                .AsNoTracking()
                .Where(r => r.Code.StartsWith(codePrefix));

            if (excludeRecipeId.HasValue)
            {
                query = query.Where(r => r.Id != excludeRecipeId.Value);
            }

            var existingCodes = query.Select(r => r.Code).ToList();
            return RecipeCodeGenerator.NextCode(prefix, existingCodes);
        }

        public string? GetRecipeDeleteBlocker(int id)
        {
            var recipeName = _db.Recipes
                .Where(r => r.Id == id)
                .Select(r => r.Name)
                .FirstOrDefault();

            if (recipeName == null) return "Recipe not found.";

            var links = new List<string>();

            var batches = _db.ProductionBatchItems.Count(b => b.RecipeId == id);
            if (batches > 0) links.Add($"{batches} production batch line{(batches == 1 ? string.Empty : "s")}");

            var stockRows = _db.ProductRepositoryStocks.Count(s => s.RecipeId == id);
            if (stockRows > 0) links.Add($"{stockRows} product repository stock record{(stockRows == 1 ? string.Empty : "s")}");

            var orderItems = _db.OrderItems.Count(i => i.RecipeId == id);
            if (orderItems > 0) links.Add($"{orderItems} order line{(orderItems == 1 ? string.Empty : "s")}");

            var defects = _db.ProductionDefects.Count(d => d.RecipeId == id);
            if (defects > 0) links.Add($"{defects} defect record{(defects == 1 ? string.Empty : "s")}");

            var returns = _db.CustomerReturns.Count(r => r.RecipeId == id);
            if (returns > 0) links.Add($"{returns} customer return{(returns == 1 ? string.Empty : "s")}");

            var accountPrices = _db.SupermarketAccountPrices.Count(p => p.RecipeId == id);
            if (accountPrices > 0) links.Add($"{accountPrices} supermarket price entr{(accountPrices == 1 ? "y" : "ies")}");

            if (links.Count == 0) return null;

            return $"'{recipeName}' cannot be permanently deleted because it is referenced by " +
                   $"{string.Join(", ", links)}. Deactivate it instead to keep production history intact.";
        }

        public bool DeleteRecipe(int id)
        {
            var recipe = _db.Recipes
                .Include(r => r.RecipeIngredients)
                .FirstOrDefault(r => r.Id == id);

            if (recipe == null) return false;

            _db.Recipes.Remove(recipe);
            _db.SaveChanges();
            return true;
        }

        private List<RecipeIngredientEntity> BuildRecipeLines(
            IEnumerable<RecipeIngredientInputDto>? inputs,
            int recipeId,
            decimal yield)
        {
            var result = new List<RecipeIngredientEntity>();
            if (inputs == null) return result;

            foreach (var input in inputs.Where(i => i.IngredientId > 0 && i.QuantityPerBatch > 0))
            {
                var item = _db.InventoryItems.FirstOrDefault(i => i.Id == input.IngredientId && i.IsActive)
                    ?? throw new InvalidOperationException("One or more selected raw materials no longer exist.");

                if (ExcludedIngredientCategories.Contains(item.Category))
                    throw new InvalidOperationException($"{item.Name} is not available for production recipes.");

                var baseUnit = NormalizeBaseUnit(item.PackageUnitOfMeasure);
                var enteredUnit = string.IsNullOrWhiteSpace(input.QuantityUnit) ? baseUnit : input.QuantityUnit.Trim();

                decimal normalized;
                if (string.Equals(baseUnit, enteredUnit, StringComparison.OrdinalIgnoreCase))
                {
                    normalized = input.QuantityPerBatch;
                }
                else if (!_unitConversion.TryConvert(input.QuantityPerBatch, enteredUnit, baseUnit, out normalized, item.Name, item.DensityKgPerLitre) || normalized <= 0m)
                {
                    throw new InvalidOperationException(
                        $"Could not convert {input.QuantityPerBatch} {enteredUnit} of {item.Name} to {baseUnit}. Check the package/density setup.");
                }

                var costPerBaseUnit = item.PackageSize > 0m
                    ? item.UnitCostPrice / item.PackageSize
                    : item.UnitCostPrice;

                result.Add(new RecipeIngredientEntity
                {
                    RecipeId = recipeId,
                    InventoryItemId = item.Id,
                    Unit = baseUnit,
                    QuantityUnit = enteredUnit,
                    QuantityPerBatch = normalized,
                    BaselineYield = yield,
                    IngredientUnitCost = Math.Round(costPerBaseUnit, 4),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            if (result.Count == 0)
                throw new InvalidOperationException("Add at least one raw material with a quantity greater than zero.");

            return result;
        }

        private ProductionProductTypeEntity ResolveProductType(int productTypeId, string? fallbackName)
        {
            if (productTypeId > 0)
            {
                return _db.ProductionProductTypes
                    .Include(p => p.ProductionCategory)
                    .FirstOrDefault(p => p.Id == productTypeId && p.IsActive && p.ProductionCategory.IsActive)
                    ?? throw new InvalidOperationException("Select an active product type.");
            }

            var byName = _db.ProductionProductTypes
                .Include(p => p.ProductionCategory)
                .FirstOrDefault(p => p.Name == fallbackName && p.IsActive && p.ProductionCategory.IsActive);

            return byName ?? throw new InvalidOperationException("Select a product type.");
        }

        // =============================================================
        // Batches
        // =============================================================

        public List<ProductionBatch> GetAllBatches()
        {
            return _db.ProductionBatches
                .AsNoTracking()
                .Include(b => b.Recipe)
                .Include(b => b.ProductionBatchItems)
                .ThenInclude(i => i.Recipe)
                .Include(b => b.ProductionProductType)
                .ThenInclude(p => p!.ProductionCategory)
                .OrderByDescending(b => b.Id)
                .ToList()
                .Select(MapBatch)
                .ToList();
        }

        public ProductionBatch? GetBatchById(int id)
        {
            var entity = _db.ProductionBatches
                .AsNoTracking()
                .Include(b => b.Recipe)
                .Include(b => b.ProductionBatchItems)
                .ThenInclude(i => i.Recipe)
                .Include(b => b.ProductionProductType)
                .ThenInclude(p => p!.ProductionCategory)
                .FirstOrDefault(b => b.Id == id);

            return entity == null ? null : MapBatch(entity);
        }

        public ProductionBatch CreateBatch(ProductionBatchCreateDto dto, string loggedBy)
        {
            var lines = (dto.Lines ?? new List<ProductionBatchLineInputDto>())
                .Where(l => l.RecipeId > 0 && l.TargetQuantity > 0)
                .ToList();

            if (lines.Count == 0)
            {
                throw new InvalidOperationException("Add at least one product recipe and target quantity.");
            }

            if (lines.Select(l => l.RecipeId).Distinct().Count() != lines.Count)
            {
                throw new InvalidOperationException("Each product recipe can only appear once in a batch.");
            }

            var recipeIds = lines.Select(l => l.RecipeId).ToList();
            var recipes = _db.Recipes
                .Include(r => r.ProductionProductType)
                .ThenInclude(p => p!.ProductionCategory)
                .Where(r => recipeIds.Contains(r.Id) && r.IsActive)
                .ToList()
                .ToDictionary(r => r.Id);

            if (recipes.Count != recipeIds.Count)
            {
                throw new InvalidOperationException("One or more selected recipes are missing or inactive.");
            }

            var now = DateTime.UtcNow;
            var batch = new ProductionBatchEntity
            {
                BatchNumber = GenerateBatchNumber(),
                Shift = string.IsNullOrWhiteSpace(dto.Shift) ? "Morning Shift" : dto.Shift.Trim(),
                BakerInCharge = NormalizeRequired(dto.BakerInCharge, "Baker / operator"),
                ProductionDate = dto.ProductionDate == default ? now : dto.ProductionDate,
                LoggedBy = string.IsNullOrWhiteSpace(loggedBy) ? "Production Manager" : loggedBy,
                Notes = dto.Notes?.Trim() ?? string.Empty,
                Status = (int)BatchStatus.Planned,
                TargetQuantity = 0,
                ActualQuantity = 0,
                CreatedAt = now,
                UpdatedAt = now
            };

            decimal totalCost = 0m;
            decimal totalInput = 0m;
            decimal flourBags = 0m;

            foreach (var line in lines)
            {
                var recipe = recipes[line.RecipeId];
                var inputQty = recipe.ExpectedYield > 0m
                    ? Math.Round(line.TargetQuantity / recipe.ExpectedYield, 3)
                    : 1m;
                var lineCost = CalculateBatchMaterialCost(recipe.Id, inputQty);
                var expectedUnitCost = line.TargetQuantity > 0
                    ? Math.Round(lineCost / line.TargetQuantity, 4)
                    : 0m;

                batch.ProductionBatchItems.Add(new ProductionBatchItemEntity
                {
                    RecipeId = recipe.Id,
                    RecipeName = recipe.Name,
                    ProductCode = recipe.Code,
                    TargetQuantity = line.TargetQuantity,
                    ActualQuantity = 0,
                    BatchInputQuantity = inputQty,
                    BatchInputUnitLabel = recipe.ProductionProductType?.Baseline ?? recipe.OutputUnit,
                    OutputUnitLabel = recipe.OutputUnit,
                    TotalBatchCost = lineCost,
                    ExpectedUnitCost = expectedUnitCost,
                    CalculatedUnitCost = 0m,
                    CreatedAt = now,
                    UpdatedAt = now
                });

                totalCost += lineCost;
                totalInput += inputQty;
                if ((recipe.ProductionProductType?.ProductionCategory?.Name ?? string.Empty)
                    .Contains("Bread", StringComparison.OrdinalIgnoreCase))
                {
                    flourBags += inputQty;
                }
            }

            var firstRecipe = recipes[lines[0].RecipeId];
            batch.RecipeId = firstRecipe.Id;
            batch.RecipeName = lines.Count == 1 ? firstRecipe.Name : $"{firstRecipe.Name} + {lines.Count - 1} more";
            batch.ProductionProductTypeId = firstRecipe.ProductionProductTypeId;
            batch.TargetQuantity = lines.Sum(l => l.TargetQuantity);
            batch.BatchInputQuantity = totalInput;
            batch.BatchInputUnitLabel = firstRecipe.ProductionProductType?.Baseline ?? firstRecipe.OutputUnit;
            batch.OutputUnitLabel = firstRecipe.OutputUnit;
            batch.FlourBagsUsed = flourBags;
            batch.TotalBatchCost = Math.Round(totalCost, 2);
            batch.ExpectedUnitCost = batch.TargetQuantity > 0 ? Math.Round(totalCost / batch.TargetQuantity, 4) : 0m;

            _db.ProductionBatches.Add(batch);
            _db.SaveChanges();

            return GetBatchById(batch.Id)!;
        }

        public ProductionBatch LogDailyProduction(DailyProductionEntryDto dto, string loggedBy)
        {
            var recipe = GetRecipeById(dto.RecipeId)
                ?? throw new InvalidOperationException("Recipe not found.");

            var plannedYield = (int)(recipe.ExpectedYield * (dto.BatchInputQuantity > 0m ? dto.BatchInputQuantity : 1m));
            var target = dto.TargetOrderQuantity > 0 ? dto.TargetOrderQuantity : plannedYield;
            var batch = CreateBatch(new ProductionBatchCreateDto
            {
                Shift = dto.Shift,
                BakerInCharge = string.IsNullOrWhiteSpace(dto.BakerInCharge) ? loggedBy : dto.BakerInCharge,
                ProductionDate = DateTime.UtcNow,
                Notes = dto.Notes ?? string.Empty,
                Lines = { new ProductionBatchLineInputDto { RecipeId = recipe.Id, TargetQuantity = Math.Max(1, target) } }
            }, loggedBy);

            var lineId = batch.Lines.First().Id;
            return RecordBatchActuals(batch.Id, new Dictionary<int, int> { [lineId] = dto.ActualYield }, loggedBy)!;
        }

        public bool UpdateBatchStatus(int id, BatchStatus status, int actualQuantity)
        {
            var entity = _db.ProductionBatches.FirstOrDefault(b => b.Id == id);
            if (entity == null) return false;
            entity.Status = (int)status;
            if (actualQuantity > 0) entity.ActualQuantity = actualQuantity;
            entity.UpdatedAt = DateTime.UtcNow;
            _db.SaveChanges();
            return true;
        }

        public ProductionBatch? RecordBatchActuals(
            int id,
            IReadOnlyDictionary<int, int> actualQuantitiesByLineId,
            string recordedBy)
        {
            var entity = _db.ProductionBatches
                .Include(b => b.ProductionBatchItems)
                .ThenInclude(i => i.Recipe)
                .Include(b => b.ProductionProductType)
                .ThenInclude(p => p!.ProductionCategory)
                .FirstOrDefault(b => b.Id == id);

            if (entity == null) return null;

            if (entity.Status is (int)BatchStatus.Completed or (int)BatchStatus.Cancelled)
            {
                throw new InvalidOperationException("Actual output has already been recorded for this batch.");
            }

            var repository = _db.ProductRepositories
                .FirstOrDefault(r => r.IsDefaultProductionOutput && r.IsActive)
                ?? throw new InvalidOperationException("Set a default factory product repository before recording output.");

            var now = DateTime.UtcNow;
            var actor = string.IsNullOrWhiteSpace(recordedBy) ? "Production Manager" : recordedBy.Trim();

            using var transaction = _db.Database.BeginTransaction();

            foreach (var line in entity.ProductionBatchItems)
            {
                if (!actualQuantitiesByLineId.TryGetValue(line.Id, out var actual))
                {
                    actual = line.ActualQuantity;
                }

                if (actual < 0)
                {
                    throw new InvalidOperationException($"Actual output for '{line.RecipeName}' cannot be negative.");
                }

                line.ActualQuantity = actual;
                line.ActualRecordedAt = now;
                line.CalculatedUnitCost = actual > 0
                    ? Math.Round(line.TotalBatchCost / actual, 4)
                    : 0m;
                line.UpdatedAt = now;

                if (actual > 0)
                {
                    _stockLedger.ApplyAsync(
                        repository.Id,
                        line.RecipeId,
                        ProductStockMovementType.ProductionIn,
                        actual,
                        "ProductionBatch",
                        entity.Id,
                        entity.BatchNumber,
                        actor,
                        $"Production output � {line.RecipeName}",
                        default).GetAwaiter().GetResult();
                }
            }

            entity.ActualQuantity = entity.ProductionBatchItems.Sum(i => i.ActualQuantity);
            entity.TotalBatchCost = entity.ProductionBatchItems.Sum(i => i.TotalBatchCost);
            entity.CalculatedUnitCost = entity.ActualQuantity > 0
                ? Math.Round(entity.TotalBatchCost / entity.ActualQuantity, 4)
                : 0m;
            entity.ExpectedUnitCost = entity.TargetQuantity > 0
                ? Math.Round(entity.TotalBatchCost / entity.TargetQuantity, 4)
                : 0m;
            entity.Status = (int)BatchStatus.Completed;
            entity.CompletedAt = now;
            entity.UpdatedAt = now;

            _db.SaveChanges();
            transaction.Commit();

            return GetBatchById(entity.Id);
        }
        public List<ProductionDefectDto> GetBatchDefects(int batchId)
        {
            return _db.ProductionDefects
                .AsNoTracking()
                .Include(defect => defect.Recipe)
                .Where(defect => defect.ProductionBatchId == batchId)
                .OrderByDescending(defect => defect.RecordedAt)
                .ThenByDescending(defect => defect.Id)
                .ToList()
                .Select(MapProductionDefect)
                .ToList();
        }

        public ProductionDefectDto? SaveBatchDefects(
            int batchId,
            int recipeId,
            int quantity,
            string? reason,
            string recordedBy)
        {
            var batch = _db.ProductionBatches
                .Include(b => b.ProductionBatchItems)
                .FirstOrDefault(candidate => candidate.Id == batchId);
            if (batch == null || quantity < 0) return null;

            var targetRecipeId = recipeId > 0
                ? recipeId
                : batch.ProductionBatchItems.Select(i => i.RecipeId).FirstOrDefault();

            if (targetRecipeId <= 0)
            {
                return null;
            }

            if (batch.ProductionBatchItems.Count > 0
                && !batch.ProductionBatchItems.Any(i => i.RecipeId == targetRecipeId))
            {
                throw new InvalidOperationException("Select a product that belongs to this batch.");
            }

            var defects = _db.ProductionDefects
                .Where(defect => defect.ProductionBatchId == batchId && defect.RecipeId == targetRecipeId)
                .OrderBy(defect => defect.Id)
                .ToList();

            if (quantity == 0)
            {
                if (defects.Count > 0)
                {
                    _db.ProductionDefects.RemoveRange(defects);
                    _db.SaveChanges();
                }

                return null;
            }

            var now = DateTime.UtcNow;
            var reasonText = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            if (reasonText.Length > 250) reasonText = reasonText[..250];

            ProductionDefectEntity entity;
            if (defects.Count > 0)
            {
                entity = defects[0];
                if (defects.Count > 1)
                {
                    _db.ProductionDefects.RemoveRange(defects.Skip(1));
                }
            }
            else
            {
                entity = new ProductionDefectEntity
                {
                    ProductionBatchId = batchId,
                    RecipeId = targetRecipeId,
                    CreatedAt = now
                };
                _db.ProductionDefects.Add(entity);
            }

            entity.Quantity = quantity;
            entity.DefectReason = reasonText;
            entity.RecordedAt = now;
            entity.RecordedBy = string.IsNullOrWhiteSpace(recordedBy) ? "Production Manager" : recordedBy.Trim();
            _db.SaveChanges();

            return MapProductionDefect(entity);
        }

        public decimal CalculateBatchMaterialCost(int recipeId, decimal batchInputQuantity)
        {
            var recipe = GetRecipeById(recipeId);
            if (recipe == null) return 0m;
            var qty = batchInputQuantity > 0m ? batchInputQuantity : 1m;
            return Math.Round(recipe.Ingredients.Sum(line => line.QuantityPerBatch * line.IngredientUnitCost) * qty, 2);
        }

        public decimal CalculateRunUnitCost(decimal totalBatchMaterialCost, decimal actualYieldQuantity)
        {
            return actualYieldQuantity > 0m
                ? Math.Round(totalBatchMaterialCost / actualYieldQuantity, 2)
                : 0m;
        }

        public BatchProductionRunDto CalculateBatchProductionRun(
            int recipeId,
            decimal batchInputQuantity,
            int actualYieldQuantity,
            int targetOrderQuantity = 0)
        {
            var recipe = GetRecipeById(recipeId);
            var inputQty = batchInputQuantity > 0m ? batchInputQuantity : 1m;
            var totalMaterialCost = CalculateBatchMaterialCost(recipeId, inputQty);
            var plannedYield = recipe != null ? (int)(recipe.ExpectedYield * inputQty) : 0;
            var targetQty = targetOrderQuantity > 0 ? targetOrderQuantity : plannedYield;
            var actualUnitCost = actualYieldQuantity > 0 ? Math.Round(totalMaterialCost / actualYieldQuantity, 2) : 0m;
            var expectedUnitCost = plannedYield > 0 ? Math.Round(totalMaterialCost / plannedYield, 2) : 0m;

            return new BatchProductionRunDto
            {
                RecipeId = recipeId,
                RecipeName = recipe?.Name ?? string.Empty,
                ProductCategory = recipe?.ProductType ?? ProductType.Bread,
                BatchInputQuantity = inputQty,
                BatchInputUnitLabel = recipe?.Baseline ?? string.Empty,
                OutputUnitLabel = recipe?.OutputUnit ?? "units",
                TargetOrderQuantity = targetOrderQuantity,
                ActualYieldQuantity = actualYieldQuantity,
                TotalBatchMaterialCost = totalMaterialCost,
                ActualUnitProductionCost = actualUnitCost,
                ExpectedUnitProductionCost = expectedUnitCost,
                Status = "COMPLETED",
                Timestamp = DateTime.UtcNow
            };
        }

        public CategoryLabelsDto GetCategoryLabels(ProductType category)
        {
            return category switch
            {
                ProductType.Bread => new CategoryLabelsDto
                {
                    InputLabel = "Bread Baseline (1 Bag of Flour, 50 kg)",
                    OutputLabel = "Loaves",
                    PlaceholderInput = "e.g., 1 bag",
                    DefaultOutputUnit = "loaves",
                    ProductionStandardTip = "Material cost is divided by the expected batch yield."
                },
                ProductType.Water => new CategoryLabelsDto
                {
                    InputLabel = "Water Production Run",
                    OutputLabel = "Bottles",
                    PlaceholderInput = "e.g., 1 run",
                    DefaultOutputUnit = "bottles",
                    ProductionStandardTip = "Material cost is divided by the expected batch yield."
                },
                ProductType.Popcorn => new CategoryLabelsDto
                {
                    InputLabel = "Popcorn Cooking Pot",
                    OutputLabel = "Bags",
                    PlaceholderInput = "e.g., 1 pot",
                    DefaultOutputUnit = "bags",
                    ProductionStandardTip = "Material cost is divided by the expected batch yield."
                },
                _ => new CategoryLabelsDto()
            };
        }

        public bool UpdateIngredientPrice(int inventoryItemId, decimal newBaseUnitCost)
        {
            if (newBaseUnitCost < 0m) return false;
            var item = _db.InventoryItems.FirstOrDefault(i => i.Id == inventoryItemId && i.IsActive);
            if (item == null) return false;

            var packageSize = item.PackageSize > 0m ? item.PackageSize : 1m;
            item.UnitCostPrice = Math.Round(newBaseUnitCost * packageSize, 4);
            item.UpdatedAt = DateTime.UtcNow;

            foreach (var line in _db.RecipeIngredients.Where(ri => ri.InventoryItemId == inventoryItemId))
            {
                line.IngredientUnitCost = newBaseUnitCost;
                line.UpdatedAt = DateTime.UtcNow;
            }

            _db.SaveChanges();
            return true;
        }

        public int ApplyPriceUpdates(IEnumerable<UpdateIngredientPriceDto> updates)
        {
            var applied = 0;
            foreach (var update in updates ?? Enumerable.Empty<UpdateIngredientPriceDto>())
            {
                if (update.IngredientId > 0 && UpdateIngredientPrice(update.IngredientId, update.NewUnitCost))
                    applied++;
            }
            return applied;
        }

        public int SyncAllPriceSnapshots()
        {
            var items = _db.InventoryItems.Where(i => i.IsActive).ToDictionary(i => i.Id);
            var touched = 0;
            foreach (var line in _db.RecipeIngredients)
            {
                if (!items.TryGetValue(line.InventoryItemId, out var item)) continue;
                var packageSize = item.PackageSize > 0m ? item.PackageSize : 1m;
                line.IngredientUnitCost = Math.Round(item.UnitCostPrice / packageSize, 4);
                line.UpdatedAt = DateTime.UtcNow;
                touched++;
            }
            _db.SaveChanges();
            return touched;
        }

        // =============================================================
        // Raw-material price approval workflow
        // =============================================================

        public List<IngredientPriceChangeRequestDto> GetIngredientPriceChangeRequests(
            IngredientPriceChangeStatus? status = null)
        {
            var query = _db.IngredientPriceChangeRequests
                .AsNoTracking()
                .Include(request => request.InventoryItem)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(request => request.Status == (int)status.Value);
            }

            return query
                .OrderByDescending(request => request.RequestedAt)
                .ThenByDescending(request => request.Id)
                .ToList()
                .Select(MapIngredientPriceChangeRequest)
                .ToList();
        }

        public PriceChangeSubmissionResultDto SubmitIngredientPriceChangeRequests(
            IEnumerable<UpdateIngredientPriceDto> updates,
            int requestedByUserId,
            string requestedByName)
        {
            var result = new PriceChangeSubmissionResultDto();
            var pending = (updates ?? Enumerable.Empty<UpdateIngredientPriceDto>())
                .Where(update => update.IngredientId > 0)
                .ToList();

            if (pending.Count == 0)
            {
                result.Errors.Add("No price changes were submitted.");
                return result;
            }

            var now = DateTime.UtcNow;
            var datePart = now.ToString("yyyyMMdd");
            var prefix = $"PRC-{datePart}-";
            var existingNumbers = _db.IngredientPriceChangeRequests
                .AsNoTracking()
                .Where(request => request.RequestNumber.StartsWith(prefix))
                .Select(request => request.RequestNumber)
                .ToList();
            var existingSet = new HashSet<string>(existingNumbers, StringComparer.OrdinalIgnoreCase);
            var nextNumber = existingNumbers
                .Select(number => number[prefix.Length..])
                .Select(suffix => int.TryParse(suffix, out var value) ? value : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            foreach (var update in pending)
            {
                var item = _db.InventoryItems.FirstOrDefault(inventoryItem =>
                    inventoryItem.Id == update.IngredientId && inventoryItem.IsActive);

                if (item == null)
                {
                    result.Errors.Add($"Inventory item #{update.IngredientId} was not found or is inactive.");
                    continue;
                }

                if (update.NewUnitCost < 0m)
                {
                    result.Errors.Add($"The proposed price for {item.Name} cannot be negative.");
                    continue;
                }

                var packageSize = item.PackageSize > 0m ? item.PackageSize : 1m;
                var currentBaseCost = Math.Round(item.UnitCostPrice / packageSize, 4);
                var requestedCost = Math.Round(update.NewUnitCost, 4);

                if (requestedCost == currentBaseCost)
                {
                    result.SkippedCount++;
                    continue;
                }

                if (_db.IngredientPriceChangeRequests.Any(request =>
                        request.InventoryItemId == item.Id &&
                        request.Status == (int)IngredientPriceChangeStatus.Pending))
                {
                    result.SkippedCount++;
                    result.Messages.Add($"{item.Name} already has a price change awaiting approval.");
                    continue;
                }

                string requestNumber;
                do
                {
                    requestNumber = $"{prefix}{nextNumber:00}";
                    nextNumber++;
                }
                while (!existingSet.Add(requestNumber));

                var request = new IngredientPriceChangeRequestEntity
                {
                    InventoryItemId = item.Id,
                    InventoryItemName = item.Name,
                    RequestNumber = requestNumber,
                    PreviousUnitCost = currentBaseCost,
                    RequestedUnitCost = requestedCost,
                    Status = (int)IngredientPriceChangeStatus.Pending,
                    RequestedByUserId = requestedByUserId > 0 ? requestedByUserId : null,
                    RequestedByName = string.IsNullOrWhiteSpace(requestedByName) ? "Production Manager" : requestedByName.Trim(),
                    RequestedAt = now,
                    ReviewNotes = string.Empty,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _db.IngredientPriceChangeRequests.Add(request);
                _db.AuditLogs.Add(new AuditLogEntity
                {
                    UserId = requestedByUserId > 0 ? requestedByUserId : null,
                    Action = "Submit",
                    EntityName = "IngredientPriceChangeRequest",
                    EntityId = requestNumber,
                    OldValues = $"Item: {item.Name}; Previous price: {currentBaseCost:0.####} per {NormalizeBaseUnit(item.PackageUnitOfMeasure)}",
                    NewValues = $"Requested price: {requestedCost:0.####} per {NormalizeBaseUnit(item.PackageUnitOfMeasure)}; Status: Pending",
                    OccurredAt = now
                });
                result.SubmittedCount++;
            }

            if (result.SubmittedCount > 0)
            {
                _db.SaveChanges();
                result.Messages.Add(
                    $"Submitted {result.SubmittedCount} price change{(result.SubmittedCount == 1 ? "" : "s")} for SuperAdmin approval.");
            }

            return result;
        }

        public PriceChangeActionResultDto ApproveIngredientPriceChangeRequest(
            int requestId,
            int reviewedByUserId,
            string reviewedByName,
            string? notes = null)
        {
            var request = _db.IngredientPriceChangeRequests
                .Include(candidate => candidate.InventoryItem)
                .FirstOrDefault(candidate => candidate.Id == requestId);

            if (request == null)
            {
                return FailedPriceAction("Price change request was not found.");
            }

            if (request.Status != (int)IngredientPriceChangeStatus.Pending)
            {
                return FailedPriceAction("Only pending price changes can be approved.");
            }

            var item = request.InventoryItem;
            if (item == null || !item.IsActive)
            {
                return FailedPriceAction("The linked inventory item is missing or inactive.");
            }

            var packageSize = item.PackageSize > 0m ? item.PackageSize : 1m;
            var currentBaseCost = Math.Round(item.UnitCostPrice / packageSize, 4);
            if (currentBaseCost != Math.Round(request.PreviousUnitCost, 4))
            {
                return FailedPriceAction(
                    "The inventory price changed after this request was submitted. Reject this request and ask for a fresh submission.");
            }

            var now = DateTime.UtcNow;
            var requestedCost = Math.Round(request.RequestedUnitCost, 4);

            item.UnitCostPrice = Math.Round(requestedCost * packageSize, 4);
            item.UpdatedAt = now;

            foreach (var line in _db.RecipeIngredients.Where(line => line.InventoryItemId == item.Id))
            {
                line.IngredientUnitCost = requestedCost;
                line.UpdatedAt = now;
            }

            request.Status = (int)IngredientPriceChangeStatus.Approved;
            request.ReviewedByUserId = reviewedByUserId > 0 ? reviewedByUserId : null;
            request.ReviewedByName = string.IsNullOrWhiteSpace(reviewedByName) ? "SuperAdmin" : reviewedByName.Trim();
            request.ReviewedAt = now;
            request.ReviewNotes = notes?.Trim() ?? string.Empty;
            request.AppliedAt = now;
            request.UpdatedAt = now;

            _db.AuditLogs.Add(new AuditLogEntity
            {
                UserId = reviewedByUserId > 0 ? reviewedByUserId : null,
                Action = "Approve",
                EntityName = "IngredientPriceChangeRequest",
                EntityId = request.RequestNumber,
                OldValues = $"Item: {request.InventoryItemName}; Previous price: {request.PreviousUnitCost:0.####}; Status: Pending",
                NewValues = $"Approved price: {requestedCost:0.####}; Status: Approved; AppliedAt: {now:O}",
                OccurredAt = now
            });

            _db.SaveChanges();

            return new PriceChangeActionResultDto
            {
                Success = true,
                Message = $"Approved {request.InventoryItemName} at {requestedCost:0.####} per {NormalizeBaseUnit(item.PackageUnitOfMeasure)}.",
                RequestId = request.Id,
                InventoryItemId = request.InventoryItemId
            };
        }

        public PriceChangeActionResultDto RejectIngredientPriceChangeRequest(
            int requestId,
            int reviewedByUserId,
            string reviewedByName,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return FailedPriceAction("A rejection reason is required.");
            }

            var request = _db.IngredientPriceChangeRequests
                .FirstOrDefault(candidate => candidate.Id == requestId);

            if (request == null)
            {
                return FailedPriceAction("Price change request was not found.");
            }

            if (request.Status != (int)IngredientPriceChangeStatus.Pending)
            {
                return FailedPriceAction("Only pending price changes can be rejected.");
            }

            var now = DateTime.UtcNow;
            request.Status = (int)IngredientPriceChangeStatus.Rejected;
            request.ReviewedByUserId = reviewedByUserId > 0 ? reviewedByUserId : null;
            request.ReviewedByName = string.IsNullOrWhiteSpace(reviewedByName) ? "SuperAdmin" : reviewedByName.Trim();
            request.ReviewedAt = now;
            request.ReviewNotes = reason.Trim();
            request.UpdatedAt = now;

            _db.AuditLogs.Add(new AuditLogEntity
            {
                UserId = reviewedByUserId > 0 ? reviewedByUserId : null,
                Action = "Reject",
                EntityName = "IngredientPriceChangeRequest",
                EntityId = request.RequestNumber,
                OldValues = $"Item: {request.InventoryItemName}; Proposed price: {request.RequestedUnitCost:0.####}; Status: Pending",
                NewValues = $"Status: Rejected; Reason: {request.ReviewNotes}",
                OccurredAt = now
            });

            _db.SaveChanges();

            return new PriceChangeActionResultDto
            {
                Success = true,
                Message = $"Rejected the price change for {request.InventoryItemName}.",
                RequestId = request.Id,
                InventoryItemId = request.InventoryItemId
            };
        }

        public ProductionDashboardDto GetProductionDashboard(DateTime date)
        {
            var start = date.Date;
            var end = start.AddDays(1);

            var batches = _db.ProductionBatches
                .AsNoTracking()
                .Include(batch => batch.Recipe)
                .Include(batch => batch.ProductionProductType)
                .ThenInclude(productType => productType!.ProductionCategory)
                .Where(batch => batch.ProductionDate >= start && batch.ProductionDate < end)
                .ToList()
                .Select(MapBatch)
                .ToList();

            var defectQuantity = _db.ProductionDefects
                .AsNoTracking()
                .Where(defect => defect.RecordedAt >= start && defect.RecordedAt < end)
                .Sum(defect => (int?)defect.Quantity) ?? 0;

            return ProductionDashboardMetrics.Build(start, batches, defectQuantity);
        }

        private static PriceChangeActionResultDto FailedPriceAction(string message)
        {
            return new PriceChangeActionResultDto
            {
                Success = false,
                Message = message
            };
        }

        private static IngredientPriceChangeRequestDto MapIngredientPriceChangeRequest(
            IngredientPriceChangeRequestEntity request)
        {
            var item = request.InventoryItem;
            var packageSize = item != null && item.PackageSize > 0m ? item.PackageSize : 1m;
            var currentBaseCost = item != null
                ? Math.Round(item.UnitCostPrice / packageSize, 4)
                : request.PreviousUnitCost;

            return new IngredientPriceChangeRequestDto
            {
                Id = request.Id,
                RequestNumber = request.RequestNumber,
                InventoryItemId = request.InventoryItemId,
                InventoryItemName = request.InventoryItemName,
                ItemCode = item?.ItemCode ?? string.Empty,
                Unit = NormalizeBaseUnit(item?.PackageUnitOfMeasure),
                PreviousUnitCost = request.PreviousUnitCost,
                RequestedUnitCost = request.RequestedUnitCost,
                CurrentUnitCost = currentBaseCost,
                Status = (IngredientPriceChangeStatus)request.Status,
                RequestedByName = request.RequestedByName,
                RequestedAt = request.RequestedAt,
                ReviewedByName = request.ReviewedByName,
                ReviewedAt = request.ReviewedAt,
                ReviewNotes = request.ReviewNotes,
                AppliedAt = request.AppliedAt
            };
        }

        // =============================================================
        // Production demand / linked orders
        // =============================================================

        public int GetDemandForRecipe(int recipeId)
        {
            return _db.OrderItems
                .AsNoTracking()
                .Where(i => i.RecipeId == recipeId
                    && i.Order.Status != (int)OrderStatus.Dispatched
                    && i.Order.Status != (int)OrderStatus.Cancelled)
                .Sum(i => (int?)i.Quantity) ?? 0;
        }

        public List<Order> GetOpenOrdersForRecipe(int? recipeId = null)
        {
            var query = _db.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .Where(o => o.Status != (int)OrderStatus.Dispatched && o.Status != (int)OrderStatus.Cancelled);

            if (recipeId.HasValue && recipeId.Value > 0)
                query = query.Where(o => o.OrderItems.Any(i => i.RecipeId == recipeId.Value));

            return query.OrderByDescending(o => o.Id).ToList().Select(MapOrder).ToList();
        }

        public List<Order> GetPreOrders()
        {
            return _db.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .Where(o => o.OrderType == (int)OrderType.PreOrder
                    && o.Status != (int)OrderStatus.Dispatched
                    && o.Status != (int)OrderStatus.Cancelled)
                .OrderByDescending(o => o.Id)
                .ToList()
                .Select(MapOrder)
                .ToList();
        }

        public Order? GetOrderById(int id)
        {
            var entity = _db.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .FirstOrDefault(o => o.Id == id);
            return entity == null ? null : MapOrder(entity);
        }

        private string GenerateBatchNumber()
        {
            var prefix = $"BATCH-{DateTime.UtcNow:yyyyMMdd}-";
            var existing = _db.ProductionBatches
                .Where(b => b.BatchNumber.StartsWith(prefix))
                .Select(b => b.BatchNumber)
                .ToList();
            return $"{prefix}{existing.Count + 1:00}";
        }

        // =============================================================
        // Mapping helpers
        // =============================================================

        private static ProductionCategory MapCategory(ProductionCategoryEntity entity)
        {
            return new ProductionCategory
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                IsActive = entity.IsActive
            };
        }

        private static ProductionProductType MapProductType(ProductionProductTypeEntity entity)
        {
            return new ProductionProductType
            {
                Id = entity.Id,
                ProductionCategoryId = entity.ProductionCategoryId,
                CategoryName = entity.ProductionCategory?.Name ?? string.Empty,
                Name = entity.Name,
                Baseline = entity.Baseline,
                OutputUnitNoun = entity.OutputUnitNoun,
                IsActive = entity.IsActive
            };
        }

        private static ProductionShift MapShift(ProductionShiftEntity entity)
        {
            return new ProductionShift
            {
                Id = entity.Id,
                Name = entity.Name,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                IsActive = entity.IsActive
            };
        }

        private static Ingredient MapIngredient(InventoryItemEntity item)
        {
            var baseUnit = NormalizeBaseUnit(item.PackageUnitOfMeasure);
            var packageSize = item.PackageSize > 0m ? item.PackageSize : 1m;
            return new Ingredient
            {
                Id = item.Id,
                ItemCode = item.ItemCode,
                Name = item.Name,
                Unit = baseUnit,
                UnitCost = Math.Round(item.UnitCostPrice / packageSize, 4),
                CurrentStock = item.QuantityInStock * packageSize,
                Category = item.Category,
                PackageUnit = item.Unit,
                PackageCostPrice = item.UnitCostPrice,
                PackageStockOnHand = item.QuantityInStock,
                PackageSize = packageSize,
                PackageUnitOfMeasure = baseUnit,
                DensityKgPerLitre = item.DensityKgPerLitre,
                SupplierName = item.PreferredSupplier?.Name,
                UpdatedAt = item.UpdatedAt
            };
        }

        private static Recipe MapRecipe(RecipeEntity entity)
        {
            var productType = entity.ProductionProductType;
            var category = productType?.ProductionCategory;
            var lines = entity.RecipeIngredients
                .Select(ri => new RecipeIngredient
                {
                    Id = ri.Id,
                    RecipeId = ri.RecipeId,
                    IngredientId = ri.InventoryItemId,
                    IngredientName = ri.InventoryItem?.Name ?? string.Empty,
                    Unit = ri.Unit,
                    QuantityUnit = ri.QuantityUnit,
                    QuantityPerBatch = ri.QuantityPerBatch,
                    BaselineYield = ri.BaselineYield,
                    IngredientUnitCost = ri.IngredientUnitCost
                })
                .ToList();

            return new Recipe
            {
                Id = entity.Id,
                Name = entity.Name,
                Code = entity.Code,
                Description = entity.Description,
                ProductionProductTypeId = entity.ProductionProductTypeId,
                ProductTypeName = productType?.Name ?? string.Empty,
                ProductionCategoryId = productType?.ProductionCategoryId,
                CategoryName = category?.Name ?? string.Empty,
                Baseline = productType?.Baseline ?? string.Empty,
                ProductType = MapProductTypeEnum(productType),
                BaselineUnit = MapBaselineUnit(productType),
                ExpectedYield = entity.ExpectedYield,
                OutputUnit = entity.OutputUnit,
                PackagingCost = 0m,
                LaborAndOverheadPerUnit = 0m,
                SellingPrice = entity.SellingPrice,
                IsActive = entity.IsActive,
                Ingredients = lines
            };
        }

        private static ProductionDefectDto MapProductionDefect(ProductionDefectEntity entity)
        {
            return new ProductionDefectDto
            {
                Id = entity.Id,
                BatchId = entity.ProductionBatchId,
                RecipeId = entity.RecipeId,
                ProductName = entity.Recipe?.Name ?? string.Empty,
                Quantity = entity.Quantity,
                DefectReason = entity.DefectReason,
                RecordedAt = entity.RecordedAt,
                RecordedBy = entity.RecordedBy
            };
        }

        private static ProductionBatch MapBatch(ProductionBatchEntity entity)
        {
            var productType = entity.ProductionProductType;
            var category = productType?.ProductionCategory;
            var lines = entity.ProductionBatchItems
                .OrderBy(i => i.Id)
                .Select(i => new ProductionBatchLine
                {
                    Id = i.Id,
                    BatchId = i.ProductionBatchId,
                    RecipeId = i.RecipeId,
                    RecipeName = i.RecipeName,
                    ProductCode = i.ProductCode,
                    TargetQuantity = i.TargetQuantity,
                    ActualQuantity = i.ActualQuantity,
                    BatchInputQuantity = i.BatchInputQuantity,
                    BatchInputUnitLabel = i.BatchInputUnitLabel,
                    OutputUnitLabel = i.OutputUnitLabel,
                    TotalBatchCost = i.TotalBatchCost,
                    ExpectedUnitCost = i.ExpectedUnitCost,
                    CalculatedUnitCost = i.CalculatedUnitCost,
                    ActualRecordedAt = i.ActualRecordedAt
                })
                .ToList();

            if (lines.Count == 0 && entity.Recipe != null)
            {
                lines.Add(new ProductionBatchLine
                {
                    Id = 0,
                    BatchId = entity.Id,
                    RecipeId = entity.Recipe.Id,
                    RecipeName = entity.RecipeName,
                    ProductCode = entity.Recipe.Code,
                    TargetQuantity = entity.TargetQuantity,
                    ActualQuantity = entity.ActualQuantity,
                    BatchInputQuantity = entity.BatchInputQuantity,
                    BatchInputUnitLabel = entity.BatchInputUnitLabel,
                    OutputUnitLabel = entity.OutputUnitLabel,
                    TotalBatchCost = entity.TotalBatchCost,
                    ExpectedUnitCost = entity.ExpectedUnitCost,
                    CalculatedUnitCost = entity.CalculatedUnitCost,
                    ActualRecordedAt = entity.Status == (int)BatchStatus.Completed ? entity.CompletedAt : null
                });
            }

            return new ProductionBatch
            {
                Id = entity.Id,
                BatchNumber = entity.BatchNumber,
                RecipeId = entity.RecipeId,
                RecipeName = entity.RecipeName,
                ProductionProductTypeId = entity.ProductionProductTypeId,
                ProductTypeName = productType?.Name ?? string.Empty,
                ProductionCategoryId = productType?.ProductionCategoryId,
                CategoryName = category?.Name ?? string.Empty,
                TargetQuantity = entity.TargetQuantity,
                ActualQuantity = entity.ActualQuantity,
                Lines = lines,
                Shift = entity.Shift,
                BakerInCharge = entity.BakerInCharge,
                FlourBagsUsed = entity.FlourBagsUsed,
                ProductCategory = MapProductTypeEnum(productType),
                BatchInputQuantity = entity.BatchInputQuantity,
                BatchInputUnitLabel = entity.BatchInputUnitLabel,
                OutputUnitLabel = entity.OutputUnitLabel,
                TargetOrderQuantity = entity.TargetOrderQuantity,
                LinkedOrderId = entity.LinkedOrderId,
                LinkedOrderNumber = entity.LinkedOrderNumber,
                CustomerName = entity.CustomerName,
                Status = (BatchStatus)entity.Status,
                ProductionDate = entity.ProductionDate,
                CompletedAt = entity.CompletedAt,
                Notes = entity.Notes,
                TotalBatchCost = entity.TotalBatchCost,
                CalculatedUnitCost = entity.CalculatedUnitCost,
                ExpectedUnitCost = entity.ExpectedUnitCost,
                LoggedBy = entity.LoggedBy
            };
        }

        private static Order MapOrder(OrderEntity entity)
        {
            return new Order
            {
                Id = entity.Id,
                OrderNumber = entity.OrderNumber,
                Type = (OrderType)entity.OrderType,
                CustomerId = entity.CustomerId,
                CustomerName = entity.CustomerName,
                CustomerCategory = (CustomerCategory)entity.CustomerCategory,
                Status = (OrderStatus)entity.Status,
                OrderDate = entity.OrderDate,
                TargetDeliveryDate = entity.TargetDeliveryDate,
                AmountPaid = entity.AmountPaid,
                Items = entity.OrderItems.Select(i => new OrderItem
                {
                    Id = i.Id,
                    OrderId = i.OrderId,
                    RecipeId = i.RecipeId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };
        }

        private static ProductType MapProductTypeEnum(ProductionProductTypeEntity? productType)
        {
            var value = $"{productType?.Name} {productType?.ProductionCategory?.Name}";
            if (value.Contains("water", StringComparison.OrdinalIgnoreCase)) return ProductType.Water;
            if (value.Contains("popcorn", StringComparison.OrdinalIgnoreCase)
                || value.Contains("snack", StringComparison.OrdinalIgnoreCase)) return ProductType.Popcorn;
            return ProductType.Bread;
        }

        private static BaselineUnit MapBaselineUnit(ProductionProductTypeEntity? productType)
        {
            return MapProductTypeEnum(productType) switch
            {
                ProductType.Water => BaselineUnit.WaterRun18_9L,
                ProductType.Popcorn => BaselineUnit.PopcornPot,
                _ => BaselineUnit.FlourBag50Kg
            };
        }

        private static string NormalizeBaseUnit(string? unit)
        {
            if (string.IsNullOrWhiteSpace(unit)) return "kg";
            var value = unit.Trim().ToLowerInvariant();
            if (value is "l" or "litre" or "litres" or "liter" or "liters") return "litres";
            if (value is "kg" or "kgs" or "kilogram" or "kilograms") return "kg";
            if (value is "g" or "gm" or "gram" or "grams") return "g";
            if (value is "ml" or "millilitre" or "millilitres" or "milliliter" or "milliliters") return "ml";
            if (value is "pcs" or "pc" or "piece" or "pieces" or "each") return "pcs";
            return unit.Trim();
        }

        private static string NormalizeRequired(string? value, string fieldName)
        {
            var trimmed = value?.Trim() ?? string.Empty;
            if (trimmed.Length == 0) throw new InvalidOperationException($"{fieldName} is required.");
            return trimmed;
        }
    }
}
