using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;

namespace Manufacture.Services
{
    /// <summary>
    /// Read-optimized settings snapshot for singleton services and synchronous
    /// Razor views. The database-backed SettingsService refreshes this cache.
    /// </summary>
    public class SettingsCache
    {
        private TreasuryPolicySettings _treasury = new();
        private PayrollSettingsDto _payroll = new();

        public TreasuryPolicySettings TreasuryPolicy => _treasury;

        public decimal PlannedCashPercent => _treasury.PlannedCashPercent;

        public decimal PlannedCashShare => _treasury.PlannedCashShare;

        public decimal AlertTolerancePercent => _treasury.AlertTolerancePercent;

        public PayrollSettingsDto Payroll => _payroll;

        public void SetTreasuryPolicy(TreasuryPolicySettings policy)
        {
            _treasury = policy;
        }

        public void SetPayrollSettings(PayrollSettingsDto settings)
        {
            _payroll = settings;
        }
    }
}
