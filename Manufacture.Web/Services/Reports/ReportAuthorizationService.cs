using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;

namespace Manufacture.Services.Reports
{
    /// <summary>
    /// Report access checks keyed on dbo.Roles.Id. A role rename must never
    /// change which reports a user can see.
    /// </summary>
    public class ReportAuthorizationService
    {
        public List<ReportDefinitionDto> VisibleReports(int roleId) =>
            ReportCatalog.ForRole(roleId);

        public bool CanAccess(int roleId, string key) =>
            roleId == (int)UserRole.SuperAdmin
            || ReportCatalog.Find(key)?.AllowedRoleIds.Contains(roleId) == true;
    }
}
