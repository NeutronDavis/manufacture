using Manufacture.Services;
using Xunit;

namespace Manufacture.Tests
{
    public class RecipeCodeGeneratorTests
    {
        [Theory]
        [InlineData("Bread", "BRD")]
        [InlineData("bread", "BRD")]
        [InlineData("Water", "WTR")]
        [InlineData("Popcorn", "POP")]
        [InlineData("Snacks", "SNK")]
        [InlineData("   ", "GEN")]
        public void PrefixFor_MapsKnownProductTypes(string name, string expected)
        {
            Assert.Equal(expected, RecipeCodeGenerator.PrefixFor(name));
        }

        [Fact]
        public void PrefixFor_UsesFirstThreeCharactersForUnknownSingleWord()
        {
            Assert.Equal("CHO", RecipeCodeGenerator.PrefixFor("Chocolate"));
        }

        [Fact]
        public void PrefixFor_UsesWordInitialsForKnownMultiWordFallback()
        {
            Assert.Equal("MPX", RecipeCodeGenerator.PrefixFor("Meat Pie"));
        }

        [Fact]
        public void NextCode_StartsAtOneWhenNoCodesExist()
        {
            Assert.Equal("BRD-001", RecipeCodeGenerator.NextCode("BRD", Array.Empty<string>()));
        }

        [Fact]
        public void NextCode_UsesTheFirstUnusedNumber()
        {
            var existing = new[] { "BRD-001", "BRD-002", "BRD-004" };

            Assert.Equal("BRD-003", RecipeCodeGenerator.NextCode("BRD", existing));
        }

        [Fact]
        public void NextCode_ContinuesAfterContiguousNumbers()
        {
            var existing = new[] { "BRD-001", "BRD-002", "BRD-003" };

            Assert.Equal("BRD-004", RecipeCodeGenerator.NextCode("BRD", existing));
        }

        [Fact]
        public void NextCode_IgnoresNonNumericSuffixes()
        {
            var existing = new[] { "BRD-JMB", "BRD-6IN1", "WTR-003" };

            Assert.Equal("BRD-001", RecipeCodeGenerator.NextCode("BRD", existing));
        }

        [Fact]
        public void NextCode_IsCaseInsensitive()
        {
            Assert.Equal("WTR-003", RecipeCodeGenerator.NextCode("WTR", new[] { "wtr-001", "WTR-002" }));
        }

        [Fact]
        public void NextCode_UsesGenWhenPrefixIsBlank()
        {
            Assert.Equal("GEN-001", RecipeCodeGenerator.NextCode("", null));
        }
    }
}
