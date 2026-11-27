using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Manufacture.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Production.Repositories
{
    public class DetailsModel : PageModel
    {
        private readonly IProductRepositoryService _repositoryService;
        private readonly RbacCache _rbac;

        public DetailsModel(IProductRepositoryService repositoryService, RbacCache rbac)
        {
            _repositoryService = repositoryService;
            _rbac = rbac;
        }

        public int RepositoryId { get; set; }
        public ProductRepositoryDto Repository { get; set; } = null!;
        public List<ProductRepositoryStockDto> Stock { get; set; } = new();
        public List<ProductStockMovementDto> Movements { get; set; } = new();
        public List<StockProductOptionDto> AllProducts { get; set; } = new();
        public List<ProductRepositoryDto> Destinations { get; set; } = new();
        public bool CanEdit { get; set; }

        [BindProperty]
        public AdjustStockInputDto AdjustInput { get; set; } = new();

        [BindProperty]
        public TransferStockInputDto TransferInput { get; set; } = new();

        [BindProperty]
        public AddStockInputModel AddInput { get; set; } = new();

        public class AddStockInputModel
        {
            public int RecipeId { get; set; }
            public decimal Quantity { get; set; }
            public decimal Threshold { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken = default)
        {
            var roleId = HttpContext.Session.GetRoleId();
            CanEdit = roleId == (int)UserRole.SuperAdmin || _rbac.CanEdit(roleId, "Production");
            await LoadAsync(id, cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostAddStockAsync(int id, CancellationToken cancellationToken = default)
        {
            if (!await CanEditAsync())
            {
                return Forbid();
            }

            try
            {
                await _repositoryService.AddOrUpdateStockItemAsync(id, AddInput.RecipeId, AddInput.Quantity, AddInput.Threshold, cancellationToken);
                TempData["SuccessMessage"] = "Product added to repository.";
                return RedirectToPage(new { id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage(new { id });
            }
        }

        public async Task<IActionResult> OnPostAdjustAsync(int id, CancellationToken cancellationToken = default)
        {
            if (!await CanEditAsync())
            {
                return Forbid();
            }

            try
            {
                AdjustInput.RepositoryId = id;
                await _repositoryService.AdjustStockAsync(AdjustInput, HttpContext.Session.GetString("UserName") ?? "Admin", cancellationToken);
                TempData["SuccessMessage"] = "Stock level adjusted.";
                return RedirectToPage(new { id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage(new { id });
            }
        }

        public async Task<IActionResult> OnPostTransferAsync(int id, CancellationToken cancellationToken = default)
        {
            if (!await CanEditAsync())
            {
                return Forbid();
            }

            try
            {
                TransferInput.FromRepositoryId = id;
                await _repositoryService.TransferStockAsync(TransferInput, HttpContext.Session.GetString("UserName") ?? "Admin", cancellationToken);
                TempData["SuccessMessage"] = "Stock transferred. Both repository balances were updated.";
                return RedirectToPage(new { id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage(new { id });
            }
        }

        public async Task<IActionResult> OnPostToggleAsync(int id, CancellationToken cancellationToken = default)
        {
            if (!await CanEditAsync())
            {
                return Forbid();
            }

            try
            {
                await _repositoryService.ToggleStatusAsync(id, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage(new { id });
        }

        private async Task LoadAsync(int id, CancellationToken cancellationToken)
        {
            RepositoryId = id;
            Repository = await _repositoryService.GetRepositoryAsync(id, cancellationToken)
                ?? throw new InvalidOperationException("Repository not found.");
            Stock = await _repositoryService.GetStockAsync(id, cancellationToken);
            Movements = await _repositoryService.GetMovementsAsync(id, take: 100, cancellationToken: cancellationToken);
            AllProducts = await _repositoryService.GetAllProductOptionsAsync(cancellationToken);
            Destinations = (await _repositoryService.GetRepositoriesAsync(includeInactive: false, cancellationToken))
                .Where(r => r.Id != id)
                .ToList();

            AdjustInput.RepositoryId = id;
            TransferInput.FromRepositoryId = id;
        }

        private Task<bool> CanEditAsync()
        {
            var roleId = HttpContext.Session.GetRoleId();
            return Task.FromResult(roleId == (int)UserRole.SuperAdmin || _rbac.CanEdit(roleId, "Production"));
        }
    }
}
