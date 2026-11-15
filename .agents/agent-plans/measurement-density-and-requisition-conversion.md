# Measurement Densities and Requisition Unit Conversion

## Summary (TL;DR)

Create a Settings-managed measurement/density catalogue, link inventory items to a selected density profile, and use that density to convert kitchen requisition units (for example litres of popcorn maize) into warehouse stock units (for example 50 kg bags). Requisition lines will preserve both the requested kitchen quantity and the converted warehouse quantity, and approval will deduct the converted stock quantity.

The user-approved outcomes are:

- A new Settings page stores **raw material name + measured density (kg/L)**.
- The density-measurement procedure is shown on that page.
- `/Inventory/Items/Create` and `/Inventory/Items/Edit` use a **density dropdown**, not a free-text density field.
- The default density option is **0 / Not applicable** for products not requested in litres.
- Warehouse stock may be deducted in **fractional packages** (for example `0.072` of a 50 kg bag).
- Editing a density record automatically updates linked inventory items.
- The storekeeper sees **both the requested quantity and converted stock quantity**.
- The requisition entry table uses exactly four columns: **RAW MATERIAL | QTY | UNIT | ACTION**, with inline rows.

## Current-State Findings

- `InventoryItems` already has `PackageSize`, `PackageUnitOfMeasure`, and `DensityKgPerLitre`.
- `UnitConversionService` only permits mass ↔ volume conversion when `IsLiquidMaterial(materialName)` matches a name-based liquid list. Popcorn maize therefore cannot convert litres → kg even if a density is supplied.
- `StoreRequestItems` stores only `Unit` and `RequestedQuantity`. `ApproveAndIssueRequestAsync` deducts that same quantity from `InventoryItems.QuantityInStock`, which is measured in warehouse/package units.
- `/Inventory/Requests/Create` currently has a separate material picker and a table with material, unit, stock, quantity, status, and action columns.
- Recipe Create/Edit contain duplicated JavaScript conversion logic that also gates volume↔mass conversion on a name-based liquid test.

## Target Data Model

### New table: `MeasurementDensities`

Suggested schema:

| Column | Type | Notes |
|---|---|---|
| `Id` | INT identity PK | |
| `MaterialName` | NVARCHAR(150) | Required, unique |
| `DensityKgPerLitre` | DECIMAL(18,4) | Required, > 0 |
| `IsActive` | BIT | Default 1 |
| `CreatedAt` / `UpdatedAt` | DATETIME2(3) | UTC defaults |
| `RowVersion` | ROWVERSION | Concurrency token |

No unit field is needed because the density unit is fixed at **kg/L**.

### InventoryItems changes

Add:

| Column | Type | Notes |
|---|---|---|
| `MeasurementDensityId` | INT NULL | FK to `MeasurementDensities`; null means not applicable / 0 |

Keep `DensityKgPerLitre` as a mirrored value for existing conversion code:

- When an inventory item is saved, set `DensityKgPerLitre` from the selected density record, or `0`/null when “Not applicable” is selected.
- When a density setting is edited, update all linked inventory items’ `DensityKgPerLitre` automatically.
- The dropdown is the only UI mechanism for assigning density.

### StoreRequestItems changes

Add:

| Column | Type | Purpose |
|---|---|---|
| `BaseQuantity` | DECIMAL(18,3) | Converted quantity in the material base unit, e.g. `3.6` kg |
| `BaseUnit` | NVARCHAR(40) | Base unit snapshot, e.g. `kg` |
| `StockQuantity` | DECIMAL(18,6) | Quantity in warehouse stock/package unit, e.g. `0.072` bags |
| `StockUnit` | NVARCHAR(40) | Warehouse stock unit snapshot, e.g. `Bags (50kg)` |

Existing `RequestedQuantity` and `Unit` remain the kitchen-entered quantity and unit.

## Implementation Phases

### Phase 1: Measurement settings backend

