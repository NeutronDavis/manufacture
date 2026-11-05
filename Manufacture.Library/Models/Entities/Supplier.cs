using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class Supplier
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string ContactPerson { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string SupplyCategory { get; set; } = null!;

    public string Address { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();

    public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
