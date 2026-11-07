using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class PayrollRun
{
    public int Id { get; set; }

    public string RunNumber { get; set; } = null!;

    public byte PayrollMonth { get; set; }

    public short PayrollYear { get; set; }

    public DateTime GeneratedAt { get; set; }

    public string GeneratedBy { get; set; } = null!;

    public int EmployeeCount { get; set; }

    public decimal GrossTotal { get; set; }

    public decimal DeductionsTotal { get; set; }

    public decimal NetTotal { get; set; }

    public string Status { get; set; } = null!;

    public string Notes { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<PayrollRecord> PayrollRecords { get; set; } = new List<PayrollRecord>();
}
