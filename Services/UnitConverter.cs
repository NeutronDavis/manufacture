namespace Manufacture.Services
{
    /// <summary>
    /// Static facade over <see cref="IUnitConversionService"/> preserving backward compatibility
    /// with existing callers, models, and unit tests while giving access to the full conversion engine.
    /// </summary>
    public static class UnitConverter
    {
        private static readonly UnitConversionService Service = new();

        /// <summary>Access to the underlying full unit conversion service.</summary>
        public static IUnitConversionService DefaultService => Service;

        public static decimal PackSizeInBaseUnits(string? unit) =>
            Service.PackSizeInBaseUnits(unit);

        public static string StripPackQualifier(string? unit) =>
            Service.StripPackQualifier(unit);

        public static MeasurementKind Classify(string? unit) =>
            Service.Classify(unit);

        public static decimal Convert(decimal quantity, string fromUnit, string toUnit) =>
            Service.Convert(quantity, fromUnit, toUnit);

        public static decimal Convert(decimal quantity, string fromUnit, string toUnit, string? materialName) =>
            Service.Convert(quantity, fromUnit, toUnit, materialName);

        public static bool CanConvert(string fromUnit, string toUnit) =>
            Service.CanConvert(fromUnit, toUnit);

        public static bool CanConvert(string fromUnit, string toUnit, string? materialName) =>
            Service.CanConvert(fromUnit, toUnit, materialName);

        public static decimal ToPricingUnit(decimal quantity, string enteredUnit, string pricingUnit) =>
            Service.ToPricingUnit(quantity, enteredUnit, pricingUnit);

        public static decimal ToPricingUnit(decimal quantity, string enteredUnit, string pricingUnit, string? materialName) =>
            Service.ToPricingUnit(quantity, enteredUnit, pricingUnit, materialName);

        public static decimal FromPricingUnit(decimal quantityInPricingUnit, string pricingUnit, string targetDisplayUnit, string? materialName = null) =>
            Service.FromPricingUnit(quantityInPricingUnit, pricingUnit, targetDisplayUnit, materialName);

        public static decimal ConvertPrice(decimal unitPrice, string fromUnit, string toUnit, string? materialName = null) =>
            Service.ConvertPrice(unitPrice, fromUnit, toUnit, materialName);

        public static IReadOnlyList<string> GetCompatibleUnits(string? baseUnit, string? materialName = null) =>
            Service.GetCompatibleUnits(baseUnit, materialName);
    }
}
