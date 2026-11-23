using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class PayrollSetting
{
    public byte Id { get; set; }

    public decimal LoanMonthlyRepaymentPercent { get; set; }

    public decimal MaxLoanPercentOfSalary { get; set; }

    public decimal HousingAllowancePercent { get; set; }

    public decimal TransportAllowancePercent { get; set; }

    public decimal PayeTaxPercent { get; set; }

    public decimal PensionPercent { get; set; }

    public decimal HealthInsurancePercent { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string UpdatedBy { get; set; } = null!;
}
