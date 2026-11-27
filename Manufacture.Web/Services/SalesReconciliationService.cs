using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using ReconciliationEntity = Manufacture.Models.Entities.SalesReconciliation;
using ReconciliationItemEntity = Manufacture.Models.Entities.SalesReconciliationItem;

namespace Manufacture.Services;

public sealed class SalesReconciliationService : ISalesReconciliationService
{
    private readonly ManufactureDbContext _db;
    private readonly ProductStockLedger _stockLedger;

    public SalesReconciliationService(ManufactureDbContext db, ProductStockLedger stockLedger)
    {
        _db = db;
        _stockLedger = stockLedger;
    }

    public async Task<SalesReconciliationDto> GetOrCreateAsync(int salesRepId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var rep = await _db.SalesReps
            .Include(r => r.PosTerminal)
            .Include(r => r.Vehicle)
            .FirstOrDefaultAsync(r => r.Id == salesRepId, cancellationToken)
            ?? throw new InvalidOperationException("Sales representative not found.");

        var terminal = rep.PosTerminal;
        if (terminal == null && rep.EmployeeId.HasValue)
        {
            terminal = await _db.PosTerminals
                .FirstOrDefaultAsync(t => t.AssignedEmployeeId == rep.EmployeeId && t.IsActive, cancellationToken);
        }

        if (terminal?.ProductRepositoryId == null)
        {
            throw new InvalidOperationException("Link a product repository to this rep's POS terminal first.");
        }

        var existing = await _db.SalesReconciliations
            .Include(r => r.SalesReconciliationItems)
            .ThenInclude(i => i.Recipe)
            .Include(r => r.SalesRep)
            .Include(r => r.PosTerminal)
            .Include(r => r.ProductRepository)
            .FirstOrDefaultAsync(r => r.SalesRepId == salesRepId && r.ReconciliationDate == date, cancellationToken);

        if (existing != null)
        {
            return await MapAsync(existing, cancellationToken);
        }

        var repositoryId = terminal.ProductRepositoryId.Value;
        var repository = await _db.ProductRepositories
            .FirstOrDefaultAsync(r => r.Id == repositoryId, cancellationToken)
            ?? throw new InvalidOperationException("The linked product repository was not found.");

        var stocks = await _db.ProductRepositoryStocks
            .AsNoTracking()
            .Include(s => s.Recipe)
            .Where(s => s.ProductRepositoryId == repositoryId)
            .OrderBy(s => s.Recipe.Name)
            .ToListAsync(cancellationToken);

        var previous = await _db.SalesReconciliations
            .AsNoTracking()
            .Include(r => r.SalesReconciliationItems)
            .Where(r => r.SalesRepId == salesRepId
                && r.ProductRepositoryId == repositoryId
                && r.ReconciliationDate < date
                && r.Status == (int)SalesReconciliationStatus.Reconciled)
            .OrderByDescending(r => r.ReconciliationDate)
            .FirstOrDefaultAsync(cancellationToken);

        var soldByRecipe = await GetSystemSoldAsync(salesRepId, terminal.Id, date, cancellationToken);

        var entity = new ReconciliationEntity
        {
            ReconciliationNumber = $"REC-{date:yyyyMMdd}-{rep.Id:000}",
            ReconciliationDate = date,
            SalesRepId = rep.Id,
            PosTerminalId = terminal.Id,
            ProductRepositoryId = repositoryId,
            RouteName = rep.RouteName ?? string.Empty,
            VehicleRegistration = rep.Vehicle?.RegistrationNumber ?? string.Empty,
            Status = (int)SalesReconciliationStatus.Draft,
            Notes = string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var stock in stocks)
        {
            soldByRecipe.TryGetValue(stock.RecipeId, out var sold);

            var previousItem = previous?.SalesReconciliationItems
                .FirstOrDefault(i => i.RecipeId == stock.RecipeId);
            var opening = previousItem?.ClosingStock
                ?? (int)Math.Max(0m, Math.Round(stock.QuantityOnHand) + sold);

            entity.SalesReconciliationItems.Add(new ReconciliationItemEntity
            {
                RecipeId = stock.RecipeId,
                ProductName = stock.Recipe.Name,
                ProductCode = stock.Recipe.Code,
                UnitPrice = stock.Recipe.SellingPrice,
                OpeningStock = opening,
                QtySold = sold,
                QtyDamaged = 0,
                QtyReplacement = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        entity.TotalExpectedRevenue = entity.SalesReconciliationItems.Sum(i => i.QtySold * i.UnitPrice);

        _db.SalesReconciliations.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        entity.SalesRep = rep;
        entity.PosTerminal = terminal;
        entity.ProductRepository = repository;
        return await MapAsync(entity, cancellationToken);
    }

    public async Task<SalesReconciliationDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.SalesReconciliations
            .AsNoTracking()
            .Include(r => r.SalesReconciliationItems)
            .ThenInclude(i => i.Recipe)
            .Include(r => r.SalesRep)
            .Include(r => r.PosTerminal)
            .Include(r => r.ProductRepository)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return entity == null ? null : await MapAsync(entity, cancellationToken);
    }

    public async Task<List<ReconciliationSummaryDto>> ListAsync(DateOnly date, int? salesRepId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.SalesReconciliations
            .AsNoTracking()
            .Include(r => r.SalesRep)
            .Include(r => r.PosTerminal)
            .Include(r => r.ProductRepository)
            .Where(r => r.ReconciliationDate == date);

        if (salesRepId.HasValue && salesRepId.Value > 0)
        {
            query = query.Where(r => r.SalesRepId == salesRepId.Value);
        }

        var rows = await query
            .OrderBy(r => r.SalesRep!.FullName)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new ReconciliationSummaryDto
        {
            Id = r.Id,
            ReconciliationNumber = r.ReconciliationNumber,
            ReconciliationDate = r.ReconciliationDate.ToDateTime(TimeOnly.MinValue),
            SalesRepId = r.SalesRepId,
            RepName = r.SalesRep?.FullName ?? string.Empty,
            PosTerminalCode = r.PosTerminal?.TerminalCode ?? string.Empty,
            ProductRepositoryName = r.ProductRepository?.Name ?? string.Empty,
            Status = (SalesReconciliationStatus)r.Status,
            TotalExpectedRevenue = r.TotalExpectedRevenue,
            TotalCollected = r.TotalCollected ?? 0m,
            Variance = r.Variance ?? 0m
        }).ToList();
    }

    public async Task<SalesReconciliationDto> SyncProductsAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await LoadTrackedAsync(id, cancellationToken);
        if (entity.Status != (int)SalesReconciliationStatus.Draft)
        {
            throw new InvalidOperationException("Reconciled sheets cannot be changed.");
        }

        if (entity.ProductRepositoryId == null)
        {
            throw new InvalidOperationException("This reconciliation has no linked product repository.");
        }

        var stocks = await _db.ProductRepositoryStocks
            .AsNoTracking()
            .Include(s => s.Recipe)
            .Where(s => s.ProductRepositoryId == entity.ProductRepositoryId.Value)
            .ToListAsync(cancellationToken);

        var existingRecipeIds = entity.SalesReconciliationItems.Select(i => i.RecipeId).ToHashSet();
        var soldByRecipe = await GetSystemSoldAsync(entity.SalesRepId, entity.PosTerminalId, entity.ReconciliationDate, cancellationToken);

        foreach (var stock in stocks.Where(s => !existingRecipeIds.Contains(s.RecipeId)))
        {
            soldByRecipe.TryGetValue(stock.RecipeId, out var sold);
            entity.SalesReconciliationItems.Add(new ReconciliationItemEntity
            {
                RecipeId = stock.RecipeId,
                ProductName = stock.Recipe.Name,
                ProductCode = stock.Recipe.Code,
                UnitPrice = stock.Recipe.SellingPrice,
                OpeningStock = 0,
                QtySold = sold,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        entity.TotalExpectedRevenue = entity.SalesReconciliationItems.Sum(i => i.QtySold * i.UnitPrice);
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetAsync(id, cancellationToken))!;
    }

    public async Task<SalesReconciliationDto> SaveDraftAsync(SaveReconciliationDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await LoadTrackedAsync(dto.Id, cancellationToken);
        if (entity.Status != (int)SalesReconciliationStatus.Draft)
        {
            throw new InvalidOperationException("This reconciliation has already been closed.");
        }

        await ApplyInputsAsync(entity, dto, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetAsync(dto.Id, cancellationToken))!;
    }

    public async Task<SalesReconciliationDto> ReconcileAsync(SaveReconciliationDto dto, string reconciledBy, CancellationToken cancellationToken = default)
    {
        var entity = await LoadTrackedAsync(dto.Id, cancellationToken);
        if (entity.Status == (int)SalesReconciliationStatus.Reconciled)
        {
            return (await GetAsync(dto.Id, cancellationToken))!;
        }

        await ApplyInputsAsync(entity, dto, cancellationToken);
        await RollClosingStockForwardAsync(entity, cancellationToken);

        if (entity.StockPostedAt == null)
        {
            if (entity.ProductRepositoryId == null)
            {
                throw new InvalidOperationException("This reconciliation has no linked product repository.");
            }

            var actor = string.IsNullOrWhiteSpace(reconciledBy) ? "Sales" : reconciledBy.Trim();
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            foreach (var item in entity.SalesReconciliationItems)
            {
                if (item.QtyDamaged > 0)
                {
                    await _stockLedger.ApplyAsync(
                        entity.ProductRepositoryId.Value,
                        item.RecipeId,
                        ProductStockMovementType.DamageOut,
                        item.QtyDamaged,
                        "Reconciliation",
                        entity.Id,
                        entity.ReconciliationNumber,
                        actor,
                        $"Damaged stock · {item.ProductName}",
                        cancellationToken);
                }

                if (item.QtyReplacement > 0)
                {
                    await _stockLedger.ApplyAsync(
                        entity.ProductRepositoryId.Value,
                        item.RecipeId,
                        ProductStockMovementType.ReplacementOut,
                        item.QtyReplacement,
                        "Reconciliation",
                        entity.Id,
                        entity.ReconciliationNumber,
                        actor,
                        $"Replacement stock · {item.ProductName}",
                        cancellationToken);
                }
            }

            entity.StockPostedAt = DateTime.UtcNow;
            entity.Status = (int)SalesReconciliationStatus.Reconciled;
            entity.ReconciledBy = actor;
            entity.ReconciledAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        return (await GetAsync(dto.Id, cancellationToken))!;
    }

    private async Task ApplyInputsAsync(ReconciliationEntity entity, SaveReconciliationDto dto, CancellationToken cancellationToken)
    {
        var canEditOpening = !await HasPriorReconciledAsync(entity, cancellationToken);
        var submittedById = dto.Items.ToDictionary(i => i.Id);
        var submittedByRecipe = dto.Items.ToDictionary(i => i.RecipeId);

        foreach (var item in entity.SalesReconciliationItems)
        {
            var input = submittedById.GetValueOrDefault(item.Id) ?? submittedByRecipe.GetValueOrDefault(item.RecipeId);
            if (input == null)
            {
                continue;
            }

            if (input.QtySold < 0 || input.QtyDamaged < 0 || input.QtyReplacement < 0 || input.OpeningStock < 0)
            {
                throw new InvalidOperationException($"Quantities for '{item.ProductName}' cannot be negative.");
            }

            if (input.OpeningStock < input.QtySold + input.QtyDamaged + input.QtyReplacement)
            {
                throw new InvalidOperationException(
                    $"Closing stock for '{item.ProductName}' would be negative. Check the opening, sold, damaged and replacement quantities.");
            }

            if (canEditOpening)
            {
                item.OpeningStock = input.OpeningStock;
            }

            item.QtySold = input.QtySold;
            item.QtyDamaged = input.QtyDamaged;
            item.QtyReplacement = input.QtyReplacement;
            item.UpdatedAt = DateTime.UtcNow;
        }

        entity.CashHandedOver = Math.Max(0m, dto.CashHandedOver);
        entity.PosTerminalReceipts = Math.Max(0m, dto.PosTerminalReceipts);
        entity.TotalExpectedRevenue = entity.SalesReconciliationItems.Sum(i => i.QtySold * i.UnitPrice);
        entity.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? entity.Notes : dto.Notes.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await Task.CompletedTask;
    }

    private async Task<ReconciliationEntity> LoadTrackedAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.SalesReconciliations
            .Include(r => r.SalesReconciliationItems)
            .ThenInclude(i => i.Recipe)
            .Include(r => r.SalesRep)
            .Include(r => r.PosTerminal)
            .Include(r => r.ProductRepository)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Sales reconciliation not found.");
    }

    private async Task RollClosingStockForwardAsync(ReconciliationEntity entity, CancellationToken cancellationToken)
    {
        var futureDrafts = await _db.SalesReconciliations
            .Include(r => r.SalesReconciliationItems)
            .Where(r => r.SalesRepId == entity.SalesRepId
                && r.ProductRepositoryId == entity.ProductRepositoryId
                && r.ReconciliationDate > entity.ReconciliationDate
                && r.Status == (int)SalesReconciliationStatus.Draft)
            .OrderBy(r => r.ReconciliationDate)
            .ToListAsync(cancellationToken);

        if (futureDrafts.Count == 0)
        {
            return;
        }

        var closingByRecipe = entity.SalesReconciliationItems.ToDictionary(
            i => i.RecipeId,
            i => i.OpeningStock - i.QtySold - i.QtyDamaged - i.QtyReplacement);

        foreach (var draft in futureDrafts)
        {
            foreach (var item in draft.SalesReconciliationItems)
            {
                var carried = closingByRecipe.GetValueOrDefault(item.RecipeId);
                var minimum = item.QtySold + item.QtyDamaged + item.QtyReplacement;
                item.OpeningStock = Math.Max(carried, minimum);
                item.UpdatedAt = DateTime.UtcNow;
                closingByRecipe[item.RecipeId] = item.OpeningStock - item.QtySold - item.QtyDamaged - item.QtyReplacement;
            }

            draft.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task<bool> HasPriorReconciledAsync(ReconciliationEntity entity, CancellationToken cancellationToken)
    {
        return await _db.SalesReconciliations
            .AsNoTracking()
            .AnyAsync(r => r.SalesRepId == entity.SalesRepId
                && r.ReconciliationDate < entity.ReconciliationDate
                && r.Status == (int)SalesReconciliationStatus.Reconciled, cancellationToken);
    }

    private async Task<Dictionary<int, int>> GetSystemSoldAsync(
        int salesRepId,
        int? posTerminalId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var start = date.ToDateTime(TimeOnly.MinValue);
        var end = start.AddDays(1);

        var query = _db.Orders
            .AsNoTracking()
            .Where(o => o.SalesRepId == salesRepId
                && o.StockDeductedAt != null
                && o.StockDeductedAt >= start
                && o.StockDeductedAt < end
                && o.Status != (int)Manufacture.Models.Domain.OrderStatus.Cancelled);

        if (posTerminalId.HasValue)
        {
            query = query.Where(o => o.PosTerminalId == posTerminalId.Value);
        }

        return await query
            .SelectMany(o => o.OrderItems)
            .GroupBy(i => i.RecipeId)
            .Select(g => new { RecipeId = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .ToDictionaryAsync(x => x.RecipeId, x => x.Quantity, cancellationToken);
    }

    private async Task<SalesReconciliationDto> MapAsync(ReconciliationEntity entity, CancellationToken cancellationToken)
    {
        var soldByRecipe = await GetSystemSoldAsync(
            entity.SalesRepId,
            entity.PosTerminalId,
            entity.ReconciliationDate,
            cancellationToken);

        var canEditOpening = !await HasPriorReconciledAsync(entity, cancellationToken);

        return new SalesReconciliationDto
        {
            Id = entity.Id,
            ReconciliationNumber = entity.ReconciliationNumber,
            ReconciliationDate = entity.ReconciliationDate.ToDateTime(TimeOnly.MinValue),
            SalesRepId = entity.SalesRepId,
            RepName = entity.SalesRep?.FullName ?? string.Empty,
            RepCode = entity.SalesRep?.RepCode ?? string.Empty,
            PosTerminalId = entity.PosTerminalId,
            PosTerminalCode = entity.PosTerminal?.TerminalCode ?? string.Empty,
            ProductRepositoryId = entity.ProductRepositoryId,
            ProductRepositoryName = entity.ProductRepository?.Name ?? string.Empty,
            RouteName = entity.RouteName,
            VehicleRegistration = entity.VehicleRegistration,
            CashHandedOver = entity.CashHandedOver,
            PosTerminalReceipts = entity.PosTerminalReceipts,
            TotalExpectedRevenue = entity.TotalExpectedRevenue,
            Status = (SalesReconciliationStatus)entity.Status,
            Notes = entity.Notes,
            ReconciledBy = entity.ReconciledBy,
            ReconciledAt = entity.ReconciledAt,
            StockPostedAt = entity.StockPostedAt,
            CanEditOpening = canEditOpening,
            Items = entity.SalesReconciliationItems
                .OrderBy(i => i.ProductName)
                .Select(i => new SalesReconciliationItemDto
                {
                    Id = i.Id,
                    RecipeId = i.RecipeId,
                    ProductName = i.ProductName,
                    ProductCode = i.ProductCode,
                    OutputUnit = i.Recipe?.OutputUnit ?? "units",
                    UnitPrice = i.UnitPrice,
                    OpeningStock = i.OpeningStock,
                    QtySold = i.QtySold,
                    QtyDamaged = i.QtyDamaged,
                    QtyReplacement = i.QtyReplacement,
                    SystemSoldQuantity = soldByRecipe.GetValueOrDefault(i.RecipeId)
                })
                .ToList()
        };
    }
}
