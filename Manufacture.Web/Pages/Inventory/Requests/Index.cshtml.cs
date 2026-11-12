using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Inventory.Requests
{
    public class IndexModel : PageModel
    {
        private readonly IInventoryService _inventoryService;

        public IndexModel(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        public List<StoreRequestDto> Requests { get; set; } = new();

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            Requests = await _inventoryService.GetRequestsAsync(cancellationToken);
        }

        public async Task<IActionResult> OnPostApproveAsync(int id, CancellationToken cancellationToken = default)
        {
            var userName = HttpContext.Session.GetString("UserName") ?? "Musa Ibrahim (Store Mgr)";

            try
            {
                var approved = await _inventoryService.ApproveAndIssueRequestAsync(id, userName, cancellationToken);
                TempData[approved ? "SuccessMessage" : "ErrorMessage"] = approved
                    ? "Store release approved. Stock deducted from warehouse."
                    : "The store release could not be approved. It may already have been processed.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int id, CancellationToken cancellationToken = default)
        {
            var userName = HttpContext.Session.GetString("UserName") ?? "Musa Ibrahim (Store Mgr)";
            var rejected = await _inventoryService.RejectRequestAsync(
                id,
                userName,
                "Insufficient raw stock / Pending supply",
                cancellationToken);
            TempData[rejected ? "ErrorMessage" : "SuccessMessage"] = rejected
                ? "Store release request rejected."
                : "The store release could not be rejected. It may already have been processed.";
            return RedirectToPage();
        }
    }
}