1. Add `MeasurementDensities.sql` to `Manufacture.Database/Tables/`.
2. Add the table include to `Manufacture.Database/Manufacture.Database.sqlproj`.
3. Add `MeasurementDensity` EF entity.
4. Add `DbSet<MeasurementDensity> MeasurementDensities` and mapping in `Manufacture.Library/Data/ManufactureDbContext.cs`.
5. Add `MeasurementDensity` domain model for UI/service use.
6. Add a dedicated `IMeasurementDensityService`/`MeasurementDensityService` with:
   - `Task<List<MeasurementDensityDto>> GetAllAsync(bool includeInactive)`
   - `Task<MeasurementDensityDto> SaveAsync(MeasurementDensityDto dto)`
   - `Task<bool> ToggleStatusAsync(int id)`
7. When saving an edited density:
   - update all linked `InventoryItems.DensityKgPerLitre` values;
   - do not rewrite historical requisition snapshots.
8. Register the service in `Manufacture.Web/Program.cs`.

### Phase 2: Settings page

1. Create `Manufacture.Web/Pages/Settings/Measurements.cshtml` and `.cshtml.cs`.
2. Include the user-provided measurement procedure as a visible help card:

   > Don’t rely on a textbook figure, because bulk density depends on the product and how it’s packed. Measure it once:
   > 1. Fill a 1 L container with the corn, level it off, and don’t shake it down.
   > 2. Weigh the contents and subtract the container’s weight.
   > 3. Repeat 2 or 3 times and take the average.

3. Provide add/edit/deactivate controls for material name and density.
4. Add a **Measurements** link to `Manufacture.Web/Pages/Settings/Index.cshtml`.
5. Apply the same Settings RBAC check used by `Settings/ProductionShifts.cshtml.cs`.

### Phase 3: Inventory item density dropdown

1. Extend `InventoryItemDto` (`Manufacture.Library/Models/DTOs/InventoryDtos.cs`) with:
   - `int? MeasurementDensityId`
   - `string? MeasurementDensityName`
   - `decimal? MeasurementDensityValue`
2. In `Manufacture.Web/Pages/Inventory/Items/Create.cshtml.cs` and `Edit.cshtml.cs`:
   - load active density records;
   - expose them to the page.
3. Replace the free-text density input in both Create and Edit views with a dropdown:
   - option value `""`: **Not applicable (0 kg/L)**
   - option values: density record IDs
   - label: `{MaterialName} ({DensityKgPerLitre:0.####} kg/L)`
4. Update `InventoryService.CreateItemAsync`/`UpdateItemAsync`:
   - resolve selected density record;
   - set `MeasurementDensityId`;
   - mirror `DensityKgPerLitre`.
5. Update `InventoryService.MapItem` to return the selected density ID/name/value.
6. Change the inventory help text to explain that density enables **mass ↔ volume** conversions, including bulk solids such as corn, and that `0` means volume requests are unavailable.

### Phase 4: Make conversion density-driven

1. In `Manufacture.Library/Interfaces/IUnitConversionService.cs`:
   - add an optional `decimal? densityKgPerLitre` parameter to `CanConvert` and `GetCompatibleUnits`;
   - keep existing callers source-compatible.
2. In `Manufacture.Web/Services/UnitConversionService.cs`:
   - allow mass ↔ volume when `densityKgPerLitre > 0` for any material;
   - use `IsLiquidMaterial` only as fallback when no density is supplied;
   - pass the density through `TryConvert` so corn conversions succeed;
   - include volume units in `GetCompatibleUnits` when density is available for a mass-based item;
   - include mass units when density is available for a volume-based item.
3. Update Recipe Create/Edit JavaScript:
   - use `material.density > 0` instead of only the liquid-name regex when adding litres/ml to the unit dropdown;
   - allow the mass/volume bridge when `densityOverride > 0`.
4. Add `CompatibleUnits` metadata to the inventory item payload used by the requisition page. Prefer generating it through `IUnitConversionService.GetCompatibleUnits(...)` so the requisition and recipe pages share the same unit rules.

