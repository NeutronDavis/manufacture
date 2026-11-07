using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;

namespace Manufacture.Library.Interfaces
{
    public interface ISettingsService
    {
        Task<TreasuryPolicySettings> GetTreasuryPolicyAsync(CancellationToken cancellationToken = default);

        Task UpdateTreasuryPolicyAsync(
            decimal plannedCashPercent,
            decimal alertTolerancePercent,
            string policyMemo,
            string updatedBy,
            CancellationToken cancellationToken = default);

        Task ResetTreasuryPolicyAsync(string updatedBy, CancellationToken cancellationToken = default);

        Task<PayrollSettingsDto> GetPayrollSettingsAsync(CancellationToken cancellationToken = default);

        Task UpdatePayrollSettingsAsync(PayrollSettingsDto settings, string updatedBy, CancellationToken cancellationToken = default);

        Task ResetPayrollSettingsAsync(string updatedBy, CancellationToken cancellationToken = default);

        Task RefreshCacheAsync(CancellationToken cancellationToken = default);
    }
}
