# Plan: Product Repositories, Multi-Product Batches & Rep Reconciliation

**Status**: Implemented (2026-11-26)
**Created**: 2026-11-25
**Owner**: Production, Sales/POS, Inventory, Reports

## Summary (TL;DR)

This update introduces a finished-goods stock layer for the factory/mall, redesigns production batches to hold **multiple product lines**, and replaces the route-dispatch/reconciliation workflow with a **new per-rep reconciliation model**.

Core outcomes:

1. **Product Repositories** (Factory Store, Mall Store, …) hold finished-goods stock per product with a reorder threshold and a full in/out movement history. One repository is the default production-output target.
2. **Production batches** are header + multiple product lines (`Recipe`, `TargetQuantity`). A special form has no Actual Output field. A **Register Actual Outcome** dialog on batch details writes each line's actual quantity and increments the default factory repository, logging one movement per product.
3. **Transfers** move stock between repositories (Factory → Mall) and write both sides of the ledger.
4. **POS terminals** link to a Product Repository. A POS only lists/sells products in its linked repository.
5. **POS sales** deduct stock only when the user explicitly triggers **Print Receipt & Deduct** on the receipt page (server action, idempotent). Insufficient stock blocks the action.
6. **Route Dispatch Bay is retired** (page, DTOs, services, domain types, DB tables `DailyRepRouteSheets`, `RouteSheetItems`, `RouteSheetSupermarkets`, `RouteSheetSupermarketDebts`) and replaced with `SalesReconciliations` + `SalesReconciliationItems`.
7. **Reconciliation** is per sales rep per day: opening = prior reconciled day's closing, sold prefilled from that day's deducted orders, and the rep keys Sold / Damaged / Replacement. Cash collection is only `Cash Handed Over` and `POS Terminal Receipts`. Damaged and replacement units post out-bound stock movements.
8. **Orders** lose print/dispatch columns and copy; Orders/Create becomes a simplified POS screen; the receipt page loses the Gate Loading Rule and Store Loader Sign-Off & Dispatch sections.
9. Reports and the SuperAdmin dashboard move off route sheets onto the new reconciliation and batch-line tables.

## Scope

**In scope**
- New repository, repository stock, stock movement, production batch line, and sales reconciliation tables.
- Repository admin UI under `/Production/Repositories` (list, create, edit, stock setup, threshold, transfer, movement history).
- Multi-product batch create/details/index and actual-outcome dialog.
- POS terminal → repository link in create/edit/index.
- Simplified `/Sales/Orders/Create`; new receipt "Print & Deduct" flow.
- New per-rep `/Sales/Reconciliation` page and service.
- Removal of the Route Dispatch Bay page, nav link, services, DTOs, domain types, DB tables, seed data, and dashboard links.
- Migration of report/dashboard queries to reconciliation + batch-line data.
- Seed/backfill scripts, RBAC/nav updates, tests, and verification.

**Out of scope**
- Historical route-sheet data preservation. The route-sheet tables are dropped; existing route-sheet rows are not migrated into reconciliations. Export first if the data must be retained.
- Changing supermarket account credit/pricing/supply behavior beyond removing links to the dispatch/reconciliation debt register.
- Editing a batch's actual outcome after it is completed (corrections would need a later adjustment feature).
- A full purchasing/receiving module for finished goods; repositories are fed by production output, transfers, and manual adjustments only.
- New RBAC module keys. Repositories live under the existing `Production` module; reconciliation stays under `Sales`.
- Multi-warehouse reservation/allocation logic. Stock is deducted directly on print/deduct and on reconciliation posting.

## User Requirements (verbatim, condensed)

- "The factory should have a product repository that tracks all the product that is produced in the factory… the system should be able to know how many product that is available and a threshold should be available for each product produced so the production guys can easily take note and prepare for a batch production."
- "The production batch form in `/Production/Batches/Create` should be able to take in multiple Product Recipe and they should be Target Quantity for each… there will be no Actual Output Produced field."
- "After the production batch is over, they should be a button to register actual outcome value for each selected produced product in that batch… a dialog listing out each selected product and a field to enter each of their Actual Output Produced value."
- "The save action… should update the batch production actual value for each selected product and also increment the product in the factory product repository and not the mall, they should be history for every factory product repository increment."
- "There should be a product item repository setup at the Production module… user can manage and add multiple repository but the system should be able to know the total number of items across all the repository."
- "The reconciliation page for sales reps to key in closing stock pointing to the list of product that was available during open stock with a field for them to key in the total sold for that day."
- "During the reconciliation… the sales rep can enter the Qty Sold, Damaged and Replacement. The Active Stock should show which is the closing stock of yesterday and it save as the opening stock for today."
- "The cash collection in the reconciliation page should only be Cash Handed Over and POS Terminal Receipts."
- "The reconciliation page for sales rep POS in the mall should list out only the unique product items in the mall directory and the same to the sales rep POS in the factory."
- "At `/Sales/Terminals/Create`, it should contain a link up field to the product item directories listing out the directories in a dropdown."
- "Remove the Route Dispatch Bay including its database model."
- "The reconciliation database model should be created and page should be for each sales rep so each of them can do their reconciliation."
- "Deduct when the sales rep has placed the order and printed a receipt."
- "All sales done in `/Sales/Orders/Create` will be deducted from the repository where the sales rep POS is assigned to."
- "When an order is placed, the page redirects to `/Sales/Orders/Receipt/`… remove the Store Loader Sign-Off & Dispatch section and the Gate Loading Rule alert."
- "At `/Sales/Orders`, remove Loading Slip column and Dispatch column. The order should not be marked as printed or not printed anymore."
- Orders/Create: "Channel & Customer Account section is not really necessary"; product input should be a searchable dropdown; POS Register Terminal and Order Flow Type on top; include Remarks in the Order Settlement Summary; "give it a very new look and simple point of sales look."

## Decisions (verbatim answers from alignment)

| Topic | Decision |
|---|---|
| Deduction trigger | **"On an explicit Print & Deduct action"** — order is saved first, then the receipt's server post deducts and prints. |
| Repository vs directory | **"Yes, one concept"** — a repository is both the stock location and the POS product directory. |
| Stock model | **"Per-repo stock + transfers"** — per-repo quantity + threshold + movement ledger; factory→mall transfer supported. |
| Batch output target | **"The default repository which is the factory repository receives the stocks"** and every incoming/outgoing move must be historical. |
| Reconciliation opening | **"Prior closing + prefilled sold"** — opening rolls from prior closing; sold prefilled from today's orders but editable. First-ever sheet allows manual opening. |
| Route sheet removal | **Option 1** — retire route sheets fully, migrate reports/dashboards to new reconciliation tables. **"Each reconciliation is saved by a particular sales rep."** |
| Supermarket debt register | **"Drop it with the dispatch bay."** |
| Orders/Create | Separate simple POS design; remove Channel & Customer Account; searchable product dropdown; POS + Order Flow Type on top; Remarks in settlement; fresh POS look. |
| Damage/replacement stock | **"Adjust stock at reconciliation"** — post out-bound movements with idempotent delta handling. |
| Pre-orders | **"Keep pre-orders, deduct at collection print"** — advance-slip printing does not deduct; collection print does. |
| Low stock | **"Block and show error"** — no negative stock; order stays saved. |
| Reconciliation cycle | **One per rep/day, opening editable only on the first-ever sheet.** |

