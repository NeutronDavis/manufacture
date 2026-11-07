using Manufacture.Models.DTOs;

namespace Manufacture.Library.Interfaces
{
    public interface IEmployeeService
    {
        Task<List<EmployeeDto>> GetAllAsync(bool includeInactive = true, CancellationToken cancellationToken = default);

        Task<EmployeeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<List<string>> GetDepartmentsAsync(CancellationToken cancellationToken = default);

        Task<EmployeeDto> CreateAsync(EmployeeDto dto, CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(EmployeeDto dto, CancellationToken cancellationToken = default);

        Task<bool> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);

        Task<bool> StaffCodeExistsAsync(string staffCode, int? excludeEmployeeId = null, CancellationToken cancellationToken = default);

        Task<bool> EmailExistsAsync(string email, int? excludeEmployeeId = null, CancellationToken cancellationToken = default);
    }
}
