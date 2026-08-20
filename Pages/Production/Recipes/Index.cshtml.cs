using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Production.Recipes
{
    public class IndexModel : PageModel
    {
        private readonly MockProductionService _productionService;

        public IndexModel(MockProductionService productionService)
        {
            _productionService = productionService;
        }

        public List<Recipe> Recipes { get; set; } = new();

        public void OnGet()
        {
            Recipes = _productionService.GetAllRecipes(includeInactive: true);
        }

        public IActionResult OnPostToggleStatus(int id)
        {
            _productionService.SoftDeleteRecipe(id);
            TempData["SuccessMessage"] = "Recipe status updated.";
            return RedirectToPage();
        }
    }
}
