using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Manufacture.Models.Entities;
using Manufacture.Models.DTOs;
using Manufacture.Services;

namespace Manufacture.Tests
{
    public class RouteSheetAndSalesTests
    {
        private readonly MockUserService _userService;
        private readonly MockLogisticsService _logisticsService;
        private readonly MockProductionService _productionService;
        private readonly MockInventoryService _inventoryService;
        private readonly MockSettingsService _settingsService;
        private readonly MockSalesService _salesService;

        public RouteSheetAndSalesTests()
        {
            _userService = new MockUserService();
            _logisticsService = new MockLogisticsService();
            _productionService = new MockProductionService();
            _inventoryService = new MockInventoryService();
            _settingsService = new MockSettingsService();

            _salesService = new MockSalesService(
                _userService,
                _logisticsService,
                _productionService,
                _inventoryService,
                _settingsService);
        }

        [Fact]
        public void CalculateRouteInventory_CorrectlySumsYesterdayAndTodayStock()
        {
            // Arrange
            var item = new RouteSKUItem
            {
                ProductId = 1,
                ProductName = "Jumbo Bread",
                UnitSellingPrice = 1400m,
                YesterdayClosingStock = 15,
                TodayNewlyLoadedQty = 85,
                QtySold = 80,
                QtyReturned = 18,
                QtyDamaged = 2
            };

            // Act
            var (active, isBalanced) = _salesService.CalculateRouteInventory(item);

            // Assert
            Assert.Equal(100, active);
            Assert.Equal(100, item.TotalActiveInventory);
            Assert.True(isBalanced);
            Assert.True(item.IsCountBalanced);
        }

        [Fact]
        public void CalculateRouteInventory_DetectsStockDiscrepancyWhenUnbalanced()
        {
            // Arrange: Active = 10 + 90 = 100, but account says Sold=70, Returned=20, Damaged=5 (Total=95 -> 5 missing)
            var item = new RouteSKUItem
            {
                ProductId = 1,
                YesterdayClosingStock = 10,
                TodayNewlyLoadedQty = 90,
                QtySold = 70,
                QtyReturned = 20,
                QtyDamaged = 5
            };

            // Act
            var (active, isBalanced) = _salesService.CalculateRouteInventory(item);

            // Assert
            Assert.Equal(100, active);
            Assert.False(isBalanced);
            Assert.False(item.IsCountBalanced);
        }

        [Fact]
        public void RouteSheetCreation_SeparatesOrderingFromPhysicalLoading()
        {
            // Arrange
            var today = DateTime.UtcNow.Date.AddDays(10);
            var repId = 1;
            var requested = new Dictionary<int, int> { { 1, 100 }, { 2, 50 } };

            // Act
            var sheet = _salesService.CreateRouteSheet(repId, today, requested);

            // Assert
            Assert.NotNull(sheet);
            Assert.Equal(RouteSheetStatus.Ordered, sheet.Status);
            Assert.Null(sheet.SignedOffByDispatcher);

            var item1 = sheet.Items.FirstOrDefault(i => i.ProductId == 1);
            Assert.NotNull(item1);
            Assert.Equal(100, item1.RepOrderRequestedQty);
            // Before bay sign-off, physically loaded quantity is 0
            Assert.Equal(0, item1.TodayNewlyLoadedQty);
        }

        [Fact]
        public void SignOffDispatch_UpdatesLoadedQuantitiesAndChangesStatusToLoadedOnRoute()
        {
            // Arrange
            var date = DateTime.UtcNow.Date.AddDays(12);
            var sheet = _salesService.CreateRouteSheet(1, date);
            var loaded = new Dictionary<int, int>
            {
                { 1, 95 },
                { 2, 48 }
            };

            // Act
            var ok = _salesService.SignOffDispatch(sheet.Id, loaded, "Oluwaseun Bay Supervisor", "Gate release approved");

            // Assert
            Assert.True(ok);
            Assert.Equal(RouteSheetStatus.LoadedOnRoute, sheet.Status);
            Assert.Equal("Oluwaseun Bay Supervisor", sheet.SignedOffByDispatcher);
            Assert.NotNull(sheet.SignedOffAt);

            var item1 = sheet.Items.First(i => i.ProductId == 1);
            Assert.Equal(95, item1.TodayNewlyLoadedQty);
        }