## Data Model

All paths are repository-relative. Follow `Manufacture.Database/README.md` conventions: `dbo`, plural PascalCase, `Id INT IDENTITY`, named PK/FK/UQ/IX/DF/CK, `DECIMAL(18,3)` quantities, `DECIMAL(18,2)` money, `SYSUTCDATETIME()` timestamps, `ROWVERSION` on aggregates.

### New tables

**`ProductRepositories`**
- `Id INT IDENTITY PK`
- `Code NVARCHAR(30) NOT NULL` (unique `UQ_ProductRepositories_Code`)
- `Name NVARCHAR(120) NOT NULL`
- `LocationType INT NOT NULL DEFAULT 0` — `0 = Factory`, `1 = Mall` (`CK_ProductRepositories_LocationType`)
- `Address NVARCHAR(300) NOT NULL DEFAULT N''`
- `IsDefaultProductionOutput BIT NOT NULL DEFAULT 0`
- `IsActive BIT NOT NULL DEFAULT 1`
- `CreatedAt`, `UpdatedAt`, `RowVersion`
- Filtered unique index `UX_ProductRepositories_DefaultOutput` on `IsDefaultProductionOutput` `WHERE IsDefaultProductionOutput = 1`.
- `CK_ProductRepositories_DefaultIsFactory`: `IsDefaultProductionOutput = 0 OR LocationType = 0`.

**`ProductRepositoryStocks`**
- `Id INT IDENTITY PK`
- `ProductRepositoryId INT NOT NULL` → `ProductRepositories` (`ON DELETE CASCADE`)
- `RecipeId INT NOT NULL` → `Recipes` (`NO ACTION`)
- `QuantityOnHand DECIMAL(18,3) NOT NULL DEFAULT 0`
- `ReorderThreshold DECIMAL(18,3) NOT NULL DEFAULT 0`
- `CreatedAt`, `UpdatedAt`, `RowVersion`
- Unique `UQ_ProductRepositoryStocks_Repo_Recipe` (`ProductRepositoryId`, `RecipeId`)
- `CK_ProductRepositoryStocks_Quantities`: both `>= 0`
- Index `IX_ProductRepositoryStocks_RecipeId`.

**`ProductRepositoryMovements`**
- `Id BIGINT IDENTITY PK`
- `ProductRepositoryId INT NOT NULL` → `ProductRepositories`
- `RecipeId INT NOT NULL` → `Recipes`
- `MovementType INT NOT NULL` — `0 ProductionIn, 1 TransferOut, 2 TransferIn, 3 SaleOut, 4 DamageOut, 5 ReplacementOut, 6 AdjustmentIn, 7 AdjustmentOut, 8 OpeningBalance` (`CK_ProductRepositoryMovements_Type`)
- `Quantity DECIMAL(18,3) NOT NULL` (`> 0`)
- `BalanceAfter DECIMAL(18,3) NOT NULL` (`>= 0`)
- `ReferenceType NVARCHAR(40) NOT NULL DEFAULT N''` — `ProductionBatch`, `Order`, `Transfer`, `Reconciliation`, `Manual`
- `ReferenceId INT NULL`
- `ReferenceNumber NVARCHAR(40) NOT NULL DEFAULT N''`
- `Notes NVARCHAR(300) NOT NULL DEFAULT N''`
- `PerformedBy NVARCHAR(150) NOT NULL DEFAULT N''`
- `CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME()`
- FKs to repository and recipe; indexes `(ProductRepositoryId, RecipeId, CreatedAt DESC)` and `(CreatedAt DESC)`.

**`ProductionBatchItems`**
- `Id INT IDENTITY PK`
- `ProductionBatchId INT NOT NULL` → `ProductionBatches` (`ON DELETE CASCADE`)
- `RecipeId INT NOT NULL` → `Recipes`
- `RecipeName NVARCHAR(150) NOT NULL DEFAULT N''`
- `ProductCode NVARCHAR(40) NOT NULL DEFAULT N''`
- `TargetQuantity INT NOT NULL DEFAULT 0` (`>= 0`)
- `ActualQuantity INT NOT NULL DEFAULT 0` (`>= 0`)
- `Variance AS (ActualQuantity - TargetQuantity) PERSISTED`
- `BatchInputQuantity DECIMAL(18,3) NOT NULL DEFAULT 0`
- `BatchInputUnitLabel NVARCHAR(60) NOT NULL DEFAULT N''`
- `OutputUnitLabel NVARCHAR(40) NOT NULL DEFAULT N''`
- `TotalBatchCost DECIMAL(18,2) NOT NULL DEFAULT 0`
- `ExpectedUnitCost DECIMAL(18,4) NOT NULL DEFAULT 0`
- `CalculatedUnitCost DECIMAL(18,4) NOT NULL DEFAULT 0`
- `ActualRecordedAt DATETIME2(3) NULL`
- `CreatedAt`, `UpdatedAt`
- Unique `UQ_ProductionBatchItems_Batch_Recipe` (`ProductionBatchId`, `RecipeId`); index `IX_ProductionBatchItems_RecipeId`.

**`SalesReconciliations`**
- `Id INT IDENTITY PK`
- `ReconciliationNumber NVARCHAR(40) NOT NULL` (unique)
- `ReconciliationDate DATE NOT NULL`
- `SalesRepId INT NOT NULL` → `SalesReps`
- `PosTerminalId INT NULL` → `PosTerminals`
- `ProductRepositoryId INT NULL` → `ProductRepositories`
- `RouteName NVARCHAR(120) NOT NULL DEFAULT N''` (snapshot)
- `VehicleRegistration NVARCHAR(40) NOT NULL DEFAULT N''` (snapshot)
- `CashHandedOver DECIMAL(18,2) NOT NULL DEFAULT 0`
- `PosTerminalReceipts DECIMAL(18,2) NOT NULL DEFAULT 0`
- `TotalCollected AS (CashHandedOver + PosTerminalReceipts) PERSISTED`
- `TotalExpectedRevenue DECIMAL(18,2) NOT NULL DEFAULT 0`
- `Variance AS (TotalExpectedRevenue - (CashHandedOver + PosTerminalReceipts)) PERSISTED`
- `Status INT NOT NULL DEFAULT 0` — `0 Draft`, `1 Reconciled` (`CK_SalesReconciliations_Status`)
- `StockPostedAt DATETIME2(3) NULL` — set once damage/replacement movements are posted
- `Notes NVARCHAR(500) NOT NULL DEFAULT N''`
- `ReconciledBy NVARCHAR(150) NULL`, `ReconciledAt DATETIME2(3) NULL`
- `CreatedAt`, `UpdatedAt`, `RowVersion`
- Unique `UQ_SalesReconciliations_Rep_Date` (`SalesRepId`, `ReconciliationDate`) — one per rep/day.
- Index `IX_SalesReconciliations_Date_Status`.

