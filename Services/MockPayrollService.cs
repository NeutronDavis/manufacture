using Manufacture.Models.Entities;
using Manufacture.Models.DTOs;

namespace Manufacture.Services
{
    public class MockPayrollService
    {
        private readonly List<SalaryAdvance> _advances = new();
        private readonly List<PayrollRecord> _payrollRecords = new();
        private readonly MockEmployeeService _employeeService;

        public MockPayrollService(MockEmployeeService employeeService)
        {
            _employeeService = employeeService;
            SeedInitialData();
        }

        private void SeedInitialData()
        {
            // Seed Salary Advances
            _advances.AddRange(new[]
            {
                new SalaryAdvance
                {
                    Id = 1,
                    EmployeeId = 1,
                    EmployeeName = "Emeka Obi",
                    StaffCode = "EMP-001",
                    PrincipalAmount = 60000,
                    MonthlyDeductionAmount = 20000,
                    TotalRepaid = 40000, // 20k left
                    RequestDate = DateTime.UtcNow.AddMonths(-2),
                    Reason = "Family Emergency / Child School Fees",
                    Status = "Active"
                },
                new SalaryAdvance
                {
                    Id = 2,
                    EmployeeId = 3,
                    EmployeeName = "Usman Danjuma",
                    StaffCode = "EMP-003",
                    PrincipalAmount = 30000,
                    MonthlyDeductionAmount = 15000,
                    TotalRepaid = 15000, // 15k left
                    RequestDate = DateTime.UtcNow.AddMonths(-1),
                    Reason = "Vehicle Repair Support",
                    Status = "Active"
                }
            });

            // Seed Previous Payroll Records (Current Month & Last Month)
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;

            _payrollRecords.AddRange(new[]
            {
                new PayrollRecord
                {
                    Id = 1,
                    EmployeeId = 1,
                    EmployeeName = "Emeka Obi",
                    StaffCode = "EMP-001",
                    Department = "Production",
                    Month = currentMonth,
                    Year = currentYear,
                    BaseSalary = 120000,
                    PerformanceBonus = 15000, // extra output incentive
                    DisciplinaryDeductions = 0,
                    AdvanceDeduction = 20000,
                    TaxAndLevies = 5000,
                    PaymentStatus = "Pending",
                    Notes = "Extra morning shift incentive included"
                },
                new PayrollRecord
                {
                    Id = 2,
                    EmployeeId = 2,
                    EmployeeName = "Folashade Adeyemi",
                    StaffCode = "EMP-002",
                    Department = "Sales",
                    Month = currentMonth,
                    Year = currentYear,
                    BaseSalary = 95000,
                    PerformanceBonus = 12500, // POS sales commission
                    DisciplinaryDeductions = 0,
                    AdvanceDeduction = 0,
                    TaxAndLevies = 4000,
                    PaymentStatus = "Pending",
                    Notes = "Target achieved bonus"
                },
                new PayrollRecord
                {
                    Id = 3,
                    EmployeeId = 3,
                    EmployeeName = "Usman Danjuma",
                    StaffCode = "EMP-003",
                    Department = "Logistics",
                    Month = currentMonth,
                    Year = currentYear,
                    BaseSalary = 85000,
                    PerformanceBonus = 5000,
                    DisciplinaryDeductions = 2000, // Late reporting
                    AdvanceDeduction = 15000,
                    TaxAndLevies = 3000,
                    PaymentStatus = "Pending",
                    Notes = "Advance deduction of 15,000 applied"
                },
                new PayrollRecord
                {
                    Id = 4,
                    EmployeeId = 4,
                    EmployeeName = "Kelechi Nnamdi",
                    StaffCode = "EMP-004",
                    Department = "Store",
                    Month = currentMonth,
                    Year = currentYear,
                    BaseSalary = 80000,
                    PerformanceBonus = 4000,
                    DisciplinaryDeductions = 0,
                    AdvanceDeduction = 0,
                    TaxAndLevies = 3000,
                    PaymentStatus = "Pending",
                    Notes = "Store audit accuracy incentive"
                }
            });
        }

