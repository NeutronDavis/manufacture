using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using RepositoryEntity = Manufacture.Models.Entities.ProductRepository;
using StockEntity = Manufacture.Models.Entities.ProductRepositoryStock;
using MovementEntity = Manufacture.Models.Entities.ProductRepositoryMovement;

namespace Manufacture.Services;

public sealed class ProductRepositoryService : IProductRepositoryService
{
    private readonly ManufactureDbContext _db;
    private readonly ProductStockLedger _ledger;

    public ProductRepositoryService(ManufactureDbContext db, ProductStockLedger ledger)
    {
        _db = db;
        _ledger = ledger;
    }

    public async Task<List<ProductRepositoryDto>> GetRepositoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _db.ProductRepositories
            .AsNoTracking()
            .Include(r => r.ProductRepositoryStocks)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(r => r.IsActive);
        }

        var repositories = await query
            .OrderByDescending(r => r.IsDefaultProductionOutput)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return repositories.Select(MapRepository).ToList();
    }

    public async Task<ProductRepositoryDto?> GetRepositoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var repository = await _db.ProductRepositories
            .AsNoTracking()
            .Include(r => r.ProductRepositoryStocks)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return repository == null ? null : MapRepository(repository);
    }

    public async Task<ProductRepositoryDto?> GetDefaultProductionRepositoryAsync(CancellationToken cancellationToken = default)
    {
        var repository = await _db.ProductRepositories
            .AsNoTracking()
            .Include(r => r.ProductRepositoryStocks)
            .FirstOrDefaultAsync(r => r.IsDefaultProductionOutput && r.IsActive, cancellationToken);

        return repository == null ? null : MapRepository(repository);
    }

    public async Task<ProductRepositoryDto> CreateRepositoryAsync(ProductRepositoryInputDto dto, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(dto.Code);
        if (await _db.ProductRepositories.AnyAsync(r => r.Code == code, cancellationToken))
        {
            throw new InvalidOperationException($"A repository with code '{code}' already exists.");
        }

        var makeDefault = dto.IsDefaultProductionOutput;
        if (makeDefault && dto.LocationType != ProductRepositoryLocationType.Factory)
        {
            throw new InvalidOperationException("Only a Factory repository can be the default production output.");
        }

        if (makeDefault)
        {
            await ClearOtherDefaultsAsync(0, cancellationToken);
        }

        var repository = new RepositoryEntity
        {
            Code = code,
            Name = dto.Name.Trim(),
            LocationType = (int)dto.LocationType,
            Address = (dto.Address ?? string.Empty).Trim(),
            IsDefaultProductionOutput = makeDefault,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.ProductRepositories.Add(repository);
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetRepositoryAsync(repository.Id, cancellationToken))!;
    }

    public async Task<ProductRepositoryDto> UpdateRepositoryAsync(int id, ProductRepositoryInputDto dto, CancellationToken cancellationToken = default)
    {
        var repository = await _db.ProductRepositories.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Product repository not found.");

        var code = NormalizeCode(dto.Code);
        if (await _db.ProductRepositories.AnyAsync(r => r.Id != id && r.Code == code, cancellationToken))
        {
            throw new InvalidOperationException($"A repository with code '{code}' already exists.");
        }

        if (dto.IsDefaultProductionOutput && dto.LocationType != ProductRepositoryLocationType.Factory)
        {
            throw new InvalidOperationException("Only a Factory repository can be the default production output.");
        }

        if (dto.IsDefaultProductionOutput)
        {
            await ClearOtherDefaultsAsync(id, cancellationToken);
        }

        repository.Code = code;
        repository.Name = dto.Name.Trim();
        repository.LocationType = (int)dto.LocationType;
        repository.Address = (dto.Address ?? string.Empty).Trim();
        repository.IsDefaultProductionOutput = dto.IsDefaultProductionOutput;
        repository.IsActive = dto.IsActive;
        repository.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return (await GetRepositoryAsync(id, cancellationToken))!;
    }

    public async Task<bool> ToggleStatusAsync(int id, CancellationToken cancellationToken = default)
    {
        var repository = await _db.ProductRepositories.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (repository == null)
        {
            return false;
        }

        if (repository.IsDefaultProductionOutput && repository.IsActive)
        {
            throw new InvalidOperationException("Choose another default production repository before deactivating this one.");
        }

        repository.IsActive = !repository.IsActive;
        repository.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<ProductRepositoryStockDto>> GetStockAsync(int repositoryId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.ProductRepositoryStocks
            .AsNoTracking()
            .Include(s => s.Recipe)
            .Where(s => s.ProductRepositoryId == repositoryId)
            .OrderBy(s => s.Recipe.Name)
            .ToListAsync(cancellationToken);

        return rows.Select(MapStock).ToList();
    }

    public async Task<List<ProductRepositoryTotalDto>> GetProductTotalsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.ProductRepositoryStocks
            .AsNoTracking()
            .Include(s => s.Recipe)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(s => new { s.RecipeId, s.Recipe.Name, s.Recipe.Code, s.Recipe.OutputUnit })
            .Select(g => new ProductRepositoryTotalDto
            {
                RecipeId = g.Key.RecipeId,
                ProductName = g.Key.Name,
                ProductCode = g.Key.Code,
                OutputUnit = g.Key.OutputUnit,
                TotalQuantityOnHand = g.Sum(s => s.QuantityOnHand),
                TotalThreshold = g.Sum(s => s.ReorderThreshold),
                RepositoryCount = g.Count()
            })
            .OrderBy(r => r.ProductName)
            .ToList();
    }

    public async Task<List<ProductRepositoryStockDto>> GetLowStockAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.ProductRepositoryStocks
            .AsNoTracking()
            .Include(s => s.Recipe)
            .Where(s => s.QuantityOnHand <= s.ReorderThreshold)
            .OrderBy(s => s.Recipe.Name)
            .ToListAsync(cancellationToken);

        return rows.Select(MapStock).ToList();
    }

    public async Task<ProductRepositoryStockDto> AddOrUpdateStockItemAsync(int repositoryId, int recipeId, decimal quantity, decimal threshold, CancellationToken cancellationToken = default)
    {
        if (quantity < 0m || threshold < 0m)
        {
            throw new InvalidOperationException("Quantity and threshold cannot be negative.");
        }

        var stock = await _db.ProductRepositoryStocks
            .FirstOrDefaultAsync(s => s.ProductRepositoryId == repositoryId && s.RecipeId == recipeId, cancellationToken);

        if (stock == null)
        {
            stock = new StockEntity
            {
                ProductRepositoryId = repositoryId,
                RecipeId = recipeId,
                QuantityOnHand = 0m,
                ReorderThreshold = Math.Max(0m, threshold),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.ProductRepositoryStocks.Add(stock);
        }

        stock.ReorderThreshold = Math.Max(0m, threshold);
        stock.UpdatedAt = DateTime.UtcNow;

        if (quantity > stock.QuantityOnHand)
        {
            await _ledger.ApplyAsync(repositoryId, recipeId, ProductStockMovementType.AdjustmentIn,
                quantity - stock.QuantityOnHand, "Manual", null, string.Empty, "Admin", "Initial / updated stock level", cancellationToken);
        }
        else if (quantity < stock.QuantityOnHand)
        {
            await _ledger.ApplyAsync(repositoryId, recipeId, ProductStockMovementType.AdjustmentOut,
                stock.QuantityOnHand - quantity, "Manual", null, string.Empty, "Admin", "Corrected stock level", cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var saved = await _db.ProductRepositoryStocks
            .AsNoTracking()
            .Include(s => s.Recipe)
            .FirstAsync(s => s.ProductRepositoryId == repositoryId && s.RecipeId == recipeId, cancellationToken);

        return MapStock(saved);
    }

    public async Task<List<StockProductOptionDto>> GetProductsForRepositoryAsync(int repositoryId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.ProductRepositoryStocks
            .AsNoTracking()
            .Include(s => s.Recipe)
            .Where(s => s.ProductRepositoryId == repositoryId)
            .OrderBy(s => s.Recipe.Name)
            .ToListAsync(cancellationToken);

        return rows.Select(s => new StockProductOptionDto
        {
            RecipeId = s.RecipeId,
            Name = s.Recipe.Name,
            Code = s.Recipe.Code,
            OutputUnit = s.Recipe.OutputUnit,
            SellingPrice = s.Recipe.SellingPrice,
            AvailableQuantity = s.QuantityOnHand
        }).ToList();
    }

    public async Task<List<StockProductOptionDto>> GetAllProductOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Recipes
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .Select(r => new StockProductOptionDto
            {
                RecipeId = r.Id,
                Name = r.Name,
                Code = r.Code,
                OutputUnit = r.OutputUnit,
                SellingPrice = r.SellingPrice,
                AvailableQuantity = 0m
            })
            .ToListAsync(cancellationToken);
    }

    public async Task AdjustStockAsync(AdjustStockInputDto dto, string performedBy, CancellationToken cancellationToken = default)
    {
        if (dto.NewQuantity < 0m)
        {
            throw new InvalidOperationException("New quantity cannot be negative.");
        }

        var stock = await _db.ProductRepositoryStocks
            .FirstOrDefaultAsync(s => s.ProductRepositoryId == dto.RepositoryId && s.RecipeId == dto.RecipeId, cancellationToken);

        var current = stock?.QuantityOnHand ?? 0m;
        if (dto.NewQuantity > current)
        {
            await _ledger.ApplyAsync(dto.RepositoryId, dto.RecipeId, ProductStockMovementType.AdjustmentIn,
                dto.NewQuantity - current, "Manual", null, string.Empty, performedBy, dto.Notes, cancellationToken);
        }
        else if (dto.NewQuantity < current)
        {
            await _ledger.ApplyAsync(dto.RepositoryId, dto.RecipeId, ProductStockMovementType.AdjustmentOut,
                current - dto.NewQuantity, "Manual", null, string.Empty, performedBy, dto.Notes, cancellationToken);
        }

        if (stock != null && dto.ReorderThreshold.HasValue)
        {
            stock.ReorderThreshold = Math.Max(0m, dto.ReorderThreshold.Value);
            stock.UpdatedAt = DateTime.UtcNow;
        }
        else if (stock == null && dto.ReorderThreshold.HasValue)
        {
            // The ledger creates the row for an inbound adjustment.
            var created = _db.ProductRepositoryStocks.Local
                .FirstOrDefault(s => s.ProductRepositoryId == dto.RepositoryId && s.RecipeId == dto.RecipeId);
            if (created != null)
            {
                created.ReorderThreshold = Math.Max(0m, dto.ReorderThreshold.Value);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task TransferStockAsync(TransferStockInputDto dto, string performedBy, CancellationToken cancellationToken = default)
    {
        if (dto.FromRepositoryId == dto.ToRepositoryId)
        {
            throw new InvalidOperationException("Choose two different repositories.");
        }

        if (dto.Quantity <= 0m)
        {
            throw new InvalidOperationException("Transfer quantity must be greater than zero.");
        }

        var referenceNumber = $"TRF-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";

        await _ledger.ApplyAsync(dto.FromRepositoryId, dto.RecipeId, ProductStockMovementType.TransferOut,
            dto.Quantity, "Transfer", null, referenceNumber, performedBy, dto.Notes, cancellationToken);
        await _ledger.ApplyAsync(dto.ToRepositoryId, dto.RecipeId, ProductStockMovementType.TransferIn,
            dto.Quantity, "Transfer", null, referenceNumber, performedBy, dto.Notes, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ProductStockMovementDto>> GetMovementsAsync(int? repositoryId = null, int? recipeId = null, int take = 200, CancellationToken cancellationToken = default)
    {
        var query = _db.ProductRepositoryMovements
            .AsNoTracking()
            .Include(m => m.ProductRepository)
            .Include(m => m.Recipe)
            .AsQueryable();

        if (repositoryId.HasValue)
        {
            query = query.Where(m => m.ProductRepositoryId == repositoryId.Value);
        }

        if (recipeId.HasValue)
        {
            query = query.Where(m => m.RecipeId == recipeId.Value);
        }

        var rows = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Take(Math.Clamp(take, 1, 1000))
            .ToListAsync(cancellationToken);

        return rows.Select(MapMovement).ToList();
    }

    private async Task ClearOtherDefaultsAsync(int exceptId, CancellationToken cancellationToken)
    {
        var others = await _db.ProductRepositories
            .Where(r => r.Id != exceptId && r.IsDefaultProductionOutput)
            .ToListAsync(cancellationToken);

        foreach (var other in others)
        {
            other.IsDefaultProductionOutput = false;
            other.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static string NormalizeCode(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();

    private static ProductRepositoryDto MapRepository(RepositoryEntity repository)
    {
        var stock = repository.ProductRepositoryStocks;
        return new ProductRepositoryDto
        {
            Id = repository.Id,
            Code = repository.Code,
            Name = repository.Name,
            LocationType = (ProductRepositoryLocationType)repository.LocationType,
            Address = repository.Address,
            IsDefaultProductionOutput = repository.IsDefaultProductionOutput,
            IsActive = repository.IsActive,
            SkuCount = stock.Count,
            TotalUnits = stock.Sum(s => s.QuantityOnHand),
            LowStockCount = stock.Count(s => s.QuantityOnHand <= s.ReorderThreshold)
        };
    }

    private static ProductRepositoryStockDto MapStock(StockEntity stock) => new()
    {
        Id = stock.Id,
        ProductRepositoryId = stock.ProductRepositoryId,
        RecipeId = stock.RecipeId,
        ProductName = stock.Recipe.Name,
        ProductCode = stock.Recipe.Code,
        OutputUnit = stock.Recipe.OutputUnit,
        QuantityOnHand = stock.QuantityOnHand,
        ReorderThreshold = stock.ReorderThreshold,
        SellingPrice = stock.Recipe.SellingPrice
    };

    private static ProductStockMovementDto MapMovement(MovementEntity movement) => new()
    {
        Id = movement.Id,
        ProductRepositoryId = movement.ProductRepositoryId,
        RepositoryName = movement.ProductRepository?.Name ?? string.Empty,
        RecipeId = movement.RecipeId,
        ProductName = movement.Recipe?.Name ?? string.Empty,
        MovementType = (ProductStockMovementType)movement.MovementType,
        Quantity = movement.Quantity,
        BalanceAfter = movement.BalanceAfter,
        ReferenceType = movement.ReferenceType,
        ReferenceId = movement.ReferenceId,
        ReferenceNumber = movement.ReferenceNumber,
        Notes = movement.Notes,
        PerformedBy = movement.PerformedBy,
        CreatedAt = movement.CreatedAt
    };
}
