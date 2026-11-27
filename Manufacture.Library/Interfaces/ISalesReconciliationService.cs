using Manufacture.Models.DTOs;

namespace Manufacture.Library.Interfaces
{
    public interface ISalesReconciliationService
    {
        Task<SalesReconciliationDto> GetOrCreateAsync(int salesRepId, DateOnly date, CancellationToken cancellationToken = default);
        Task<SalesReconciliationDto?> GetAsync(int id, CancellationToken cancellationToken = default);
        Task<List<ReconciliationSummaryDto>> ListAsync(DateOnly date, int? salesRepId = null, CancellationToken cancellationToken = default);
        Task<SalesReconciliationDto> SyncProductsAsync(int id, CancellationToken cancellationToken = default);
        Task<SalesReconciliationDto> SaveDraftAsync(SaveReconciliationDto dto, CancellationToken cancellationToken = default);
        Task<SalesReconciliationDto> ReconcileAsync(SaveReconciliationDto dto, string reconciledBy, CancellationToken cancellationToken = default);
    }
}
