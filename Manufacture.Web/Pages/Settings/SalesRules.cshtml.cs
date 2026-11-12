using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Settings
{
    public class SalesRulesModel : PageModel
    {
        private readonly ISalesService _salesService;

        public SalesRulesModel(ISalesService salesService)
        {
            _salesService = salesService;
        }

        public List<BonusRule> Rules { get; set; } = new();
        public List<Recipe> Products { get; set; } = new();

        [BindProperty]
        public BonusRuleDto Input { get; set; } = new() { IsActive = true, ThresholdQuantity = 10, BonusQuantity = 1 };

        public async Task OnGetAsync(int? editId, CancellationToken cancellationToken = default)
        {
            await LoadAsync(cancellationToken);
            if (editId.HasValue)
            {
                var rule = Rules.FirstOrDefault(r => r.Id == editId.Value);
                if (rule != null)
                {
                    Input = new BonusRuleDto
                    {
                        Id = rule.Id,
                        RecipeId = rule.RecipeId,
                        ThresholdQuantity = rule.ThresholdQuantity,
                        BonusQuantity = rule.BonusQuantity,
                        IsActive = rule.IsActive
                    };
                }
            }
        }

        public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                await LoadAsync(cancellationToken);
                return Page();
            }

            try
            {
                await _salesService.SaveBonusRuleAsync(Input, cancellationToken);
                TempData["SuccessMessage"] = "Bonus rule saved successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleAsync(int id, CancellationToken cancellationToken = default)
        {
            await _salesService.ToggleBonusRuleStatusAsync(id, cancellationToken);
            return RedirectToPage();
        }

        private async Task LoadAsync(CancellationToken cancellationToken)
        {
            Rules = await _salesService.GetBonusRulesAsync(includeInactive: true, cancellationToken);
            Products = await _salesService.GetSellableProductsAsync(cancellationToken);
        }
    }
}
