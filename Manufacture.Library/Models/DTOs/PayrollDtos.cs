using System.ComponentModel.DataAnnotations;

namespace Manufacture.Models.DTOs
{
    public class SalaryAdvanceDto
    {
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required, Range(100, 100000000)]
        public decimal PrincipalAmount { get; set; }

        /// <summary>Legacy compatibility field; the DB-backed service derives this from the policy percent.</summary>
        public decimal MonthlyDeductionAmount { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;
    }

    public class SalaryAdvanceListDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string StaffCode { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal PrincipalAmount { get; set; }
        public decimal TotalRepaid { get; set; }
        public decimal RemainingBalance { get; set; }
        public decimal MonthlyDeductionAmount { get; set; }
        public decimal MonthlyRepaymentPercent { get; set; }
        public DateOnly RequestDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public bool IsFullyRepaid => RemainingBalance <= 0;
    }

    public class PayrollDeductionLineDto
    {
        public long Id { get; set; }
        public int PayrollRecordId { get; set; }
        public string DeductionType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int? SalaryAdvanceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PayrollRunDto
    {
        public int Id { get; set; }
        public string RunNumber { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public decimal GrossTotal { get; set; }
        public decimal DeductionsTotal { get; set; }
        public decimal NetTotal { get; set; }
        public string Status { get; set; } = "Pending";
        public string Notes { get; set; } = string.Empty;
    }

    public class PayrollRunDetailsDto
    {
        public PayrollRunDto Run { get; set; } = new();
        public List<PayrollRecordDto> Records { get; set; } = new();
    }

    public class PayrollRecordDto
    {
        public int Id { get; set; }
        public int? PayrollRunId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string StaffCode { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal PerformanceBonus { get; set; }
        public decimal HousingAllowance { get; set; }
        public decimal TransportAllowance { get; set; }
        public decimal OtherAllowances { get; set; }
        public decimal DisciplinaryDeductions { get; set; }
        public decimal AdvanceDeduction { get; set; }
        public decimal TaxAndLevies { get; set; }
        public decimal PensionDeduction { get; set; }
        public decimal HealthInsuranceDeduction { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
        public string PaymentStatus { get; set; } = "Pending";
        public DateTime? PaymentDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<PayrollDeductionLineDto> DeductionLines { get; set; } = new();
    }

    public class PayrollSettingsDto
    {
        public decimal LoanMonthlyRepaymentPercent { get; set; } = 20m;
        public decimal MaxLoanPercentOfSalary { get; set; } = 50m;
        public decimal HousingAllowancePercent { get; set; } = 12.5m;
        public decimal TransportAllowancePercent { get; set; } = 6.25m;
        public decimal PayeTaxPercent { get; set; } = 3.5m;
        public decimal PensionPercent { get; set; } = 5m;
        public decimal HealthInsurancePercent { get; set; } = 2.65m;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class PayrollRunEmployeeDto
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string StaffCode { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public int ActiveLoanCount { get; set; }
        public decimal MonthlyLoanRecovery { get; set; }
        public decimal RemainingLoanBalance { get; set; }
    }

    public class ProcessPayrollDto
    {
        [Required, Range(1, 12)]
        public int Month { get; set; } = DateTime.UtcNow.Month;

        [Required, Range(2020, 2050)]
        public int Year { get; set; } = DateTime.UtcNow.Year;

        public List<PayrollEmployeeAdjustmentDto> Adjustments { get; set; } = new();
    }

    public class PayrollEmployeeAdjustmentDto
    {
        public int EmployeeId { get; set; }
        public decimal PerformanceBonus { get; set; }
        public decimal DisciplinaryDeductions { get; set; }

        /// <summary>
        /// Optional manual loan recovery for this payroll run. When supplied, it
        /// overrides the policy amount (for example when an employee agrees to pay
        /// more than the standard percentage). It is capped at the outstanding balance.
        /// </summary>
        public decimal? LoanRecoveryAmount { get; set; }

        public string Notes { get; set; } = string.Empty;
    }
}
