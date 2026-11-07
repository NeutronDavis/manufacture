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

        public async Task<List<EmployeeDto>> GetAllAsync(bool includeInactive = true, CancellationToken cancellationToken = default)
        {
            var query = _db.Employees.AsNoTracking().AsQueryable();
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
                .Where(e => e.Department != "")
                .Select(e => e.Department)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync(cancellationToken);
        }

        public async Task<EmployeeDto> CreateAsync(EmployeeDto dto, CancellationToken cancellationToken = default)
        {
            var staffCode = dto.StaffCode.Trim();
            var email = dto.Email.Trim().ToLowerInvariant();

            if (await StaffCodeExistsAsync(staffCode, null, cancellationToken))
            {
                throw new InvalidOperationException("A staff member with that staff code already exists.");
            }

            if (!string.IsNullOrWhiteSpace(email) && await EmailExistsAsync(email, null, cancellationToken))
            {
                throw new InvalidOperationException("A staff member with that email address already exists.");
            }

            var employee = new Employee
            {
                FullName = dto.FullName.Trim(),
                StaffCode = staffCode,
                Department = dto.Department.Trim(),
                Designation = dto.Designation.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                Email = email,
                Address = dto.Address.Trim(),
                BankName = dto.BankName.Trim(),
                BankAccountNumber = dto.BankAccountNumber.Trim(),
                EmploymentStatus = NormalizeStatus(dto.EmploymentStatus),
                BaseSalary = dto.BaseSalary,
                EmergencyContactName = dto.EmergencyContactName.Trim(),
                EmergencyContactPhone = dto.EmergencyContactPhone.Trim(),
                EmergencyRelationship = dto.EmergencyRelationship.Trim(),
                GuarantorName = dto.GuarantorName.Trim(),
                GuarantorPhone = dto.GuarantorPhone.Trim(),
                GuarantorAddress = dto.GuarantorAddress.Trim(),
                GuarantorOccupation = dto.GuarantorOccupation.Trim(),
                EmploymentDate = DateOnly.FromDateTime(dto.EmploymentDate),
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Employees.Add(employee);
            await _db.SaveChangesAsync(cancellationToken);
            return Map(employee);
        }

        public async Task<bool> UpdateAsync(EmployeeDto dto, CancellationToken cancellationToken = default)
        {
            var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == dto.Id, cancellationToken);
            if (employee == null)
            {
                return false;
            }

            var staffCode = dto.StaffCode.Trim();
            var email = dto.Email.Trim().ToLowerInvariant();

            if (await StaffCodeExistsAsync(staffCode, dto.Id, cancellationToken))
            {
                throw new InvalidOperationException("A staff member with that staff code already exists.");
            }

            if (!string.IsNullOrWhiteSpace(email) && await EmailExistsAsync(email, dto.Id, cancellationToken))
            {
                throw new InvalidOperationException("A staff member with that email address already exists.");
            }

            employee.FullName = dto.FullName.Trim();
            employee.StaffCode = staffCode;
            employee.Department = dto.Department.Trim();
            employee.Designation = dto.Designation.Trim();
            employee.PhoneNumber = dto.PhoneNumber.Trim();
            employee.Email = email;
            employee.Address = dto.Address.Trim();
            employee.BankName = dto.BankName.Trim();
            employee.BankAccountNumber = dto.BankAccountNumber.Trim();
            employee.EmploymentStatus = NormalizeStatus(dto.EmploymentStatus);
            employee.BaseSalary = dto.BaseSalary;
            employee.EmergencyContactName = dto.EmergencyContactName.Trim();
            employee.EmergencyContactPhone = dto.EmergencyContactPhone.Trim();
            employee.EmergencyRelationship = dto.EmergencyRelationship.Trim();
            employee.GuarantorName = dto.GuarantorName.Trim();
            employee.GuarantorPhone = dto.GuarantorPhone.Trim();
            employee.GuarantorAddress = dto.GuarantorAddress.Trim();
            employee.GuarantorOccupation = dto.GuarantorOccupation.Trim();
            employee.EmploymentDate = DateOnly.FromDateTime(dto.EmploymentDate);
            employee.IsActive = dto.IsActive;
            employee.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
        {
            var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (employee == null)
            {
                return false;
            }

            employee.IsActive = isActive;
            employee.EmploymentStatus = isActive ? "Active" : "Inactive";
            employee.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public Task<bool> StaffCodeExistsAsync(string staffCode, int? excludeEmployeeId = null, CancellationToken cancellationToken = default)
        {
            var normalized = staffCode.Trim();
            return _db.Employees.AnyAsync(
                e => e.StaffCode == normalized && (!excludeEmployeeId.HasValue || e.Id != excludeEmployeeId.Value),
                cancellationToken);
        }

        public Task<bool> EmailExistsAsync(string email, int? excludeEmployeeId = null, CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return _db.Employees.AnyAsync(
                e => e.Email == normalized && (!excludeEmployeeId.HasValue || e.Id != excludeEmployeeId.Value),
                cancellationToken);
        }

        private static EmployeeDto Map(Employee employee)
        {
            return new EmployeeDto
            {
                Id = employee.Id,
                FullName = employee.FullName,
                StaffCode = employee.StaffCode,
                Department = employee.Department,
                Designation = employee.Designation,
                PhoneNumber = employee.PhoneNumber,
                Email = employee.Email,
                Address = employee.Address,
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
                IsActive = employee.IsActive
            };
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
