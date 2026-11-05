using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class CustomerReturn
{
    public int Id { get; set; }

    public string ReturnNumber { get; set; } = null!;

    public int? OrderId { get; set; }

    public int? CustomerId { get; set; }

    public int RecipeId { get; set; }

    public int? SalesRepId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? RefundAmount { get; set; }

    public DateTime ReturnDate { get; set; }

    public string Reason { get; set; } = null!;

    public bool ReplacementIssued { get; set; }

    public string Notes { get; set; } = null!;

    public string RecordedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Order? Order { get; set; }

    public virtual Recipe Recipe { get; set; } = null!;

    public virtual SalesRep? SalesRep { get; set; }
}
