using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Manufacture.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.Reconciliation
{
    public class IndexModel : PageModel
    {
        private readonly ISalesReconciliationService _reconciliationService;
        private readonly ISalesService _salesService;
        private readonly RbacCache _rbac;

        public IndexModel(
            ISalesReconciliationService reconciliationService,
            ISalesService salesService,
            RbacCache rbac)
        {
            _reconciliationService = reconciliationService;
            _salesService = salesService;
            _rbac = rbac;
        }

        public List<ReconciliationSummaryDto> Sheets { get; set; } = new();
        public SalesReconciliationDto? Selected { get; set; }
        public List<SalesRepProfile> SalesReps { get; set; } = new();
        public DateOnly FilterDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
        public int? SheetId { get; set; }
        public int? RepFilter { get; set; }
        public bool CanEdit { get; set; }
        public bool IsSalesRep { get; set; }
        public SalesRepProfile? CurrentSalesRep { get; set; }
        public string UserRole { get; set; } = string.Empty;
        public string DefaultReconcilerName { get; set; } = string.Empty;

        [BindProperty]
        public SaveReconciliationDto SaveInput { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? sheetId, DateTime? filterDate, int? repId, CancellationToken cancellationToken = default)
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            if (!_rbac.HasAccess(UserRole, "Sales") && !_rbac.HasAccess(UserRole, "Reports"))
            {
                return RedirectToPage("/Dashboard/Index");
            }

            DefaultReconcilerName = HttpContext.Session.GetString("UserName") ?? "Sales Rep";
            IsSalesRep = string.Equals(UserRole, "SalesRep", StringComparison.OrdinalIgnoreCase);
            FilterDate = DateOnly.FromDateTime(filterDate?.Date ?? DateTime.UtcNow.Date);
            SheetId = sheetId;

            if (IsSalesRep)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    CurrentSalesRep = await _salesService.GetSalesRepByUserIdAsync(userId.Value, cancellationToken);
                }

                if (CurrentSalesRep == null)
                {
                    TempData["ErrorMessage"] = "Your login is not linked to a sales representative profile.";
                    return Page();
                }

                RepFilter = CurrentSalesRep.Id;
                CanEdit = true;
            }
            else
            {
                SalesReps = await _salesService.GetSalesRepsAsync(cancellationToken: cancellationToken);
                RepFilter = repId;
                CanEdit = UserRole == "SuperAdmin" || _rbac.CanEdit(UserRole, "Sales");
            }

            Sheets = await _reconciliationService.ListAsync(FilterDate, RepFilter, cancellationToken);

            var selectedSummary = sheetId.HasValue
                ? Sheets.FirstOrDefault(s => s.Id == sheetId.Value)
                : Sheets.FirstOrDefault();

            if (selectedSummary != null)
            {
                SheetId = selectedSummary.Id;
                Selected = await _reconciliationService.GetAsync(selectedSummary.Id, cancellationToken);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostStartAsync(DateTime? filterDate, int? repId, CancellationToken cancellationToken = default)
        {
            var role = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            var (currentRep, _) = await ResolveCurrentRepAsync(cancellationToken);
            var rep = string.Equals(role, "SalesRep", StringComparison.OrdinalIgnoreCase)
                ? currentRep
                : (repId.HasValue ? await _salesService.GetSalesRepAsync(repId.Value, cancellationToken) : null);

            if (rep == null)
            {
                TempData["ErrorMessage"] = "Select a sales representative to start a reconciliation.";
                return RedirectToPage();
            }

            var date = DateOnly.FromDateTime(filterDate?.Date ?? DateTime.UtcNow.Date);
            var sheet = await _reconciliationService.GetOrCreateAsync(rep.Id, date, cancellationToken);
            return RedirectToPage(new { sheetId = sheet.Id, filterDate = sheet.ReconciliationDate.ToString("yyyy-MM-dd") });
        }

        public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken = default)
        {
            if (!await CanManageAsync(SaveInput.Id, cancellationToken))
            {
                return Forbid();
            }

            try
            {
                var sheet = await _reconciliationService.SaveDraftAsync(SaveInput, cancellationToken);
                TempData["SuccessMessage"] = $"Draft saved for {sheet.RepName} · {sheet.ReconciliationDate:yyyy-MM-dd}.";
                return RedirectToPage(new { sheetId = sheet.Id, filterDate = sheet.ReconciliationDate.ToString("yyyy-MM-dd") });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage(new { sheetId = SaveInput.Id });
            }
        }

        public async Task<IActionResult> OnPostReconcileAsync(CancellationToken cancellationToken = default)
        {
            if (!await CanManageAsync(SaveInput.Id, cancellationToken))
            {
                return Forbid();
            }

            try
            {
                var actor = HttpContext.Session.GetString("UserName") ?? "Sales";
                var sheet = await _reconciliationService.ReconcileAsync(SaveInput, actor, cancellationToken);
                TempData["SuccessMessage"] = $"Reconciliation {sheet.ReconciliationNumber} closed. Closing stock rolls into the next sheet.";
                return RedirectToPage(new { sheetId = sheet.Id, filterDate = sheet.ReconciliationDate.ToString("yyyy-MM-dd") });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage(new { sheetId = SaveInput.Id });
            }
        }

        public async Task<IActionResult> OnPostSyncAsync(int id, CancellationToken cancellationToken = default)
        {
            if (!await CanManageAsync(id, cancellationToken))
            {
                return Forbid();
            }

            try
            {
                var sheet = await _reconciliationService.SyncProductsAsync(id, cancellationToken);
                TempData["SuccessMessage"] = "Repository products synchronized.";
                return RedirectToPage(new { sheetId = sheet.Id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage(new { sheetId = id });
            }
        }

        private async Task<(SalesRepProfile? Rep, int? UserId)> ResolveCurrentRepAsync(CancellationToken cancellationToken)
        {
            var role = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            var userId = HttpContext.Session.GetInt32("UserId");

            if (string.Equals(role, "SalesRep", StringComparison.OrdinalIgnoreCase) && userId.HasValue)
            {
                return (await _salesService.GetSalesRepByUserIdAsync(userId.Value, cancellationToken), userId);
            }

            return (null, userId);
        }

        private async Task<bool> CanManageAsync(int sheetId, CancellationToken cancellationToken)
        {
            var role = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            var sheet = await _reconciliationService.GetAsync(sheetId, cancellationToken);
            if (sheet == null)
            {
                return false;
            }

            if (string.Equals(role, "SalesRep", StringComparison.OrdinalIgnoreCase))
            {
                var (rep, _) = await ResolveCurrentRepAsync(cancellationToken);
                return rep != null && rep.Id == sheet.SalesRepId;
            }

            return role == "SuperAdmin" || _rbac.CanEdit(role, "Sales");
        }
    }
}
