using Manufacture.Models.DTOs;

namespace Manufacture.Library.Interfaces
{
    public interface IOrganizationService
    {
        Task<List<DepartmentDto>> GetDepartmentsAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

        Task<DepartmentDto> SaveDepartmentAsync(DepartmentDto dto, CancellationToken cancellationToken = default);

        Task<bool> ToggleDepartmentStatusAsync(int id, CancellationToken cancellationToken = default);

        Task<List<JobRoleDto>> GetJobRolesAsync(int? departmentId = null, bool includeInactive = false, CancellationToken cancellationToken = default);

        Task<JobRoleDto> SaveJobRoleAsync(JobRoleDto dto, CancellationToken cancellationToken = default);

        Task<bool> ToggleJobRoleStatusAsync(int id, CancellationToken cancellationToken = default);
    }
}
