namespace Manufacture.Services
{
    /// <summary>
    /// Classification of measurement units.
    /// </summary>
    public enum MeasurementKind
    {
        Unknown = 0,
        Mass,
        Volume,
        Count
    }

    /// <summary>
    /// Detailed result of a unit conversion attempt.
    /// </summary>
    public record UnitConversionResult(
        bool Success,
        decimal ConvertedQuantity,
        string FromUnit,
        string ToUnit,
        decimal Factor,
        string? Explanation = null
    );

    /// <summary>
    /// Universal unit conversion service for recipes, inventory, and production.
    /// Handles mass (kg, g, mg, ton, lb, oz), volume (litres, ml, cl, dl, gal, cup, tbsp, tsp),
    /// count/packaging (pcs, dozen, packs, cartons), compound pack units (e.g. Bag (50kg)),
    /// and culinary bridges (e.g. water 1L = 1kg).
    /// </summary>
    public interface IUnitConversionService
    {
        /// <summary>Classifies a unit string into its measurement kind.</summary>
        MeasurementKind Classify(string? unit);

        /// <summary>Checks whether two units can be converted into each other.</summary>
        bool CanConvert(string? fromUnit, string? toUnit, string? materialName = null);

        /// <summary>
        /// Converts a quantity from one unit to another. Returns 0 if conversion is invalid.
        /// </summary>
        decimal Convert(decimal quantity, string fromUnit, string toUnit, string? materialName = null, decimal? densityKgPerLitre = null);

        /// <summary>
        /// Tries to convert a quantity from one unit to another.
        /// </summary>
        bool TryConvert(decimal quantity, string fromUnit, string toUnit, out decimal result, string? materialName = null, decimal? densityKgPerLitre = null);

        /// <summary>
        /// Normalises an entered quantity into an ingredient's base pricing unit.
        /// E.g. 500 g against a kg pricing unit returns 0.5.
        /// </summary>
        decimal ToPricingUnit(decimal quantity, string enteredUnit, string pricingUnit, string? materialName = null);

        /// <summary>
        /// Converts a stored quantity in pricing unit back to a target display unit.
        /// E.g. 0.5 kg back to g returns 500.
        /// </summary>
        decimal FromPricingUnit(decimal quantityInPricingUnit, string pricingUnit, string targetDisplayUnit, string? materialName = null);

        /// <summary>
        /// Converts a unit cost between units. E.g. ₦4,500/kg -> ₦4.50/g.
        /// </summary>
        decimal ConvertPrice(decimal unitPrice, string fromUnit, string toUnit, string? materialName = null);

        /// <summary>
        /// Returns all compatible units that make sense for a given material and its base unit.
        /// </summary>
        IReadOnlyList<string> GetCompatibleUnits(string? baseUnit, string? materialName = null);

        /// <summary>
        /// Extracts pack size from compound units like "Bags (50kg)" -> 50.
        /// </summary>
        decimal PackSizeInBaseUnits(string? unit);

        /// <summary>
        /// Strips any pack qualifier, e.g. "Bags (50kg)" -> "Bags".
        /// </summary>
        string StripPackQualifier(string? unit);

        /// <summary>
        /// Returns a human-readable conversion explanation, e.g. "500 g = 0.5 kg".
        /// </summary>
        string FormatConversionHint(decimal quantity, string fromUnit, string toUnit, string? materialName = null);
    }
}