**`SalesReconciliationItems`**
- `Id INT IDENTITY PK`
- `SalesReconciliationId INT NOT NULL` → `SalesReconciliations` (`ON DELETE CASCADE`)
- `RecipeId INT NOT NULL` → `Recipes`
- `ProductName NVARCHAR(150) NOT NULL DEFAULT N''`, `ProductCode NVARCHAR(40) NOT NULL DEFAULT N''`
- `UnitPrice DECIMAL(18,2) NOT NULL DEFAULT 0`
- `OpeningStock INT NOT NULL DEFAULT 0`
- `QtySold INT NOT NULL DEFAULT 0`
- `QtyDamaged INT NOT NULL DEFAULT 0`
- `QtyReplacement INT NOT NULL DEFAULT 0`
- `ClosingStock AS (OpeningStock - ((QtySold + QtyDamaged) + QtyReplacement)) PERSISTED`
- `ExpectedRevenue AS (QtySold * UnitPrice) PERSISTED`
- `CreatedAt`, `UpdatedAt`
- Unique `UQ_SalesReconciliationItems_Reconciliation_Recipe`
- `CK_SalesReconciliationItems_Quantities`: all inputs `>= 0` **and** `OpeningStock >= QtySold + QtyDamaged + QtyReplacement`.

### Modified tables

**`PosTerminals`**
- Add `ProductRepositoryId INT NULL` → `ProductRepositories`, filtered index, `NO ACTION`.
- Create/Edit require a repository in the UI; existing rows are backfilled to the default factory repository. If a legacy POS has none, selling is blocked with "Link a product repository to this terminal."

**`Orders`**
- Add `ProductRepositoryId INT NULL` → `ProductRepositories` (snapshot of the repository used).
- Add `StockDeductedAt DATETIME2(3) NULL`, `StockDeductedBy NVARCHAR(150) NULL`.
- Drop `IsPrintedForLoading`, `PrintedAt`, `VerifiedByLoaderName`, `LoadedAt`.
- Extend `CK_Orders_Status` to `0..6` for new `OrderStatus.Completed = 6`.

**`ProductionBatches`**
- Keep the table as the batch header. Header `TargetQuantity` and `ActualQuantity` become **denormalized totals** of the lines (so existing aggregate dashboards keep working).
- Make `RecipeId` nullable (multi-product batches have no single recipe); keep `RecipeName` as a display summary ("Jumbo Family Bread + 1 more") or the single recipe name.
- Add `CompletedAt DATETIME2(3) NULL`; set when actuals are recorded.
- Existing single-recipe rows are backfilled into `ProductionBatchItems`; the legacy header columns stay in place for compatibility and historical rows.

### Dropped tables / types

Drop, in FK-safe order, `RouteSheetSupermarketDebts`, `RouteSheetSupermarkets`, `RouteSheetItems`, `DailyRepRouteSheets`.

Delete/refactor:
- `Manufacture.Library/Models/Entities/DailyRepRouteSheet.cs`, `RouteSheetItem.cs`, `RouteSheetSupermarket.cs`, `RouteSheetSupermarketDebt.cs`.
- `DbSet` entries and Fluent blocks in `ManufactureDbContext.cs:27`, `:89-93`, `:358-422`, `:1468-1544`.
- Navigation properties in `FleetDispatch.cs:38`, `PosTerminal.cs:34`, `Recipe.cs:44`, `SalesRep.cs:38`, `SupermarketAccount.cs:40-42`, `Vehicle.cs:44`.
- `Manufacture.Library/Models/Domain/RouteSheetEntities.cs` route types (`RouteSheetStatus`, `RouteSKUItem`, `RouteSheetSupermarketLink`, `RouteSheetSupermarketDebt`, `DailyRepRouteSheet`). Move `SupermarketAccount` into `SalesEntities.cs` (or a new `SupermarketEntities.cs`) before deleting the file.
- Route DTOs in `Manufacture.Library/Models/DTOs/SalesDtos.cs` (`SignOffLoadingInputDto`, `ReconciliationInputDto`, `SalesRepRouteDebtorDto`, `SalesRepRouteDebtorLineDto`).
- `Manufacture.Web/Pages/Sales/Dispatch/` (entire folder).
- `RouteSupermarketDebtCalculator` and its tests (only used by the dropped debt register).

## Implementation Steps

### Phase 1 — Database and EF foundation

1. **Create repository tables and stock ledger.**
   - *Details*: Add `Manufacture.Database/Tables/ProductRepositories.sql`, `ProductRepositoryStocks.sql`, `ProductRepositoryMovements.sql`; add `<Build Include="Tables/...sql" />` to `Manufacture.Database/Manufacture.Database.sqlproj`.
   - Add `Models/Entities/ProductRepository.cs`, `ProductRepositoryStock.cs`, `ProductRepositoryMovement.cs`; add `DbSet`s and Fluent blocks in `ManufactureDbContext.cs` (follow the `PosTerminals`/`InventoryItems` patterns).
   - *Dependencies*: None.

2. **Create production batch lines.**
   - *Details*: Add `ProductionBatchItems.sql`, entity, `DbSet`, Fluent block. Alter `ProductionBatches.sql`: nullable `RecipeId`, add `CompletedAt`. Keep legacy columns.
   - *Dependencies*: Parallel with Step 1.

3. **Create reconciliation tables.**
   - *Details*: Add `SalesReconciliations.sql`, `SalesReconciliationItems.sql`, entities, `DbSet`s, Fluent blocks, enum mappings.
   - *Dependencies*: Parallel with Steps 1-2.

4. **Alter POS terminals and orders.**
   - *Details*: Add `PosTerminals.ProductRepositoryId`; add `Orders.ProductRepositoryId`, `StockDeductedAt`, `StockDeductedBy`; drop the four print/loader columns; extend the status check to `0..6`. Update matching entities (`PosTerminal.cs`, `Order.cs`) and `ManufactureDbContext` Fluent blocks at `:819-892`, `:1097-1131`.
   - *Dependencies*: Depends on Step 1.

5. **Retire route-sheet schema and types.**
   - *Details*: Delete the four table scripts and their `.sqlproj` entries. Remove the C# entities, DbSets, Fluent blocks, navigation properties, route domain types/DTOs. Move `SupermarketAccount` out of `RouteSheetEntities.cs` first.
   - *Dependencies*: Depends on Step 3 (new reconciliation tables exist).

6. **Seed, backfill, and clean seed data.**
   - *Details*: In `scripts/seed-database.sql`:
     - Seed `FAC-01` "Factory Finished Goods Store" (`LocationType=0`, `IsDefaultProductionOutput=1`) and `MALL-01` "Mall Retail Store" (`LocationType=1`).
     - Seed a `ProductRepositoryStocks` row for every active recipe in the factory repository (`QuantityOnHand=0`, `ReorderThreshold` default e.g. 20).
     - Backfill `PosTerminals.ProductRepositoryId` to the default factory repository where null.
     - Backfill `ProductionBatchItems` from each legacy `ProductionBatches` row where no line exists.
     - Remove all route-sheet seed blocks (`seed-database.sql:1141`, `:1534-1568`) and remove `IsPrintedForLoading`/`PrintedAt`/`VerifiedByLoaderName`/`LoadedAt` from every `INSERT INTO Orders` (`:1404-1424`).
     - Keep the script idempotent (`IF NOT EXISTS`/`MERGE`).
   - *Dependencies*: Depends on Steps 1-5.