### Phase 5: Requisition create page rework

1. Rework `Manufacture.Web/Pages/Inventory/Requests/Create.cshtml`:
   - remove the separate material picker;
   - add an **Add Material** button that appends an inline row;
   - render table columns exactly: `RAW MATERIAL | QTY | UNIT | ACTION`;
   - put stock information as secondary text under the material name, e.g. `Stock: 8 Bags (50kg) (400 kg)`;
   - render the material cell as a select of active inventory items;
   - render the unit cell as a dropdown of compatible units for the selected item;
   - render action as a remove button.
2. Add a shared JS row state containing:
   - `inventoryItemId`, `name`, `code`
   - `requestedQuantity`, `requestedUnit`
   - `packageSize`, `packageUnitOfMeasure`, `stockUnit`
   - `densityKgPerLitre`, `compatibleUnits`
3. On material change:
   - load compatible units;
   - default to the item’s base unit or stock unit;
   - clear/recalculate quantity conversion.
4. On quantity/unit change:
   - display a conversion hint: `5 litres = 3.6 kg = 0.072 Bags (50kg)`;
   - show an “Exceeds stock” warning when converted `StockQuantity > QuantityInStock`;
   - keep the server as the authority; do not trust client-posted conversion values.
5. Post only the requested fields:
   - `InventoryItemId`
   - `RequestedQuantity`
   - `Unit`
   - `ItemName` (optional snapshot/display)
6. `Create.cshtml.cs` continues to load active inventory items. Extend the loaded payload with density, package data, and compatible units.

### Phase 6: Server-side requisition conversion and deduction

1. Extend `StoreRequestItemDto` with:
   - `BaseQuantity`, `BaseUnit`
   - `StockQuantity`, `StockUnit`
2. Update `InventoryService.CreateRequestAsync`:
   - load selected inventory items with measurement density;
   - normalize `PackageUnitOfMeasure` to a base unit (`kg`, `g`, `L`/`litres`, `ml`, `pcs`);
   - call `IUnitConversionService.TryConvert(requestedQty, requestedUnit, baseUnit, itemName, density)`.
   - if conversion fails, throw a clear `InvalidOperationException` naming the material and units;
   - calculate `StockQuantity = BaseQuantity / PackageSize` when `PackageSize > 0`;
   - store `RequestedQuantity`, `Unit`, `BaseQuantity`, `BaseUnit`, `StockQuantity`, `StockUnit`.
   - `StockUnit` should be the inventory item’s snapshot `Unit` (for example `Bags (50kg)`).
3. Update `ApproveAndIssueRequestAsync`:
   - group shortage checks by `InventoryItemId` and compare `StockQuantity` to `QuantityInStock`;
   - set `IssuedQuantity = RequestedQuantity`;
   - deduct `InventoryItem.QuantityInStock -= StockQuantity`;
   - create `StockMovement` with `Quantity = StockQuantity` and notes containing the requested equivalent, e.g. `Approved store release: 5 L = 3.6 kg = 0.072 Bags (50kg)`.
4. Update `MapRequest` and `StoreRequestItemDto` mapping to include the new fields.

### Phase 7: Requisition index/approval display

1. Update `Manufacture.Web/Pages/Inventory/Requests/Index.cshtml`:
   - show requested quantity/unit;
   - show converted base quantity/unit;
   - show converted stock quantity/unit;
   - show issued quantity.
2. Ensure approval/rejection behavior and status filters remain unchanged.
3. Keep the existing full-issue workflow; partial issuing is out of scope.

### Phase 8: Schema deployment and seed

1. Deploy `Manufacture.Database` to LocalDB.
2. Add seed data for existing density-bearing items where meaningful:
   - Vegetable Frying Oil — `0.92 kg/L`
   - Vanilla Flavour Concentrate — `1.00 kg/L`
   - Do not invent a popcorn-maize density. Leave it unset until the user measures it.
