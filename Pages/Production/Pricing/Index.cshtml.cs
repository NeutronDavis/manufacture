using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.DTOs;
using Manufacture.Services;

namespace Manufacture.Pages.Production.Pricing
{
    /// <summary>
    /// Admin price &amp; margin dashboard (doc/recipe-costing.md UI #3).
    /// <para>
    /// Two tables: the master ingredient price list (editable, price changes
    /// cascade live to every recipe) and a profitability report comparing each
    /// product's actual unit production cost against its configured selling price.
    /// </para>
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly MockProductionService _productionService;
        private readonly RecipeCostingEngine _engine;

        public IndexModel(MockProductionService productionService, RecipeCostingEngine engine)
        {
            _productionService = productionService;
            _engine = engine;
        }

        public List<IngredientPriceDto> PriceMaster { get; set; } = new();
        public ProfitabilityReportDto Report { get; set; } = new();
        public List<string> Categories { get; set; } = new();

        /// <summary>Individual edits, one per row of the price table.</summary>
        [BindProperty]
        public List<UpdateIngredientPriceDto> Edits { get; set; } = new();

        public void OnGet()
        {
            Refresh();
        }

        public IActionResult OnPostUpdatePrices()
        {
            var pending = Edits.Where(e => e.IngredientId > 0).ToList();

            if (pending.Count == 0)
            {
                TempData["ErrorMessage"] = "No price changes were submitted.";
                Refresh();
                return Page();
            }

            var applied = _engine.ApplyPriceUpdates(pending);
            Refresh();

            if (applied == 0)
            {
                TempData["ErrorMessage"] = "No ingredient prices could be updated.";
                return Page();
            }

            TempData["SuccessMessage"] =
                $"Updated {applied} ingredient price{(applied == 1 ? "" : "s")}. " +
                $"All recipe unit costs and margins have been recalculated.";

            if (Report.NegativeMarginCount > 0)
            {
                TempData["ErrorMessage"] =
                    $"{Report.NegativeMarginCount} product(s) are now priced below cost after this update.";
            }

            Edits = new List<UpdateIngredientPriceDto>();
            return Page();
        }

        private void Refresh()
        {
            PriceMaster = _engine.GetPriceMaster();
            Categories = PriceMaster.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();
            Report = _engine.BuildProfitabilityReport(includeInactive: false);
        }
    }
}
