using Manufacture.Models.DTOs;

namespace Manufacture.Services.Reports
{
    public class ReportAuthorizationService
    {
        public List<ReportDefinitionDto> VisibleReports(string role) =>
            ReportCatalog.ForRole(role);

        public bool CanAccess(string role, string key) =>
            string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase)
            || ReportCatalog.Find(key)?.AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase) == true;
    }
}