3. Link existing inventory items with a non-null `DensityKgPerLitre` to the seeded measurement records.
4. Add/update seed entries only if they do not already exist.

## Relevant Files

### Database
- `Manufacture.Database/Tables/MeasurementDensities.sql` (new)
- `Manufacture.Database/Tables/InventoryItems.sql`
- `Manufacture.Database/Tables/StoreRequestItems.sql`
- `Manufacture.Database/Manufacture.Database.sqlproj`
- `scripts/seed-database.sql`

### Library
- `Manufacture.Library/Models/Entities/MeasurementDensity.cs` (new)
- `Manufacture.Library/Models/Entities/InventoryItem.cs`
- `Manufacture.Library/Models/Entities/StoreRequestItem.cs`
- `Manufacture.Library/Models/DTOs/InventoryDtos.cs`
- `Manufacture.Library/Models/Domain/` (new density domain model or DTO)
- `Manufacture.Library/Interfaces/IInventoryService.cs`
- `Manufacture.Library/Interfaces/IUnitConversionService.cs`
- `Manufacture.Library/Data/ManufactureDbContext.cs`

### Web
- `Manufacture.Web/Services/InventoryService.cs`
- `Manufacture.Web/Services/UnitConversionService.cs`
- `Manufacture.Web/Program.cs`
- `Manufacture.Web/Pages/Settings/Measurements.cshtml` (new)
- `Manufacture.Web/Pages/Settings/Measurements.cshtml.cs` (new)
- `Manufacture.Web/Pages/Settings/Index.cshtml`
- `Manufacture.Web/Pages/Inventory/Items/Create.cshtml` / `.cshtml.cs`
- `Manufacture.Web/Pages/Inventory/Items/Edit.cshtml` / `.cshtml.cs`
- `Manufacture.Web/Pages/Inventory/Requests/Create.cshtml` / `.cshtml.cs`
- `Manufacture.Web/Pages/Inventory/Requests/Index.cshtml`
- `Manufacture.Web/Pages/Production/Recipes/Create.cshtml`
- `Manufacture.Web/Pages/Production/Recipes/Edit.cshtml`

## Verification

### Automated
```powershell
dotnet build Manufacture.slnx -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\
dotnet test Manufacture.Tests/Manufacture.Tests.csproj --no-build -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\
```

### Manual
1. Open `/Settings/Measurements`; confirm the measurement instructions are visible.
2. Add `Popcorn Maize` with density `0.72 kg/L`.
3. Open `/Inventory/Items/Create`; confirm the density dropdown contains:
   - `Not applicable (0 kg/L)`
   - `Popcorn Maize (0.72 kg/L)`
4. Create/edit the corn inventory item with `PackageSize=50`, `PackageUnitOfMeasure=kg`, `Unit=Bags (50kg)`, and select the corn density.
5. Open `/Inventory/Requests/Create`; add corn, enter `5` and select `litres`; confirm the hint shows `5 litres = 3.6 kg = 0.072 Bags (50kg)`.
6. Submit the requisition; verify the database line stores requested `5 litres`, base `3.6 kg`, stock `0.072 Bags (50kg)`.
7. Approve and issue the request; verify:
   - stock decreases by `0.072` bags;
   - a stock movement of `0.072` bags is created;
   - the requisition index shows both requested and converted quantities.
8. Select an item with density `0`; confirm litres/ml are not offered in the unit dropdown.
9. Edit the `Popcorn Maize` density; confirm linked inventory items update automatically while existing requisition snapshots remain unchanged.

## Decisions / Non-Goals

- Density records are managed centrally under Settings; inventory items select a density profile.
- Density is measured in **kg/L** and applies to bulk solids as well as liquids.
- Fractional package deductions are allowed.
- Density changes update linked inventory items but do not rewrite historical requisition quantities.
- The storekeeper sees both requested and converted stock quantities.
- Requisition rows use exactly four columns with inline editing.
- Out of scope: partial issuing, separate loose-stock accounting, unit-master redesign, and supplier-specific density variants.
