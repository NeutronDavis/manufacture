using System.Security.Cryptography;
using Manufacture.Library.Calculations;
using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using AuditLogEntity = Manufacture.Models.Entities.AuditLog;
using CustomerEntity = Manufacture.Models.Entities.Customer;
using EmployeeEntity = Manufacture.Models.Entities.Employee;
using CustomerReturnEntity = Manufacture.Models.Entities.CustomerReturn;
using OrderEntity = Manufacture.Models.Entities.Order;
using OrderItemEntity = Manufacture.Models.Entities.OrderItem;
using OrderPaymentEntity = Manufacture.Models.Entities.OrderPayment;
using PosTerminalEntity = Manufacture.Models.Entities.PosTerminal;
using ProductionProductTypeEntity = Manufacture.Models.Entities.ProductionProductType;
using RecipeEntity = Manufacture.Models.Entities.Recipe;
using RouteSheetEntity = Manufacture.Models.Entities.DailyRepRouteSheet;
using RouteSheetItemEntity = Manufacture.Models.Entities.RouteSheetItem;
using RouteSheetSupermarketEntity = Manufacture.Models.Entities.RouteSheetSupermarket;
using RouteSheetSupermarketDebtEntity = Manufacture.Models.Entities.RouteSheetSupermarketDebt;
using SalesBonusRuleEntity = Manufacture.Models.Entities.SalesBonusRule;
using SalesRepEntity = Manufacture.Models.Entities.SalesRep;
using SupermarketAccountEntity = Manufacture.Models.Entities.SupermarketAccount;
using SupermarketAccountPriceEntity = Manufacture.Models.Entities.SupermarketAccountPrice;

namespace Manufacture.Services
{
    public class SalesService : ISalesService
    {
        private readonly ManufactureDbContext _db;

        public SalesService(ManufactureDbContext db)
        {
            _db = db;
        }

        public async Task<List<Customer>> GetCustomersAsync(
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Customers.AsNoTracking().AsQueryable();
            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            var customers = await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);
            return customers.Select(MapCustomer).ToList();
        }

        public async Task<Customer?> GetCustomerAsync(int id, CancellationToken cancellationToken = default)
        {
            var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            return customer == null ? null : MapCustomer(customer);
        }

