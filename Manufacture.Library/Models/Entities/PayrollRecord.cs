using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class PayrollRecord
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public byte PayrollMonth { get; set; }

    public short PayrollYear { get; set; }

    public decimal BaseSalary { get; set; }

    public decimal PerformanceBonus { get; set; }

    public decimal DisciplinaryDeductions { get; set; }

    public decimal AdvanceDeduction { get; set; }

    public decimal TaxAndLevies { get; set; }

    public decimal? GrossSalary { get; set; }

    public decimal? TotalDeductions { get; set; }

    public decimal? NetSalary { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public DateTime? PaymentDate { get; set; }

    public string Notes { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
