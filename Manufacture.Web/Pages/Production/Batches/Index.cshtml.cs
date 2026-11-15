using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;

namespace Manufacture.Pages.Production.Batches
{
    public class IndexModel : PageModel
    {
        private readonly IProductionService _productionService;

        public IndexModel(IProductionService productionService)
        {
            _productionService = productionService;
        }

        public List<ProductionBatch> Batches { get; set; } = new();
        public List<ProductionCategory> Categories { get; set; } = new();

        public void OnGet()
        {
            Batches = _productionService.GetAllBatches();
            Categories = _productionService.GetCategories(includeInactive: true)
                .Where(category => category.IsActive
                    || Batches.Any(batch => batch.ProductionCategoryId == category.Id))
                .OrderBy(category => category.Name)
                .ToList();
        }
    }
}
