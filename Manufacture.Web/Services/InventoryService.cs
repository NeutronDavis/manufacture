using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using InventoryItemEntity = Manufacture.Models.Entities.InventoryItem;
using RecipeEntity = Manufacture.Models.Entities.Recipe;
using RecipeIngredientEntity = Manufacture.Models.Entities.RecipeIngredient;
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

        public InventoryService(ManufactureDbContext db)
        {
            _db = db;
        }

        public async Task<List<InventoryItemDto>> GetItemsAsync(
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var query = _db.InventoryItems
                .AsNoTracking()
                .Include(i => i.PreferredSupplier)
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
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

            return item == null ? null : MapItem(item);
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

            item.Name = NormalizeRequired(dto.Name, "Item name");
            item.Category = NormalizeCategory(dto.Category);
            item.Unit = NormalizeRequired(dto.Unit, "Unit of measurement");
            item.ReorderThreshold = Math.Max(0m, dto.ReorderThreshold);
            item.UnitCostPrice = Math.Max(0m, dto.UnitCostPrice);
            item.PreferredSupplierId = supplier?.Id;
            item.UpdatedAt = DateTime.UtcNow;

            // Stock quantity is intentionally not editable here. It only moves:
            //   + when stock is received through a restock, and
            //   - when a pending store request is approved and issued.
            await _db.SaveChangesAsync(cancellationToken);

            item.PreferredSupplier = supplier;
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
            CancellationToken cancellationToken = default)
        {
            var query = _db.StockMovements
                .AsNoTracking()
                .Include(m => m.InventoryItem)
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
                .ThenInclude(ri => ri.Ingredient)
                .ToListAsync(cancellationToken);

            var itemName = item.Name.Trim().ToLowerInvariant();
            var itemCode = item.ItemCode.ToUpperInvariant();

            bool UsesItem(RecipeIngredientEntity line)
            {
                var ingredientName = line.Ingredient?.Name?.Trim().ToLowerInvariant() ?? string.Empty;
                if (ingredientName.Length == 0)
                {
                    return false;
                }

                return ingredientName.Contains(itemName)
                    || itemName.Contains(ingredientName)
                    || (itemCode.Contains("FLR") && ingredientName.Contains("flour"))
                    || (itemCode.Contains("SGR") && ingredientName.Contains("sugar"))
                    || (itemCode.Contains("YST") && ingredientName.Contains("yeast"))
                    || (itemCode.Contains("SFT") && ingredientName.Contains("softener"))
                    || (itemCode.Contains("FLV") && ingredientName.Contains("flavour"))
                    || (itemCode.Contains("FLV") && ingredientName.Contains("flavor"))
                    || (itemCode.Contains("FAT") && ingredientName.Contains("shortening"))
                    || (itemCode.Contains("SHT") && ingredientName.Contains("shortening"))
                    || (itemCode.Contains("SLT") && ingredientName.Contains("salt"))
                    || (itemCode.Contains("JMB") && ingredientName.Contains("jumbo"))
                    || (itemCode.Contains("MED") && ingredientName.Contains("medium"))
                    || (itemCode.Contains("BTL") && ingredientName.Contains("bottle"))
                    || (itemCode.Contains("POP") && ingredientName.Contains("popcorn"))
                    || (itemCode.Contains("CRN") && ingredientName.Contains("maize"))
                    || (itemCode.Contains("OIL") && ingredientName.Contains("oil"));
            }

            return recipes
                .Where(recipe => recipe.RecipeIngredients.Any(UsesItem))
                .Select(recipe => new InventoryRecipeUsageDto
                {
                    Id = recipe.Id,
                    Name = recipe.Name,
                    Code = recipe.Code,
                    ExpectedYield = recipe.ExpectedYield,
                    OutputUnit = recipe.OutputUnit
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
                .GroupBy(i => i.InventoryItemId)
                .Select(group => new
                {
                    InventoryItemId = group.Key,
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
                request.StoreRequestItems.Add(new StoreRequestItemEntity
                {
                    InventoryItemId = inventoryItem.Id,
                    ItemName = inventoryItem.Name,
                    Unit = inventoryItem.Unit,
                    RequestedQuantity = requested.RequestedQuantity,
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
                    Requested = group.Sum(line => line.RequestedQuantity),
                    Available = group.First().InventoryItem?.QuantityInStock ?? 0m
                })
                .Where(item => item.Available < item.Requested)
                .Select(item => $"Insufficient stock for {item.Line.ItemName} — available {item.Available:N1} {item.Line.Unit}, requested {item.Requested:N1} {item.Line.Unit}.")
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

                if (line.InventoryItem == null || line.IssuedQuantity <= 0)
                {
                    continue;
                }

                line.InventoryItem.QuantityInStock -= line.IssuedQuantity;
                line.InventoryItem.UpdatedAt = DateTime.UtcNow;

                _db.StockMovements.Add(new StockMovementEntity
                {
                    InventoryItemId = line.InventoryItem.Id,
                    MovementType = "Issue",
                    Quantity = line.IssuedQuantity,
                    BalanceAfter = line.InventoryItem.QuantityInStock,
                    StoreRequestId = request.Id,
                    ReferenceNumber = request.RequestNumber,
                    Notes = "Approved store release",
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
                        IssuedQuantity = i.IssuedQuantity
                    })
                    .ToList()
            };
        }
    }
}
