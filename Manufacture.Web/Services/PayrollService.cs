using Manufacture.Library.Calculations;
using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Manufacture.Services
{
    public class PayrollService : IPayrollService
    {
        private readonly ManufactureDbContext _db;
        private readonly ISettingsService _settingsService;

        public PayrollService(ManufactureDbContext db, ISettingsService settingsService)
        {
            _db = db;
            _settingsService = settingsService;
        }

        public async Task<List<SalaryAdvanceListDto>> GetAdvancesAsync(CancellationToken cancellationToken = default)
        {
            var settings = await _settingsService.GetPayrollSettingsAsync(cancellationToken);
            var advances = await _db.SalaryAdvances
                .AsNoTracking()
                .Include(a => a.Employee)
                .OrderByDescending(a => a.Id)
                .ToListAsync(cancellationToken);

            return advances.Select(a => MapAdvance(a, settings)).ToList();
        }

        public async Task<List<SalaryAdvanceListDto>> GetAdvancesForEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
        {
            var settings = await _settingsService.GetPayrollSettingsAsync(cancellationToken);
            var advances = await _db.SalaryAdvances
                .AsNoTracking()
                .Include(a => a.Employee)
                .Where(a => a.EmployeeId == employeeId)
                .OrderByDescending(a => a.Id)
                .ToListAsync(cancellationToken);

            return advances.Select(a => MapAdvance(a, settings)).ToList();
        }

        public async Task<SalaryAdvanceListDto?> GetAdvanceByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var settings = await _settingsService.GetPayrollSettingsAsync(cancellationToken);
            var advance = await _db.SalaryAdvances
                .AsNoTracking()
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            return advance == null ? null : MapAdvance(advance, settings);
        }

        public async Task<SalaryAdvanceListDto> CreateAdvanceAsync(
            SalaryAdvanceDto dto,
            string createdBy,
            CancellationToken cancellationToken = default)
        {
            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId, cancellationToken)
                ?? throw new InvalidOperationException("Select a valid employee.");

            if (!employee.IsActive || employee.EmploymentStatus == "Inactive")
            {
                throw new InvalidOperationException("Salary advances cannot be issued to an inactive employee.");
            }

            if (dto.PrincipalAmount <= 0)
            {
                throw new InvalidOperationException("Principal amount must be greater than zero.");
            }

            var settings = await _settingsService.GetPayrollSettingsAsync(cancellationToken);
            var maximumLoan = Math.Round(employee.BaseSalary * settings.MaxLoanPercentOfSalary / 100m, 2);
            if (maximumLoan > 0 && dto.PrincipalAmount > maximumLoan)
            {
                throw new InvalidOperationException(
                    $"The requested principal exceeds the maximum allowed loan of ₦{maximumLoan:N2} " +
                    $"({settings.MaxLoanPercentOfSalary:0.#}% of base salary).");
            }

            var monthlyDeduction = Math.Round(
                dto.PrincipalAmount * settings.LoanMonthlyRepaymentPercent / 100m,
                2,
                MidpointRounding.AwayFromZero);

            var advance = new SalaryAdvance
            {
                EmployeeId = employee.Id,
                PrincipalAmount = dto.PrincipalAmount,
                MonthlyDeductionAmount = monthlyDeduction,
                TotalRepaid = 0m,
                RequestDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Reason = dto.Reason.Trim(),
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.SalaryAdvances.Add(advance);
            await _db.SaveChangesAsync(cancellationToken);

            advance.Employee = employee;
            return MapAdvance(advance, settings);
        }

        public async Task<bool> UpdateAdvanceAsync(
            SalaryAdvanceDto dto,
            string updatedBy,
            CancellationToken cancellationToken = default)
        {
            var advance = await _db.SalaryAdvances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == dto.Id, cancellationToken);

            if (advance == null)
            {
                return false;
            }

            if (dto.PrincipalAmount < advance.TotalRepaid)
            {
                throw new InvalidOperationException(
                    "The principal amount cannot be less than the amount already repaid.");
            }

            var settings = await _settingsService.GetPayrollSettingsAsync(cancellationToken);
            advance.PrincipalAmount = dto.PrincipalAmount;
            advance.Reason = dto.Reason.Trim();

            var remaining = advance.PrincipalAmount - advance.TotalRepaid;
            advance.MonthlyDeductionAmount = LoanRecoveryCalculator.CalculateForRemaining(
                advance.PrincipalAmount,
                remaining,
                settings.LoanMonthlyRepaymentPercent);

            if (remaining <= 0)
            {
                advance.Status = "FullyRepaid";
            }
            else if (!string.Equals(advance.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                advance.Status = "Active";
            }

            advance.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DeleteAdvanceAsync(int id, CancellationToken cancellationToken = default)
        {
            var advance = await _db.SalaryAdvances.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (advance == null)
            {
                return false;
            }

            _db.SalaryAdvances.Remove(advance);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<List<PayrollDeductionLineDto>> GetAdvanceRepaymentsAsync(
            int advanceId,
            CancellationToken cancellationToken = default)
        {
            return await _db.PayrollDeductionLines
                .AsNoTracking()
                .Where(l => l.SalaryAdvanceId == advanceId)
                .OrderBy(l => l.CreatedAt)
                .Select(l => new PayrollDeductionLineDto
                {
                    Id = l.Id,
                    PayrollRecordId = l.PayrollRecordId,
                    DeductionType = l.DeductionType,
                    Description = l.Description,
                    Amount = l.Amount,
                    SalaryAdvanceId = l.SalaryAdvanceId,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<PayrollRunDto>> GetPayrollRunsAsync(
            DateOnly? generatedDate = null,
            CancellationToken cancellationToken = default)
        {
            var query = _db.PayrollRuns.AsNoTracking().AsQueryable();

            if (generatedDate.HasValue)
            {
                var start = generatedDate.Value.ToDateTime(TimeOnly.MinValue);
                var end = start.AddDays(1);
                query = query.Where(r => r.GeneratedAt >= start && r.GeneratedAt < end);
            }

            var runs = await query
                .OrderByDescending(r => r.GeneratedAt)
                .ThenByDescending(r => r.Id)
                .ToListAsync(cancellationToken);

            return runs.Select(MapRun).ToList();
        }

        public async Task<PayrollRunDetailsDto?> GetPayrollRunAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var run = await _db.PayrollRuns
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (run == null)
            {
                return null;
            }

            var records = await QueryPayrollRecords()
                .Where(p => p.PayrollRunId == id)
                .OrderBy(p => p.Employee.FullName)
                .ToListAsync(cancellationToken);

            return new PayrollRunDetailsDto
            {
                Run = MapRun(run),
                Records = records.Select(MapPayrollRecord).ToList()
            };
        }

        public async Task<List<PayrollRecordDto>> GetPayrollRecordsAsync(
            int month,
            int year,
            CancellationToken cancellationToken = default)
        {
            var records = await QueryPayrollRecords()
                .Where(p => p.PayrollMonth == month && p.PayrollYear == year)
                .OrderBy(p => p.Employee.FullName)
                .ToListAsync(cancellationToken);

            return records.Select(MapPayrollRecord).ToList();
        }

        public async Task<List<PayrollRecordDto>> GetAllPayrollRecordsAsync(
            DateOnly? generatedDate = null,
            CancellationToken cancellationToken = default)
        {
            var query = QueryPayrollRecords();

            if (generatedDate.HasValue)
            {
                var start = generatedDate.Value.ToDateTime(TimeOnly.MinValue);
                var end = start.AddDays(1);
                query = query.Where(p => p.CreatedAt >= start && p.CreatedAt < end);
            }

            var records = await query
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.PayrollYear)
                .ThenByDescending(p => p.PayrollMonth)
                .ThenBy(p => p.Employee.FullName)
                .ToListAsync(cancellationToken);

            return records.Select(MapPayrollRecord).ToList();
        }

        public async Task<PayrollRecordDto?> GetPayrollRecordAsync(
            int employeeId,
            int month,
            int year,
            CancellationToken cancellationToken = default)
        {
            var record = await QueryPayrollRecords()
                .FirstOrDefaultAsync(
                    p => p.EmployeeId == employeeId && p.PayrollMonth == month && p.PayrollYear == year,
                    cancellationToken);

            return record == null ? null : MapPayrollRecord(record);
        }

        public async Task<PayrollRecordDto?> GetPayrollRecordByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var record = await QueryPayrollRecords()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            return record == null ? null : MapPayrollRecord(record);
        }

        public async Task<List<PayrollRunEmployeeDto>> GetRunEmployeesAsync(
            int month,
            int year,
            CancellationToken cancellationToken = default)
        {
            var settings = await _settingsService.GetPayrollSettingsAsync(cancellationToken);
            var employees = await _db.Employees
                .AsNoTracking()
                .Where(e => e.IsActive && e.EmploymentStatus != "Inactive")
                .OrderBy(e => e.FullName)
                .ToListAsync(cancellationToken);

            var advances = await _db.SalaryAdvances
                .AsNoTracking()
                .Where(a => a.Status != "Cancelled")
                .ToListAsync(cancellationToken);

            // If this period was already processed but not paid, the run page must
            // preview the same values a re-run would calculate (the service reverses
            // the previous loan lines before recalculating).
            var priorLoanByAdvance = await _db.PayrollDeductionLines
                .AsNoTracking()
                .Where(l => l.SalaryAdvanceId.HasValue
                    && l.PayrollRecord.PayrollMonth == month
                    && l.PayrollRecord.PayrollYear == year)
                .GroupBy(l => l.SalaryAdvanceId!.Value)
                .Select(g => new { SalaryAdvanceId = g.Key, Amount = g.Sum(x => x.Amount) })
                .ToDictionaryAsync(x => x.SalaryAdvanceId, x => x.Amount, cancellationToken);

            var advancesByEmployee = advances
                .GroupBy(a => a.EmployeeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return employees.Select(employee =>
            {
                var employeeAdvances = (advancesByEmployee.GetValueOrDefault(employee.Id) ?? new List<SalaryAdvance>())
                    .Where(a => (a.Status == "Active" && a.TotalRepaid < a.PrincipalAmount)
                        || priorLoanByAdvance.ContainsKey(a.Id))
                    .ToList();
                var monthlyRecovery = 0m;
                var effectiveRemaining = 0m;

                foreach (var advance in employeeAdvances)
                {
                    var prior = priorLoanByAdvance.GetValueOrDefault(advance.Id);
                    var remaining = Math.Max(0m, advance.PrincipalAmount - advance.TotalRepaid + prior);
                    effectiveRemaining += remaining;
                    monthlyRecovery += LoanRecoveryCalculator.CalculateForRemaining(
                        advance.PrincipalAmount,
                        remaining,
                        settings.LoanMonthlyRepaymentPercent);
                }

                return new PayrollRunEmployeeDto
                {
                    EmployeeId = employee.Id,
                    FullName = employee.FullName,
                    StaffCode = employee.StaffCode,
                    Department = employee.Department,
                    BaseSalary = employee.BaseSalary,
                    ActiveLoanCount = employeeAdvances.Count,
                    MonthlyLoanRecovery = monthlyRecovery,
                    RemainingLoanBalance = effectiveRemaining
                };
            }).ToList();
        }

        public async Task<decimal> GetEstimatedNetPayoutAsync(int month, int year, CancellationToken cancellationToken = default)
        {
            var employees = await GetRunEmployeesAsync(month, year, cancellationToken);
            return employees.Sum(e => e.BaseSalary) - employees.Sum(e => e.MonthlyLoanRecovery);
        }

        public async Task ProcessMonthlyPayrollAsync(
            int month,
            int year,
            IReadOnlyList<PayrollEmployeeAdjustmentDto> adjustments,
            string processedBy,
            CancellationToken cancellationToken = default)
        {
            var settings = await _settingsService.GetPayrollSettingsAsync(cancellationToken);
            var now = DateTime.UtcNow;
            var changedBy = string.IsNullOrWhiteSpace(processedBy) ? "System" : processedBy.Trim();

            var run = await _db.PayrollRuns
                .FirstOrDefaultAsync(r => r.PayrollMonth == month && r.PayrollYear == year, cancellationToken);

            if (run == null)
            {
                run = new PayrollRun
                {
                    RunNumber = $"PAY-{year}-{month:00}",
                    PayrollMonth = (byte)month,
                    PayrollYear = (short)year,
                    GeneratedAt = now,
                    GeneratedBy = changedBy,
                    Status = "Pending",
                    Notes = $"Payroll run for {new DateTime(year, month, 1):MMMM yyyy}",
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _db.PayrollRuns.Add(run);
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                run.GeneratedBy = string.IsNullOrWhiteSpace(run.GeneratedBy) ? changedBy : run.GeneratedBy;
                run.UpdatedAt = now;
            }

            var employees = await _db.Employees
                .Where(e => e.IsActive && e.EmploymentStatus != "Inactive")
                .OrderBy(e => e.FullName)
                .ToListAsync(cancellationToken);

            var advances = await _db.SalaryAdvances
                .OrderBy(a => a.Id)
                .ToListAsync(cancellationToken);

            var existingRecords = await _db.PayrollRecords
                .Include(p => p.PayrollDeductionLines)
                .Where(p => p.PayrollMonth == month && p.PayrollYear == year)
                .ToListAsync(cancellationToken);

            if (existingRecords.Any(p => p.PaymentStatus == "Paid"))
            {
                throw new InvalidOperationException(
                    "Payroll for this period has already been paid and cannot be recalculated.");
            }

            // Re-running an unpaid payroll must not double-charge active loans.
            // Reverse the loan portions from the previous run before recalculating.
            var advancesById = advances.ToDictionary(a => a.Id);
            foreach (var existingLine in existingRecords
                .SelectMany(p => p.PayrollDeductionLines)
                .Where(l => l.SalaryAdvanceId.HasValue))
            {
                if (advancesById.TryGetValue(existingLine.SalaryAdvanceId!.Value, out var advance))
                {
                    advance.TotalRepaid = Math.Max(0m, advance.TotalRepaid - existingLine.Amount);
                    if (!string.Equals(advance.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    {
                        advance.Status = "Active";
                    }
                    advance.UpdatedAt = DateTime.UtcNow;
                }
            }

            var advancesByEmployee = advances
                .Where(a => a.Status == "Active" && a.TotalRepaid < a.PrincipalAmount)
                .GroupBy(a => a.EmployeeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var existingByEmployee = existingRecords.ToDictionary(p => p.EmployeeId);

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            foreach (var employee in employees)
            {
                var adjustment = adjustments.FirstOrDefault(a => a.EmployeeId == employee.Id);
                var employeeAdvances = advancesByEmployee.GetValueOrDefault(employee.Id) ?? new List<SalaryAdvance>();

                var baseSalary = employee.BaseSalary;
                var bonus = Math.Max(0m, adjustment?.PerformanceBonus ?? 0m);
                var disciplinary = Math.Max(0m, adjustment?.DisciplinaryDeductions ?? 0m);
                var housing = Round(baseSalary * settings.HousingAllowancePercent / 100m);
                var transport = Round(baseSalary * settings.TransportAllowancePercent / 100m);
                var taxableGross = baseSalary + bonus + housing + transport;
                var tax = Round(taxableGross * settings.PayeTaxPercent / 100m);
                var pension = Round(baseSalary * settings.PensionPercent / 100m);
                var health = Round(baseSalary * settings.HealthInsurancePercent / 100m);

                var loanLines = BuildLoanRecoveryPlan(
                    employeeAdvances,
                    settings.LoanMonthlyRepaymentPercent,
                    adjustment?.LoanRecoveryAmount);
                var loanRecovery = loanLines.Sum(l => l.Amount);

                if (!existingByEmployee.TryGetValue(employee.Id, out var record))
                {
                    record = new PayrollRecord
                    {
                        PayrollRunId = run.Id,
                        EmployeeId = employee.Id,
                        PayrollMonth = (byte)month,
                        PayrollYear = (short)year,
                        PaymentStatus = "Pending",
                        CreatedAt = now
                    };
                    _db.PayrollRecords.Add(record);
                    existingByEmployee[employee.Id] = record;
                }

                record.PayrollRunId = run.Id;
                record.BaseSalary = baseSalary;
                record.PerformanceBonus = bonus;
                record.HousingAllowance = housing;
                record.TransportAllowance = transport;
                record.OtherAllowances = 0m;
                record.DisciplinaryDeductions = disciplinary;
                record.AdvanceDeduction = loanRecovery;
                record.TaxAndLevies = tax;
                record.PensionDeduction = pension;
                record.HealthInsuranceDeduction = health;
                record.Notes = string.IsNullOrWhiteSpace(adjustment?.Notes) ? "Generated automatically" : adjustment!.Notes.Trim();
                record.UpdatedAt = now;

                if (record.PayrollDeductionLines.Count > 0)
                {
                    _db.PayrollDeductionLines.RemoveRange(record.PayrollDeductionLines);
                }

                AddDeductionLine(record, employee, "Tax", "PAYE tax and statutory levies", tax);
                AddDeductionLine(record, employee, "Pension", "Employee pension contribution", pension);
                AddDeductionLine(record, employee, "Health", "Health insurance contribution", health);

                if (disciplinary > 0)
                {
                    AddDeductionLine(record, employee, "Disciplinary", "Disciplinary / attendance penalty", disciplinary);
                }

                foreach (var (advance, amount) in loanLines)
                {
                    AddDeductionLine(
                        record,
                        employee,
                        "Loan",
                        $"Salary advance repayment — {advance.Reason} (Loan #{advance.Id})",
                        amount,
                        advance.Id);

                    advance.TotalRepaid += amount;
                    advance.MonthlyDeductionAmount = amount;
                    advance.UpdatedAt = now;
                    if (advance.TotalRepaid >= advance.PrincipalAmount)
                    {
                        advance.TotalRepaid = advance.PrincipalAmount;
                        advance.Status = "FullyRepaid";
                    }
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            await UpdateRunTotalsAsync(run.Id, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        public async Task<bool> MarkPaidAsync(int payrollId, string updatedBy, CancellationToken cancellationToken = default)
        {
            var record = await _db.PayrollRecords
                .FirstOrDefaultAsync(p => p.Id == payrollId, cancellationToken);
            if (record == null)
            {
                return false;
            }

            record.PaymentStatus = "Paid";
            record.PaymentDate = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            if (record.PayrollRunId.HasValue)
            {
                await UpdateRunTotalsAsync(record.PayrollRunId.Value, cancellationToken);
            }

            return true;
        }

        public async Task<bool> MarkRunPaidAsync(
            int payrollRunId,
            string updatedBy,
            CancellationToken cancellationToken = default)
        {
            var run = await _db.PayrollRuns
                .Include(r => r.PayrollRecords)
                .FirstOrDefaultAsync(r => r.Id == payrollRunId, cancellationToken);

            if (run == null)
            {
                return false;
            }

            var records = run.PayrollRecords.ToList();
            if (records.Count == 0)
            {
                return false;
            }

            var now = DateTime.UtcNow;
            foreach (var record in records)
            {
                record.PaymentStatus = "Paid";
                record.PaymentDate = now;
                record.UpdatedAt = now;
            }

            await _db.SaveChangesAsync(cancellationToken);
            await UpdateRunTotalsAsync(run.Id, cancellationToken);
            return true;
        }

        public async Task<List<PayrollDeductionLineDto>> GetDeductionLinesAsync(
            int payrollRecordId,
            CancellationToken cancellationToken = default)
        {
            return await _db.PayrollDeductionLines
                .AsNoTracking()
                .Where(l => l.PayrollRecordId == payrollRecordId)
                .OrderBy(l => l.Id)
                .Select(l => new PayrollDeductionLineDto
                {
                    Id = l.Id,
                    PayrollRecordId = l.PayrollRecordId,
                    DeductionType = l.DeductionType,
                    Description = l.Description,
                    Amount = l.Amount,
                    SalaryAdvanceId = l.SalaryAdvanceId,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }

        private IQueryable<PayrollRecord> QueryPayrollRecords()
        {
            return _db.PayrollRecords
                .AsNoTracking()
                .Include(p => p.Employee)
                .Include(p => p.PayrollDeductionLines);
        }

        private static SalaryAdvanceListDto MapAdvance(SalaryAdvance advance, PayrollSettingsDto settings)
        {
            var remaining = advance.PrincipalAmount - advance.TotalRepaid;
            var status = remaining <= 0
                ? "FullyRepaid"
                : advance.Status;

            var monthly = status == "Cancelled" || remaining <= 0
                ? 0m
                : CalculateMonthlyRecovery(advance, settings.LoanMonthlyRepaymentPercent);

            return new SalaryAdvanceListDto
            {
                Id = advance.Id,
                EmployeeId = advance.EmployeeId,
                EmployeeName = advance.Employee?.FullName ?? string.Empty,
                StaffCode = advance.Employee?.StaffCode ?? string.Empty,
                Department = advance.Employee?.Department ?? string.Empty,
                PrincipalAmount = advance.PrincipalAmount,
                TotalRepaid = advance.TotalRepaid,
                RemainingBalance = Math.Max(0m, remaining),
                MonthlyDeductionAmount = monthly,
                MonthlyRepaymentPercent = settings.LoanMonthlyRepaymentPercent,
                RequestDate = advance.RequestDate,
                Reason = advance.Reason,
                Status = status
            };
        }

        private static PayrollRunDto MapRun(PayrollRun run)
        {
            return new PayrollRunDto
            {
                Id = run.Id,
                RunNumber = run.RunNumber,
                Month = run.PayrollMonth,
                Year = run.PayrollYear,
                GeneratedAt = run.GeneratedAt,
                GeneratedBy = run.GeneratedBy,
                EmployeeCount = run.EmployeeCount,
                GrossTotal = run.GrossTotal,
                DeductionsTotal = run.DeductionsTotal,
                NetTotal = run.NetTotal,
                Status = run.Status,
                Notes = run.Notes
            };
        }

        private static PayrollRecordDto MapPayrollRecord(PayrollRecord record)
        {
            return new PayrollRecordDto
            {
                Id = record.Id,
                PayrollRunId = record.PayrollRunId,
                EmployeeId = record.EmployeeId,
                EmployeeName = record.Employee?.FullName ?? string.Empty,
                StaffCode = record.Employee?.StaffCode ?? string.Empty,
                Department = record.Employee?.Department ?? string.Empty,
                Month = record.PayrollMonth,
                Year = record.PayrollYear,
                BaseSalary = record.BaseSalary,
                PerformanceBonus = record.PerformanceBonus,
                HousingAllowance = record.HousingAllowance,
                TransportAllowance = record.TransportAllowance,
                OtherAllowances = record.OtherAllowances,
                DisciplinaryDeductions = record.DisciplinaryDeductions,
                AdvanceDeduction = record.AdvanceDeduction,
                TaxAndLevies = record.TaxAndLevies,
                PensionDeduction = record.PensionDeduction,
                HealthInsuranceDeduction = record.HealthInsuranceDeduction,
                GrossSalary = record.GrossSalary ?? 0m,
                TotalDeductions = record.TotalDeductions ?? 0m,
                NetSalary = record.NetSalary ?? 0m,
                PaymentStatus = record.PaymentStatus,
                PaymentDate = record.PaymentDate,
                Notes = record.Notes,
                CreatedAt = record.CreatedAt,
                DeductionLines = record.PayrollDeductionLines
                    .OrderBy(l => l.Id)
                    .Select(l => new PayrollDeductionLineDto
                    {
                        Id = l.Id,
                        PayrollRecordId = l.PayrollRecordId,
                        DeductionType = l.DeductionType,
                        Description = l.Description,
                        Amount = l.Amount,
                        SalaryAdvanceId = l.SalaryAdvanceId,
                        CreatedAt = l.CreatedAt
                    })
                    .ToList()
            };
        }

        private async Task UpdateRunTotalsAsync(int runId, CancellationToken cancellationToken)
        {
            var run = await _db.PayrollRuns.FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);
            if (run == null)
            {
                return;
            }

            var records = await _db.PayrollRecords
                .Where(p => p.PayrollRunId == runId)
                .ToListAsync(cancellationToken);

            run.EmployeeCount = records.Count;
            run.GrossTotal = records.Sum(p => p.GrossSalary ?? 0m);
            run.DeductionsTotal = records.Sum(p => p.TotalDeductions ?? 0m);
            run.NetTotal = records.Sum(p => p.NetSalary ?? 0m);
            run.Status = records.Count == 0
                ? "Pending"
                : records.All(p => p.PaymentStatus == "Paid")
                    ? "Paid"
                    : records.Any(p => p.PaymentStatus == "Paid")
                        ? "PartiallyPaid"
                        : "Pending";
            run.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
        }

        private static List<(SalaryAdvance Advance, decimal Amount)> BuildLoanRecoveryPlan(
            IReadOnlyList<SalaryAdvance> advances,
            decimal percent,
            decimal? overrideAmount)
        {
            var plan = new List<(SalaryAdvance Advance, decimal Amount)>();
            if (advances.Count == 0)
            {
                return plan;
            }

            if (overrideAmount.HasValue)
            {
                var requested = Math.Max(0m, overrideAmount.Value);
                var totalRemaining = advances.Sum(a => Math.Max(0m, a.PrincipalAmount - a.TotalRepaid));

                if (requested > totalRemaining + 0.005m)
                {
                    throw new InvalidOperationException(
                        $"Loan recovery cannot exceed the total outstanding balance of ₦{totalRemaining:N2}.");
                }

                var toAllocate = Math.Min(requested, totalRemaining);
                foreach (var advance in advances.OrderBy(a => a.Id))
                {
                    if (toAllocate <= 0)
                    {
                        break;
                    }

                    var balance = Math.Max(0m, advance.PrincipalAmount - advance.TotalRepaid);
                    if (balance <= 0)
                    {
                        continue;
                    }

                    var amount = Math.Min(toAllocate, balance);
                    plan.Add((advance, amount));
                    toAllocate -= amount;
                }

                return plan;
            }

            foreach (var advance in advances)
            {
                var amount = CalculateMonthlyRecovery(advance, percent);
                if (amount > 0)
                {
                    plan.Add((advance, amount));
                }
            }

            return plan;
        }

        private static decimal CalculateMonthlyRecovery(SalaryAdvance advance, decimal percent)
        {
            // Policy is a fixed percentage of the original principal. The final
            // installment is capped at the outstanding balance.
            return LoanRecoveryCalculator.CalculateScheduledRecovery(
                advance.PrincipalAmount,
                advance.TotalRepaid,
                percent);
        }

        private static decimal Round(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private void AddDeductionLine(
            PayrollRecord record,
            Employee employee,
            string type,
            string description,
            decimal amount,
            int? salaryAdvanceId = null)
        {
            if (amount <= 0)
            {
                return;
            }

            record.PayrollDeductionLines.Add(new PayrollDeductionLine
            {
                PayrollRecord = record,
                EmployeeId = employee.Id,
                DeductionType = type,
                Description = description,
                Amount = amount,
                SalaryAdvanceId = salaryAdvanceId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
