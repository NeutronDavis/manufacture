namespace Manufacture.Models.Entities
{
    /// <summary>
    /// Product families handled by the costing engine. Each family has its own
    /// baseline production unit (see <see cref="BaselineUnit"/>).
    /// </summary>
    public enum ProductType
    {
        /// <summary>Bread &amp; confectionery — baseline is one 50kg bag of flour.</summary>
        Bread = 0,

        /// <summary>18.9L dispenser water — baseline is one full production run.</summary>
        Water = 1,

        /// <summary>Popcorn — baseline is one standard cooking pot.</summary>
        Popcorn = 2
    }

    /// <summary>
    /// The baseline production unit that ingredient quantities in a recipe are
    /// expressed relative to. See doc/recipe-costing.md section 1.A / 1.B.
    /// </summary>
    public enum BaselineUnit
    {
        /// <summary>1 bag of wheat flour (50 kg) — industrial mixer standard.</summary>
        FlourBag50Kg = 0,

        /// <summary>One full dispenser water run (~18.9L bottles).</summary>
        WaterRun18_9L = 1,

        /// <summary>One standard popcorn cooking pot (~2kg corn + oil + seasoning).</summary>
        PopcornPot = 2
    }

    /// <summary>
    /// Master raw-material record. <see cref="Unit"/> is the unit the material is
    /// PRICED in (kg, litres, pcs, ...) and <see cref="UnitCost"/> is the cost of
    /// one such unit. Recipe quantities in other units (g, ml, bags) are normalised
    /// to this pricing unit by <c>UnitConverter</c> before costing.
    /// </summary>
    public class Ingredient
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = "kg"; // pricing unit: kg, litres, pcs
        public decimal UnitCost { get; set; } // cost per one <see cref="Unit"/>
        public decimal CurrentStock { get; set; }
        public string Category { get; set; } = "Raw Materials";

        /// <summary>Tracks the last vendor price change so the costing admin
        /// dashboard can show how recently a price was refreshed.</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// One ingredient line inside a recipe.
    /// <para>
    /// <see cref="QuantityPerBatch"/> is the single source of truth and is ALWAYS
    /// stored normalised to the parent ingredient's pricing unit (see
    /// <see cref="QuantityUnit"/> for the unit the user originally typed).
    /// </para>
    /// <para>
    /// Per-unit quantities are derived, per doc/recipe-costing.md section 1.A:
    /// <c>Ingredient per unit = QuantityPerBatch / ExpectedBatchYield</c>.
    /// </para>
    /// </summary>
    public class RecipeIngredient
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;

        /// <summary>The pricing unit of the underlying ingredient (kg, litres, pcs).</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// The unit the batch quantity was entered in (kg, g, litres, ml, pcs).
        /// Retained for display/audit; storage is normalised to <see cref="Unit"/>.
        /// </summary>
        public string QuantityUnit { get; set; } = string.Empty;

        /// <summary>Quantity of this material consumed by ONE baseline batch.</summary>
        public decimal QuantityPerBatch { get; set; }

        /// <summary>
        /// Denormalised expected yield of the parent recipe, synced whenever the
        /// recipe is created or updated, so per-unit maths is available on the line.
        /// </summary>
        public decimal BaselineYield { get; set; } = 1m;

        /// <summary>
        /// Last resolved live unit cost. This is a display snapshot only — the
        /// costing engine always re-resolves the live price from the ingredient
        /// master so admin price changes cascade to every recipe.
        /// </summary>
        public decimal IngredientUnitCost { get; set; }

        /// <summary>Derived: material consumed per single finished unit.</summary>
        public decimal QuantityPerUnit =>
            BaselineYield > 0 ? QuantityPerBatch / BaselineYield : 0m;

        /// <summary>Cost of this line for one whole baseline batch.</summary>
        public decimal BatchLineCost => QuantityPerBatch * IngredientUnitCost;

        /// <summary>Cost of this line per single finished unit.</summary>
        public decimal TotalCost => QuantityPerUnit * IngredientUnitCost;
    }

    /// <summary>
    /// A product recipe expressed relative to a single baseline production unit.
    /// <para>
    /// <see cref="SellingPrice"/> doubles as the spec's
    /// <c>products.configured_selling_price</c> — this app has no separate
    /// Product table; the recipe is the sellable product record and Sales
    /// orders reference it by <c>RecipeId</c>.
    /// </para>
    /// </summary>
    public class Recipe
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // e.g. "Jumbo Family Bread"
        public string Code { get; set; } = string.Empty; // e.g. "BRD-JMB"
        public string Description { get; set; } = string.Empty;

        public ProductType ProductType { get; set; } = ProductType.Bread;

        /// <summary>The baseline unit that all ingredient batch quantities refer to.</summary>
        public BaselineUnit BaselineUnit { get; set; } = BaselineUnit.FlourBag50Kg;

        /// <summary>
        /// Expected number of finished units produced by one baseline batch.
        /// E.g. 1 bag of flour yields ~200 jumbo loaves; one water run yields ~250 bottles.
        /// </summary>
        public decimal ExpectedYield { get; set; } = 200m;

        /// <summary>Noun describing what one finished unit is (loaves, bottles, bags).</summary>
        public string OutputUnit { get; set; } = "loaves";

        public decimal PackagingCost { get; set; } // nylon wrapper / tag cost per unit
        public decimal LaborAndOverheadPerUnit { get; set; }
        public decimal SellingPrice { get; set; }
        public List<RecipeIngredient> Ingredients { get; set; } = new();

        public bool IsActive { get; set; } = true;

        /// <summary>Total material cost of one whole baseline batch (live pricing).</summary>
        public decimal TotalBatchCost => Ingredients.Sum(i => i.BatchLineCost);

        /// <summary>Material cost allocated to a single finished unit.</summary>
        public decimal IngredientsCostPerUnit =>
            ExpectedYield > 0 ? TotalBatchCost / ExpectedYield : 0m;

        /// <summary>
        /// Per-unit raw ingredient cost. Retained under its original name because
        /// the existing recipe views render it as "Raw Ingredients" per unit.
        /// </summary>
        public decimal IngredientsCost => IngredientsCostPerUnit;

        public decimal TotalUnitCost => IngredientsCostPerUnit + PackagingCost + LaborAndOverheadPerUnit;

        public decimal MarginAmount => SellingPrice - TotalUnitCost;

        public decimal MarginPercentage =>
            SellingPrice > 0 ? Math.Round((MarginAmount / SellingPrice) * 100, 2) : 0m;

        /// <summary>Unit production cost at an actual (rather than expected) yield.</summary>
        public decimal UnitCostAtYield(decimal actualYield) =>
            actualYield > 0 ? TotalBatchCost / actualYield + PackagingCost + LaborAndOverheadPerUnit : 0m;

        /// <summary>
        /// Pushes this recipe's expected yield down onto every ingredient line so
        /// per-unit maths on the lines stays correct. Called whenever a recipe's
        /// expected yield changes.
        /// </summary>
        public void SyncIngredientYield()
        {
            foreach (var line in Ingredients)
            {
                line.BaselineYield = ExpectedYield;
            }
        }
    }

    public enum BatchStatus
    {
        Planned,
        InMixing,
        Baking,
        Completed,
        Cancelled
    }

    /// <summary>
    /// A logged production run. Costing fields (<see cref="TotalBatchCost"/> and
    /// <see cref="CalculatedUnitCost"/>) are snapshotted at the moment the batch
    /// was logged so historical margins do not drift when ingredient prices change.
    /// </summary>
    public class ProductionBatch
    {
        public int Id { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int RecipeId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public int TargetQuantity { get; set; } // expected yield from the recipe
        public int ActualQuantity { get; set; } // completed loaves/bags
        public int Variance => ActualQuantity - TargetQuantity;
        public string Shift { get; set; } = "Morning";
        public string BakerInCharge { get; set; } = string.Empty;
        public decimal FlourBagsUsed { get; set; }
        public ProductType ProductCategory { get; set; } = ProductType.Bread;
        public decimal BatchInputQuantity { get; set; } = 1m;
        public string BatchInputUnitLabel { get; set; } = "Flour Bags (50kg)";
        public string OutputUnitLabel { get; set; } = "loaves";
        public int TargetOrderQuantity { get; set; } // Sourced from Sales Rep Orders Placed
        public int? LinkedOrderId { get; set; }
        public string? LinkedOrderNumber { get; set; }
        public string? CustomerName { get; set; }
        public BatchStatus Status { get; set; } = BatchStatus.Planned;
        public DateTime ProductionDate { get; set; } = DateTime.UtcNow;
        public string Notes { get; set; } = string.Empty;

        // ---- Costing snapshot (doc/recipe-costing.md section 2) ----

        /// <summary>Total material cost of this batch at the prices current when logged.</summary>
        public decimal TotalBatchCost { get; set; }

        /// <summary>TotalBatchCost / ActualQuantity at the prices current when logged.</summary>
        public decimal CalculatedUnitCost { get; set; }

        /// <summary>Expected unit cost based on recipe planned yield.</summary>
        public decimal ExpectedUnitCost { get; set; }

        /// <summary>Variance per unit between actual and expected unit cost.</summary>
        public decimal CostVariancePerUnit => CalculatedUnitCost - ExpectedUnitCost;

        /// <summary>User who logged the daily production output.</summary>
        public string LoggedBy { get; set; } = string.Empty;
    }
}
