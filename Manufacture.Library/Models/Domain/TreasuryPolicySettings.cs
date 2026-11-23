using System;

namespace Manufacture.Models.Domain
{
    /// <summary>
    /// Corporate treasury policy governing physical cash collection quotas,
    /// digital payment benchmarks (Transfer/POS), and sales rep cash float thresholds.
    /// </summary>
    public class TreasuryPolicySettings
    {
        /// <summary>
        /// Benchmark percentage of expected revenue planned to be settled in physical cash (e.g. 32.0m for 32%).
        /// </summary>
        public decimal PlannedCashPercent { get; set; } = 32.0m;

        /// <summary>
        /// Decimal fraction representation of the planned cash benchmark (e.g. 0.32m).
        /// </summary>
        public decimal PlannedCashShare => PlannedCashPercent / 100m;

        /// <summary>
        /// Expected percentage of digital tender (Bank Transfers + POS Terminals), which is 100% - PlannedCashPercent.
        /// </summary>
        public decimal PlannedDigitalPercent => Math.Max(0m, 100m - PlannedCashPercent);

        /// <summary>
        /// Tolerance percentage above the benchmark before flagging an account with 'Cash float above plan'.
        /// Default is 6.0%.
        /// </summary>
        public decimal AlertTolerancePercent { get; set; } = 6.0m;

        /// <summary>
        /// Administrative directive or memo explaining the reason for this benchmark.
        /// </summary>
        public string PolicyMemo { get; set; } = "Standard retail benchmark: 32% expected in physical cash; 68% routed via bank transfer and POS terminals to limit route cash-in-transit risk.";

        /// <summary>
        /// Timestamp when this policy was last updated.
        /// </summary>
        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The administrator who configured this policy.
        /// </summary>
        public string UpdatedBy { get; set; } = "SuperAdmin";
    }
}
