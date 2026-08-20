using Manufacture.Models.Entities;
using Manufacture.Models.DTOs;

namespace Manufacture.Services
{
    public class MockSalesService
    {
        private readonly List<Customer> _customers = new();
        private readonly List<PosTerminal> _terminals = new();
        private readonly List<Order> _orders = new();

        public MockSalesService()
        {
            SeedInitialData();
        }

        private void SeedInitialData()
        {
            // Seed Customers & Vendors
            _customers.AddRange(new[]
            {
                new Customer { Id = 1, Name = "General Walk-in Counter", MemberCode = "WLK-001", Category = CustomerCategory.WalkIn, PhoneNumber = "N/A", Address = "Bakery Front Desk", OutstandingBalance = 0 },
                new Customer { Id = 2, Name = "Goodness Supermarket Ikeja", MemberCode = "SUP-101", Category = CustomerCategory.Supermarket, PhoneNumber = "+2348033221100", Address = "12 Oba Akran Way, Ikeja", OutstandingBalance = 45000 },
                new Customer { Id = 3, Name = "Grand Square Supermarket VI", MemberCode = "SUP-102", Category = CustomerCategory.Supermarket, PhoneNumber = "+2348055443322", Address = "Victoria Island, Lagos", OutstandingBalance = 0 },
                new Customer { Id = 4, Name = "Alhaja Basirat Bread Depot (Vendor)", MemberCode = "VND-201", Category = CustomerCategory.ExternalVendor, PhoneNumber = "+2348099887766", Address = "Oshodi Market Shop 44", OutstandingBalance = 120000 },
                new Customer { Id = 5, Name = "Madam Ngozi Wholesalers", MemberCode = "VND-202", Category = CustomerCategory.ExternalVendor, PhoneNumber = "+2348077665544", Address = "Agege Main Depot", OutstandingBalance = 15000 }
            });

            // Seed POS Terminals
            _terminals.AddRange(new[]
            {
                new PosTerminal
                {
                    Id = 1,
                    TerminalCode = "POS-01",
                    TerminalName = "Front Retail Register 1",
                    AssignedUserId = 4,
                    AssignedUserName = "Babatunde Alabi",
                    CashAccountName = "Bakery Till #1 (Main Cash)",
                    TransferAccountName = "FirstBank PLC - 3021948291 (Bakery Ops)",
                    PosBankAccountName = "Moniepoint POS #01 - 9028129",
                    IsActive = true
                },
                new PosTerminal
                {
                    Id = 2,
                    TerminalCode = "POS-02",
                    TerminalName = "Wholesale & Dispatch Register",
                    AssignedUserId = null,
                    AssignedUserName = "Unassigned",
                    CashAccountName = "Dispatch Till #2",
                    TransferAccountName = "Zenith Bank PLC - 1012938475 (Wholesale)",
                    PosBankAccountName = "OPay Merchant POS - 8102938",
                    IsActive = true
                },
                new PosTerminal
                {
                    Id = 3,
                    TerminalCode = "POS-03",
                    TerminalName = "Van 1 Mobile Sales POS",
                    AssignedUserId = null,
                    AssignedUserName = "Unassigned",
                    CashAccountName = "Driver Float Cash",
                    TransferAccountName = "FirstBank PLC - 3021948291 (Bakery Ops)",
                    PosBankAccountName = "PalmPay POS #03 - 7012948",
                    IsActive = true
                }
            });

            // Seed Orders
            _orders.AddRange(new[]
            {
                new Order
                {
                    Id = 1,
                    OrderNumber = "ORD-20260819-001",
                    Type = OrderType.ImmediateSale,
                    CustomerId = 1,
                    CustomerName = "General Walk-in Counter",
                    CustomerCategory = CustomerCategory.WalkIn,
                    SalesRepUserId = 4,
                    SalesRepName = "Babatunde Alabi",
                    PosTerminalCode = "POS-01",
                    PaymentMethod = PaymentMethod.Cash,
                    Status = OrderStatus.Dispatched,
                    OrderDate = DateTime.UtcNow.AddHours(-3),
                    TargetDeliveryDate = DateTime.UtcNow,
                    Discount = 0,
                    AmountPaid = 14000,
                    IsPrintedForLoading = true,
                    PrintedAt = DateTime.UtcNow.AddHours(-3),
                    VerifiedByLoaderName = "Kelechi Nnamdi",
                    LoadedAt = DateTime.UtcNow.AddHours(-2),
                    Items = new List<OrderItem>
                    {
                        new() { Id = 1, OrderId = 1, RecipeId = 1, ProductName = "Jumbo Family Bread", Quantity = 10, UnitPrice = 1400 }
                    }
                },
                new Order
                {
                    Id = 2,
                    OrderNumber = "ORD-20260819-002",
                    Type = OrderType.ImmediateSale,
                    CustomerId = 4,
                    CustomerName = "Alhaja Basirat Bread Depot (Vendor)",
                    CustomerCategory = CustomerCategory.ExternalVendor,
                    SalesRepUserId = 4,
                    SalesRepName = "Babatunde Alabi",
                    PosTerminalCode = "POS-01",
                    PaymentMethod = PaymentMethod.BankTransfer,
                    Status = OrderStatus.Dispatched,
                    OrderDate = DateTime.UtcNow.AddHours(-2),
                    TargetDeliveryDate = DateTime.UtcNow,
                    Discount = 5000,
                    AmountPaid = 135000,
                    IsPrintedForLoading = true,
                    PrintedAt = DateTime.UtcNow.AddHours(-2),
                    VerifiedByLoaderName = "Kelechi Nnamdi",
                    LoadedAt = DateTime.UtcNow.AddHours(-1),
                    Items = new List<OrderItem>
                    {
                        new() { Id = 2, OrderId = 2, RecipeId = 1, ProductName = "Jumbo Family Bread", Quantity = 100, UnitPrice = 1400 }
                    }
                },
                new Order
                {
                    Id = 3,
                    OrderNumber = "ORD-20260819-003",
                    Type = OrderType.PreOrder,
                    CustomerId = 2,
                    CustomerName = "Goodness Supermarket Ikeja",
                    CustomerCategory = CustomerCategory.Supermarket,
                    SalesRepUserId = 4,
                    SalesRepName = "Babatunde Alabi",
                    PosTerminalCode = "POS-01",
                    PaymentMethod = PaymentMethod.CreditOwing,
                    Status = OrderStatus.InProduction,
                    OrderDate = DateTime.UtcNow.AddHours(-1),
                    TargetDeliveryDate = DateTime.UtcNow.AddDays(1),
                    Discount = 0,
                    AmountPaid = 0,
                    IsPrintedForLoading = false,
                    Items = new List<OrderItem>
                    {
                        new() { Id = 3, OrderId = 3, RecipeId = 1, ProductName = "Jumbo Family Bread", Quantity = 80, UnitPrice = 1400 },
                        new() { Id = 4, OrderId = 3, RecipeId = 2, ProductName = "Medium Loaf Bread", Quantity = 50, UnitPrice = 900 }
                    }
                },
                new Order
                {
                    Id = 4,
                    OrderNumber = "ORD-20260819-004",
                    Type = OrderType.PreOrder,
                    CustomerId = 5,
                    CustomerName = "Madam Ngozi Wholesalers",
                    CustomerCategory = CustomerCategory.ExternalVendor,
                    SalesRepUserId = 4,
                    SalesRepName = "Babatunde Alabi",
                    PosTerminalCode = "POS-01",
                    PaymentMethod = PaymentMethod.PosBank,
                    Status = OrderStatus.ReadyToLoad,
                    OrderDate = DateTime.UtcNow.AddHours(-1),
                    TargetDeliveryDate = DateTime.UtcNow,
                    Discount = 2000,
                    AmountPaid = 97000,
                    IsPrintedForLoading = true,
                    PrintedAt = DateTime.UtcNow.AddMinutes(-30),
                    Items = new List<OrderItem>
                    {
                        new() { Id = 5, OrderId = 4, RecipeId = 2, ProductName = "Medium Loaf Bread", Quantity = 110, UnitPrice = 900 }
                    }
                }
            });
        }

