# Production Module: Database Migration + Configurable Product Types + Recipe Form Rework

## Summary (TL;DR)

Replace the in-memory `MockProductionService` with a database-backed production service across the entire Production module (dashboard, recipes, batches, pricing, and production-related dashboard data).

Introduce database-managed production categories and product types under Settings, then refactor the recipe builder so that:

- Product Type is selected from the new Product Types table.
- Baseline Unit and Output Unit Noun are read-only values populated from the selected Product Type.
- Packaging and Labor/Overhead fields are removed from the UI and all costing formulas.
- Raw materials come directly from `InventoryItems`, excluding `Fuel` and `Utilities`.
- Bulk-package to recipe-unit conversion uses explicit package size, package unit, and supplier density data.
- Unit production cost = total batch material cost / expected batch yield.

The plan is implementation-ready and assumes the decisions recorded below.

---

## Approved Decisions

1. **Recipes use a ProductTypeId FK.**
   - Add `ProductionCategories` and `ProductionProductTypes`.
   - `Recipes.ProductionProductTypeId` references `ProductionProductTypes`.
   - Existing `Recipes.ProductType` and `Recipes.BaselineUnit` enum columns are removed after migration.
   - Keep `Recipes.OutputUnit` as a saved snapshot of the product type’s output unit noun to limit unrelated Sales/Analytics changes.

2. **Recipe ingredients use `InventoryItems` directly.**
   - `RecipeIngredients` gains `InventoryItemId` FK.
   - The old `Ingredients` table and `RecipeIngredients.IngredientId` relationship are retired after migration.
   - Raw-material display names and prices come from live inventory.

3. **Bulk/density conversion data lives on `InventoryItems`.**
   - Add explicit package-size and density fields.
   - Use `IUnitConversionService` for g/kg/ml/litre/pcs conversions.
   - If a conversion cannot be performed, recipe save must fail with a clear validation message.

4. **Packaging and labor columns are dropped.**
   - Remove `Recipes.PackagingCost` and `Recipes.LaborAndOverheadPerUnit` from the database, EF entities, DTOs, costing, and views.
   - Unit cost no longer includes handling, packaging, labor, or overhead.

---

## Scope

### In Scope

- Production dashboard (`/Production`)
- Recipes index/create/edit/details
- Production batches index/create/details/output recording
- Pricing and margin page
- Production settings page for categories and product types
- Database schema, EF Core model, seed data, and data migration
- Removing all Production page/service dependencies on `MockProductionService`
- Replacing production pre-order demand data that currently comes from `MockSalesService` with database-backed sales data
- Inventory item package/density fields and related forms
- Production-related dashboard data currently sourced from `MockProductionService`

### Out of Scope

- Redesigning Sales, HR, Fleet, or Payroll modules beyond the production references needed to compile and function
- Adding a general-purpose unit-of-measure master beyond the explicit package fields on inventory items
- Changing production business statuses unless required by the schema migration
- Building a full audit UI for production changes beyond existing audit conventions

---

## Target Database Model

### New: `ProductionCategories`

| Column | Type | Notes |
|---|---|---|
| Id | INT identity PK | |
| Name | NVARCHAR(80) | Unique, required |
| Description | NVARCHAR(300) | Default empty |
| IsActive | BIT | Default 1 |
| CreatedAt / UpdatedAt | DATETIME2(3) | Default UTC |
| RowVersion | ROWVERSION | |

Seed categories:

- `Bread`
- `Water`
- `Snacks`

### New: `ProductionProductTypes`

| Column | Type | Notes |
|---|---|---|
| Id | INT identity PK | |
| ProductionCategoryId | INT FK | Required |
| Name | NVARCHAR(120) | Required |
| Baseline | NVARCHAR(150) | Required |
| OutputUnitNoun | NVARCHAR(40) | Required |
| IsActive | BIT | Default 1 |
| CreatedAt / UpdatedAt | DATETIME2(3) | Default UTC |
| RowVersion | ROWVERSION | |

Unique index: `(ProductionCategoryId, Name)`.

Seed data:

| Category | Product Type | Baseline | Output Unit Noun |
|---|---|---|---|
| Bread | Bread | 1 Bag of Flour (50 kg) | loaves |
| Water | Water | 1 Production Run | bottles |
| Snacks | Popcorn | 1 Cooking Pot (~2 kg corn) | bags |

