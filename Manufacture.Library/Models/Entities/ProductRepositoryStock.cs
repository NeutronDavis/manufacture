using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class ProductRepositoryStock
{
    public int Id { get; set; }

    public int ProductRepositoryId { get; set; }

    public int RecipeId { get; set; }

    public decimal QuantityOnHand { get; set; }

    public decimal ReorderThreshold { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ProductRepository ProductRepository { get; set; } = null!;

    public virtual Recipe Recipe { get; set; } = null!;
}
