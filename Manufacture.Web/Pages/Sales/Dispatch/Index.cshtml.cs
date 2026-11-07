using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.Domain;
using Manufacture.Services;

namespace Manufacture.Pages.Sales.Dispatch
{
    public class IndexModel : PageModel
    {
        private readonly MockSalesService _salesService;
        private readonly RbacCache _rbacService;

        public IndexModel(MockSalesService salesService, RbacCache rbacService)
        {
            _salesService = salesService;
            _rbacService = rbacService;
        }

        public List<DailyRepRouteSheet> RouteSheets { get; set; } = new();
        public DailyRepRouteSheet? SelectedSheet { get; set; }
        public List<SalesRepProfile> Reps { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? SheetId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FilterDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? RepId { get; set; }

        public bool CanEdit { get; set; }
        public string UserRole { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            if (!_rbacService.HasAccess(UserRole, "Sales") && !_rbacService.HasAccess(UserRole, "Dispatch"))
            {
                TempData["ErrorMessage"] = "Access Denied: You do not have permission to view Dispatch & Route Loading.";
                return RedirectToPage("/Dashboard/Index");
            }

            CanEdit = UserRole == "SuperAdmin" ||
                      _rbacService.CanEdit(UserRole, "Sales") ||
                      _rbacService.CanCreate(UserRole, "Dispatch");

            Reps = _salesService.GetAllReps();
            var date = FilterDate ?? DateTime.UtcNow.Date;
            RouteSheets = _salesService.GetAllRouteSheets(date, RepId);

            if (SheetId.HasValue && SheetId.Value > 0)
            {
                SelectedSheet = _salesService.GetRouteSheetById(SheetId.Value);
            }
            else if (RouteSheets.Any())
            {
                SelectedSheet = RouteSheets.First();
                SheetId = SelectedSheet.Id;
            }

            return Page();
        }

        public IActionResult OnPostSignOff(int sheetId, Dictionary<int, int> loadedQuantities, string dispatcherName, string notes)
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            var userName = HttpContext.Session.GetString("UserName") ?? "Loading Supervisor";

            var disp = string.IsNullOrWhiteSpace(dispatcherName) ? userName : dispatcherName;
            var success = _salesService.SignOffDispatch(sheetId, loadedQuantities, disp, notes);

            if (success)
            {
                TempData["SuccessMessage"] = $"Route Sheet #{sheetId} signed off successfully! Quantities locked and vehicle authorized for gate exit.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Failed to sign off Route Sheet #{sheetId}.";
            }

            return RedirectToPage(new { sheetId, FilterDate, RepId });
        }

        public IActionResult OnPostCreateSheet(int newRepId, DateTime sheetDate, Dictionary<int, int> requested)
        {
            var sheet = _salesService.CreateRouteSheet(newRepId, sheetDate, requested);
            TempData["SuccessMessage"] = $"Created new route sheet {sheet.RouteSheetRef} for {sheet.RepName}.";
            return RedirectToPage(new { sheetId = sheet.Id, FilterDate = sheetDate, repId = newRepId });
        }
    }
}
