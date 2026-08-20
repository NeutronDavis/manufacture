using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Production.Batches
{
    public class IndexModel : PageModel
    {
        private readonly MockProductionService _productionService;

        public IndexModel(MockProductionService productionService)
        {
            _productionService = productionService;
        }

        public List<ProductionBatch> Batches { get; set; } = new();

        public void OnGet()
        {
            Batches = _productionService.GetAllBatches();
        }
    }
}
