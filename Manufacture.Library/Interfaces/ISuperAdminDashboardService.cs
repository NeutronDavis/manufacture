using Manufacture.Models.DTOs;

namespace Manufacture.Library.Interfaces
{
    /// <summary>
    /// Database-backed executive dashboard for the SuperAdmin control centre.
    /// Reads the operational route sheets, production runs, factory defects and
    /// customer returns rather than synthesising figures.
    /// </summary>
    public interface ISuperAdminDashboardService
    {
        ExecutiveOverviewDto GetOverview(DateTime? date = null);
        DashboardOverviewApiResponse GetOverviewApi(string? timeframe = null, DateTime? date = null);
    }
}
