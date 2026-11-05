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
                new Supplier { Id = 2, Name = "Dangote Sugar Refinery", ContactPerson = "Mrs. Bisi Adeleke", PhoneNumber = "+2348028877662", Email = "sales@dangotesugar.com", SupplyCategory = "Sweeteners & Salt", Address = "Shed 20, NPA Apapa, Lagos" },
                new Supplier { Id = 3, Name = "FlexiPack Ltd (Bakery Packaging)", ContactPerson = "Mr. Kenneth Okon", PhoneNumber = "+2348076655443", Email = "packaging@flexipack.ng", SupplyCategory = "Packaging (Nylon & Tags)", Address = "Industrial Estate, Ikeja, Lagos" },
                new Supplier { Id = 4, Name = "Lesaffre / Saf-Instant Yeast West Africa", ContactPerson = "David Mensah", PhoneNumber = "+2348091122334", Email = "sales@lesaffre.com", SupplyCategory = "Yeast & Additives", Address = "Oregun Industrial Area, Lagos" },
                new Supplier { Id = 5, Name = "AquaPure Industrial Systems", ContactPerson = "Engr. Patrick Chukwu", PhoneNumber = "+2348023456789", Email = "support@aquapure.ng", SupplyCategory = "Water Treatment & Bottles", Address = "Plot 14 Ogba Industrial Scheme, Lagos" },
                new Supplier { Id = 6, Name = "SeedCo Grains & Agro Allied", ContactPerson = "Malam Bello Zaria", PhoneNumber = "+2348035678901", Email = "sales@seedcoagro.ng", SupplyCategory = "Corn & Grains", Address = "Grain Terminal, Dawanau / Iddo, Lagos" },
                new Supplier { Id = 7, Name = "Presco PLC / Grand Cereals", ContactPerson = "Mrs. Folake Adebayo", PhoneNumber = "+2348041234567", Email = "oilsupply@prescoplc.ng", SupplyCategory = "Vegetable Oils & Fats", Address = "Commercial Avenue, Yaba, Lagos" },
                new Supplier { Id = 8, Name = "TotalEnergies Nigeria PLC", ContactPerson = "Chinedu Eze", PhoneNumber = "+2348099881122", Email = "fleet.orders@totalenergies.ng", SupplyCategory = "Fuel & Lubricants", Address = "Marina, Lagos Island, Lagos" }
            });

            // Correlated Inventory Items across Bread, Water, Popcorn, and Fleet Utility
            _items.AddRange(new[]
            {
                // ---- Raw Materials (Flour, Sugar, Yeast, Shortening, Salt, Corn, Oil, Water) ----
                new InventoryItem { Id = 1,  ItemCode = "RAW-FLR-01", Name = "Golden Penny Wheat Flour",           Category = "Raw Materials", Unit = "Bags (50kg)",       QuantityInStock = 45,   ReorderThreshold = 20,  UnitCostPrice = 55000, PreferredSupplierId = 1, PreferredSupplierName = "Flour Mills of Nigeria PLC (Golden Penny)" },
                new InventoryItem { Id = 2,  ItemCode = "RAW-SGR-01", Name = "Dangote Refined Sugar",              Category = "Raw Materials", Unit = "Bags (50kg)",       QuantityInStock = 12,   ReorderThreshold = 10,  UnitCostPrice = 72500, PreferredSupplierId = 2, PreferredSupplierName = "Dangote Sugar Refinery" },
                new InventoryItem { Id = 3,  ItemCode = "RAW-YST-01", Name = "Saf-Instant Dry Yeast (Carton)",     Category = "Raw Materials", Unit = "Cartons (10kg)",    QuantityInStock = 6,    ReorderThreshold = 8,   UnitCostPrice = 38000, PreferredSupplierId = 4, PreferredSupplierName = "Lesaffre / Saf-Instant Yeast West Africa" },
                new InventoryItem { Id = 4,  ItemCode = "RAW-FAT-01", Name = "Golden Margarine / Shortening",      Category = "Raw Materials", Unit = "Buckets (15kg)",    QuantityInStock = 18,   ReorderThreshold = 8,   UnitCostPrice = 33000, PreferredSupplierId = 1, PreferredSupplierName = "Flour Mills of Nigeria PLC (Golden Penny)" },
                new InventoryItem { Id = 5,  ItemCode = "RAW-SLT-01", Name = "Dangote Pure Iodized Salt",          Category = "Raw Materials", Unit = "Bags (25kg)",       QuantityInStock = 8,    ReorderThreshold = 5,   UnitCostPrice = 10000, PreferredSupplierId = 2, PreferredSupplierName = "Dangote Sugar Refinery" },
                new InventoryItem { Id = 6,  ItemCode = "RAW-WTR-01", Name = "Purified Process Water",             Category = "Raw Materials", Unit = "Litres",            QuantityInStock = 5000, ReorderThreshold = 1500, UnitCostPrice = 10,    PreferredSupplierId = 5, PreferredSupplierName = "AquaPure Industrial Systems" },
                new InventoryItem { Id = 7,  ItemCode = "RAW-CRN-01", Name = "Popcorn Maize (Mota)",               Category = "Raw Materials", Unit = "Bags (50kg)",       QuantityInStock = 10,   ReorderThreshold = 4,   UnitCostPrice = 82500, PreferredSupplierId = 6, PreferredSupplierName = "SeedCo Grains & Agro Allied" },
                new InventoryItem { Id = 8,  ItemCode = "RAW-OIL-01", Name = "Vegetable Frying Oil",               Category = "Raw Materials", Unit = "Jerrycans (25L)",   QuantityInStock = 8,    ReorderThreshold = 4,   UnitCostPrice = 60000, PreferredSupplierId = 7, PreferredSupplierName = "Presco PLC / Grand Cereals" },

                // ---- Packaging Materials ----
                new InventoryItem { Id = 9,  ItemCode = "PKG-NYL-JMB", Name = "Jumbo Bread Printed Nylon Wrappers", Category = "Packaging",     Unit = "Bundles (1,000 pcs)", QuantityInStock = 25,   ReorderThreshold = 10,  UnitCostPrice = 12000, PreferredSupplierId = 3, PreferredSupplierName = "FlexiPack Ltd (Bakery Packaging)" },
                new InventoryItem { Id = 10, ItemCode = "PKG-NYL-MED", Name = "Medium Bread Printed Nylon Wrappers",Category = "Packaging",     Unit = "Bundles (1,000 pcs)", QuantityInStock = 30,   ReorderThreshold = 10,  UnitCostPrice = 8000,  PreferredSupplierId = 3, PreferredSupplierName = "FlexiPack Ltd (Bakery Packaging)" },
                new InventoryItem { Id = 11, ItemCode = "PKG-BTL-189", Name = "18.9L Dispenser Water Bottle",       Category = "Packaging",     Unit = "Pieces",              QuantityInStock = 1200, ReorderThreshold = 300, UnitCostPrice = 95,    PreferredSupplierId = 5, PreferredSupplierName = "AquaPure Industrial Systems" },
                new InventoryItem { Id = 12, ItemCode = "PKG-BAG-POP", Name = "Popcorn Small Bag (Printed)",        Category = "Packaging",     Unit = "Packs (500 pcs)",     QuantityInStock = 20,   ReorderThreshold = 8,   UnitCostPrice = 3000,  PreferredSupplierId = 3, PreferredSupplierName = "FlexiPack Ltd (Bakery Packaging)" },

                // ---- Additives & Flavoring ----
                new InventoryItem { Id = 13, ItemCode = "RAW-PRV-01", Name = "Calcium Propionate (Preservative)",  Category = "Additives",     Unit = "Cartons (25kg)",      QuantityInStock = 3,    ReorderThreshold = 2,   UnitCostPrice = 112500,PreferredSupplierId = 4, PreferredSupplierName = "Lesaffre / Saf-Instant Yeast West Africa" },
                new InventoryItem { Id = 14, ItemCode = "RAW-FLV-01", Name = "Milk Flavour Powder",                 Category = "Additives",     Unit = "Cartons (10kg)",      QuantityInStock = 4,    ReorderThreshold = 2,   UnitCostPrice = 95000, PreferredSupplierId = 4, PreferredSupplierName = "Lesaffre / Saf-Instant Yeast West Africa" },
                new InventoryItem { Id = 15, ItemCode = "RAW-SEA-01", Name = "Popcorn Butter Seasoning Powder",    Category = "Additives",     Unit = "Cartons (10kg)",      QuantityInStock = 5,    ReorderThreshold = 2,   UnitCostPrice = 62000, PreferredSupplierId = 4, PreferredSupplierName = "Lesaffre / Saf-Instant Yeast West Africa" },

                // ---- Fuel & Utility ----
                new InventoryItem { Id = 16, ItemCode = "ENG-DSL-01", Name = "Automotive Gas Oil (AGO Diesel Fuel)",Category = "Fuel & Utility", Unit = "Litres",            QuantityInStock = 800,  ReorderThreshold = 300, UnitCostPrice = 1250,  PreferredSupplierId = 8, PreferredSupplierName = "TotalEnergies Nigeria PLC" },
                new InventoryItem { Id = 17, ItemCode = "ENG-PMS-01", Name = "Premium Motor Spirit (PMS Petrol)",   Category = "Fuel & Utility", Unit = "Litres",            QuantityInStock = 650,  ReorderThreshold = 250, UnitCostPrice = 980,   PreferredSupplierId = 8, PreferredSupplierName = "TotalEnergies Nigeria PLC" }
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
                        new() { Id = 3, StoreRequestId = 1, InventoryItemId = 9, ItemName = "Jumbo Bread Printed Nylon Wrappers", Unit = "Bundles (1,000 pcs)", RequestedQuantity = 1, IssuedQuantity = 1 }
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
                },
                new StoreRequest
                {
                    Id = 3,
                    RequestNumber = "REQ-20260819-03",
                    RequestedBy = "Fatima Bello (Water Supervisor)",
                    Department = "Water Bottling Line",
                    Purpose = "Morning Run #04 Purified Dispenser Refilling (250 Bottles)",
                    Status = StoreRequestStatus.Issued,
                    RequestedAt = DateTime.UtcNow.AddHours(-2),
                    ApprovedBy = "Musa Ibrahim (Store Mgr)",
                    ApprovedAt = DateTime.UtcNow.AddHours(-1),
                    Items = new List<StoreRequestItem>
                    {
                        new() { Id = 6, StoreRequestId = 3, InventoryItemId = 11, ItemName = "18.9L Dispenser Water Bottle", Unit = "Pieces", RequestedQuantity = 250, IssuedQuantity = 250 }
                    }
                },
                new StoreRequest
                {
                    Id = 4,
                    RequestNumber = "REQ-20260819-04",
                    RequestedBy = "Blessing Etim (Snacks Supervisor)",
                    Department = "Popcorn & Confectionery Section",
                    Purpose = "Batch #05 Popcorn Cooking Run",
                    Status = StoreRequestStatus.Issued,
                    RequestedAt = DateTime.UtcNow.AddHours(-2),
                    ApprovedBy = "Musa Ibrahim (Store Mgr)",
                    ApprovedAt = DateTime.UtcNow.AddHours(-1),
                    Items = new List<StoreRequestItem>
                    {
                        new() { Id = 7, StoreRequestId = 4, InventoryItemId = 7, ItemName = "Popcorn Maize (Mota)", Unit = "Bags (50kg)", RequestedQuantity = 1, IssuedQuantity = 1 },
                        new() { Id = 8, StoreRequestId = 4, InventoryItemId = 8, ItemName = "Vegetable Frying Oil", Unit = "Jerrycans (25L)", RequestedQuantity = 1, IssuedQuantity = 1 },
                        new() { Id = 9, StoreRequestId = 4, InventoryItemId = 12, ItemName = "Popcorn Small Bag (Printed)", Unit = "Packs (500 pcs)", RequestedQuantity = 1, IssuedQuantity = 1 }
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

        public InventoryItem? UpdateItem(int id, InventoryItemDto dto)
        {
            var item = GetItemById(id);
            if (item == null) return null;

            var supplier = dto.PreferredSupplierId.HasValue ? GetSupplierById(dto.PreferredSupplierId.Value) : null;
            item.ItemCode = dto.ItemCode;
            item.Name = dto.Name;
            item.Category = dto.Category;
            item.Unit = dto.Unit;
            item.QuantityInStock = dto.QuantityInStock;
            item.ReorderThreshold = dto.ReorderThreshold;
            item.UnitCostPrice = dto.UnitCostPrice;
            item.PreferredSupplierId = dto.PreferredSupplierId;
            item.PreferredSupplierName = supplier?.Name;
            return item;
        }

        public bool DeleteItem(int id)
        {
            var item = GetItemById(id);
            if (item == null) return false;
            _items.Remove(item);
            return true;
        }

        public bool RestockItem(int itemId, decimal addedQuantity)
        {
            var item = GetItemById(itemId);
            if (item == null) return false;
            item.QuantityInStock += addedQuantity;
            item.LastRestockedDate = DateTime.UtcNow;
            return true;
        }

        public List<StoreRequest> GetRequestsByItemId(int itemId)
        {
            return _storeRequests
                .Where(r => r.Items.Any(i => i.InventoryItemId == itemId))
                .OrderByDescending(r => r.RequestedAt)
                .ToList();
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
