using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Services
{
    /// <summary>
    /// The closing TOTAL band of a report, keyed by report column key.
    ///
    /// Both exports render totals from this one map: the CSV lays it out beneath
    /// the last data row and the PDF paints it into the dark band at the foot of
    /// the final page. Deriving the values from a key map rather than a fixed
    /// list of strings is what stops the two downloads from disagreeing — and
    /// stops a column being added from silently shifting every figure sideways.
    /// </summary>
    public static class ReportTotals
    {
        public static Dictionary<string, string> For(ReportResultDto result)
        {
            var t = result.GrandTotals;
            var totals = new Dictionary<string, string>(StringComparer.Ordinal);

            switch (result.Module)
            {
                case AnalyticsModule.Financial:
                    totals[nameof(ReportRowDto.ExpectedRevenue)] = Money(t.ExpectedRevenue);
                    totals[nameof(ReportRowDto.CollectedCash)] = Money(result.Tender.Cash);
                    totals[nameof(ReportRowDto.CollectedTransfer)] = Money(result.Tender.BankTransfer);
                    totals[nameof(ReportRowDto.CollectedPosBank)] = Money(result.Tender.PosBank);
                    totals[nameof(ReportRowDto.CollectionRatePercent)] = Pct(t.CollectionRatePercent);
                    totals[nameof(ReportRowDto.OutstandingBalance)] = Money(t.OutstandingBalance);
                    break;

                case AnalyticsModule.Logistics:
                {
                    var fleet = result.LogisticsSummary.Rows;
                    totals[nameof(ReportRowDto.Trips)] = fleet.Sum(r => r.Trips).ToString("N0");
                    totals[nameof(ReportRowDto.DistanceKm)] = fleet.Sum(r => r.DistanceKm).ToString("N0");
                    totals[nameof(ReportRowDto.Litres)] = fleet.Sum(r => r.Litres).ToString("N0");
                    totals[nameof(ReportRowDto.FuelCost)] = Money(fleet.Sum(r => r.FuelCost));
                    totals[nameof(ReportRowDto.MaintenanceCost)] = Money(fleet.Sum(r => r.MaintenanceCost));
                    totals[nameof(ReportRowDto.TripExpenses)] = Money(fleet.Sum(r => r.TripExpenses));

                    // Fleet averages, not sums: an on-time rate or a cost per unit
                    // added across vehicles would be meaningless.
                    totals[nameof(ReportRowDto.OnTimePercent)] = Pct(Avg(fleet, r => r.OnTimePercent));
                    totals[nameof(ReportRowDto.DeliveryEfficiencyIndex)] = Avg(fleet, r => r.DeliveryEfficiencyIndex).ToString("N0");
                    totals[nameof(ReportRowDto.CostPerUnitSold)] = Money(Avg(fleet, r => r.CostPerUnitSold));
                    break;
                }

                default:
                    totals[nameof(ReportRowDto.OrdersPlaced)] = t.OrdersPlaced.ToString("N0");
                    totals[nameof(ReportRowDto.Produced)] = t.Produced.ToString("N0");
                    totals[nameof(ReportRowDto.Loaded)] = t.Loaded.ToString("N0");
                    totals[nameof(ReportRowDto.Sold)] = t.Sold.ToString("N0");
                    totals[nameof(ReportRowDto.Returned)] = t.Returned.ToString("N0");
                    totals[nameof(ReportRowDto.Damaged)] = t.Damaged.ToString("N0");
                    totals[nameof(ReportRowDto.FillRatePercent)] = Pct(t.FillRatePercent);
                    totals[nameof(ReportRowDto.SellThroughPercent)] = Pct(t.SellThroughPercent);
                    totals[nameof(ReportRowDto.DamagePercent)] = Pct(t.DamagePercent);
                    break;
            }

            return totals;
        }

        private static decimal Avg(List<LogisticsPointDto> fleet, Func<LogisticsPointDto, decimal> selector) =>
            fleet.Count == 0 ? 0m : Math.Round(fleet.Average(selector), 1);

        private static string Money(decimal value) => ReportColumnCatalog.FormatMoney(value);

        private static string Pct(decimal value) => value.ToString("0.#") + "%";
    }
}
