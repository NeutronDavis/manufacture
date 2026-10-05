using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Services
{
    /// <summary>
    /// Metadata describing each baseline production unit from
    /// doc/recipe-costing.md section 1.A / 1.B. Drives the recipe builder UI so
    /// the form can offer sensible defaults and quantity units per product family.
    /// </summary>
    public static class RecipeCatalog
    {
        /// <summary>Ingredient prices older than this are flagged as stale on the admin dashboard.</summary>
        public const int StalePriceAfterDays = 30;

        public static readonly IReadOnlyList<BaselineOptionDto> Baselines = new List<BaselineOptionDto>
        {
            new()
            {
                Key = nameof(BaselineUnit.FlourBag50Kg),
                DisplayName = "1 Bag of Flour (50 kg)",
                Description = "Industrial mixer standard. All ingredient quantities are defined relative to a single 50 kg bag of wheat flour.",
                OutputUnit = "loaves",
                SuggestedYield = 200m,
                SuggestedQuantityUnits = { "kg", "g", "litres", "ml" }
            },
            new()
            {
                Key = nameof(BaselineUnit.WaterRun18_9L),
                DisplayName = "One Water Run (18.9 L bottles)",
                Description = "A full dispenser production run. Unit cost = total run material cost / actual filled bottles.",
                OutputUnit = "bottles",
                SuggestedYield = 250m,
                SuggestedQuantityUnits = { "litres", "ml" }
            },
            new()
            {
                Key = nameof(BaselineUnit.PopcornPot),
                DisplayName = "One Popcorn Cooking Pot",
                Description = "A standard cooking pot (~2 kg corn + oil + seasoning). Unit cost = total pot cost / actual output.",
                OutputUnit = "bags",
                SuggestedYield = 60m,
                SuggestedQuantityUnits = { "kg", "g" }
            }
        };

        public static readonly IReadOnlyList<ProductType> ProductTypes = new List<ProductType>
        {
            ProductType.Bread,
            ProductType.Water,
            ProductType.Popcorn
        };

        public static BaselineOptionDto? FindBaseline(BaselineUnit unit) =>
            Baselines.FirstOrDefault(b => string.Equals(b.Key, unit.ToString(), StringComparison.OrdinalIgnoreCase));

        public static BaselineOptionDto? FindBaseline(string? key) =>
            Baselines.FirstOrDefault(b => string.Equals(b.Key, key, StringComparison.OrdinalIgnoreCase));

        public static string BaselineDisplayName(BaselineUnit unit) =>
            FindBaseline(unit)?.DisplayName ?? unit.ToString();

        public static string BaselineDisplayName(string? key) =>
            string.IsNullOrWhiteSpace(key) ? "Unknown Baseline" : BaselineDisplayName(ParseBaseline(key));

        public static string OutputUnitFor(BaselineUnit unit) =>
            FindBaseline(unit)?.OutputUnit ?? "units";

        /// <summary>Default output noun for a product type, used when seeding recipes.</summary>
        public static string OutputUnitFor(ProductType type) => type switch
        {
            ProductType.Bread => "loaves",
            ProductType.Water => "bottles",
            ProductType.Popcorn => "bags",
            _ => "units"
        };

        public static string DefaultBaselineFor(ProductType type) => type switch
        {
            ProductType.Bread => nameof(BaselineUnit.FlourBag50Kg),
            ProductType.Water => nameof(BaselineUnit.WaterRun18_9L),
            ProductType.Popcorn => nameof(BaselineUnit.PopcornPot),
            _ => nameof(BaselineUnit.FlourBag50Kg)
        };

        public static ProductType ParseProductType(string? value) =>
            Enum.TryParse<ProductType>(value, true, out var parsed) ? parsed : ProductType.Bread;

        public static BaselineUnit ParseBaseline(string? value) =>
            Enum.TryParse<BaselineUnit>(value, true, out var parsed) ? parsed : BaselineUnit.FlourBag50Kg;
    }
}
