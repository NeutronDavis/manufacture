namespace Manufacture.Models.Entities
{
    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SupplyCategory { get; set; } = "Flour & Grains"; // Packaging, Flour & Grains, Sweeteners & Yeast, Fats & Oils
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class InventoryItem
    {
        public int Id { get; set; }
        public string ItemCode { get; set; } = string.Empty; // RAW-FLR-01
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "Raw Materials"; // Raw Materials, Packaging, Additives, Fuel
        public string Unit { get; set; } = "Bags (50kg)"; // Bags, kg, Litres, Bundles, Pieces
        public decimal QuantityInStock { get; set; }
        public decimal ReorderThreshold { get; set; }
        public decimal UnitCostPrice { get; set; }
        public int? PreferredSupplierId { get; set; }
        public string? PreferredSupplierName { get; set; }
        public DateTime LastRestockedDate { get; set; } = DateTime.UtcNow;
        public bool IsLowStock => QuantityInStock <= ReorderThreshold;
    }

    public enum StoreRequestStatus
    {
        Pending,
        Approved,
        Rejected,
        Issued
    }

    public class StoreRequestItem
    {
        public int Id { get; set; }
        public int StoreRequestId { get; set; }
        public int InventoryItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal RequestedQuantity { get; set; }
        public decimal IssuedQuantity { get; set; }
    }

    public class StoreRequest
    {
        public int Id { get; set; }
        public string RequestNumber { get; set; } = string.Empty; // REQ-2026-001
        public string RequestedBy { get; set; } = string.Empty; // Production Manager
        public string Department { get; set; } = "Bakery Floor";
        public string Purpose { get; set; } = string.Empty; // e.g. "Morning Shift Batch #102 Jumbo Loaves"
        public List<StoreRequestItem> Items { get; set; } = new();
        public StoreRequestStatus Status { get; set; } = StoreRequestStatus.Pending;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }
    }
}
