using System.ComponentModel.DataAnnotations;

namespace Manufacture.Models.DTOs
{
    public class RecipeIngredientDto
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = "kg";
        public decimal QuantityPerUnit { get; set; }
        public decimal IngredientUnitCost { get; set; }
    }

    public class RecipeDto
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [Range(0, 100000)]
        public decimal PackagingCost { get; set; }
        [Range(0, 100000)]
        public decimal LaborAndOverheadPerUnit { get; set; }
        [Required, Range(1, 1000000)]
        public decimal SellingPrice { get; set; }
        public List<RecipeIngredientDto> Ingredients { get; set; } = new();
    }

    public class ProductionBatchDto
    {
        public int Id { get; set; }
        [Required]
        public int RecipeId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        [Required, Range(1, 100000)]
        public int TargetQuantity { get; set; }
        [Range(0, 100000)]
        public int ActualQuantity { get; set; }
        public string Shift { get; set; } = "Morning";
        [Required]
        public string BakerInCharge { get; set; } = string.Empty;
        public decimal FlourBagsUsed { get; set; }
        public string Status { get; set; } = "Completed";
        public DateTime ProductionDate { get; set; } = DateTime.UtcNow;
        public string Notes { get; set; } = string.Empty;
    }
}
