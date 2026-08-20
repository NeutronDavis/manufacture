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
        [Required, Range(10, 100000000)]
        public decimal MonthlyDeductionAmount { get; set; }
        [Required]
        public string Reason { get; set; } = string.Empty;
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
        public string Notes { get; set; } = string.Empty;
    }
}
