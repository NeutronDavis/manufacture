using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class TreasuryPolicySetting
{
    public byte Id { get; set; }

    public decimal PlannedCashPercent { get; set; }

    public decimal AlertTolerancePercent { get; set; }

    public string PolicyMemo { get; set; } = null!;

    public DateTime LastUpdatedUtc { get; set; }

    public string UpdatedBy { get; set; } = null!;
}
