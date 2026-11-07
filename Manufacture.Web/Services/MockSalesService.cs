using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;

namespace Manufacture.Services
{
    public class MockSalesService
    {
        private readonly List<Customer> _customers = new();
        private readonly List<PosTerminal> _terminals = new();
        private readonly List<Order> _orders = new();
        private readonly List<SalesRepProfile> _reps = new();
        private readonly List<DailyRepRouteSheet> _routeSheets = new();
        private readonly List<SupermarketAccount> _supermarketAccounts = new();

        private readonly MockProductionService? _productionService;
        private readonly MockInventoryService? _inventoryService;

        public MockSalesService(
            MockUserService userService,
            MockLogisticsService logisticsService,
            MockProductionService? productionService = null,
            MockInventoryService? inventoryService = null)
        {
            _productionService = productionService;
            _inventoryService = inventoryService;

            SeedInitialData();
            SeedReps(userService, logisticsService);
            SeedSupermarketAccounts();
            SeedRouteSheets();
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
                },
                new Order
                {
                    Id = 5,
                    OrderNumber = "ORD-20260819-005",
                    Type = OrderType.PreOrder,
                    CustomerId = 3,
                    CustomerName = "Grand Square Supermarket VI",
                    CustomerCategory = CustomerCategory.Supermarket,
                    SalesRepUserId = 1,
                    SalesRepName = "Babatunde Alabi",
                    PosTerminalCode = "POS-02",
                    PaymentMethod = PaymentMethod.BankTransfer,
                    Status = OrderStatus.InProduction,
                    OrderDate = DateTime.UtcNow.AddMinutes(-45),
                    TargetDeliveryDate = DateTime.UtcNow,
                    Discount = 0,
                    AmountPaid = 0,
                    Items = new List<OrderItem>
                    {
                        new() { Id = 6, OrderId = 5, RecipeId = 6, ProductName = "Purified Water 18.9L", Quantity = 250, UnitPrice = 250 }
                    }
                },
                new Order
                {
                    Id = 6,
                    OrderNumber = "ORD-20260819-006",
                    Type = OrderType.PreOrder,
                    CustomerId = 2,
                    CustomerName = "Goodness Supermarket Ikeja",
                    CustomerCategory = CustomerCategory.Supermarket,
                    SalesRepUserId = 2,
                    SalesRepName = "Chiamaka Nwosu",
                    PosTerminalCode = "POS-03",
                    PaymentMethod = PaymentMethod.PosBank,
                    Status = OrderStatus.InProduction,
                    OrderDate = DateTime.UtcNow.AddMinutes(-20),
                    TargetDeliveryDate = DateTime.UtcNow,
                    Discount = 0,
                    AmountPaid = 0,
                    Items = new List<OrderItem>
                    {
                        new() { Id = 7, OrderId = 6, RecipeId = 7, ProductName = "Popcorn 40g Bag", Quantity = 120, UnitPrice = 200 }
                    }
                }
            });
        }

        /// <summary>
        /// The field roster. Every rep is bound to a real login account, a real POS
        /// terminal and a real vehicle, so a rep named on the dashboard is the same
        /// person who signs in, appears in HR, and can be found on an order.
        ///
        /// Terminal and vehicle are assigned here rather than invented downstream:
        ///   * POS-01 is the front retail register and is owned outright by its user.
        ///   * POS-02 is the wholesale/dispatch register, shared by the mainland reps.
        ///   * POS-03 is a van's mobile POS, so the two reps on a van share it.
        /// </summary>
        private void SeedReps(MockUserService userService, MockLogisticsService logisticsService)
        {
            // Resolve by registration number so a renamed or renumbered vehicle in the
            // fleet master cannot silently orphan a rep.
            var vehicle = (string reg) => logisticsService.GetAllVehicles()
                .FirstOrDefault(v => string.Equals(v.RegistrationNumber, reg, StringComparison.OrdinalIgnoreCase));

            var repUser = (string name) => userService.GetAll(includeInactive: false)
                .FirstOrDefault(u => u.Role == nameof(UserRole.SalesRep)
                                  && string.Equals(u.Name, name, StringComparison.OrdinalIgnoreCase));

            void Add(int id, string name, string phone, string route, string terminal, string reg, string focus)
            {
                var account = repUser(name);
                _reps.Add(new SalesRepProfile
                {
                    Id = id,
                    UserId = account?.Id,
                    FullName = account?.Name ?? name,
                    PhoneNumber = account?.PhoneNumber ?? phone,
                    RouteName = route,
                    PosTerminalCode = terminal,
                    VehicleRegistration = vehicle(reg)?.RegistrationNumber ?? reg,
                    ProductFocus = focus,
                    IsActive = true
                });
            }

            Add(1, "Babatunde Alabi", "+2348044445555", "Ikeja & Ojota", "POS-01", "KJA-892-XA", "Bread & Water");
            Add(2, "Chiamaka Nwosu", "+2348055556677", "Lagos Island & Victoria Is.", "POS-03", "KJA-892-XA", "Bread & Popcorn");
            Add(3, "Ibrahim Sanni", "+2348066667788", "Yaba & Surulere", "POS-02", "LSR-441-YB", "Water & Bread");
            Add(4, "Blessing Etim", "+2348077778899", "Maryland & Ikeja", "POS-02", "LSR-441-YB", "Popcorn & Water");
            Add(5, "Segun Adewale", "+2348088889900", "Agege & Ilupeju", "POS-02", "EKY-319-ZC", "Bread");
            Add(6, "Halima Yusuf", "+2348099990011", "Oshodi & Islington", "POS-03", "EKY-319-ZC", "Water & Popcorn");
        }

        // Sales reps
        public List<SalesRepProfile> GetAllReps() => _reps.Where(r => r.IsActive).ToList();

        public SalesRepProfile? GetRepById(int id) => _reps.FirstOrDefault(r => r.Id == id);

        public SalesRepProfile? GetRepByUserId(int userId) => _reps.FirstOrDefault(r => r.UserId == userId);

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

        public PosTerminal CreateTerminal(PosTerminal terminal)
        {
            terminal.Id = _terminals.Any() ? _terminals.Max(t => t.Id) + 1 : 1;
            if (string.IsNullOrWhiteSpace(terminal.TerminalCode))
            {
                terminal.TerminalCode = $"POS-{terminal.Id:00}";
            }
            if (string.IsNullOrWhiteSpace(terminal.TerminalName))
            {
                terminal.TerminalName = $"Checkout Register #{terminal.Id}";
            }
            if (string.IsNullOrWhiteSpace(terminal.CashAccountName))
            {
                terminal.CashAccountName = $"Cash Drawer Till #{terminal.Id}";
            }
            if (string.IsNullOrWhiteSpace(terminal.TransferAccountName))
            {
                terminal.TransferAccountName = "FirstBank PLC - 3021948291";
            }
            if (string.IsNullOrWhiteSpace(terminal.PosBankAccountName))
            {
                terminal.PosBankAccountName = $"Moniepoint POS #{terminal.Id:00} - 9028129";
            }
            if (!terminal.AssignedUserId.HasValue)
            {
                terminal.AssignedUserName = "Unassigned";
            }

            _terminals.Add(terminal);
            return terminal;
        }

        public bool UpdateTerminal(PosTerminal model)
        {
            var existing = GetTerminalById(model.Id);
            if (existing == null) return false;

            if (!string.IsNullOrWhiteSpace(model.TerminalCode))
                existing.TerminalCode = model.TerminalCode.Trim();

            if (!string.IsNullOrWhiteSpace(model.TerminalName))
                existing.TerminalName = model.TerminalName.Trim();

            if (!string.IsNullOrWhiteSpace(model.CashAccountName))
                existing.CashAccountName = model.CashAccountName.Trim();

            if (!string.IsNullOrWhiteSpace(model.TransferAccountName))
                existing.TransferAccountName = model.TransferAccountName.Trim();

            if (!string.IsNullOrWhiteSpace(model.PosBankAccountName))
                existing.PosBankAccountName = model.PosBankAccountName.Trim();

            existing.IsActive = model.IsActive;
            existing.AssignedUserId = model.AssignedUserId;
            existing.AssignedUserName = string.IsNullOrWhiteSpace(model.AssignedUserName) ? "Unassigned" : model.AssignedUserName.Trim();

            return true;
        }

        public bool ToggleTerminalStatus(int id)
        {
            var terminal = GetTerminalById(id);
            if (terminal == null) return false;
            terminal.IsActive = !terminal.IsActive;
            return true;
        }

        // Orders
        public List<Order> GetAllOrders() => _orders.OrderByDescending(o => o.Id).ToList();
        public List<Order> GetPreOrders() => _orders.Where(o => o.Type == OrderType.PreOrder).OrderByDescending(o => o.Id).ToList();
        public Order? GetOrderById(int id) => _orders.FirstOrDefault(o => o.Id == id);
        public Order? GetOrderByNumber(string orderNum) => _orders.FirstOrDefault(o => o.OrderNumber.Equals(orderNum, StringComparison.OrdinalIgnoreCase));

        public Order CreateOrder(CreateOrderDto dto, int? currentUserId, string? currentUserName)
        {
            Customer? customer = null;
            if (dto.CustomerId > 0)
            {
                customer = GetCustomerById(dto.CustomerId);
            }
            if (customer == null && !string.IsNullOrWhiteSpace(dto.CustomerName))
            {
                customer = _customers.FirstOrDefault(c => c.Name.Equals(dto.CustomerName, StringComparison.OrdinalIgnoreCase));
            }

            var resolvedCustomerName = !string.IsNullOrWhiteSpace(dto.CustomerName)
                ? dto.CustomerName.Trim()
                : (customer?.Name ?? "Walk-In Counter Customer");

            var orderType = Enum.TryParse<OrderType>(dto.OrderType, true, out var ot) ? ot : OrderType.ImmediateSale;
            var payMethod = Enum.TryParse<PaymentMethod>(dto.PaymentMethod, true, out var pm) ? pm : PaymentMethod.Cash;
            var custCategory = customer?.Category ?? (Enum.TryParse<CustomerCategory>(dto.CustomerCategory, true, out var cc) ? cc : CustomerCategory.WalkIn);

            var order = new Order
            {
                Id = _orders.Any() ? _orders.Max(o => o.Id) + 1 : 1,
                OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{_orders.Count + 1:000}",
                Type = orderType,
                CustomerId = customer?.Id ?? dto.CustomerId,
                CustomerName = resolvedCustomerName,
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

        /// <summary>
        /// Aggregates demand from Sales Rep orders placed (active pre-orders & pending dispatches)
        /// for a specific recipe to drive production batch planning.
        /// </summary>
        public int GetDemandForRecipe(int recipeId)
        {
            return _orders
                .Where(o => o.Status != OrderStatus.Dispatched && o.Status != OrderStatus.Cancelled)
                .SelectMany(o => o.Items)
                .Where(item => item.RecipeId == recipeId)
                .Sum(item => item.Quantity);
        }

        /// <summary>
        /// Returns open customer orders / pre-orders that need production or loading.
        /// If a recipeId is supplied, filters down to orders that contain this recipe item.
        /// </summary>
        public List<Order> GetOpenOrdersForRecipe(int? recipeId = null)
        {
            var query = _orders.Where(o => o.Status != OrderStatus.Dispatched && o.Status != OrderStatus.Cancelled);
            if (recipeId.HasValue && recipeId.Value > 0)
            {
                query = query.Where(o => o.Items.Any(i => i.RecipeId == recipeId.Value));
            }
            return query.OrderByDescending(o => o.Id).ToList();
        }

        // =========================================================================
        // Supermarket POS Accounts & Custom Agreed Pricing
        // =========================================================================

        private void SeedSupermarketAccounts()
        {
            var goodness = _customers.FirstOrDefault(c => c.Id == 2) ?? new Customer
            {
                Id = 2, Name = "Goodness Supermarket Ikeja", MemberCode = "SUP-101",
                Category = CustomerCategory.Supermarket, PhoneNumber = "+2348033221100",
                Address = "12 Oba Akran Way, Ikeja", OutstandingBalance = 45000
            };
            var grandSquare = _customers.FirstOrDefault(c => c.Id == 3) ?? new Customer
            {
                Id = 3, Name = "Grand Square Supermarket VI", MemberCode = "SUP-102",
                Category = CustomerCategory.Supermarket, PhoneNumber = "+2348055443322",
                Address = "Victoria Island, Lagos", OutstandingBalance = 0
            };

            _supermarketAccounts.AddRange(new[]
            {
                new SupermarketAccount
                {
                    Id = 1,
                    CustomerId = 2,
                    AccountName = goodness.Name,
                    ContactPerson = "Mrs. Ngozi Okafor",
                    PhoneNumber = goodness.PhoneNumber,
                    Address = goodness.Address,
                    AssignedRepId = 1,
                    AssignedRepName = "Babatunde Alabi",
                    CreditLimit = 200000m,
                    PaymentTerms = "Net 15 Days",
                    CurrentOutstandingCredit = goodness.OutstandingBalance,
                    CustomAgreedPrices = new Dictionary<int, decimal>
                    {
                        { 1, 1320m }, // Jumbo Bread discounted from ₦1400 to ₦1320
                        { 2, 850m },  // Medium Loaf discounted from ₦900 to ₦850
                        { 6, 230m }   // Water 18.9L discounted from ₦250 to ₦230
                    }
                },
                new SupermarketAccount
                {
                    Id = 2,
                    CustomerId = 3,
                    AccountName = grandSquare.Name,
                    ContactPerson = "Mr. Femi Adeyemi",
                    PhoneNumber = grandSquare.PhoneNumber,
                    Address = grandSquare.Address,
                    AssignedRepId = 2,
                    AssignedRepName = "Chiamaka Nwosu",
                    CreditLimit = 300000m,
                    PaymentTerms = "Net 30 Days",
                    CurrentOutstandingCredit = grandSquare.OutstandingBalance,
                    CustomAgreedPrices = new Dictionary<int, decimal>
                    {
                        { 1, 1300m }, // Jumbo Bread ₦1300
                        { 3, 1020m }, // Special 61 ₦1020
                        { 7, 180m }   // Popcorn 40g ₦180
                    }
                },
                new SupermarketAccount
                {
                    Id = 3,
                    CustomerId = 4,
                    AccountName = "Alhaja Basirat Bread Depot (Vendor)",
                    ContactPerson = "Alhaja Basirat",
                    PhoneNumber = "+2348099887766",
                    Address = "Oshodi Market Shop 44",
                    AssignedRepId = 6,
                    AssignedRepName = "Halima Yusuf",
                    CreditLimit = 150000m,
                    PaymentTerms = "Net 7 Days",
                    CurrentOutstandingCredit = 120000m,
                    CustomAgreedPrices = new Dictionary<int, decimal>
                    {
                        { 1, 1250m },
                        { 2, 800m },
                        { 4, 760m }
                    }
                },
                new SupermarketAccount
                {
                    Id = 4,
                    CustomerId = 5,
                    AccountName = "Madam Ngozi Wholesalers",
                    ContactPerson = "Madam Ngozi Eze",
                    PhoneNumber = "+2348077665544",
                    Address = "Agege Main Depot",
                    AssignedRepId = 5,
                    AssignedRepName = "Segun Adewale",
                    CreditLimit = 100000m,
                    PaymentTerms = "Net 14 Days",
                    CurrentOutstandingCredit = 15000m,
                    CustomAgreedPrices = new Dictionary<int, decimal>
                    {
                        { 2, 820m },
                        { 4, 770m },
                        { 5, 710m }
                    }
                }
            });
        }

        public List<SupermarketAccount> GetAllSupermarketAccounts() => _supermarketAccounts.OrderBy(a => a.AccountName).ToList();

        public SupermarketAccount? GetSupermarketAccountById(int id) =>
            _supermarketAccounts.FirstOrDefault(a => a.Id == id || a.CustomerId == id);

        public bool SetCustomAgreedPrice(int accountId, int productId, decimal customPrice)
        {
            var account = GetSupermarketAccountById(accountId);
            if (account == null) return false;
            account.CustomAgreedPrices[productId] = Math.Max(0m, customPrice);
            return true;
        }

        public bool SetBatchCustomAgreedPrices(int accountId, Dictionary<int, decimal> prices)
        {
            var account = GetSupermarketAccountById(accountId);
            if (account == null) return false;
            foreach (var kvp in prices)
            {
                if (kvp.Value > 0)
                {
                    account.CustomAgreedPrices[kvp.Key] = kvp.Value;
                }
                else
                {
                    account.CustomAgreedPrices.Remove(kvp.Key);
                }
            }
            return true;
        }

        public bool RecordSupermarketPayment(int accountId, decimal amountPaid, string paymentMethod, string reference)
        {
            var account = GetSupermarketAccountById(accountId);
            if (account == null || amountPaid <= 0) return false;
            account.CurrentOutstandingCredit = Math.Max(0m, account.CurrentOutstandingCredit - amountPaid);
            var customer = GetCustomerById(account.CustomerId);
            if (customer != null)
            {
                customer.OutstandingBalance = account.CurrentOutstandingCredit;
            }
            return true;
        }

        public bool SaveSupermarketAccount(SupermarketAccount model)
        {
            var existing = GetSupermarketAccountById(model.Id);
            if (existing != null)
            {
                existing.AccountName = model.AccountName.Trim();
                existing.ContactPerson = model.ContactPerson.Trim();
                existing.PhoneNumber = model.PhoneNumber.Trim();
                existing.Address = model.Address.Trim();
                existing.CreditLimit = Math.Max(0m, model.CreditLimit);
                existing.PaymentTerms = string.IsNullOrWhiteSpace(model.PaymentTerms) ? "Net 15 Days" : model.PaymentTerms.Trim();
                existing.IsActive = model.IsActive;
                existing.AssignedRepId = model.AssignedRepId;
                var rep = GetRepById(model.AssignedRepId);
                if (rep != null) existing.AssignedRepName = rep.FullName;

                var cust = GetCustomerById(existing.CustomerId);
                if (cust != null)
                {
                    cust.Name = existing.AccountName;
                    cust.PhoneNumber = existing.PhoneNumber;
                    cust.Address = existing.Address;
                    cust.IsActive = existing.IsActive;
                }
                return true;
            }
            else
            {
                int newCustId = _customers.Any() ? _customers.Max(c => c.Id) + 1 : 1;
                var newCust = new Customer
                {
                    Id = newCustId,
                    Name = model.AccountName.Trim(),
                    MemberCode = $"SUP-{newCustId + 100}",
                    Category = CustomerCategory.Supermarket,
                    PhoneNumber = model.PhoneNumber.Trim(),
                    Address = model.Address.Trim(),
                    OutstandingBalance = 0,
                    IsActive = model.IsActive
                };
                _customers.Add(newCust);

                int newId = _supermarketAccounts.Any() ? _supermarketAccounts.Max(a => a.Id) + 1 : 1;
                model.Id = newId;
                model.CustomerId = newCustId;
                model.PaymentTerms = string.IsNullOrWhiteSpace(model.PaymentTerms) ? "Net 15 Days" : model.PaymentTerms.Trim();
                var rep = GetRepById(model.AssignedRepId);
                if (rep != null) model.AssignedRepName = rep.FullName;
                _supermarketAccounts.Add(model);
                return true;
            }
        }

        public bool ToggleSupermarketStatus(int id)
        {
            var account = GetSupermarketAccountById(id);
            if (account == null) return false;
            account.IsActive = !account.IsActive;
            var cust = GetCustomerById(account.CustomerId);
            if (cust != null) cust.IsActive = account.IsActive;
            return true;
        }

        public bool FulfillSupermarketOrder(int accountId, int repId, List<OrderItemDto> items, string notes)
        {
            var account = GetSupermarketAccountById(accountId);
            if (account == null || items == null || items.Count == 0) return false;

            var rep = GetRepById(repId) ?? _reps.FirstOrDefault(r => r.Id == account.AssignedRepId) ?? _reps.First();
            var customer = GetCustomerById(account.CustomerId);

            // Calculate total based on custom agreed prices, fallback to item.UnitPrice
            decimal orderTotal = 0m;
            var processedItems = new List<OrderItem>();

            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                var price = account.CustomAgreedPrices.TryGetValue(it.RecipeId, out var custom) ? custom : it.UnitPrice;
                orderTotal += (it.Quantity * price);

                processedItems.Add(new OrderItem
                {
                    Id = i + 1,
                    RecipeId = it.RecipeId,
                    ProductName = it.ProductName,
                    Quantity = it.Quantity,
                    UnitPrice = price
                });

                // Deduct from finished inventory if inventory service is wired
                if (_inventoryService != null)
                {
                    // Finished goods stock tracking
                }
            }

            // Charge the supermarket account running credit balance
            account.CurrentOutstandingCredit += orderTotal;
            if (customer != null)
            {
                customer.OutstandingBalance = account.CurrentOutstandingCredit;
            }

            // Record as a fulfilled order
            var order = new Order
            {
                Id = _orders.Any() ? _orders.Max(o => o.Id) + 1 : 1,
                OrderNumber = $"SUP-{DateTime.UtcNow:yyyyMMdd}-{_orders.Count + 1:000}",
                Type = OrderType.ImmediateSale,
                CustomerId = account.CustomerId,
                CustomerName = account.AccountName,
                CustomerCategory = CustomerCategory.Supermarket,
                SalesRepUserId = rep.UserId,
                SalesRepName = rep.FullName,
                PosTerminalCode = rep.PosTerminalCode,
                PaymentMethod = PaymentMethod.CreditOwing,
                Status = OrderStatus.Dispatched,
                OrderDate = DateTime.UtcNow,
                TargetDeliveryDate = DateTime.UtcNow,
                Discount = 0,
                AmountPaid = 0,
                Notes = string.IsNullOrWhiteSpace(notes) ? "Delivered to supermarket on credit" : notes,
                Items = processedItems
            };
            _orders.Add(order);

            return true;
        }

        // =========================================================================
        // Sales Rep Daily Route Loading & Financial Reconciliation Sheets
        // =========================================================================

        public List<(int Id, string Code, string Name, ProductType Category, decimal Price)> GetCatalogSkus()
        {
            if (_productionService != null)
            {
                var recipes = _productionService.GetAllRecipes();
                if (recipes.Any())
                {
                    return recipes.Select(r => (r.Id, r.Code, r.Name, r.ProductType, r.SellingPrice)).ToList();
                }
            }

            // Canonical fallback master
            return new List<(int, string, string, ProductType, decimal)>
            {
                (1, "BRD-JMB",  "Jumbo Family Bread",       ProductType.Bread,   1400m),
                (2, "BRD-MED",  "Medium Loaf Bread",        ProductType.Bread,   900m),
                (3, "BRD-61",   "Special 61 Butter Bread",  ProductType.Bread,   1100m),
                (4, "BRD-RND",  "Round Family Loaf",        ProductType.Bread,   850m),
                (5, "BRD-6IN1", "6-in-1 Multigrain Loaf",   ProductType.Bread,   780m),
                (6, "WTR-189",  "Purified Water 18.9L",     ProductType.Water,   250m),
                (7, "POP-40",   "Popcorn 40g Bag",          ProductType.Popcorn, 200m)
            };
        }

        private void SeedRouteSheets()
        {
            var skus = GetCatalogSkus();
            var today = DateTime.UtcNow.Date;

            // Sheet 1: Babatunde Alabi - Loaded & out on route
            var rep1 = _reps.FirstOrDefault(r => r.Id == 1) ?? _reps.First();
            var sheet1 = new DailyRepRouteSheet
            {
                Id = 1,
                RouteSheetRef = $"RTE-{today:yyyyMMdd}-001",
                Date = today,
                RepId = rep1.Id,
                RepName = rep1.FullName,
                RepCode = "REP-001",
                VehicleRegNumber = rep1.VehicleRegistration,
                RouteName = rep1.RouteName,
                PosTerminalCode = rep1.PosTerminalCode,
                PreviousAccountBalance = 22000m,
                Status = RouteSheetStatus.LoadedOnRoute,
                SignedOffByDispatcher = "Kelechi Nnamdi (Dispatch Mgr)",
                SignedOffAt = today.AddHours(7).AddMinutes(30),
                Notes = "Morning wholesale & supermarket dispatch.",
                Items = skus.Select(sku =>
                {
                    int yest = sku.Id == 1 ? 12 : (sku.Id == 2 ? 8 : 0);
                    int loaded = sku.Id == 1 ? 88 : (sku.Id == 2 ? 92 : (sku.Id == 6 ? 40 : (sku.Id == 7 ? 50 : 0)));
                    return new RouteSKUItem
                    {
                        ProductId = sku.Id,
                        ProductCode = sku.Code,
                        ProductName = sku.Name,
                        Category = sku.Category,
                        UnitSellingPrice = sku.Price,
                        RepOrderRequestedQty = loaded + 10,
                        YesterdayClosingStock = yest,
                        TodayNewlyLoadedQty = loaded
                    };
                }).ToList()
            };

            // Sheet 2: Chiamaka Nwosu - Order placed, waiting for loading verification
            var rep2 = _reps.FirstOrDefault(r => r.Id == 2) ?? _reps.Skip(1).FirstOrDefault() ?? _reps.First();
            var sheet2 = new DailyRepRouteSheet
            {
                Id = 2,
                RouteSheetRef = $"RTE-{today:yyyyMMdd}-002",
                Date = today,
                RepId = rep2.Id,
                RepName = rep2.FullName,
                RepCode = "REP-002",
                VehicleRegNumber = rep2.VehicleRegistration,
                RouteName = rep2.RouteName,
                PosTerminalCode = rep2.PosTerminalCode,
                PreviousAccountBalance = 10000m,
                Status = RouteSheetStatus.Ordered,
                Notes = "Island route: awaiting vehicle bay release.",
                Items = skus.Select(sku =>
                {
                    int yest = sku.Id == 1 ? 10 : (sku.Id == 7 ? 5 : 0);
                    int req = sku.Id == 1 ? 120 : (sku.Id == 3 ? 60 : (sku.Id == 7 ? 80 : 0));
                    return new RouteSKUItem
                    {
                        ProductId = sku.Id,
                        ProductCode = sku.Code,
                        ProductName = sku.Name,
                        Category = sku.Category,
                        UnitSellingPrice = sku.Price,
                        RepOrderRequestedQty = req,
                        YesterdayClosingStock = yest,
                        TodayNewlyLoadedQty = 0 // Not loaded yet
                    };
                }).ToList()
            };

            // Sheet 3: Ibrahim Sanni - Reconciled & Closed
            var rep3 = _reps.FirstOrDefault(r => r.Id == 3) ?? _reps.Skip(2).FirstOrDefault() ?? _reps.First();
            var sheet3 = new DailyRepRouteSheet
            {
                Id = 3,
                RouteSheetRef = $"RTE-{today:yyyyMMdd}-003",
                Date = today,
                RepId = rep3.Id,
                RepName = rep3.FullName,
                RepCode = "REP-003",
                VehicleRegNumber = rep3.VehicleRegistration,
                RouteName = rep3.RouteName,
                PosTerminalCode = rep3.PosTerminalCode,
                PreviousAccountBalance = 15000m,
                CashCollected = 75000m,
                BankTransferCollected = 60000m,
                PosTerminalCollected = 14500m,
                Status = RouteSheetStatus.Reconciled,
                SignedOffByDispatcher = "Kelechi Nnamdi (Dispatch Mgr)",
                SignedOffAt = today.AddHours(7).AddMinutes(15),
                ReconciledBy = "Adekunle Johnson (SuperAdmin)",
                ReconciledAt = today.AddHours(14).AddMinutes(45),
                Notes = "Yaba & Surulere route: full reconciliation complete.",
                Items = skus.Select(sku =>
                {
                    int loaded = sku.Id == 1 ? 80 : (sku.Id == 6 ? 150 : 0);
                    int sold = sku.Id == 1 ? 76 : (sku.Id == 6 ? 142 : 0);
                    int ret = sku.Id == 1 ? 3 : (sku.Id == 6 ? 8 : 0);
                    int dmg = sku.Id == 1 ? 1 : 0;
                    return new RouteSKUItem
                    {
                        ProductId = sku.Id,
                        ProductCode = sku.Code,
                        ProductName = sku.Name,
                        Category = sku.Category,
                        UnitSellingPrice = sku.Price,
                        RepOrderRequestedQty = loaded,
                        YesterdayClosingStock = 0,
                        TodayNewlyLoadedQty = loaded,
                        QtySold = sold,
                        QtyReturned = ret,
                        QtyDamaged = dmg
                    };
                }).ToList()
            };

            _routeSheets.AddRange(new[] { sheet1, sheet2, sheet3 });
        }

        public (int ActiveInventory, bool IsBalanced) CalculateRouteInventory(RouteSKUItem item)
        {
            var active = item.YesterdayClosingStock + item.TodayNewlyLoadedQty;
            var isBalanced = (item.QtySold + item.QtyReturned + item.QtyDamaged) == active;
            return (active, isBalanced);
        }

        public List<DailyRepRouteSheet> GetAllRouteSheets(DateTime? date = null, int? repId = null, RouteSheetStatus? status = null)
        {
            var query = _routeSheets.AsQueryable();
            if (date.HasValue) query = query.Where(s => s.Date.Date == date.Value.Date);
            if (repId.HasValue && repId.Value > 0) query = query.Where(s => s.RepId == repId.Value);
            if (status.HasValue) query = query.Where(s => s.Status == status.Value);
            return query.OrderByDescending(s => s.Date).ThenByDescending(s => s.Id).ToList();
        }

        public DailyRepRouteSheet? GetRouteSheetById(int id) =>
            _routeSheets.FirstOrDefault(s => s.Id == id);

        public DailyRepRouteSheet CreateRouteSheet(int repId, DateTime date, Dictionary<int, int>? requestedQuantities = null)
        {
            var rep = GetRepById(repId) ?? _reps.First();
            var skus = GetCatalogSkus();

            // Find yesterday's closing stock for this rep
            var previousSheet = _routeSheets
                .Where(s => s.RepId == repId && s.Date < date.Date)
                .OrderByDescending(s => s.Date)
                .FirstOrDefault();

            // Previous balance
            decimal prevBalance = previousSheet?.NewOutstandingAccountBalance ?? 0m;

            var sheet = new DailyRepRouteSheet
            {
                Id = _routeSheets.Any() ? _routeSheets.Max(s => s.Id) + 1 : 1,
                RouteSheetRef = $"RTE-{date:yyyyMMdd}-{_routeSheets.Count + 1:000}",
                Date = date.Date,
                RepId = rep.Id,
                RepName = rep.FullName,
                RepCode = $"REP-{rep.Id:000}",
                VehicleRegNumber = rep.VehicleRegistration,
                RouteName = rep.RouteName,
                PosTerminalCode = rep.PosTerminalCode,
                PreviousAccountBalance = prevBalance,
                Status = RouteSheetStatus.Ordered,
                Notes = $"Daily route order for {rep.FullName}"
            };

            foreach (var sku in skus)
            {
                int yesterdayStock = 0;
                if (previousSheet != null)
                {
                    var prevItem = previousSheet.Items.FirstOrDefault(i => i.ProductId == sku.Id);
                    if (prevItem != null)
                    {
                        yesterdayStock = prevItem.QtyReturned;
                    }
                }

                int req = 0;
                if (requestedQuantities != null && requestedQuantities.TryGetValue(sku.Id, out var r))
                {
                    req = r;
                }

                sheet.Items.Add(new RouteSKUItem
                {
                    ProductId = sku.Id,
                    ProductCode = sku.Code,
                    ProductName = sku.Name,
                    Category = sku.Category,
                    UnitSellingPrice = sku.Price,
                    RepOrderRequestedQty = req,
                    YesterdayClosingStock = yesterdayStock,
                    TodayNewlyLoadedQty = 0 // Awaiting loading sign-off
                });
            }

            _routeSheets.Add(sheet);
            return sheet;
        }

        public bool SignOffDispatch(int sheetId, Dictionary<int, int> loadedQuantities, string dispatcherName, string notes)
        {
            var sheet = GetRouteSheetById(sheetId);
            if (sheet == null) return false;

            if (loadedQuantities != null)
            {
                foreach (var item in sheet.Items)
                {
                    if (loadedQuantities.TryGetValue(item.ProductId, out var loaded))
                    {
                        item.TodayNewlyLoadedQty = Math.Max(0, loaded);
                    }
                }
            }

            sheet.SignedOffByDispatcher = string.IsNullOrWhiteSpace(dispatcherName) ? "Loading Bay Supervisor" : dispatcherName;
            sheet.SignedOffAt = DateTime.UtcNow;
            sheet.Status = RouteSheetStatus.LoadedOnRoute;
            if (!string.IsNullOrWhiteSpace(notes)) sheet.Notes = notes;

            return true;
        }

        public bool ReconcileDailyRepAccount(ReconciliationInputDto input)
        {
            var sheet = GetRouteSheetById(input.RouteSheetId);
            if (sheet == null) return false;

            foreach (var item in sheet.Items)
            {
                input.SoldQuantities.TryGetValue(item.ProductId, out var sold);
                input.ReturnedQuantities.TryGetValue(item.ProductId, out var returned);
                input.DamagedQuantities.TryGetValue(item.ProductId, out var damaged);

                item.QtySold = Math.Max(0, sold);
                item.QtyReturned = Math.Max(0, returned);
                item.QtyDamaged = Math.Max(0, damaged);
            }

            sheet.CashCollected = Math.Max(0m, input.CashCollected);
            sheet.BankTransferCollected = Math.Max(0m, input.BankTransferCollected);
            sheet.PosTerminalCollected = Math.Max(0m, input.PosTerminalCollected);

            sheet.ReconciledBy = string.IsNullOrWhiteSpace(input.CashierName) ? "Main Cashier" : input.CashierName;
            sheet.ReconciledAt = DateTime.UtcNow;
            sheet.Status = RouteSheetStatus.Reconciled;
            if (!string.IsNullOrWhiteSpace(input.Notes)) sheet.Notes = input.Notes;

            // Carry over unsold closing stock for the next day's route sheet if one already exists
            var nextDaySheet = _routeSheets
                .Where(s => s.RepId == sheet.RepId && s.Date > sheet.Date)
                .OrderBy(s => s.Date)
                .FirstOrDefault();

            if (nextDaySheet != null)
            {
                foreach (var item in sheet.Items)
                {
                    var nextItem = nextDaySheet.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                    if (nextItem != null)
                    {
                        nextItem.YesterdayClosingStock = item.QtyReturned;
                    }
                }
                nextDaySheet.PreviousAccountBalance = sheet.NewOutstandingAccountBalance;
            }

            return true;
        }
    }
}
