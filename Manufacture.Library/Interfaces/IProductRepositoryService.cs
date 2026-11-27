using Manufacture.Models.DTOs;

namespace Manufacture.Library.Interfaces
{
    public interface IProductRepositoryService
    {
        Task<List<ProductRepositoryDto>> GetRepositoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
        Task<ProductRepositoryDto?> GetRepositoryAsync(int id, CancellationToken cancellationToken = default);
        Task<ProductRepositoryDto?> GetDefaultProductionRepositoryAsync(CancellationToken cancellationToken = default);
        Task<ProductRepositoryDto> CreateRepositoryAsync(ProductRepositoryInputDto dto, CancellationToken cancellationToken = default);
        Task<ProductRepositoryDto> UpdateRepositoryAsync(int id, ProductRepositoryInputDto dto, CancellationToken cancellationToken = default);
        Task<bool> ToggleStatusAsync(int id, CancellationToken cancellationToken = default);

        Task<List<ProductRepositoryStockDto>> GetStockAsync(int repositoryId, CancellationToken cancellationToken = default);
        Task<List<ProductRepositoryTotalDto>> GetProductTotalsAsync(CancellationToken cancellationToken = default);
        Task<List<ProductRepositoryStockDto>> GetLowStockAsync(CancellationToken cancellationToken = default);
        Task<ProductRepositoryStockDto> AddOrUpdateStockItemAsync(int repositoryId, int recipeId, decimal quantity, decimal threshold, CancellationToken cancellationToken = default);
        Task<List<StockProductOptionDto>> GetProductsForRepositoryAsync(int repositoryId, CancellationToken cancellationToken = default);
        Task<List<StockProductOptionDto>> GetAllProductOptionsAsync(CancellationToken cancellationToken = default);

        Task AdjustStockAsync(AdjustStockInputDto dto, string performedBy, CancellationToken cancellationToken = default);
        Task TransferStockAsync(TransferStockInputDto dto, string performedBy, CancellationToken cancellationToken = default);
        Task<List<ProductStockMovementDto>> GetMovementsAsync(int? repositoryId = null, int? recipeId = null, int take = 200, CancellationToken cancellationToken = default);
    }
}