        [Fact]
        public void ReconcileDailyRepAccount_CalculatesExpectedRevenueAndCarriesOverBalance()
        {
            // Arrange
            var date = DateTime.UtcNow.Date.AddDays(15);
            var sheet = _salesService.CreateRouteSheet(1, date);
            sheet.PreviousAccountBalance = 20000m;

            var loaded = new Dictionary<int, int> { { 1, 50 } };
            _salesService.SignOffDispatch(sheet.Id, loaded, "Dispatcher", "OK");

            // SKU 1 unit price is ₦1,400.
            // Sold = 45 -> Expected Revenue = 45 * 1400 = ₦63,000.
            // Returned = 4, Damaged = 1 -> Total accounted for = 50 (balanced).
            var input = new ReconciliationInputDto
            {
                RouteSheetId = sheet.Id,
                CashierName = "Adekunle Johnson",
                SoldQuantities = new Dictionary<int, int> { { 1, 45 } },
                ReturnedQuantities = new Dictionary<int, int> { { 1, 4 } },
                DamagedQuantities = new Dictionary<int, int> { { 1, 1 } },
                CashCollected = 40000m,
                BankTransferCollected = 20000m,
                PosTerminalCollected = 3000m, // Total Collections = ₦63,000
                Notes = "Evening audit clean."
            };

            // Act
            var ok = _salesService.ReconcileDailyRepAccount(input);

            // Assert
            Assert.True(ok);
            Assert.Equal(RouteSheetStatus.Reconciled, sheet.Status);
            Assert.Equal(63000m, sheet.TotalExpectedSalesRevenue);
            Assert.Equal(63000m, sheet.TotalPaymentsCollectedToday);

            // New Balance = Previous (20,000) + Expected (63,000) - Paid (63,000) = 20,000
            Assert.Equal(20000m, sheet.NewOutstandingAccountBalance);
        }

        [Fact]
        public void ReconcileDailyRepAccount_RollsOverUnsoldStockToNextDaySheet()
        {
            // Arrange
            var day1 = DateTime.UtcNow.Date.AddDays(20);
            var day2 = DateTime.UtcNow.Date.AddDays(21);

            var sheet1 = _salesService.CreateRouteSheet(2, day1);
            var sheet2 = _salesService.CreateRouteSheet(2, day2);

            _salesService.SignOffDispatch(sheet1.Id, new Dictionary<int, int> { { 1, 100 } }, "Dispatcher", "Day 1 load");

            // Reconcile Day 1 with 12 unsold loaves returned
            var input = new ReconciliationInputDto
            {
                RouteSheetId = sheet1.Id,
                CashierName = "Cashier",
                SoldQuantities = new Dictionary<int, int> { { 1, 88 } },
                ReturnedQuantities = new Dictionary<int, int> { { 1, 12 } },
                DamagedQuantities = new Dictionary<int, int> { { 1, 0 } },
                CashCollected = 88 * 1400m
            };

            // Act
            _salesService.ReconcileDailyRepAccount(input);

            // Assert: Day 2 sheet SKU 1 should now reflect yesterday's closing stock of 12
            var day2Item1 = sheet2.Items.First(i => i.ProductId == 1);
            Assert.Equal(12, day2Item1.YesterdayClosingStock);
        }

        [Fact]
        public void SupermarketOrderFulfillment_AppliesAgreedCustomPrices()
        {
            // Arrange
            // Supermarket 1 (Goodness Supermarket Ikeja) has custom price ₦1,320 for SKU 1 (Master is ₦1,400)
            var account = _salesService.GetSupermarketAccountById(1);
            Assert.NotNull(account);
            decimal startingCredit = account.CurrentOutstandingCredit;

            var items = new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    RecipeId = 1,
                    ProductName = "Jumbo Bread",
                    Quantity = 50,
                    UnitPrice = 1400m // Catalog master price
                }
            };

            // Act
            var ok = _salesService.FulfillSupermarketOrder(account.Id, 1, items, "Test direct replenishment");

