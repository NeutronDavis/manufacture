using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Manufacture.Services
{
    public class SettingsService : ISettingsService
    {
        private const decimal DefaultCashPercent = 32.0m;
        private const decimal DefaultTolerancePercent = 6.0m;
        private const string DefaultTreasuryMemo =
            "Standard retail benchmark: 32% expected in physical cash; 68% routed via bank transfer and POS terminals to limit route cash-in-transit risk.";

        private readonly ManufactureDbContext _db;
        private readonly SettingsCache _cache;

        public SettingsService(ManufactureDbContext db, SettingsCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<TreasuryPolicySettings> GetTreasuryPolicyAsync(CancellationToken cancellationToken = default)
        {
            var row = await _db.TreasuryPolicySettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);

            return row == null
                ? new TreasuryPolicySettings()
                : new TreasuryPolicySettings
                {
                    PlannedCashPercent = row.PlannedCashPercent,
                    AlertTolerancePercent = row.AlertTolerancePercent,
                    PolicyMemo = row.PolicyMemo,
                    LastUpdatedUtc = row.LastUpdatedUtc,
                    UpdatedBy = row.UpdatedBy
                };
        }

        public async Task UpdateTreasuryPolicyAsync(
            decimal plannedCashPercent,
            decimal alertTolerancePercent,
            string policyMemo,
            string updatedBy,
            CancellationToken cancellationToken = default)
        {
            var row = await _db.TreasuryPolicySettings
                .FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);

            if (row == null)
            {
                row = new TreasuryPolicySetting { Id = 1 };
                _db.TreasuryPolicySettings.Add(row);
            }

            row.PlannedCashPercent = Math.Clamp(plannedCashPercent, 0m, 100m);
            row.AlertTolerancePercent = Math.Clamp(alertTolerancePercent, 0m, 100m);
            row.PolicyMemo = string.IsNullOrWhiteSpace(policyMemo) ? "Treasury benchmark updated." : policyMemo.Trim();
            row.LastUpdatedUtc = DateTime.UtcNow;
            row.UpdatedBy = string.IsNullOrWhiteSpace(updatedBy) ? "Admin" : updatedBy.Trim();

            await _db.SaveChangesAsync(cancellationToken);
            await RefreshCacheAsync(cancellationToken);
        }

        public async Task ResetTreasuryPolicyAsync(string updatedBy, CancellationToken cancellationToken = default)
        {
            await UpdateTreasuryPolicyAsync(
                DefaultCashPercent,
                DefaultTolerancePercent,
                DefaultTreasuryMemo,
                updatedBy,
                cancellationToken);
        }

        public async Task<PayrollSettingsDto> GetPayrollSettingsAsync(CancellationToken cancellationToken = default)
        {
            var row = await _db.PayrollSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);

            return row == null
                ? new PayrollSettingsDto()
                : new PayrollSettingsDto
                {
                    LoanMonthlyRepaymentPercent = row.LoanMonthlyRepaymentPercent,
                    MaxLoanPercentOfSalary = row.MaxLoanPercentOfSalary,
                    HousingAllowancePercent = row.HousingAllowancePercent,
                    TransportAllowancePercent = row.TransportAllowancePercent,
                    PayeTaxPercent = row.PayeTaxPercent,
                    PensionPercent = row.PensionPercent,
                    HealthInsurancePercent = row.HealthInsurancePercent,
                    UpdatedAt = row.UpdatedAt,
                    UpdatedBy = row.UpdatedBy
                };
        }

        public async Task UpdatePayrollSettingsAsync(
            PayrollSettingsDto settings,
            string updatedBy,
            CancellationToken cancellationToken = default)
        {
            var row = await _db.PayrollSettings
                .FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);

            if (row == null)
            {
                row = new PayrollSetting { Id = 1 };
                _db.PayrollSettings.Add(row);
            }

            row.LoanMonthlyRepaymentPercent = Math.Clamp(settings.LoanMonthlyRepaymentPercent, 0m, 100m);
            row.MaxLoanPercentOfSalary = Math.Clamp(settings.MaxLoanPercentOfSalary, 0m, 100m);
            row.HousingAllowancePercent = Math.Clamp(settings.HousingAllowancePercent, 0m, 100m);
            row.TransportAllowancePercent = Math.Clamp(settings.TransportAllowancePercent, 0m, 100m);
            row.PayeTaxPercent = Math.Clamp(settings.PayeTaxPercent, 0m, 100m);
            row.PensionPercent = Math.Clamp(settings.PensionPercent, 0m, 100m);
            row.HealthInsurancePercent = Math.Clamp(settings.HealthInsurancePercent, 0m, 100m);
            row.UpdatedAt = DateTime.UtcNow;
            row.UpdatedBy = string.IsNullOrWhiteSpace(updatedBy) ? "Admin" : updatedBy.Trim();

            await _db.SaveChangesAsync(cancellationToken);
            await RefreshCacheAsync(cancellationToken);
        }

        public async Task ResetPayrollSettingsAsync(string updatedBy, CancellationToken cancellationToken = default)
        {
            await UpdatePayrollSettingsAsync(new PayrollSettingsDto(), updatedBy, cancellationToken);
        }

        public async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
        {
            _cache.SetTreasuryPolicy(await GetTreasuryPolicyAsync(cancellationToken));
            _cache.SetPayrollSettings(await GetPayrollSettingsAsync(cancellationToken));
        }
    }
}
