using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class RecipeIngredient
{
    public int Id { get; set; }

    public int RecipeId { get; set; }

    public int IngredientId { get; set; }

    public string Unit { get; set; } = null!;

    public string QuantityUnit { get; set; } = null!;

    public decimal QuantityPerBatch { get; set; }

    public decimal BaselineYield { get; set; }

    public decimal IngredientUnitCost { get; set; }

    public decimal? QuantityPerUnit { get; set; }

    public decimal? BatchLineCost { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual Recipe Recipe { get; set; } = null!;
}
