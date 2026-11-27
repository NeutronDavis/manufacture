using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;

namespace Manufacture.Services
{
    /// <summary>
    /// Read-optimized snapshot of the RBAC matrix. Razor layouts need synchronous
    /// access checks on every request, so the database-backed matrix is cached
    /// here and refreshed after every save/reset. Keys are role IDs (dbo.Roles.Id),
    /// not role names, so renaming a role never changes behaviour.
    /// </summary>
    public class RbacCache
    {
        private static readonly RbacPermissionLevelDto FullAccess = new()
        {
            Id = 0,
            Name = "Full Access",
            Rank = int.MaxValue,
            CanView = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = true,
            CanApprove = true,
            CanPrint = true,
            CanManage = true,
            CanRequest = true,
            CanIssue = true,
            CanDisburse = true,
            CanBookOwn = true,
            SortOrder = int.MaxValue
        };

        private IReadOnlyDictionary<string, RbacPermissionLevelDto> _access =
            new Dictionary<string, RbacPermissionLevelDto>(StringComparer.OrdinalIgnoreCase);

        public bool IsLoaded { get; private set; }

        public void SetSnapshot(RbacMatrixDto matrix)
        {
            var access = new Dictionary<string, RbacPermissionLevelDto>(StringComparer.OrdinalIgnoreCase);
            var levels = matrix.PermissionLevels.ToDictionary(l => l.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var row in matrix.Rows)
            {
                foreach (var cell in row.Cells)
                {
                    if (levels.TryGetValue(cell.PermissionLevelName, out var level))
                    {
                        access[Key(cell.RoleId, row.Module.ModuleKey)] = level;
                    }
                }
            }

            _access = access;
            IsLoaded = true;
        }

        public bool HasAccess(int roleId, string moduleKey)
        {
            if (IsSuperAdmin(roleId))
            {
                return true;
            }

            return GetLevel(roleId, moduleKey) is { IsNone: false };
        }

        public string GetPermissionLevel(int roleId, string moduleKey)
        {
            if (IsSuperAdmin(roleId))
            {
                return FullAccess.Name;
            }

            return GetLevel(roleId, moduleKey)?.Name ?? "None";
        }

        public bool CanView(int roleId, string moduleKey) => Capability(roleId, moduleKey, l => l.CanView);
        public bool CanCreate(int roleId, string moduleKey) => Capability(roleId, moduleKey, l => l.CanCreate);
        public bool CanEdit(int roleId, string moduleKey) => Capability(roleId, moduleKey, l => l.CanEdit);
        public bool CanDelete(int roleId, string moduleKey) => Capability(roleId, moduleKey, l => l.CanDelete);
        public bool CanApprove(int roleId, string moduleKey) => Capability(roleId, moduleKey, l => l.CanApprove);
        public bool CanPrint(int roleId, string moduleKey) => Capability(roleId, moduleKey, l => l.CanPrint);
        public bool CanManage(int roleId, string moduleKey) => Capability(roleId, moduleKey, l => l.CanManage);
        public bool CanRequest(int roleId, string moduleKey) => Capability(roleId, moduleKey, l => l.CanRequest);
        public bool CanIssue(int roleId, string moduleKey) => Capability(roleId, moduleKey, l => l.CanIssue);
        public bool CanDisburse(int roleId, string moduleKey) => Capability(roleId, moduleKey, l => l.CanDisburse);
        public bool CanBookOwn(int roleId, string moduleKey) => Capability(roleId, moduleKey, l => l.CanBookOwn);

        private bool Capability(int roleId, string moduleKey, Func<RbacPermissionLevelDto, bool> selector)
        {
            if (IsSuperAdmin(roleId))
            {
                return selector(FullAccess);
            }

            var level = GetLevel(roleId, moduleKey);
            return level != null && selector(level);
        }

        private RbacPermissionLevelDto? GetLevel(int roleId, string moduleKey)
        {
            return _access.TryGetValue(Key(roleId, moduleKey), out var level) ? level : null;
        }

        private static bool IsSuperAdmin(int roleId) => roleId == (int)UserRole.SuperAdmin;

        private static string Key(int roleId, string moduleKey) => roleId + "|" + moduleKey;
    }
}
