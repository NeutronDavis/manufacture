using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class SalesBonusRule
{
    public int Id { get; set; }

    public int RecipeId { get; set; }

    public int ThresholdQuantity { get; set; }

    public int BonusQuantity { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Recipe Recipe { get; set; } = null!;
}
