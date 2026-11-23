namespace Manufacture.Models.Domain
{
    public enum CustomerCategory
    {
        WalkIn,
        SalesRep,
        ExternalVendor, // Wholesalers/Distributors
        Supermarket
    }

    public enum PaymentMethod
    {
        Cash,
        BankTransfer,
        PosBank,
        CreditOwing // Unpaid / Credit
    }

    public enum OrderType
    {
        ImmediateSale,
        PreOrder
    }

    public enum OrderStatus
    {
        Pending,
        Confirmed,
        InProduction,
        ReadyToLoad,
        Dispatched,
        Cancelled
    }

    public enum OrderPaymentStatus
    {
        Open,
        PartiallyPaid,
        Completed
    }

    public enum OrderBookedByType
    {
        Staff,
        Customer,
        System
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MemberCode { get; set; } = string.Empty; // e.g. "VND-1002", "SUP-401"
        public CustomerCategory Category { get; set; } = CustomerCategory.WalkIn;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal OutstandingBalance { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class PosTerminal
    {
        public int Id { get; set; }
        public string TerminalCode { get; set; } = string.Empty; // POS-01, POS-02
        public string TerminalName { get; set; } = string.Empty;
        public int? AssignedEmployeeId { get; set; }
        public string? AssignedEmployeeName { get; set; }
        public string? AssignedEmployeeCode { get; set; }
        public int? AssignedUserId { get; set; }
        public string? AssignedUserName { get; set; }
        // Multi-Account POS Mapping
        public string CashAccountName { get; set; } = "Bakery Cash Drawer";
        public string TransferAccountName { get; set; } = "FirstBank - 3021948291";
        public string PosBankAccountName { get; set; } = "Moniepoint POS - 9028129";
        public bool IsActive { get; set; } = true;
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int RecipeId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int BonusQuantity { get; set; }
        public int TotalQuantity => Quantity + BonusQuantity;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
    }

    /// <summary>
    /// A sales rep that carries product into the field. Reps are the reconciliation
    /// unit for the whole lifecycle: what they requested, sold, returned and damaged.
    ///
    /// This is the single owner of the field roster. Every module that needs a rep
    /// (sales orders, the executive dashboard, /reports) reads it from here, so a
    /// rep can never be one person on the dashboard and somebody else on an order.
    /// </summary>
    public class SalesRepProfile
    {
        public int Id { get; set; }

        /// <summary>The HR employee record for this rep.</summary>
        public int? EmployeeId { get; set; }

        /// <summary>HR employee code, e.g. EMP-007.</summary>
        public string? EmployeeCode { get; set; }

        /// <summary>The login account this rep signs in with, when they have one.</summary>
        public int? UserId { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;

        /// <summary>The register this rep sells from; must match a seeded PosTerminal.</summary>
        public string PosTerminalCode { get; set; } = string.Empty;

        /// <summary>The van this rep runs; must match a seeded Vehicle registration.</summary>
        public string VehicleRegistration { get; set; } = string.Empty;

        /// <summary>
        /// Which product lines this rep carries, e.g. "Bread &amp; Water". Drives the
        /// per-rep demand weighting, so a rep sells more of what they actually stock.
        /// </summary>
        public string ProductFocus { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        /// <summary>The product lines this rep carries, parsed from <see cref="ProductFocus"/>.</summary>
        public IEnumerable<string> FocusLines =>
            ProductFocus.Split('&', ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty; // ORD-2026-001
        public OrderType Type { get; set; } = OrderType.ImmediateSale;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public CustomerCategory CustomerCategory { get; set; } = CustomerCategory.WalkIn;
        public int? SalesRepId { get; set; }
        public int? SalesRepUserId { get; set; }
        public string? SalesRepName { get; set; }
        public string? PosTerminalCode { get; set; }
        public OrderBookedByType BookedByType { get; set; } = OrderBookedByType.Staff;
        public int? CreatedByUserId { get; set; }
        public List<OrderItem> Items { get; set; } = new();
        public decimal SubTotal => Items.Sum(i => i.TotalPrice);
        public decimal Discount { get; set; }
        public decimal TotalAmount => SubTotal - Discount;
        public decimal AmountPaid { get; set; }
        public decimal BalanceDue => TotalAmount - AmountPaid;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public OrderPaymentStatus PaymentStatus { get; set; } = OrderPaymentStatus.Open;
        public DateTime? ClosedAt { get; set; }
        public List<OrderPayment> Payments { get; set; } = new();
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime TargetDeliveryDate { get; set; } = DateTime.UtcNow;
        // Print-to-load and verification
        public bool IsPrintedForLoading { get; set; }
        public DateTime? PrintedAt { get; set; }
        public string? VerifiedByLoaderName { get; set; }
        public DateTime? LoadedAt { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class OrderPayment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public string Reference { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string ReceivedBy { get; set; } = string.Empty;
    }

    public class CustomerReturn
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public int? OrderId { get; set; }
        public int? CustomerId { get; set; }
        public int RecipeId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int? SalesRepId { get; set; }
        public int Quantity { get; set; }
        public int ReplacementQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal RefundAmount => Quantity * UnitPrice;
        public DateTime ReturnDate { get; set; } = DateTime.UtcNow;
        public string Reason { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string RecordedBy { get; set; } = string.Empty;
    }

    public class BonusRule
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public string RecipeCode { get; set; } = string.Empty;
        public int ThresholdQuantity { get; set; }
        public int BonusQuantity { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
