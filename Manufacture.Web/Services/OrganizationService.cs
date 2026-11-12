using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using DepartmentEntity = Manufacture.Models.Entities.Department;
using JobRoleEntity = Manufacture.Models.Entities.JobRole;

namespace Manufacture.Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly ManufactureDbContext _db;

        public OrganizationService(ManufactureDbContext db)
        {
            _db = db;
        }

        public async Task<List<DepartmentDto>> GetDepartmentsAsync(
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Departments.AsNoTracking().AsQueryable();
            if (!includeInactive)
            {
                query = query.Where(d => d.IsActive);
            }

            return await query
                .OrderBy(d => d.Name)
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    IsActive = d.IsActive
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<DepartmentDto> SaveDepartmentAsync(
            DepartmentDto dto,
            CancellationToken cancellationToken = default)
        {
            var name = NormalizeRequired(dto.Name, "Department name");
            if (await _db.Departments.AnyAsync(d => d.Name == name && d.Id != dto.Id, cancellationToken))
            {
                throw new InvalidOperationException("A department with that name already exists.");
            }

            DepartmentEntity department;
            if (dto.Id > 0)
            {
                department = await _db.Departments.FirstOrDefaultAsync(d => d.Id == dto.Id, cancellationToken)
                    ?? throw new InvalidOperationException("The department was not found.");
            }
            else
            {
                department = new DepartmentEntity { CreatedAt = DateTime.UtcNow };
                _db.Departments.Add(department);
            }

            department.Name = name;
            department.Description = dto.Description?.Trim() ?? string.Empty;
            department.IsActive = dto.IsActive;
            department.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return new DepartmentDto
            {
                Id = department.Id,
                Name = department.Name,
                Description = department.Description,
                IsActive = department.IsActive
            };
        }

        public async Task<bool> ToggleDepartmentStatusAsync(int id, CancellationToken cancellationToken = default)
        {
            var department = await _db.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
            if (department == null)
            {
                return false;
            }

            department.IsActive = !department.IsActive;
            department.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<List<JobRoleDto>> GetJobRolesAsync(
            int? departmentId = null,
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var query = _db.JobRoles
                .AsNoTracking()
                .Include(r => r.Department)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(r => r.IsActive);
            }

            if (departmentId.HasValue)
            {
                query = query.Where(r => r.DepartmentId == departmentId.Value || r.DepartmentId == null);
            }

            return await query
                .OrderBy(r => r.Department!.Name)
                .ThenBy(r => r.Name)
                .Select(r => new JobRoleDto
                {
                    Id = r.Id,
                    DepartmentId = r.DepartmentId,
                    DepartmentName = r.Department != null ? r.Department.Name : "Shared",
                    Name = r.Name,
                    Description = r.Description,
                    IsActive = r.IsActive
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<JobRoleDto> SaveJobRoleAsync(
            JobRoleDto dto,
            CancellationToken cancellationToken = default)
        {
            var name = NormalizeRequired(dto.Name, "Job role name");
            if (await _db.JobRoles.AnyAsync(r => r.Name == name && r.Id != dto.Id, cancellationToken))
            {
                throw new InvalidOperationException("A job role with that name already exists.");
            }

            if (dto.DepartmentId.HasValue &&
                !await _db.Departments.AnyAsync(d => d.Id == dto.DepartmentId.Value, cancellationToken))
            {
                throw new InvalidOperationException("Select a valid department.");
            }

            JobRoleEntity role;
            if (dto.Id > 0)
            {
                role = await _db.JobRoles.FirstOrDefaultAsync(r => r.Id == dto.Id, cancellationToken)
                    ?? throw new InvalidOperationException("The job role was not found.");
            }
            else
            {
                role = new JobRoleEntity { CreatedAt = DateTime.UtcNow };
                _db.JobRoles.Add(role);
            }

            role.DepartmentId = dto.DepartmentId;
            role.Name = name;
            role.Description = dto.Description?.Trim() ?? string.Empty;
            role.IsActive = dto.IsActive;
            role.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            var departmentName = role.DepartmentId.HasValue
                ? await _db.Departments.Where(d => d.Id == role.DepartmentId.Value).Select(d => d.Name).FirstOrDefaultAsync(cancellationToken)
                : null;

            return new JobRoleDto
            {
                Id = role.Id,
                DepartmentId = role.DepartmentId,
                DepartmentName = departmentName ?? "Shared",
                Name = role.Name,
                Description = role.Description,
                IsActive = role.IsActive
            };
        }

        public async Task<bool> ToggleJobRoleStatusAsync(int id, CancellationToken cancellationToken = default)
        {
            var role = await _db.JobRoles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (role == null)
            {
                return false;
            }

            role.IsActive = !role.IsActive;
            role.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static string NormalizeRequired(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"{fieldName} is required.");
            }

            return value.Trim();
        }
    }
}
