using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Production.Batches
{
    public class CreateModel : PageModel
    {
        private readonly IProductionService _productionService;
        private readonly IProductRepositoryService _repositoryService;

        public CreateModel(IProductionService productionService, IProductRepositoryService repositoryService)
        {
            _productionService = productionService;
            _repositoryService = repositoryService;
        }

        [BindProperty]
        public ProductionBatchCreateDto BatchInput { get; set; } = new();

        public List<Recipe> Recipes { get; set; } = new();
        public List<ProductionShift> Shifts { get; set; } = new();
        public List<ProductRepositoryStockDto> FactoryStock { get; set; } = new();
        public string FactoryRepositoryName { get; set; } = string.Empty;

        public async Task OnGetAsync(int? recipeId, CancellationToken cancellationToken = default)
        {
            LoadFormData();
            await LoadFactoryStockAsync(cancellationToken);

            BatchInput.Shift = Shifts.FirstOrDefault()?.Name ?? "Morning Shift";
            BatchInput.BakerInCharge = HttpContext.Session.GetString("UserName") ?? string.Empty;
            BatchInput.ProductionDate = DateTime.UtcNow;

            var selected = recipeId.HasValue
                ? Recipes.FirstOrDefault(r => r.Id == recipeId.Value)
                : Recipes.FirstOrDefault();

            if (selected != null)
            {
                BatchInput.Lines.Add(new ProductionBatchLineInputDto
                {
                    RecipeId = selected.Id,
                    TargetQuantity = Math.Max(1, (int)selected.ExpectedYield)
                });
            }
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
        {
            LoadFormData();
            await LoadFactoryStockAsync(cancellationToken);

            BatchInput.Lines = (BatchInput.Lines ?? new List<ProductionBatchLineInputDto>())
                .Where(l => l.RecipeId > 0 && l.TargetQuantity > 0)
                .ToList();

            if (BatchInput.Lines.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Add at least one product recipe and target quantity.");
            }

            if (!ModelState.IsValid)
            {
                EnsureFormDefaults();
                return Page();
            }

            try
            {
                var loggedBy = HttpContext.Session.GetString("UserName") ?? "Production Manager";
                var batch = _productionService.CreateBatch(BatchInput, loggedBy);
                TempData["SuccessMessage"] =
                    $"Production batch '{batch.BatchNumber}' planned with {batch.Lines.Count} product line(s). " +
                    "Record the actual outcome when the run is complete.";
                return RedirectToPage("/Production/Batches/Details", new { id = batch.Id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsureFormDefaults();
                return Page();
            }
        }

        private void LoadFormData()
        {
            Recipes = _productionService.GetAllRecipes(includeInactive: false);
            Shifts = _productionService.GetShifts();
        }

        private async Task LoadFactoryStockAsync(CancellationToken cancellationToken)
        {
            var factories = await _repositoryService.GetRepositoriesAsync(includeInactive: false, cancellationToken);
            var factory = factories.FirstOrDefault(r => r.IsDefaultProductionOutput) ?? factories.FirstOrDefault();
            if (factory != null)
            {
                FactoryRepositoryName = factory.Name;
                FactoryStock = await _repositoryService.GetStockAsync(factory.Id, cancellationToken);
            }
        }

        private void EnsureFormDefaults()
        {
            if (string.IsNullOrWhiteSpace(BatchInput.Shift))
            {
                BatchInput.Shift = Shifts.FirstOrDefault()?.Name ?? "Morning Shift";
            }

            if (string.IsNullOrWhiteSpace(BatchInput.BakerInCharge))
            {
                BatchInput.BakerInCharge = HttpContext.Session.GetString("UserName") ?? string.Empty;
            }
        }
    }
}
