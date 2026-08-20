using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Dashboard
{
    public class IndexModel : PageModel
    {
        private readonly MockDashboardService _dashboardService;
        private readonly MockSalesService _salesService;
        private readonly MockProductionService _productionService;
        private readonly MockInventoryService _inventoryService;
        private readonly MockEmployeeService _employeeService;
        private readonly MockPayrollService _payrollService;
        private readonly MockLogisticsService _logisticsService;

        public IndexModel(
            MockDashboardService dashboardService,
            MockSalesService salesService,
            MockProductionService productionService,
            MockInventoryService inventoryService,
            MockEmployeeService employeeService,
            MockPayrollService payrollService,
            MockLogisticsService logisticsService)
        {
            _dashboardService = dashboardService;
            _salesService = salesService;
            _productionService = productionService;
            _inventoryService = inventoryService;
            _employeeService = employeeService;
            _payrollService = payrollService;
            _logisticsService = logisticsService;
        }

        public string ActiveRole { get; set; } = "SuperAdmin";
        public string ActiveUserName { get; set; } = "Adekunle Johnson";

        // Executive / Shared data
        public DashboardSummaryDto Summary { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();
        public List<ProductionBatch> TodayBatches { get; set; } = new();
        public List<InventoryItem> StockAlertItems { get; set; } = new();
        public List<Recipe> TopRecipes { get; set; } = new();

        // Store Manager
        public List<StoreRequest> PendingStoreRequests { get; set; } = new();
        public List<Supplier> Suppliers { get; set; } = new();

        // Sales Rep
        public PosTerminal? AssignedTerminal { get; set; }
        public List<Order> UnprintedOrders { get; set; } = new();
        public List<Order> RepTodayOrders { get; set; } = new();

        // Vendor / Supermarket Client
        public Customer? VendorAccount { get; set; }
        public List<Order> VendorOrders { get; set; } = new();

        // HR & Payroll
        public List<Employee> Employees { get; set; } = new();
        public List<SalaryAdvance> PendingAdvances { get; set; } = new();
        public List<PayrollRecord> RecentPayroll { get; set; } = new();

        // Logistics
        public List<Vehicle> Fleet { get; set; } = new();
        public List<FuelLog> RecentFuelLogs { get; set; } = new();

        public void OnGet()
        {
            ActiveRole = HttpContext.Session.GetString("UserRole") ?? "SuperAdmin";
            ActiveUserName = HttpContext.Session.GetString("UserName") ?? "Manager";

            Summary = _dashboardService.GetSummary();
            RecentOrders = _salesService.GetAllOrders().Take(8).ToList();
            TodayBatches = _productionService.GetAllBatches();
            StockAlertItems = _inventoryService.GetAllItems().OrderBy(i => i.QuantityInStock / (i.ReorderThreshold == 0 ? 1 : i.ReorderThreshold)).Take(5).ToList();
            TopRecipes = _productionService.GetAllRecipes();

            // Load Store Data
            PendingStoreRequests = _inventoryService.GetAllRequests().Where(r => r.Status == StoreRequestStatus.Pending).ToList();
            Suppliers = _inventoryService.GetAllSuppliers();

            // Load Sales Rep Data
            var terminals = _salesService.GetAllTerminals();
            AssignedTerminal = terminals.FirstOrDefault(t => t.TerminalCode == "POS-01") ?? terminals.FirstOrDefault();
            UnprintedOrders = _salesService.GetAllOrders().Where(o => !o.IsPrintedForLoading).ToList();
            RepTodayOrders = _salesService.GetAllOrders().Where(o => o.OrderDate.Date == DateTime.UtcNow.Date).ToList();

            // Load Vendor Data
            var customers = _salesService.GetAllCustomers();
            VendorAccount = customers.FirstOrDefault(c => c.Category == CustomerCategory.Supermarket) ?? customers.FirstOrDefault();
            if (VendorAccount != null)
            {
                VendorOrders = _salesService.GetAllOrders().Where(o => o.CustomerId == VendorAccount.Id).ToList();
            }

            // Load HR & Payroll Data
            Employees = _employeeService.GetAll();
            PendingAdvances = _payrollService.GetAllAdvances().Where(a => a.Status == "Active" && a.RemainingBalance > 0).ToList();
            RecentPayroll = _payrollService.GetAllPayrollRecords();

            // Load Logistics Data
            Fleet = _logisticsService.GetAllVehicles();
            RecentFuelLogs = _logisticsService.GetAllFuelLogs().Take(5).ToList();
        }
    }
}
