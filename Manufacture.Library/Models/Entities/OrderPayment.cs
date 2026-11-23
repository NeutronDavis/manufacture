using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class OrderPayment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public int PaymentMethod { get; set; }

    public DateTime PaymentDate { get; set; }

    public string Reference { get; set; } = null!;

    public string Notes { get; set; } = null!;

    public string ReceivedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
