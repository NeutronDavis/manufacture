using Manufacture.Models.DTOs;

namespace Manufacture.Library.Interfaces
{
    public interface IInventoryService
    {
        Task<List<InventoryItemDto>> GetItemsAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

        Task<InventoryItemDto?> GetItemAsync(int id, CancellationToken cancellationToken = default);

        Task<InventoryItemDto> CreateItemAsync(InventoryItemDto dto, string? performedBy = null, CancellationToken cancellationToken = default);

        Task<InventoryItemDto?> UpdateItemAsync(int id, InventoryItemDto dto, CancellationToken cancellationToken = default);

        Task<bool> RestockItemAsync(int id, decimal addedQuantity, string? performedBy = null, CancellationToken cancellationToken = default);

        Task<int> GetLowStockCountAsync(CancellationToken cancellationToken = default);

        Task<List<StockMovementDto>> GetStockMovementsAsync(
            int? inventoryItemId = null,
            string? movementType = null,
            int take = 200,
            CancellationToken cancellationToken = default);

        Task<List<InventoryRecipeUsageDto>> GetRecipeUsageAsync(int itemId, CancellationToken cancellationToken = default);

        Task<List<SupplierDto>> GetSuppliersAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

        Task<SupplierDto> CreateSupplierAsync(SupplierDto dto, CancellationToken cancellationToken = default);

        Task<List<StoreRequestDto>> GetRequestsAsync(CancellationToken cancellationToken = default);

        Task<List<StoreRequestDto>> GetRequestsByItemAsync(int itemId, CancellationToken cancellationToken = default);

        Task<StoreRequestDto> CreateRequestAsync(StoreRequestDto dto, CancellationToken cancellationToken = default);

        Task<bool> ApproveAndIssueRequestAsync(int id, string approverName, CancellationToken cancellationToken = default);

        Task<bool> RejectRequestAsync(int id, string approverName, string reason, CancellationToken cancellationToken = default);

        Task<int> GetPendingRequestCountAsync(CancellationToken cancellationToken = default);
    }
}