### Change: `Recipes`

Add:

- `ProductionProductTypeId INT NOT NULL` FK → `ProductionProductTypes(Id)`.
- Index `(ProductionProductTypeId, IsActive)`.

Remove after data migration:

- `ProductType INT`
- `BaselineUnit INT`
- `PackagingCost DECIMAL(18,4)`
- `LaborAndOverheadPerUnit DECIMAL(18,4)`

Keep:

- `OutputUnit NVARCHAR(40)` as a snapshot populated from the selected Product Type.
- `ExpectedYield`, `SellingPrice`, `IsActive`, audit columns.

Data migration mapping:

- Recipes 1–5 (`ProductType = 0`) → Bread / Bread.
- Recipe 6 (`ProductType = 1`) → Water / Water.
- Recipe 7 (`ProductType = 2`) → Snacks / Popcorn.
- Set `OutputUnit` from the mapped Product Type before dropping `BaselineUnit` and `ProductType`.

### Change: `InventoryItems`

Add:

- `PackageSize DECIMAL(18,3) NOT NULL DEFAULT 1`
- `PackageUnitOfMeasure NVARCHAR(20) NOT NULL DEFAULT 'kg'`
  - Expected values: `kg`, `g`, `L`, `ml`, `pcs`
- `DensityKgPerLitre DECIMAL(18,4) NULL`
  - Required for liquids that need mass↔volume or count↔volume conversion.
  - Validate `> 0` when supplied.

Interpretation:

- `InventoryItems.UnitCostPrice` remains the price of one inventory `Unit`/package.
- `Unit` remains the display package description (e.g. `Bags (50kg)`).
- Production conversion uses `PackageSize` + `PackageUnitOfMeasure` as the explicit source of truth.
- Cost per package unit = `UnitCostPrice / PackageSize`.
- Base stock quantity remains in inventory `Unit` packages.

Seed/backfill examples:

| Inventory item | PackageSize | PackageUnitOfMeasure | DensityKgPerLitre |
|---|---:|---|---:|
| Flour / Sugar / Salt / Maize bags | 50 / 50 / 25 / 50 | kg | null |
| Shortening bucket | 15 | kg | null |
| Softener / preservative / flavour cartons | 10 / 25 / 10 | kg | null |
| Process water | 1 | L | 1.0 |
| Vegetable oil | 1 | L | 0.92 |
| Milk / liquid flavour | 1 | L | supplier value or 1.03 default |
| Printed wrappers / bags / labels / caps | pack or bundle count | pcs | null |

### Change: `RecipeIngredients`

- Replace `IngredientId` FK → `Ingredients` with `InventoryItemId` FK → `InventoryItems`.
- Keep `Unit`, `QuantityUnit`, `QuantityPerBatch`, `BaselineYield`, `IngredientUnitCost`.
- Optionally rename `IngredientUnitCost` to `UnitCostSnapshot` only if a coordinated rename is acceptable; otherwise keep the column name to reduce churn.
- `Unit` = inventory pricing/package unit, not an arbitrary ingredient unit.
- `QuantityUnit` = the unit the user typed.
- `QuantityPerBatch` = quantity normalized to the inventory package unit.

### Change: `ProductionBatches`

- Add `ProductionProductTypeId INT NOT NULL` FK → `ProductionProductTypes(Id)`.
- Replace `ProductCategory INT` with category information derived from the product type, or retain `ProductCategory` as a legacy snapshot while the new service reads `ProductionProductTypeId`.
- Preferred: drop `ProductCategory` and its check constraint, derive category name from `ProductionProductTypes`/`ProductionCategories`.
- Keep `OutputUnitLabel` as a batch snapshot for historical display.

### Drop: `Ingredients`

Drop only after:

1. `RecipeIngredients` has been migrated to `InventoryItems`.
2. All EF navigation properties and service references are removed.
3. No remaining production or inventory code references `Ingredients`.

---

## Service Architecture

### New interface: `Manufacture.Library/Interfaces/IProductionService.cs`

Suggested methods:

