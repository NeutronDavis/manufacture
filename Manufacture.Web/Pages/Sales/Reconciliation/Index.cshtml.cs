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
        private readonly ISalesService _salesService;
        private readonly RbacCache _rbacService;
        private readonly SettingsCache _settingsCache;

        public IndexModel(
            ISalesService salesService,
            RbacCache rbacService,
            SettingsCache settingsCache)
        {
            _salesService = salesService;
            _rbacService = rbacService;
            _settingsCache = settingsCache;
        }

        public List<DailyRepRouteSheet> RouteSheets { get; set; } = new();
        public DailyRepRouteSheet? SelectedSheet { get; set; }
        public List<CustomerReturn> CustomerReturns { get; set; } = new();
        public List<Customer> Customers { get; set; } = new();
        public List<Recipe> Products { get; set; } = new();
        public List<SalesRepProfile> SalesReps { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? SheetId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FilterDate { get; set; }

        [BindProperty]
        public CustomerReturnInputDto ReturnInput { get; set; } = new();

        public decimal PlannedCashPercent => _settingsCache.PlannedCashPercent;

        public decimal GrossSales => SelectedSheet?.TotalExpectedSalesRevenue ?? 0m;
        public decimal CustomerReturnValue => CustomerReturns.Sum(r => r.RefundAmount);
        public decimal DamagedValue => SelectedSheet?.Items.Sum(i => i.QtyDamaged * i.UnitSellingPrice) ?? 0m;
        public decimal NetRevenue => GrossSales - CustomerReturnValue - DamagedValue;

        public bool CanEdit { get; set; }
        public bool IsSalesRep { get; set; }
        public SalesRepProfile? CurrentSalesRep { get; set; }
        public string UserRole { get; set; } = string.Empty;
        public string DefaultCashierName { get; set; } = "Chief Cashier";

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            DefaultCashierName = HttpContext.Session.GetString("UserName") ?? "Chief Cashier";
            if (!_rbacService.HasAccess(UserRole, "Sales") && !_rbacService.HasAccess(UserRole, "Reports"))
            {
                TempData["ErrorMessage"] = "Access Denied: You do not have permission to access Sales Reconciliation.";
                return RedirectToPage("/Dashboard/Index");
            }

            IsSalesRep = string.Equals(UserRole, "SalesRep", StringComparison.OrdinalIgnoreCase);
            CanEdit = UserRole == "SuperAdmin"
                || IsSalesRep
                || _rbacService.CanEdit(UserRole, "Sales");

            int? repFilter = null;
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

                // Sales reps can only reconcile their own daily route sheets.
                repFilter = CurrentSalesRep.Id;
            }

            var date = FilterDate ?? (IsSalesRep ? DateTime.UtcNow.Date : (DateTime?)null);
            RouteSheets = await _salesService.GetRouteSheetsAsync(date, repFilter, null, cancellationToken);
            Customers = await _salesService.GetCustomersAsync(cancellationToken: cancellationToken);
            Products = await _salesService.GetSellableProductsAsync(cancellationToken);
            SalesReps = await _salesService.GetSalesRepsAsync(cancellationToken: cancellationToken);
            if (IsSalesRep && CurrentSalesRep != null)
            {
                SalesReps = SalesReps.Where(r => r.Id == CurrentSalesRep.Id).ToList();
            }

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
                SelectedSheet = RouteSheets.FirstOrDefault(s => s.Status == RouteSheetStatus.LoadedOnRoute) ?? RouteSheets.First();
                SheetId = SelectedSheet.Id;
            }

            if (SelectedSheet == null && RouteSheets.Any())
            {
                SelectedSheet = RouteSheets.FirstOrDefault(s => s.Status == RouteSheetStatus.LoadedOnRoute) ?? RouteSheets.First();
                SheetId = SelectedSheet.Id;
            }

            CustomerReturns = await _salesService.GetCustomerReturnsAsync(date ?? SelectedSheet?.Date, SelectedSheet?.RepId, cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostReconcileAsync(ReconciliationInputDto input, CancellationToken cancellationToken = default)
        {
            if (!await CanManageSheetAsync(input.RouteSheetId, cancellationToken))
            {
                TempData["ErrorMessage"] = "You can only reconcile your own daily route sheets.";
                return RedirectToPage(new { sheetId = input.RouteSheetId, filterDate = FilterDate });
            }

            var userName = HttpContext.Session.GetString("UserName") ?? "Chief Cashier";
            if (string.IsNullOrWhiteSpace(input.CashierName))
            {
                input.CashierName = userName;
            }

            var success = await _salesService.ReconcileDailyRepAccountAsync(input, cancellationToken);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? $"Sales reconciliation for route sheet #{input.RouteSheetId} completed. Unsold closing stock rolled into the next day."
                : $"Failed to reconcile route sheet #{input.RouteSheetId}.";

            return RedirectToPage(new { sheetId = input.RouteSheetId, filterDate = FilterDate });
        }

        public async Task<IActionResult> OnPostRecordReturnAsync(CancellationToken cancellationToken = default)
        {
            ReturnInput.RecordedBy = HttpContext.Session.GetString("UserName") ?? "Sales";

            var role = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            if (string.Equals(role, "SalesRep", StringComparison.OrdinalIgnoreCase))
            {
                var currentRep = await GetCurrentSalesRepAsync(cancellationToken);
                if (currentRep == null)
                {
                    TempData["ErrorMessage"] = "Your sales rep login is not linked to an active sales representative profile.";
                    return RedirectToPage(new { sheetId = SheetId, filterDate = FilterDate });
                }

                ReturnInput.SalesRepId = currentRep.Id;
            }

            try
            {
                await _salesService.RecordCustomerReturnAsync(ReturnInput, cancellationToken);
                TempData["SuccessMessage"] = "Customer return recorded successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage(new { sheetId = SheetId, filterDate = FilterDate });
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
