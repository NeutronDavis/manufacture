using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Inventory.Items
{
    public class CreateModel : PageModel
    {
        private readonly IInventoryService _inventoryService;
        private readonly IMeasurementDensityService _measurementService;

        public CreateModel(IInventoryService inventoryService, IMeasurementDensityService measurementService)
        {
            _inventoryService = inventoryService;
            _measurementService = measurementService;
        }

        [BindProperty]
        public InventoryItemDto ItemInput { get; set; } = new();

        public List<SupplierDto> Suppliers { get; set; } = new();
        public List<MeasurementDensityDto> MeasurementDensities { get; set; } = new();

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            await LoadFormDataAsync(cancellationToken);
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                await LoadFormDataAsync(cancellationToken);
                return Page();
            }

            try
            {
                var userName = HttpContext.Session.GetString("UserName") ?? "Store Manager";
                var item = await _inventoryService.CreateItemAsync(ItemInput, userName, cancellationToken);
                TempData["SuccessMessage"] = $"Stock item '{item.Name}' added to warehouse inventory with code {item.ItemCode}.";
                return RedirectToPage("/Inventory/Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadFormDataAsync(cancellationToken);
                return Page();
            }
        }

        private async Task LoadFormDataAsync(CancellationToken cancellationToken)
        {
            Suppliers = await _inventoryService.GetSuppliersAsync(cancellationToken: cancellationToken);
            MeasurementDensities = await _measurementService.GetAllAsync(includeInactive: false, cancellationToken);
        }
    }
}