        public async Task<Customer?> GetCustomerByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var customer = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId && u.CustomerId != null)
                .Select(u => u.Customer)
                .FirstOrDefaultAsync(cancellationToken);

            return customer == null || !customer.IsActive ? null : MapCustomer(customer);
        }

        public async Task<Customer> CreateCustomerAsync(CustomerDto dto, CancellationToken cancellationToken = default)
        {
            var category = ParseCustomerCategory(dto.Category);
            var customer = new CustomerEntity
            {
                Name = NormalizeRequired(dto.Name, "Customer name"),
                MemberCode = await GenerateMemberCodeAsync(category, cancellationToken),
                Category = (int)category,
                PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty,
                Address = dto.Address?.Trim() ?? string.Empty,
                OutstandingBalance = Math.Max(0m, dto.OutstandingBalance),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Customers.Add(customer);
            await _db.SaveChangesAsync(cancellationToken);
            return MapCustomer(customer);
        }

        public async Task<List<PosTerminal>> GetTerminalsAsync(
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var query = _db.PosTerminals
                .AsNoTracking()
                .Include(t => t.AssignedEmployee)
                .Include(t => t.AssignedUser)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(t => t.IsActive);
            }

            var terminals = await query.OrderBy(t => t.TerminalCode).ToListAsync(cancellationToken);
            return terminals.Select(MapTerminal).ToList();
        }

        public async Task<PosTerminal?> GetTerminalAsync(int id, CancellationToken cancellationToken = default)
        {
            var terminal = await _db.PosTerminals
                .AsNoTracking()
                .Include(t => t.AssignedEmployee)
                .Include(t => t.AssignedUser)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
            return terminal == null ? null : MapTerminal(terminal);
        }

        public async Task<PosTerminal> CreateTerminalAsync(
            PosTerminal terminal,
            CancellationToken cancellationToken = default)
        {
            var code = NormalizeRequired(terminal.TerminalCode, "Terminal code").ToUpperInvariant();
            if (await _db.PosTerminals.AnyAsync(t => t.TerminalCode == code, cancellationToken))
            {
                throw new InvalidOperationException("A terminal with that code already exists.");
            }

            var employee = await ResolveSalesEmployeeAsync(terminal.AssignedEmployeeId, cancellationToken);
            var entity = new PosTerminalEntity
            {
                TerminalCode = code,
                TerminalName = NormalizeRequired(terminal.TerminalName, "Terminal name"),
                AssignedEmployeeId = employee?.Id,
                AssignedUserId = terminal.AssignedUserId,
                CashAccountName = string.IsNullOrWhiteSpace(terminal.CashAccountName) ? "Bakery Cash Drawer" : terminal.CashAccountName.Trim(),
                TransferAccountName = string.IsNullOrWhiteSpace(terminal.TransferAccountName) ? "FirstBank - 3021948291" : terminal.TransferAccountName.Trim(),
                PosBankAccountName = string.IsNullOrWhiteSpace(terminal.PosBankAccountName) ? "Moniepoint POS - 9028129" : terminal.PosBankAccountName.Trim(),
                IsActive = terminal.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.PosTerminals.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            entity.AssignedEmployee = employee;
            return MapTerminal(entity);
        }

        public async Task<bool> UpdateTerminalAsync(
            PosTerminal terminal,
            CancellationToken cancellationToken = default)
        {
            var entity = await _db.PosTerminals
                .Include(t => t.AssignedEmployee)
                .Include(t => t.AssignedUser)
                .FirstOrDefaultAsync(t => t.Id == terminal.Id, cancellationToken);

            if (entity == null)
            {
                return false;
            }

            var employee = await ResolveSalesEmployeeAsync(terminal.AssignedEmployeeId, cancellationToken);
            var code = NormalizeRequired(terminal.TerminalCode, "Terminal code").ToUpperInvariant();
            if (await _db.PosTerminals.AnyAsync(t => t.TerminalCode == code && t.Id != terminal.Id, cancellationToken))
            {
                throw new InvalidOperationException("A terminal with that code already exists.");
            }

            entity.TerminalCode = code;
            entity.TerminalName = NormalizeRequired(terminal.TerminalName, "Terminal name");
            entity.AssignedEmployeeId = employee?.Id;
            entity.AssignedEmployee = employee;
            entity.AssignedUserId = terminal.AssignedUserId;
            entity.CashAccountName = NormalizeRequired(terminal.CashAccountName, "Cash account name");
            entity.TransferAccountName = NormalizeRequired(terminal.TransferAccountName, "Transfer account name");
            entity.PosBankAccountName = NormalizeRequired(terminal.PosBankAccountName, "POS bank account name");
            entity.IsActive = terminal.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> ToggleTerminalStatusAsync(int id, CancellationToken cancellationToken = default)
        {
            var terminal = await _db.PosTerminals.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
            if (terminal == null)
            {
                return false;
            }

            terminal.IsActive = !terminal.IsActive;
            terminal.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> AssignTerminalEmployeeAsync(
            int terminalId,
            int? employeeId,
            CancellationToken cancellationToken = default)
        {
            var terminal = await _db.PosTerminals.FirstOrDefaultAsync(t => t.Id == terminalId, cancellationToken);
            if (terminal == null)
            {
                return false;
            }

            var employee = await ResolveSalesEmployeeAsync(employeeId, cancellationToken);
            terminal.AssignedEmployeeId = employee?.Id;
            terminal.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<List<SalesRepProfile>> GetSalesRepsAsync(
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var query = _db.SalesReps
                .AsNoTracking()
                .Include(r => r.Employee)
                .Include(r => r.PosTerminal)
                .Include(r => r.Vehicle)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(r => r.IsActive);
            }

            var reps = await query.OrderBy(r => r.FullName).ToListAsync(cancellationToken);
            return reps.Select(MapSalesRep).ToList();
        }

        public async Task<SalesRepProfile?> GetSalesRepAsync(int id, CancellationToken cancellationToken = default)
        {
            var rep = await _db.SalesReps
                .AsNoTracking()
                .Include(r => r.Employee)
                .Include(r => r.PosTerminal)
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            return rep == null ? null : MapSalesRep(rep);
        }

        public async Task<SalesRepProfile?> GetSalesRepByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var rep = await _db.SalesReps
                .AsNoTracking()
                .Include(r => r.Employee)
                .Include(r => r.PosTerminal)
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(r => r.UserId == userId && r.IsActive, cancellationToken);
            return rep == null ? null : MapSalesRep(rep);
        }

        public async Task<List<Recipe>> GetSellableProductsAsync(CancellationToken cancellationToken = default)
        {
            var recipes = await _db.Recipes
                .AsNoTracking()
                .Include(r => r.ProductionProductType)
                .ThenInclude(p => p!.ProductionCategory)
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync(cancellationToken);

            return recipes.Select(MapRecipe).ToList();
        }

        public async Task<List<(int Id, string Code, string Name, ProductType Category, decimal Price)>> GetCatalogSkusAsync(
            CancellationToken cancellationToken = default)
        {
            var recipes = await _db.Recipes
                .AsNoTracking()
                .Include(r => r.ProductionProductType)
                .ThenInclude(p => p!.ProductionCategory)
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync(cancellationToken);

            return recipes
                .Select(r => (r.Id, r.Code, r.Name, MapProductType(r.ProductionProductType), r.SellingPrice))
                .ToList();
        }

        public async Task<List<Order>> GetOrdersAsync(CancellationToken cancellationToken = default)
        {
            var orders = await _db.Orders
                .AsNoTracking()
                .AsSplitQuery()
                .Include(o => o.OrderItems)
                .Include(o => o.OrderPayments)
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.Id)
                .ToListAsync(cancellationToken);

            return orders.Select(MapOrder).ToList();
        }

        public async Task<List<Order>> GetPreOrdersAsync(
            int? customerId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Orders
                .AsNoTracking()
                .AsSplitQuery()
                .Include(o => o.OrderItems)
                .Include(o => o.OrderPayments)
                .Where(o => o.OrderType == (int)OrderType.PreOrder);

            if (customerId.HasValue && customerId.Value > 0)
            {
                query = query.Where(o => o.CustomerId == customerId.Value);
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.Id)
                .ToListAsync(cancellationToken);

            return orders.Select(MapOrder).ToList();
        }

        public async Task<Order?> GetOrderAsync(int id, CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .AsSplitQuery()
                .Include(o => o.OrderItems)
                .Include(o => o.OrderPayments)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

            return order == null ? null : MapOrder(order);
        }

        public async Task<Order> CreateOrderAsync(
            CreateOrderDto dto,
            int? currentUserId,
            string? currentUserName,
            string? currentUserRole = null,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var isVendor = string.Equals(currentUserRole, "Customer", StringComparison.OrdinalIgnoreCase);
            var orderType = ParseOrderType(dto.OrderType);
            if (isVendor)
            {
                orderType = OrderType.PreOrder;
            }

            CustomerEntity customer;
            if (isVendor)
            {
                if (!currentUserId.HasValue)
                {
                    throw new InvalidOperationException("The customer account is not linked to a customer record.");
                }

                var portalUser = await _db.Users
                    .Include(u => u.Customer)
                    .FirstOrDefaultAsync(u => u.Id == currentUserId.Value, cancellationToken);

                customer = portalUser?.Customer
                    ?? throw new InvalidOperationException("This customer account is not linked to a customer record.");
            }
            else
            {
                customer = await ResolveOrCreateCustomerAsync(dto, cancellationToken);
            }

            PosTerminalEntity? terminal = null;
            if (!isVendor)
            {
                terminal = string.IsNullOrWhiteSpace(dto.PosTerminalCode)
                    ? null
                    : await _db.PosTerminals.FirstOrDefaultAsync(t => t.TerminalCode == dto.PosTerminalCode, cancellationToken);
            }

            SalesRepEntity? salesRep;
            if (!isVendor && string.Equals(currentUserRole, "SalesRep", StringComparison.OrdinalIgnoreCase))
            {
                if (!currentUserId.HasValue)
                {
                    throw new InvalidOperationException("Your sales rep login is not linked to an active sales representative profile.");
                }

                salesRep = await _db.SalesReps
                    .FirstOrDefaultAsync(r => r.UserId == currentUserId.Value && r.IsActive, cancellationToken)
                    ?? throw new InvalidOperationException("Your sales rep login is not linked to an active sales representative profile.");
            }
            else
            {
                salesRep = isVendor || orderType == OrderType.PreOrder
                    ? (dto.SalesRepId.HasValue
                        ? await _db.SalesReps.FirstOrDefaultAsync(r => r.Id == dto.SalesRepId.Value, cancellationToken)
                        : null)
                    : await ResolveSalesRepAsync(dto.SalesRepId, currentUserId, terminal, cancellationToken);
            }

            if (!isVendor && string.Equals(currentUserRole, "SalesRep", StringComparison.OrdinalIgnoreCase))
            {
                if (salesRep?.EmployeeId == null)
                {
                    throw new InvalidOperationException("Your sales rep login is not linked to an employee record.");
                }

                if (terminal == null || terminal.AssignedEmployeeId != salesRep.EmployeeId)
                {
                    throw new InvalidOperationException("Select a POS terminal assigned to your employee record.");
                }
            }

            if (!isVendor && orderType != OrderType.PreOrder && salesRep == null)
            {
                throw new InvalidOperationException("Select a sales representative for this order.");
            }

            var paymentMethod = isVendor || orderType == OrderType.PreOrder
                ? PaymentMethod.CreditOwing
                : ParsePaymentMethod(dto.PaymentMethod);

            var customPrices = await GetCustomPricesAsync(customer.Id, cancellationToken);
            var bonusRules = await GetActiveBonusRulesAsync(cancellationToken);
            var itemInputs = dto.Items.Where(i => i.Quantity > 0).ToList();
            if (itemInputs.Count == 0)
            {
                throw new InvalidOperationException("Please add at least one product to the order.");
            }

            var orderItems = new List<OrderItemEntity>();
            foreach (var input in itemInputs)
            {
                var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.Id == input.RecipeId && r.IsActive, cancellationToken)
                    ?? throw new InvalidOperationException("One or more selected products are no longer available.");

                var unitPrice = customPrices.TryGetValue(recipe.Id, out var customPrice)
                    ? customPrice
                    : recipe.SellingPrice;

                orderItems.Add(new OrderItemEntity
                {
                    RecipeId = recipe.Id,
                    ProductName = recipe.Name,
                    Quantity = input.Quantity,
                    BonusQuantity = CalculateBonus(bonusRules, recipe.Id, input.Quantity),
                    UnitPrice = unitPrice,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            var subTotal = orderItems.Sum(i => i.Quantity * i.UnitPrice);
            var discount = isVendor ? 0m : Math.Clamp(dto.Discount, 0m, subTotal);
            var total = subTotal - discount;
            var amountPaid = isVendor || orderType == OrderType.PreOrder
                ? 0m
                : Math.Clamp(dto.AmountPaid, 0m, total);
            var paymentStatus = ComputePaymentStatus(total, amountPaid);

            var order = new OrderEntity
            {
                OrderNumber = await GenerateOrderNumberAsync(cancellationToken),
                OrderType = (int)orderType,
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                CustomerCategory = customer.Category,
                SalesRepId = salesRep?.Id,
                SalesRepName = salesRep?.FullName,
                PosTerminalId = terminal?.Id,
                PosTerminalCode = terminal?.TerminalCode,
                BookedByType = isVendor ? (int)OrderBookedByType.Customer : (int)OrderBookedByType.Staff,
                CreatedByUserId = currentUserId,
                SubTotal = subTotal,
                Discount = discount,
                AmountPaid = amountPaid,
                PaymentMethod = (int)paymentMethod,
                PaymentStatus = (int)paymentStatus,
                Status = orderType == OrderType.PreOrder ? (int)OrderStatus.Pending : (int)OrderStatus.ReadyToLoad,
                ClosedAt = paymentStatus == OrderPaymentStatus.Completed ? DateTime.UtcNow : null,
                OrderDate = DateTime.UtcNow,
                TargetDeliveryDate = dto.TargetDeliveryDate == default ? DateTime.UtcNow : dto.TargetDeliveryDate,
                IsPrintedForLoading = false,
                Notes = dto.Notes?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var item in orderItems)
            {
                order.OrderItems.Add(item);
            }

            if (amountPaid > 0)
            {
                order.OrderPayments.Add(new OrderPaymentEntity
                {
                    Amount = amountPaid,
                    PaymentMethod = (int)paymentMethod,
                    PaymentDate = DateTime.UtcNow,
                    Reference = "ORDER-CREATE",
                    Notes = "Initial payment at order creation",
                    ReceivedBy = string.IsNullOrWhiteSpace(currentUserName) ? "Sales" : currentUserName.Trim(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            _db.Orders.Add(order);

            var balanceDue = total - amountPaid;
            customer.OutstandingBalance = Math.Max(0m, customer.OutstandingBalance + balanceDue);
            customer.UpdatedAt = DateTime.UtcNow;

            var supermarket = await _db.SupermarketAccounts.FirstOrDefaultAsync(a => a.CustomerId == customer.Id, cancellationToken);
            if (supermarket != null && balanceDue > 0)
            {
                supermarket.CurrentOutstandingCredit = Math.Max(0m, supermarket.CurrentOutstandingCredit + balanceDue);
                supermarket.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            order.Customer = customer;
            order.SalesRep = salesRep;
            order.PosTerminal = terminal;
            return MapOrder(order);
        }

        public async Task<bool> AssignOrderSalesRepAsync(
            int orderId,
            int? salesRepId,
            CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
            if (order == null)
            {
                return false;
            }

            // Only an open, unassigned order can receive a rep. Dispatched and
            // cancelled orders are closed, and reassignment is not part of this
            // workflow (the UI only offers assignment while the order is unassigned).
            if (order.SalesRepId.HasValue)
            {
                return false;
            }

            if (order.Status == (int)OrderStatus.Dispatched || order.Status == (int)OrderStatus.Cancelled)
            {
                return false;
            }

            if (!salesRepId.HasValue || salesRepId.Value <= 0)
            {
                return false;
            }

            var rep = await _db.SalesReps.FirstOrDefaultAsync(r => r.Id == salesRepId.Value && r.IsActive, cancellationToken);
            if (rep == null)
            {
                throw new InvalidOperationException("Select a valid active sales representative.");
            }

            order.SalesRepId = rep.Id;
            order.SalesRepName = rep.FullName;
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> MarkPrintedForLoadingAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
            if (order == null)
            {
                return false;
            }

            order.IsPrintedForLoading = true;
            order.PrintedAt = DateTime.UtcNow;
            if (order.Status == (int)OrderStatus.Pending || order.Status == (int)OrderStatus.InProduction)
            {
                order.Status = (int)OrderStatus.ReadyToLoad;
            }
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> VerifyAndDispatchAsync(
            int orderId,
            string loaderName,
            CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
            if (order == null || !order.IsPrintedForLoading)
            {
                return false;
            }

            order.VerifiedByLoaderName = string.IsNullOrWhiteSpace(loaderName) ? "Loading Supervisor" : loaderName.Trim();
            order.LoadedAt = DateTime.UtcNow;
            order.Status = (int)OrderStatus.Dispatched;
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<List<OrderPayment>> GetOrderPaymentsAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var payments = await _db.OrderPayments
                .AsNoTracking()
                .Where(p => p.OrderId == orderId)
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.Id)
                .ToListAsync(cancellationToken);

            return payments.Select(MapOrderPayment).ToList();
        }

        public async Task<bool> ApplyOrderPaymentAsync(
            OrderPaymentInputDto input,
            CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == input.OrderId, cancellationToken);

            if (order == null)
            {
                return false;
            }

            var total = order.TotalAmount ?? order.OrderItems.Sum(i => i.Quantity * i.UnitPrice);
            var balance = Math.Max(0m, total - order.AmountPaid);
            var amount = Math.Min(Math.Max(0m, input.Amount), balance);
            if (amount <= 0)
            {
                return false;
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            _db.OrderPayments.Add(new OrderPaymentEntity
            {
                OrderId = order.Id,
                Amount = amount,
                PaymentMethod = (int)ParsePaymentMethod(input.PaymentMethod),
                PaymentDate = DateTime.UtcNow,
                Reference = input.Reference?.Trim() ?? string.Empty,
                Notes = input.Notes?.Trim() ?? string.Empty,
                ReceivedBy = string.IsNullOrWhiteSpace(input.ReceivedBy) ? "Sales" : input.ReceivedBy.Trim(),
                CreatedAt = DateTime.UtcNow
            });

            order.AmountPaid += amount;
            order.PaymentStatus = (int)ComputePaymentStatus(total, order.AmountPaid);
            order.ClosedAt = order.PaymentStatus == (int)OrderPaymentStatus.Completed ? DateTime.UtcNow : null;
            order.UpdatedAt = DateTime.UtcNow;

            order.Customer.OutstandingBalance = Math.Max(0m, order.Customer.OutstandingBalance - amount);
            order.Customer.UpdatedAt = DateTime.UtcNow;

            var supermarket = await _db.SupermarketAccounts.FirstOrDefaultAsync(
                a => a.CustomerId == order.CustomerId,
                cancellationToken);
            if (supermarket != null)
            {
                supermarket.CurrentOutstandingCredit = Math.Max(0m, supermarket.CurrentOutstandingCredit - amount);
                supermarket.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        public async Task<List<SupermarketAccount>> GetSupermarketAccountsAsync(
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var query = _db.SupermarketAccounts
                .AsNoTracking()
                .Include(a => a.Customer)
                .Include(a => a.AssignedRep)
                .Include(a => a.SupermarketAccountPrices)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(a => a.IsActive);
            }

            var accounts = await query.OrderBy(a => a.AccountName).ToListAsync(cancellationToken);
            return accounts.Select(MapSupermarketAccount).ToList();
        }

        public async Task<SupermarketAccount?> GetSupermarketAccountAsync(int id, CancellationToken cancellationToken = default)
        {
            var account = await _db.SupermarketAccounts
                .AsNoTracking()
                .Include(a => a.Customer)
                .Include(a => a.AssignedRep)
                .Include(a => a.SupermarketAccountPrices)
                .FirstOrDefaultAsync(a => a.Id == id || a.CustomerId == id, cancellationToken);

            return account == null ? null : MapSupermarketAccount(account);
        }

        public async Task<bool> SaveSupermarketAccountAsync(
            SupermarketAccount account,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(account.AccountName))
            {
                return false;
            }

            SupermarketAccountEntity entity;
            CustomerEntity customer;

            if (account.Id > 0)
            {
                entity = await _db.SupermarketAccounts
                    .Include(a => a.Customer)
                    .FirstOrDefaultAsync(a => a.Id == account.Id, cancellationToken)
                    ?? throw new InvalidOperationException("The supermarket account was not found.");
                customer = entity.Customer;
            }
            else
            {
                customer = new CustomerEntity
                {
                    Name = account.AccountName.Trim(),
                    MemberCode = await GenerateMemberCodeAsync(CustomerCategory.Supermarket, cancellationToken),
                    Category = (int)CustomerCategory.Supermarket,
                    PhoneNumber = account.PhoneNumber?.Trim() ?? string.Empty,
                    Address = account.Address?.Trim() ?? string.Empty,
                    OutstandingBalance = 0m,
                    IsActive = account.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.Customers.Add(customer);
                await _db.SaveChangesAsync(cancellationToken);

                entity = new SupermarketAccountEntity
                {
                    CustomerId = customer.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _db.SupermarketAccounts.Add(entity);
            }

            entity.AccountName = account.AccountName.Trim();
            entity.ContactPerson = account.ContactPerson?.Trim() ?? string.Empty;
            entity.PhoneNumber = account.PhoneNumber?.Trim() ?? string.Empty;
            entity.Address = account.Address?.Trim() ?? string.Empty;
            entity.AssignedRepId = account.AssignedRepId > 0 ? account.AssignedRepId : null;
            entity.CreditLimit = Math.Max(0m, account.CreditLimit);
            entity.PaymentTerms = string.IsNullOrWhiteSpace(account.PaymentTerms) ? "Net 15 Days" : account.PaymentTerms.Trim();
            entity.IsActive = account.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            customer.Name = entity.AccountName;
            customer.PhoneNumber = entity.PhoneNumber;
            customer.Address = entity.Address;
            customer.IsActive = entity.IsActive;
            customer.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            account.Id = entity.Id;
            account.CustomerId = entity.CustomerId;
            return true;
        }

        public async Task<bool> SetCustomAgreedPriceAsync(
            int accountId,
            int productId,
            decimal customPrice,
            CancellationToken cancellationToken = default)
        {
            return await SetBatchCustomAgreedPricesAsync(
                accountId,
                new Dictionary<int, decimal> { [productId] = customPrice },
                cancellationToken);
        }

        public async Task<bool> SetBatchCustomAgreedPricesAsync(
            int accountId,
            Dictionary<int, decimal> prices,
            CancellationToken cancellationToken = default)
        {
            if (!await _db.SupermarketAccounts.AnyAsync(a => a.Id == accountId, cancellationToken))
            {
                return false;
            }

            foreach (var pair in prices)
            {
                var existing = await _db.SupermarketAccountPrices
                    .FirstOrDefaultAsync(p => p.SupermarketAccountId == accountId && p.RecipeId == pair.Key, cancellationToken);

                if (pair.Value > 0)
                {
                    if (existing == null)
                    {
                        _db.SupermarketAccountPrices.Add(new SupermarketAccountPriceEntity
                        {
                            SupermarketAccountId = accountId,
                            RecipeId = pair.Key,
                            AgreedPrice = pair.Value,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        existing.AgreedPrice = pair.Value;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (existing != null)
                {
                    _db.SupermarketAccountPrices.Remove(existing);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> RecordSupermarketPaymentAsync(
            int accountId,
            decimal amountPaid,
            string paymentMethod,
            string reference,
            CancellationToken cancellationToken = default)
        {
            if (amountPaid <= 0)
            {
                return false;
            }

            var account = await _db.SupermarketAccounts
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

            if (account == null)
            {
                return false;
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var openOrders = await _db.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.CustomerId == account.CustomerId && o.BalanceDue > 0)
                .OrderBy(o => o.OrderDate)
                .ThenBy(o => o.Id)
                .ToListAsync(cancellationToken);

            var remaining = amountPaid;
            var method = (int)ParsePaymentMethod(paymentMethod);

            foreach (var order in openOrders)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var total = order.TotalAmount ?? order.OrderItems.Sum(i => i.Quantity * i.UnitPrice);
                var balance = Math.Max(0m, total - order.AmountPaid);
                var applied = Math.Min(remaining, balance);
                if (applied <= 0)
                {
                    continue;
                }

                _db.OrderPayments.Add(new OrderPaymentEntity
                {
                    OrderId = order.Id,
                    Amount = applied,
                    PaymentMethod = method,
                    PaymentDate = DateTime.UtcNow,
                    Reference = reference?.Trim() ?? string.Empty,
                    Notes = "Supermarket account payment allocated to oldest open order",
                    ReceivedBy = "Supermarket Payment",
                    CreatedAt = DateTime.UtcNow
                });

                order.AmountPaid += applied;
                order.PaymentStatus = (int)ComputePaymentStatus(total, order.AmountPaid);
                order.ClosedAt = order.PaymentStatus == (int)OrderPaymentStatus.Completed ? DateTime.UtcNow : null;
                order.UpdatedAt = DateTime.UtcNow;
                remaining -= applied;
            }

            account.CurrentOutstandingCredit = Math.Max(0m, account.CurrentOutstandingCredit - amountPaid);
            account.Customer.OutstandingBalance = Math.Max(0m, account.Customer.OutstandingBalance - amountPaid);
            account.UpdatedAt = DateTime.UtcNow;
            account.Customer.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        public async Task<bool> ToggleSupermarketStatusAsync(int id, CancellationToken cancellationToken = default)
        {
            var account = await _db.SupermarketAccounts
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (account == null)
            {
                return false;
            }

            account.IsActive = !account.IsActive;
            account.Customer.IsActive = account.IsActive;
            account.UpdatedAt = DateTime.UtcNow;
            account.Customer.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> FulfillSupermarketOrderAsync(
            int accountId,
            int repId,
            List<OrderItemDto> items,
            string notes,
            CancellationToken cancellationToken = default)
        {
            if (items == null || items.Count == 0)
            {
                return false;
            }

            var account = await _db.SupermarketAccounts
                .Include(a => a.Customer)
                .Include(a => a.AssignedRep)
                .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

            if (account == null)
            {
                return false;
            }

            var rep = await _db.SalesReps
                .Include(r => r.PosTerminal)
                .FirstOrDefaultAsync(r => r.Id == repId, cancellationToken)
                ?? account.AssignedRep;

            if (rep == null)
            {
                return false;
            }

            var customPrices = await GetCustomPricesAsync(account.CustomerId, cancellationToken);
            var bonusRules = await GetActiveBonusRulesAsync(cancellationToken);
            var order = new OrderEntity
            {
                OrderNumber = await GenerateOrderNumberAsync(cancellationToken),
                OrderType = (int)OrderType.ImmediateSale,
                CustomerId = account.CustomerId,
                CustomerName = account.AccountName,
                CustomerCategory = (int)CustomerCategory.Supermarket,
                SalesRepId = rep.Id,
                SalesRepName = rep.FullName,
                PosTerminalId = rep.PosTerminalId,
                PosTerminalCode = rep.PosTerminal?.TerminalCode,
                Discount = 0m,
                AmountPaid = 0m,
                PaymentMethod = (int)PaymentMethod.CreditOwing,
                PaymentStatus = (int)OrderPaymentStatus.Open,
                Status = (int)OrderStatus.Dispatched,
                OrderDate = DateTime.UtcNow,
                TargetDeliveryDate = DateTime.UtcNow,
                Notes = string.IsNullOrWhiteSpace(notes) ? "Supermarket credit supply" : notes.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            decimal subTotal = 0m;
            foreach (var input in items.Where(i => i.Quantity > 0))
            {
                var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.Id == input.RecipeId && r.IsActive, cancellationToken);
                if (recipe == null)
                {
                    continue;
                }

                var price = customPrices.TryGetValue(recipe.Id, out var custom)
                    ? custom
                    : (input.UnitPrice > 0 ? input.UnitPrice : recipe.SellingPrice);

                subTotal += input.Quantity * price;
                order.OrderItems.Add(new OrderItemEntity
                {
                    RecipeId = recipe.Id,
                    ProductName = recipe.Name,
                    Quantity = input.Quantity,
                    BonusQuantity = CalculateBonus(bonusRules, recipe.Id, input.Quantity),
                    UnitPrice = price,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            if (!order.OrderItems.Any())
            {
                return false;
            }

            order.SubTotal = subTotal;
            account.CurrentOutstandingCredit = Math.Max(0m, account.CurrentOutstandingCredit + subTotal);
            account.Customer.OutstandingBalance = Math.Max(0m, account.Customer.OutstandingBalance + subTotal);
            account.UpdatedAt = DateTime.UtcNow;
            account.Customer.UpdatedAt = DateTime.UtcNow;

            _db.Orders.Add(order);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<List<DailyRepRouteSheet>> GetRouteSheetsAsync(
            DateTime? date = null,
            int? repId = null,
            RouteSheetStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var query = _db.DailyRepRouteSheets
                .AsNoTracking()
                .Include(s => s.RouteSheetItems)
                .Include(s => s.RouteSheetSupermarkets)
                .ThenInclude(x => x.SupermarketAccount)
                .Include(s => s.RouteSheetSupermarketDebts)
                .ThenInclude(x => x.SupermarketAccount)
                .Include(s => s.SalesRep)
                .Include(s => s.Vehicle)
                .Include(s => s.FleetDispatch)
                .ThenInclude(f => f!.Vehicle)
                .AsQueryable();

            if (date.HasValue)
            {
                var day = DateOnly.FromDateTime(date.Value);
                query = query.Where(s => s.RouteDate == day);
            }

            if (repId.HasValue && repId.Value > 0)
            {
                query = query.Where(s => s.SalesRepId == repId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == (int)status.Value);
            }

            var sheets = await query
                .OrderByDescending(s => s.RouteDate)
                .ThenByDescending(s => s.Id)
                .ToListAsync(cancellationToken);

            return sheets.Select(MapRouteSheet).ToList();
        }

        public async Task<DailyRepRouteSheet?> GetRouteSheetAsync(int id, CancellationToken cancellationToken = default)
        {
            var sheet = await _db.DailyRepRouteSheets
                .AsNoTracking()
                .Include(s => s.RouteSheetItems)
                .Include(s => s.RouteSheetSupermarkets)
                .ThenInclude(x => x.SupermarketAccount)
                .Include(s => s.RouteSheetSupermarketDebts)
                .ThenInclude(x => x.SupermarketAccount)
                .Include(s => s.SalesRep)
                .Include(s => s.Vehicle)
                .Include(s => s.FleetDispatch)
                .ThenInclude(f => f!.Vehicle)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            return sheet == null ? null : MapRouteSheet(sheet);
        }

        public async Task<DailyRepRouteSheet> CreateRouteSheetAsync(
            int repId,
            DateTime date,
            Dictionary<int, int>? requestedQuantities = null,
            IReadOnlyCollection<int>? supermarketAccountIds = null,
            int? fleetDispatchId = null,
            CancellationToken cancellationToken = default)
        {
            var rep = await _db.SalesReps
                .Include(r => r.Vehicle)
                .Include(r => r.PosTerminal)
                .FirstOrDefaultAsync(r => r.Id == repId, cancellationToken)
                ?? throw new InvalidOperationException("Sales representative not found.");

            var routeDate = DateOnly.FromDateTime(date);

            // A sales rep may create more than one route sheet per day.

            // A daily route must ride an existing, newly created delivery dispatch.
            if (!fleetDispatchId.HasValue || fleetDispatchId.Value <= 0)
            {
                throw new InvalidOperationException("Select a delivery dispatch for this route sheet.");
            }

            var fleetDispatch = await _db.FleetDispatches
                .Include(d => d.Vehicle)
                .FirstOrDefaultAsync(d => d.Id == fleetDispatchId.Value, cancellationToken)
                ?? throw new InvalidOperationException("The selected delivery dispatch was not found.");

            if (fleetDispatch.Status != (int)FleetDispatchStatus.Planned)
            {
                throw new InvalidOperationException("Only planned delivery dispatches can be linked to a route sheet.");
            }

            var fleetDispatchAlreadyLinked = await _db.DailyRepRouteSheets
                .AnyAsync(s => s.FleetDispatchId == fleetDispatch.Id, cancellationToken);
            if (!FleetDispatchRules.IsAvailableForRouteSheet(
                    (FleetDispatchStatus)fleetDispatch.Status,
                    fleetDispatchAlreadyLinked))
            {
                throw new InvalidOperationException("That delivery dispatch is already linked to another route sheet.");
            }

            var previousSheet = await _db.DailyRepRouteSheets
                .AsNoTracking()
                .Include(s => s.RouteSheetItems)
                .Include(s => s.RouteSheetSupermarkets)
                .Where(s => s.SalesRepId == repId && s.RouteDate < routeDate)
                .OrderByDescending(s => s.RouteDate)
                .FirstOrDefaultAsync(cancellationToken);

            var recipes = await _db.Recipes
                .AsNoTracking()
                .Include(r => r.ProductionProductType)
                .ThenInclude(p => p!.ProductionCategory)
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync(cancellationToken);

            var sheet = new RouteSheetEntity
            {
                RouteSheetRef = await GenerateRouteSheetRefAsync(date, cancellationToken),
                RouteDate = routeDate,
                SalesRepId = rep.Id,
                RouteName = rep.RouteName,
                VehicleId = fleetDispatch.VehicleId,
                VehicleRegistration = fleetDispatch.Vehicle?.RegistrationNumber,
                FleetDispatchId = fleetDispatch.Id,
                PosTerminalId = rep.PosTerminalId,
                PosTerminalCode = rep.PosTerminal?.TerminalCode,
                PreviousAccountBalance = 0m,
                Status = (int)RouteSheetStatus.Ordered,
                Notes = $"Daily route order for {rep.FullName} · {fleetDispatch.DispatchRef}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var recipe in recipes)
            {
                var yesterday = previousSheet?.RouteSheetItems
                    .FirstOrDefault(i => i.RecipeId == recipe.Id)?.ClosingStockQty ?? 0;

                var requested = 0;
                requestedQuantities?.TryGetValue(recipe.Id, out requested);

                sheet.RouteSheetItems.Add(new RouteSheetItemEntity
                {
                    RecipeId = recipe.Id,
                    ProductCode = recipe.Code,
                    ProductName = recipe.Name,
                    Category = (int)MapProductType(recipe.ProductionProductType),
                    UnitSellingPrice = recipe.SellingPrice,
                    RepOrderRequestedQty = Math.Max(0, requested),
                    YesterdayClosingStock = yesterday,
                    TodayNewlyLoadedQty = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            // A null selection inherits the previous day's supermarket round, so the
            // next-day sheet created by reconciliation keeps the same stops. An
            // explicit (possibly empty) selection replaces it.
            var participantIds = supermarketAccountIds == null
                ? previousSheet?.RouteSheetSupermarkets.Select(x => x.SupermarketAccountId).Distinct().ToList()
                  ?? new List<int>()
                : supermarketAccountIds.Distinct().ToList();

            if (participantIds.Count > 0)
            {
                var supermarkets = await _db.SupermarketAccounts
                    .Where(a => participantIds.Contains(a.Id) && a.IsActive)
                    .OrderBy(a => a.AccountName)
                    .ToListAsync(cancellationToken);

                foreach (var supermarket in supermarkets)
                {
                    sheet.RouteSheetSupermarkets.Add(new RouteSheetSupermarketEntity
                    {
                        SupermarketAccountId = supermarket.Id,
                        SupermarketAccount = supermarket,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            _db.DailyRepRouteSheets.Add(sheet);
            await _db.SaveChangesAsync(cancellationToken);
            sheet.SalesRep = rep;
            sheet.FleetDispatch = fleetDispatch;
            return MapRouteSheet(sheet);
        }

        public async Task<bool> UpdateRouteSheetSupermarketsAsync(
            int sheetId,
            IReadOnlyCollection<int> accountIds,
            CancellationToken cancellationToken = default)
        {
            var sheet = await _db.DailyRepRouteSheets
                .Include(s => s.RouteSheetSupermarkets)
                .FirstOrDefaultAsync(s => s.Id == sheetId, cancellationToken);

            if (sheet == null || sheet.Status != (int)RouteSheetStatus.Ordered)
            {
                return false;
            }

            var requestedIds = (accountIds ?? Array.Empty<int>()).Distinct().ToList();
            var validIds = requestedIds.Count == 0
                ? new List<int>()
                : await _db.SupermarketAccounts
                    .Where(a => requestedIds.Contains(a.Id) && a.IsActive)
                    .Select(a => a.Id)
                    .ToListAsync(cancellationToken);

            var existingIds = sheet.RouteSheetSupermarkets
                .Select(x => x.SupermarketAccountId)
                .ToHashSet();

            var toRemove = sheet.RouteSheetSupermarkets
                .Where(x => !validIds.Contains(x.SupermarketAccountId))
                .ToList();

            if (toRemove.Count > 0)
            {
                _db.RouteSheetSupermarkets.RemoveRange(toRemove);
            }

            foreach (var id in validIds.Where(id => !existingIds.Contains(id)))
            {
                sheet.RouteSheetSupermarkets.Add(new RouteSheetSupermarketEntity
                {
                    RouteSheetId = sheet.Id,
                    SupermarketAccountId = id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            sheet.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> SaveRouteSheetSupermarketDebtsAsync(
            int routeSheetId,
            IReadOnlyDictionary<int, decimal> debtAmounts,
            IReadOnlyCollection<int> settledAccountIds,
            string recordedBy,
            CancellationToken cancellationToken = default)
        {
            var sheet = await _db.DailyRepRouteSheets
                .Include(s => s.RouteSheetSupermarkets)
                .Include(s => s.RouteSheetSupermarketDebts)
                .FirstOrDefaultAsync(s => s.Id == routeSheetId, cancellationToken);

            // Debt is only written between gate sign-off and reconciliation.
            if (sheet == null || sheet.Status != (int)RouteSheetStatus.LoadedOnRoute)
            {
                return false;
            }

            var participantIds = sheet.RouteSheetSupermarkets
                .Select(x => x.SupermarketAccountId)
                .ToHashSet();

            if (participantIds.Count == 0)
            {
                return false;
            }

            var settled = (settledAccountIds ?? Array.Empty<int>()).ToHashSet();
            var amounts = debtAmounts ?? new Dictionary<int, decimal>();

            var accounts = await _db.SupermarketAccounts
                .Include(a => a.Customer)
                .Where(a => participantIds.Contains(a.Id))
                .ToListAsync(cancellationToken);

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var editor = string.IsNullOrWhiteSpace(recordedBy) ? "Sales Reconciliation" : recordedBy.Trim();

            foreach (var account in accounts)
            {
                amounts.TryGetValue(account.Id, out var requested);
                var isSettled = settled.Contains(account.Id);
                var debt = isSettled ? 0m : Math.Max(0m, requested);

                var existing = sheet.RouteSheetSupermarketDebts
                    .FirstOrDefault(d => d.SupermarketAccountId == account.Id);

                // Nothing owed and nothing recorded yet: leave no noise row.
                if (existing == null && !isSettled && debt <= 0m)
                {
                    continue;
                }

                var delta = RouteSupermarketDebtCalculator.CalculateCreditDelta(
                    existing?.AppliedCredit ?? 0m,
                    isSettled,
                    debt);

                if (existing == null)
                {
                    existing = new RouteSheetSupermarketDebtEntity
                    {
                        RouteSheetId = sheet.Id,
                        SupermarketAccountId = account.Id,
                        IsSettled = isSettled,
                        DebtAmount = debt,
                        AppliedCredit = 0m,
                        RecordedBy = editor,
                        RecordedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    sheet.RouteSheetSupermarketDebts.Add(existing);
                }
                else
                {
                    existing.IsSettled = isSettled;
                    existing.DebtAmount = debt;
                    existing.RecordedBy = editor;
                    existing.UpdatedAt = DateTime.UtcNow;
                }

                existing.AppliedCredit = Math.Max(0m, existing.AppliedCredit + delta);

                if (delta != 0m)
                {
                    account.CurrentOutstandingCredit = Math.Max(0m, account.CurrentOutstandingCredit + delta);
                    account.Customer.OutstandingBalance = Math.Max(0m, account.Customer.OutstandingBalance + delta);
                    account.UpdatedAt = DateTime.UtcNow;
                    account.Customer.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        public async Task<List<SalesRepRouteDebtorDto>> GetSalesRepRouteDebtorsAsync(
            int salesRepId,
            CancellationToken cancellationToken = default)
        {
            if (salesRepId <= 0)
            {
                return new List<SalesRepRouteDebtorDto>();
            }

            var rows = await _db.RouteSheetSupermarketDebts
                .AsNoTracking()
                .Include(d => d.RouteSheet)
                .ThenInclude(s => s.SalesRep)
                .Include(d => d.SupermarketAccount)
                .Where(d => !d.IsSettled
                    && d.DebtAmount > 0
                    && d.RouteSheet.SalesRepId == salesRepId)
                .ToListAsync(cancellationToken);

            return rows
                .GroupBy(d => d.RouteSheet)
                .Select(group => new SalesRepRouteDebtorDto
                {
                    RouteSheetId = group.Key.Id,
                    RouteSheetRef = group.Key.RouteSheetRef,
                    RouteDate = group.Key.RouteDate.ToDateTime(TimeOnly.MinValue),
                    RouteName = group.Key.RouteName,
                    SalesRepName = group.Key.SalesRep?.FullName ?? string.Empty,
                    Status = ((RouteSheetStatus)group.Key.Status).ToString(),
                    Debtors = group
                        .OrderBy(d => d.SupermarketAccount.AccountName)
                        .Select(d => new SalesRepRouteDebtorLineDto
                        {
                            SupermarketAccountId = d.SupermarketAccountId,
                            AccountName = d.SupermarketAccount.AccountName,
                            ContactPerson = d.SupermarketAccount.ContactPerson,
                            PhoneNumber = d.SupermarketAccount.PhoneNumber,
                            DebtAmount = d.DebtAmount,
                            CurrentOutstandingCredit = d.SupermarketAccount.CurrentOutstandingCredit,
                            CreditLimit = d.SupermarketAccount.CreditLimit,
                            RecordedBy = d.RecordedBy,
                            RecordedAt = d.RecordedAt
                        })
                        .ToList()
                })
                .OrderBy(dto => dto.RouteName)
                .ThenByDescending(dto => dto.RouteDate)
                .ThenByDescending(dto => dto.RouteSheetId)
                .ToList();
        }

        public async Task<bool> SignOffDispatchAsync(
            int sheetId,
            Dictionary<int, int> loadedQuantities,
            string dispatcherName,
            string notes,
            CancellationToken cancellationToken = default)
        {
            var sheet = await _db.DailyRepRouteSheets
                .Include(s => s.RouteSheetItems)
                .FirstOrDefaultAsync(s => s.Id == sheetId, cancellationToken);

            if (sheet == null || sheet.Status != (int)RouteSheetStatus.Ordered)
            {
                return false;
            }

            foreach (var item in sheet.RouteSheetItems)
            {
                if (loadedQuantities.TryGetValue(item.RecipeId, out var loaded))
                {
                    item.TodayNewlyLoadedQty = Math.Max(0, loaded);
                    item.UpdatedAt = DateTime.UtcNow;
                }
            }

            sheet.SignedOffByDispatcher = string.IsNullOrWhiteSpace(dispatcherName) ? "Loading Supervisor" : dispatcherName.Trim();
            sheet.SignedOffAt = DateTime.UtcNow;
            sheet.Status = (int)RouteSheetStatus.LoadedOnRoute;
            sheet.Notes = string.IsNullOrWhiteSpace(notes) ? sheet.Notes : notes.Trim();
            sheet.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> ReconcileDailyRepAccountAsync(
            ReconciliationInputDto input,
            CancellationToken cancellationToken = default)
        {
            var sheet = await _db.DailyRepRouteSheets
                .Include(s => s.RouteSheetItems)
                .Include(s => s.SalesRep)
                .FirstOrDefaultAsync(s => s.Id == input.RouteSheetId, cancellationToken);

            if (sheet == null)
            {
                return false;
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            foreach (var item in sheet.RouteSheetItems)
            {
                input.SoldQuantities.TryGetValue(item.RecipeId, out var sold);
                input.ReturnedQuantities.TryGetValue(item.RecipeId, out var returned);
                input.DamagedQuantities.TryGetValue(item.RecipeId, out var damaged);
                input.ReplacementQuantities.TryGetValue(item.RecipeId, out var replacements);

                item.QtySold = Math.Max(0, sold);
                item.ClosingStockQty = Math.Max(0, returned);
                item.QtyDamaged = Math.Max(0, damaged);
                item.QtyReplacementIssued = Math.Max(0, replacements);
                item.UpdatedAt = DateTime.UtcNow;
            }

            sheet.CashCollected = Math.Max(0m, input.CashCollected);
            sheet.BankTransferCollected = Math.Max(0m, input.BankTransferCollected);
            sheet.PosTerminalCollected = Math.Max(0m, input.PosTerminalCollected);
            sheet.TotalExpectedSalesRevenue = sheet.RouteSheetItems.Sum(i => i.QtySold * i.UnitSellingPrice);
            sheet.ReconciledBy = string.IsNullOrWhiteSpace(input.CashierName) ? "Main Cashier" : input.CashierName.Trim();
            sheet.ReconciledAt = DateTime.UtcNow;
            sheet.Status = (int)RouteSheetStatus.Reconciled;
            sheet.Notes = string.IsNullOrWhiteSpace(input.Notes) ? sheet.Notes : input.Notes.Trim();
            sheet.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            var nextDate = sheet.RouteDate.AddDays(1);
            var nextSheet = await _db.DailyRepRouteSheets
                .Include(s => s.RouteSheetItems)
                .FirstOrDefaultAsync(
                    s => s.SalesRepId == sheet.SalesRepId && s.RouteDate == nextDate,
                    cancellationToken);

            // Do not auto-create the next day's sheet: every new route sheet must be
            // linked to a freshly created delivery dispatch. Only closing stock rolls
            // forward; prior debt never carries into another reconciliation.
            if (nextSheet != null)
            {
                nextSheet.UpdatedAt = DateTime.UtcNow;
                foreach (var current in sheet.RouteSheetItems)
                {
                    var next = nextSheet.RouteSheetItems.FirstOrDefault(i => i.RecipeId == current.RecipeId);
                    if (next != null)
                    {
                        next.YesterdayClosingStock = current.ClosingStockQty;
                        next.UpdatedAt = DateTime.UtcNow;
                    }
                }
                await _db.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DeleteRouteSheetAsync(
            int sheetId,
            string deletedBy,
            CancellationToken cancellationToken = default)
        {
            var sheet = await _db.DailyRepRouteSheets
                .Include(s => s.RouteSheetItems)
                .FirstOrDefaultAsync(s => s.Id == sheetId, cancellationToken);

            if (sheet == null || sheet.Status != (int)RouteSheetStatus.Ordered)
            {
                return false;
            }

            _db.RouteSheetItems.RemoveRange(sheet.RouteSheetItems);
            _db.DailyRepRouteSheets.Remove(sheet);
            _db.AuditLogs.Add(new AuditLogEntity
            {
                Action = "Delete",
                EntityName = "DailyRepRouteSheet",
                EntityId = sheet.Id.ToString(),
                NewValues = $"Deleted route sheet {sheet.RouteSheetRef} by {deletedBy}.",
                OccurredAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<List<CustomerReturn>> GetCustomerReturnsAsync(
            DateTime? date = null,
            int? salesRepId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _db.CustomerReturns
                .AsNoTracking()
                .Include(r => r.Customer)
                .Include(r => r.Recipe)
                .Include(r => r.SalesRep)
                .AsQueryable();

            if (date.HasValue)
            {
                var start = date.Value.Date;
                var end = start.AddDays(1);
                query = query.Where(r => r.ReturnDate >= start && r.ReturnDate < end);
            }

            if (salesRepId.HasValue)
            {
                query = query.Where(r => r.SalesRepId == salesRepId.Value);
            }

            var returns = await query.OrderByDescending(r => r.ReturnDate).ToListAsync(cancellationToken);
            return returns.Select(MapCustomerReturn).ToList();
        }

        public async Task<CustomerReturn> RecordCustomerReturnAsync(
            CustomerReturnInputDto input,
            CancellationToken cancellationToken = default)
        {
            var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.Id == input.RecipeId, cancellationToken)
                ?? throw new InvalidOperationException("Product not found.");
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == input.CustomerId, cancellationToken)
                ?? throw new InvalidOperationException("Customer not found.");

            var unitPrice = input.UnitPrice;
            if (unitPrice <= 0 && input.OrderId.HasValue)
            {
                unitPrice = await _db.OrderItems
                    .Where(i => i.OrderId == input.OrderId.Value && i.RecipeId == input.RecipeId)
                    .Select(i => i.UnitPrice)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            if (unitPrice <= 0)
            {
                unitPrice = recipe.SellingPrice;
            }

            var entity = new CustomerReturnEntity
            {
                ReturnNumber = await GenerateReturnNumberAsync(cancellationToken),
                OrderId = input.OrderId,
                CustomerId = customer.Id,
                RecipeId = recipe.Id,
                SalesRepId = input.SalesRepId,
                Quantity = Math.Max(1, input.Quantity),
                UnitPrice = unitPrice,
                ReplacementQuantity = Math.Max(0, input.ReplacementQuantity),
                ReturnDate = DateTime.UtcNow,
                Reason = string.IsNullOrWhiteSpace(input.Reason) ? "Mold / expired return" : input.Reason.Trim(),
                ReplacementIssued = input.ReplacementQuantity > 0,
                Notes = input.Notes?.Trim() ?? string.Empty,
                RecordedBy = string.IsNullOrWhiteSpace(input.RecordedBy) ? "Sales" : input.RecordedBy.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _db.CustomerReturns.Add(entity);

            if (input.ReplacementQuantity > 0 && input.SalesRepId.HasValue)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var sheet = await _db.DailyRepRouteSheets
                    .Include(s => s.RouteSheetItems)
                    .FirstOrDefaultAsync(
                        s => s.SalesRepId == input.SalesRepId.Value && s.RouteDate == today,
                        cancellationToken);

                var item = sheet?.RouteSheetItems.FirstOrDefault(i => i.RecipeId == input.RecipeId);
                if (item != null)
                {
                    item.QtyReplacementIssued += input.ReplacementQuantity;
                    item.UpdatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                var refund = entity.Quantity * unitPrice;
                customer.OutstandingBalance = Math.Max(0m, customer.OutstandingBalance - refund);
                customer.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
            entity.Recipe = recipe;
            entity.Customer = customer;
            return MapCustomerReturn(entity);
        }

        public async Task<List<BonusRule>> GetBonusRulesAsync(
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var query = _db.SalesBonusRules
                .AsNoTracking()
                .Include(r => r.Recipe)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(r => r.IsActive);
            }

            var rules = await query.OrderBy(r => r.Recipe.Name).ThenBy(r => r.ThresholdQuantity).ToListAsync(cancellationToken);
            return rules.Select(rule => MapBonusRule(rule)).ToList();
        }

        public async Task<int> CalculateBonusQuantityAsync(
            int recipeId,
            int quantity,
            CancellationToken cancellationToken = default)
        {
            if (quantity <= 0)
            {
                return 0;
            }

            var rules = await GetActiveBonusRulesAsync(cancellationToken);
            return CalculateBonus(rules, recipeId, quantity);
        }

        public async Task<BonusRule> SaveBonusRuleAsync(BonusRuleDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.ThresholdQuantity <= 0 || dto.BonusQuantity <= 0)
            {
                throw new InvalidOperationException("Threshold and bonus quantities must be greater than zero.");
            }

            if (!await _db.Recipes.AnyAsync(r => r.Id == dto.RecipeId, cancellationToken))
            {
                throw new InvalidOperationException("Select a valid product.");
            }

            SalesBonusRuleEntity entity;
            if (dto.Id > 0)
            {
                entity = await _db.SalesBonusRules.FirstOrDefaultAsync(r => r.Id == dto.Id, cancellationToken)
                    ?? throw new InvalidOperationException("The bonus rule was not found.");
            }
            else
            {
                entity = await _db.SalesBonusRules.FirstOrDefaultAsync(
                    r => r.RecipeId == dto.RecipeId && r.ThresholdQuantity == dto.ThresholdQuantity,
                    cancellationToken) ?? new SalesBonusRuleEntity { CreatedAt = DateTime.UtcNow };

                if (entity.Id == 0)
                {
                    _db.SalesBonusRules.Add(entity);
                }
            }

            entity.RecipeId = dto.RecipeId;
            entity.ThresholdQuantity = dto.ThresholdQuantity;
            entity.BonusQuantity = dto.BonusQuantity;
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.Id == entity.RecipeId, cancellationToken);
            return MapBonusRule(entity, recipe);
        }

        public async Task<bool> ToggleBonusRuleStatusAsync(int id, CancellationToken cancellationToken = default)
        {
            var rule = await _db.SalesBonusRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (rule == null)
            {
                return false;
            }

            rule.IsActive = !rule.IsActive;
            rule.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<CustomerEntity> ResolveOrCreateCustomerAsync(
            CreateOrderDto dto,
            CancellationToken cancellationToken)
        {
            CustomerEntity? customer = null;
            if (dto.CustomerId > 0)
            {
                customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == dto.CustomerId, cancellationToken);
            }

            var name = string.IsNullOrWhiteSpace(dto.CustomerName)
                ? "Walk-In Counter Customer"
                : dto.CustomerName.Trim();

            if (customer == null)
            {
                customer = await _db.Customers.FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
            }

            if (customer != null)
            {
                return customer;
            }

            var category = ParseCustomerCategory(dto.CustomerCategory);
            customer = new CustomerEntity
            {
                Name = name,
                MemberCode = await GenerateMemberCodeAsync(category, cancellationToken),
                Category = (int)category,
                PhoneNumber = string.Empty,
                Address = string.Empty,
                OutstandingBalance = 0m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync(cancellationToken);
            return customer;
        }

        private async Task<SalesRepEntity?> ResolveSalesRepAsync(
            int? requestedRepId,
            int? currentUserId,
            PosTerminalEntity? terminal,
            CancellationToken cancellationToken)
        {
            if (requestedRepId.HasValue && requestedRepId.Value > 0)
            {
                return await _db.SalesReps.FirstOrDefaultAsync(r => r.Id == requestedRepId.Value, cancellationToken);
            }

            if (currentUserId.HasValue)
            {
                var byUser = await _db.SalesReps.FirstOrDefaultAsync(r => r.UserId == currentUserId.Value, cancellationToken);
                if (byUser != null)
                {
                    return byUser;
                }
            }

            if (terminal?.AssignedEmployeeId != null)
            {
                return await _db.SalesReps.FirstOrDefaultAsync(r => r.EmployeeId == terminal.AssignedEmployeeId, cancellationToken);
            }

            return null;
        }

        private async Task<EmployeeEntity?> ResolveSalesEmployeeAsync(
            int? employeeId,
            CancellationToken cancellationToken)
        {
            if (!employeeId.HasValue || employeeId.Value <= 0)
            {
                return null;
            }

            var employee = await _db.Employees
                .Include(e => e.DepartmentNavigation)
                .FirstOrDefaultAsync(e => e.Id == employeeId.Value && !e.IsDeleted && e.IsActive, cancellationToken);

            if (employee == null || !IsSalesDepartment(employee))
            {
                throw new InvalidOperationException("Select an active employee from the Sales department.");
            }

            return employee;
        }

        private static bool IsSalesDepartment(EmployeeEntity employee)
        {
            return string.Equals(employee.DepartmentNavigation?.Name, "Sales", StringComparison.OrdinalIgnoreCase)
                || string.Equals(employee.Department, "Sales", StringComparison.OrdinalIgnoreCase)
                || employee.Department.Contains("Sales", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<Dictionary<int, decimal>> GetCustomPricesAsync(
            int customerId,
            CancellationToken cancellationToken)
        {
            return await _db.SupermarketAccountPrices
                .AsNoTracking()
                .Where(p => p.SupermarketAccount.CustomerId == customerId)
                .ToDictionaryAsync(p => p.RecipeId, p => p.AgreedPrice, cancellationToken);
        }

        private async Task<List<SalesBonusRuleEntity>> GetActiveBonusRulesAsync(CancellationToken cancellationToken)
        {
            return await _db.SalesBonusRules
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.ThresholdQuantity)
                .ToListAsync(cancellationToken);
        }

        private static int CalculateBonus(
            IEnumerable<SalesBonusRuleEntity> rules,
            int recipeId,
            int quantity)
        {
            var rule = rules
                .Where(r => r.RecipeId == recipeId && r.ThresholdQuantity <= quantity)
                .OrderByDescending(r => r.ThresholdQuantity)
                .FirstOrDefault();

            return rule?.BonusQuantity ?? 0;
        }

        private async Task<string> GenerateMemberCodeAsync(
            CustomerCategory category,
            CancellationToken cancellationToken)
        {
            var prefix = category switch
            {
                CustomerCategory.Supermarket => "SUP",
                CustomerCategory.WalkIn => "WLK",
                CustomerCategory.SalesRep => "REP",
                _ => "CUS"
            };

            for (var attempt = 0; attempt < 20; attempt++)
            {
                var code = $"{prefix}-{RandomCode(6)}";
                if (!await _db.Customers.AnyAsync(c => c.MemberCode == code, cancellationToken))
                {
                    return code;
                }
            }

            throw new InvalidOperationException("Could not generate a unique customer code. Please try again.");
        }

        private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
        {
            var prefix = $"ORD-{DateTime.UtcNow:yyyyMMdd}-";
            var existing = await _db.Orders
                .AsNoTracking()
                .Where(o => o.OrderNumber.StartsWith(prefix))
                .Select(o => o.OrderNumber)
                .ToListAsync(cancellationToken);

            var next = existing
                .Select(n => n[prefix.Length..])
                .Select(n => int.TryParse(n, out var value) ? value : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return $"{prefix}{next:000}";
        }

        private async Task<string> GenerateReturnNumberAsync(CancellationToken cancellationToken)
        {
            var prefix = $"RET-{DateTime.UtcNow:yyyyMMdd}-";
            var existing = await _db.CustomerReturns
                .AsNoTracking()
                .Where(r => r.ReturnNumber.StartsWith(prefix))
                .Select(r => r.ReturnNumber)
                .ToListAsync(cancellationToken);

            var next = existing
                .Select(n => n[prefix.Length..])
                .Select(n => int.TryParse(n, out var value) ? value : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return $"{prefix}{next:000}";
        }

        private async Task<string> GenerateRouteSheetRefAsync(
            DateTime date,
            CancellationToken cancellationToken)
        {
            var prefix = $"RTE-{date:yyyyMMdd}-";
            var existing = await _db.DailyRepRouteSheets
                .AsNoTracking()
                .Where(s => s.RouteSheetRef.StartsWith(prefix))
                .Select(s => s.RouteSheetRef)
                .ToListAsync(cancellationToken);

            var next = existing
                .Select(n => n[prefix.Length..])
                .Select(n => int.TryParse(n, out var value) ? value : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return $"{prefix}{next:000}";
        }

        private static string RandomCode(int length)
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var bytes = RandomNumberGenerator.GetBytes(length);
            var chars = new char[length];
            for (var i = 0; i < length; i++)
            {
                chars[i] = alphabet[bytes[i] % alphabet.Length];
            }
            return new string(chars);
        }

        private static OrderType ParseOrderType(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return OrderType.ImmediateSale;
            }

            if (value.Contains("pre", StringComparison.OrdinalIgnoreCase)
                || value.Contains("advance", StringComparison.OrdinalIgnoreCase)
                || value.Contains("phone", StringComparison.OrdinalIgnoreCase))
            {
                return OrderType.PreOrder;
            }

            return Enum.TryParse<OrderType>(value, true, out var parsed) ? parsed : OrderType.ImmediateSale;
        }

        private static PaymentMethod ParsePaymentMethod(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return PaymentMethod.Cash;
            }

            return value.ToLowerInvariant() switch
            {
                "cash" => PaymentMethod.Cash,
                "banktransfer" or "bank transfer" => PaymentMethod.BankTransfer,
                "posbank" or "pos" or "pos bank" => PaymentMethod.PosBank,
                "creditowing" or "credit" or "credit owing" => PaymentMethod.CreditOwing,
                _ => Enum.TryParse<PaymentMethod>(value, true, out var parsed) ? parsed : PaymentMethod.Cash
            };
        }

        private static CustomerCategory ParseCustomerCategory(string? value)
        {
            return Enum.TryParse<CustomerCategory>(value, true, out var parsed)
                ? parsed
                : CustomerCategory.WalkIn;
        }

        private static OrderPaymentStatus ComputePaymentStatus(decimal total, decimal paid)
        {
            if (paid <= 0)
            {
                return OrderPaymentStatus.Open;
            }

            return paid + 0.005m >= total
                ? OrderPaymentStatus.Completed
                : OrderPaymentStatus.PartiallyPaid;
        }

        private static string NormalizeRequired(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"{fieldName} is required.");
            }

            return value.Trim();
        }

        private static Customer MapCustomer(CustomerEntity customer)
        {
            return new Customer
            {
                Id = customer.Id,
                Name = customer.Name,
                MemberCode = customer.MemberCode,
                Category = (CustomerCategory)customer.Category,
                PhoneNumber = customer.PhoneNumber,
                Address = customer.Address,
                OutstandingBalance = customer.OutstandingBalance,
                IsActive = customer.IsActive
            };
        }

        private static PosTerminal MapTerminal(PosTerminalEntity terminal)
        {
            return new PosTerminal
            {
                Id = terminal.Id,
                TerminalCode = terminal.TerminalCode,
                TerminalName = terminal.TerminalName,
                AssignedEmployeeId = terminal.AssignedEmployeeId,
                AssignedEmployeeName = terminal.AssignedEmployee?.FullName,
                AssignedEmployeeCode = terminal.AssignedEmployee?.StaffCode,
                AssignedUserId = terminal.AssignedUserId,
                AssignedUserName = terminal.AssignedUser?.Name,
                CashAccountName = terminal.CashAccountName,
                TransferAccountName = terminal.TransferAccountName,
                PosBankAccountName = terminal.PosBankAccountName,
                IsActive = terminal.IsActive
            };
        }

        private static SalesRepProfile MapSalesRep(SalesRepEntity rep)
        {
            return new SalesRepProfile
            {
                Id = rep.Id,
                EmployeeId = rep.EmployeeId,
                EmployeeCode = rep.Employee?.StaffCode,
                UserId = rep.UserId,
                FullName = rep.FullName,
                PhoneNumber = rep.PhoneNumber,
                RouteName = rep.RouteName,
                PosTerminalCode = rep.PosTerminal?.TerminalCode ?? string.Empty,
                VehicleRegistration = rep.Vehicle?.RegistrationNumber ?? string.Empty,
                ProductFocus = rep.ProductFocus,
                IsActive = rep.IsActive
            };
        }

        private static Recipe MapRecipe(RecipeEntity recipe)
        {
            return new Recipe
            {
                Id = recipe.Id,
                Name = recipe.Name,
                Code = recipe.Code,
                Description = recipe.Description,
                ProductionProductTypeId = recipe.ProductionProductTypeId,
                ProductTypeName = recipe.ProductionProductType?.Name ?? string.Empty,
                ProductionCategoryId = recipe.ProductionProductType?.ProductionCategoryId,
                CategoryName = recipe.ProductionProductType?.ProductionCategory?.Name ?? string.Empty,
                Baseline = recipe.ProductionProductType?.Baseline ?? string.Empty,
                ProductType = MapProductType(recipe.ProductionProductType),
                BaselineUnit = MapBaselineUnit(recipe.ProductionProductType),
                ExpectedYield = recipe.ExpectedYield,
                OutputUnit = recipe.OutputUnit,
                PackagingCost = 0m,
                LaborAndOverheadPerUnit = 0m,
                SellingPrice = recipe.SellingPrice,
                IsActive = recipe.IsActive
            };
        }

        private static ProductType MapProductType(ProductionProductTypeEntity? productType)
        {
            var value = $"{productType?.Name} {productType?.ProductionCategory?.Name}";
            if (value.Contains("water", StringComparison.OrdinalIgnoreCase)) return ProductType.Water;
            if (value.Contains("popcorn", StringComparison.OrdinalIgnoreCase)
                || value.Contains("snack", StringComparison.OrdinalIgnoreCase)) return ProductType.Popcorn;
            return ProductType.Bread;
        }

        private static BaselineUnit MapBaselineUnit(ProductionProductTypeEntity? productType)
        {
            return MapProductType(productType) switch
            {
                ProductType.Water => BaselineUnit.WaterRun18_9L,
                ProductType.Popcorn => BaselineUnit.PopcornPot,
                _ => BaselineUnit.FlourBag50Kg
            };
        }

        private static Order MapOrder(OrderEntity order)
        {
            var domain = new Order
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                Type = (OrderType)order.OrderType,
                CustomerId = order.CustomerId,
                CustomerName = order.CustomerName,
                CustomerCategory = (CustomerCategory)order.CustomerCategory,
                SalesRepId = order.SalesRepId,
                SalesRepUserId = order.SalesRep?.UserId,
                SalesRepName = order.SalesRepName,
                PosTerminalCode = order.PosTerminalCode,
                BookedByType = (OrderBookedByType)order.BookedByType,
                CreatedByUserId = order.CreatedByUserId,
                Discount = order.Discount,
                AmountPaid = order.AmountPaid,
                PaymentMethod = (PaymentMethod)order.PaymentMethod,
                Status = (OrderStatus)order.Status,
                PaymentStatus = (OrderPaymentStatus)order.PaymentStatus,
                ClosedAt = order.ClosedAt,
                OrderDate = order.OrderDate,
                TargetDeliveryDate = order.TargetDeliveryDate,
                IsPrintedForLoading = order.IsPrintedForLoading,
                PrintedAt = order.PrintedAt,
                VerifiedByLoaderName = order.VerifiedByLoaderName,
                LoadedAt = order.LoadedAt,
                Notes = order.Notes
            };

            domain.Items = order.OrderItems
                .OrderBy(i => i.Id)
                .Select(i => new OrderItem
                {
                    Id = i.Id,
                    OrderId = i.OrderId,
                    RecipeId = i.RecipeId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    BonusQuantity = i.BonusQuantity,
                    UnitPrice = i.UnitPrice
                })
                .ToList();

            domain.Payments = order.OrderPayments
                .OrderBy(p => p.PaymentDate)
                .ThenBy(p => p.Id)
                .Select(MapOrderPayment)
                .ToList();

            return domain;
        }

        private static OrderPayment MapOrderPayment(OrderPaymentEntity payment)
        {
            return new OrderPayment
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                Amount = payment.Amount,
                PaymentMethod = (PaymentMethod)payment.PaymentMethod,
                PaymentDate = payment.PaymentDate,
                Reference = payment.Reference,
                Notes = payment.Notes,
                ReceivedBy = payment.ReceivedBy
            };
        }

        private static SupermarketAccount MapSupermarketAccount(SupermarketAccountEntity account)
        {
            return new SupermarketAccount
            {
                Id = account.Id,
                CustomerId = account.CustomerId,
                AccountName = account.AccountName,
                ContactPerson = account.ContactPerson,
                PhoneNumber = account.PhoneNumber,
                Address = account.Address,
                AssignedRepId = account.AssignedRepId ?? 0,
                AssignedRepName = account.AssignedRep?.FullName ?? string.Empty,
                CurrentOutstandingCredit = account.CurrentOutstandingCredit,
                CreditLimit = account.CreditLimit,
                PaymentTerms = account.PaymentTerms,
                IsActive = account.IsActive,
                CustomAgreedPrices = account.SupermarketAccountPrices
                    .ToDictionary(p => p.RecipeId, p => p.AgreedPrice)
            };
        }

        private static DailyRepRouteSheet MapRouteSheet(RouteSheetEntity sheet)
        {
            return new DailyRepRouteSheet
            {
                Id = sheet.Id,
                RouteSheetRef = sheet.RouteSheetRef,
                Date = sheet.RouteDate.ToDateTime(TimeOnly.MinValue),
                RepId = sheet.SalesRepId,
                RepName = sheet.SalesRep?.FullName ?? string.Empty,
                RepCode = sheet.SalesRep?.RepCode ?? string.Empty,
                RouteName = sheet.RouteName,
                VehicleRegNumber = sheet.VehicleRegistration ?? sheet.Vehicle?.RegistrationNumber ?? string.Empty,
                PosTerminalCode = sheet.PosTerminalCode ?? string.Empty,
                FleetDispatchId = sheet.FleetDispatchId,
                FleetDispatchRef = sheet.FleetDispatch?.DispatchRef ?? string.Empty,
                FleetDispatchStatus = sheet.FleetDispatch == null ? string.Empty : ((FleetDispatchStatus)sheet.FleetDispatch.Status).ToString(),
                FleetDispatchVehicleRegNumber = sheet.FleetDispatch?.Vehicle?.RegistrationNumber ?? sheet.VehicleRegistration ?? string.Empty,
                FleetDispatchDriverName = sheet.FleetDispatch?.DriverName ?? string.Empty,
                FleetDispatchDate = sheet.FleetDispatch == null ? null : sheet.FleetDispatch.DispatchDate.ToDateTime(TimeOnly.MinValue),
                PreviousAccountBalance = sheet.PreviousAccountBalance,
                CashCollected = sheet.CashCollected,
                BankTransferCollected = sheet.BankTransferCollected,
                PosTerminalCollected = sheet.PosTerminalCollected,
                Status = (RouteSheetStatus)sheet.Status,
                SignedOffByDispatcher = sheet.SignedOffByDispatcher,
                SignedOffAt = sheet.SignedOffAt,
                ReconciledBy = sheet.ReconciledBy,
                ReconciledAt = sheet.ReconciledAt,
                Notes = sheet.Notes,
                Supermarkets = sheet.RouteSheetSupermarkets
                    .OrderBy(x => x.SupermarketAccount.AccountName)
                    .Select(x => new RouteSheetSupermarketLink
                    {
                        SupermarketAccountId = x.SupermarketAccountId,
                        AccountName = x.SupermarketAccount.AccountName,
                        ContactPerson = x.SupermarketAccount.ContactPerson,
                        PhoneNumber = x.SupermarketAccount.PhoneNumber,
                        CurrentOutstandingCredit = x.SupermarketAccount.CurrentOutstandingCredit,
                        CreditLimit = x.SupermarketAccount.CreditLimit,
                        IsActive = x.SupermarketAccount.IsActive
                    })
                    .ToList(),
                SupermarketDebts = sheet.RouteSheetSupermarketDebts
                    .OrderBy(d => d.SupermarketAccount.AccountName)
                    .Select(d => new RouteSheetSupermarketDebt
                    {
                        Id = d.Id,
                        RouteSheetId = d.RouteSheetId,
                        SupermarketAccountId = d.SupermarketAccountId,
                        AccountName = d.SupermarketAccount.AccountName,
                        IsSettled = d.IsSettled,
                        DebtAmount = d.DebtAmount,
                        AppliedCredit = d.AppliedCredit,
                        RecordedBy = d.RecordedBy,
                        RecordedAt = d.RecordedAt
                    })
                    .ToList(),
                Items = sheet.RouteSheetItems
                    .OrderBy(i => i.Id)
                    .Select(i => new RouteSKUItem
                    {
                        ProductId = i.RecipeId,
                        ProductCode = i.ProductCode,
                        ProductName = i.ProductName,
                        Category = (ProductType)i.Category,
                        UnitSellingPrice = i.UnitSellingPrice,
                        RepOrderRequestedQty = i.RepOrderRequestedQty,
                        YesterdayClosingStock = i.YesterdayClosingStock,
                        TodayNewlyLoadedQty = i.TodayNewlyLoadedQty,
                        QtySold = i.QtySold,
                        QtyReturned = i.ClosingStockQty,
                        QtyDamaged = i.QtyDamaged,
                        QtyReplacementIssued = i.QtyReplacementIssued
                    })
                    .ToList()
            };
        }

        private static CustomerReturn MapCustomerReturn(CustomerReturnEntity entity)
        {
            return new CustomerReturn
            {
                Id = entity.Id,
                ReturnNumber = entity.ReturnNumber,
                OrderId = entity.OrderId,
                CustomerId = entity.CustomerId,
                RecipeId = entity.RecipeId,
                ProductName = entity.Recipe?.Name ?? string.Empty,
                SalesRepId = entity.SalesRepId,
                Quantity = entity.Quantity,
                ReplacementQuantity = entity.ReplacementQuantity,
                UnitPrice = entity.UnitPrice,
                ReturnDate = entity.ReturnDate,
                Reason = entity.Reason,
                Notes = entity.Notes,
                RecordedBy = entity.RecordedBy
            };
        }

        private static BonusRule MapBonusRule(SalesBonusRuleEntity entity, RecipeEntity? recipe = null)
        {
            return new BonusRule
            {
                Id = entity.Id,
                RecipeId = entity.RecipeId,
                RecipeName = recipe?.Name ?? entity.Recipe?.Name ?? string.Empty,
                RecipeCode = recipe?.Code ?? entity.Recipe?.Code ?? string.Empty,
                ThresholdQuantity = entity.ThresholdQuantity,
                BonusQuantity = entity.BonusQuantity,
                IsActive = entity.IsActive
            };
        }
    }
}
