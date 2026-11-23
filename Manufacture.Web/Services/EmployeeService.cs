using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Manufacture.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly ManufactureDbContext _db;

        public EmployeeService(ManufactureDbContext db)
        {
            _db = db;
        }

        public async Task<List<EmployeeDto>> GetAllAsync(bool includeInactive = true, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            var query = _db.Employees.AsNoTracking().AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(e => !e.IsDeleted);
            }

            if (!includeInactive)
            {
                query = query.Where(e => e.IsActive && e.EmploymentStatus != "Inactive");
            }

            var employees = await query
                .OrderBy(e => e.FullName)
                .ToListAsync(cancellationToken);

            return employees.Select(Map).ToList();
        }

        public async Task<EmployeeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var employee = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

            return employee == null ? null : Map(employee);
        }

        public async Task<List<string>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Employees
                .AsNoTracking()
                .Where(e => !e.IsDeleted && e.Department != "")
                .Select(e => e.Department)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync(cancellationToken);
        }

        public async Task<EmployeeDto> CreateAsync(EmployeeDto dto, CancellationToken cancellationToken = default)
        {
            var staffCode = dto.StaffCode.Trim();
            var email = dto.Email.Trim().ToLowerInvariant();
            var status = NormalizeStatus(dto.EmploymentStatus);

            if (await StaffCodeExistsAsync(staffCode, null, cancellationToken))
            {
                throw new InvalidOperationException("A staff member with that staff code already exists.");
            }

            if (!string.IsNullOrWhiteSpace(email) && await EmailExistsAsync(email, null, cancellationToken))
            {
                throw new InvalidOperationException("A staff member with that email address already exists.");
            }

            var department = await ResolveDepartmentAsync(dto.DepartmentId, dto.Department, cancellationToken);
            var jobRole = await ResolveJobRoleAsync(dto.JobRoleId, dto.Designation, cancellationToken);

            var employee = new Employee
            {
                FullName = dto.FullName.Trim(),
                StaffCode = staffCode,
                DepartmentId = department?.Id,
                JobRoleId = jobRole?.Id,
                Department = department?.Name ?? dto.Department.Trim(),
                Designation = jobRole?.Name ?? dto.Designation.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                Email = email,
                Address = dto.Address.Trim(),
                PassportUrl = dto.PassportUrl,
                BankName = dto.BankName.Trim(),
                BankAccountNumber = dto.BankAccountNumber.Trim(),
                EmploymentStatus = status,
                BaseSalary = dto.BaseSalary,
                EmergencyContactName = dto.EmergencyContactName.Trim(),
                EmergencyContactPhone = dto.EmergencyContactPhone.Trim(),
                EmergencyRelationship = dto.EmergencyRelationship.Trim(),
                GuarantorName = dto.GuarantorName.Trim(),
                GuarantorPhone = dto.GuarantorPhone.Trim(),
                GuarantorAddress = dto.GuarantorAddress.Trim(),
                GuarantorOccupation = dto.GuarantorOccupation.Trim(),
                EmploymentDate = DateOnly.FromDateTime(dto.EmploymentDate),
                IsActive = status != "Inactive",
                IsDeleted = false,
                TerminationReason = string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Employees.Add(employee);
            await _db.SaveChangesAsync(cancellationToken);
            return Map(employee);
        }

        public async Task<bool> UpdateAsync(EmployeeDto dto, CancellationToken cancellationToken = default)
        {
            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.Id == dto.Id && !e.IsDeleted, cancellationToken);
            if (employee == null)
            {
                return false;
            }

            var staffCode = dto.StaffCode.Trim();
            var email = dto.Email.Trim().ToLowerInvariant();
            var status = NormalizeStatus(dto.EmploymentStatus);

            if (await StaffCodeExistsAsync(staffCode, dto.Id, cancellationToken))
            {
                throw new InvalidOperationException("A staff member with that staff code already exists.");
            }

            if (!string.IsNullOrWhiteSpace(email) && await EmailExistsAsync(email, dto.Id, cancellationToken))
            {
                throw new InvalidOperationException("A staff member with that email address already exists.");
            }

            var department = await ResolveDepartmentAsync(dto.DepartmentId, dto.Department, cancellationToken);
            var jobRole = await ResolveJobRoleAsync(dto.JobRoleId, dto.Designation, cancellationToken);

            employee.FullName = dto.FullName.Trim();
            employee.StaffCode = staffCode;
            employee.DepartmentId = department?.Id;
            employee.JobRoleId = jobRole?.Id;
            employee.Department = department?.Name ?? dto.Department.Trim();
            employee.Designation = jobRole?.Name ?? dto.Designation.Trim();
            employee.PhoneNumber = dto.PhoneNumber.Trim();
            employee.Email = email;
            employee.Address = dto.Address.Trim();
            employee.PassportUrl = string.IsNullOrWhiteSpace(dto.PassportUrl) ? employee.PassportUrl : dto.PassportUrl;
            employee.BankName = dto.BankName.Trim();
            employee.BankAccountNumber = dto.BankAccountNumber.Trim();
            employee.EmploymentStatus = status;
            employee.BaseSalary = dto.BaseSalary;
            employee.EmergencyContactName = dto.EmergencyContactName.Trim();
            employee.EmergencyContactPhone = dto.EmergencyContactPhone.Trim();
            employee.EmergencyRelationship = dto.EmergencyRelationship.Trim();
            employee.GuarantorName = dto.GuarantorName.Trim();
            employee.GuarantorPhone = dto.GuarantorPhone.Trim();
            employee.GuarantorAddress = dto.GuarantorAddress.Trim();
            employee.GuarantorOccupation = dto.GuarantorOccupation.Trim();
            employee.EmploymentDate = DateOnly.FromDateTime(dto.EmploymentDate);
            employee.IsActive = status != "Inactive";
            employee.UpdatedAt = DateTime.UtcNow;

            if (!employee.IsActive)
            {
                await UnassignVehiclesAsync(employee.Id, cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
        {
            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
            if (employee == null)
            {
                return false;
            }

            if (!isActive)
            {
                await UnassignVehiclesAsync(employee.Id, cancellationToken);
            }

            employee.IsActive = isActive;
            employee.EmploymentStatus = isActive ? "Active" : "Inactive";
            employee.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> ChangeStatusAsync(int id, string status, CancellationToken cancellationToken = default)
        {
            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
            if (employee == null)
            {
                return false;
            }

            var normalizedStatus = NormalizeStatus(status);
            if (normalizedStatus == "Inactive")
            {
                await UnassignVehiclesAsync(employee.Id, cancellationToken);
            }

            employee.EmploymentStatus = normalizedStatus;
            employee.IsActive = normalizedStatus != "Inactive";
            employee.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<EmployeeDependencyDto> GetDependenciesAsync(int id, CancellationToken cancellationToken = default)
        {
            return new EmployeeDependencyDto
            {
                PayrollRecords = await _db.PayrollRecords.CountAsync(p => p.EmployeeId == id, cancellationToken),
                PayrollDeductionLines = await _db.PayrollDeductionLines.CountAsync(p => p.EmployeeId == id, cancellationToken),
                SalaryAdvances = await _db.SalaryAdvances.CountAsync(a => a.EmployeeId == id, cancellationToken),
                FuelLogs = await _db.FuelLogs.CountAsync(f => f.EmployeeId == id, cancellationToken),
                CharterBookings = await _db.CharterBookings.CountAsync(c => c.DriverEmployeeId == id, cancellationToken),
                VehicleAssignments = await _db.Vehicles.CountAsync(v => v.AssignedEmployeeId == id, cancellationToken)
            };
        }

        public async Task<bool> ArchiveAsync(int id, string reason, int? archivedBy = null, CancellationToken cancellationToken = default)
        {
            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (employee == null)
            {
                return false;
            }

            if (employee.IsDeleted)
            {
                return true;
            }

            await UnassignVehiclesAsync(employee.Id, cancellationToken);

            employee.IsDeleted = true;
            employee.IsActive = false;
            employee.EmploymentStatus = "Inactive";
            employee.DeletedAt = DateTime.UtcNow;
            employee.DeletedBy = archivedBy;
            employee.TerminationDate = DateOnly.FromDateTime(DateTime.UtcNow);
            employee.TerminationReason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            employee.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default)
        {
            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.Id == id && e.IsDeleted, cancellationToken);
            if (employee == null)
            {
                return false;
            }

            employee.IsDeleted = false;
            employee.DeletedAt = null;
            employee.DeletedBy = null;
            employee.TerminationDate = null;
            employee.TerminationReason = string.Empty;
            employee.EmploymentStatus = "Active";
            employee.IsActive = true;
            employee.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException(
                    "This employee cannot be restored because their staff code or email address is already used by an active employee.",
                    ex);
            }

            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (employee == null)
            {
                return false;
            }

            var dependencies = await GetDependenciesAsync(id, cancellationToken);
            if (!dependencies.CanHardDelete)
            {
                return false;
            }

            _db.Employees.Remove(employee);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public Task<bool> StaffCodeExistsAsync(string staffCode, int? excludeEmployeeId = null, CancellationToken cancellationToken = default)
        {
            var normalized = staffCode.Trim();
            return _db.Employees.AnyAsync(
                e => !e.IsDeleted
                     && e.StaffCode == normalized
                     && (!excludeEmployeeId.HasValue || e.Id != excludeEmployeeId.Value),
                cancellationToken);
        }

        public Task<bool> EmailExistsAsync(string email, int? excludeEmployeeId = null, CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return _db.Employees.AnyAsync(
                e => !e.IsDeleted
                     && e.Email == normalized
                     && (!excludeEmployeeId.HasValue || e.Id != excludeEmployeeId.Value),
                cancellationToken);
        }

        private async Task UnassignVehiclesAsync(int employeeId, CancellationToken cancellationToken)
        {
            var vehicles = await _db.Vehicles
                .Where(v => v.AssignedEmployeeId == employeeId)
                .ToListAsync(cancellationToken);

            foreach (var vehicle in vehicles)
            {
                vehicle.AssignedEmployeeId = null;
            }
        }

        private static EmployeeDto Map(Employee employee)
        {
            return new EmployeeDto
            {
                Id = employee.Id,
                FullName = employee.FullName,
                StaffCode = employee.StaffCode,
                DepartmentId = employee.DepartmentId,
                JobRoleId = employee.JobRoleId,
                Department = employee.Department,
                Designation = employee.Designation,
                PhoneNumber = employee.PhoneNumber,
                Email = employee.Email,
                Address = employee.Address,
                PassportUrl = employee.PassportUrl ?? string.Empty,
                BankName = employee.BankName,
                BankAccountNumber = employee.BankAccountNumber,
                EmploymentStatus = employee.EmploymentStatus,
                BaseSalary = employee.BaseSalary,
                EmergencyContactName = employee.EmergencyContactName,
                EmergencyContactPhone = employee.EmergencyContactPhone,
                EmergencyRelationship = employee.EmergencyRelationship,
                GuarantorName = employee.GuarantorName,
                GuarantorPhone = employee.GuarantorPhone,
                GuarantorAddress = employee.GuarantorAddress,
                GuarantorOccupation = employee.GuarantorOccupation,
                EmploymentDate = employee.EmploymentDate.ToDateTime(TimeOnly.MinValue),
                IsActive = employee.IsActive,
                IsDeleted = employee.IsDeleted,
                DeletedAt = employee.DeletedAt,
                DeletedBy = employee.DeletedBy,
                TerminationDate = employee.TerminationDate?.ToDateTime(TimeOnly.MinValue),
                TerminationReason = employee.TerminationReason
            };
        }

        private async Task<Manufacture.Models.Entities.Department?> ResolveDepartmentAsync(
            int? departmentId,
            string? departmentName,
            CancellationToken cancellationToken)
        {
            if (departmentId.HasValue && departmentId.Value > 0)
            {
                var byId = await _db.Departments
                    .FirstOrDefaultAsync(d => d.Id == departmentId.Value && d.IsActive, cancellationToken);
                if (byId == null)
                {
                    throw new InvalidOperationException("Select a valid active department.");
                }
                return byId;
            }

            if (string.IsNullOrWhiteSpace(departmentName))
            {
                return null;
            }

            return await _db.Departments
                .FirstOrDefaultAsync(d => d.Name == departmentName.Trim() && d.IsActive, cancellationToken);
        }

        private async Task<Manufacture.Models.Entities.JobRole?> ResolveJobRoleAsync(
            int? jobRoleId,
            string? roleName,
            CancellationToken cancellationToken)
        {
            if (jobRoleId.HasValue && jobRoleId.Value > 0)
            {
                var byId = await _db.JobRoles
                    .FirstOrDefaultAsync(r => r.Id == jobRoleId.Value && r.IsActive, cancellationToken);
                if (byId == null)
                {
                    throw new InvalidOperationException("Select a valid active job role.");
                }
                return byId;
            }

            if (string.IsNullOrWhiteSpace(roleName))
            {
                return null;
            }

            return await _db.JobRoles
                .FirstOrDefaultAsync(r => r.Name == roleName.Trim() && r.IsActive, cancellationToken);
        }

        private static string NormalizeStatus(string status)
        {
            return status switch
            {
                "OnLeave" => "OnLeave",
                "Inactive" => "Inactive",
                _ => "Active"
            };
        }
    }
}
