using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Manufacture.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.Dispatch
{
    public class IndexModel : PageModel
    {
        private readonly ISalesService _salesService;
        private readonly IEmployeeService _employeeService;
        private readonly RbacCache _rbacService;

        public IndexModel(ISalesService salesService, IEmployeeService employeeService, RbacCache rbacService)
        {
            _salesService = salesService;
            _employeeService = employeeService;
            _rbacService = rbacService;
        }

        public List<DailyRepRouteSheet> RouteSheets { get; set; } = new();
        public DailyRepRouteSheet? SelectedSheet { get; set; }
        public List<SalesRepProfile> Reps { get; set; } = new();
        public List<EmployeeDto> LogisticsEmployees { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? SheetId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FilterDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? RepId { get; set; }

        public bool CanEdit { get; set; }
        public bool IsSalesRep { get; set; }
        public SalesRepProfile? CurrentSalesRep { get; set; }
        public string UserRole { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            if (!_rbacService.HasAccess(UserRole, "Sales") && !_rbacService.HasAccess(UserRole, "Dispatch"))
            {
                TempData["ErrorMessage"] = "Access Denied: You do not have permission to view Dispatch & Route Loading.";
                return RedirectToPage("/Dashboard/Index");
            }

            IsSalesRep = string.Equals(UserRole, "SalesRep", StringComparison.OrdinalIgnoreCase);
            if (IsSalesRep)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    CurrentSalesRep = await _salesService.GetSalesRepByUserIdAsync(userId.Value, cancellationToken);
                }

                if (CurrentSalesRep == null)
                {
                    TempData["ErrorMessage"] = "Your sales rep login is not linked to an active sales representative profile.";
                    return RedirectToPage("/Sales/Index");
                }

                // Sales reps can only see their own daily route sheets.
                RepId = CurrentSalesRep.Id;
            }

            CanEdit = UserRole == "SuperAdmin"
                || _rbacService.CanEdit(UserRole, "Sales")
                || _rbacService.CanCreate(UserRole, "Dispatch");

            Reps = await _salesService.GetSalesRepsAsync(cancellationToken: cancellationToken);
            if (IsSalesRep && CurrentSalesRep != null)
            {
                Reps = Reps.Where(r => r.Id == CurrentSalesRep.Id).ToList();
            }

            LogisticsEmployees = (await _employeeService.GetAllAsync(includeInactive: false, includeDeleted: false, cancellationToken))
                .Where(e => string.Equals(e.Department, "Logistics", StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.FullName)
                .ToList();

            var date = FilterDate ?? (IsSalesRep ? DateTime.UtcNow.Date : (DateTime?)null);
            RouteSheets = await _salesService.GetRouteSheetsAsync(date, RepId, null, cancellationToken);

            if (SheetId.HasValue && SheetId.Value > 0)
            {
                var requested = await _salesService.GetRouteSheetAsync(SheetId.Value, cancellationToken);
                if (requested != null && (!IsSalesRep || requested.RepId == CurrentSalesRep?.Id))
                {
                    SelectedSheet = requested;
                }
            }
            else if (RouteSheets.Any())
            {
                SelectedSheet = RouteSheets.First();
                SheetId = SelectedSheet.Id;
            }

            if (SelectedSheet == null && RouteSheets.Any())
            {
                SelectedSheet = RouteSheets.First();
                SheetId = SelectedSheet.Id;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSignOffAsync(
            int sheetId,
            Dictionary<int, int> loadedQuantities,
            string dispatcherName,
            string notes,
            CancellationToken cancellationToken = default)
        {
            if (!await CanManageSheetAsync(sheetId, cancellationToken))
            {
                TempData["ErrorMessage"] = "You can only sign off your own daily route sheets.";
                return RedirectToPage(new { FilterDate, RepId });
            }

            var userName = HttpContext.Session.GetString("UserName") ?? "Loading Supervisor";
            var dispatcher = string.IsNullOrWhiteSpace(dispatcherName) ? userName : dispatcherName;

            var success = await _salesService.SignOffDispatchAsync(sheetId, loadedQuantities ?? new(), dispatcher, notes, cancellationToken);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? $"Route Sheet #{sheetId} signed off successfully! Quantities locked and vehicle authorized for gate exit."
                : $"Failed to sign off Route Sheet #{sheetId}.";

            return RedirectToPage(new { sheetId, FilterDate, RepId });
        }

        public async Task<IActionResult> OnPostCreateSheetAsync(
            int newRepId,
            DateTime sheetDate,
            Dictionary<int, int> requested,
            CancellationToken cancellationToken = default)
        {
            var role = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            if (string.Equals(role, "SalesRep", StringComparison.OrdinalIgnoreCase))
            {
                var currentRep = await GetCurrentSalesRepAsync(cancellationToken);
                if (currentRep == null)
                {
                    TempData["ErrorMessage"] = "Your sales rep login is not linked to an active sales representative profile.";
                    return RedirectToPage(new { FilterDate, RepId });
                }

                newRepId = currentRep.Id;
            }

            try
            {
                var sheet = await _salesService.CreateRouteSheetAsync(newRepId, sheetDate, requested, cancellationToken);
                TempData["SuccessMessage"] = $"Created new route sheet {sheet.RouteSheetRef} for {sheet.RepName}.";
                return RedirectToPage(new { sheetId = sheet.Id, FilterDate = sheetDate, repId = newRepId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostDeleteSheetAsync(int sheetId, CancellationToken cancellationToken = default)
        {
            if (!await CanManageSheetAsync(sheetId, cancellationToken))
            {
                TempData["ErrorMessage"] = "You can only delete your own daily route sheets.";
                return RedirectToPage(new { FilterDate, RepId });
            }

            var userName = HttpContext.Session.GetString("UserName") ?? "Dispatcher";
            var success = await _salesService.DeleteRouteSheetAsync(sheetId, userName, cancellationToken);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? $"Route sheet #{sheetId} deleted."
                : "Only ordered route sheets that have not been loaded can be deleted.";

            return RedirectToPage(new { FilterDate, RepId });
        }

        private async Task<SalesRepProfile?> GetCurrentSalesRepAsync(CancellationToken cancellationToken)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            return userId.HasValue
                ? await _salesService.GetSalesRepByUserIdAsync(userId.Value, cancellationToken)
                : null;
        }

        private async Task<bool> CanManageSheetAsync(int sheetId, CancellationToken cancellationToken)
        {
            var role = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            if (!string.Equals(role, "SalesRep", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var currentRep = await GetCurrentSalesRepAsync(cancellationToken);
            if (currentRep == null)
            {
                return false;
            }

            var sheet = await _salesService.GetRouteSheetAsync(sheetId, cancellationToken);
            return sheet != null && sheet.RepId == currentRep.Id;
        }
    }
}
