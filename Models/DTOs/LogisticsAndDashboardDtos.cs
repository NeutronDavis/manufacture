using System.ComponentModel.DataAnnotations;

namespace Manufacture.Models.DTOs
{
    public class VehicleDto
    {
        public int Id { get; set; }
        [Required]
        public string RegistrationNumber { get; set; } = string.Empty;
        [Required]
        public string MakeAndModel { get; set; } = string.Empty;
        public string Type { get; set; } = "Van";
        [Required]
        public string AssignedDriverName { get; set; } = string.Empty;
        [Required, Phone]
        public string DriverPhoneNumber { get; set; } = string.Empty;
        public string FuelType { get; set; } = "PMS (Petrol)";
        public decimal TankCapacityLitres { get; set; } = 70;
        public decimal CurrentOdometerKm { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class FuelLogDto
    {
        public int Id { get; set; }
        [Required]
        public int VehicleId { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        [Required, Range(0.1, 10000)]
        public decimal LitresDispensed { get; set; }
        [Required, Range(1, 100000)]
        public decimal PricePerLitre { get; set; }
        [Range(0, 10000000)]
        public decimal OdometerReadingKm { get; set; }
        [Required]
        public string PetrolStation { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;
        public string RouteDescription { get; set; } = string.Empty;
    }

    public class DashboardSummaryDto
    {
        public decimal TodaySalesTotal { get; set; }
        public decimal MonthlySalesTotal { get; set; }
        public int TodayOrdersCount { get; set; }
        public int PendingPreOrdersCount { get; set; }
        public int TargetProductionLoavesToday { get; set; }
        public int ActualProducedLoavesToday { get; set; }
        public int LowStockItemsCount { get; set; }
        public int ActiveEmployeesCount { get; set; }
        public decimal TotalOutstandingCredit { get; set; }

        // Payment Split Breakdown
        public decimal CashSalesAmount { get; set; }
        public decimal BankTransferSalesAmount { get; set; }
        public decimal PosBankSalesAmount { get; set; }
        public decimal UnpaidCreditSalesAmount { get; set; }
    }
}
