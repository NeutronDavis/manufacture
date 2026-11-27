using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class ProductRepository
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int LocationType { get; set; }

    public string Address { get; set; } = null!;

    public bool IsDefaultProductionOutput { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<PosTerminal> PosTerminals { get; set; } = new List<PosTerminal>();

    public virtual ICollection<ProductRepositoryMovement> ProductRepositoryMovements { get; set; } = new List<ProductRepositoryMovement>();

    public virtual ICollection<ProductRepositoryStock> ProductRepositoryStocks { get; set; } = new List<ProductRepositoryStock>();

    public virtual ICollection<SalesReconciliation> SalesReconciliations { get; set; } = new List<SalesReconciliation>();
}
