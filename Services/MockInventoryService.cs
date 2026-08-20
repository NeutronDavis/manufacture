using Manufacture.Models.Entities;
using Manufacture.Models.DTOs;

namespace Manufacture.Services
{
    public class MockInventoryService
    {
        private readonly List<Supplier> _suppliers = new();
        private readonly List<InventoryItem> _items = new();
        private readonly List<StoreRequest> _storeRequests = new();

        public MockInventoryService()
        {
            SeedInitialData();
        }

        private void SeedInitialData()
        {
            // Suppliers
            _suppliers.AddRange(new[]
            {
                new Supplier { Id = 1, Name = "Flour Mills of Nigeria PLC (Golden Penny)", ContactPerson = "Alhaji Shehu Garba", PhoneNumber = "+2348039988771", Email = "orders@fmnplc.com", SupplyCategory = "Flour & Grains", Address = "Apapa Port Complex, Lagos" },
                new Supplier { Id = 2, Name = "Dangote Sugar Refinery", ContactPerson = "Mrs. Bisi Adeleke", PhoneNumber = "+2348028877662", Email = "sales@dangotesugar.com", SupplyCategory = "Sweeteners & Yeast", Address = "Shed 20, NPA Apapa, Lagos" },
                new Supplier { Id = 3, Name = "FlexiPack Ltd (Bakery Packaging)", ContactPerson = "Mr. Kenneth Okon", PhoneNumber = "+2348076655443", Email = "packaging@flexipack.ng", SupplyCategory = "Packaging (Nylon & Tags)", Address = "Industrial Estate, Ikeja, Lagos" },
                new Supplier { Id = 4, Name = "Lesaffre / Saf-Instant Yeast West Africa", ContactPerson = "David Mensah", PhoneNumber = "+2348091122334", Email = "sales@lesaffre.com", SupplyCategory = "Sweeteners & Yeast", Address = "Oregun Industrial Area, Lagos" }
            });

            // Inventory Items
            _items.AddRange(new[]
            {
                new InventoryItem { Id = 1, ItemCode = "RAW-FLR-01", Name = "Golden Penny Wheat Flour", Category = "Raw Materials", Unit = "Bags (50kg)", QuantityInStock = 45, ReorderThreshold = 20, UnitCostPrice = 55000, PreferredSupplierId = 1, PreferredSupplierName = "Flour Mills of Nigeria PLC" },
                new InventoryItem { Id = 2, ItemCode = "RAW-SGR-01", Name = "Dangote Refined Sugar", Category = "Raw Materials", Unit = "Bags (50kg)", QuantityInStock = 12, ReorderThreshold = 10, UnitCostPrice = 72500, PreferredSupplierId = 2, PreferredSupplierName = "Dangote Sugar Refinery" },
                new InventoryItem { Id = 3, ItemCode = "RAW-YST-01", Name = "Saf-Instant Dry Yeast (Carton)", Category = "Raw Materials", Unit = "Cartons (10kg)", QuantityInStock = 6, ReorderThreshold = 8, UnitCostPrice = 38000, PreferredSupplierId = 4, PreferredSupplierName = "Lesaffre / Saf-Instant Yeast" },
                new InventoryItem { Id = 4, ItemCode = "RAW-FAT-01", Name = "Golden Margarine / Shortening", Category = "Raw Materials", Unit = "Buckets (15kg)", QuantityInStock = 18, ReorderThreshold = 8, UnitCostPrice = 33000, PreferredSupplierId = 1, PreferredSupplierName = "Flour Mills of Nigeria PLC" },
                new InventoryItem { Id = 5, ItemCode = "PKG-NYL-JMB", Name = "Jumbo Bread Printed Nylon Wrappers", Category = "Packaging", Unit = "Bundles (1,000 pcs)", QuantityInStock = 4, ReorderThreshold = 10, UnitCostPrice = 65000, PreferredSupplierId = 3, PreferredSupplierName = "FlexiPack Ltd" },
                new InventoryItem { Id = 6, ItemCode = "PKG-NYL-MED", Name = "Medium Bread Printed Nylon Wrappers", Category = "Packaging", Unit = "Bundles (1,000 pcs)", QuantityInStock = 15, ReorderThreshold = 10, UnitCostPrice = 45000, PreferredSupplierId = 3, PreferredSupplierName = "FlexiPack Ltd" },
                new InventoryItem { Id = 7, ItemCode = "RAW-SLT-01", Name = "Dangote Pure Iodized Salt", Category = "Raw Materials", Unit = "Bags (25kg)", QuantityInStock = 8, ReorderThreshold = 5, UnitCostPrice = 10000, PreferredSupplierId = 2, PreferredSupplierName = "Dangote Sugar Refinery" }
            });

            // Store Requests
            _storeRequests.AddRange(new[]
            {
                new StoreRequest
                {
                    Id = 1,
                    RequestNumber = "REQ-20260819-01",
                    RequestedBy = "Chidinma Okoro (Production Mgr)",
                    Department = "Bakery Production Floor",
                    Purpose = "Morning Shift Batch #101 Mixing (400 Jumbo Loaves)",
                    Status = StoreRequestStatus.Issued,
                    RequestedAt = DateTime.UtcNow.AddHours(-4),
                    ApprovedBy = "Musa Ibrahim (Store Mgr)",
                    ApprovedAt = DateTime.UtcNow.AddHours(-3),
                    Items = new List<StoreRequestItem>
                    {
                        new() { Id = 1, StoreRequestId = 1, InventoryItemId = 1, ItemName = "Golden Penny Wheat Flour", Unit = "Bags (50kg)", RequestedQuantity = 4, IssuedQuantity = 4 },
                        new() { Id = 2, StoreRequestId = 1, InventoryItemId = 2, ItemName = "Dangote Refined Sugar", Unit = "Bags (50kg)", RequestedQuantity = 1, IssuedQuantity = 1 },
                        new() { Id = 3, StoreRequestId = 1, InventoryItemId = 5, ItemName = "Jumbo Bread Printed Nylon Wrappers", Unit = "Bundles (1,000 pcs)", RequestedQuantity = 1, IssuedQuantity = 1 }
                    }
                },
                new StoreRequest
                {
                    Id = 2,
                    RequestNumber = "REQ-20260819-02",
                    RequestedBy = "Chidinma Okoro (Production Mgr)",
                    Department = "Bakery Production Floor",
                    Purpose = "Afternoon Shift Batch #102 Mixing",
                    Status = StoreRequestStatus.Pending,
                    RequestedAt = DateTime.UtcNow.AddMinutes(-45),
                    Items = new List<StoreRequestItem>
                    {
                        new() { Id = 4, StoreRequestId = 2, InventoryItemId = 1, ItemName = "Golden Penny Wheat Flour", Unit = "Bags (50kg)", RequestedQuantity = 3, IssuedQuantity = 0 },
                        new() { Id = 5, StoreRequestId = 2, InventoryItemId = 3, ItemName = "Saf-Instant Dry Yeast (Carton)", Unit = "Cartons (10kg)", RequestedQuantity = 1, IssuedQuantity = 0 }
                    }
                }
            });
        }

