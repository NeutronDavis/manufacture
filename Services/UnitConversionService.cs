using System.Globalization;
using System.Text.RegularExpressions;

namespace Manufacture.Services
{
    /// <summary>
    /// Production implementation of <see cref="IUnitConversionService"/>.
    /// Provides universal, high-precision unit conversions across Mass, Volume, and Count,
    /// with support for compound pack sizes, culinary units, and fluid density bridges.
    /// </summary>
    public class UnitConversionService : IUnitConversionService
    {
        // Canonical base units:
        // Mass: Gram (g)
        // Volume: Millilitre (ml)
        // Count: Piece / Each (pcs)

        private const decimal GramsPerKilogram = 1000m;
        private const decimal GramsPerMilligram = 0.001m;
        private const decimal GramsPerMetricTon = 1_000_000m;
        private const decimal GramsPerPound = 453.59237m;
        private const decimal GramsPerOunce = 28.349523125m;

        private const decimal MlPerLitre = 1000m;
        private const decimal MlPerCentilitre = 10m;
        private const decimal MlPerDecilitre = 100m;
        private const decimal MlPerCubicMetre = 1_000_000m;
        private const decimal MlPerUsGallon = 3785.411784m;
        private const decimal MlPerFluidOunce = 29.5735295625m;
        private const decimal MlPerMetricCup = 250m;
        private const decimal MlPerTablespoon = 15m;
        private const decimal MlPerTeaspoon = 5m;

        private static readonly Regex InnerPackRegex = new(@"\(([^)]+)\)", RegexOptions.Compiled);
        private static readonly Regex NumericPrefixRegex = new(@"^([\d\.]+)\s*(.*)$", RegexOptions.Compiled);

        public MeasurementKind Classify(string? unit)
        {
            if (string.IsNullOrWhiteSpace(unit)) return MeasurementKind.Unknown;

            // Compound pack units e.g. "Bags (50kg)" or "Cartons (10kg)"
            var packInner = ExtractPackContents(unit);
            if (!string.IsNullOrEmpty(packInner))
            {
                var innerUnit = ExtractUnitName(packInner);
                var innerKind = Classify(innerUnit);
                if (innerKind != MeasurementKind.Unknown) return innerKind;
            }

            var clean = StripPackQualifier(unit).Trim().ToLowerInvariant();
            var unitOnly = ExtractUnitName(clean).ToLowerInvariant();

            // Mass
            if (clean is "kg" or "kgs" or "kilogram" or "kilograms" or "kilo" or "kilos"
                      or "g" or "gm" or "gram" or "grams" or "gramme" or "grammes"
                      or "mg" or "milligram" or "milligrams"
                      or "ton" or "tons" or "tonne" or "tonnes" or "t" or "metric ton"
                      or "lb" or "lbs" or "pound" or "pounds"
                      or "oz" or "ounce" or "ounces"
                      or "bag50kg" or "bag25kg"
                || unitOnly is "kg" or "kgs" or "kilogram" or "kilograms" or "kilo" or "kilos"
                           or "g" or "gm" or "gram" or "grams" or "gramme" or "grammes"
                           or "mg" or "milligram" or "milligrams"
                           or "ton" or "tons" or "tonne" or "tonnes" or "t" or "metric ton"
                           or "lb" or "lbs" or "pound" or "pounds"
                           or "oz" or "ounce" or "ounces")
            {
                return MeasurementKind.Mass;
            }

            // Volume
            if (clean is "l" or "ltr" or "litre" or "litres" or "liter" or "liters" or "ltrs"
                      or "ml" or "millilitre" or "millilitres" or "milliliter" or "milliliters"
                      or "cl" or "centilitre" or "centilitres" or "centiliter" or "centiliters"
                      or "dl" or "decilitre" or "decilitres" or "deciliter" or "deciliters"
                      or "m3" or "cubic metre" or "cubic meters"
                      or "gal" or "gallon" or "gallons"
                      or "fl oz" or "fl. oz" or "fluid ounce" or "fluid ounces"
                      or "cup" or "cups"
                      or "tbsp" or "tablespoon" or "tablespoons"
                      or "tsp" or "teaspoon" or "teaspoons"
                      or "bottle18_9l" or "18.9l"
                || unitOnly is "l" or "ltr" or "litre" or "litres" or "liter" or "liters" or "ltrs"
                           or "ml" or "millilitre" or "millilitres" or "milliliter" or "milliliters"
                           or "cl" or "centilitre" or "centilitres" or "centiliter" or "centiliters"
                           or "dl" or "decilitre" or "decilitres" or "deciliter" or "deciliters"
                           or "m3" or "cubic metre" or "cubic meters"
                           or "gal" or "gallon" or "gallons"
                           or "fl oz" or "fl. oz" or "fluid ounce" or "fluid ounces"
                           or "cup" or "cups"
                           or "tbsp" or "tablespoon" or "tablespoons"
                           or "tsp" or "teaspoon" or "teaspoons")
            {
                return MeasurementKind.Volume;
            }

            // Count
            if (clean is "pcs" or "pc" or "piece" or "pieces" or "each" or "unit" or "units" or "item" or "items"
                      or "pack" or "packs" or "packet" or "packets" or "bag" or "bags"
                      or "bottle" or "bottles" or "carton" or "cartons" or "bucket" or "buckets"
                      or "bundle" or "bundles" or "tank" or "tanks" or "crate" or "crates"
                      or "roll" or "rolls" or "box" or "boxes"
                      or "dozen" or "doz" or "dozens"
                      or "bakers dozen" or "baker's dozen"
                      or "gross" or "score" or "pair" or "pairs"
                || unitOnly is "pcs" or "pc" or "piece" or "pieces" or "each" or "unit" or "units" or "item" or "items"
                           or "pack" or "packs" or "packet" or "packets" or "bag" or "bags"
                           or "bottle" or "bottles" or "carton" or "cartons" or "bucket" or "buckets"
                           or "bundle" or "bundles" or "tank" or "tanks" or "crate" or "crates"
                           or "roll" or "rolls" or "box" or "boxes"
                           or "dozen" or "doz" or "dozens"
                           or "bakers dozen" or "baker's dozen"
                           or "gross" or "score" or "pair" or "pairs")
            {
                return MeasurementKind.Count;
            }

            return MeasurementKind.Unknown;
        }

