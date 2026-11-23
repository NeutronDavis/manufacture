using Manufacture.Services;
using Xunit;

namespace Manufacture.Tests
{
    /// <summary>
    /// doc/recipe-costing.md section 5 requires unit conversion support
    /// (e.g. grams to kilograms). These tests pin the conversion behaviour the
    /// costing engine relies on when normalising builder input.
    /// </summary>
    public class UnitConverterTests
    {
        [Theory]
        [InlineData(500, "g", "kg", 0.5)]
        [InlineData(1, "kg", "g", 1000)]
        [InlineData(1500, "g", "kg", 1.5)]
        [InlineData(0.25, "kg", "g", 250)]
        [InlineData(50, "kgs", "kg", 50)]
        [InlineData(2, "kilograms", "g", 2000)]
        [InlineData(750, "mg", "g", 0.75)]
        public void Convert_MassUnits_ConvertsCorrectly(decimal qty, string from, string to, decimal expected)
        {
            Assert.Equal(expected, UnitConverter.Convert(qty, from, to));
        }

        [Theory]
        [InlineData(500, "ml", "litres", 0.5)]
        [InlineData(2, "litres", "ml", 2000)]
        [InlineData(18900, "ml", "litres", 18.9)]
        [InlineData(18.9, "l", "litre", 18.9)]
        public void Convert_VolumeUnits_ConvertsCorrectly(decimal qty, string from, string to, decimal expected)
        {
            Assert.Equal(expected, UnitConverter.Convert(qty, from, to));
        }

        [Theory]
        [InlineData(250, "pcs", "pcs", 250)]
        [InlineData(96, "bags", "pcs", 96)]
        public void Convert_CountUnits_IsIdentity(decimal qty, string from, string to, decimal expected)
        {
            Assert.Equal(expected, UnitConverter.Convert(qty, from, to));
        }

        [Fact]
        public void Convert_MassToVolume_ReturnsZero()
        {
            // Flour in kg cannot be expressed in litres.
            Assert.Equal(0m, UnitConverter.Convert(1, "kg", "litres"));
        }

        [Fact]
        public void Convert_UnknownUnit_ReturnsZero()
        {
            Assert.Equal(0m, UnitConverter.Convert(5, "sacks", "kg"));
        }

        [Fact]
        public void CanConvert_IsFalseAcrossKinds()
        {
            Assert.False(UnitConverter.CanConvert("kg", "litres"));
            Assert.False(UnitConverter.CanConvert("pcs", "g"));
            Assert.True(UnitConverter.CanConvert("g", "kg"));
            Assert.True(UnitConverter.CanConvert("ml", "litres"));
        }

        [Theory]
        [InlineData("Bags (50kg)", 50)]
        [InlineData("Cartons (10kg)", 10)]
        [InlineData("kg", 1)]
        [InlineData("pcs", 1)]
        [InlineData("", 1)]
        [InlineData(null, 1)]
        public void PackSizeInBaseUnits_ReadsCompoundPackUnits(string? unit, decimal expected)
        {
            Assert.Equal(expected, UnitConverter.PackSizeInBaseUnits(unit));
        }

        [Theory]
        [InlineData("Bags (50kg)", "Bags")]
        [InlineData("Cartons (10kg)", "Cartons")]
        [InlineData("kg", "kg")]
        public void StripPackQualifier_RemovesPackSuffix(string unit, string expected)
        {
            Assert.Equal(expected, UnitConverter.StripPackQualifier(unit));
        }

        [Fact]
        public void ToPricingUnit_GramsToKilograms()
        {
            // 500g of yeast on a recipe priced per kg.
            Assert.Equal(0.5m, UnitConverter.ToPricingUnit(500, "g", "kg"));
        }

        [Fact]
        public void ToPricingUnit_MillilitresToLitres()
        {
            Assert.Equal(1.5m, UnitConverter.ToPricingUnit(1500, "ml", "litres"));
        }

        [Fact]
        public void ToPricingUnit_FiftyKgBagExpandsToFiftyKilograms()
        {
            // 1 bag of flour (50kg) recorded against a per-kg price is 50kg.
            Assert.Equal(50m, UnitConverter.ToPricingUnit(1, "Bags (50kg)", "kg"));
        }

        [Fact]
        public void ToPricingUnit_SameUnit_ReturnsQuantityUnchanged()
        {
            Assert.Equal(50m, UnitConverter.ToPricingUnit(50, "kg", "kg"));
        }

        [Fact]
        public void ToPricingUnit_IncompatibleUnits_ReturnsZero()
        {
            Assert.Equal(0m, UnitConverter.ToPricingUnit(30, "litres", "kg"));
        }
    }
}
