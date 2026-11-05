using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class Customer
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string MemberCode { get; set; } = null!;

    public int Category { get; set; }

    public string PhoneNumber { get; set; } = null!;

    public string Address { get; set; } = null!;

    public decimal OutstandingBalance { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<CustomerReturn> CustomerReturns { get; set; } = new List<CustomerReturn>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual SupermarketAccount? SupermarketAccount { get; set; }
}
