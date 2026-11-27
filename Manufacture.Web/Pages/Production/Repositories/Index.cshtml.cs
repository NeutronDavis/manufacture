using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Manufacture.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Production.Repositories
{
    public class IndexModel : PageModel
    {
        private readonly IProductRepositoryService _repositoryService;
        private readonly RbacCache _rbac;

        public IndexModel(IProductRepositoryService repositoryService, RbacCache rbac)
        {
            _repositoryService = repositoryService;
            _rbac = rbac;
        }

        public List<ProductRepositoryDto> Repositories { get; set; } = new();
        public List<ProductRepositoryTotalDto> Totals { get; set; } = new();
        public List<ProductRepositoryStockDto> LowStock { get; set; } = new();
        public bool CanEdit { get; set; }
        public decimal GrandTotalUnits => Repositories.Sum(r => r.TotalUnits);
        public int GrandSkuCount => Totals.Count;
        public int GrandLowStockCount => LowStock.Count;

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            var role = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            CanEdit = role == "SuperAdmin" || _rbac.CanEdit(role, "Production");
            Repositories = await _repositoryService.GetRepositoriesAsync(includeInactive: true, cancellationToken);
            Totals = await _repositoryService.GetProductTotalsAsync(cancellationToken);
            LowStock = await _repositoryService.GetLowStockAsync(cancellationToken);
        }
    }
}