### Phase 2 — Repository service and admin UI

7. **Add repository DTOs, domain models, and stock ledger.**
   - *Details*: Add `Models/DTOs/ProductRepositoryDtos.cs` and domain enums/models (`ProductRepositoryType`, `ProductStockMovementType`, `ProductRepository`, `ProductRepositoryStock`, `ProductStockMovement`). Add `Interfaces/IProductRepositoryService.cs` and `Web/Services/ProductRepositoryService.cs`:
     - `GetRepositoriesAsync`, `GetRepositoryAsync`, `CreateRepositoryAsync`, `UpdateRepositoryAsync`, `ToggleRepositoryStatusAsync`, `SetDefaultProductionOutputAsync`
     - `GetStockAsync`, `GetProductTotalsAcrossRepositoriesAsync`, `AddOrUpdateStockItemAsync`
     - `AdjustStockAsync`, `TransferStockAsync`, `GetMovementsAsync`
     - `GetProductsForRepositoryAsync`
   - Add a scoped `ProductStockLedger` helper (or private service class) with one method that loads a stock row, validates the quantity, updates `QuantityOnHand`, and appends a `ProductRepositoryMovements` row **without calling `SaveChanges`** so the caller commits atomically.
     - Inbound movement types (`ProductionIn`, `TransferIn`, `AdjustmentIn`, `OpeningBalance`) create the `ProductRepositoryStocks` row if the product is not yet in that repository.
     - Outbound movement types (`TransferOut`, `SaleOut`, `DamageOut`, `ReplacementOut`, `AdjustmentOut`) require an existing row with enough quantity and throw a descriptive `InvalidOperationException` otherwise.
   - *Dependencies*: Depends on Phase 1.

8. **Build `/Production/Repositories` admin pages.**
   - *Details*: `Pages/Production/Repositories/Index.cshtml(.cs)`, `Create`, `Edit`, `Details`.
     - Index: repository cards/table (code, name, type, default badge, SKU count, total units, low-stock count) + a consolidated "all repositories" product-total table (total on hand, total threshold, low-stock flag).
     - Create/Edit: code, name, location type, address, active, and "Default production output" (factory only; saving clears the previous default).
     - Details: product stock table (search/add product, on-hand, threshold, status), inline threshold/qty edit, adjustment action, transfer action (from this repo → another, product, qty, notes), and movement history table.
   - *Dependencies*: Depends on Step 7.

9. **Wire navigation and permissions.**
   - *Details*: Add "Product Repositories" to the Production nav in `Pages/Shared/_Layout.cshtml` (around lines 199-234), visible with `Rbac.HasAccess(userRole, "Production")`. Repositories use the existing `Production` RBAC module; edit/transfer controls require `Rbac.CanEdit(userRole, "Production")` or SuperAdmin.
   - *Dependencies*: Depends on Step 8.

10. **Link POS terminals to repositories.**
    - *Details*: Add `ProductRepositoryId` to `PosTerminalDto`; update `SalesService.CreateTerminalAsync`/`UpdateTerminalAsync`/`MapTerminal`. Update `Pages/Sales/Terminals/Create.cshtml(.cs)`, `Edit`, `Index`, and `Assign` to load active repositories, require a selection on create/edit, show the linked repository on cards/read-only summary. Default the dropdown to the default production-output repository.
    - *Dependencies*: Parallel with Step 8; depends on Step 7.

### Phase 3 — Multi-product production batches

11. **Reshape production DTOs and domain models.**
    - *Details*:
      - Replace `ProductionBatchDto` scalar `RecipeId`/`TargetQuantity`/`ActualQuantity` usage with a create DTO (`Shift`, `BakerInCharge`, `ProductionDate`, `Notes`, `List<ProductionBatchLineInputDto> Lines`) and a `RecordBatchActualsDto` (`BatchId`, `List<BatchActualLineDto> Lines` with `LineId`, `ActualQuantity`).
      - Add `ProductionBatchLine` to `Models/Domain/ProductionEntities.cs` and `List<ProductionBatchLine> Lines` to `ProductionBatch`; expose aggregate `TargetQuantity`/`ActualQuantity` from lines when present, else legacy header values.
      - Keep `ProductionDefectDto` but add `RecipeId`/`ProductName` so defects can target a line.
    - *Dependencies*: Depends on Phase 1.

12. **Update `ProductionService` for multi-line batches and output stock.**
    - *Details* (`Web/Services/ProductionService.cs`):
      - `CreateBatch`: validate 1+ distinct lines, per-line target > 0, active recipes; insert header (`Status = Planned`, legacy aggregates, `RecipeId = first line recipe`) + `ProductionBatchItems`; compute each line's `BatchInputQuantity = target / recipe.ExpectedYield`, material cost, expected/calculated unit cost; set header totals.
      - `GetAllBatches`/`GetBatchById`: include `ProductionBatchItems.Recipe` and map to domain lines.
      - Add `RecordBatchActuals(batchId, IReadOnlyDictionary<int,int> actualByLineId, string recordedBy)`: reject if batch is `Completed`/`Cancelled`; validate non-negative; update each line `ActualQuantity`, `ActualRecordedAt`, line unit cost; update header totals; set `Status = Completed`, `CompletedAt`; resolve the active repository with `IsDefaultProductionOutput`; call `ProductStockLedger` for each line with `ActualQuantity > 0` as `ProductionIn` (reference `ProductionBatch`/`batch.Id`/`BatchNumber`); save once in a transaction.
      - `SaveBatchDefects`: accept `recipeId` and validate it belongs to the batch; keep aggregate-per-line semantics or allow one row per line.
      - `DeleteRecipe` blocker (`ProductionService.cs:378-393`): add `ProductionBatchItems` and `ProductRepositoryStocks` checks; replace the `RouteSheetItems` check.
    - *Dependencies*: Depends on Steps 1-2, 7, 11.

13. **Redesign batch pages.**
    - *Details*:
      - `Pages/Production/Batches/Create.cshtml(.cs)`: new header (shift, baker, date, notes) + repeatable line editor (`Product Recipe` searchable/select, `Target Quantity`, calculated cost/unit hint). For each selected recipe show `Available in factory` and `Reorder threshold` (from the default factory repository), and offer a suggested target = max(0, threshold − available). Add/remove line buttons; server-side duplicate detection. Remove Actual Output, Status, Batch Input Size, linked-order fields, and the single-recipe calculator endpoint; recalc totals client-side from recipe data.
      - `Pages/Production/Batches/Details.cshtml(.cs)`: line table with target/actual/variance, per-line BOM sections, and a **Register Actual Outcome** `<dialog>` listing every line with an actual input. Post handler `RecordActuals` calls Step 12. After completion, show actuals, variances, and "Stock posted to {repository}" and hide/disable the dialog.
      - `Pages/Production/Batches/Index.cshtml(.cs)`: columns become Batch #, Date/Shift, Products (chips/count), Total Target, Total Actual, Yield, Status, Action; update KPI sums to line-aware aggregation.
      - `Pages/Production/Index.cshtml` and `ProductionDashboardMetrics`: group by batch lines/categories, not header recipe.
    - *Dependencies*: Depends on Step 12.

