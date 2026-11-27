using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Manufacture.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Production.Repositories
{
    public class CreateModel : PageModel
    {
        private readonly IProductRepositoryService _repositoryService;
        private readonly RbacCache _rbac;

        public CreateModel(IProductRepositoryService repositoryService, RbacCache rbac)
        {
            _repositoryService = repositoryService;
            _rbac = rbac;
        }

        [BindProperty]
        public ProductRepositoryInputDto Input { get; set; } = new()
        {
            LocationType = ProductRepositoryLocationType.Mall,
            IsActive = true
        };

        public IActionResult OnGet()
        {
            var roleId = HttpContext.Session.GetRoleId();
            if (roleId != (int)UserRole.SuperAdmin && !_rbac.CanEdit(roleId, "Production"))
            {
                return RedirectToPage("/Production/Repositories/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                await _repositoryService.CreateRepositoryAsync(Input, cancellationToken);
                TempData["SuccessMessage"] = $"Repository '{Input.Name}' created.";
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
