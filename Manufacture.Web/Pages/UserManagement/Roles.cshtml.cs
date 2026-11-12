using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Manufacture.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.UserManagement
{
    public class RolesModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IRbacService _rbacService;
        private readonly RbacCache _rbacCache;

        public RolesModel(IUserService userService, IRbacService rbacService, RbacCache rbacCache)
        {
            _userService = userService;
            _rbacService = rbacService;
            _rbacCache = rbacCache;
        }

        public List<RoleDto> Roles { get; set; } = new();

        [BindProperty]
        public RoleDto Input { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? editId, CancellationToken cancellationToken = default)
        {
            if (!CanManageRoles())
            {
                TempData["ErrorMessage"] = "Only an administrator with Settings access can manage platform roles.";
                return RedirectToPage("/UserManagement/Index");
            }

            await LoadAsync(cancellationToken);
            if (editId.HasValue)
            {
                var role = Roles.FirstOrDefault(r => r.Id == editId.Value);
                if (role != null)
                {
                    Input = role;
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken = default)
        {
            if (!CanManageRoles())
            {
                TempData["ErrorMessage"] = "Only an administrator with Settings access can manage platform roles.";
                return RedirectToPage("/UserManagement/Index");
            }

            try
            {
                var role = await _userService.SaveRoleAsync(Input, cancellationToken);
                await _rbacService.RefreshCacheAsync(cancellationToken);
                TempData["SuccessMessage"] = $"Platform role '{role.DisplayName}' saved successfully.";
                return RedirectToPage();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadAsync(cancellationToken);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            if (!CanManageRoles())
            {
                TempData["ErrorMessage"] = "Only an administrator with Settings access can manage platform roles.";
                return RedirectToPage("/UserManagement/Index");
            }

            try
            {
                var deleted = await _userService.DeleteRoleAsync(id, cancellationToken);
                await _rbacService.RefreshCacheAsync(cancellationToken);
                TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted
                    ? "Platform role deleted."
                    : "Role not found.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        private async Task LoadAsync(CancellationToken cancellationToken)
        {
            Roles = await _userService.GetRolesAsync(cancellationToken);
        }

        private bool CanManageRoles()
        {
            var userRole = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            return string.Equals(userRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase)
                || _rbacCache.CanManage(userRole, "Settings");
        }
    }
}