14. **Update production reporting to line level.**
    - *Details*: Update `ReportQueries` produced/product-performance queries and `SuperAdminDashboardService`/`SuperAdminKpiDetailBuilder` produced-detail builders to read `ProductionBatchItems` (per product) instead of header `RecipeId`/`ActualQuantity`.
    - *Dependencies*: Parallel with Step 13; depends on Step 12.

### Phase 4 — Simplified POS, receipt and stock deduction

15. **Simplify order creation service/DTO.**
    - *Details*:
      - `CreateOrderDto`: keep `OrderType`, `PosTerminalCode`, `SalesRepId`, `Items`, `Discount`, `AmountPaid`, `PaymentMethod`, `Notes`; remove the `[Required]` from `CustomerName` and stop binding customer fields in the POS page. Service resolves the default `WLK-001` walk-in customer when `CustomerId <= 0` and writes `CustomerName`/`CustomerCategory` from that record.
      - `SalesService.CreateOrderAsync`: validate the POS exists, is active, and has a linked repository; validate each item recipe exists in that repository's directory; save the order as `Pending` with no stock movement; set `ProductRepositoryId` from the POS; keep payment/credit handling.
      - Remove `MarkPrintedForLoadingAsync` and `VerifyAndDispatchAsync` from `ISalesService`/`SalesService`; add `DeductAndCompleteOrderAsync(int orderId, string performedBy)`.
      - `DeductAndCompleteOrderAsync`: idempotent on `StockDeductedAt`; load order/items/POS/repository; for each item deduct `Quantity + BonusQuantity` via `ProductStockLedger` as `SaleOut` (reference `Order`, `OrderNumber`); throw an `InvalidOperationException` naming the product if a stock row is missing or insufficient; set `Orders.ProductRepositoryId`, `StockDeductedAt/By`, `Status = Completed`, `ClosedAt`; save once.
    - *Dependencies*: Depends on Steps 4, 7.

16. **Redesign `/Sales/Orders/Create`.**
    - *Details* (`Pages/Sales/Orders/Create.cshtml(.cs)`):
      - Top row: `POS Register Terminal` (auto-selected/locked for sales reps) and `Order Flow Type` (`Direct Walk-in Sale`, `Pre-Order`).
      - Remove the `Channel & Customer Account` section entirely; resolve the walk-in customer server-side.
      - Product entry: a searchable combobox (text search + filtered results; no new JS dependency) showing only products in the selected terminal's repository, with available stock and price. Selecting a product adds it to the basket; quantity ± controls remain.
      - Keep a clean basket table and an `Order Settlement Summary` containing payment method, discount, amount paid, totals, and a **Remarks** textarea.
      - Removing a channel removes scheduled delivery date and loader/gate copy.
      - On terminal change, refresh products via a page handler (`OnGetProducts(int terminalId)`) returning JSON.
      - Submit saves the order (no deduction) and redirects to `/Sales/Orders/Receipt?id=...`.
    - *Dependencies*: Depends on Step 15.

17. **Rework the receipt page and print/deduct action.**
    - *Details* (`Pages/Sales/Orders/Receipt.cshtml(.cs)`):
      - Remove the `Gate Loading Rule` alert, the `Store Loader Sign-Off & Dispatch` card, loader sign-off, and all "loading slip/dispatch/gate pass" copy and signature blocks.
      - Title becomes `Receipt`; keep customer/product/payment totals and thermal receipt.
      - Replace `MarkPrinted`/`DispatchOrder` handlers with `OnPostPrintAndDeductAsync(int id)`:
        - Immediate sale or pre-order collection: call `DeductAndCompleteOrderAsync`, set `TempData["AutoPrint"] = true`, redirect to the same receipt.
        - Pre-order advance print (stock not deducted yet): provide a plain client-side `window.print()` button; no server deduction.
      - On GET with `AutoPrint`, emit `window.print()` after load.
      - If already deducted, show a "Reprint Receipt" button (prints only) and a small "Stock deducted" confirmation.
    - *Dependencies*: Depends on Step 15.

18. **Clean order surfaces and pre-order flow.**
    - *Details*:
      - `Pages/Sales/Orders/Index.cshtml(.cs)`: remove `Loading Slip`/`Dispatch` columns and the `Slip Printed`/`Unprinted`/`Dispatched` filters; replace the row action with `Receipt`; show status/payment only. Remove `CanPrintSlip`/print-state logic.
      - `Pages/Sales/Orders/Details.cshtml`: update "Receipt / Loading Slip" copy; add a "Complete Sale & Deduct" action for pending pre-orders that posts to the same service then opens the receipt. Change `IsOpenStatus`/`IsOrderOpen` so `Completed` is a terminal state and `Pending`/`Confirmed` remain open.
      - `Pages/Sales/PreOrderSchedule/Index.cshtml(.cs)` and `Details`: remove the `Loading Slip`/`Dispatch` columns and the `Dispatched`/`Slip Printed`/`Unprinted` filters and `IsPrintedForLoading` row attributes; add "Collect & Deduct" for scheduled pre-orders when collected; keep status/payment columns.
      - `Pages/Dashboard/Index.cshtml(.cs)` and `MockSalesService`: remove `IsPrintedForLoading` usage, the unprinted-orders KPI, the supermarket-debtors card, and `MyRouteDebtors`; replace with reconciliation/repository summaries where useful.
      - `ReportQueries.cs:457`: replace the print-state filter with order status/settlement logic.
    - *Dependencies*: Parallel with Step 17.

### Phase 5 — Per-rep reconciliation and route retirement

19. **Add reconciliation DTOs, domain types, and service.**
    - *Details*: Add `Models/DTOs/SalesReconciliationDtos.cs` (`SalesReconciliationDto`, `SalesReconciliationItemDto`, `SaveReconciliationDto`, `ReconciliationSummaryDto`). Add `SalesReconciliationStatus` (`Draft=0, Reconciled=1`) and domain models.
    - Add `Interfaces/ISalesReconciliationService.cs` + `Web/Services/SalesReconciliationService.cs`:
      - `GetOrCreateAsync(int salesRepId, DateOnly date)`: resolve rep → POS (`SalesReps.PosTerminalId`, fallback `PosTerminals.AssignedEmployeeId`) → `ProductRepositoryId`; create a `SalesReconciliations` row (one per rep/day) and items for every unique product in that repository; opening = latest prior **Reconciled** reconciliation's `ClosingStock` for the same rep/POS/recipe (0 if none); sold = sum of `OrderItems.Quantity` for orders of that rep/POS with `StockDeductedAt` on that date (excluding cancelled); unit price from `Recipes.SellingPrice`.
      - `SyncProductsAsync(int reconciliationId)`: add items for repository products missing from a draft sheet.
      - `ListAsync(DateOnly date, int? salesRepId)`, `GetAsync(int id)`.
      - `SaveDraftAsync` / `ReconcileAsync`: validate quantities, recompute `TotalExpectedRevenue`; accept submitted `OpeningStock` values only when the sheet has no prior reconciled sheet, otherwise ignore/retain the rolled-forward opening. `ReconcileAsync` also posts `DamageOut`/`ReplacementOut` movements once (guarded by `StockPostedAt`), sets status/reconciled-by/at, and never reposts on a second call.
      - Enforce rep ownership in the page; admins may view all reps.
    - *Dependencies*: Depends on Steps 3, 7.

