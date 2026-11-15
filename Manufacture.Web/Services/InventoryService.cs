using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using InventoryItemEntity = Manufacture.Models.Entities.InventoryItem;
using MeasurementDensityEntity = Manufacture.Models.Entities.MeasurementDensity;
using RecipeEntity = Manufacture.Models.Entities.Recipe;
using StockMovementEntity = Manufacture.Models.Entities.StockMovement;
using StoreRequestEntity = Manufacture.Models.Entities.StoreRequest;
using StoreRequestItemEntity = Manufacture.Models.Entities.StoreRequestItem;
using SupplierEntity = Manufacture.Models.Entities.Supplier;

namespace Manufacture.Services
{
    public class InventoryService : IInventoryService
    {
        public static readonly string[] SupportedCategories =
        {
            "Raw Materials",
            "Packaging",
            "Additive/Flavor",
            "Fuel",
            "Utilities"
        };

        private readonly ManufactureDbContext _db;
        private readonly IUnitConversionService _unitConversion;

        public InventoryService(ManufactureDbContext db, IUnitConversionService unitConversion)
        {
            _db = db;
            _unitConversion = unitConversion;
        }

        public async Task<List<InventoryItemDto>> GetItemsAsync(
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var query = _db.InventoryItems
                .AsNoTracking()
                .Include(i => i.PreferredSupplier)
                .Include(i => i.MeasurementDensity)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(i => i.IsActive);
            }

            var items = await query
                .OrderBy(i => i.Name)
                .ToListAsync(cancellationToken);

            return items.Select(MapItem).ToList();
        }

        public async Task<InventoryItemDto?> GetItemAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _db.InventoryItems
                .AsNoTracking()
                .Include(i => i.PreferredSupplier)
                .Include(i => i.MeasurementDensity)
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