```csharp
// Settings
Task<List<ProductionCategoryDto>> GetCategoriesAsync(bool includeInactive = false, CancellationToken ct = default);
Task<ProductionCategoryDto> SaveCategoryAsync(ProductionCategoryDto dto, CancellationToken ct = default);
Task<bool> ToggleCategoryStatusAsync(int id, CancellationToken ct = default);

Task<List<ProductionProductTypeDto>> GetProductTypesAsync(int? categoryId = null, bool includeInactive = false, CancellationToken ct = default);
Task<ProductionProductTypeDto> SaveProductTypeAsync(ProductionProductTypeDto dto, CancellationToken ct = default);
Task<bool> ToggleProductTypeStatusAsync(int id, CancellationToken ct = default);

// Raw materials
Task<List<ProductionRawMaterialDto>> GetRawMaterialsAsync(CancellationToken ct = default);

// Recipes
Task<List<Recipe>> GetRecipesAsync(bool includeInactive = false, CancellationToken ct = default);
Task<Recipe?> GetRecipeAsync(int id, CancellationToken ct = default);
Task<Recipe> CreateRecipeAsync(RecipeBuilderDto dto, CancellationToken ct = default);
Task<bool> UpdateRecipeAsync(RecipeBuilderDto dto, CancellationToken ct = default);
Task<bool> ToggleRecipeStatusAsync(int id, CancellationToken ct = default);

// Batches
Task<List<ProductionBatch>> GetBatchesAsync(CancellationToken ct = default);
Task<ProductionBatch?> GetBatchAsync(int id, CancellationToken ct = default);
Task<ProductionBatch> CreateBatchAsync(ProductionBatchDto dto, string loggedBy, CancellationToken ct = default);
Task<ProductionBatch?> RecordBatchOutputAsync(int id, int actualQuantity, BatchStatus status, string? notes, string? bakerInCharge, CancellationToken ct = default);

// Costing
Task<decimal> CalculateBatchMaterialCostAsync(int recipeId, decimal batchInputQuantity, CancellationToken ct = default);
Task<RecipeCostingResultDto> CostRecipeAsync(int recipeId, decimal? actualYield = null, CancellationToken ct = default);
Task<ProfitabilityReportDto> BuildProfitabilityReportAsync(bool includeInactive = false, CancellationToken ct = default);
```

### New implementation: `Manufacture.Web/Services/ProductionService.cs`

Dependencies:

- `ManufactureDbContext`
- `IUnitConversionService`
- `ISalesService` for production demand/linked orders where needed

Responsibilities:

- Map EF entities to domain models.
- Load Product Types with categories for recipe forms and badges.
- Source recipe lines from InventoryItems.
- Normalize recipe quantities to inventory package units using `IUnitConversionService`, including density.
- Calculate batch and unit costs using only material cost / yield.
- Generate batch numbers and persist batch snapshots.
- Enforce validation:
  - Product type must be active.
  - Expected yield > 0.
  - At least one valid raw material line.
  - No Fuel/Utilities items in recipe lines.
  - Conversion must succeed; do not silently fall back to the raw quantity.
  - Density is required when a mass↔volume conversion cannot otherwise be resolved.

### Refactor: `RecipeCostingEngine`

- Change its dependency from `MockProductionService` to `IProductionService` (or make it a pure calculator that accepts loaded recipes and inventory prices).
- Remove `PackagingCost` and `LaborAndOverheadPerUnit` from:
  - `RecipeCostingResultDto`
  - `TotalUnitCost`
  - `UnitCostAtYield`
  - `PreviewDailyProduction`
  - profitability reports
  - all UI summaries
- Formula:

```text
BatchLineCost = ConvertedQuantityPerBatch × (InventoryItem.UnitCostPrice ÷ PackageSize)
TotalBatchCost = Sum(BatchLineCost)
UnitProductionCost = TotalBatchCost ÷ ExpectedYield
Margin = SellingPrice − UnitProductionCost
```

### Replace production demand data

`MockSalesService.GetOpenOrdersForRecipe(...)` and `GetDemandForRecipe(...)` must be replaced with database-backed sales queries.

Preferred:

- Add to `ISalesService`:
  - `GetOpenOrdersForRecipeAsync(int? recipeId, CancellationToken ct)`
  - `GetDemandForRecipeAsync(int recipeId, CancellationToken ct)`
- Implement against `Orders` / `OrderItems`, excluding completed/cancelled states as appropriate.
- Update production pages to call these methods.

Alternative if a smaller change is desired:

- Load `GetOrdersAsync()` in production pages and compute demand from non-cancelled pre-orders.

### Dependency registration

Update `Manufacture.Web/Program.cs`:

