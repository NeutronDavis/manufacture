using System;
using Manufacture.Models.Entities;

namespace Manufacture.Services
{
    /// <summary>
    /// In-memory singleton settings service storing live system configurations,
    /// including the benchmark treasury policy for sales reps and field cash management.
    /// </summary>
    public class MockSettingsService
    {
        private static readonly object _lock = new();
        private TreasuryPolicySettings _policy = new();

        /// <summary>
        /// Retrieves a snapshot of the current treasury policy.
        /// </summary>
        public TreasuryPolicySettings GetTreasuryPolicy()
        {
            lock (_lock)
            {
                return new TreasuryPolicySettings
                {
                    PlannedCashPercent = _policy.PlannedCashPercent,
                    AlertTolerancePercent = _policy.AlertTolerancePercent,
                    PolicyMemo = _policy.PolicyMemo,
                    LastUpdatedUtc = _policy.LastUpdatedUtc,
                    UpdatedBy = _policy.UpdatedBy
                };
            }
        }

        /// <summary>
        /// Returns the decimal fraction ratio of the cash benchmark (e.g. 0.32m for 32%).
        /// </summary>
        public decimal PlannedCashShare
        {
            get
            {
                lock (_lock)
                {
                    return _policy.PlannedCashShare;
                }
            }
        }

        /// <summary>
        /// Returns the percentage of the cash benchmark (e.g. 32.0m for 32%).
        /// </summary>
        public decimal PlannedCashPercent
        {
            get
            {
                lock (_lock)
                {
                    return _policy.PlannedCashPercent;
                }
            }
        }

        /// <summary>
        /// Returns the tolerance threshold percent over plan (e.g. 6.0m for 6%).
        /// </summary>
        public decimal AlertTolerancePercent
        {
            get
            {
                lock (_lock)
                {
                    return _policy.AlertTolerancePercent;
                }
            }
        }

        /// <summary>
        /// Updates the treasury policy and benchmarks with audit info.
        /// </summary>
        public void UpdateTreasuryPolicy(decimal plannedCashPercent, decimal alertTolerancePercent, string policyMemo, string updatedBy)
        {
            lock (_lock)
            {
                _policy.PlannedCashPercent = Math.Clamp(plannedCashPercent, 0m, 100m);
                _policy.AlertTolerancePercent = Math.Max(0m, alertTolerancePercent);
                _policy.PolicyMemo = string.IsNullOrWhiteSpace(policyMemo) ? "Treasury benchmark updated." : policyMemo.Trim();
                _policy.LastUpdatedUtc = DateTime.UtcNow;
                _policy.UpdatedBy = string.IsNullOrWhiteSpace(updatedBy) ? "Admin" : updatedBy.Trim();
            }
        }

        /// <summary>
        /// Restores the factory default 32% benchmark.
        /// </summary>
        public void ResetTreasuryPolicyToDefaults(string updatedBy)
        {
            lock (_lock)
            {
                _policy = new TreasuryPolicySettings
                {
                    PlannedCashPercent = 32.0m,
                    AlertTolerancePercent = 6.0m,
                    PolicyMemo = "Standard retail benchmark: 32% expected in physical cash; 68% routed via bank transfer and POS terminals to limit route cash-in-transit risk.",
                    LastUpdatedUtc = DateTime.UtcNow,
                    UpdatedBy = string.IsNullOrWhiteSpace(updatedBy) ? "SuperAdmin" : updatedBy.Trim()
                };
            }
        }
    }
}
