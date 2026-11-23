using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Manufacture.Services
{
    /// <summary>
    /// EF Core implementation of user management. Replaces the in-memory
    /// MockUserService for authentication and the /UserManagement pages.
    /// <para>
    /// All queries use EF Core LINQ, which composes parameterized SQL. User
    /// input is never concatenated into a command, so values such as
    /// <c>' OR 1=1 --</c> are treated as literal data and cannot alter the query.
    /// </para>
    /// </summary>
    public class UserService : IUserService
    {
        private const string SuperAdminRole = "SuperAdmin";

        private readonly ManufactureDbContext _db;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserService(ManufactureDbContext db, IPasswordHasher<User> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        public async Task<List<UserDto>> GetAllAsync(bool includeInactive = true, CancellationToken cancellationToken = default)
        {
            var query = _db.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Include(u => u.Employee)
                .Include(u => u.Customer)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(u => u.IsActive);
            }

            var users = await query
                .OrderBy(u => u.Name)
                .ToListAsync(cancellationToken);

            return users.Select(Map).ToList();
        }

        public async Task<UserDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Include(u => u.Employee)
                .Include(u => u.Customer)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            return user == null ? null : Map(user);
        }

        public async Task<List<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Roles
                .AsNoTracking()
                .OrderBy(r => r.Id)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    DisplayName = r.DisplayName,
                    Description = r.Description,
                    IsSystemRole = r.IsSystemRole
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<RoleDto> SaveRoleAsync(RoleDto dto, CancellationToken cancellationToken = default)
        {
            var name = dto.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Role name is required.");
            }

            if (await _db.Roles.AnyAsync(r => r.Name == name && r.Id != dto.Id, cancellationToken))
            {
                throw new InvalidOperationException("A role with that name already exists.");
            }

            Role role;
            if (dto.Id > 0)
            {
                role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == dto.Id, cancellationToken)
                    ?? throw new InvalidOperationException("The role was not found.");

                if (role.IsSystemRole && !string.Equals(role.Name, name, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("System role names cannot be changed.");
                }
            }
            else
            {
                role = new Role
                {
                    IsSystemRole = false,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Roles.Add(role);
            }

            role.Name = name;
            role.DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? name : dto.DisplayName.Trim();
            role.Description = dto.Description?.Trim() ?? string.Empty;
            role.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                DisplayName = role.DisplayName,
                Description = role.Description,
                IsSystemRole = role.IsSystemRole
            };
        }

        public async Task<bool> DeleteRoleAsync(int id, CancellationToken cancellationToken = default)
        {
            var role = await _db.Roles
                .Include(r => r.Users)
                .Include(r => r.RbacRolePermissions)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (role == null)
            {
                return false;
            }

            if (role.IsSystemRole)
            {
                throw new InvalidOperationException("System roles cannot be deleted. Edit the display name or description instead.");
            }

            if (role.Users.Any())
            {
                throw new InvalidOperationException("This role cannot be deleted while user accounts are assigned to it.");
            }

            if (role.RbacRolePermissions.Any())
            {
                _db.RbacRolePermissions.RemoveRange(role.RbacRolePermissions);
            }

            _db.Roles.Remove(role);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<UserDto> CreateAsync(UserDto dto, CancellationToken cancellationToken = default)
        {
            var email = NormalizeEmail(dto.Email);
            if (await EmailExistsAsync(email, null, cancellationToken))
            {
                throw new InvalidOperationException("A user with that email address already exists.");
            }

            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == dto.RoleId, cancellationToken)
                ?? throw new InvalidOperationException("Select a valid system role.");

            var customerId = await ValidateCustomerLinkAsync(dto.CustomerId, role.Name, cancellationToken);
            var employee = await ValidateEmployeeLinkAsync(dto.EmployeeId, null, cancellationToken);
            var pin = RequirePin(dto.PinCode);
            var password = RequirePassword(dto.Password);

            var user = new User
            {
                Name = dto.Name.Trim(),
                Email = email,
                RoleId = role.Id,
                EmployeeId = employee.Id,
                CustomerId = customerId,
                MustChangePassword = true,
                PosTerminalCode = string.IsNullOrWhiteSpace(dto.PosTerminalId) ? null : dto.PosTerminalId.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim(),
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            user.PinCodeHash = _passwordHasher.HashPassword(user, pin);
            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);

            user.Role = role;
            user.Employee = employee;
            return Map(user);
        }

        public async Task<bool> UpdateAsync(UserDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == dto.Id, cancellationToken);
            if (user == null)
            {
                return false;
            }

            var email = NormalizeEmail(dto.Email);
            if (await EmailExistsAsync(email, dto.Id, cancellationToken))
            {
                throw new InvalidOperationException("A user with that email address already exists.");
            }

            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == dto.RoleId, cancellationToken);
            if (role == null)
            {
                throw new InvalidOperationException("Select a valid system role.");
            }

            var customerId = await ValidateCustomerLinkAsync(dto.CustomerId, role.Name, cancellationToken);
            var employee = await ValidateEmployeeLinkAsync(dto.EmployeeId, dto.Id, cancellationToken);

            user.Name = dto.Name.Trim();
            user.Email = email;
            user.RoleId = dto.RoleId;
            user.EmployeeId = employee.Id;
            user.Employee = employee;
            user.CustomerId = customerId;
            user.PosTerminalCode = string.IsNullOrWhiteSpace(dto.PosTerminalId) ? null : dto.PosTerminalId.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
            user.IsActive = dto.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(dto.PinCode))
            {
                user.PinCodeHash = _passwordHasher.HashPassword(user, RequirePin(dto.PinCode));
            }

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, RequirePassword(dto.Password));
            }

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (user == null)
            {
                return false;
            }

            if (!isActive && user.Role.Name == SuperAdminRole)
            {
                var otherActiveAdmins = await _db.Users
                    .Include(u => u.Role)
                    .CountAsync(u => u.Id != id && u.IsActive && u.Role.Name == SuperAdminRole, cancellationToken);

                if (otherActiveAdmins == 0)
                {
                    throw new InvalidOperationException("Cannot deactivate the last active Super Admin.");
                }
            }

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (user == null)
            {
                return false;
            }

            if (user.Role.Name == SuperAdminRole)
            {
                var otherActiveAdmins = await _db.Users
                    .Include(u => u.Role)
                    .CountAsync(u => u.Id != id && u.IsActive && u.Role.Name == SuperAdminRole, cancellationToken);

                if (otherActiveAdmins == 0)
                {
                    throw new InvalidOperationException("Cannot delete the last active Super Admin.");
                }
            }

            _db.Users.Remove(user);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<UserDto?> AuthenticatePasswordAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = NormalizeEmail(email);
            var user = await _db.Users
                .Include(u => u.Role)
                .Include(u => u.Employee)
                .Include(u => u.Customer)
                .FirstOrDefaultAsync(u => u.IsActive && u.Email == normalizedEmail, cancellationToken);

            if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return null;
            }

            PasswordVerificationResult result;
            try
            {
                result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            }
            catch (FormatException)
            {
                return null;
            }

            if (result == PasswordVerificationResult.Failed)
            {
                return null;
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Map(user);
        }

        public async Task<UserDto?> AuthenticatePinAsync(string pinCode, CancellationToken cancellationToken = default)
        {
            var users = await _db.Users
                .Include(u => u.Role)
                .Include(u => u.Employee)
                .Include(u => u.Customer)
                .Where(u => u.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                if (string.IsNullOrWhiteSpace(user.PinCodeHash))
                {
                    continue;
                }

                PasswordVerificationResult result;
                try
                {
                    result = _passwordHasher.VerifyHashedPassword(user, user.PinCodeHash, pinCode);
                }
                catch (FormatException)
                {
                    continue;
                }

                if (result != PasswordVerificationResult.Failed)
                {
                    user.LastLoginAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync(cancellationToken);
                    return Map(user);
                }
            }

            return null;
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = NormalizeEmail(email);
            return await _db.Users.AnyAsync(
                u => u.Email == normalizedEmail && (!excludeUserId.HasValue || u.Id != excludeUserId.Value),
                cancellationToken);
        }

        private async Task<Employee> ValidateEmployeeLinkAsync(
            int? employeeId,
            int? excludeUserId,
            CancellationToken cancellationToken)
        {
            if (!employeeId.HasValue || employeeId.Value <= 0)
            {
                throw new InvalidOperationException("Select the employee record for this user.");
            }

            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.Id == employeeId.Value && !e.IsDeleted && e.IsActive, cancellationToken)
                ?? throw new InvalidOperationException("Select an active employee record.");

            var alreadyLinked = await _db.Users.AnyAsync(
                u => u.EmployeeId == employeeId.Value && (!excludeUserId.HasValue || u.Id != excludeUserId.Value),
                cancellationToken);

            if (alreadyLinked)
            {
                throw new InvalidOperationException("That employee already has a platform user account.");
            }

            return employee;
        }

        private async Task<int?> ValidateCustomerLinkAsync(
            int? customerId,
            string roleName,
            CancellationToken cancellationToken)
        {
            if (!customerId.HasValue || customerId.Value <= 0)
            {
                if (string.Equals(roleName, "Customer", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Select the customer account for a Customer portal user.");
                }

                return null;
            }

            var customerExists = await _db.Customers.AnyAsync(c => c.Id == customerId.Value && c.IsActive, cancellationToken);
            if (!customerExists)
            {
                throw new InvalidOperationException("Select a valid active customer account.");
            }

            return customerId.Value;
        }

        private static UserDto Map(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                RoleId = user.RoleId,
                EmployeeId = user.EmployeeId,
                EmployeeName = user.Employee?.FullName,
                EmployeeDepartment = user.Employee?.Department,
                EmployeeDesignation = user.Employee?.Designation,
                CustomerId = user.CustomerId,
                CustomerName = user.Customer?.Name,
                Role = user.Role?.Name ?? user.RoleId.ToString(),
                RoleDisplayName = user.Role?.DisplayName ?? user.Role?.Name ?? string.Empty,
                PosTerminalId = user.PosTerminalCode,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                HasPin = !string.IsNullOrWhiteSpace(user.PinCodeHash),
                HasPassword = !string.IsNullOrWhiteSpace(user.PasswordHash),
                CreatedAt = user.CreatedAt
            };
        }

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        private static string RequirePin(string? pinCode)
        {
            var pin = pinCode?.Trim() ?? string.Empty;
            if (pin.Length != 6 || !pin.All(char.IsDigit))
            {
                throw new InvalidOperationException("PIN must be exactly 6 digits.");
            }

            return pin;
        }

        private static string RequirePassword(string? password)
        {
            var value = password ?? string.Empty;
            if (value.Length < 8)
            {
                throw new InvalidOperationException("Password must be at least 8 characters.");
            }

            return value;
        }
    }
}