- Register `IProductionService` as scoped.
- Replace `MockProductionService` injection in production pages and dashboard.
- Change `RecipeCostingEngine` registration to scoped (or factory scoped) if it depends on scoped `IProductionService`.
- Remove production pages’ dependency on `MockSalesService` for production demand.

---

## Domain / DTO Refactor

Update production domain models and DTOs to remove enum-driven product families and removed cost fields.

### `ProductionEntities.cs`

- Remove or deprecate:
  - `ProductType` enum
  - `BaselineUnit` enum
  - `Recipe.ProductType`
  - `Recipe.BaselineUnit`
  - `Recipe.PackagingCost`
  - `Recipe.LaborAndOverheadPerUnit`
  - `ProductionBatch.ProductCategory` enum value
- Add:
  - `Recipe.ProductionProductTypeId`
  - `Recipe.ProductTypeName`
  - `Recipe.CategoryId`
  - `Recipe.CategoryName`
  - `Recipe.Baseline`
  - `Recipe.OutputUnit` retained as snapshot
  - `ProductionBatch.ProductionProductTypeId`
  - `ProductionBatch.ProductTypeName`
  - `ProductionBatch.CategoryName`
- Update `Recipe.TotalUnitCost` and `UnitCostAtYield` to remove packaging/labor.
- Update `RecipeIngredient.IngredientId` to `InventoryItemId` (or add mapping properties).

### DTOs

Update or add:

- `ProductionCategoryDto`
- `ProductionProductTypeDto`
- `ProductionRawMaterialDto`
- Update `RecipeBuilderDto`:
  - `ProductionProductTypeId` (required)
  - Remove `BaselineUnit`, `PackagingCost`, `LaborAndOverheadPerUnit`
  - Keep `ExpectedYield`, `OutputUnit` (read-only snapshot), `SellingPrice`
- Update `RecipeIngredientInputDto`:
  - `InventoryItemId`
  - `InventoryItemName`
  - `QuantityUnit`
  - `QuantityPerBatch`
- Update `RecipeCostingResultDto` and `RecipeCostLineDto` to remove packaging/labor fields.
- Update `ProductionBatchDto` to use ProductTypeId and product type/category display fields.
- Update `BaselineOptionDto` or retire it in favor of product type data.

### References that must be updated

Search and update all references to:

- `MockProductionService`
- `Recipe.PackagingCost`
- `Recipe.LaborAndOverheadPerUnit`
- `Recipe.BaselineUnit`
- `Recipe.ProductType`
- `ProductionBatch.ProductCategory`
- `RecipeCatalog`
- `RecipeIngredients.IngredientId`
- `Ingredients` entity/table
- `RecipeIngredient.IngredientId`

Known affected areas:

- `Manufacture.Web/Pages/Production/**`
- `Manufacture.Web/Pages/Dashboard/Index.cshtml.cs`
- `Manufacture.Web/Services/RecipeCostingEngine.cs`
- `Manufacture.Web/Services/SalesService.cs` recipe mapping
- `Manufacture.Web/Services/InventoryService.cs` recipe usage
- `Manufacture.Web/Services/MockAnalyticsService.cs` where it consumes production recipes
- `Manufacture.Library/Data/ManufactureDbContext.cs`
- `Manufacture.Library/Models/Domain/ProductionEntities.cs`
- `Manufacture.Library/Models/Entities/*`
- `Manufacture.Library/Models/DTOs/RecipeCostingDtos.cs`
- `Manufacture.Library/Models/DTOs/DualModeRecipeDtos.cs`
- `scripts/seed-database.sql`

---

## Production Settings Page

### New page: `/Pages/Settings/Production.cshtml` + `.cshtml.cs`

Suggested UI:

- Two sections on one page:
  1. **Production Categories** — name, description, active toggle.
  2. **Product Types** — category dropdown, product type name, baseline, output unit noun, active toggle.
- Follow existing Settings pages (`Departments`, `JobRoles`) for styling and CRUD pattern.
- Add a link to `/Settings/Production` on `/Settings/Index`.
- Enforce:
  - Category name required and unique.
  - Product type name required and unique within its category.
  - Baseline and output unit noun required.
  - Cannot deactivate a category/product type that has active recipes without an explicit warning or block, depending on chosen policy.
  - Product types can be deactivated; inactive types must not appear in the recipe create/edit dropdown.

---

## Recipe Builder / Edit Form Changes

