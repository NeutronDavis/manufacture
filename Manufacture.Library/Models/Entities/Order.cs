using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class Order
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = null!;

    public int OrderType { get; set; }

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;

    public int CustomerCategory { get; set; }

    public int? SalesRepId { get; set; }

    public string? SalesRepName { get; set; }

    public int? PosTerminalId { get; set; }

    public string? PosTerminalCode { get; set; }

    public decimal SubTotal { get; set; }

    public decimal Discount { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal? BalanceDue { get; set; }

    public int PaymentMethod { get; set; }

    public int Status { get; set; }

    public DateTime OrderDate { get; set; }

    public DateTime TargetDeliveryDate { get; set; }

    public bool IsPrintedForLoading { get; set; }

    public DateTime? PrintedAt { get; set; }

    public string? VerifiedByLoaderName { get; set; }

    public DateTime? LoadedAt { get; set; }

    public string Notes { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<CustomerReturn> CustomerReturns { get; set; } = new List<CustomerReturn>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual PosTerminal? PosTerminal { get; set; }

    public virtual ICollection<ProductionBatch> ProductionBatches { get; set; } = new List<ProductionBatch>();

    public virtual SalesRep? SalesRep { get; set; }
}
