using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using System.Collections.Generic;

namespace Manufacture.Pages.UserManagement
{
    public class PermissionsModel : PageModel
    {
        private readonly MockRbacService _rbacService;

        public PermissionsModel(MockRbacService rbacService)
        {
            _rbacService = rbacService;
        }

        public List<ModulePermission> Matrix { get; set; } = new();
        public bool IsEditMode { get; set; } = false;
        public string[] AllRoles => MockRbacService.AllRoles;
        public string[] PermissionLevels => MockRbacService.PermissionLevels;

        public void OnGet(bool? edit)
        {
            IsEditMode = edit ?? false;
            Matrix = _rbacService.GetMatrix();
        }

        public IActionResult OnPostSave(Dictionary<string, Dictionary<string, string>> matrixInput)
        {
            if (matrixInput != null && matrixInput.Count > 0)
            {
                _rbacService.UpdateMatrix(matrixInput);
                TempData["SuccessMessage"] = "RBAC Matrix permissions updated successfully. Live access privileges are now active across all roles.";
            }
            return RedirectToPage("/UserManagement/Permissions");
        }

        public IActionResult OnPostReset()
        {
            _rbacService.ResetToDefaults();
            TempData["SuccessMessage"] = "RBAC Matrix restored to factory default access specifications.";
            return RedirectToPage("/UserManagement/Permissions");
        }
    }
}