### Product Type section

- Product Type select options come from active `ProductionProductTypes`.
- Each option exposes:
  - `value = Id`
  - `data-baseline`
  - `data-output-unit`
- On change:
  - Set `Baseline Unit` text input (disabled) to the selected baseline.
  - Set `Output Unit Noun` text input (disabled) to the selected output unit noun.
- On load:
  - If editing, populate from the saved product type.
  - If creating, select the first active product type or Bread if present.
- Baseline and Output Unit are disabled and non-posted/read-only inputs. Server re-derives them from ProductTypeId.

### Remove overhead fields

Remove from Create and Edit views:

- `Handling & Label Cost per Unit`
- `Labor & Overhead per Unit`

Remove from:

- JS cost calculations
- Summary card
- Server DTOs
- Recipe costing result DTO/UI
- Batch preview and batch creation formulas
- Pricing/profitability formulas

Do not add replacement overhead fields.

### Raw Ingredient Formula

- Load raw materials from `InventoryItems`.
- Exclude categories:
  - `Fuel`
  - `Utilities`
  - Include `Raw Materials`, `Packaging`, and `Additive/Flavor` because the requirement says all except Fuel and Utilities.
- Dropdown text must show **only the product name**.
  - Do not show item code, stock, or price in the option label.
- Carry the rest of the data in option `data-*` attributes or a JS map:
  - inventory item id
  - package unit
  - package size
  - package unit of measure
  - density
  - unit cost price
- Server must re-resolve all item data from the database on POST; do not trust posted prices.

### Unit conversion and cost

Client-side preview:

- Use package size/unit + density to show converted quantity and line cost.
- Display converted quantity for user clarity.
- Keep line cost and per-unit cost live.

Server-side authoritative calculation:

- For each line:
  1. Load inventory item by ID.
  2. Reject Fuel/Utilities.
  3. Convert entered quantity from `QuantityUnit` to the inventory item’s `PackageUnitOfMeasure`.
  4. If conversion fails, add a field error and do not save.
  5. Store normalized quantity in `QuantityPerBatch`.
  6. Compute line cost using `UnitCostPrice / PackageSize`.
- Compute total batch material cost and unit cost by expected yield.

Acceptance examples:

- 40 g yeast priced per 50 kg bag → `40 / 1000 / 50` bags; cost = fractional bags × bag price.
- 2 L liquid flavour with density 1.03 kg/L priced per kg → conversion uses density.
- 2 litres of process water priced per litre → 2 L.
- Packaging count entered in pcs against a 1,000-pcs bundle → fractional bundle cost.

---

## Production Batch / Dashboard Migration

### `/Production` dashboard

- Replace `MockProductionService` recipes/batches with `IProductionService`.
- Replace `MockSalesService.GetPreOrders()` demand calculation with `ISalesService` database pre-orders.
- Update category/product type display fields.
- Keep KPIs but source from database.

### `/Production/Batches`

- Replace mock recipes, batches, and sales order lookups with DB services.
- Batch costing snapshots:
  - Material cost at time of logging.
  - Expected unit cost.
  - Actual unit cost.
  - No packaging/labor overhead.
- Batch number generation should query existing batch numbers and use the existing convention.
- Batch output recording must update only database records and preserve historical snapshots.

### `/Production/Pricing`

- Use inventory item prices as the master price source.
- Recalculate recipe margins after price changes.
- Remove overhead/packaging from profitability report.
- Keep stale-price indicators based on `InventoryItems.UpdatedAt` or `LastRestockedDate`.

---

## Seed and Data Migration

### SQL / seed changes

1. Add new tables and columns.
2. Backfill category/product type rows.
3. Backfill `Recipes.ProductionProductTypeId` from the old enum mapping.
4. Backfill `InventoryItems.PackageSize`, `PackageUnitOfMeasure`, and `DensityKgPerLitre`.
5. Migrate any `RecipeIngredients` rows from `IngredientId` to `InventoryItemId` by ItemCode/Name.
6. Drop `RecipeIngredients.IngredientId` FK and column.
7. Drop `Recipes.ProductType`, `Recipes.BaselineUnit`, `Recipes.PackagingCost`, `Recipes.LaborAndOverheadPerUnit`.
8. Drop `Ingredients` table.
9. Add `ProductionBatches.ProductionProductTypeId` and backfill from recipe.
10. Drop `ProductionBatches.ProductCategory` if adopting the derived-category model.

