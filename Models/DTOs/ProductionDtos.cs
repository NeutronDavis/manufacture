using System.ComponentModel.DataAnnotations;
using Manufacture.Models.Entities;

namespace Manufacture.Models.DTOs
{
    public class CategoryLabelsDto
    {
        public string InputLabel { get; set; } = "Flour Bags (50kg)";
        public string OutputLabel { get; set; } = "Loaves / Packs";
        public string PlaceholderInput { get; set; } = "e.g., 2 bags";
        public string DefaultOutputUnit { get; set; } = "loaves";
        public string ProductionStandardTip { get; set; } = "Standard yield benchmark is 95 - 105 loaves per 50kg flour bag for Family Loaves, and 130 - 140 loaves for Standard Size.";
    }

    /// <summary>
    /// Batch Production Run Data Contract specified in doc/plan.txt.
    /// </summary>
    public class BatchProductionRunDto
    {
        public string Id { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;               // e.g., "BAT-20261005-01"
        public ProductType ProductCategory { get; set; } = ProductType.Bread;
        public int RecipeId { get; set; }
        public string RecipeName { get; set; } = string.Empty;

        // Dynamic Batch Input Measures (Values depend on ProductCategory)
        public decimal BatchInputQuantity { get; set; } = 1m;                 // e.g., 2 (Bags of Flour / Runs / Cooking Pots)
        public string BatchInputUnitLabel { get; set; } = string.Empty;       // e.g., "50kg Bags of Flour" | "Production Runs" | "Cooking Pots"

        // Targets & Real Output
        public int TargetOrderQuantity { get; set; }                          // Sourced from Sales Rep Orders Placed
        public int? LinkedOrderId { get; set; }
        public string? LinkedOrderNumber { get; set; }
        public string? CustomerName { get; set; }
        public int ActualYieldQuantity { get; set; }                          // Entered by Production Manager
        public string OutputUnitLabel { get; set; } = string.Empty;          // "Loaves" | "Dispenser Bottles" | "Racks"

        // Dynamically Computed Costs
        public decimal TotalBatchMaterialCost { get; set; }                   // Calculated: SUM(Recipe Ingredient Qty * Master Price) * batchInputQuantity
        public decimal ActualUnitProductionCost { get; set; }                 // Calculated: totalBatchMaterialCost / actualYieldQuantity
        public decimal ExpectedUnitProductionCost { get; set; }               // Calculated: totalBatchMaterialCost / expectedYield
        public decimal CostVariancePerUnit => ActualUnitProductionCost - ExpectedUnitProductionCost;

        public string Status { get; set; } = "COMPLETED";
        public string LoggedBy { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Shift { get; set; } = "Morning";
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Postback for manually creating a planned or logged production batch.
    /// </summary>
    public class ProductionBatchDto
    {
        public int Id { get; set; }

        public ProductType ProductCategory { get; set; } = ProductType.Bread;

        [Required]
        public int RecipeId { get; set; }

        public string RecipeName { get; set; } = string.Empty;

        [Required, Range(1, 100000)]
        public int TargetQuantity { get; set; }

        [Range(0, 100000)]
        public int ActualQuantity { get; set; }

        public string Shift { get; set; } = "Morning Shift";

        [Required]
        public string BakerInCharge { get; set; } = string.Empty;

        [Range(0.01, 10000)]
        public decimal BatchInputQuantity { get; set; } = 1.0m;

        public string BatchInputUnitLabel { get; set; } = "Flour Bags (50kg)";

        public string OutputUnitLabel { get; set; } = "loaves";

        public int TargetOrderQuantity { get; set; }
        public int? LinkedOrderId { get; set; }
        public string? LinkedOrderNumber { get; set; }
        public string? CustomerName { get; set; }

        /// <summary>Legacy field kept in sync with BatchInputQuantity for bread batches.</summary>
        public decimal FlourBagsUsed
        {
            get => BatchInputQuantity;
            set => BatchInputQuantity = value;
        }

        public string Status { get; set; } = "Completed";

        public DateTime ProductionDate { get; set; } = DateTime.UtcNow;

        public string Notes { get; set; } = string.Empty;
    }
}
