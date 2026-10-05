using Manufacture.Services;
using Xunit;

namespace Manufacture.Tests
{
    public class UnitConversionServiceTests
    {
        private readonly UnitConversionService _converter = new();

        [Theory]
        [InlineData(500, "g", "kg", 0.5)]
        [InlineData(1, "kg", "g", 1000)]
        [InlineData(250, "g", "kg", 0.25)]
        [InlineData(1000, "mg", "g", 1)]
        [InlineData(500, "mg", "g", 0.5)]
        [InlineData(1, "ton", "kg", 1000)]
        [InlineData(2, "tonne", "kg", 2000)]
        [InlineData(1, "lb", "g", 453.59237)]
        public void Convert_MassUnits_HighPrecision(decimal qty, string from, string to, decimal expected)
        {
            var result = _converter.Convert(qty, from, to);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(500, "ml", "litres", 0.5)]
        [InlineData(2, "litres", "ml", 2000)]
        [InlineData(100, "cl", "litres", 1)]
        [InlineData(10, "dl", "litres", 1)]
        [InlineData(1, "cup", "ml", 250)]
        [InlineData(2, "tbsp", "ml", 30)]
        [InlineData(3, "tsp", "ml", 15)]
        [InlineData(1, "m3", "litres", 1000)]
        public void Convert_VolumeUnits_HighPrecision(decimal qty, string from, string to, decimal expected)
        {
            var result = _converter.Convert(qty, from, to);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(1, "dozen", "pcs", 12)]
        [InlineData(2, "dozen", "pcs", 24)]
        [InlineData(1, "bakers dozen", "pcs", 13)]
        [InlineData(1, "gross", "pcs", 144)]
        [InlineData(24, "pcs", "dozen", 2)]
        public void Convert_CountUnits_Correct(decimal qty, string from, string to, decimal expected)
        {
            var result = _converter.Convert(qty, from, to);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Convert_CompoundPack_Bags50kgToKilograms()
        {
            var result = _converter.Convert(2, "Bags (50kg)", "kg");
            Assert.Equal(100m, result);
        }

        [Fact]
        public void Convert_CompoundPack_GramsToBags50kg()
        {
            var result = _converter.Convert(100_000, "g", "Bags (50kg)");
            Assert.Equal(2m, result);
        }

        [Fact]
        public void Convert_WaterDensityBridge_LitresToKg()
        {
            // For water, 1 litre = 1 kg
            var result = _converter.Convert(5, "litres", "kg", "Purified Process Water");
            Assert.Equal(5m, result);
        }

        [Fact]
        public void Convert_WaterDensityBridge_GramsToMl()
        {
            // 500 g water = 500 ml
            var result = _converter.Convert(500, "g", "ml", "Purified Process Water");
            Assert.Equal(500m, result);
        }

        [Fact]
        public void Convert_DispenserBottleBridge_LitresToBottles()
        {
            // 18.9 L Dispenser Water Bottle
            var result = _converter.Convert(18.9m, "litres", "pcs", "18.9L Dispenser Water Bottle");
            Assert.Equal(1m, result);

            var half = _converter.Convert(37.8m, "litres", "pcs", "18.9L Dispenser Water Bottle");
            Assert.Equal(2m, half);
        }

        [Fact]
        public void ToPricingUnit_GramsToKg_ProducesFraction()
        {
            // 500 g against ₦4,500/kg
            var normalised = _converter.ToPricingUnit(500, "g", "kg", "Calcium Propionate");
            Assert.Equal(0.5m, normalised);
        }

        [Fact]
        public void FromPricingUnit_RoundTripsAccurately()
        {
            // Stored 0.5 kg -> target display 'g'
            var display = _converter.FromPricingUnit(0.5m, "kg", "g");
            Assert.Equal(500m, display);
        }

        [Fact]
        public void ConvertPrice_CalculatesUnitPriceAccurately()
        {
            // Price is ₦4,500 per kg -> Price per g should be ₦4.50
            var pricePerGram = _converter.ConvertPrice(4500m, "kg", "g");
            Assert.Equal(4.50m, pricePerGram);
        }

        [Fact]
        public void GetCompatibleUnits_MassReturnsGramsAndKg()
        {
            var units = _converter.GetCompatibleUnits("kg", "Instant Dry Yeast");
            Assert.Contains("kg", units);
            Assert.Contains("g", units);
            Assert.Contains("mg", units);
        }

        [Fact]
        public void CanConvert_ValidatesCompatibility()
        {
            Assert.True(_converter.CanConvert("g", "kg"));
            Assert.True(_converter.CanConvert("ml", "litres"));
            Assert.True(_converter.CanConvert("pcs", "dozen"));
            Assert.False(_converter.CanConvert("kg", "pcs"));
        }
    }
}