### Seed recipe lines

Current database has 7 recipes but `RecipeIngredients` is empty. To make the Production module usable immediately:

- Seed the seven existing recipes with their raw-material lines from the current mock recipe definitions.
- Map each mock ingredient to an inventory item by `ItemCode`/name.
- Normalize quantities to the inventory package unit.
- Seed only if no recipe ingredient rows exist, so the script remains idempotent.
- Do not seed duplicate recipe lines on repeated deploy/seed runs.

### Seed production batches

Optional but recommended for initial dashboard parity:

- Seed a small set of representative production batches from the existing mock data.
- Only insert when `ProductionBatches` is empty.
- Preserve batch costing snapshots using the new DB costing model.

---

## Implementation Steps

### Phase 1 — Schema and seed foundation

1. Create `ProductionCategories.sql` and `ProductionProductTypes.sql`.
2. Add `PackageSize`, `PackageUnitOfMeasure`, and `DensityKgPerLitre` to `InventoryItems.sql`.
3. Add `ProductionProductTypeId` to `Recipes.sql`; remove enum/product fields after migration.
4. Replace `RecipeIngredients.IngredientId` with `InventoryItemId`.
5. Add `ProductionProductTypeId` to `ProductionBatches.sql`; remove or retire `ProductCategory`.
6. Update `ManufactureDbContext` mappings, indexes, FKs, and computed columns.
7. Update `scripts/seed-database.sql` with categories, product types, package/density data, recipe product-type mapping, and recipe line seed data.
8. Add an idempotent migration script if the database project cannot express data backfill through schema changes alone.

Depends on: none.

### Phase 2 — EF entities, domain models, DTOs

1. Add `ProductionCategory` and `ProductionProductType` EF entities and DbSets.
2. Update `Recipe`, `RecipeIngredient`, `ProductionBatch`, `InventoryItem` EF entities.
3. Update production domain models to use ProductTypeId/string names and remove packaging/labor.
4. Add/update production DTOs.
5. Update all mapping code that converts EF entities to domain/DTO objects.

Depends on Phase 1.

### Phase 3 — Database-backed production service

1. Add `IProductionService`.
2. Implement `ProductionService` for:
   - categories and product types
   - raw-material loading
   - recipes and recipe lines
   - recipe conversion/costing
   - batches and batch output
   - profitability data
3. Refactor `RecipeCostingEngine` to use the new service/pure calculation model.
4. Add database-backed sales demand methods to `ISalesService`/`SalesService` or compute demand in production pages.
5. Register services in `Program.cs`.

Depends on Phase 2.

### Phase 4 — Settings UI

1. Add `/Settings/Production` page.
2. Add category and product type CRUD/toggle actions.
3. Add settings navigation link.
4. Add validation and active/inactive rules.

Depends on Phase 3 for settings service methods; can run parallel with Phase 5 after service contract exists.

### Phase 5 — Production pages migration

Can proceed in parallel by page group once Phase 3 is stable:

- Recipes Index / Details
- Recipes Create / Edit
- Batches Index / Create / Details
- Production Index / dashboard
- Pricing page
- Dashboard production data

Update each page to use `IProductionService`, `ISalesService`, and the new DTO/domain shapes. Remove `MockProductionService` and `MockSalesService` from these pages.

### Phase 6 — Recipe form behavior

1. Add Product Type selection from DB.
2. Auto-populate disabled Baseline and Output Unit Noun fields.
3. Remove Packaging/Labor fields and calculations.
4. Load inventory raw materials excluding Fuel/Utilities; display only product names.
5. Add package/density data attributes and client preview.
6. Implement authoritative server-side conversion and validation.
7. Update the costing summary and all labels to use only material cost / yield.

Depends on Phase 3 and Phase 5.

### Phase 7 — Inventory package/density editing

1. Add package size/unit-of-measure/density fields to inventory item create/edit pages.
2. Validate package size > 0 and density > 0 when supplied.
3. Show density/package information where relevant in Inventory.
4. Ensure existing inventory items retain useable defaults.

Depends on Phase 1; parallel with Phase 6.

### Phase 8 — Verification and cleanup

1. Remove unused `MockProductionService` registration and usages.
2. Remove or retire `RecipeCatalog` and obsolete enum helpers.
3. Remove obsolete DTO fields/views.
4. Run build/tests.
5. Deploy database and run seed.
6. Perform manual verification.
7. Check for remaining production references to mock data, old columns, and old inventory naming.

