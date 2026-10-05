using System.ComponentModel.DataAnnotations;
using Manufacture.Models.Entities;

namespace Manufacture.Models.DTOs
{
    // =====================================================================
    // INGREDIENT PRICE MASTER
    // =====================================================================

    /// <summary>
    /// A raw material line shown on the admin price &amp; margin dashboard, including
    /// the live ingredient price and how many recipes currently consume it.
    /// </summary>
    public class IngredientPriceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>Pricing unit of the material (kg, litres, pcs).</summary>
        public string Unit { get; set; } = "kg";

        public string Category { get; set; } = "Raw Materials";
        public decimal CurrentUnitCost { get; set; }
        public decimal CurrentStock { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>Number of active recipes that consume this material.</summary>
        public int UsedByRecipeCount { get; set; }

        /// <summary>True when the price is stale beyond <c>StaleAfterDays</c>.</summary>
        public bool IsPriceStale { get; set; }
    }

    /// <summary>Postback for a single ingredient price edit from the admin dashboard.</summary>
    public class UpdateIngredientPriceDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select a valid ingredient.")]
        public int IngredientId { get; set; }

        [Range(0, 100_000_000, ErrorMessage = "Unit cost cannot be negative.")]
        public decimal NewUnitCost { get; set; }
    }

    /// <summary>A batch of ingredient price edits applied together from the admin table.</summary>
    public class BulkPriceUpdateDto
    {
        public List<UpdateIngredientPriceDto> Updates { get; set; } = new();
    }

    // =====================================================================
    // RECIPE BUILDER
    // =====================================================================

    /// <summary>Describes a selectable baseline unit, used to drive the builder UI.</summary>
    public class BaselineOptionDto
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OutputUnit { get; set; } = "loaves";
        public decimal SuggestedYield { get; set; }

        /// <summary>Units the builder should offer for quantity entry, e.g. kg, g.</summary>
        public List<string> SuggestedQuantityUnits { get; set; } = new();
    }

    /// <summary>
    /// One ingredient row inside the recipe builder form. Quantities are entered
    /// against the recipe's baseline unit; the costing engine normalises them to
    /// each ingredient's pricing unit.
    /// </summary>
    public class RecipeIngredientInputDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select a raw material.")]
        public int IngredientId { get; set; }

        public string IngredientName { get; set; } = string.Empty;

        /// <summary>Pricing unit of the underlying ingredient (kg, litres, pcs).</summary>
        public string IngredientUnit { get; set; } = "kg";

        /// <summary>The unit the entered quantity is expressed in (kg, g, litres, ml, pcs).</summary>
        public string QuantityUnit { get; set; } = "kg";

        [Range(0, 1_000_000, ErrorMessage = "Batch quantity cannot be negative.")]
        public decimal QuantityPerBatch { get; set; }

        /// <summary>Quantity per single unit (used in SingleUnit / Bottom-Up entry mode).</summary>
        public decimal QuantityPerUnit { get; set; }

        /// <summary>Optional live price override for this line; falls back to the master price.</summary>
        public decimal? UnitCostOverride { get; set; }
    }

    /// <summary>Postback payload for creating a recipe from the builder form.</summary>
    public class RecipeBuilderDto
    {
        [Range(0, int.MaxValue)]
        public int Id { get; set; }

        /// <summary>Dual-mode entry mode: BatchFirst (Top-Down) or SingleUnit (Bottom-Up).</summary>
        public RecipeEntryMode EntryMode { get; set; } = RecipeEntryMode.BatchFirst;

        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(120, MinimumLength = 2, ErrorMessage = "Product name must be 2-120 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product code is required.")]
        [StringLength(40, ErrorMessage = "Product code is too long.")]
        public string Code { get; set; } = string.Empty;

        [StringLength(400)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Select a product type.")]
        public string ProductType { get; set; } = "Bread";

        [Required(ErrorMessage = "Select a baseline production unit.")]
        public string BaselineUnit { get; set; } = "FlourBag50Kg";

        [Range(1, 1_000_000, ErrorMessage = "Expected yield must be at least 1 unit per batch.")]
        public decimal ExpectedYield { get; set; } = 200m;

        [StringLength(24)]
        public string OutputUnit { get; set; } = "loaves";

        [Range(0, 100_000)]
        public decimal PackagingCost { get; set; }

        [Range(0, 100_000)]
        public decimal LaborAndOverheadPerUnit { get; set; }

        [Required(ErrorMessage = "Selling price is required.")]
        [Range(1, 100_000_000, ErrorMessage = "Selling price must be greater than zero.")]
        public decimal SellingPrice { get; set; }

        public List<RecipeIngredientInputDto> Ingredients { get; set; } = new();
    }

    // =====================================================================
    // COSTING ENGINE OUTPUT
    // =====================================================================

    /// <summary>A single material's cost contribution, at batch and per-unit level.</summary>
    public class RecipeCostLineDto
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        /// <summary>Quantity consumed by one baseline batch (normalised to <see cref="Unit"/>).</summary>
        public decimal QuantityPerBatch { get; set; }

        /// <summary>Quantity consumed per single finished unit.</summary>
        public decimal QuantityPerUnit { get; set; }

        /// <summary>Live unit cost resolved from the ingredient master.</summary>
        public decimal UnitCost { get; set; }

        /// <summary>QuantityPerBatch * UnitCost.</summary>
        public decimal BatchLineCost { get; set; }

        /// <summary>QuantityPerUnit * UnitCost.</summary>
        public decimal UnitLineCost { get; set; }

        /// <summary>Share of total batch cost, 0-100.</summary>
        public decimal ShareOfBatchPercent { get; set; }
    }

    /// <summary>
    /// Full costing result for a recipe. Mirrors the spec formulas:
    /// <c>TotalBatchCost = SUM(IngredientBatchQty * CurrentUnitPrice)</c>,
    /// <c>ActualUnitCost = TotalBatchCost / ActualYield</c>,
    /// <c>ProfitMargin = SellingPrice - ActualUnitCost</c>.
    /// </summary>
    public class RecipeCostingResultDto
    {
        public int RecipeId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public string RecipeCode { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string BaselineUnit { get; set; } = string.Empty;
        public string BaselineUnitName { get; set; } = string.Empty;
        public string OutputUnit { get; set; } = "loaves";

        /// <summary>
        /// Singular form of <see cref="OutputUnit"/> for per-unit labels, e.g.
        /// "loaves" becomes "loaf" and "bottles" becomes "bottle".
        /// </summary>
        public string SingularOutputUnit => Singularize(OutputUnit);

        /// <summary>
        /// Irregular plurals used as output units in this app. English "-ves"
        /// plurals are not mechanically reversible ("loaves" to "loaf" needs a
        /// letter replaced, not just removed), so they are listed explicitly.
        /// </summary>
        private static readonly Dictionary<string, string> IrregularPlurals =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["loaves"] = "loaf",
                ["leaves"] = "leaf",
                ["knives"] = "knife",
                ["wives"] = "wife"
            };

        /// <summary>
        /// Reduces a plural output unit to its singular form for per-unit labels,
        /// e.g. "loaves" becomes "loaf" and "bottles" becomes "bottle". Unrecognised
        /// words are returned unchanged rather than mangled.
        /// </summary>
        public static string Singularize(string? plural)
        {
            if (string.IsNullOrWhiteSpace(plural)) return "unit";
            var value = plural.Trim();

            if (IrregularPlurals.TryGetValue(value, out var irregular))
                return irregular;

            if (value.EndsWith("s", StringComparison.OrdinalIgnoreCase) && value.Length > 1)
                return value[..^1];                       // bottles -> bottle

            return value;                                 // "loaf" stays "loaf"
        }

        /// <summary>The recipe's planned expected yield.</summary>
        public decimal ExpectedYield { get; set; }

        /// <summary>
        /// The yield actually used for this calculation. Equal to
        /// <see cref="ExpectedYield"/> unless the production manager entered a
        /// real daily yield.
        /// </summary>
        public decimal EffectiveYield { get; set; }

        /// <summary>True when <see cref="EffectiveYield"/> came from real logged output.</summary>
        public bool UsesActualYield { get; set; }

        public List<RecipeCostLineDto> Lines { get; set; } = new();

        // ---- Batch level ----
        public decimal TotalBatchCost { get; set; }
        public decimal BatchYieldVarianceUnits { get; set; }
        public decimal BatchYieldVariancePercent { get; set; }

        // ---- Per unit level ----
        public decimal IngredientsCostPerUnit { get; set; }
        public decimal PackagingCost { get; set; }
        public decimal LaborAndOverheadPerUnit { get; set; }
        public decimal TotalUnitCost { get; set; }

        // ---- Profitability ----
        public decimal SellingPrice { get; set; }
        public decimal MarginAmount { get; set; }
        public decimal MarginPercentage { get; set; }

        // ---- Health flags for the dashboard ----
        public bool HasNegativeMargin => MarginAmount < 0;
        public bool HasThinMargin => MarginAmount >= 0 && MarginPercentage < 15m;
        public bool HasMissingPrice => Lines.Any(l => l.UnitCost <= 0);
    }

    // =====================================================================
    // DAILY PRODUCTION ENTRY
    // =====================================================================

    /// <summary>Postback for the daily production entry modal (spec UI #2).</summary>
    public class DailyProductionEntryDto
    {
        [Required, Range(1, int.MaxValue, ErrorMessage = "Select a recipe.")]
        public int RecipeId { get; set; }

        public ProductType ProductCategory { get; set; } = ProductType.Bread;

        [Range(0.01, 10000)]
        public decimal BatchInputQuantity { get; set; } = 1.0m;

        public string BatchInputUnitLabel { get; set; } = "Flour Bags (50kg)";

        public int TargetOrderQuantity { get; set; }
        public int? LinkedOrderId { get; set; }
        public string? LinkedOrderNumber { get; set; }
        public string? CustomerName { get; set; }

        [Required, Range(1, 1_000_000, ErrorMessage = "Actual yield must be at least 1 unit.")]
        public int ActualYield { get; set; }

        public string Shift { get; set; } = "Morning Shift";
        public string BakerInCharge { get; set; } = string.Empty;

        public decimal FlourBagsUsed
        {
            get => BatchInputQuantity;
            set => BatchInputQuantity = value;
        }

        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Live preview shown in the daily production entry modal before submitting,
    /// so the manager sees real-time batch and per-unit cost as they type the yield.
    /// </summary>
    public class DailyProductionPreviewDto
    {
        public bool IsValid { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public string BaselineUnitName { get; set; } = string.Empty;
        public string OutputUnit { get; set; } = "loaves";

        /// <summary>Singular form of <see cref="OutputUnit"/> for per-unit labels.</summary>
        public string SingularOutputUnit => RecipeCostingResultDto.Singularize(OutputUnit);

        public decimal BatchInputQuantity { get; set; } = 1m;
        public string BatchInputUnitLabel { get; set; } = string.Empty;
        public int TargetOrderQuantity { get; set; }

        public decimal ExpectedYield { get; set; }
        public decimal ActualYield { get; set; }
        public decimal TotalBatchCost { get; set; }
        public decimal UnitCostAtActualYield { get; set; }
        public decimal UnitCostAtExpectedYield { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal MarginAmount { get; set; }
        public decimal MarginPercentage { get; set; }
        public decimal YieldVarianceUnits { get; set; }
    }

    // =====================================================================
    // PROFITABILITY REPORT (spec GET /api/reports/profitability)
    // =====================================================================

    /// <summary>Portfolio-level profitability roll-up for the admin dashboard.</summary>
    public class ProfitabilityReportDto
    {
        public List<RecipeCostingResultDto> Rows { get; set; } = new();

        public decimal TotalBatchCost { get; set; }
        public decimal TotalExpectedRevenue { get; set; }
        public decimal TotalExpectedMargin { get; set; }
        public decimal WeightedMarginPercentage { get; set; }
        public int NegativeMarginCount { get; set; }
        public int ThinMarginCount { get; set; }
        public int MissingPriceCount { get; set; }
    }
}
