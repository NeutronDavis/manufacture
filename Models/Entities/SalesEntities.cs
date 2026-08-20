namespace Manufacture.Models.Entities
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
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
    }

    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty; // ORD-2026-001
        public OrderType Type { get; set; } = OrderType.ImmediateSale;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public CustomerCategory CustomerCategory { get; set; } = CustomerCategory.WalkIn;
        public int? SalesRepUserId { get; set; }
        public string? SalesRepName { get; set; }
        public string? PosTerminalCode { get; set; }
        public List<OrderItem> Items { get; set; } = new();
        public decimal SubTotal => Items.Sum(i => i.TotalPrice);
        public decimal Discount { get; set; }
        public decimal TotalAmount => SubTotal - Discount;
        public decimal AmountPaid { get; set; }
        public decimal BalanceDue => TotalAmount - AmountPaid;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime TargetDeliveryDate { get; set; } = DateTime.UtcNow;
        // Print-to-load and verification
        public bool IsPrintedForLoading { get; set; }
        public DateTime? PrintedAt { get; set; }
        public string? VerifiedByLoaderName { get; set; }
        public DateTime? LoadedAt { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
