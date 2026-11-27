namespace Manufacture.Models.DTOs
{
    public enum SalesReconciliationStatus
    {
        Draft = 0,
        Reconciled = 1
    }

    public class SalesReconciliationDto
    {
        public int Id { get; set; }
        public string ReconciliationNumber { get; set; } = string.Empty;
        public DateTime ReconciliationDate { get; set; }
        public int SalesRepId { get; set; }
        public string RepName { get; set; } = string.Empty;
        public string RepCode { get; set; } = string.Empty;
        public int? PosTerminalId { get; set; }
        public string PosTerminalCode { get; set; } = string.Empty;
        public int? ProductRepositoryId { get; set; }
        public string ProductRepositoryName { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string VehicleRegistration { get; set; } = string.Empty;
        public decimal CashHandedOver { get; set; }
        public decimal PosTerminalReceipts { get; set; }
        public decimal TotalCollected => CashHandedOver + PosTerminalReceipts;
        public decimal TotalExpectedRevenue { get; set; }
        public decimal Variance => TotalExpectedRevenue - TotalCollected;
        public SalesReconciliationStatus Status { get; set; }
        public bool IsReconciled => Status == SalesReconciliationStatus.Reconciled;
        public string Notes { get; set; } = string.Empty;
        public string? ReconciledBy { get; set; }
        public DateTime? ReconciledAt { get; set; }
        public DateTime? StockPostedAt { get; set; }
        public bool CanEditOpening { get; set; }
        public List<SalesReconciliationItemDto> Items { get; set; } = new();
    }

    public class SalesReconciliationItemDto
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string OutputUnit { get; set; } = "units";
        public decimal UnitPrice { get; set; }
        public int OpeningStock { get; set; }
        public int QtySold { get; set; }
        public int QtyDamaged { get; set; }
        public int QtyReplacement { get; set; }
        public int ClosingStock => OpeningStock - QtySold - QtyDamaged - QtyReplacement;
        public decimal ExpectedRevenue => QtySold * UnitPrice;
        public int SystemSoldQuantity { get; set; }
    }

    public class SaveReconciliationDto
    {
        public int Id { get; set; }
        public decimal CashHandedOver { get; set; }
        public decimal PosTerminalReceipts { get; set; }
        public string Notes { get; set; } = string.Empty;
        public List<SaveReconciliationItemDto> Items { get; set; } = new();
    }

    public class SaveReconciliationItemDto
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public int OpeningStock { get; set; }
        public int QtySold { get; set; }
        public int QtyDamaged { get; set; }
        public int QtyReplacement { get; set; }
    }

    public class ReconciliationSummaryDto
    {
        public int Id { get; set; }
        public string ReconciliationNumber { get; set; } = string.Empty;
        public DateTime ReconciliationDate { get; set; }
        public int SalesRepId { get; set; }
        public string RepName { get; set; } = string.Empty;
        public string PosTerminalCode { get; set; } = string.Empty;
        public string ProductRepositoryName { get; set; } = string.Empty;
        public SalesReconciliationStatus Status { get; set; }
        public decimal TotalExpectedRevenue { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal Variance { get; set; }
    }
}
