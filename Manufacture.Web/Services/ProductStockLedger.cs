using Manufacture.Library.Data;
using Manufacture.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using StockEntity = Manufacture.Models.Entities.ProductRepositoryStock;
using MovementEntity = Manufacture.Models.Entities.ProductRepositoryMovement;

namespace Manufacture.Services;

/// <summary>
/// Applies a single finished-goods stock movement to a repository without saving.
/// The caller owns the transaction and calls <c>SaveChangesAsync</c> once.
/// </summary>
public sealed class ProductStockLedger
{
    private readonly ManufactureDbContext _db;

    public ProductStockLedger(ManufactureDbContext db)
    {
        _db = db;
    }

    public async Task ApplyAsync(
        int repositoryId,
        int recipeId,
        ProductStockMovementType movementType,
        decimal quantity,
        string referenceType,
        int? referenceId,
        string referenceNumber,
        string performedBy,
        string notes,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0m)
        {
            throw new InvalidOperationException("Stock movement quantity must be greater than zero.");
        }

        var repository = await _db.ProductRepositories
            .FirstOrDefaultAsync(r => r.Id == repositoryId, cancellationToken)
            ?? throw new InvalidOperationException("Product repository not found.");

        if (!repository.IsActive)
        {
            throw new InvalidOperationException($"Repository '{repository.Name}' is inactive.");
        }

        var recipe = await _db.Recipes
            .FirstOrDefaultAsync(r => r.Id == recipeId, cancellationToken)
            ?? throw new InvalidOperationException("Product recipe not found.");

        var isInbound = movementType is ProductStockMovementType.ProductionIn
            or ProductStockMovementType.TransferIn
            or ProductStockMovementType.AdjustmentIn
            or ProductStockMovementType.OpeningBalance;

        var stock = _db.ProductRepositoryStocks.Local
            .FirstOrDefault(s => s.ProductRepositoryId == repositoryId && s.RecipeId == recipeId);

        stock ??= await _db.ProductRepositoryStocks
            .FirstOrDefaultAsync(s => s.ProductRepositoryId == repositoryId && s.RecipeId == recipeId, cancellationToken);

        if (stock == null)
        {
            if (!isInbound)
            {
                throw new InvalidOperationException(
                    $"'{recipe.Name}' is not stocked in '{repository.Name}'. Add it to the repository first.");
            }

            stock = new StockEntity
            {
                ProductRepositoryId = repositoryId,
                RecipeId = recipeId,
                QuantityOnHand = 0m,
                ReorderThreshold = 0m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.ProductRepositoryStocks.Add(stock);
        }

        var newBalance = isInbound
            ? stock.QuantityOnHand + quantity
            : stock.QuantityOnHand - quantity;

        if (newBalance < 0m)
        {
            throw new InvalidOperationException(
                $"Insufficient stock for '{recipe.Name}' in '{repository.Name}'. " +
                $"Available {stock.QuantityOnHand:N0} {recipe.OutputUnit}, requested {quantity:N0} {recipe.OutputUnit}.");
        }

        stock.QuantityOnHand = newBalance;
        stock.UpdatedAt = DateTime.UtcNow;

        _db.ProductRepositoryMovements.Add(new MovementEntity
        {
            ProductRepositoryId = repositoryId,
            RecipeId = recipeId,
            MovementType = (int)movementType,
            Quantity = quantity,
            BalanceAfter = newBalance,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            ReferenceNumber = referenceNumber,
            Notes = notes,
            PerformedBy = performedBy,
            CreatedAt = DateTime.UtcNow
        });
    }
}