            // Assert
            Assert.True(ok);
            // Total should be charged at 50 * ₦1,320 = ₦66,000 (NOT 50 * ₦1,400 = ₦70,000)
            decimal expectedAddition = 50 * 1320m;
            Assert.Equal(startingCredit + expectedAddition, account.CurrentOutstandingCredit);
        }

        [Fact]
        public void SupermarketOrderFulfillment_FallsBackToCatalogPriceIfNoCustomPriceSet()
        {
            // Arrange
            var account = _salesService.GetSupermarketAccountById(1);
            Assert.NotNull(account);
            // Ensure SKU 3 has no custom agreed price in Goodness Supermarket
            account.CustomAgreedPrices.Remove(3);
            decimal initialBalance = account.CurrentOutstandingCredit;

            var items = new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    RecipeId = 3,
                    ProductName = "Special 61 Butter Bread",
                    Quantity = 20,
                    UnitPrice = 1100m
                }
            };

            // Act
            var ok = _salesService.FulfillSupermarketOrder(account.Id, 1, items, "Standard pricing delivery");

            // Assert
            Assert.True(ok);
            decimal expectedCharge = 20 * 1100m;
            Assert.Equal(initialBalance + expectedCharge, account.CurrentOutstandingCredit);
        }

        [Fact]
        public void SupermarketPayment_ReducesOutstandingCredit()
        {
            // Arrange
            var account = _salesService.GetSupermarketAccountById(2); // Grand Square VI
            Assert.NotNull(account);
            account.CurrentOutstandingCredit = 150000m;

            // Act: Pay ₦100,000
            var ok = _salesService.RecordSupermarketPayment(account.Id, 100000m, "BankTransfer", "TRF-TEST-8812");

            // Assert
            Assert.True(ok);
            Assert.Equal(50000m, account.CurrentOutstandingCredit);
        }

        [Fact]
        public void SupermarketAccountEdit_UpdatesDetailsAndSyncsCustomer()
        {
            // Arrange
            var account = _salesService.GetSupermarketAccountById(1);
            Assert.NotNull(account);
            int origCustId = account.CustomerId;

            var editModel = new SupermarketAccount
            {
                Id = account.Id,
                CustomerId = account.CustomerId,
                AccountName = "Goodness Mega Store (Updated)",
                ContactPerson = "Chief Emeka Okafor",
                PhoneNumber = "+234 803 999 8888",
                Address = "Plot 99 Commercial Avenue, Ikeja",
                CreditLimit = 600000m,
                PaymentTerms = "Net 30 Days",
                AssignedRepId = 2,
                IsActive = true
            };

            // Act
            var ok = _salesService.SaveSupermarketAccount(editModel);

            // Assert
            Assert.True(ok);
            var updated = _salesService.GetSupermarketAccountById(1);
            Assert.NotNull(updated);
            Assert.Equal("Goodness Mega Store (Updated)", updated.AccountName);
            Assert.Equal("Chief Emeka Okafor", updated.ContactPerson);
            Assert.Equal("+234 803 999 8888", updated.PhoneNumber);
            Assert.Equal("Plot 99 Commercial Avenue, Ikeja", updated.Address);
            Assert.Equal(600000m, updated.CreditLimit);
            Assert.Equal("Net 30 Days", updated.PaymentTerms);
            Assert.Equal(2, updated.AssignedRepId);

            // Verify linked customer ledger was synced
            var customer = _salesService.GetCustomerById(origCustId);
            Assert.NotNull(customer);
            Assert.Equal("Goodness Mega Store (Updated)", customer.Name);
            Assert.Equal("+234 803 999 8888", customer.PhoneNumber);
            Assert.Equal("Plot 99 Commercial Avenue, Ikeja", customer.Address);
        }

        [Fact]
        public void SupermarketAccount_ToggleStatus_SuspendsAndReactivates()
        {
            // Arrange
            var account = _salesService.GetSupermarketAccountById(1);
            Assert.NotNull(account);
            bool originalStatus = account.IsActive;

            // Act: Toggle
            var ok = _salesService.ToggleSupermarketStatus(account.Id);

            // Assert
            Assert.True(ok);
            var afterToggle = _salesService.GetSupermarketAccountById(1);
            Assert.NotNull(afterToggle);
            Assert.Equal(!originalStatus, afterToggle.IsActive);

            var customer = _salesService.GetCustomerById(afterToggle.CustomerId);
            Assert.NotNull(customer);
            Assert.Equal(!originalStatus, customer.IsActive);

            // Re-toggle back
            _salesService.ToggleSupermarketStatus(account.Id);
            Assert.Equal(originalStatus, _salesService.GetSupermarketAccountById(1)!.IsActive);
        }

        [Fact]
        public void TreasuryPolicySettings_ConfiguresAllowableVariance()
        {
            // Arrange
            var policy = _settingsService.GetTreasuryPolicy();
            Assert.NotNull(policy);

            // Act
            _settingsService.UpdateTreasuryPolicy(35.0m, 8.0m, "Custom treasury benchmark policy", "Tester");

            // Assert
            var updated = _settingsService.GetTreasuryPolicy();
            Assert.Equal(35.0m, updated.PlannedCashPercent);
            Assert.Equal(8.0m, updated.AlertTolerancePercent);
            Assert.Equal(0.35m, _settingsService.PlannedCashShare);
        }

        [Fact]
        public void CreateTerminal_AddsNewTerminalWithAccountMapping()
        {
            // Arrange
            var newTerminal = new PosTerminal
            {
                TerminalCode = "POS-99",
                TerminalName = "Festac Express Till",
                AssignedUserId = 4,
                AssignedUserName = "Babatunde Alabi",
                CashAccountName = "Festac Cash Till",
                TransferAccountName = "Zenith Bank PLC - 1012938475",
                PosBankAccountName = "OPay Merchant POS - 8102938",
                IsActive = true
            };

            // Act
            var created = _salesService.CreateTerminal(newTerminal);

            // Assert
            Assert.NotNull(created);
            Assert.True(created.Id > 0);
            Assert.Equal("POS-99", created.TerminalCode);
            var fetched = _salesService.GetTerminalById(created.Id);
            Assert.NotNull(fetched);
            Assert.Equal("Festac Express Till", fetched.TerminalName);
            Assert.Equal("Zenith Bank PLC - 1012938475", fetched.TransferAccountName);
        }

        [Fact]
        public void UpdateTerminal_ModifiesBankAccountsAndStatus()
        {
            // Arrange: POS-01
            var term = _salesService.GetTerminalById(1);
            Assert.NotNull(term);

            var updatedModel = new PosTerminal
            {
                Id = 1,
                TerminalCode = "POS-01-RENAMED",
                TerminalName = "Front Retail Register Premium",
                AssignedUserId = 1,
                AssignedUserName = "Adekunle Johnson",
                CashAccountName = "Updated Main Cash Drawer",
                TransferAccountName = "Guaranty Trust Bank - 0129847291",
                PosBankAccountName = "Moniepoint Upgraded - 9912048",
                IsActive = false // Setting offline
            };

            // Act
            var ok = _salesService.UpdateTerminal(updatedModel);

            // Assert
            Assert.True(ok);
            var modified = _salesService.GetTerminalById(1);
            Assert.NotNull(modified);
            Assert.Equal("POS-01-RENAMED", modified.TerminalCode);
            Assert.Equal("Front Retail Register Premium", modified.TerminalName);
            Assert.Equal("Updated Main Cash Drawer", modified.CashAccountName);
            Assert.Equal("Guaranty Trust Bank - 0129847291", modified.TransferAccountName);
            Assert.Equal("Moniepoint Upgraded - 9912048", modified.PosBankAccountName);
            Assert.False(modified.IsActive);
        }

        [Fact]
        public void ToggleTerminalStatus_InvertsActiveState()
        {
            // Arrange: POS-02 is initially active
            var term = _salesService.GetTerminalById(2);
            Assert.NotNull(term);
            bool originalState = term.IsActive;

            // Act 1: Toggle
            var ok1 = _salesService.ToggleTerminalStatus(2);
            Assert.True(ok1);
            Assert.Equal(!originalState, term.IsActive);

            // Act 2: Toggle back
            var ok2 = _salesService.ToggleTerminalStatus(2);
            Assert.True(ok2);
            Assert.Equal(originalState, term.IsActive);
        }
    }
}
