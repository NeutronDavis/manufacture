using System.ComponentModel.DataAnnotations;

namespace Manufacture.Models.DTOs
{
    public enum ProductRepositoryLocationType
    {
        Factory = 0,
        Mall = 1
    }

    public enum ProductStockMovementType
    {
        ProductionIn = 0,
        TransferOut = 1,
        TransferIn = 2,
        SaleOut = 3,
        DamageOut = 4,
        ReplacementOut = 5,
        AdjustmentIn = 6,
        AdjustmentOut = 7,
        OpeningBalance = 8
    }

    public class ProductRepositoryDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public ProductRepositoryLocationType LocationType { get; set; }
        public string Address { get; set; } = string.Empty;
        public bool IsDefaultProductionOutput { get; set; }
        public bool IsActive { get; set; } = true;
        public int SkuCount { get; set; }
        public decimal TotalUnits { get; set; }
        public int LowStockCount { get; set; }
    }

    public class ProductRepositoryInputDto
    {
        [Required, MaxLength(30)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        public ProductRepositoryLocationType LocationType { get; set; } = ProductRepositoryLocationType.Mall;

        [MaxLength(300)]
        public string Address { get; set; } = string.Empty;

        public bool IsDefaultProductionOutput { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ProductRepositoryStockDto
    {
        public int Id { get; set; }
        public int ProductRepositoryId { get; set; }
        public int RecipeId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string OutputUnit { get; set; } = "units";
        public decimal QuantityOnHand { get; set; }
        public decimal ReorderThreshold { get; set; }
        public decimal SellingPrice { get; set; }
        public bool IsLowStock => QuantityOnHand <= ReorderThreshold;
    }

    public class ProductRepositoryTotalDto
    {
        public int RecipeId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string OutputUnit { get; set; } = "units";
        public decimal TotalQuantityOnHand { get; set; }
        public decimal TotalThreshold { get; set; }
        public int RepositoryCount { get; set; }
        public bool IsLowStock => TotalQuantityOnHand <= TotalThreshold;
    }

    public class ProductStockMovementDto
    {
        public long Id { get; set; }
        public int ProductRepositoryId { get; set; }
        public string RepositoryName { get; set; } = string.Empty;
        public int RecipeId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public ProductStockMovementType MovementType { get; set; }
        public decimal Quantity { get; set; }
        public decimal BalanceAfter { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public int? ReferenceId { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string MovementLabel => MovementType switch
        {
            ProductStockMovementType.ProductionIn => "Production In",
            ProductStockMovementType.TransferOut => "Transfer Out",
            ProductStockMovementType.TransferIn => "Transfer In",
            ProductStockMovementType.SaleOut => "Sale Out",
            ProductStockMovementType.DamageOut => "Damaged",
            ProductStockMovementType.ReplacementOut => "Replacement",
            ProductStockMovementType.AdjustmentIn => "Adjustment In",
            ProductStockMovementType.AdjustmentOut => "Adjustment Out",
            ProductStockMovementType.OpeningBalance => "Opening Balance",
            _ => MovementType.ToString()
        };

        public bool IsInbound => MovementType is ProductStockMovementType.ProductionIn
            or ProductStockMovementType.TransferIn
            or ProductStockMovementType.AdjustmentIn
            or ProductStockMovementType.OpeningBalance;
    }

    public class TransferStockInputDto
    {
        [Range(1, int.MaxValue)]
        public int FromRepositoryId { get; set; }

        [Range(1, int.MaxValue)]
        public int ToRepositoryId { get; set; }

        [Range(1, int.MaxValue)]
        public int RecipeId { get; set; }

        [Range(0.001, 1000000)]
        public decimal Quantity { get; set; }

        [MaxLength(300)]
        public string Notes { get; set; } = string.Empty;
    }

    public class AdjustStockInputDto
    {
        [Range(1, int.MaxValue)]
        public int RepositoryId { get; set; }

        [Range(1, int.MaxValue)]
        public int RecipeId { get; set; }

        [Range(0, 1000000)]
        public decimal NewQuantity { get; set; }

        [Range(0, 1000000)]
        public decimal? ReorderThreshold { get; set; }

        [MaxLength(300)]
        public string Notes { get; set; } = string.Empty;
    }

    public class StockProductOptionDto
    {
        public int RecipeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string OutputUnit { get; set; } = "units";
        public decimal SellingPrice { get; set; }
        public decimal AvailableQuantity { get; set; }
    }
}