        // Suppliers
        public List<Supplier> GetAllSuppliers() => _suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToList();
        public Supplier? GetSupplierById(int id) => _suppliers.FirstOrDefault(s => s.Id == id);
        public Supplier CreateSupplier(SupplierDto dto)
        {
            var sup = new Supplier
            {
                Id = _suppliers.Any() ? _suppliers.Max(s => s.Id) + 1 : 1,
                Name = dto.Name,
                ContactPerson = dto.ContactPerson,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email,
                SupplyCategory = dto.SupplyCategory,
                Address = dto.Address,
                IsActive = true
            };
            _suppliers.Add(sup);
            return sup;
        }

        // Inventory Items
        public List<InventoryItem> GetAllItems() => _items.OrderBy(i => i.Name).ToList();
        public List<InventoryItem> GetLowStockItems() => _items.Where(i => i.IsLowStock).ToList();
        public InventoryItem? GetItemById(int id) => _items.FirstOrDefault(i => i.Id == id);
        
        public InventoryItem CreateItem(InventoryItemDto dto)
        {
            var supplier = dto.PreferredSupplierId.HasValue ? GetSupplierById(dto.PreferredSupplierId.Value) : null;
            var item = new InventoryItem
            {
                Id = _items.Any() ? _items.Max(i => i.Id) + 1 : 1,
                ItemCode = string.IsNullOrWhiteSpace(dto.ItemCode) ? $"RAW-{_items.Count + 101}" : dto.ItemCode,
                Name = dto.Name,
                Category = dto.Category,
                Unit = dto.Unit,
                QuantityInStock = dto.QuantityInStock,
                ReorderThreshold = dto.ReorderThreshold,
                UnitCostPrice = dto.UnitCostPrice,
                PreferredSupplierId = dto.PreferredSupplierId,
                PreferredSupplierName = supplier?.Name,
                LastRestockedDate = DateTime.UtcNow
            };
            _items.Add(item);
            return item;
        }

