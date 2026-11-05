using System.ComponentModel.DataAnnotations;

namespace Manufacture.Models.DTOs
{
    public class OrderItemDto
    {
        public int RecipeId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
    }

    public class CreateOrderDto
    {
        public string OrderType { get; set; } = "ImmediateSale"; // ImmediateSale / PreOrder
        public int CustomerId { get; set; }
        [Required]
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCategory { get; set; } = "WalkIn";
        public string PosTerminalCode { get; set; } = "POS-01";
        public List<OrderItemDto> Items { get; set; } = new();
        public decimal Discount { get; set; }
        public decimal AmountPaid { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, BankTransfer, PosBank, CreditOwing
        public DateTime TargetDeliveryDate { get; set; } = DateTime.UtcNow;
        public string Notes { get; set; } = string.Empty;
    }

    public class CustomerDto
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string MemberCode { get; set; } = string.Empty;
        public string Category { get; set; } = "WalkIn";
        [Required, Phone]
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal OutstandingBalance { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class PosTerminalDto
    {
        public int Id { get; set; }
        [Required]
        public string TerminalCode { get; set; } = string.Empty;
        [Required]
        public string TerminalName { get; set; } = string.Empty;
        public int? AssignedUserId { get; set; }
        public string CashAccountName { get; set; } = "Bakery Cash Drawer";
        public string TransferAccountName { get; set; } = "FirstBank - 3021948291";
        public string PosBankAccountName { get; set; } = "Moniepoint POS - 9028129";
        public bool IsActive { get; set; } = true;
    }

    public class SignOffLoadingInputDto
    {
        public int RouteSheetId { get; set; }
        public Dictionary<int, int> LoadedQuantities { get; set; } = new();
        public string DispatcherName { get; set; } = "Loading Bay Supervisor";
        public string Notes { get; set; } = string.Empty;
    }

    public class ReconciliationInputDto
    {
        public int RouteSheetId { get; set; }
        public Dictionary<int, int> SoldQuantities { get; set; } = new();
        public Dictionary<int, int> ReturnedQuantities { get; set; } = new();
        public Dictionary<int, int> DamagedQuantities { get; set; } = new();

        public decimal CashCollected { get; set; }
        public decimal BankTransferCollected { get; set; }
        public decimal PosTerminalCollected { get; set; }

        public string CashierName { get; set; } = "Main Cashier";
        public string Notes { get; set; } = string.Empty;
    }

    public class SetCustomPriceInputDto
    {
        public int AccountId { get; set; }
        public int ProductId { get; set; }
        public decimal CustomPrice { get; set; }
    }

    public class SupplySupermarketInputDto
    {
        public int AccountId { get; set; }
        public int RepId { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
        public string Notes { get; set; } = string.Empty;
    }
}
