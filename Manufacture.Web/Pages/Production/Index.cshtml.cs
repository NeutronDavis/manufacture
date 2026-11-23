using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;

namespace Manufacture.Pages.Production
{
    public class IndexModel : PageModel
    {
        private readonly IProductionService _productionService;

        public IndexModel(IProductionService productionService)
        {
            _productionService = productionService;
        }

        public List<Recipe> Recipes { get; set; } = new();
        public List<ProductionBatch> Batches { get; set; } = new();
        public List<AggregatedDemandItem> AggregatedDemand { get; set; } = new();

        public class AggregatedDemandItem
        {
            public string ProductName { get; set; } = string.Empty;
            public int TotalQuantityOrdered { get; set; }
        }

        public void OnGet()
        {
            Recipes = _productionService.GetAllRecipes();
            Batches = _productionService.GetAllBatches();

            var demandMap = Recipes.ToDictionary(r => r.Name, _ => 0);
            foreach (var order in _productionService.GetPreOrders())
            {
                foreach (var item in order.Items)
                {
                    if (demandMap.ContainsKey(item.ProductName))
                        demandMap[item.ProductName] += item.Quantity;
                    else
                        demandMap[item.ProductName] = item.Quantity;
                }
            }

            AggregatedDemand = demandMap.Select(kv => new AggregatedDemandItem
            {
                ProductName = kv.Key,
                TotalQuantityOrdered = kv.Value
            }).ToList();
        }
    }
}