        public bool RestockItem(int itemId, decimal addedQuantity)
        {
            var item = GetItemById(itemId);
            if (item == null) return false;
            item.QuantityInStock += addedQuantity;
            item.LastRestockedDate = DateTime.UtcNow;
            return true;
        }

        // Store Requests
        public List<StoreRequest> GetAllRequests() => _storeRequests.OrderByDescending(r => r.Id).ToList();
        public StoreRequest? GetRequestById(int id) => _storeRequests.FirstOrDefault(r => r.Id == id);

        public StoreRequest CreateRequest(StoreRequestDto dto)
        {
            var req = new StoreRequest
            {
                Id = _storeRequests.Any() ? _storeRequests.Max(r => r.Id) + 1 : 1,
                RequestNumber = $"REQ-{DateTime.UtcNow:yyyyMMdd}-{_storeRequests.Count + 1:00}",
                RequestedBy = dto.RequestedBy,
                Department = dto.Department,
                Purpose = dto.Purpose,
                Status = StoreRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                Items = dto.Items.Select((item, idx) => new StoreRequestItem
                {
                    Id = idx + 1,
                    InventoryItemId = item.InventoryItemId,
                    ItemName = item.ItemName,
                    Unit = item.Unit,
                    RequestedQuantity = item.RequestedQuantity,
                    IssuedQuantity = 0
                }).ToList()
            };
            _storeRequests.Add(req);
            return req;
        }

        public bool ApproveAndIssueRequest(int requestId, string approverName)
        {
            var req = GetRequestById(requestId);
            if (req == null) return false;

            req.Status = StoreRequestStatus.Issued;
            req.ApprovedBy = approverName;
            req.ApprovedAt = DateTime.UtcNow;

            // Deduct stock from inventory
            foreach (var item in req.Items)
            {
                item.IssuedQuantity = item.RequestedQuantity;
                var invItem = GetItemById(item.InventoryItemId);
                if (invItem != null)
                {
                    invItem.QuantityInStock = Math.Max(0, invItem.QuantityInStock - item.IssuedQuantity);
                }
            }
            return true;
        }

        public bool RejectRequest(int requestId, string approverName, string reason)
        {
            var req = GetRequestById(requestId);
            if (req == null) return false;
            req.Status = StoreRequestStatus.Rejected;
            req.ApprovedBy = approverName;
            req.ApprovedAt = DateTime.UtcNow;
            req.RejectionReason = reason;
            return true;
        }
    }
}
