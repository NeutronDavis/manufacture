using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Inventory.Requests
{
    public class CreateModel : PageModel
    {
        private readonly MockInventoryService _inventoryService;

        public CreateModel(MockInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [BindProperty]
        public StoreRequestDto RequestInput { get; set; } = new();

        public List<InventoryItem> AvailableItems { get; set; } = new();

        public void OnGet()
        {
            AvailableItems = _inventoryService.GetAllItems();
            RequestInput.RequestedBy = HttpContext.Session.GetString("UserName") ?? "Chidinma Okoro (Production Mgr)";
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                AvailableItems = _inventoryService.GetAllItems();
                return Page();
            }

            RequestInput.Items = RequestInput.Items.Where(i => i.RequestedQuantity > 0).ToList();
            if (!RequestInput.Items.Any())
            {
                ModelState.AddModelError(string.Empty, "Please enter a quantity for at least one item.");
                AvailableItems = _inventoryService.GetAllItems();
                return Page();
            }

            var req = _inventoryService.CreateRequest(RequestInput);
            TempData["SuccessMessage"] = $"Store request '{req.RequestNumber}' submitted for approval.";
            return RedirectToPage("/Inventory/Requests/Index");
        }
    }
}