            return item == null ? null : MapItem(item);
        }

        public async Task<StoreRequestConversionDto> ConvertRequestQuantityAsync(
            int inventoryItemId,
            decimal requestedQuantity,
            string requestedUnit,
            CancellationToken cancellationToken = default)
        {
            var item = await _db.InventoryItems
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == inventoryItemId && i.IsActive, cancellationToken);

            if (item == null)
            {
                return new StoreRequestConversionDto
                {
                    InventoryItemId = inventoryItemId,
                    RequestedQuantity = requestedQuantity,
                    RequestedUnit = requestedUnit,
                    ErrorMessage = "The selected inventory item is no longer available."
                };
            }

            return ConvertRequestQuantity(item, requestedQuantity, requestedUnit);
        }

        public async Task<InventoryItemDto> CreateItemAsync(
            InventoryItemDto dto,
            string? performedBy = null,
            CancellationToken cancellationToken = default)
        {
            var name = NormalizeRequired(dto.Name, "Item name");
            var category = NormalizeCategory(dto.Category);
            var unit = NormalizeRequired(dto.Unit, "Unit of measurement");
            var supplier = await ResolveSupplierAsync(dto.PreferredSupplierId, cancellationToken);
            var measurementDensity = await ResolveMeasurementDensityAsync(dto.MeasurementDensityId, cancellationToken);
            var itemCode = await GenerateItemCodeAsync(category, name, cancellationToken);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var item = new InventoryItemEntity
            {
                ItemCode = itemCode,
                Name = name,
                Category = category,
                Unit = unit,
                QuantityInStock = Math.Max(0m, dto.QuantityInStock),
                ReorderThreshold = Math.Max(0m, dto.ReorderThreshold),
                UnitCostPrice = Math.Max(0m, dto.UnitCostPrice),
                PackageSize = dto.PackageSize > 0m ? dto.PackageSize : 1m,
                PackageUnitOfMeasure = string.IsNullOrWhiteSpace(dto.PackageUnitOfMeasure) ? "kg" : dto.PackageUnitOfMeasure.Trim(),
                DensityKgPerLitre = measurementDensity?.DensityKgPerLitre,
                MeasurementDensityId = measurementDensity?.Id,
                PreferredSupplierId = supplier?.Id,
                LastRestockedDate = today,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            _db.InventoryItems.Add(item);
            await _db.SaveChangesAsync(cancellationToken);

            if (item.QuantityInStock > 0)
            {
                _db.StockMovements.Add(new StockMovementEntity
                {
                    InventoryItemId = item.Id,
                    MovementType = "Receipt",
                    Quantity = item.QuantityInStock,
                    BalanceAfter = item.QuantityInStock,
                    ReferenceNumber = "OPENING",
                    Notes = "Opening stock balance",
                    PerformedBy = NormalizePerformedBy(performedBy),
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            item.PreferredSupplier = supplier;
            item.MeasurementDensity = measurementDensity;
            return MapItem(item);
        }

        public async Task<InventoryItemDto?> UpdateItemAsync(
            int id,
            InventoryItemDto dto,
            CancellationToken cancellationToken = default)
        {
            var item = await _db.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

            if (item == null)
            {
                return null;
            }

            var supplier = await ResolveSupplierAsync(dto.PreferredSupplierId, cancellationToken);
            var measurementDensity = await ResolveMeasurementDensityAsync(dto.MeasurementDensityId, cancellationToken);

            item.Name = NormalizeRequired(dto.Name, "Item name");
            item.Category = NormalizeCategory(dto.Category);
            item.Unit = NormalizeRequired(dto.Unit, "Unit of measurement");
            item.ReorderThreshold = Math.Max(0m, dto.ReorderThreshold);
            item.UnitCostPrice = Math.Max(0m, dto.UnitCostPrice);
            item.PackageSize = dto.PackageSize > 0m ? dto.PackageSize : 1m;
            item.PackageUnitOfMeasure = string.IsNullOrWhiteSpace(dto.PackageUnitOfMeasure) ? "kg" : dto.PackageUnitOfMeasure.Trim();
            item.DensityKgPerLitre = measurementDensity?.DensityKgPerLitre;
            item.MeasurementDensityId = measurementDensity?.Id;
            item.PreferredSupplierId = supplier?.Id;
            item.UpdatedAt = DateTime.UtcNow;

            // Stock quantity is intentionally not editable here. It only moves:
            //   + when stock is received through a restock, and
            //   - when a pending store request is approved and issued.
            await _db.SaveChangesAsync(cancellationToken);

            item.PreferredSupplier = supplier;
            item.MeasurementDensity = measurementDensity;
            return MapItem(item);
        }

        public async Task<bool> RestockItemAsync(
            int id,
            decimal addedQuantity,
            string? performedBy = null,
            CancellationToken cancellationToken = default)
        {
            if (addedQuantity <= 0)
            {
                return false;
            }

            var item = await _db.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

            if (item == null)
            {
                return false;
            }

            item.QuantityInStock += addedQuantity;
            item.LastRestockedDate = DateOnly.FromDateTime(DateTime.UtcNow);
            item.UpdatedAt = DateTime.UtcNow;

            _db.StockMovements.Add(new StockMovementEntity
            {
                InventoryItemId = item.Id,
                MovementType = "Receipt",
                Quantity = addedQuantity,
                BalanceAfter = item.QuantityInStock,
                ReferenceNumber = "RESTOCK",
                Notes = "Stock received into warehouse",
                PerformedBy = NormalizePerformedBy(performedBy),
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public Task<int> GetLowStockCountAsync(CancellationToken cancellationToken = default)
        {
            return _db.InventoryItems.CountAsync(
                i => i.IsActive && i.QuantityInStock <= i.ReorderThreshold,
                cancellationToken);
        }

        public async Task<List<StockMovementDto>> GetStockMovementsAsync(
            int? inventoryItemId = null,
            string? movementType = null,
            int take = 200,
            DateOnly? fromDate = null,
            DateOnly? toDate = null,
            string? search = null,
            CancellationToken cancellationToken = default)
        {
            var query = _db.StockMovements
                .AsNoTracking()
                .Include(m => m.InventoryItem)
                .Include(m => m.StoreRequestItem)
                .Include(m => m.StoreRequest)
                .AsQueryable();

            if (inventoryItemId.HasValue)
            {
                query = query.Where(m => m.InventoryItemId == inventoryItemId.Value);
            }

            if (!string.IsNullOrWhiteSpace(movementType))
            {
                var normalizedType = movementType.Trim();
                query = query.Where(m => m.MovementType == normalizedType);
            }

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.ToDateTime(TimeOnly.MinValue);
                query = query.Where(m => m.CreatedAt >= from);
            }

            if (toDate.HasValue)
            {
                var toExclusive = toDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
                query = query.Where(m => m.CreatedAt < toExclusive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(m =>
                    m.InventoryItem.Name.Contains(term)
                    || m.InventoryItem.ItemCode.Contains(term)
                    || (m.ReferenceNumber != null && m.ReferenceNumber.Contains(term))
                    || (m.Notes != null && m.Notes.Contains(term))
                    || m.PerformedBy.Contains(term));
            }

            var movements = await query
                .OrderByDescending(m => m.CreatedAt)
                .ThenByDescending(m => m.Id)
                .Take(Math.Clamp(take, 1, 500))
                .ToListAsync(cancellationToken);

            return movements.Select(MapStockMovement).ToList();
        }

        public async Task<List<InventoryRecipeUsageDto>> GetRecipeUsageAsync(
            int itemId,
            CancellationToken cancellationToken = default)
        {
            var item = await _db.InventoryItems
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);

            if (item == null)
            {
                return new List<InventoryRecipeUsageDto>();
            }

            var recipes = await _db.Recipes
                .AsNoTracking()
                .Include(r => r.RecipeIngredients)
                .ToListAsync(cancellationToken);

            return recipes
                .Where(recipe => recipe.RecipeIngredients.Any(line => line.InventoryItemId == itemId))
                .Select(recipe =>
                {
                    var line = recipe.RecipeIngredients.First(i => i.InventoryItemId == itemId);
                    return new InventoryRecipeUsageDto
                    {
                        Id = recipe.Id,
                        Name = recipe.Name,
                        Code = recipe.Code,
                        ExpectedYield = recipe.ExpectedYield,
                        OutputUnit = recipe.OutputUnit,
                        QuantityPerBatch = line.QuantityPerBatch,
                        Unit = line.Unit,
                        BaselineYield = line.BaselineYield,
                        BatchLineCost = line.BatchLineCost ?? 0m
                    };
                })
                .OrderBy(recipe => recipe.Name)
                .ToList();
        }

        public async Task<List<SupplierDto>> GetSuppliersAsync(
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Suppliers.AsNoTracking().AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            var suppliers = await query
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);

            return suppliers.Select(MapSupplier).ToList();
        }

        public async Task<SupplierDto> CreateSupplierAsync(
            SupplierDto dto,
            CancellationToken cancellationToken = default)
        {
            var name = NormalizeRequired(dto.Name, "Supplier name");

            if (await _db.Suppliers.AnyAsync(s => s.Name == name, cancellationToken))
            {
                throw new InvalidOperationException("A supplier with that name already exists.");
            }

            var supplier = new SupplierEntity
            {
                Name = name,
                ContactPerson = dto.ContactPerson?.Trim() ?? string.Empty,
                PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty,
                Email = dto.Email?.Trim() ?? string.Empty,
                SupplyCategory = string.IsNullOrWhiteSpace(dto.SupplyCategory)
                    ? "Flour & Grains"
                    : dto.SupplyCategory.Trim(),
                Address = dto.Address?.Trim() ?? string.Empty,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Suppliers.Add(supplier);
            await _db.SaveChangesAsync(cancellationToken);
            return MapSupplier(supplier);
        }

        public async Task<List<StoreRequestDto>> GetRequestsAsync(CancellationToken cancellationToken = default)
        {
            var requests = await _db.StoreRequests
                .AsNoTracking()
                .Include(r => r.StoreRequestItems)
                .OrderByDescending(r => r.RequestedAt)
                .ThenByDescending(r => r.Id)
                .ToListAsync(cancellationToken);

            return requests.Select(MapRequest).ToList();
        }

        public async Task<List<StoreRequestDto>> GetRequestsByItemAsync(
            int itemId,
            CancellationToken cancellationToken = default)
        {
            var requests = await _db.StoreRequests
                .AsNoTracking()
                .Include(r => r.StoreRequestItems)
                .Where(r => r.StoreRequestItems.Any(i => i.InventoryItemId == itemId))
                .OrderByDescending(r => r.RequestedAt)
                .ThenByDescending(r => r.Id)
                .ToListAsync(cancellationToken);

            return requests.Select(MapRequest).ToList();
        }

        public async Task<StoreRequestDto> CreateRequestAsync(
            StoreRequestDto dto,
            CancellationToken cancellationToken = default)
        {
            var requestedBy = NormalizeRequired(dto.RequestedBy, "Requested by");
            var purpose = NormalizeRequired(dto.Purpose, "Purpose");
            var requestedItems = dto.Items
                .Where(i => i.RequestedQuantity > 0)
                .GroupBy(i => new { i.InventoryItemId, Unit = (i.Unit ?? string.Empty).Trim() })
                .Select(group => new
                {
                    InventoryItemId = group.Key.InventoryItemId,
                    Unit = group.Key.Unit,
                    RequestedQuantity = group.Sum(i => i.RequestedQuantity)
                })
                .ToList();

            if (requestedItems.Count == 0)
            {
                throw new InvalidOperationException("Please enter a quantity for at least one item.");
            }

            var itemIds = requestedItems.Select(i => i.InventoryItemId).ToList();
            var inventoryItems = await _db.InventoryItems
                .Where(i => itemIds.Contains(i.Id) && i.IsActive)
                .ToDictionaryAsync(i => i.Id, cancellationToken);

            if (inventoryItems.Count != itemIds.Count)
            {
                throw new InvalidOperationException("One or more selected inventory items are no longer available.");
            }

            var request = new StoreRequestEntity
            {
                RequestNumber = await GenerateRequestNumberAsync(cancellationToken),
                RequestedBy = requestedBy,
                Department = string.IsNullOrWhiteSpace(dto.Department)
                    ? "Bakery Floor"
                    : dto.Department.Trim(),
                Purpose = purpose,
                Status = (int)StoreRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var requested in requestedItems)
            {
                var inventoryItem = inventoryItems[requested.InventoryItemId];
                var conversion = ConvertRequestQuantity(
                    inventoryItem,
                    requested.RequestedQuantity,
                    string.IsNullOrWhiteSpace(requested.Unit) ? NormalizeBaseUnit(inventoryItem.PackageUnitOfMeasure) : requested.Unit);

                if (!string.IsNullOrWhiteSpace(conversion.ErrorMessage))
                {
                    throw new InvalidOperationException(conversion.ErrorMessage);
                }

                request.StoreRequestItems.Add(new StoreRequestItemEntity
                {
                    InventoryItemId = inventoryItem.Id,
                    ItemName = inventoryItem.Name,
                    Unit = conversion.RequestedUnit,
                    RequestedQuantity = conversion.RequestedQuantity,
                    BaseQuantity = conversion.BaseQuantity,
                    BaseUnit = conversion.BaseUnit,
                    StockQuantity = conversion.StockQuantity,
                    StockUnit = conversion.StockUnit,
                    IssuedQuantity = 0m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            _db.StoreRequests.Add(request);
            await _db.SaveChangesAsync(cancellationToken);
            return MapRequest(request);
        }

        public async Task<bool> ApproveAndIssueRequestAsync(
            int id,
            string approverName,
            CancellationToken cancellationToken = default)
        {
            var request = await _db.StoreRequests
                .Include(r => r.StoreRequestItems)
                .ThenInclude(i => i.InventoryItem)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (request == null || request.Status != (int)StoreRequestStatus.Pending)
            {
                return false;
            }

            var approver = string.IsNullOrWhiteSpace(approverName)
                ? "Store Manager"
                : approverName.Trim();

            var shortages = request.StoreRequestItems
                .GroupBy(line => line.InventoryItemId)
                .Select(group => new
                {
                    Line = group.First(),
                    RequestedStock = group.Sum(line => line.StockQuantity),
                    Available = group.First().InventoryItem?.QuantityInStock ?? 0m
                })
                .Where(item => item.Available < item.RequestedStock)
                .Select(item => $"Insufficient stock for {item.Line.ItemName} - available {item.Available:N3} {item.Line.StockUnit}, required {item.RequestedStock:N3} {item.Line.StockUnit}.")
                .ToList();

            if (shortages.Count > 0)
            {
                throw new InvalidOperationException(string.Join(" ", shortages));
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            request.Status = (int)StoreRequestStatus.Issued;
            request.ApprovedBy = approver;
            request.ApprovedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            foreach (var line in request.StoreRequestItems)
            {
                line.IssuedQuantity = line.RequestedQuantity;
                line.UpdatedAt = DateTime.UtcNow;

                var stockQuantity = line.StockQuantity > 0m
                    ? line.StockQuantity
                    : line.RequestedQuantity;

                if (line.InventoryItem == null || stockQuantity <= 0m)
                {
                    continue;
                }

                line.InventoryItem.QuantityInStock -= stockQuantity;
                line.InventoryItem.UpdatedAt = DateTime.UtcNow;

                _db.StockMovements.Add(new StockMovementEntity
                {
                    InventoryItemId = line.InventoryItem.Id,
                    MovementType = "Issue",
                    Quantity = stockQuantity,
                    BalanceAfter = line.InventoryItem.QuantityInStock,
                    StoreRequestId = request.Id,
                    StoreRequestItemId = line.Id,
                    ReferenceNumber = request.RequestNumber,
                    Notes = $"Approved store release: {line.RequestedQuantity:N3} {line.Unit} = {line.BaseQuantity:N3} {line.BaseUnit} = {stockQuantity:N6} {line.StockUnit}",
                    PerformedBy = approver,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        public async Task<bool> RejectRequestAsync(
            int id,
            string approverName,
            string reason,
            CancellationToken cancellationToken = default)
        {
            var request = await _db.StoreRequests
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (request == null || request.Status != (int)StoreRequestStatus.Pending)
            {
                return false;
            }

            request.Status = (int)StoreRequestStatus.Rejected;
            request.ApprovedBy = string.IsNullOrWhiteSpace(approverName)
                ? "Store Manager"
                : approverName.Trim();
            request.ApprovedAt = DateTime.UtcNow;
            request.RejectionReason = string.IsNullOrWhiteSpace(reason)
                ? "Insufficient raw stock / Pending supply"
                : reason.Trim();
            request.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public Task<int> GetPendingRequestCountAsync(CancellationToken cancellationToken = default)
        {
            return _db.StoreRequests.CountAsync(
                r => r.Status == (int)StoreRequestStatus.Pending,
                cancellationToken);
        }

        private async Task<SupplierEntity?> ResolveSupplierAsync(
            int? supplierId,
            CancellationToken cancellationToken)
        {
            if (!supplierId.HasValue)
            {
                return null;
            }

            var supplier = await _db.Suppliers
                .FirstOrDefaultAsync(s => s.Id == supplierId.Value && s.IsActive, cancellationToken);

            if (supplier == null)
            {
                throw new InvalidOperationException("Select a valid active supplier.");
            }

            return supplier;
        }

        private async Task<MeasurementDensityEntity?> ResolveMeasurementDensityAsync(
            int? densityId,
            CancellationToken cancellationToken)
        {
            if (!densityId.HasValue)
            {
                return null;
            }

            var density = await _db.MeasurementDensities
                .FirstOrDefaultAsync(d => d.Id == densityId.Value && d.IsActive, cancellationToken);

            if (density == null)
            {
                throw new InvalidOperationException("Select a valid active density measurement.");
            }

            return density;
        }

        private async Task<string> GenerateItemCodeAsync(
            string category,
            string name,
            CancellationToken cancellationToken)
        {
            var categoryPrefix = CategoryPrefix(category);
            var nameToken = NameToken(name);
            var tokenPrefix = $"{categoryPrefix}-{nameToken}-";

            var existingCodes = await _db.InventoryItems
                .AsNoTracking()
                .Where(i => i.ItemCode.StartsWith(categoryPrefix + "-"))
                .Select(i => i.ItemCode)
                .ToListAsync(cancellationToken);

            var nextNumber = existingCodes
                .Where(code => code.StartsWith(tokenPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(code => code[tokenPrefix.Length..])
                .Select(suffix => int.TryParse(suffix, out var number) ? number : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            var candidate = $"{tokenPrefix}{nextNumber:00}";
            while (existingCodes.Contains(candidate, StringComparer.OrdinalIgnoreCase))
            {
                nextNumber++;
                candidate = $"{tokenPrefix}{nextNumber:00}";
            }

            return candidate;
        }

        private async Task<string> GenerateRequestNumberAsync(CancellationToken cancellationToken)
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var prefix = $"REQ-{datePart}-";
            var existingNumbers = await _db.StoreRequests
                .AsNoTracking()
                .Where(r => r.RequestNumber.StartsWith(prefix))
                .Select(r => r.RequestNumber)
                .ToListAsync(cancellationToken);

            var nextNumber = existingNumbers
                .Select(number => number[prefix.Length..])
                .Select(suffix => int.TryParse(suffix, out var number) ? number : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            var candidate = $"{prefix}{nextNumber:00}";
            while (existingNumbers.Contains(candidate, StringComparer.OrdinalIgnoreCase))
            {
                nextNumber++;
                candidate = $"{prefix}{nextNumber:00}";
            }

            return candidate;
        }

        private static string NormalizeCategory(string? category)
        {
            var value = category?.Trim() ?? string.Empty;

            return value.ToLowerInvariant() switch
            {
                "raw material" or "raw materials" => "Raw Materials",
                "packaging" or "packaging materials" => "Packaging",
                "additive" or "additives" or "additive/flavor" or "additive/flavour"
                    or "additives & flavors" or "additives & flavours" => "Additive/Flavor",
                "fuel" or "fuels" => "Fuel",
                "utilities" or "utility" or "fuel & utility" or "fuel & utilities" => "Utilities",
                _ => "Raw Materials"
            };
        }

        private static string CategoryPrefix(string category)
        {
            return category switch
            {
                "Packaging" => "PKG",
                "Additive/Flavor" => "ADD",
                "Fuel" => "FUE",
                "Utilities" => "UTL",
                _ => "RAW"
            };
        }

        private static string NameToken(string name)
        {
            var lower = name.ToLowerInvariant();
            var mappings = new (string Keyword, string Token)[]
            {
                ("cap sealing", "SEA"),
                ("sealing", "SEA"),
                ("bottle cap", "CAP"),
                ("bottle caps", "CAP"),
                ("caps", "CAP"),
                ("label", "LBL"),
                ("tag", "LBL"),
                ("outer packaging", "OUT"),
                ("outer pack", "OUT"),
                ("dispenser bottle", "BTL"),
                ("bottle", "BTL"),
                ("nylon", "NYL"),
                ("wrapper", "NYL"),
                ("flour", "FLR"),
                ("sugar", "SGR"),
                ("yeast", "YST"),
                ("softener", "SFT"),
                ("improver", "SFT"),
                ("flavour", "FLV"),
                ("flavor", "FLV"),
                ("shortening", "SHT"),
                ("margarine", "SHT"),
                ("preservative", "PRV"),
                ("seasoning", "SEA"),
                ("diesel", "DSL"),
                ("ago", "DSL"),
                ("pms", "PMS"),
                ("petrol", "PMS"),
                ("lpg", "LPG"),
                ("gas", "LPG"),
                ("water", "WTR"),
                ("maize", "CRN"),
                ("corn", "CRN"),
                ("oil", "OIL"),
                ("salt", "SLT"),
                ("bag", "BAG"),
                ("butter", "BTR")
            };

            foreach (var (keyword, token) in mappings)
            {
                if (lower.Contains(keyword, StringComparison.Ordinal))
                {
                    return token;
                }
            }

            var letters = new string(name.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            return letters.Length >= 3 ? letters[..3] : "GEN";
        }

        private static string NormalizeRequired(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"{fieldName} is required.");
            }

            return value.Trim();
        }

        private static string NormalizePerformedBy(string? performedBy)
        {
            return string.IsNullOrWhiteSpace(performedBy)
                ? "System"
                : performedBy.Trim();
        }

        private StoreRequestConversionDto ConvertRequestQuantity(
            InventoryItemEntity item,
            decimal requestedQuantity,
            string requestedUnit)
        {
            var result = new StoreRequestConversionDto
            {
                InventoryItemId = item.Id,
                ItemName = item.Name,
                RequestedQuantity = requestedQuantity,
                RequestedUnit = requestedUnit?.Trim() ?? string.Empty,
                AvailableStock = item.QuantityInStock,
                StockUnit = item.Unit
            };

            if (requestedQuantity <= 0m)
            {
                result.ErrorMessage = "Requested quantity must be greater than zero.";
                return result;
            }

            var baseUnit = NormalizeBaseUnit(item.PackageUnitOfMeasure);
            var unit = string.IsNullOrWhiteSpace(requestedUnit) ? baseUnit : requestedUnit.Trim();

            decimal baseQuantity;
            if (string.Equals(unit, baseUnit, StringComparison.OrdinalIgnoreCase))
            {
                baseQuantity = requestedQuantity;
            }
            else if (!_unitConversion.TryConvert(
                         requestedQuantity,
                         unit,
                         baseUnit,
                         out baseQuantity,
                         item.Name,
                         item.DensityKgPerLitre) || baseQuantity <= 0m)
            {
                result.ErrorMessage =
                    $"Could not convert {requestedQuantity:N2} {unit} of {item.Name} to {baseUnit}. " +
                    "Configure a density for this material or choose a compatible unit.";
                return result;
            }

            var packageSize = item.PackageSize > 0m ? item.PackageSize : 1m;
            var stockQuantity = baseQuantity / packageSize;

            result.BaseQuantity = Math.Round(baseQuantity, 3);
            result.BaseUnit = baseUnit;
            result.StockQuantity = Math.Round(stockQuantity, 6);
            result.IsEnoughStock = stockQuantity <= item.QuantityInStock;
            return result;
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

        private static InventoryItemDto MapItem(InventoryItemEntity item)
        {
            return new InventoryItemDto
            {
                Id = item.Id,
                ItemCode = item.ItemCode,
                Name = item.Name,
                Category = item.Category,
                Unit = item.Unit,
                QuantityInStock = item.QuantityInStock,
                ReorderThreshold = item.ReorderThreshold,
                UnitCostPrice = item.UnitCostPrice,
                PackageSize = item.PackageSize,
                PackageUnitOfMeasure = item.PackageUnitOfMeasure,
                DensityKgPerLitre = item.DensityKgPerLitre,
                MeasurementDensityId = item.MeasurementDensityId,
                MeasurementDensityName = item.MeasurementDensity?.MaterialName,
                PreferredSupplierId = item.PreferredSupplierId,
                PreferredSupplierName = item.PreferredSupplier?.Name,
                LastRestockedDate = item.LastRestockedDate.ToDateTime(TimeOnly.MinValue),
                IsActive = item.IsActive
            };
        }

        private static StockMovementDto MapStockMovement(StockMovementEntity movement)
        {
            return new StockMovementDto
            {
                Id = movement.Id,
                InventoryItemId = movement.InventoryItemId,
                ItemCode = movement.InventoryItem?.ItemCode ?? string.Empty,
                ItemName = movement.InventoryItem?.Name ?? string.Empty,
                Unit = movement.InventoryItem?.Unit ?? string.Empty,
                MovementType = movement.MovementType,
                Quantity = movement.Quantity,
                BalanceAfter = movement.BalanceAfter,
                StoreRequestId = movement.StoreRequestId,
                StoreRequestItemId = movement.StoreRequestItemId,
                RequestNumber = movement.StoreRequest?.RequestNumber,
                RequestedQuantity = movement.StoreRequestItem?.RequestedQuantity,
                RequestedUnit = movement.StoreRequestItem?.Unit,
                BaseQuantity = movement.StoreRequestItem?.BaseQuantity,
                BaseUnit = movement.StoreRequestItem?.BaseUnit,
                StockQuantity = movement.StoreRequestItem?.StockQuantity,
                StockUnit = movement.StoreRequestItem?.StockUnit,
                ReferenceNumber = movement.ReferenceNumber,
                Notes = movement.Notes,
                PerformedBy = movement.PerformedBy,
                CreatedAt = movement.CreatedAt
            };
        }

        private static SupplierDto MapSupplier(SupplierEntity supplier)
        {
            return new SupplierDto
            {
                Id = supplier.Id,
                Name = supplier.Name,
                ContactPerson = supplier.ContactPerson,
                PhoneNumber = supplier.PhoneNumber,
                Email = supplier.Email,
                SupplyCategory = supplier.SupplyCategory,
                Address = supplier.Address,
                IsActive = supplier.IsActive
            };
        }

        private static StoreRequestDto MapRequest(StoreRequestEntity request)
        {
            return new StoreRequestDto
            {
                Id = request.Id,
                RequestNumber = request.RequestNumber,
                RequestedBy = request.RequestedBy,
                Department = request.Department,
                Purpose = request.Purpose,
                Status = (StoreRequestStatus)request.Status,
                RequestedAt = request.RequestedAt,
                ApprovedBy = request.ApprovedBy,
                ApprovedAt = request.ApprovedAt,
                RejectionReason = request.RejectionReason,
                Items = request.StoreRequestItems
                    .OrderBy(i => i.Id)
                    .Select(i => new StoreRequestItemDto
                    {
                        Id = i.Id,
                        StoreRequestId = i.StoreRequestId,
                        InventoryItemId = i.InventoryItemId,
                        ItemName = i.ItemName,
                        Unit = i.Unit,
                        RequestedQuantity = i.RequestedQuantity,
                        BaseQuantity = i.BaseQuantity,
                        BaseUnit = i.BaseUnit,
                        StockQuantity = i.StockQuantity,
                        StockUnit = i.StockUnit,
                        IssuedQuantity = i.IssuedQuantity
                    })
                    .ToList()
            };
        }
    }
}