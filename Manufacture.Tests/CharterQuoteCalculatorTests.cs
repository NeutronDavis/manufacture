using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Manufacture.Services;
using Xunit;

namespace Manufacture.Tests;

public class CharterQuoteCalculatorTests
{
    [Fact]
    public void Calculate_ShouldSumLegAmountsDistancesAndIncidentals()
    {
        var legs = new List<CharterQuoteLegDto>
        {
            new() { LegNumber = 1, DistanceKm = 120, QuotedLegAmount = 250_000m },
            new() { LegNumber = 2, DistanceKm = 80, QuotedLegAmount = 175_000m }
        };

        var totals = CharterQuoteCalculator.Calculate(legs, 40_000m, 25_000m, 10_000m);

        Assert.Equal(200m, totals.TotalDistanceKm);
        Assert.Equal(425_000m, totals.QuotedAmount);
        Assert.Equal(75_000m, totals.TotalTripIncidentals);
        Assert.Equal(350_000m, totals.NetEstimate);
    }

    [Fact]
    public void Validate_ShouldRejectEmptyLegs()
    {
        var quote = new CharterQuoteDto();

        var errors = CharterQuoteCalculator.Validate(quote);

        Assert.Contains(errors, error => error.Contains("At least one quote leg"));
    }

    [Fact]
    public void Validate_ShouldRejectMissingAddressesAndZeroAmountButAllowBlankDistance()
    {
        var quote = new CharterQuoteDto
        {
            Notes = "Client request details.",
            Legs =
            [
                new CharterQuoteLegDto
                {
                    LegNumber = 1,
                    OriginAddress = "",
                    OriginCityState = "Lagos",
                    DestinationAddress = "",
                    DestinationCityState = "Abuja FCT",
                    DistanceKm = 0,
                    QuotedLegAmount = 0
                }
            ]
        };

        var errors = CharterQuoteCalculator.Validate(quote);

        Assert.Contains(errors, error => error.Contains("origin address"));
        Assert.Contains(errors, error => error.Contains("destination address"));
        Assert.Contains(errors, error => error.Contains("quoted amount"));
        Assert.DoesNotContain(errors, error => error.Contains("distance"));
    }

    [Fact]
    public void Validate_ShouldRejectMissingQuoteNotes()
    {
        var quote = new CharterQuoteDto
        {
            Notes = "",
            Legs =
            [
                new CharterQuoteLegDto
                {
                    LegNumber = 1,
                    OriginAddress = "Victoria Island, Lagos",
                    OriginCityState = "Lagos",
                    DestinationAddress = "Port Harcourt GRA",
                    DestinationCityState = "Rivers",
                    DistanceKm = 0,
                    QuotedLegAmount = 500_000m
                }
            ]
        };

        var errors = CharterQuoteCalculator.Validate(quote);

        Assert.Contains(errors, error => error.Contains("Quote notes"));
        Assert.DoesNotContain(errors, error => error.Contains("distance"));
    }

    [Fact]
    public void Validate_ShouldRejectIncidentalsGreaterThanQuote()
    {
        var quote = new CharterQuoteDto
        {
            Notes = "Client request details.",
            DriverAllowance = 90_000m,
            Legs =
            [
                new CharterQuoteLegDto
                {
                    LegNumber = 1,
                    OriginAddress = "Victoria Island, Lagos",
                    OriginCityState = "Lagos",
                    DestinationAddress = "Port Harcourt GRA",
                    DestinationCityState = "Rivers",
                    DistanceKm = 610,
                    QuotedLegAmount = 50_000m
                }
            ]
        };

        var errors = CharterQuoteCalculator.Validate(quote);

        Assert.Contains(errors, error => error.Contains("Incidentals cannot exceed"));
    }

    [Fact]
    public void CanConvert_ShouldRequireApprovedNonExpiredQuote()
    {
        Assert.False(CharterQuoteCalculator.CanConvert(new CharterQuoteDto
        {
            Status = CharterQuoteStatus.PendingApproval,
            ValidUntil = DateTime.UtcNow.Date.AddDays(1)
        }));

        Assert.False(CharterQuoteCalculator.CanConvert(new CharterQuoteDto
        {
            Status = CharterQuoteStatus.Approved,
            ValidUntil = DateTime.UtcNow.Date.AddDays(-1)
        }));

        Assert.True(CharterQuoteCalculator.CanConvert(new CharterQuoteDto
        {
            Status = CharterQuoteStatus.Approved,
            ValidUntil = DateTime.UtcNow.Date.AddDays(1)
        }));
    }
}
