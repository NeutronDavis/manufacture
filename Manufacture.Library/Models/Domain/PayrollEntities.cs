namespace Manufacture.Models.Entities
{
    public class SalaryAdvance
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string StaffCode { get; set; } = string.Empty;
        public decimal PrincipalAmount { get; set; }
        public decimal MonthlyDeductionAmount { get; set; }
        public decimal TotalRepaid { get; set; }
        public decimal RemainingBalance => PrincipalAmount - TotalRepaid;
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "Active"; // Active, FullyRepaid, Cancelled
        public bool IsFullyRepaid => RemainingBalance <= 0;
    }

    public class PayrollRecord
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string StaffCode { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int Month { get; set; } = DateTime.UtcNow.Month;
        public int Year { get; set; } = DateTime.UtcNow.Year;
        public decimal BaseSalary { get; set; }
        public decimal PerformanceBonus { get; set; } // Incentives / extra shifts
        public decimal DisciplinaryDeductions { get; set; } // Loss or penalties
        public decimal AdvanceDeduction { get; set; } // Auto-deducted salary loan
        public decimal TaxAndLevies { get; set; }
        public decimal GrossSalary => BaseSalary + PerformanceBonus;
        public decimal TotalDeductions => DisciplinaryDeductions + AdvanceDeduction + TaxAndLevies;
        public decimal NetSalary => GrossSalary - TotalDeductions;
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, OnHold
        public DateTime? PaymentDate { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
