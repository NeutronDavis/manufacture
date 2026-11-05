using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Services
{
    public class MockDashboardService
    {
        private readonly MockSalesService _salesService;
        private readonly MockProductionService _productionService;
        private readonly MockInventoryService _inventoryService;
        private readonly MockEmployeeService _employeeService;

        public MockDashboardService(
            MockSalesService salesService,
            MockProductionService productionService,
            MockInventoryService inventoryService,
            MockEmployeeService employeeService)
        {
            _salesService = salesService;
            _productionService = productionService;
            _inventoryService = inventoryService;
            _employeeService = employeeService;
        }

        public DashboardSummaryDto GetSummary()
        {
            var orders = _salesService.GetAllOrders();
            var today = DateTime.UtcNow.Date;
            var todayOrders = orders.Where(o => o.OrderDate.Date == today).ToList();

            var preOrders = _salesService.GetPreOrders().Where(o => o.Status != OrderStatus.Dispatched && o.Status != OrderStatus.Cancelled).ToList();
            var lowStockItems = _inventoryService.GetLowStockItems();
            var batches = _productionService.GetAllBatches().Where(b => b.ProductionDate.Date == today).ToList();

            var cashSales = orders.Where(o => o.PaymentMethod == PaymentMethod.Cash).Sum(o => o.AmountPaid);
            var transferSales = orders.Where(o => o.PaymentMethod == PaymentMethod.BankTransfer).Sum(o => o.AmountPaid);
            var posSales = orders.Where(o => o.PaymentMethod == PaymentMethod.PosBank).Sum(o => o.AmountPaid);
            var unpaidSales = orders.Sum(o => o.BalanceDue);

            var customers = _salesService.GetAllCustomers();

            return new DashboardSummaryDto
            {
                TodaySalesTotal = todayOrders.Sum(o => o.TotalAmount),
                MonthlySalesTotal = orders.Sum(o => o.TotalAmount),
                TodayOrdersCount = todayOrders.Count,
                PendingPreOrdersCount = preOrders.Count,
                TargetProductionLoavesToday = batches.Sum(b => b.TargetQuantity),
                ActualProducedLoavesToday = batches.Sum(b => b.ActualQuantity),
                LowStockItemsCount = lowStockItems.Count,
                ActiveEmployeesCount = _employeeService.GetAll(includeInactive: false).Count,
                TotalOutstandingCredit = customers.Sum(c => c.OutstandingBalance),
                CashSalesAmount = cashSales,
                BankTransferSalesAmount = transferSales,
                PosBankSalesAmount = posSales,
                UnpaidCreditSalesAmount = unpaidSales
            };
        }
    }
}
