using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Manufacture.Services.Reports;

namespace Manufacture.Pages.Reports
{
    public class IndexModel : PageModel
    {
        private readonly ReportQueryService _reports;
        private readonly ReportAuthorizationService _authorization;
        private readonly IProductionService _productionService;
        private readonly ISalesService _salesService;

        public IndexModel(
            ReportQueryService reports,
            ReportAuthorizationService authorization,
            IProductionService productionService,
            ISalesService salesService)
        {
            _reports = reports;
            _authorization = authorization;
            _productionService = productionService;
            _salesService = salesService;
        }

        public string ActiveRole { get; set; } = "SuperAdmin";
        public int UserId { get; set; }
        public List<ReportDefinitionDto> VisibleReports { get; set; } = new();
        public ReportDefinitionDto? SelectedReport { get; set; }
        public ReportViewResultDto? Result { get; set; }
        public ReportFilterDto Filters { get; set; } = new();
        public List<ProductionCategory> Categories { get; set; } = new();
        public List<SalesRepProfile> SalesReps { get; set; } = new();

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalPages => Result is null ? 1 : Math.Max(1, (int)Math.Ceiling(Result.TotalRows / (double)PageSize));
        public List<Dictionary<string, object?>> PagedRows => Result is null
            ? new List<Dictionary<string, object?>>()
            : Result.Rows.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

        public async Task<IActionResult> OnGetAsync(
            string? report,
            DateTime? from,
            DateTime? to,
            int? categoryId,
            int? salesRepId,
            string? status,
            string? search,
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            ActiveRole = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            UserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            VisibleReports = _authorization.VisibleReports(ActiveRole);

            if (string.IsNullOrWhiteSpace(report))
            {
                return Page();
            }

            SelectedReport = ReportCatalog.Find(report);
            if (SelectedReport is null || !_authorization.CanAccess(ActiveRole, report))
            {
                TempData["ErrorMessage"] = "That report is not available for your role.";
                return RedirectToPage("/Reports/Index");
            }

            Filters = new ReportFilterDto
            {
                StartDate = from ?? DateTime.UtcNow.Date.AddDays(-(Math.Max(1, SelectedReport.DefaultDays) - 1)),
                EndDate = to ?? DateTime.UtcNow.Date,
                CategoryId = categoryId,
                SalesRepId = salesRepId,
                Status = status,
                Search = search,
                Page = Math.Max(1, page),
                PageSize = 50
            };

            PageNumber = Math.Max(1, page);
            Categories = _productionService.GetCategories().OrderBy(category => category.Name).ToList();
            SalesReps = await _salesService.GetSalesRepsAsync(cancellationToken: cancellationToken);

            try
            {
                Result = await _reports.RunAsync(report, Filters, ActiveRole, UserId, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToPage("/Reports/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnGetExportCsvAsync(
            string report,
            DateTime? from,
            DateTime? to,
            int? categoryId,
            int? salesRepId,
            string? status,
            string? search,
            CancellationToken cancellationToken = default)
        {
            var result = await LoadForExportAsync(report, from, to, categoryId, salesRepId, status, search, cancellationToken);
            if (result is null) return Forbid();
            return File(GenericReportExportService.BuildCsv(result), "text/csv; charset=utf-8", GenericReportExportService.CsvFileName(result));
        }

        public async Task<IActionResult> OnGetExportPdfAsync(
            string report,
            DateTime? from,
            DateTime? to,
            int? categoryId,
            int? salesRepId,
            string? status,
            string? search,
            CancellationToken cancellationToken = default)
        {
            var result = await LoadForExportAsync(report, from, to, categoryId, salesRepId, status, search, cancellationToken);
            if (result is null) return Forbid();
            return File(GenericReportExportService.BuildPdf(result), "application/pdf", GenericReportExportService.PdfFileName(result));
        }

        private async Task<ReportViewResultDto?> LoadForExportAsync(
            string report,
            DateTime? from,
            DateTime? to,
            int? categoryId,
            int? salesRepId,
            string? status,
            string? search,
            CancellationToken cancellationToken)
        {
            ActiveRole = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            UserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var definition = ReportCatalog.Find(report);
            if (definition is null || !_authorization.CanAccess(ActiveRole, report)) return null;

            var filters = new ReportFilterDto
            {
                StartDate = from ?? DateTime.UtcNow.Date.AddDays(-(Math.Max(1, definition.DefaultDays) - 1)),
                EndDate = to ?? DateTime.UtcNow.Date,
                CategoryId = categoryId,
                SalesRepId = salesRepId,
                Status = status,
                Search = search,
                Page = 1,
                PageSize = 5000
            };

            return await _reports.RunAsync(report, filters, ActiveRole, UserId, cancellationToken);
        }

        public string ReportUrl(string key) => Url.Page("/Reports/Index", new { report = key }) ?? $"/Reports?report={key}";
        public string PageUrl(int page)
        {
            var query = new Dictionary<string, string?>
            {
                ["report"] = SelectedReport?.Key,
                ["from"] = Filters.StartDate.ToString("yyyy-MM-dd"),
                ["to"] = Filters.EndDate.ToString("yyyy-MM-dd"),
                ["categoryId"] = Filters.CategoryId?.ToString(),
                ["salesRepId"] = Filters.SalesRepId?.ToString(),
                ["status"] = Filters.Status,
                ["search"] = Filters.Search,
                ["page"] = page.ToString()
            };
            var queryString = string.Join("&", query.Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
                .Select(pair => $"{pair.Key}={Uri.EscapeDataString(pair.Value!)}"));
            return $"/Reports?{queryString}";
        }
    }
}
