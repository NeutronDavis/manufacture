using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Manufacture.Services;

namespace Manufacture.Pages.Production.Pricing
{
    public class IndexModel : PageModel
    {
        private readonly RecipeCostingEngine _engine;
        private readonly IProductionService _productionService;

        public IndexModel(RecipeCostingEngine engine, IProductionService productionService)
        {
            _engine = engine;
            _productionService = productionService;
        }

        public List<IngredientPriceDto> PriceMaster { get; set; } = new();
        public ProfitabilityReportDto Report { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public List<IngredientPriceChangeRequestDto> PendingRequests { get; set; } = new();
        public List<IngredientPriceChangeRequestDto> PriceHistory { get; set; } = new();
        public Dictionary<int, IngredientPriceChangeRequestDto> PendingByItemId { get; set; } = new();

        public string UserRole { get; set; } = string.Empty;
        public bool CanSubmitPriceChanges =>
            string.Equals(UserRole, "ProductionManager", StringComparison.OrdinalIgnoreCase);
        public bool CanApprovePriceChanges =>
            string.Equals(UserRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

        [BindProperty]
        public List<UpdateIngredientPriceDto> Edits { get; set; } = new();

        public IActionResult OnGet()
        {
            LoadRole();

            if (!CanSubmitPriceChanges && !CanApprovePriceChanges)
            {
                TempData["ErrorMessage"] = "You do not have access to the ingredient price control page.";
                return RedirectToPage("/Dashboard/Index");
            }

            Refresh();
            return Page();
        }

        public IActionResult OnPostSubmitPriceChanges()
        {
            LoadRole();

            if (!CanSubmitPriceChanges)
            {
                TempData["ErrorMessage"] = "Only the Production Manager can submit price changes for approval.";
                return RedirectToPage();
            }

            var updates = Edits
                .Where(edit => edit.IngredientId > 0)
                .ToList();

            if (updates.Count == 0)
            {
                TempData["ErrorMessage"] = "No price changes were submitted.";
                return RedirectToPage();
            }

            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var userName = HttpContext.Session.GetString("UserName") ?? "Production Manager";
            var result = _productionService.SubmitIngredientPriceChangeRequests(updates, userId, userName);

            if (result.Errors.Count > 0)
            {
                TempData["ErrorMessage"] = string.Join(" ", result.Errors);
            }
            else if (result.SubmittedCount > 0)
            {
                TempData["SuccessMessage"] = result.Messages.LastOrDefault()
                    ?? "Price changes were submitted for approval.";
            }
            else
            {
                TempData["ErrorMessage"] = result.Messages.FirstOrDefault()
                    ?? "No price changes differed from the current approved prices.";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostApprovePriceRequest(int id)
        {
            LoadRole();

            if (!CanApprovePriceChanges)
            {
                TempData["ErrorMessage"] = "Only the SuperAdmin can approve ingredient price changes.";
                return RedirectToPage();
            }

            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var userName = HttpContext.Session.GetString("UserName") ?? "SuperAdmin";
            var result = _productionService.ApproveIngredientPriceChangeRequest(id, userId, userName);

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage();
        }

        public IActionResult OnPostRejectPriceRequest(int id, string? reason)
        {
            LoadRole();

            if (!CanApprovePriceChanges)
            {
                TempData["ErrorMessage"] = "Only the SuperAdmin can reject ingredient price changes.";
                return RedirectToPage();
            }

            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var userName = HttpContext.Session.GetString("UserName") ?? "SuperAdmin";
            var result = _productionService.RejectIngredientPriceChangeRequest(
                id,
                userId,
                userName,
                reason ?? string.Empty);

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage();
        }

        private void LoadRole()
        {
            UserRole = HttpContext.Session.GetString("UserRole") ?? string.Empty;
        }

        private void Refresh()
        {
            PriceMaster = _engine.GetPriceMaster();
            Categories = PriceMaster.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();
            Report = _engine.BuildProfitabilityReport(includeInactive: false);

            PendingRequests = _productionService.GetIngredientPriceChangeRequests(IngredientPriceChangeStatus.Pending);
            PriceHistory = _productionService.GetIngredientPriceChangeRequests();
            PendingByItemId = PendingRequests
                .GroupBy(request => request.InventoryItemId)
                .ToDictionary(group => group.Key, group => group.First());
        }
    }
}
