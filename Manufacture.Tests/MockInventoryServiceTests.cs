using Xunit;
using Manufacture.Services;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Tests
{
    public class MockInventoryServiceTests
    {
        private readonly MockInventoryService _service;

        public MockInventoryServiceTests()
        {
            _service = new MockInventoryService();
        }

        [Fact]
        public void GetAllItems_ReturnsSeededItems()
        {
            var items = _service.GetAllItems();
            Assert.NotNull(items);
            Assert.True(items.Count >= 17);
        }

        [Fact]
        public void GetItemById_WithValidId_ReturnsItem()
        {
            var item = _service.GetItemById(1);
            Assert.NotNull(item);
            Assert.Equal("RAW-FLR-01", item.ItemCode);
            Assert.Equal("Golden Penny Wheat Flour", item.Name);
            Assert.Equal("Bags (50kg)", item.Unit);
            Assert.Equal(55000m, item.UnitCostPrice);
        }

        [Fact]
        public void UpdateItem_ModifiesItemPropertiesSuccessfully()
        {
            var dto = new InventoryItemDto
            {
                ItemCode = "RAW-FLR-01-MOD",
                Name = "Golden Penny Wheat Flour Premium",
                Category = "Raw Materials",
                Unit = "Bags (50kg)",
                QuantityInStock = 50,
                ReorderThreshold = 15,
                UnitCostPrice = 56000,
                PreferredSupplierId = 1
            };

            var updated = _service.UpdateItem(1, dto);

            Assert.NotNull(updated);
            Assert.Equal("RAW-FLR-01-MOD", updated.ItemCode);
            Assert.Equal("Golden Penny Wheat Flour Premium", updated.Name);
            Assert.Equal(50m, updated.QuantityInStock);
            Assert.Equal(56000m, updated.UnitCostPrice);
            Assert.Equal("Flour Mills of Nigeria PLC (Golden Penny)", updated.PreferredSupplierName);
        }

        [Fact]
        public void RestockItem_IncreasesStockQuantity()
        {
            var initial = _service.GetItemById(1)!.QuantityInStock;
            var success = _service.RestockItem(1, 20);

            Assert.True(success);
            var updated = _service.GetItemById(1)!;
            Assert.Equal(initial + 20, updated.QuantityInStock);
        }

        [Fact]
        public void GetRequestsByItemId_ReturnsAssociatedRequisitions()
        {
            var requests = _service.GetRequestsByItemId(1);
            Assert.NotEmpty(requests);
            Assert.All(requests, r => Assert.Contains(r.Items, i => i.InventoryItemId == 1));
        }

        [Fact]
        public void GetAllSuppliers_ReturnsConfiguredSuppliers()
        {
            var suppliers = _service.GetAllSuppliers();
            Assert.NotEmpty(suppliers);
            Assert.Contains(suppliers, s => s.Name.Contains("Flour Mills"));
            Assert.Contains(suppliers, s => s.Name.Contains("Dangote"));
        }
    }
}
