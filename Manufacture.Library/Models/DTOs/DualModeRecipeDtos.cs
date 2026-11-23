using System.Text.Json.Serialization;

namespace Manufacture.Models.DTOs
{
    /// <summary>
    /// Supported entry modes for recipe formulation and costing.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<RecipeEntryMode>))]
    public enum RecipeEntryMode
    {
        /// <summary>Top-Down: Enter 50kg batch qty -> auto-calculate per-unit.</summary>
        [JsonStringEnumMemberName("BATCH_FIRST")]
        BatchFirst,

        /// <summary>Bottom-Up: Enter per-unit qty -> auto-calculate batch qty.</summary>
        [JsonStringEnumMemberName("SINGLE_UNIT")]
        SingleUnit
    }

    /// <summary>
    /// Dual-mode ingredient contract. Both batchQuantity and perUnitQuantity
    /// are calculated and maintained regardless of entry mode.
    /// </summary>
    public class RecipeIngredientDto
    {
        [JsonPropertyName("ingredientId")]
        public string IngredientId { get; set; } = string.Empty;

        [JsonPropertyName("ingredientName")]
        public string IngredientName { get; set; } = string.Empty;

        [JsonPropertyName("unitOfMeasure")]
        public string UnitOfMeasure { get; set; } = "kg"; // 'kg', 'g', 'L', 'ml'

        [JsonPropertyName("currentUnitCost")]
        public decimal CurrentUnitCost { get; set; } // Price per unit of measure (e.g. ₦1,000 per kg)

        // Quantities (Both are maintained in DTO regardless of entry mode)
        [JsonPropertyName("batchQuantity")]
        public decimal BatchQuantity { get; set; } // Total qty for 50kg flour bag / water run / popcorn pot

        [JsonPropertyName("perUnitQuantity")]
        public decimal PerUnitQuantity { get; set; } // Calculated or manually entered qty per single loaf/item

        // Calculated Costs
        [JsonPropertyName("batchIngredientCost")]
        public decimal BatchIngredientCost { get; set; } // batchQuantity * currentUnitCost

        [JsonPropertyName("perUnitIngredientCost")]
        public decimal PerUnitIngredientCost { get; set; } // perUnitQuantity * currentUnitCost
    }

    /// <summary>
    /// Dual-mode recipe contract supporting Top-Down (Batch First) and Bottom-Up (Single Unit) costing.
    /// </summary>
    public class RecipeDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("productCategory")]
        public string ProductCategory { get; set; } = "BREAD"; // 'BREAD' | 'WATER' | 'POPCORN'

        [JsonPropertyName("baselineUnitLabel")]
        public string BaselineUnitLabel { get; set; } = string.Empty; // e.g., "1 Bag (50kg Flour)", "1 Dispenser Run", "1 Cooking Pot"

        [JsonPropertyName("entryMode")]
        public RecipeEntryMode EntryMode { get; set; } = RecipeEntryMode.BatchFirst; // BATCH_FIRST or SINGLE_UNIT

        [JsonPropertyName("expectedBatchYield")]
        public decimal ExpectedBatchYield { get; set; } // e.g., 50 loaves per batch

        [JsonPropertyName("ingredients")]
        public List<RecipeIngredientDto> Ingredients { get; set; } = new();

        // Aggregate Totals (Computed by Mock Service / DTO helper)
        [JsonPropertyName("totalBatchCost")]
        public decimal TotalBatchCost { get; set; } // SUM(batchIngredientCost)

        [JsonPropertyName("calculatedUnitProductionCost")]
        public decimal CalculatedUnitProductionCost { get; set; } // totalBatchCost / expectedBatchYield
    }

    /// <summary>Type alias matching uppercase TypeScript specification RecipeIngredientDTO.</summary>
    public class RecipeIngredientDTO : RecipeIngredientDto { }

    /// <summary>Type alias matching uppercase TypeScript specification RecipeDTO.</summary>
    public class RecipeDTO : RecipeDto { }
}