---

## Relevant Files

### Database

- `Manufacture.Database/Tables/Recipes.sql`
- `Manufacture.Database/Tables/RecipeIngredients.sql`
- `Manufacture.Database/Tables/Ingredients.sql`
- `Manufacture.Database/Tables/InventoryItems.sql`
- `Manufacture.Database/Tables/ProductionBatches.sql`
- New: `Manufacture.Database/Tables/ProductionCategories.sql`
- New: `Manufacture.Database/Tables/ProductionProductTypes.sql`
- `scripts/seed-database.sql`

### Library

- `Manufacture.Library/Data/ManufactureDbContext.cs`
- `Manufacture.Library/Models/Entities/Recipe.cs`
- `Manufacture.Library/Models/Entities/RecipeIngredient.cs`
- `Manufacture.Library/Models/Entities/Ingredient.cs`
- `Manufacture.Library/Models/Entities/InventoryItem.cs`
- `Manufacture.Library/Models/Entities/ProductionBatch.cs`
- `Manufacture.Library/Models/Domain/ProductionEntities.cs`
- `Manufacture.Library/Models/DTOs/RecipeCostingDtos.cs`
- `Manufacture.Library/Models/DTOs/DualModeRecipeDtos.cs`
- `Manufacture.Library/Models/DTOs/InventoryDtos.cs`
- `Manufacture.Library/Interfaces/IProductionService.cs` (new)
- `Manufacture.Library/Interfaces/IInventoryService.cs`
- `Manufacture.Library/Interfaces/ISalesService.cs`

### Web services

- New: `Manufacture.Web/Services/ProductionService.cs`
- `Manufacture.Web/Services/RecipeCostingEngine.cs`
- `Manufacture.Web/Services/RecipeCatalog.cs` (retire/replace)
- `Manufacture.Web/Services/SalesService.cs`
- `Manufacture.Web/Services/InventoryService.cs`
- `Manufacture.Web/Services/MockAnalyticsService.cs`
- `Manufacture.Web/Services/MockDashboardService.cs`
- `Manufacture.Web/Program.cs`

### Web pages

- `Manufacture.Web/Pages/Production/Index.cshtml.cs`
- `Manufacture.Web/Pages/Production/Recipes/Index.cshtml.cs`
- `Manufacture.Web/Pages/Production/Recipes/Create.cshtml.cs`
- `Manufacture.Web/Pages/Production/Recipes/Create.cshtml`
- `Manufacture.Web/Pages/Production/Recipes/Edit.cshtml.cs`
- `Manufacture.Web/Pages/Production/Recipes/Edit.cshtml`
- `Manufacture.Web/Pages/Production/Recipes/Details.cshtml.cs`
- `Manufacture.Web/Pages/Production/Recipes/Details.cshtml`
- `Manufacture.Web/Pages/Production/Batches/Index.cshtml.cs`
- `Manufacture.Web/Pages/Production/Batches/Create.cshtml.cs`
- `Manufacture.Web/Pages/Production/Batches/Details.cshtml.cs`
- `Manufacture.Web/Pages/Production/Pricing/Index.cshtml.cs`
- `Manufacture.Web/Pages/Dashboard/Index.cshtml.cs`
- New: `Manufacture.Web/Pages/Settings/Production.cshtml`
- New: `Manufacture.Web/Pages/Settings/Production.cshtml.cs`
- `Manufacture.Web/Pages/Settings/Index.cshtml`
- Inventory item create/edit pages under `Manufacture.Web/Pages/Inventory/Items/`

---

## Verification

### Build

```powershell
dotnet build Manufacture.slnx -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\
```

### Tests

```powershell
dotnet test Manufacture.Tests/Manufacture.Tests.csproj --no-build -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\
```

Add unit tests for:

- Category/product type validation and uniqueness.
- Product type selection returning baseline and output noun.
- Raw-material filtering excludes Fuel and Utilities.
- g → kg, ml → L, pcs → pack conversions.
- Density conversion for oil/milk/liquid flavour.
- Recipe batch cost and unit cost with no packaging/labor.
- Recipe save rejects unconvertible quantities.
- Batch creation snapshots total cost and unit cost correctly.
- Profitability report excludes overhead.

### Database deployment

