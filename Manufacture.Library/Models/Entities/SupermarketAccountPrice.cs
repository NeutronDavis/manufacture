using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class SupermarketAccountPrice
{
    public int Id { get; set; }

    public int SupermarketAccountId { get; set; }

    public int RecipeId { get; set; }

    public decimal AgreedPrice { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Recipe Recipe { get; set; } = null!;

    public virtual SupermarketAccount SupermarketAccount { get; set; } = null!;
}
