using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Sales.Terminals
{
    public class IndexModel : PageModel
    {
        private readonly MockSalesService _salesService;

        public IndexModel(MockSalesService salesService)
        {
            _salesService = salesService;
        }

        public List<PosTerminal> Terminals { get; set; } = new();

        public void OnGet()
        {
            Terminals = _salesService.GetAllTerminals();
        }
    }
}