```powershell
powershell -ExecutionPolicy Bypass -File scripts/deploy-database.ps1
```

### Manual UI checks

1. Settings → Production:
   - Create/edit/toggle categories.
   - Create/edit/toggle product types.
   - Verify Bread/Water/Snacks seed data.
2. Recipes Create:
   - Product Type dropdown lists DB product types.
   - Selecting Bread fills baseline `1 Bag of Flour (50 kg)` and output `loaves`; both are disabled.
   - Selecting Water fills `1 Production Run` / `bottles`.
   - Selecting Popcorn fills `1 Cooking Pot (~2 kg corn)` / `bags`.
   - No Handling/Labor fields are present.
   - Raw-material options contain only product names and exclude Fuel/Utilities.
   - Enter a g/kg or ml/L quantity and confirm cost preview and saved cost.
3. Recipes Edit:
   - Existing recipe loads the correct product type/baseline/output.
   - Changing product type updates disabled fields.
4. Recipe save:
   - Valid recipe persists with recipe lines in the database.
   - Invalid conversion shows an error and does not save.
5. Production batches:
   - Create a batch from a DB recipe.
   - Confirm total batch cost and unit cost are material-cost/yield only.
6. Pricing:
   - Update an inventory item price.
   - Confirm recipe/batch margins re-calculate without overhead.
7. Production dashboard:
   - Recipes and batches come from the database.
   - Demand comes from database pre-orders.
8. Regression:
   - Sales product catalog still lists recipes.
   - Existing orders and recipe references still open.
   - Dashboard no longer uses `MockProductionService`.

---

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Dropping `PackagingCost` and `LaborAndOverheadPerUnit` is destructive | Take a database backup; deploy to LocalDB first; verify migration mapping before production deploy. |
| Removing `Ingredients` table breaks unseen references | Search entire solution for `Ingredients`/`IngredientId`; update EF mappings and InventoryService recipe usage before dropping. |
| Unit conversions silently fall back to raw quantity | Replace fallback with explicit validation errors; add tests for unconvertible entries. |
| Density missing for a liquid | Require density when a mass↔volume conversion is needed; block save with a clear message. |
| Existing recipes have no ingredient lines | Seed recipe lines from the current mock definitions and map by inventory item code; verify each recipe’s total cost. |
| Product type deactivation used by existing recipes | Do not delete product types; soft-deactivate and prevent new selection. Existing recipes retain their FK. |
| `RecipeCostingEngine` is synchronous and depends on mock service | Refactor to async DB-backed production service or pure calculator with loaded data; update all callers. |
| Sales/Analytics still reference recipe enum fields | Update mappings to new ProductTypeId/name/output fields and run full solution build/tests. |

---

## Acceptance Criteria

- [ ] All Production pages use database-backed services; no Production page or production-related dashboard path uses `MockProductionService`.
- [ ] Production categories `Bread`, `Water`, `Snacks` exist and are manageable in Settings.
- [ ] Product types map to categories with name, baseline, and output unit noun.
- [ ] Recipe Create/Edit Product Type is populated from the database.
- [ ] Baseline Unit and Output Unit Noun auto-populate and are disabled.
- [ ] Handling/Labor fields are gone from UI, DTOs, calculations, and schema.
- [ ] Raw-material dropdown lists inventory items except Fuel/Utilities and shows only names.
- [ ] Recipe quantities are converted using package size and density; saved costs match the conversion.
- [ ] Unit production cost = total batch material cost / expected yield.
- [ ] Existing seven recipes remain available and are linked to correct product types.
- [ ] Recipe ingredient lines are seeded/migrated and recipe costing works.
- [ ] Production batches can be created and costed from database recipes.
- [ ] Build is clean and all tests pass.

---

## Open Items / Assumptions

- The category is `Snacks` while the product type is `Popcorn`; the current enum name `Popcorn` is retained as the product type name.
- `Recipes.OutputUnit` remains a saved snapshot to avoid unnecessary Sales/Analytics churn. If the product type’s output noun changes, existing recipes keep their original snapshot unless an explicit refresh action is added.
- The `Ingredients` table is assumed to be unused and safe to drop after migration; current database inspection shows zero rows.
- Recipe ingredient seed data will be derived from the existing mock recipe definitions and mapped by inventory item code/name.
- Existing recipe rows keep their current names/codes/yields/selling prices; only the product type and cost formula change.
