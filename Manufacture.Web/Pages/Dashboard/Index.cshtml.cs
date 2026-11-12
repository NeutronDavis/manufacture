using Manufacture.Library.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.DTOs;
using Manufacture.Models.Domain;

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
        private readonly MockAnalyticsService _analytics;
        private readonly ISalesService _dbSalesService;

        public IndexModel(
            MockDashboardService dashboardService,
            MockSalesService salesService,
            MockProductionService productionService,
            MockInventoryService inventoryService,
            MockEmployeeService employeeService,
            MockPayrollService payrollService,
            MockLogisticsService logisticsService,
            MockAnalyticsService analytics,
            ISalesService dbSalesService)
        {
            _dashboardService = dashboardService;
            _salesService = salesService;
            _productionService = productionService;
            _inventoryService = inventoryService;
            _employeeService = employeeService;
            _payrollService = payrollService;
            _logisticsService = logisticsService;
            _analytics = analytics;
            _dbSalesService = dbSalesService;
        }

        /// <summary>The eight-stage lifecycle shown on the executive quick-overview.</summary>
        public IReadOnlyList<LifecycleStage> Stages { get; } = new[]
        {
            new LifecycleStage("Orders Placed", "fa-solid fa-receipt", "info", LifecycleMetric.OrdersPlaced),
            new LifecycleStage("Produced", "fa-solid fa-bread-slice", "primary", LifecycleMetric.Produced),
            new LifecycleStage("Loaded", "fa-solid fa-truck-ramp-box", "secondary", LifecycleMetric.Loaded),
            new LifecycleStage("Factory Defects", "fa-solid fa-ban", "warning", LifecycleMetric.FactoryDefects),
            new LifecycleStage("Sold", "fa-solid fa-basket-shopping", "success", LifecycleMetric.Sold),
            new LifecycleStage("Closing Stock", "fa-solid fa-warehouse", "accent", LifecycleMetric.Returned),
            new LifecycleStage("Damaged", "fa-solid fa-trash-can", "error", LifecycleMetric.Damaged),
            new LifecycleStage("Returns", "fa-solid fa-rotate-left", "neutral", LifecycleMetric.CustomerReturns)
        };

        /// <summary>Cumulative Today / This Week / This Month / This Year KPI cards.</summary>
        public ExecutiveOverviewDto Overview { get; set; } = new();

        public sealed record LifecycleStage(string Label, string IconClass, string Tone, LifecycleMetric Metric);

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
        public int RepCompletedTickets => RepTodayOrders.Count(o => o.Status == OrderStatus.Dispatched);

        // Vendor / Supermarket Client
        public Customer? VendorAccount { get; set; }
        public List<Order> VendorOrders { get; set; } = new();
        public List<Order> DailyStandingPreOrders { get; set; } = new();
        public int DailyPreOrderUnits => DailyStandingPreOrders.Sum(o => o.Items.Sum(i => i.Quantity));
        public decimal DailyPreOrderValue => DailyStandingPreOrders.Sum(o => o.TotalAmount);

        // HR & Payroll
        public List<Employee> Employees { get; set; } = new();
        public List<SalaryAdvance> PendingAdvances { get; set; } = new();
        public List<PayrollRecord> RecentPayroll { get; set; } = new();

        // Logistics
        public List<Vehicle> Fleet { get; set; } = new();
        public List<FuelLog> RecentFuelLogs { get; set; } = new();

        /// <summary>
        /// Deep link into /reports with the card's own context pre-applied, so an
        /// executive click lands on a report that is already scoped correctly.
        /// </summary>
        public static string ReportsUrl(string timeframe, string? category = null, int? repId = null)
        {
            var values = new List<string>
            {
                "module=Operational",
                "timeframe=" + Uri.EscapeDataString(timeframe),
                "view=table"
            };

            if (category != null) values.Add("category=" + Uri.EscapeDataString(category));
            if (repId.HasValue) values.Add("rep_id=" + repId.Value);

            return "/Reports?" + string.Join("&", values);
        }

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
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

            // Load Sales Rep Data. Sales reps must see their own database orders and their
            // assigned HR employee terminal, not the shared mock dashboard data.
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (ActiveRole == "SalesRep" && currentUserId.HasValue)
            {
                var currentSalesRep = await _dbSalesService.GetSalesRepByUserIdAsync(currentUserId.Value, cancellationToken);
                var terminals = await _dbSalesService.GetTerminalsAsync(cancellationToken: cancellationToken);
                var orders = await _dbSalesService.GetOrdersAsync(cancellationToken);

                if (currentSalesRep != null)
                {
                    AssignedTerminal = terminals.FirstOrDefault(t => t.AssignedEmployeeId == currentSalesRep.EmployeeId)
                        ?? terminals.FirstOrDefault(t => t.TerminalCode == currentSalesRep.PosTerminalCode);
                    RepTodayOrders = orders
                        .Where(o => o.SalesRepId == currentSalesRep.Id
                            && o.OrderDate.Date == DateTime.UtcNow.Date
                            && o.Status != OrderStatus.Cancelled)
                        .ToList();
                    UnprintedOrders = orders
                        .Where(o => o.SalesRepId == currentSalesRep.Id && !o.IsPrintedForLoading)
                        .ToList();
                }
            }
            else
            {
                var terminals = _salesService.GetAllTerminals();
                AssignedTerminal = terminals.FirstOrDefault(t => t.TerminalCode == "POS-01") ?? terminals.FirstOrDefault();
                UnprintedOrders = _salesService.GetAllOrders().Where(o => !o.IsPrintedForLoading).ToList();
                RepTodayOrders = _salesService.GetAllOrders().Where(o => o.OrderDate.Date == DateTime.UtcNow.Date).ToList();
            }

            // Load Vendor Data from the database and filter pre-orders to the signed-in customer.
            if (ActiveRole == "Customer" && currentUserId.HasValue)
            {
                VendorAccount = await _dbSalesService.GetCustomerByUserIdAsync(currentUserId.Value, cancellationToken);
            }
            else
            {
                var customers = _salesService.GetAllCustomers();
                VendorAccount = customers.FirstOrDefault(c => c.Category == CustomerCategory.Supermarket) ?? customers.FirstOrDefault();
            }

            if (VendorAccount != null)
            {
                var dbOrders = await _dbSalesService.GetOrdersAsync(cancellationToken);
                VendorOrders = dbOrders.Where(o => o.CustomerId == VendorAccount.Id).ToList();
            }

            if (ActiveRole == "Customer")
            {
                // A customer must only ever see their own pre-orders. If the login is not
                // linked to a customer account, show nothing rather than every order.
                if (VendorAccount != null)
                {
                    DailyStandingPreOrders = (await _dbSalesService.GetPreOrdersAsync(VendorAccount.Id, cancellationToken))
                        .Where(o => o.Status != OrderStatus.Cancelled)
                        .Take(5)
                        .ToList();
                }
            }
            else
            {
                DailyStandingPreOrders = (await _dbSalesService.GetPreOrdersAsync(null, cancellationToken))
                    .Where(o => o.Status != OrderStatus.Cancelled)
                    .Take(5)
                    .ToList();
            }

            // Load HR & Payroll Data
            Employees = _employeeService.GetAll();
            PendingAdvances = _payrollService.GetAllAdvances().Where(a => a.Status == "Active" && a.RemainingBalance > 0).ToList();
            RecentPayroll = _payrollService.GetAllPayrollRecords();

            // Load Logistics Data
            Fleet = _logisticsService.GetAllVehicles();
            RecentFuelLogs = _logisticsService.GetAllFuelLogs().Take(5).ToList();

            // Executive quick-overview. Only the SuperAdmin control centre needs it;
            // every other role keeps its existing operational layout.
            if (ActiveRole == "SuperAdmin")
            {
                Overview = _analytics.GetDashboardOverview();
            }
        }
    }
}
