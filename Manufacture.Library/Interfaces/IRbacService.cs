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

        /// <summary>
        /// Creates a "None" permission row for every active module that has no
        /// row yet for the role. Returns the number of rows created.
        /// </summary>
        Task<int> InitializeRolePermissionsAsync(int roleId, string updatedBy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs <see cref="InitializeRolePermissionsAsync"/> for every role.
        /// Idempotent; used at startup for roles created outside the app.
        /// </summary>
        Task<int> InitializeMissingRolePermissionsAsync(string updatedBy, CancellationToken cancellationToken = default);

        Task RefreshCacheAsync(CancellationToken cancellationToken = default);
    }
}
