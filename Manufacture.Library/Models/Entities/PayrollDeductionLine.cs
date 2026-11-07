using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class PayrollDeductionLine
{
    public long Id { get; set; }

    public int PayrollRecordId { get; set; }

    public int EmployeeId { get; set; }

    public string DeductionType { get; set; } = null!;

    public string Description { get; set; } = null!;

    public decimal Amount { get; set; }

    public int? SalaryAdvanceId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual PayrollRecord PayrollRecord { get; set; } = null!;

    public virtual SalaryAdvance? SalaryAdvance { get; set; }
}
