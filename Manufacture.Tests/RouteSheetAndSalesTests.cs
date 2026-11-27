using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Manufacture.Models.Domain;
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
        private readonly SettingsCache _settingsCache;
        private readonly MockSalesService _salesService;

        public RouteSheetAndSalesTests()
        {
            _userService = new MockUserService();
            _logisticsService = new MockLogisticsService();
            _productionService = new MockProductionService();
            _inventoryService = new MockInventoryService();
            _settingsCache = new SettingsCache();

            _salesService = new MockSalesService(
                _userService,
                _logisticsService,
                _productionService,
                _inventoryService);
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
            var policy = _settingsCache.TreasuryPolicy;
            Assert.NotNull(policy);

            // Act
            _settingsCache.SetTreasuryPolicy(new TreasuryPolicySettings
            {
                PlannedCashPercent = 35.0m,
                AlertTolerancePercent = 8.0m,
                PolicyMemo = "Custom treasury benchmark policy",
                LastUpdatedUtc = DateTime.UtcNow,
                UpdatedBy = "Tester"
            });

            // Assert
            var updated = _settingsCache.TreasuryPolicy;
            Assert.Equal(35.0m, updated.PlannedCashPercent);
            Assert.Equal(8.0m, updated.AlertTolerancePercent);
            Assert.Equal(0.35m, _settingsCache.PlannedCashShare);
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
