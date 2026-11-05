using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Inventory.Requests
{
    public class IndexModel : PageModel
    {
        private readonly MockInventoryService _inventoryService;

        public IndexModel(MockInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        public List<StoreRequest> Requests { get; set; } = new();

        public void OnGet()
        {
            Requests = _inventoryService.GetAllRequests();
        }

        public IActionResult OnPostApprove(int id)
        {
            var userName = HttpContext.Session.GetString("UserName") ?? "Musa Ibrahim (Store Mgr)";
            _inventoryService.ApproveAndIssueRequest(id, userName);
            TempData["SuccessMessage"] = "Store release approved. Stock deducted from warehouse.";
            return RedirectToPage();
        }

        public IActionResult OnPostReject(int id)
        {
            var userName = HttpContext.Session.GetString("UserName") ?? "Musa Ibrahim (Store Mgr)";
            _inventoryService.RejectRequest(id, userName, "Insufficient raw stock / Pending supply");
            TempData["ErrorMessage"] = "Store release request rejected.";
            return RedirectToPage();
        }
    }
}
