using Manufacture.Models.DTOs;

namespace Manufacture.Services
{
    public sealed record CharterQuoteTotals(
        decimal TotalDistanceKm,
        decimal QuotedAmount,
        decimal TotalTripIncidentals,
        decimal NetEstimate);

    public static class CharterQuoteCalculator
    {
        public static CharterQuoteTotals Calculate(
            IEnumerable<CharterQuoteLegDto> legs,
            decimal driverAllowance,
            decimal securityEscortFee,
            decimal tollsAndRoadLevies)
        {
            var legList = legs?.ToList() ?? new List<CharterQuoteLegDto>();
            var distance = legList.Sum(leg => leg.DistanceKm.GetValueOrDefault());
            var quoted = legList.Sum(leg => leg.QuotedLegAmount);
            var incidentals = driverAllowance + securityEscortFee + tollsAndRoadLevies;

            return new CharterQuoteTotals(
                Math.Round(distance, 2),
                Math.Round(quoted, 2),
                Math.Round(incidentals, 2),
                Math.Round(quoted - incidentals, 2));
        }

        public static List<string> Validate(CharterQuoteDto quote)
        {
            var errors = new List<string>();

            if (quote.Legs is null || quote.Legs.Count == 0)
            {
                errors.Add("At least one quote leg is required.");
                return errors;
            }

            for (var i = 0; i < quote.Legs.Count; i++)
            {
                var leg = quote.Legs[i];
                var number = i + 1;

                if (string.IsNullOrWhiteSpace(leg.OriginAddress))
                {
                    errors.Add($"Leg {number}: origin address is required.");
                }

                if (string.IsNullOrWhiteSpace(leg.DestinationAddress))
                {
                    errors.Add($"Leg {number}: destination address is required.");
                }

                if (string.IsNullOrWhiteSpace(leg.OriginCityState))
                {
                    errors.Add($"Leg {number}: origin city/state is required.");
                }

                if (string.IsNullOrWhiteSpace(leg.DestinationCityState))
                {
                    errors.Add($"Leg {number}: destination city/state is required.");
                }

                if (leg.QuotedLegAmount < 0)
                {
                    errors.Add($"Leg {number}: quoted amount cannot be negative.");
                }
            }

            if (string.IsNullOrWhiteSpace(quote.Notes))
            {
                errors.Add("Quote notes / client requirements are required.");
            }

            if (quote.DriverAllowance < 0 || quote.SecurityEscortFee < 0 || quote.TollsAndRoadLevies < 0)
            {
                errors.Add("Trip incidentals cannot be negative.");
            }

            var totals = Calculate(quote.Legs, quote.DriverAllowance, quote.SecurityEscortFee, quote.TollsAndRoadLevies);
            if (totals.QuotedAmount <= 0)
            {
                errors.Add("The total quoted amount must be greater than zero.");
            }

            if (totals.NetEstimate < 0)
            {
                errors.Add("Incidentals cannot exceed the quoted amount.");
            }

            return errors;
        }

        public static bool CanConvert(CharterQuoteDto quote) =>
            quote.Status == CharterQuoteStatus.Approved
            && (!quote.ValidUntil.HasValue || quote.ValidUntil.Value.Date >= DateTime.UtcNow.Date);
    }
}
