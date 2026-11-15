using System.Globalization;
using Manufacture.Models.DTOs;

namespace Manufacture.Services.Reports
{
    public static class ReportFormatter
    {
        public static string Format(object? value, ReportColumnType type)
        {
            if (value is null) return string.Empty;

            return type switch
            {
                ReportColumnType.Money => value is decimal money
                    ? "₦" + money.ToString("N0", CultureInfo.InvariantCulture)
                    : value.ToString() ?? string.Empty,
                ReportColumnType.Percent => value is decimal percent
                    ? percent.ToString("0.0", CultureInfo.InvariantCulture) + "%"
                    : value.ToString() ?? string.Empty,
                ReportColumnType.Number => value is decimal number
                    ? number.ToString("N2", CultureInfo.InvariantCulture)
                    : value is double d ? d.ToString("N2", CultureInfo.InvariantCulture)
                    : value.ToString() ?? string.Empty,
                ReportColumnType.Integer => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
                ReportColumnType.Date => value is DateTime date
                    ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : value.ToString() ?? string.Empty,
                _ => value.ToString() ?? string.Empty
            };
        }

        public static decimal Numeric(object? value) => value switch
        {
            decimal d => d,
            double d => (decimal)d,
            float f => (decimal)f,
            int i => i,
            long l => l,
            short s => s,
            _ => 0m
        };
    }
}
