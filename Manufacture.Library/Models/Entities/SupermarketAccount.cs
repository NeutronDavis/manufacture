using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class SupermarketAccount
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public string AccountName { get; set; } = null!;

    public string ContactPerson { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Address { get; set; } = null!;

    public int? AssignedRepId { get; set; }

    public decimal CurrentOutstandingCredit { get; set; }

    public decimal CreditLimit { get; set; }

    public string PaymentTerms { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual SalesRep? AssignedRep { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<SupermarketAccountPrice> SupermarketAccountPrices { get; set; } = new List<SupermarketAccountPrice>();
}
