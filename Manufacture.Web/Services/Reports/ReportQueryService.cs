using Manufacture.Library.Data;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Manufacture.Services.Reports
{
    public class ReportQueryService
    {
        private readonly ManufactureDbContext _db;
        private readonly ReportAuthorizationService _authorization;

        public ReportQueryService(ManufactureDbContext db, ReportAuthorizationService authorization)
        {
            _db = db;
            _authorization = authorization;
        }

        public async Task<ReportViewResultDto> RunAsync(
            string key,
            ReportFilterDto filters,
            int roleId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var definition = ReportCatalog.Find(key)
                ?? throw new InvalidOperationException("Report not found.");

            if (!_authorization.CanAccess(roleId, key))
            {
                throw new UnauthorizedAccessException("You do not have access to this report.");
            }

            NormalizeDates(filters, definition);
            filters.Page = Math.Max(1, filters.Page);
            filters.PageSize = Math.Clamp(filters.PageSize, 10, 200);

            int? salesRepId = null;
            int? customerId = null;

            if (roleId == (int)UserRole.SalesRep && userId > 0)
            {
                salesRepId = await _db.SalesReps.AsNoTracking()
                    .Where(rep => rep.UserId == userId && rep.IsActive)
                    .Select(rep => (int?)rep.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (roleId == (int)UserRole.Customer && userId > 0)
            {
                customerId = await _db.Users.AsNoTracking()
                    .Where(user => user.Id == userId)
                    .Select(user => user.CustomerId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var context = new ReportQueryContext
            {
                Definition = definition,
                Filters = filters,
                RoleId = roleId,
                UserId = userId,
                CurrentSalesRepId = salesRepId,
                CurrentCustomerId = customerId
            };

            return await ReportQueries.ExecuteAsync(_db, context, cancellationToken);
        }

        public ReportFilterDto CreateDefaultFilters(ReportDefinitionDto definition)
        {
            var today = DateTime.UtcNow.Date;
            return new ReportFilterDto
            {
                StartDate = today.AddDays(-(Math.Max(1, definition.DefaultDays) - 1)),
                EndDate = today,
                Page = 1,
                PageSize = 50
            };
        }

        private static void NormalizeDates(ReportFilterDto filters, ReportDefinitionDto definition)
        {
            var today = DateTime.UtcNow.Date;
            if (filters.StartDate == default && filters.EndDate == default)
            {
                filters.EndDate = today;
                filters.StartDate = today.AddDays(-(Math.Max(1, definition.DefaultDays) - 1));
            }

            filters.StartDate = filters.StartDate.Date;
            filters.EndDate = filters.EndDate.Date;
            if (filters.EndDate < filters.StartDate)
            {
                (filters.StartDate, filters.EndDate) = (filters.EndDate, filters.StartDate);
            }

            if (filters.EndDate > today) filters.EndDate = today;
            if (filters.StartDate > filters.EndDate) filters.StartDate = filters.EndDate;
        }
    }
}
