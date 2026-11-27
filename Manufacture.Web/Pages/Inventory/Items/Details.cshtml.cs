using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Services;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Inventory.Items
{
    public class DetailsModel : PageModel
    {
        private readonly IInventoryService _inventoryService;

        public DetailsModel(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        public InventoryItemDto Item { get; set; } = default!;
        public List<StoreRequestDto> ItemRequests { get; set; } = new();
        public List<InventoryRecipeUsageDto> AssociatedRecipes { get; set; } = new();
        public List<RequisitionHistoryRow> RequisitionHistory { get; set; } = new();
        public int UserRoleId { get; set; }
        public bool CanManageStock =>
            UserRoleId != (int)UserRole.ProductionManager;
        public decimal TotalStockValuation => Item != null ? Item.QuantityInStock * Item.UnitCostPrice : 0m;
        public decimal StockBaseQuantity => Item != null ? Item.QuantityInStock * Item.PackageSize : 0m;
        public decimal UnitBaseCost => Item != null && Item.PackageSize > 0m
            ? Item.UnitCostPrice / Item.PackageSize
            : Item?.UnitCostPrice ?? 0m;

        public class RequisitionHistoryRow
        {
            public int RequestId { get; set; }
            public string RequestNumber { get; set; } = string.Empty;
            public string RequestedBy { get; set; } = string.Empty;
            public string Department { get; set; } = string.Empty;
            public string Purpose { get; set; } = string.Empty;
            public DateTime RequestedAt { get; set; }
            public StoreRequestStatus Status { get; set; }
            public decimal RequestedQuantity { get; set; }
            public string RequestedUnit { get; set; } = string.Empty;
            public decimal BaseQuantity { get; set; }
            public string BaseUnit { get; set; } = string.Empty;
            public decimal StockQuantity { get; set; }
            public string StockUnit { get; set; } = string.Empty;
            public decimal IssuedQuantity { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _inventoryService.GetItemAsync(id, cancellationToken);
            if (item == null)
            {
                TempData["ErrorMessage"] = "Warehouse inventory item not found.";
                return RedirectToPage("/Inventory/Index");
            }

            Item = item;
            UserRoleId = HttpContext.Session.GetRoleId();
            ItemRequests = await _inventoryService.GetRequestsByItemAsync(id, cancellationToken);
            RequisitionHistory = ItemRequests
                .SelectMany(request => request.Items
                    .Where(line => line.InventoryItemId == id)
                    .Select(line => new RequisitionHistoryRow
                    {
                        RequestId = request.Id,
                        RequestNumber = request.RequestNumber,
                        RequestedBy = request.RequestedBy,
                        Department = request.Department,
                        Purpose = request.Purpose,
                        RequestedAt = request.RequestedAt,
                        Status = request.Status,
                        RequestedQuantity = line.RequestedQuantity,
                        RequestedUnit = line.Unit,
                        BaseQuantity = line.BaseQuantity,
                        BaseUnit = line.BaseUnit,
                        StockQuantity = line.StockQuantity,
                        StockUnit = line.StockUnit,
                        IssuedQuantity = line.IssuedQuantity
                    }))
                .OrderByDescending(row => row.RequestedAt)
                .ThenByDescending(row => row.RequestId)
                .ToList();
            AssociatedRecipes = await _inventoryService.GetRecipeUsageAsync(id, cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostRestockAsync(
            int id,
            decimal addedQuantity,
            CancellationToken cancellationToken = default)
        {
            UserRoleId = HttpContext.Session.GetRoleId();

            if (!CanManageStock)
            {
                TempData["ErrorMessage"] = "Production Managers cannot restock inventory items.";
                return RedirectToPage(new { id });
            }

            if (addedQuantity <= 0)
            {
                TempData["ErrorMessage"] = "Restock quantity must be greater than zero.";
                return RedirectToPage(new { id });
            }

            var item = await _inventoryService.GetItemAsync(id, cancellationToken);
            if (item == null)
            {
                TempData["ErrorMessage"] = "Item not found.";
                return RedirectToPage("/Inventory/Index");
            }

            var userName = HttpContext.Session.GetString("UserName") ?? "Store Manager";
            var restocked = await _inventoryService.RestockItemAsync(id, addedQuantity, userName, cancellationToken);
            if (!restocked)
            {
                TempData["ErrorMessage"] = "The inventory item could not be restocked.";
                return RedirectToPage(new { id });
            }

            var updated = await _inventoryService.GetItemAsync(id, cancellationToken);
            TempData["SuccessMessage"] = $"Stock replenished: Added +{addedQuantity:N0} {item.Unit} to '{item.Name}'. New balance: {updated?.QuantityInStock ?? item.QuantityInStock:N0} {item.Unit}.";
            return RedirectToPage(new { id });
        }
    }
}
