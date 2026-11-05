using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class SalaryAdvance
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public decimal PrincipalAmount { get; set; }

    public decimal MonthlyDeductionAmount { get; set; }

    public decimal TotalRepaid { get; set; }

    public decimal? RemainingBalance { get; set; }

    public DateOnly RequestDate { get; set; }

    public string Reason { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
