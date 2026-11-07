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
                    Description = r.Description
                })
                .ToListAsync(cancellationToken);
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

            var pin = RequirePin(dto.PinCode);
            var password = RequirePassword(dto.Password);

            var user = new User
            {
                Name = dto.Name.Trim(),
                Email = email,
                RoleId = role.Id,
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

            var roleExists = await _db.Roles.AnyAsync(r => r.Id == dto.RoleId, cancellationToken);
            if (!roleExists)
            {
                throw new InvalidOperationException("Select a valid system role.");
            }

            user.Name = dto.Name.Trim();
            user.Email = email;
            user.RoleId = dto.RoleId;
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

        private static UserDto Map(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                RoleId = user.RoleId,
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