        // Customers
        public List<Customer> GetAllCustomers() => _customers.Where(c => c.IsActive).OrderBy(c => c.Name).ToList();
        public Customer? GetCustomerById(int id) => _customers.FirstOrDefault(c => c.Id == id);
        public Customer CreateCustomer(CustomerDto dto)
        {
            var cat = Enum.TryParse<CustomerCategory>(dto.Category, true, out var c) ? c : CustomerCategory.WalkIn;
            var customer = new Customer
            {
                Id = _customers.Any() ? _customers.Max(c => c.Id) + 1 : 1,
                Name = dto.Name,
                MemberCode = string.IsNullOrWhiteSpace(dto.MemberCode) ? $"CUST-{_customers.Count + 101}" : dto.MemberCode,
                Category = cat,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                OutstandingBalance = dto.OutstandingBalance,
                IsActive = true
            };
            _customers.Add(customer);
            return customer;
        }

        // Terminals
        public List<PosTerminal> GetAllTerminals() => _terminals.ToList();
        public PosTerminal? GetTerminalById(int id) => _terminals.FirstOrDefault(t => t.Id == id);
        public bool AssignTerminalUser(int terminalId, int? userId, string? userName)
        {
            var terminal = GetTerminalById(terminalId);
            if (terminal == null) return false;
            terminal.AssignedUserId = userId;
            terminal.AssignedUserName = userName ?? "Unassigned";
            return true;
        }

