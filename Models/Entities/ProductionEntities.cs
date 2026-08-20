namespace Manufacture.Models.Entities
{
    public class Ingredient
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = "kg"; // kg, litres, bags, pcs
        public decimal UnitCost { get; set; } // cost per unit (e.g. per kg or bag)
        public decimal CurrentStock { get; set; }
    }

    public class RecipeIngredient
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal QuantityPerUnit { get; set; } // quantity needed for 1 finished product
        public decimal IngredientUnitCost { get; set; }
        public decimal TotalCost => QuantityPerUnit * IngredientUnitCost;
    }

    public class Recipe
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // e.g. "Jumbo Bread", "Medium Loaf", "61 Bread"
        public string Code { get; set; } = string.Empty; // e.g. "BRD-JMB"
        public string Description { get; set; } = string.Empty;
        public decimal PackagingCost { get; set; } // nylon wrapper / tag cost per unit
        public decimal LaborAndOverheadPerUnit { get; set; }
        public decimal SellingPrice { get; set; }
        public List<RecipeIngredient> Ingredients { get; set; } = new();

        public decimal IngredientsCost => Ingredients.Sum(i => i.TotalCost);
        public decimal TotalUnitCost => IngredientsCost + PackagingCost + LaborAndOverheadPerUnit;
        public decimal MarginAmount => SellingPrice - TotalUnitCost;
        public decimal MarginPercentage => SellingPrice > 0 ? Math.Round((MarginAmount / SellingPrice) * 100, 2) : 0;
        public bool IsActive { get; set; } = true;
    }

    public enum BatchStatus
    {
        Planned,
        InMixing,
        Baking,
        Completed,
        Cancelled
    }

    public class ProductionBatch
    {
        public int Id { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int RecipeId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public int TargetQuantity { get; set; } // from aggregated pre-orders or manager plan
        public int ActualQuantity { get; set; } // completed loaves/bags
        public int Variance => ActualQuantity - TargetQuantity; // e.g. +2 extra loaves or -3 short
        public string Shift { get; set; } = "Morning"; // Morning / Night
        public string BakerInCharge { get; set; } = string.Empty;
        public decimal FlourBagsUsed { get; set; } // e.g. 3.5 bags
        public BatchStatus Status { get; set; } = BatchStatus.Planned;
        public DateTime ProductionDate { get; set; } = DateTime.UtcNow;
        public string Notes { get; set; } = string.Empty;
    }
}
