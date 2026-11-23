using Manufacture.Models.DTOs;

namespace Manufacture.Library.Interfaces
{
    /// <summary>
    /// Database-backed user, role, credential, and account-status operations.
    /// </summary>
    public interface IUserService
    {
        Task<List<UserDto>> GetAllAsync(bool includeInactive = true, CancellationToken cancellationToken = default);

        Task<UserDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<List<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);

        Task<RoleDto> SaveRoleAsync(RoleDto dto, CancellationToken cancellationToken = default);

        Task<bool> DeleteRoleAsync(int id, CancellationToken cancellationToken = default);

        Task<UserDto> CreateAsync(UserDto dto, CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(UserDto dto, CancellationToken cancellationToken = default);

        Task<bool> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

        Task<UserDto?> AuthenticatePasswordAsync(string email, string password, CancellationToken cancellationToken = default);

        Task<UserDto?> AuthenticatePinAsync(string pinCode, CancellationToken cancellationToken = default);

        Task<bool> EmailExistsAsync(string email, int? excludeUserId = null, CancellationToken cancellationToken = default);
    }
}