        // Orders
        public List<Order> GetAllOrders() => _orders.OrderByDescending(o => o.Id).ToList();
        public List<Order> GetPreOrders() => _orders.Where(o => o.Type == OrderType.PreOrder).OrderByDescending(o => o.Id).ToList();
        public Order? GetOrderById(int id) => _orders.FirstOrDefault(o => o.Id == id);
        public Order? GetOrderByNumber(string orderNum) => _orders.FirstOrDefault(o => o.OrderNumber.Equals(orderNum, StringComparison.OrdinalIgnoreCase));

        public Order CreateOrder(CreateOrderDto dto, int? currentUserId, string? currentUserName)
        {
            var customer = GetCustomerById(dto.CustomerId);
            var orderType = Enum.TryParse<OrderType>(dto.OrderType, true, out var ot) ? ot : OrderType.ImmediateSale;
            var payMethod = Enum.TryParse<PaymentMethod>(dto.PaymentMethod, true, out var pm) ? pm : PaymentMethod.Cash;
            var custCategory = Enum.TryParse<CustomerCategory>(dto.CustomerCategory, true, out var cc) ? cc : (customer?.Category ?? CustomerCategory.WalkIn);

            var order = new Order
            {
                Id = _orders.Any() ? _orders.Max(o => o.Id) + 1 : 1,
                OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{_orders.Count + 1:000}",
                Type = orderType,
                CustomerId = dto.CustomerId,
                CustomerName = customer?.Name ?? dto.CustomerName,
                CustomerCategory = custCategory,
                SalesRepUserId = currentUserId,
                SalesRepName = currentUserName ?? "Sales Rep",
                PosTerminalCode = dto.PosTerminalCode,
                PaymentMethod = payMethod,
                Status = orderType == OrderType.PreOrder ? OrderStatus.InProduction : OrderStatus.ReadyToLoad,
                OrderDate = DateTime.UtcNow,
                TargetDeliveryDate = dto.TargetDeliveryDate,
                Discount = dto.Discount,
                AmountPaid = dto.AmountPaid,
                Notes = dto.Notes,
                Items = dto.Items.Select((item, idx) => new OrderItem
                {
                    Id = idx + 1,
                    RecipeId = item.RecipeId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList()
            };

            _orders.Add(order);

            // Update customer balance if unpaid / credit
            if (customer != null && order.BalanceDue > 0)
            {
                customer.OutstandingBalance += order.BalanceDue;
            }

            return order;
        }

        public bool MarkPrintedForLoading(int orderId)
        {
            var order = GetOrderById(orderId);
            if (order == null) return false;
            order.IsPrintedForLoading = true;
            order.PrintedAt = DateTime.UtcNow;
            if (order.Status == OrderStatus.InProduction || order.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.ReadyToLoad;
            }
            return true;
        }

        public bool VerifyAndDispatchByLoader(int orderId, string loaderName)
        {
            var order = GetOrderById(orderId);
            if (order == null) return false;
            // Rule: cannot dispatch without printing
            if (!order.IsPrintedForLoading) return false;

            order.VerifiedByLoaderName = loaderName;
            order.LoadedAt = DateTime.UtcNow;
            order.Status = OrderStatus.Dispatched;
            return true;
        }
    }
}
