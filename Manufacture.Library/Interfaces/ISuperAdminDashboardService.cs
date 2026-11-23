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

        /// <summary>
        /// Source records behind one lifecycle KPI for a timeframe, product scope
        /// and anchor date. Powers the dashboard record drawer.
        /// </summary>
        KpiDetailDto GetKpiDetail(string? metric, string? timeframe, string? scopeKey, DateTime? date = null);
    }
}
