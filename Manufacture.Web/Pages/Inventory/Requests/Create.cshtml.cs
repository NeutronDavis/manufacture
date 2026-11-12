using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Inventory.Requests
{
    public class CreateModel : PageModel
    {
        private readonly IInventoryService _inventoryService;

        public CreateModel(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [BindProperty]
        public StoreRequestDto RequestInput { get; set; } = new();

        public List<InventoryItemDto> AvailableItems { get; set; } = new();

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            await LoadItemsAsync(cancellationToken);
            RequestInput.RequestedBy = HttpContext.Session.GetString("UserName") ?? "Chidinma Okoro (Production Mgr)";
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                await LoadItemsAsync(cancellationToken);
                return Page();
            }

            RequestInput.Items = RequestInput.Items.Where(i => i.RequestedQuantity > 0).ToList();
            if (!RequestInput.Items.Any())
            {
                ModelState.AddModelError(string.Empty, "Please enter a quantity for at least one item.");
                await LoadItemsAsync(cancellationToken);
                return Page();
            }

            try
            {
                var req = await _inventoryService.CreateRequestAsync(RequestInput, cancellationToken);
                TempData["SuccessMessage"] = $"Store request '{req.RequestNumber}' submitted for approval.";
                return RedirectToPage("/Inventory/Requests/Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadItemsAsync(cancellationToken);
                return Page();
            }
        }

        private async Task LoadItemsAsync(CancellationToken cancellationToken)
        {
            AvailableItems = await _inventoryService.GetItemsAsync(cancellationToken: cancellationToken);
        }
    }
}
