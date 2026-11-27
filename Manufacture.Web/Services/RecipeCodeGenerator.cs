using System.Text;

namespace Manufacture.Services
{
    /// <summary>
    /// Builds recipe (product) codes from a production product type name.
    /// Codes follow the <c>PREFIX-###</c> shape, for example <c>BRD-001</c>,
    /// <c>WTR-001</c>, and <c>POP-001</c>.
    /// </summary>
    public static class RecipeCodeGenerator
    {
        private static readonly (string Keyword, string Prefix)[] KnownPrefixes =
        {
            ("POPCORN", "POP"),
            ("BREAD", "BRD"),
            ("WATER", "WTR"),
            ("SNACK", "SNK")
        };

        /// <summary>
        /// Derives a stable three-character prefix from a product type name.
        /// </summary>
        public static string PrefixFor(string? productTypeName)
        {
            var normalized = Normalize(productTypeName);
            if (normalized.Length == 0) return "GEN";

            foreach (var (keyword, prefix) in KnownPrefixes)
            {
                if (normalized.Contains(keyword, StringComparison.Ordinal))
                {
                    return prefix;
                }
            }

            var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var letters = words.Length > 1
                ? new string(words.Take(3).Select(word => word[0]).ToArray())
                : words[0];

            var builder = new StringBuilder(3);
            foreach (var character in letters)
            {
                if (!char.IsLetterOrDigit(character)) continue;
                builder.Append(character);
                if (builder.Length == 3) break;
            }

            if (builder.Length == 0) return "GEN";
            if (builder.Length < 3) builder.Append('X', 3 - builder.Length);
            return builder.ToString();
        }

        /// <summary>
        /// Returns the next unused <c>PREFIX-###</c> code. Existing codes with
        /// non-numeric suffixes (for example <c>BRD-JMB</c>) are ignored.
        /// </summary>
        public static string NextCode(string prefix, IEnumerable<string>? existingCodes)
        {
            var tokenPrefix = $"{(string.IsNullOrWhiteSpace(prefix) ? "GEN" : prefix.Trim().ToUpperInvariant())}-";
            var usedNumbers = new HashSet<int>();

            if (existingCodes != null)
            {
                foreach (var code in existingCodes)
                {
                    if (string.IsNullOrWhiteSpace(code)
                        || !code.StartsWith(tokenPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var suffix = code[tokenPrefix.Length..];
                    if (int.TryParse(suffix, out var number) && number > 0)
                    {
                        usedNumbers.Add(number);
                    }
                }
            }

            var next = 1;
            while (usedNumbers.Contains(next)) next++;
            return $"{tokenPrefix}{next:000}";
        }

        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var builder = new StringBuilder(value.Length);
            var previousWasSpace = true;

            foreach (var character in value.ToUpperInvariant())
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                    previousWasSpace = false;
                }
                else if (!previousWasSpace)
                {
                    builder.Append(' ');
                    previousWasSpace = true;
                }
            }

            return builder.ToString().Trim();
        }
    }
}
