using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Services;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Production.Pricing
{
    public class HistoryModel : PageModel
    {
        private readonly IProductionService _productionService;

        public HistoryModel(IProductionService productionService)
        {
            _productionService = productionService;
        }

        public IngredientPriceChangeRequestDto PriceChange { get; set; } = null!;
        public int UserRoleId { get; set; }

        public decimal DifferencePercent =>
            PriceChange.PreviousUnitCost <= 0m
                ? 0m
                : Math.Round(PriceChange.Difference / PriceChange.PreviousUnitCost * 100m, 2);

        public IActionResult OnGet(int id)
        {
            UserRoleId = HttpContext.Session.GetRoleId();
            if (UserRoleId != (int)UserRole.ProductionManager
                && UserRoleId != (int)UserRole.SuperAdmin)
            {
                TempData["ErrorMessage"] = "You do not have access to price change history.";
                return RedirectToPage("/Dashboard/Index");
            }

            var request = _productionService.GetIngredientPriceChangeRequest(id);
            if (request == null)
            {
                TempData["ErrorMessage"] = $"Price change request #{id} was not found.";
                return RedirectToPage("/Production/Pricing/Index");
            }

            PriceChange = request;
            return Page();
        }
    }
}
