using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Production.Batches
{
    public class CreateModel : PageModel
    {
        private readonly MockProductionService _productionService;

        public CreateModel(MockProductionService productionService)
        {
            _productionService = productionService;
        }

        [BindProperty]
        public ProductionBatchDto BatchInput { get; set; } = new();

        public List<Recipe> Recipes { get; set; } = new();

        public void OnGet(int? recipeId)
        {
            Recipes = _productionService.GetAllRecipes();
            if (recipeId.HasValue)
            {
                BatchInput.RecipeId = recipeId.Value;
            }
            BatchInput.BakerInCharge = "Emeka Obi";
            BatchInput.TargetQuantity = 200;
            BatchInput.ActualQuantity = 200;
            BatchInput.FlourBagsUsed = 2.0m;
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                Recipes = _productionService.GetAllRecipes();
                return Page();
            }

            var batch = _productionService.CreateBatch(BatchInput);
            TempData["SuccessMessage"] = $"Production batch '{batch.BatchNumber}' recorded ({batch.ActualQuantity} loaves, variance: {batch.Variance:+0;-0;0}).";
            return RedirectToPage("/Production/Batches/Index");
        }
    }
}