20. **Rewrite `/Sales/Reconciliation` page.**
    - *Details* (`Pages/Sales/Reconciliation/Index.cshtml(.cs)`):
      - Left roster: date filter and reconciliations for the date (admins see all reps, reps see only their own). "Start Reconciliation" creates the rep/day sheet.
      - Right sheet header: rep, POS, repository, date, status, snapshot route/vehicle.
      - Stock table columns: Product, **Active Stock (Opening = yesterday's closing)**, Qty Sold, Damaged, Replacement, Closing, Unit Price, Expected Value. Active Stock is editable only when no prior reconciled sheet exists; otherwise read-only. Show system-deducted sold as a hint. Saving writes today's Closing as tomorrow's Opening.
      - Payment panel: only `Cash Handed Over` and `POS Terminal Receipts`; show total collected, expected revenue and variance.
      - Remarks/notes + `Save Draft` and `Reconcile & Close` buttons. Reconciled sheets are read-only.
      - Remove the supermarket debt register, prior-debt fields, route-sheet selectors, and route/dispatch links.
    - *Dependencies*: Depends on Step 19.

21. **Remove route-sheet services, pages, mocks and links.**
    - *Details*:
      - Delete `Pages/Sales/Dispatch/`.
      - Remove route methods from `ISalesService`/`SalesService` (`GetRouteSheetsAsync`, `GetRouteSheetAsync`, `CreateRouteSheetAsync`, `UpdateRouteSheetSupermarketsAsync`, `SaveRouteSheetSupermarketDebtsAsync`, `GetSalesRepRouteDebtorsAsync`, `SignOffDispatchAsync`, `ReconcileDailyRepAccountAsync`, `DeleteRouteSheetAsync`) and `MapRouteSheet`. Remove `MapOrder` print/loader fields.
      - Remove route seeds/methods from `MockSalesService`.
      - Remove the `Route Dispatch Bay` nav item and the dispatch card in `Pages/Sales/Index.cshtml`; replace the KPI "Loading Slips Issued" with "Stock-deducted orders" or repository/low-stock count.
      - Remove `Daily Dispatch Bay` links in `Pages/Sales/PosAccounts/Index.cshtml` and `Edit.cshtml`.
      - Remove `GetAvailableForSalesRouteAsync` from `IFleetDispatchService`/`FleetDispatchService` and the `FleetDispatch.DailyRepRouteSheet` navigation.
      - Remove `Manufacture.Library/Calculations/FleetDispatchRules.cs` and `Manufacture.Tests/FleetDispatchRulesTests.cs` (only used when creating route sheets).
      - Remove the route-sheet block from `SalesService.RecordCustomerReturnAsync` (`SalesService.cs:1632-1638`); customer returns keep recording the return and balance adjustment, and replacements are entered manually on the new reconciliation.
      - Remove the dashboard supermarket-debtors card and `GetSalesRepRouteDebtorsAsync` usage.
    - *Dependencies*: Depends on Step 20.

### Phase 6 — Reports, dashboards and tests

22. **Migrate report queries.**
    - *Details* (`Web/Services/Reports/ReportQueries.cs`): replace route-sheet reads with reconciliation reads using this mapping:
      - `DailyRepRouteSheets` → `SalesReconciliations`; `RouteSheetItems` → `SalesReconciliationItems`.
      - `RepOrderRequestedQty`/`TodayNewlyLoadedQty` → `OpeningStock` (rename report labels to "Opening"; drop "Requested"/"Fill" where no equivalent).
      - `ClosingStockQty` → `ClosingStock`; `QtyReplacementIssued` → `QtyReplacement`; `UnitSellingPrice` → `UnitPrice`; `ExpectedItemRevenue` → `ExpectedRevenue`.
      - `CashCollected + BankTransferCollected + PosTerminalCollected` → `CashHandedOver + PosTerminalReceipts`; `NewOutstandingAccountBalance` → `Variance`.
      - `RouteSheetRef` → `ReconciliationNumber`; `Status` → `SalesReconciliationStatus`.
      - Update `ExecutiveDailyFunnel`, `FleetVehicleCostRows`, `ExecutiveIntegrityPack`, `ExecutiveAlerts` (low finished-goods stock instead of short loads), `ProductRows`, `RouteRepRows`, `RouteItemRows`.
    - *Dependencies*: Depends on Step 19.

23. **Migrate SuperAdmin dashboard and KPI drawers.**
    - *Details* (`Web/Services/SuperAdminDashboardService.cs`, `SuperAdminKpiDetailBuilder.cs`, `SuperAdminDashboardAggregator.cs`): replace `DispatchLineSource` route-sheet fields with reconciliation fields; `Loaded` maps to `OpeningStock`, `Returned` to `ClosingStock`, `Damaged` stays; URLs point to `/Sales/Reconciliation?reconciliationId={id}`. Remove `SheetStatusLabel` route-sheet switch.
    - Use `ProductionBatchItems` for per-product produced facts.
    - *Dependencies*: Parallel with Step 22.

24. **Update tests.**
    - *Details*:
      - Delete route-sheet/reconciliation tests from `Manufacture.Tests/RouteSheetAndSalesTests.cs`; keep supermarket/terminal tests and adapt terminal tests for `ProductRepositoryId`.
      - Delete `RouteSupermarketDebtCalculatorTests`, the calculator, `FleetDispatchRulesTests`, and `FleetDispatchRules`.
      - Add pure-calculator tests for reconciliation closing/variance and stock deduction/transfer math (extract `ReconciliationMath` and `StockLedgerMath` if helpful, since existing tests use no EF).
      - Update `ProductionDashboardMetricsTests` and `SuperAdminKpiDetailBuilderTests` for batch-line sources.
    - *Dependencies*: Depends on Phases 3-6.

### Phase 7 — Deploy and verify

25. **Build, deploy, and manually walk the flows.**
    - *Details*: Build solution/tests, build the database project, deploy, then execute the manual checks below. Stop any dev server afterwards.
    - *Dependencies*: Depends on all prior steps.

## Relevant Files & Symbols

**Database / EF**
- `Manufacture.Database/Manufacture.Database.sqlproj`: add/remove table Build items.
- `Manufacture.Database/Tables/ProductRepositories.sql`, `ProductRepositoryStocks.sql`, `ProductRepositoryMovements.sql`, `ProductionBatchItems.sql`, `SalesReconciliations.sql`, `SalesReconciliationItems.sql`: new.
- `Manufacture.Database/Tables/PosTerminals.sql`, `Orders.sql`, `ProductionBatches.sql`: alter.
- `Manufacture.Database/Tables/DailyRepRouteSheets.sql`, `RouteSheetItems.sql`, `RouteSheetSupermarkets.sql`, `RouteSheetSupermarketDebts.sql`: delete.
- `Manufacture.Library/Data/ManufactureDbContext.cs`: DbSets, Fluent config, computed columns, remove route blocks (`:27`, `:89-93`, `:358-422`, `:1468-1544`).
- `Manufacture.Library/Models/Entities/*`: new entities; remove route entities and nav properties.
- `scripts/seed-database.sql`: repositories/stock/POS backfill, batch-line backfill, remove route seeds and order print columns.
- `scripts/deploy-database.ps1`: reuse as-is (schema then seed).

**Library models / contracts**
- `Manufacture.Library/Models/Domain/ProductionEntities.cs`: `ProductionBatch`, new `ProductionBatchLine`, `BatchStatus`.
- `Manufacture.Library/Models/Domain/SalesEntities.cs`: `Order` fields, `OrderStatus.Completed`, `PosTerminal`, move `SupermarketAccount` here.
- `Manufacture.Library/Models/Domain/RouteSheetEntities.cs`: delete after moving `SupermarketAccount`.
- `Manufacture.Library/Models/DTOs/ProductionDtos.cs`: create/record-actual DTOs, defect recipe.
- `Manufacture.Library/Models/DTOs/SalesDtos.cs`: `CreateOrderDto`, `PosTerminalDto`, new reconciliation DTOs; remove route DTOs.
- `Manufacture.Library/Models/DTOs/ProductRepositoryDtos.cs`: new.
- `Manufacture.Library/Interfaces/IProductionService.cs`: batch/actual/defect signatures.
- `Manufacture.Library/Interfaces/ISalesService.cs`: remove print/dispatch/route methods; add deduct method.
- `Manufacture.Library/Interfaces/IProductRepositoryService.cs`, `ISalesReconciliationService.cs`: new.
- `Manufacture.Library/Interfaces/IFleetDispatchService.cs`: remove available-for-route method.

**Web services**
- `Manufacture.Web/Services/ProductRepositoryService.cs` + `ProductStockLedger`: new.
- `Manufacture.Web/Services/SalesReconciliationService.cs`: new.
- `Manufacture.Web/Services/ProductionService.cs`: `CreateBatch` (`:515-562`), `RecordBatchActualOutput` (`:622-664`), `SaveBatchDefects` (`:678-734`), `MapBatch` (`:1374-1409`), `DeleteRecipe` blockers (`:378-393`).
- `Manufacture.Web/Services/SalesService.cs`: `CreateOrderAsync` (`:339-531`), print/dispatch methods (`:575-611`), route methods (`:1032-1557`), `MapOrder`/`MapTerminal` (`:2045-2177`).
- `Manufacture.Web/Services/MockSalesService.cs`: remove route sheet seed/methods and print fields.
- `Manufacture.Web/Services/FleetDispatchService.cs`: remove `GetAvailableForSalesRouteAsync` (`:95-114`).
- `Manufacture.Web/Services/Reports/ReportQueries.cs`: route reads at `:109`, `:1256`, `:1887`, `:1915-1916`, `:1994`, `:2081-2116`; batch reads.
- `Manufacture.Web/Services/SuperAdminDashboardService.cs` (`:233`, `:280-320`, `:437-500`, `:547-600`), `SuperAdminKpiDetailBuilder.cs` (`:41-53`, `:280-320`, `:495-510`), `SuperAdminDashboardAggregator.cs`.
- `Manufacture.Web/Program.cs`: register `IProductRepositoryService` and `ISalesReconciliationService` (scoped) near line 101.

**Pages / navigation**
- `Manufacture.Web/Pages/Production/Repositories/` (new Index/Create/Edit/Details).
- `Manufacture.Web/Pages/Production/Batches/Create.cshtml(.cs)`, `Details.cshtml(.cs)`, `Index.cshtml(.cs)`.
- `Manufacture.Web/Pages/Production/Index.cshtml(.cs)`.
- `Manufacture.Web/Pages/Sales/Orders/Create.cshtml(.cs)`, `Receipt.cshtml(.cs)`, `Index.cshtml(.cs)`, `Details.cshtml(.cs)`.
- `Manufacture.Web/Pages/Sales/PreOrderSchedule/Index.cshtml(.cs)`, `Details.cshtml(.cs)`.
- `Manufacture.Web/Pages/Sales/Reconciliation/Index.cshtml(.cs)` (rewrite).
- `Manufacture.Web/Pages/Sales/Dispatch/` (delete).
- `Manufacture.Web/Pages/Sales/Terminals/Create.cshtml(.cs)`, `Edit`, `Index`, `Assign`.
- `Manufacture.Web/Pages/Sales/Index.cshtml` (hub cards/KPIs), `Pages/Sales/PosAccounts/Index.cshtml`, `Edit.cshtml` (remove dispatch links).
- `Manufacture.Web/Pages/Dashboard/Index.cshtml(.cs)`.
- `Manufacture.Web/Pages/Shared/_Layout.cshtml`: add repositories, remove `Route Dispatch Bay` (`:319`).

**Tests**
- `Manufacture.Tests/RouteSheetAndSalesTests.cs`, `RouteSupermarketDebtCalculatorTests.cs`, `ProductionDashboardMetricsTests.cs`, `SuperAdminKpiDetailBuilderTests.cs`, `RbacTests.cs`.

## Verification Plan

1. **Automated**
   - `dotnet build Manufacture.slnx`
   - `dotnet test Manufacture.Tests/Manufacture.Tests.csproj`
   - `dotnet build Manufacture.Database/Manufacture.Database.sqlproj`
   - If `pwsh` is unavailable: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/deploy-database.ps1`.
   - For locked `bin/obj`, use `-p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\`.

2. **Schema checks after deploy**
   - `ProductRepositories` has exactly one row with `IsDefaultProductionOutput = 1` and `LocationType = 0`.
   - Every `PosTerminals` row has a non-null `ProductRepositoryId`.
   - Every legacy `ProductionBatches` row has at least one `ProductionBatchItems` row.
   - Route-sheet tables no longer exist; `Orders` has `StockDeductedAt` and no print/loader columns.
   - `SalesReconciliations` unique index on `(SalesRepId, ReconciliationDate)`.

3. **Manual flows**
   - **Repositories**: create a Mall repository; add products with thresholds; adjust stock; transfer Factory → Mall; confirm both balances and both movement rows.
   - **Batch creation**: create a batch with 2+ recipes and target quantities; no Actual field; details show all lines and totals.
   - **Actual outcome**: click Register Actual Outcome, enter actuals, save; lines show actual/variance, batch becomes Completed, default factory stock increases by each actual, and one `ProductionIn` movement per line references the batch.
   - **POS link**: create/edit a terminal and select a repository; terminal card shows it.
   - **Orders/Create**: select a terminal; only that repository's products appear in the searchable dropdown; submit; redirect to receipt.
   - **Receipt**: immediate sale → Print Receipt & Deduct reduces stock and sets the order Completed; reprint does not deduct twice. Pre-order → advance print does not deduct; Collect & Deduct does.
   - **Insufficient stock**: attempt to sell more than available → blocked with product name and available quantity; order remains saved.
   - **Orders index**: no Loading Slip/Dispatch columns or print filters; receipt action works.
   - **Reconciliation**: rep opens the page; opening equals prior closing; sold prefilled from that day's deducted orders; enter damaged/replacement; save; reconcile; damaged/replacement post out-bound movements once; next day's opening equals today's closing.
   - **Route removal**: `/Sales/Dispatch` returns 404; nav has no Route Dispatch Bay; Sales hub has no dispatch card.
   - **Reports/dashboard**: all report views and SuperAdmin KPI drawers load without route-sheet references and show reconciliation/batch-line values.

4. **Regression**
   - Supermarket account pricing/supply/payment pages still work (without dispatch/debt-register links).
   - Login/RBAC unchanged; SalesRep sees only own orders/reconciliation.
   - Existing orders, recipes, batches, and customers remain readable.

## Context & Decisions Log

- **2026-11-25**: User requested a major update: factory/mall product repositories with thresholds and stock history; multi-recipe production batches with target quantities and an actual-outcome dialog; per-rep reconciliation; remove Route Dispatch Bay and its DB model; POS terminal → repository link; simpler POS page; receipt print/deduct; remove loading-slip/dispatch status.
- **2026-11-25**: Deduction happens on an **explicit Print & Deduct server action** (not at order save, not at client print).
- **2026-11-25**: A product repository and a "product item directory" are the **same concept**.
- **2026-11-25**: Stock is tracked **per repository**, with reorder threshold, movement ledger, and factory→mall transfers.
- **2026-11-25**: Batch output always posts to the **default factory repository**; every receipt and transfer writes history.
- **2026-11-25**: Reconciliation opening rolls from prior closing; Qty Sold is prefilled from orders but editable; one sheet per rep/day; first-ever opening is editable.
- **2026-11-25**: Route sheets are **fully retired** and reports/dashboards migrate to the new reconciliation model. Each reconciliation belongs to one sales rep.
- **2026-11-25**: Supermarket dispatch debt register is **removed** with the dispatch bay.
- **2026-11-25**: Orders/Create is a new simple POS design: no Channel & Customer Account; POS + Order Flow Type at the top; searchable product dropdown; Remarks in settlement.
- **2026-11-25**: Damaged/replacement quantities **post out-bound stock movements at reconciliation**; low stock blocks sales.

## Key Decisions & Trade-offs

| Topic | Decision | Why |
|---|---|---|
| Repository granularity | One repository = stock location + POS product directory | Matches the user's explicit "same concept" decision and avoids a second catalog layer. |
| Stock ledger | Append-only `ProductRepositoryMovements` with `BalanceAfter` | Gives auditable history for production receipts, transfers, sales, damage, replacements, and manual adjustments. |
| Batch schema | Header aggregates + `ProductionBatchItems` lines; legacy columns retained | Supports multi-product batches without breaking existing aggregate dashboards/history; old rows are backfilled. |
| Output repository | Single default factory repository | Explicit user decision; avoids per-batch repository complexity. |
| Deduction point | Explicit server-side Print & Deduct | Client `window.print()` is not a reliable event; idempotent `StockDeductedAt` prevents double deduction. |
| Pre-orders | No deduction on advance print; deduct at collection | Matches the chosen pre-order behavior and avoids consuming stock before collection. |
| Reconciliation stock | Sold deducted at print; damaged/replacement posted at reconcile | Keeps repository balance correct while recording the rep's counts; `StockPostedAt` prevents double posting. |
| Sold edits | Qty Sold is editable/reporting-focused; no re-deduction | The stock was already deducted by receipt; editing only affects the rep's count/revenue figures. |
| Route sheet data | Dropped, not migrated | The dispatch-bay workflow is retired; retaining dual models would keep dead code and reports alive. Export first if historical data is needed. |
| Supermarket debt register | Removed | Explicit user decision; supermarket account credit/payment remains elsewhere. |
| Reconciliation ownership | One row per `(SalesRepId, ReconciliationDate)` | Explicit "each reconciliation is saved by a particular sales rep"; prevents duplicate day sheets. |
| RBAC | Reuse `Production` and `Sales` modules | Avoids adding modules/seed/test churn; pages still enforce module access and rep ownership. |

## Risks / Notes for the Implementing Agent

- **Dacpac drops**: `scripts/deploy-database.ps1` uses `BlockOnPossibleDataLoss=False`. Confirm the target DB is disposable or back up before dropping route-sheet tables and order columns.
- **Seed ordering**: dacpac publish happens before seed. Seed blocks must only reference tables/columns that exist after the new schema deploy.
- **Scaffold**: Do not run `scripts/scaffold-database.ps1 --force` blindly; it overwrites hand-written entity edits. Add the new entities/config by hand, or review the scaffold diff carefully.
- **Atomicity**: `ProductStockLedger` must not call `SaveChanges`; callers commit order status + stock movements together in one transaction.
- **Concurrency**: Batch numbers, order numbers, and reconciliation numbers use count-based generators. Keep the existing pattern but handle unique-constraint collisions where practical.
- **No EF test harness**: New DB-backed behavior needs manual verification; extract pure math for unit tests where possible.

## Implementation Record (2026-11-26)

- **Build**: `dotnet build Manufacture.slnx` — 0 errors.
- **Tests**: `dotnet test Manufacture.Tests/Manufacture.Tests.csproj` — 263 passed, 0 failed.
- **Database**: `dotnet build Manufacture.Database/Manufacture.Database.sqlproj` — 0 errors; `scripts/deploy-database.ps1` published the schema and seeded idempotently.
- **Schema verified**: `ProductRepositories`, `ProductRepositoryStocks`, `ProductRepositoryMovements`, `ProductionBatchItems`, `SalesReconciliations`, `SalesReconciliationItems` exist; route-sheet tables are dropped; `Orders` has `StockDeductedAt`/`StockDeductedBy` and no print/loader columns; every POS row has a repository.
- **Manual flows verified over HTTP**:
  - Multi-product batch created and actual outcome recorded; stock and `ProductionIn` movements posted to the default factory repository.
  - POS order saved as Pending, then Print & Deduct set it Completed and posted a `SaleOut` movement.
  - Overselling was blocked with an "Insufficient stock" message and no deduction.
  - Per-rep reconciliation started, pre-filled sold quantities from deducted orders, reconciled, and posted `DamageOut`/`ReplacementOut` movements once.
  - Transfer moved stock between factory and mall repositories and wrote both ledger sides.
  - Repository list/create/edit/details, batch create/details, POS create/receipt, orders, terminals, and reconciliation pages returned HTTP 200.
- **Cleanup**: Automated verification rows (test batch, test orders, test reconciliations, test movements) were removed and stock reset to the seeded baseline; the development server used for verification was stopped.

