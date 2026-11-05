using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Manufacture.Services;

namespace Manufacture.Pages.Sales.Reconciliation
{
    public class IndexModel : PageModel
    {
        private readonly MockSalesService _salesService;
        private readonly MockRbacService _rbacService;
        private readonly MockSettingsService _settingsService;

        public IndexModel(
            MockSalesService salesService,
            MockRbacService rbacService,
            MockSettingsService settingsService)
        {
            _salesService = salesService;
            _rbacService = rbacService;
            _settingsService = settingsService;
        }

        public List<DailyRepRouteSheet> RouteSheets { get; set; } = new();
        public DailyRepRouteSheet? SelectedSheet { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SheetId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FilterDate { get; set; }

        public decimal PlannedCashPercent => _settingsService.PlannedCashPercent;

        public bool CanEdit { get; set; }
        public string UserRole { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            if (!_rbacService.HasAccess(UserRole, "Sales") && !_rbacService.HasAccess(UserRole, "Reports"))
            {
                TempData["ErrorMessage"] = "Access Denied: You do not have permission to access Route Closing Reconciliation.";
                return RedirectToPage("/Dashboard/Index");
            }

            CanEdit = UserRole == "SuperAdmin" ||
                      _rbacService.GetPermissionLevel(UserRole, "Sales").Contains("Edit") ||
                      _rbacService.GetPermissionLevel(UserRole, "Sales") == "Full Access";

            var date = FilterDate ?? DateTime.UtcNow.Date;
            RouteSheets = _salesService.GetAllRouteSheets(date);

            if (SheetId.HasValue && SheetId.Value > 0)
            {
                SelectedSheet = _salesService.GetRouteSheetById(SheetId.Value);
            }
            else if (RouteSheets.Any())
            {
                // Prefer one that is LoadedOnRoute or Ordered
                SelectedSheet = RouteSheets.FirstOrDefault(s => s.Status == RouteSheetStatus.LoadedOnRoute) ?? RouteSheets.First();
                SheetId = SelectedSheet.Id;
            }

            return Page();
        }

        public IActionResult OnPostReconcile(ReconciliationInputDto input)
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            var userName = HttpContext.Session.GetString("UserName") ?? "Chief Cashier";

            if (string.IsNullOrWhiteSpace(input.CashierName))
            {
                input.CashierName = userName;
            }

            var success = _salesService.ReconcileDailyRepAccount(input);
            if (success)
            {
                TempData["SuccessMessage"] = $"Route Sheet #{input.RouteSheetId} successfully reconciled and account balance updated! Unsold stock will roll over to tomorrow.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Failed to reconcile Route Sheet #{input.RouteSheetId}.";
            }

            return RedirectToPage(new { sheetId = input.RouteSheetId, filterDate = FilterDate });
        }
    }
}
