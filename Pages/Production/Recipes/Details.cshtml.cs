using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Production.Recipes
{
    public class DetailsModel : PageModel
    {
        private readonly MockProductionService _productionService;

        public DetailsModel(MockProductionService productionService)
        {
            _productionService = productionService;
        }

        public Recipe Recipe { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var recipe = _productionService.GetRecipeById(id);
            if (recipe == null)
            {
                return RedirectToPage("/Production/Recipes/Index");
            }
            Recipe = recipe;
            return Page();
        }
    }
}
