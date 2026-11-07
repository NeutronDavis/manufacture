using Manufacture.Models.DTOs;

namespace Manufacture.Services
{
    /// <summary>
    /// Read-optimized snapshot of the RBAC matrix. Razor layouts need synchronous
    /// access checks on every request, so the database-backed matrix is cached
    /// here and refreshed after every save/reset.
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
                        access[Key(cell.RoleName, row.Module.ModuleKey)] = level;
                    }
                }
            }

            _access = access;
            IsLoaded = true;
        }

        public bool HasAccess(string role, string moduleKey)
        {
            if (IsSuperAdmin(role))
            {
                return true;
            }

            return GetLevel(role, moduleKey) is { IsNone: false };
        }

        public string GetPermissionLevel(string role, string moduleKey)
        {
            if (IsSuperAdmin(role))
            {
                return FullAccess.Name;
            }

            return GetLevel(role, moduleKey)?.Name ?? "None";
        }

        public bool CanView(string role, string moduleKey) => Capability(role, moduleKey, l => l.CanView);
        public bool CanCreate(string role, string moduleKey) => Capability(role, moduleKey, l => l.CanCreate);
        public bool CanEdit(string role, string moduleKey) => Capability(role, moduleKey, l => l.CanEdit);
        public bool CanDelete(string role, string moduleKey) => Capability(role, moduleKey, l => l.CanDelete);
        public bool CanApprove(string role, string moduleKey) => Capability(role, moduleKey, l => l.CanApprove);
        public bool CanPrint(string role, string moduleKey) => Capability(role, moduleKey, l => l.CanPrint);
        public bool CanManage(string role, string moduleKey) => Capability(role, moduleKey, l => l.CanManage);
        public bool CanRequest(string role, string moduleKey) => Capability(role, moduleKey, l => l.CanRequest);
        public bool CanIssue(string role, string moduleKey) => Capability(role, moduleKey, l => l.CanIssue);
        public bool CanDisburse(string role, string moduleKey) => Capability(role, moduleKey, l => l.CanDisburse);
        public bool CanBookOwn(string role, string moduleKey) => Capability(role, moduleKey, l => l.CanBookOwn);

        private bool Capability(string role, string moduleKey, Func<RbacPermissionLevelDto, bool> selector)
        {
            if (IsSuperAdmin(role))
            {
                return selector(FullAccess);
            }

            var level = GetLevel(role, moduleKey);
            return level != null && selector(level);
        }

        private RbacPermissionLevelDto? GetLevel(string role, string moduleKey)
        {
            return _access.TryGetValue(Key(role, moduleKey), out var level) ? level : null;
        }

        private static bool IsSuperAdmin(string role) =>
            string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

        private static string Key(string role, string moduleKey) => role + "|" + moduleKey;
    }
}