        public bool CanConvert(string? fromUnit, string? toUnit, string? materialName = null)
        {
            if (string.IsNullOrWhiteSpace(fromUnit) || string.IsNullOrWhiteSpace(toUnit))
                return false;

            if (string.Equals(fromUnit.Trim(), toUnit.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

            var fromKind = Classify(fromUnit);
            var toKind = Classify(toUnit);

            if (fromKind != MeasurementKind.Unknown && fromKind == toKind)
                return true;

            // Liquid / water bridge between mass and volume
            if (IsLiquidMaterial(materialName)
                && ((fromKind == MeasurementKind.Mass && toKind == MeasurementKind.Volume) ||
                    (fromKind == MeasurementKind.Volume && toKind == MeasurementKind.Mass)))
            {
                return true;
            }

            // Container / item bridge from material name (e.g. "18.9L Dispenser Water Bottle" in pcs vs litres)
            if (CanBridgeItemVolumeOrMass(materialName, fromUnit, toUnit))
                return true;

            return false;
        }

        public decimal Convert(decimal quantity, string fromUnit, string toUnit, string? materialName = null, decimal? densityKgPerLitre = null)
        {
            if (quantity == 0m) return 0m;
            if (string.IsNullOrWhiteSpace(fromUnit) || string.IsNullOrWhiteSpace(toUnit)) return 0m;

            fromUnit = fromUnit.Trim();
            toUnit = toUnit.Trim();

            if (string.Equals(fromUnit, toUnit, StringComparison.OrdinalIgnoreCase))
                return quantity;

            var fromKind = Classify(fromUnit);
            var toKind = Classify(toUnit);

            // 1. Same measurement kind conversion
            if (fromKind != MeasurementKind.Unknown && fromKind == toKind)
            {
                var inBase = ToBaseUnits(quantity, fromUnit, fromKind);
                return FromBaseUnits(inBase, toUnit, toKind);
            }

            // 2. Liquid bridge between Mass and Volume (e.g. Water 1L = 1kg)
            if (IsLiquidMaterial(materialName) &&
                ((fromKind == MeasurementKind.Mass && toKind == MeasurementKind.Volume) ||
                 (fromKind == MeasurementKind.Volume && toKind == MeasurementKind.Mass)))
            {
                var density = densityKgPerLitre.GetValueOrDefault(GetDefaultDensity(materialName));

                if (fromKind == MeasurementKind.Volume && toKind == MeasurementKind.Mass)
                {
                    // Volume (ml) -> Litres -> Mass (kg * density) -> Mass base (g)
                    var volInMl = ToBaseUnits(quantity, fromUnit, MeasurementKind.Volume);
                    var volLitres = volInMl / MlPerLitre;
                    var massKg = volLitres * density;
                    var massGrams = massKg * GramsPerKilogram;
                    return FromBaseUnits(massGrams, toUnit, MeasurementKind.Mass);
                }
                else
                {
                    // Mass (g) -> kg -> Litres (/ density) -> Volume base (ml)
                    var massGrams = ToBaseUnits(quantity, fromUnit, MeasurementKind.Mass);
                    var massKg = massGrams / GramsPerKilogram;
                    var volLitres = density > 0 ? massKg / density : massKg;
                    var volInMl = volLitres * MlPerLitre;
                    return FromBaseUnits(volInMl, toUnit, MeasurementKind.Volume);
                }
            }

            // 3. Container / Item name bridge (e.g. "18.9L Dispenser Water Bottle" [pcs] <-> "litres")
            if (TryBridgeItemVolumeOrMass(quantity, fromUnit, toUnit, materialName, out var bridged))
            {
                return bridged;
            }

            return 0m;
        }

        public bool TryConvert(decimal quantity, string fromUnit, string toUnit, out decimal result, string? materialName = null, decimal? densityKgPerLitre = null)
        {
            if (CanConvert(fromUnit, toUnit, materialName))
            {
                result = Convert(quantity, fromUnit, toUnit, materialName, densityKgPerLitre);
                return true;
            }

            result = 0m;
            return false;
        }

        public decimal ToPricingUnit(decimal quantity, string enteredUnit, string pricingUnit, string? materialName = null)
        {
            if (string.IsNullOrWhiteSpace(enteredUnit) || string.IsNullOrWhiteSpace(pricingUnit))
                return quantity;

            enteredUnit = enteredUnit.Trim();
            pricingUnit = pricingUnit.Trim();

            if (string.Equals(enteredUnit, pricingUnit, StringComparison.OrdinalIgnoreCase))
                return quantity;

            if (!CanConvert(enteredUnit, pricingUnit, materialName))
                return 0m;

            // Compound pack entered: "1 Bag (50kg)" -> 50 kg
            var packContents = ExtractPackContents(enteredUnit);
            if (!string.IsNullOrEmpty(packContents))
            {
                var packSize = PackSizeInBaseUnits(enteredUnit);
                var innerUnit = ExtractUnitName(packContents);
                var convertedInner = Convert(quantity * packSize, innerUnit, pricingUnit, materialName);
                if (convertedInner > 0m) return convertedInner;
            }

            // Pricing unit is a pack: e.g. material is priced per "Bags (50kg)"
            var pricingPack = ExtractPackContents(pricingUnit);
            if (!string.IsNullOrEmpty(pricingPack))
            {
                var packSize = PackSizeInBaseUnits(pricingUnit);
                var innerUnit = ExtractUnitName(pricingPack);
                var qtyInInner = Convert(quantity, enteredUnit, innerUnit, materialName);
                if (qtyInInner > 0m && packSize > 0m) return qtyInInner / packSize;
            }

            var direct = Convert(quantity, enteredUnit, pricingUnit, materialName);
            return direct > 0m ? direct : quantity;
        }

        public decimal FromPricingUnit(decimal quantityInPricingUnit, string pricingUnit, string targetDisplayUnit, string? materialName = null)
        {
            if (string.IsNullOrWhiteSpace(pricingUnit) || string.IsNullOrWhiteSpace(targetDisplayUnit))
                return quantityInPricingUnit;

            pricingUnit = pricingUnit.Trim();
            targetDisplayUnit = targetDisplayUnit.Trim();

            if (string.Equals(pricingUnit, targetDisplayUnit, StringComparison.OrdinalIgnoreCase))
                return quantityInPricingUnit;

            var converted = Convert(quantityInPricingUnit, pricingUnit, targetDisplayUnit, materialName);
            return converted > 0m ? converted : quantityInPricingUnit;
        }

        public decimal ConvertPrice(decimal unitPrice, string fromUnit, string toUnit, string? materialName = null)
        {
            if (unitPrice <= 0m || string.IsNullOrWhiteSpace(fromUnit) || string.IsNullOrWhiteSpace(toUnit))
                return unitPrice;

            if (string.Equals(fromUnit.Trim(), toUnit.Trim(), StringComparison.OrdinalIgnoreCase))
                return unitPrice;

            // Price per fromUnit: how many fromUnits in 1 toUnit?
            // E.g. Price is 4500 per kg. We want price per g.
            // 1 g = 0.001 kg -> Price per g = 4500 * 0.001 = 4.50.
            var factor = Convert(1m, toUnit, fromUnit, materialName);
            if (factor > 0m) return unitPrice * factor;

            return unitPrice;
        }

        public IReadOnlyList<string> GetCompatibleUnits(string? baseUnit, string? materialName = null)
        {
            var kind = Classify(baseUnit);
            var result = new List<string>();

            if (kind == MeasurementKind.Mass)
            {
                result.AddRange(new[] { "kg", "g", "mg", "ton", "lb", "oz", "Bags (50kg)", "Bags (25kg)" });
                if (IsLiquidMaterial(materialName))
                {
                    result.AddRange(new[] { "litres", "ml", "cl" });
                }
            }
            else if (kind == MeasurementKind.Volume)
            {
                result.AddRange(new[] { "litres", "ml", "cl", "dl", "gal", "cup", "tbsp", "tsp", "m3" });
                if (IsLiquidMaterial(materialName))
                {
                    result.AddRange(new[] { "kg", "g" });
                }
            }
            else // Count or Unknown
            {
                result.AddRange(new[] { "pcs", "dozen", "pack", "carton", "box", "crate", "bundle" });
                if (HasItemVolume(materialName))
                {
                    result.AddRange(new[] { "litres", "ml" });
                }
            }

            // Ensure baseUnit is included at the front
            if (!string.IsNullOrWhiteSpace(baseUnit))
            {
                var cleanBase = baseUnit.Trim();
                result.RemoveAll(u => string.Equals(u, cleanBase, StringComparison.OrdinalIgnoreCase));
                result.Insert(0, cleanBase);
            }

            return result;
        }

        public decimal PackSizeInBaseUnits(string? unit)
        {
            var inner = ExtractPackContents(unit);
            if (string.IsNullOrEmpty(inner)) return 1m;

            var match = NumericPrefixRegex.Match(inner);
            if (match.Success && decimal.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var size) && size > 0m)
            {
                return size;
            }

            return 1m;
        }

        public string StripPackQualifier(string? unit)
        {
            if (string.IsNullOrWhiteSpace(unit)) return string.Empty;
            var open = unit.IndexOf('(');
            return open < 0 ? unit.Trim() : unit.Substring(0, open).Trim();
        }

        public string FormatConversionHint(decimal quantity, string fromUnit, string toUnit, string? materialName = null)
        {
            if (string.Equals(fromUnit?.Trim(), toUnit?.Trim(), StringComparison.OrdinalIgnoreCase))
                return $"{quantity:0.##} {fromUnit}";

            var converted = Convert(quantity, fromUnit ?? "", toUnit ?? "", materialName);
            return $"{quantity:0.##} {fromUnit} = {converted:0.####} {toUnit}";
        }

        // =============================================================
        // Internal Helpers
        // =============================================================

        private static string? ExtractPackContents(string? unit)
        {
            if (string.IsNullOrWhiteSpace(unit)) return null;
            var match = InnerPackRegex.Match(unit);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private static string ExtractUnitName(string text)
        {
            var match = NumericPrefixRegex.Match(text.Trim());
            return match.Success && !string.IsNullOrWhiteSpace(match.Groups[2].Value)
                ? match.Groups[2].Value.Trim()
                : text.Trim();
        }

        private static bool IsLiquidMaterial(string? materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName)) return false;
            var lower = materialName.ToLowerInvariant();
            return lower.Contains("water")
                   || lower.Contains("oil")
                   || lower.Contains("milk")
                   || lower.Contains("syrup")
                   || lower.Contains("juice")
                   || lower.Contains("flavour")
                   || lower.Contains("flavor")
                   || lower.Contains("extract")
                   || lower.Contains("liquid")
                   || lower.Contains("solution");
        }

