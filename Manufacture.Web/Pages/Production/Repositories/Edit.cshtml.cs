using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Manufacture.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Production.Repositories
{
    public class EditModel : PageModel
    {
        private readonly IProductRepositoryService _repositoryService;
        private readonly RbacCache _rbac;

        public EditModel(IProductRepositoryService repositoryService, RbacCache rbac)
        {
            _repositoryService = repositoryService;
            _rbac = rbac;
        }

        public int RepositoryId { get; set; }

        [BindProperty]
        public ProductRepositoryInputDto Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken = default)
        {
            var role = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            if (role != "SuperAdmin" && !_rbac.CanEdit(role, "Production"))
            {
                return RedirectToPage("/Production/Repositories/Index");
            }

            var repository = await _repositoryService.GetRepositoryAsync(id, cancellationToken);
            if (repository == null)
            {
                TempData["ErrorMessage"] = "Repository not found.";
                return RedirectToPage("/Production/Repositories/Index");
            }

            RepositoryId = repository.Id;
            Input = new ProductRepositoryInputDto
            {
                Code = repository.Code,
                Name = repository.Name,
                LocationType = repository.LocationType,
                Address = repository.Address,
                IsDefaultProductionOutput = repository.IsDefaultProductionOutput,
                IsActive = repository.IsActive
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken = default)
        {
            RepositoryId = id;
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                await _repositoryService.UpdateRepositoryAsync(id, Input, cancellationToken);
                TempData["SuccessMessage"] = $"Repository '{Input.Name}' updated.";
                return RedirectToPage("/Production/Repositories/Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }
}
