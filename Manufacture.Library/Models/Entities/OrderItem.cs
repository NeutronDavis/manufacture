using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int RecipeId { get; set; }

    public string ProductName { get; set; } = null!;

    public int Quantity { get; set; }

    public int BonusQuantity { get; set; }

    public decimal UnitPrice { get; set; }

    public int? TotalQuantity { get; set; }

    public decimal? LineTotal { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Recipe Recipe { get; set; } = null!;
}
