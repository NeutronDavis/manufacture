using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Manufacture.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.UserManagement
{
    public class PermissionsModel : PageModel
    {
        private readonly IRbacService _rbacService;
        private readonly RbacCache _rbacCache;

        public PermissionsModel(IRbacService rbacService, RbacCache rbacCache)
        {
            _rbacService = rbacService;
            _rbacCache = rbacCache;
        }

        public RbacMatrixDto Matrix { get; set; } = new();
        public bool IsEditMode { get; set; }

        [BindProperty]
        public RbacSaveInputDto Input { get; set; } = new();

        public string UserRole => HttpContext.Session.GetString("UserRole") ?? string.Empty;
        public string UserName => HttpContext.Session.GetString("UserName") ?? "System";

        public async Task<IActionResult> OnGetAsync(bool? edit)
        {
            if (!CanManageRbac())
            {
                TempData["ErrorMessage"] = "Only an administrator with Settings access can manage RBAC permissions.";
                return RedirectToPage("/UserManagement/Index");
            }

            Matrix = await _rbacService.GetMatrixAsync();
            IsEditMode = edit ?? false;
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (!CanManageRbac())
            {
                TempData["ErrorMessage"] = "Only an administrator with Settings access can manage RBAC permissions.";
                return RedirectToPage("/UserManagement/Index");
            }

            if (Input.Cells.Count == 0)
            {
                TempData["ErrorMessage"] = "No permission cells were submitted.";
                return RedirectToPage("/UserManagement/Permissions");
            }

            try
            {
                await _rbacService.SaveMatrixAsync(Input.Cells, UserName);
                TempData["SuccessMessage"] = "RBAC matrix updated. The new permissions are active immediately.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage("/UserManagement/Permissions");
        }

        public async Task<IActionResult> OnPostResetAsync()
        {
            if (!CanManageRbac())
            {
                TempData["ErrorMessage"] = "Only an administrator with Settings access can manage RBAC permissions.";
                return RedirectToPage("/UserManagement/Index");
            }

            try
            {
                await _rbacService.ResetToDefaultsAsync(UserName);
                TempData["SuccessMessage"] = "RBAC matrix restored to the default access specification.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage("/UserManagement/Permissions");
        }

        private bool CanManageRbac()
        {
            return string.Equals(UserRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase)
                || _rbacCache.CanManage(UserRole, "Settings");
        }
    }
}