        private static decimal GetDefaultDensity(string? materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName)) return 1.0m;
            var lower = materialName.ToLowerInvariant();
            if (lower.Contains("oil")) return 0.92m; // vegetable oil
            if (lower.Contains("milk")) return 1.03m;
            if (lower.Contains("syrup")) return 1.33m;
            return 1.0m; // Water standard: 1 L = 1 kg
        }

        private static bool HasItemVolume(string? materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName)) return false;
            return materialName.Contains("18.9L", StringComparison.OrdinalIgnoreCase)
                   || materialName.Contains("18.9 L", StringComparison.OrdinalIgnoreCase)
                   || materialName.Contains("75cl", StringComparison.OrdinalIgnoreCase)
                   || materialName.Contains("50cl", StringComparison.OrdinalIgnoreCase)
                   || materialName.Contains("Dispenser", StringComparison.OrdinalIgnoreCase);
        }

        private static bool CanBridgeItemVolumeOrMass(string? materialName, string fromUnit, string toUnit)
        {
            if (string.IsNullOrWhiteSpace(materialName)) return false;
            var from = fromUnit.Trim().ToLowerInvariant();
            var to = toUnit.Trim().ToLowerInvariant();

            if (materialName.Contains("18.9", StringComparison.OrdinalIgnoreCase))
            {
                var isVol = from is "l" or "ltr" or "litre" or "litres" or "liter" or "liters" or "ml";
                var isPcs = from is "pcs" or "pc" or "bottle" or "bottles";
                var toVol = to is "l" or "ltr" or "litre" or "litres" or "liter" or "liters" or "ml";
                var toPcs = to is "pcs" or "pc" or "bottle" or "bottles";

                return (isVol && toPcs) || (isPcs && toVol);
            }

            return false;
        }

        private static bool TryBridgeItemVolumeOrMass(decimal quantity, string fromUnit, string toUnit, string? materialName, out decimal result)
        {
            result = 0m;
            if (string.IsNullOrWhiteSpace(materialName)) return false;

            if (materialName.Contains("18.9", StringComparison.OrdinalIgnoreCase))
            {
                const decimal bottleCapacityLitres = 18.9m;
                var from = fromUnit.Trim().ToLowerInvariant();
                var to = toUnit.Trim().ToLowerInvariant();

                // From Volume (litres/ml) to bottles/pcs
                if (from is "l" or "ltr" or "litre" or "litres" or "liter" or "liters")
                {
                    if (to is "pcs" or "pc" or "bottle" or "bottles" or "each")
                    {
                        result = quantity / bottleCapacityLitres;
                        return true;
                    }
                }
                else if (from is "ml")
                {
                    if (to is "pcs" or "pc" or "bottle" or "bottles" or "each")
                    {
                        result = (quantity / 1000m) / bottleCapacityLitres;
                        return true;
                    }
                }
                // From bottles/pcs to Volume (litres/ml)
                else if (from is "pcs" or "pc" or "bottle" or "bottles" or "each")
                {
                    if (to is "l" or "ltr" or "litre" or "litres" or "liter" or "liters")
                    {
                        result = quantity * bottleCapacityLitres;
                        return true;
                    }
                    if (to is "ml")
                    {
                        result = quantity * bottleCapacityLitres * 1000m;
                        return true;
                    }
                }
            }

            return false;
        }

        private static decimal ToBaseUnits(decimal quantity, string unit, MeasurementKind kind)
        {
            var packSize = 1m;
            var inner = ExtractPackContents(unit);
            if (!string.IsNullOrEmpty(inner))
            {
                var match = NumericPrefixRegex.Match(inner);
                if (match.Success && decimal.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var size))
                {
                    packSize = size;
                }
                unit = ExtractUnitName(inner);
            }

            var u = unit.Trim().ToLowerInvariant();

            return kind switch
            {
                MeasurementKind.Mass => u switch
                {
                    "g" or "gm" or "gram" or "grams" or "gramme" or "grammes" => quantity * packSize,
                    "mg" or "milligram" or "milligrams" => (quantity * packSize) * GramsPerMilligram,
                    "ton" or "tons" or "tonne" or "tonnes" or "t" or "metric ton" => (quantity * packSize) * GramsPerMetricTon,
                    "lb" or "lbs" or "pound" or "pounds" => (quantity * packSize) * GramsPerPound,
                    "oz" or "ounce" or "ounces" => (quantity * packSize) * GramsPerOunce,
                    "bag50kg" => (quantity * packSize) * 50_000m,
                    "bag25kg" => (quantity * packSize) * 25_000m,
                    _ => (quantity * packSize) * GramsPerKilogram // Default mass is kg
                },
                MeasurementKind.Volume => u switch
                {
                    "ml" or "millilitre" or "millilitres" or "milliliter" or "milliliters" => quantity * packSize,
                    "cl" or "centilitre" or "centilitres" => (quantity * packSize) * MlPerCentilitre,
                    "dl" or "decilitre" or "decilitres" => (quantity * packSize) * MlPerDecilitre,
                    "m3" or "cubic metre" => (quantity * packSize) * MlPerCubicMetre,
                    "gal" or "gallon" or "gallons" => (quantity * packSize) * MlPerUsGallon,
                    "fl oz" or "fl. oz" or "fluid ounce" => (quantity * packSize) * MlPerFluidOunce,
                    "cup" or "cups" => (quantity * packSize) * MlPerMetricCup,
                    "tbsp" or "tablespoon" => (quantity * packSize) * MlPerTablespoon,
                    "tsp" or "teaspoon" => (quantity * packSize) * MlPerTeaspoon,
                    _ => (quantity * packSize) * MlPerLitre // Default volume is litres
                },
                _ => u switch
                {
                    "dozen" or "doz" or "dozens" => quantity * packSize * 12m,
                    "bakers dozen" or "baker's dozen" => quantity * packSize * 13m,
                    "gross" => quantity * packSize * 144m,
                    "pair" or "pairs" => quantity * packSize * 2m,
                    "score" => quantity * packSize * 20m,
                    _ => quantity * packSize
                }
            };
        }

        private static decimal FromBaseUnits(decimal baseQuantity, string unit, MeasurementKind kind)
        {
            var packSize = 1m;
            var inner = ExtractPackContents(unit);
            if (!string.IsNullOrEmpty(inner))
            {
                var match = NumericPrefixRegex.Match(inner);
                if (match.Success && decimal.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var size) && size > 0m)
                {
                    packSize = size;
                }
                unit = ExtractUnitName(inner);
            }

            var u = unit.Trim().ToLowerInvariant();

            var unscaled = kind switch
            {
                MeasurementKind.Mass => u switch
                {
                    "g" or "gm" or "gram" or "grams" or "gramme" or "grammes" => baseQuantity,
                    "mg" or "milligram" or "milligrams" => baseQuantity / GramsPerMilligram,
                    "ton" or "tons" or "tonne" or "tonnes" or "t" or "metric ton" => baseQuantity / GramsPerMetricTon,
                    "lb" or "lbs" or "pound" or "pounds" => baseQuantity / GramsPerPound,
                    "oz" or "ounce" or "ounces" => baseQuantity / GramsPerOunce,
                    "bag50kg" => baseQuantity / 50_000m,
                    "bag25kg" => baseQuantity / 25_000m,
                    _ => baseQuantity / GramsPerKilogram // kg
                },
                MeasurementKind.Volume => u switch
                {
                    "ml" or "millilitre" or "millilitres" or "milliliter" or "milliliters" => baseQuantity,
                    "cl" or "centilitre" or "centilitres" => baseQuantity / MlPerCentilitre,
                    "dl" or "decilitre" or "decilitres" => baseQuantity / MlPerDecilitre,
                    "m3" or "cubic metre" => baseQuantity / MlPerCubicMetre,
                    "gal" or "gallon" or "gallons" => baseQuantity / MlPerUsGallon,
                    "fl oz" or "fl. oz" or "fluid ounce" => baseQuantity / MlPerFluidOunce,
                    "cup" or "cups" => baseQuantity / MlPerMetricCup,
                    "tbsp" or "tablespoon" => baseQuantity / MlPerTablespoon,
                    "tsp" or "teaspoon" => baseQuantity / MlPerTeaspoon,
                    _ => baseQuantity / MlPerLitre // litres
                },
                _ => u switch
                {
                    "dozen" or "doz" or "dozens" => baseQuantity / 12m,
                    "bakers dozen" or "baker's dozen" => baseQuantity / 13m,
                    "gross" => baseQuantity / 144m,
                    "pair" or "pairs" => baseQuantity / 2m,
                    "score" => baseQuantity / 20m,
                    _ => baseQuantity
                }
            };

            return packSize > 0m ? unscaled / packSize : unscaled;
        }
    }
}
