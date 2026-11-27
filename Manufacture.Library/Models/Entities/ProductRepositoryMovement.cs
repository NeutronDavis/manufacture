using System;

namespace Manufacture.Models.Entities;

public partial class ProductRepositoryMovement
{
    public long Id { get; set; }

    public int ProductRepositoryId { get; set; }

    public int RecipeId { get; set; }

    public int MovementType { get; set; }

    public decimal Quantity { get; set; }

    public decimal BalanceAfter { get; set; }

    public string ReferenceType { get; set; } = null!;

    public int? ReferenceId { get; set; }

    public string ReferenceNumber { get; set; } = null!;

    public string Notes { get; set; } = null!;

    public string PerformedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ProductRepository ProductRepository { get; set; } = null!;

    public virtual Recipe Recipe { get; set; } = null!;
}
