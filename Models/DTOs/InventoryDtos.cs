using System.ComponentModel.DataAnnotations;

namespace Manufacture.Models.DTOs
{
    public class SupplierDto
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string ContactPerson { get; set; } = string.Empty;
        [Required, Phone]
        public string PhoneNumber { get; set; } = string.Empty;
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string SupplyCategory { get; set; } = "Flour & Grains";
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class InventoryItemDto
    {
        public int Id { get; set; }
        [Required]
        public string ItemCode { get; set; } = string.Empty;
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Category { get; set; } = "Raw Materials";
        [Required]
        public string Unit { get; set; } = "Bags (50kg)";
        [Range(0, 1000000)]
        public decimal QuantityInStock { get; set; }
        [Range(0, 1000000)]
        public decimal ReorderThreshold { get; set; }
        [Range(0, 10000000)]
        public decimal UnitCostPrice { get; set; }
        public int? PreferredSupplierId { get; set; }
    }

    public class StoreRequestItemDto
    {
        public int InventoryItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal RequestedQuantity { get; set; }
    }

    public class StoreRequestDto
    {
        public int Id { get; set; }
        [Required]
        public string RequestedBy { get; set; } = string.Empty;
        public string Department { get; set; } = "Bakery Floor";
        [Required]
        public string Purpose { get; set; } = string.Empty;
        public List<StoreRequestItemDto> Items { get; set; } = new();
    }
}
