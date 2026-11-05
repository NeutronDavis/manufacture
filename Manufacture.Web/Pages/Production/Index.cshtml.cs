using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Production
{
    public class IndexModel : PageModel
    {
        private readonly MockProductionService _productionService;
        private readonly MockSalesService _salesService;

        public IndexModel(MockProductionService productionService, MockSalesService salesService)
        {
            _productionService = productionService;
            _salesService = salesService;
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

            // Calculate demand from pre-orders
            var preOrders = _salesService.GetPreOrders();
            var demandMap = new Dictionary<string, int>();

            foreach (var r in Recipes)
            {
                demandMap[r.Name] = 0;
            }

            foreach (var order in preOrders)
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
