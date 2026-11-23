using Manufacture.Models.DTOs;

namespace Manufacture.Library.Interfaces
{
    /// <summary>
    /// Database-backed role/module permission matrix.
    /// </summary>
    public interface IRbacService
    {
        Task<RbacMatrixDto> GetMatrixAsync(CancellationToken cancellationToken = default);

        Task SaveMatrixAsync(IReadOnlyList<RbacCellInputDto> cells, string updatedBy, CancellationToken cancellationToken = default);

        Task ResetToDefaultsAsync(string updatedBy, CancellationToken cancellationToken = default);

        Task RefreshCacheAsync(CancellationToken cancellationToken = default);
    }
}