        // Advances
        public List<SalaryAdvance> GetAllAdvances() => _advances.OrderByDescending(a => a.Id).ToList();
        public List<SalaryAdvance> GetActiveAdvancesForEmployee(int employeeId) =>
            _advances.Where(a => a.EmployeeId == employeeId && !a.IsFullyRepaid).ToList();

        public SalaryAdvance CreateAdvance(SalaryAdvanceDto dto)
        {
            var emp = _employeeService.GetById(dto.EmployeeId);
            var advance = new SalaryAdvance
            {
                Id = _advances.Any() ? _advances.Max(a => a.Id) + 1 : 1,
                EmployeeId = dto.EmployeeId,
                EmployeeName = emp?.FullName ?? "Unknown",
                StaffCode = emp?.StaffCode ?? "EMP-XXX",
                PrincipalAmount = dto.PrincipalAmount,
                MonthlyDeductionAmount = dto.MonthlyDeductionAmount,
                TotalRepaid = 0,
                RequestDate = DateTime.UtcNow,
                Reason = dto.Reason,
                Status = "Active"
            };
            _advances.Add(advance);
            return advance;
        }

        // Payroll
        public List<PayrollRecord> GetPayrollForMonth(int month, int year) =>
            _payrollRecords.Where(p => p.Month == month && p.Year == year).ToList();

        public List<PayrollRecord> GetAllPayrollRecords() => _payrollRecords.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToList();

        public void ProcessMonthlyPayroll(int month, int year, List<PayrollEmployeeAdjustmentDto> adjustments)
        {
            var employees = _employeeService.GetAll(includeInactive: false);

            foreach (var emp in employees)
            {
                var adj = adjustments.FirstOrDefault(a => a.EmployeeId == emp.Id);
                var activeAdvances = GetActiveAdvancesForEmployee(emp.Id);
                decimal advanceDeduction = 0;

                foreach (var adv in activeAdvances)
                {
                    decimal toDeduct = Math.Min(adv.MonthlyDeductionAmount, adv.RemainingBalance);
                    advanceDeduction += toDeduct;
                    adv.TotalRepaid += toDeduct;
                    if (adv.IsFullyRepaid) adv.Status = "FullyRepaid";
                }

                var existing = _payrollRecords.FirstOrDefault(p => p.EmployeeId == emp.Id && p.Month == month && p.Year == year);
                if (existing != null)
                {
                    existing.BaseSalary = emp.BaseSalary;
                    existing.PerformanceBonus = adj?.PerformanceBonus ?? existing.PerformanceBonus;
                    existing.DisciplinaryDeductions = adj?.DisciplinaryDeductions ?? existing.DisciplinaryDeductions;
                    existing.AdvanceDeduction = advanceDeduction;
                    existing.Notes = adj?.Notes ?? existing.Notes;
                }
                else
                {
                    var record = new PayrollRecord
                    {
                        Id = _payrollRecords.Any() ? _payrollRecords.Max(r => r.Id) + 1 : 1,
                        EmployeeId = emp.Id,
                        EmployeeName = emp.FullName,
                        StaffCode = emp.StaffCode,
                        Department = emp.Department,
                        Month = month,
                        Year = year,
                        BaseSalary = emp.BaseSalary,
                        PerformanceBonus = adj?.PerformanceBonus ?? 0,
                        DisciplinaryDeductions = adj?.DisciplinaryDeductions ?? 0,
                        AdvanceDeduction = advanceDeduction,
                        TaxAndLevies = Math.Round(emp.BaseSalary * 0.035m, 2),
                        PaymentStatus = "Pending",
                        Notes = adj?.Notes ?? "Generated automatically"
                    };
                    _payrollRecords.Add(record);
                }
            }
        }

        public bool MarkPaid(int payrollId)
        {
            var record = _payrollRecords.FirstOrDefault(p => p.Id == payrollId);
            if (record == null) return false;
            record.PaymentStatus = "Paid";
            record.PaymentDate = DateTime.UtcNow;
            return true;
        }
    }
}
