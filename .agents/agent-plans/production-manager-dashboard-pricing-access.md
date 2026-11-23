# Plan: Production Manager Dashboard, Pricing Approval & Access

**Status**: Done

## Summary

Make the Production Manager experience production-wide instead of bread-only, add a SuperAdmin approval workflow for raw-material price changes with an auditable price history, and remove the Production Manager's inventory/sales management controls.

The dashboard will read today's production batches across every product line, aggregate output by product/unit, and link each batch to its review page. The pricing page will remain sourced from `InventoryItems` (raw materials only) but will submit price-change requests instead of applying prices immediately. Only SuperAdmin can approve or reject a request. Approved requests update the store item cost, recipe costing snapshots, and audit history atomically.

## Scope

- **In-Scope**
  - Production Manager dashboard KPIs and today's batch log.
  - Generic production output by product type/unit.
  - Ingredient price-change request table, approval/rejection, and history.
  - Approved price changes updating `InventoryItems.UnitCostPrice` and `RecipeIngredients.IngredientUnitCost`.
  - Production Manager access restrictions on Inventory item management.
  - Production Manager access restrictions on Sales/Orders creation, slip action, and order detail management sections.
  - Route-level and handler-level guards, not just hidden buttons.
- **Out-of-Scope**
  - Finished-product selling-price approval. This plan covers raw-material unit costs only.
  - New Sales/POS functionality or changes to SalesRep/SuperAdmin workflows.
  - Email or notification delivery for price approvals.
  - Pagination or export for price history.
  - Changing StoreManager restrictions or the existing requisition approval flow.
  - Hiding the receipt/loading-slip link on `/Sales/Orders/Details`; only the table `Slip` action and the two named detail sections are in scope.

## Current State

- `Dashboard/Index.cshtml.cs` loads `TodayBatches = _productionService.GetAllBatches()` with no date filter. The Production Manager cards sum `ActualQuantity` and label the result "loaves".
- The dashboard batch log has no action column.
- `/Production/Pricing` immediately calls `RecipeCostingEngine.ApplyPriceUpdates(...)`. There is no request, approval, or history table.
- The price master is already sourced from database inventory items through `ProductionService.GetAllIngredients()`, which excludes `Fuel` and `Utilities`.
- `/Inventory` always renders `Add Stock Item`, `Edit`, and `Restock`.
- `/Sales/Orders` always renders `New POS Order` and the `Slip` action.
- `/Sales/Orders/Details` always renders `Sales Rep Assignment` and `Settlement`.
- `RequireSessionPageFilter` currently has role branches for `SalesRep`, `StoreManager`, and `Customer`, but no `ProductionManager` branch.

## Decisions

- **Dashboard KPIs**: production-wide operational KPIs. The dashboard must not assume bread or loaves.
- **Price workflow**: Production Manager submits price changes; SuperAdmin approves or rejects them. Nothing changes until approval.
- **Price scope**: raw-material unit cost only, sourced from `InventoryItems`, not a mock or unrelated product list.
- **Approval semantics**: a rejected request is retained in history with its reason. Only approved requests mutate item cost and recipe costing.
- **One open request per item**: a filtered unique index prevents multiple pending requests for the same inventory item. The page shows the pending proposal until it is reviewed.
- **Routing**: unauthorized Production Manager routes redirect to a useful page (`/Sales/Orders/Index` or `/Inventory/Index`) rather than returning a raw 403.
- **Read-only Sales visibility**: Production Manager can still view orders and order details, but cannot create POS orders, print the table slip action, assign reps, or settle payments.

## Implementation Steps

### Phase 1: Price Approval Data & Service Layer

1. **Add `IngredientPriceChangeRequests` table and entity**
   - *Details*: Create `Manufacture.Database/Tables/IngredientPriceChangeRequests.sql`.
   - Columns:
     - `Id INT IDENTITY PRIMARY KEY`
     - `InventoryItemId INT NOT NULL` FK to `InventoryItems`
     - `InventoryItemName NVARCHAR(150) NOT NULL` (snapshot for history)
     - `RequestNumber NVARCHAR(40) NOT NULL` unique
     - `PreviousUnitCost DECIMAL(18,4) NOT NULL`
     - `RequestedUnitCost DECIMAL(18,4) NOT NULL`
     - `Status INT NOT NULL DEFAULT 0` (`0=Pending`, `1=Approved`, `2=Rejected`)
     - `RequestedByUserId INT NULL` FK to `Users`, `ON DELETE SET NULL`
     - `RequestedByName NVARCHAR(150) NOT NULL DEFAULT ''`
     - `RequestedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME()`
     - `ReviewedByUserId INT NULL` FK to `Users`, `ON DELETE SET NULL`
     - `ReviewedByName NVARCHAR(150) NULL`
     - `ReviewedAt DATETIME2(3) NULL`
     - `ReviewNotes NVARCHAR(500) NOT NULL DEFAULT ''`
     - `AppliedAt DATETIME2(3) NULL`
     - `CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME()`
     - `UpdatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME()`
     - `RowVersion ROWVERSION NOT NULL`
   - Checks: `PreviousUnitCost >= 0`, `RequestedUnitCost >= 0`, `Status IN (0,1,2)`.
   - Indexes: `IX_IngredientPriceChangeRequests_Status_RequestedAt`, `IX_IngredientPriceChangeRequests_InventoryItemId`, and `UX_IngredientPriceChangeRequests_OpenItem` unique filtered on `InventoryItemId WHERE Status = 0`.
   - Add the SQL file to `Manufacture.Database/Manufacture.Database.sqlproj`.
   - Add `Manufacture.Library/Models/Entities/IngredientPriceChangeRequest.cs` and `DbSet<IngredientPriceChangeRequest>` plus `OnModelCreating` mapping to `Manufacture.Library/Data/ManufactureDbContext.cs`. Match existing column lengths, defaults, rowversion, and FK conventions. Do not use EF migrations; this project is database-first.
   - *Dependencies*: None.

2. **Add price-approval DTOs and status enum**
   - *Details*: Add to `Manufacture.Library/Models/DTOs/RecipeCostingDtos.cs`:
     - `IngredientPriceChangeStatus` enum: `Pending = 0`, `Approved = 1`, `Rejected = 2`.
     - `IngredientPriceChangeRequestDto`: id, request number, inventory item id/name/code, unit, previous/requested/current unit cost, status, requested/reviewed metadata, review notes, applied timestamp.
     - `PriceChangeSubmissionResultDto`: submitted count, skipped count, messages/errors.
     - `PriceChangeActionResultDto`: success flag, message, affected item/request.
   - Keep all monetary values as base-unit costs per `Ingredient.Unit` (kg, litre, piece), matching the pricing page.
   - *Dependencies*: Depends on Step 1 for the persisted status values, but DTOs can be written in parallel.

3. **Implement request submission, approval, and rejection in `ProductionService`**
   - *Details*: Extend `Manufacture.Library/Interfaces/IProductionService.cs` and `Manufacture.Web/Services/ProductionService.cs` with:
     - `List<IngredientPriceChangeRequestDto> GetIngredientPriceChangeRequests(IngredientPriceChangeStatus? status = null)`
     - `PriceChangeSubmissionResultDto SubmitIngredientPriceChangeRequests(IEnumerable<UpdateIngredientPriceDto> updates, int requestedByUserId, string requestedByName)`
     - `PriceChangeActionResultDto ApproveIngredientPriceChangeRequest(int requestId, int reviewedByUserId, string reviewedByName, string? notes)`
     - `PriceChangeActionResultDto RejectIngredientPriceChangeRequest(int requestId, int reviewedByUserId, string reviewedByName, string reason)`
   - Submission:
     - Only process rows whose `NewUnitCost` differs from the current base-unit cost.
     - Validate `>= 0` and reject duplicate pending requests.
     - Snapshot the inventory item name and previous cost.
     - Generate `PRC-{yyyyMMdd}-{nn}` numbers using the same pattern as `GenerateBatchNumber`.
     - Write an `AuditLogs` row for each submitted request.
   - Approval:
     - Load the request with `Status == Pending`.
     - Recompute the item's current base-unit cost as `UnitCostPrice / PackageSize` and compare with `PreviousUnitCost`. If they differ, return a failure message asking for a fresh request; do not overwrite silently.
     - In one `SaveChanges` transaction, mark the request approved, set `AppliedAt`, set reviewer metadata, update `InventoryItems.UnitCostPrice` using the existing package conversion, and update every matching `RecipeIngredients.IngredientUnitCost`.
     - Write an `AuditLogs` row containing old/new price and request number.
   - Rejection:
     - Require a non-empty reason, mark `Status = Rejected`, set reviewer metadata and `ReviewNotes`, and write an `AuditLogs` row.
   - Keep `UpdateIngredientPrice` and `ApplyPriceUpdates` intact for existing tests/compatibility, but the pricing page must no longer call `ApplyPriceUpdates` directly.
   - *Dependencies*: Depends on Steps 1 and 2.

4. **Add a database-backed production dashboard summary**
   - *Details*: Add DTOs to `Manufacture.Library/Models/DTOs/ProductionDtos.cs`:
     - `ProductionDashboardDto`: date, total/completed/active/planned batches, defect quantity, material cost, total target/actual units, yield achievement percent, a list of output groups, and today's mapped batches.
     - `ProductionOutputGroupDto`: category name, product type name, output unit, batch count, target, actual, variance, yield percent.
   - Add a pure `Manufacture.Web/Services/ProductionDashboardMetrics.cs` helper with `Build(DateTime date, IEnumerable<ProductionBatch> batches, int defectQuantity)` so the aggregation can be unit-tested without a database.
   - Add `ProductionDashboardDto GetProductionDashboard(DateTime date)` to `IProductionService` and `ProductionService`.
   - Query non-cancelled batches where `ProductionDate >= date.Date && ProductionDate < date.Date.AddDays(1)`.
   - Query `ProductionDefects` where `RecordedAt` falls in the same day.
   - Pass the mapped batches and defect total to `ProductionDashboardMetrics.Build`.
   - Group output by `ProductionCategoryName + ProductTypeName + OutputUnitLabel`; never sum unlike units. Use `"Uncategorised"`/`"units"` as safe fallbacks.
   - Exclude `BatchStatus.Cancelled` from cost, output, and yield. Compute yield as `TotalActual / TotalTarget * 100` when target > 0, otherwise `0`.
   - *Dependencies*: None.

### Phase 2: Production Manager Dashboard

5. **Replace the Production Manager dashboard KPI section**
   - *Details*: In `Manufacture.Web/Pages/Dashboard/Index.cshtml.cs`:
     - Add `public ProductionDashboardDto ProductionDashboard { get; set; } = new();`
     - After resolving `ActiveRole`, call `_productionService.GetProductionDashboard(DateTime.UtcNow)`.
     - Set `TodayBatches = ProductionDashboard.Batches` for the Production Manager. Remove the unfiltered `GetAllBatches()` assignment for this section.
   - In `Manufacture.Web/Pages/Dashboard/Index.cshtml`, replace the three bread-only stat cards with six compact responsive cards:
     - **Batches Today**: total, with completed/active/planned breakdown.
     - **Completed Today**: completed batch count.
     - **In Progress / Planned**: active + planned count.
     - **Yield Achievement**: percentage actual vs target across all product lines.
     - **Material Cost Today**: sum of today's `TotalBatchCost`.
     - **Defects Logged**: today's recorded defect quantity.
   - Add a **Today's Output by Product Line** card above the batch log. Render one row per output group with category/product type, unit, batch count, target, actual, variance, and yield percentage. This is the authoritative multi-product output view; do not add unlike units together.
   - Use daisyUI cards, badges, and existing color conventions. Keep the section only for `ActiveRole == "ProductionManager"`.
   - *Dependencies*: Depends on Step 4.

6. **Make the batch log generic and add the review action**
   - *Details*: Rename the heading to **Today's Production Batch Log**.
   - Columns: Batch Code, Product / Recipe (category badge), Shift, Target Output, Actual Output, Variance, Status, Action.
   - Remove bread-specific labels such as "loaves" and "Flour Bags Used"; use `batch.OutputUnitLabel` and `batch.BatchInputQuantity/BatchInputUnitLabel`.
   - Add a clear **Review Batch** action linking to `/Production/Batches/Details/{id}`. Use a compact button or icon-plus-text action consistent with other tables.
   - Add an empty-state row when there are no batches today.
   - *Dependencies*: Depends on Step 5.

### Phase 3: Pricing Approval Workflow

7. **Rework `/Production/Pricing` page model for role-aware requests**
   - *Details*: Modify `Manufacture.Web/Pages/Production/Pricing/Index.cshtml.cs`.
   - Inject `IProductionService` alongside `RecipeCostingEngine`.
   - Add: `UserRole`, `CanSubmitPriceChanges` (`ProductionManager`), `CanApprovePriceChanges` (`SuperAdmin`), `PendingRequests`, `PriceHistory`, and `PendingByItemId`.
   - On GET:
     - Resolve session role.
     - Allow only `ProductionManager` and `SuperAdmin`; redirect other roles to `/Dashboard/Index`.
     - Load `PriceMaster`, `Categories`, and `Report` as today.
     - Load pending requests and full history through the production service.
   - Replace `OnPostUpdatePrices` with:
     - `OnPostSubmitPriceChanges`: ProductionManager only; validate changed prices; call `SubmitIngredientPriceChangeRequests`; set success/error TempData; refresh and return Page.
     - `OnPostApprovePriceRequest(int id)`: SuperAdmin only; call `ApproveIngredientPriceChangeRequest`; refresh; show applied/error message.
     - `OnPostRejectPriceRequest(int id, string reason)`: SuperAdmin only; reject with required reason; refresh.
   - Never call `_engine.ApplyPriceUpdates(...)` from the page.
   - *Dependencies*: Depends on Step 3.

8. **Rebuild the `/Production/Pricing` view**
   - *Details*: Modify `Manufacture.Web/Pages/Production/Pricing/Index.cshtml`.
   - Remove the single outer `<form>` wrapper. The current page nests all content in one form, which prevents adding separate approval forms.
   - Header/subtitle: explain that submitted prices are proposals and only SuperAdmin approval updates store costs.
   - **Pending Price Approvals** card, visible to SuperAdmin:
     - Request number, material, current price, proposed price, requested by/at, optional notes.
     - Approve button per request.
     - Reject form per request with a required reason field.
     - Empty state when nothing is waiting.
   - **Master Ingredient Price List** card:
     - Keep the inventory-backed rows. Show item code, material name, `per @row.Unit`, category, stock, recipe usage, current price, proposed price, approval status, and last updated.
     - Wrap only this table in `<form method="post" asp-page-handler="SubmitPriceChanges">`.
     - For ProductionManager rows with no pending request, render an editable number input.
     - For rows with a pending request or for SuperAdmin, render read-only text plus hidden current-value inputs so indexing stays stable.
     - Show a **Pending approval** badge and the proposed amount where applicable.
     - Replace the old **Recalculate All Costs** submit button with a normal page refresh link or remove it. It must not submit price edits or call `ApplyPriceUpdates`.
     - Button text: **Submit Price Changes for Approval**; only visible to ProductionManager.
   - **Price Change History** card:
     - Columns: Request #, Material, Old Price, New Price, Difference, Status, Requested By/At, Reviewed By/At, Notes.
     - Include a status filter (All / Pending / Approved / Rejected) and a search input for material name or request number. Filtering can be server-side or client-side, but both controls are required.
     - Show rejected and approved requests, including reasons.
   - Keep the existing Profitability report, with copy clarifying that it reflects approved prices only.
   - Keep the existing JavaScript that highlights changed inputs; update the instructional text.
   - *Dependencies*: Depends on Step 7.

### Phase 4: Production Manager Access Restrictions

9. **Add central route guards**
   - *Details*: Modify `Manufacture.Web/Services/RequireSessionPageFilter.cs`.
   - Add a `ProductionManager` branch before the customer branch.
   - Block `/Sales/Orders/Create` and redirect to `/Sales/Orders/Index`.
   - Block `/Inventory/Items/Create` and `/Inventory/Items/Edit` and redirect to `/Inventory/Index`.
   - Keep `/Sales/Orders`, `/Sales/Orders/Details`, `/Inventory`, `/Inventory/Items/Details`, `/Inventory/Movements`, `/Production`, and `/Production/Pricing` accessible as appropriate.
   - Add defense-in-depth page-level role checks where handlers mutate data.
   - *Dependencies*: None.

10. **Hide and protect Inventory item-management controls**
    - *Details*: Modify `Manufacture.Web/Pages/Inventory/Index.cshtml.cs` and `Index.cshtml`.
    - Add `UserRole` and `CanManageStock` (`UserRole != "ProductionManager"`).
    - In `OnPostRestockAsync`, reject ProductionManager posts with a friendly error and redirect; do not call `RestockItemAsync`.
    - Hide `Add Stock Item` for ProductionManager.
    - Keep `View` always visible.
    - Hide `Edit` and `Restock` for ProductionManager.
    - Hide/leave the quick-restock modal unused for ProductionManager; ensure no trigger remains.
    - *Dependencies*: Depends on Step 9.

11. **Hide and protect Sales/Orders manager actions**
    - *Details*: Modify `Manufacture.Web/Pages/Sales/Orders/Index.cshtml.cs`, `Index.cshtml`, `Details.cshtml.cs`, `Details.cshtml`, and `Manufacture.Web/Pages/Shared/_Layout.cshtml`.
    - Orders Index model: add `IsProductionManager`, `CanCreatePosOrder`, `CanPrintSlip`.
    - Orders view: hide `New POS Order` and the table `Slip` action for ProductionManager.
    - Layout: hide the **Point of Sale** submenu for ProductionManager even though the role has `PreOrders: View Only`; retain **Orders & Invoices**.
    - Details model: add `UserRole`, `CanManageSales` (`UserRole != "ProductionManager"`). Skip loading sales reps for ProductionManager if practical.
    - Details view: hide `Sales Rep Assignment` and `Settlement` cards for ProductionManager. If both are hidden, let the left content span the full width instead of leaving an empty sidebar column.
    - Guard `OnPostAssignRepAsync` and `OnPostApplyPaymentAsync`: ProductionManager must not mutate an order; redirect back with an error.
    - Add the same ProductionManager guard at the top of `/Sales/Orders/Create` `OnGetAsync` and `OnPostAsync` as defense-in-depth, even though the global filter blocks the route.
    - Leave the `Receipt / Loading Slip` link in the details header in scope only as a read-only view, since the user specifically asked to hide the table `Slip` action and the two named sections.
    - *Dependencies*: Depends on Step 9.

### Phase 5: Tests, Deployment & Verification

12. **Add focused automated tests**
    - *Details*: Add `Manufacture.Tests/ProductionDashboardMetricsTests.cs` covering the pure `ProductionDashboardMetrics.Build` helper:
      - Cancelled batches excluded from counts, cost, and output.
      - Bread, snack, and water output groups stay separate by unit.
      - Correct completed/active/planned counts.
      - Correct yield percentage, material cost, and defect total.
      - Empty-day fallback values.
    - Keep price-approval integration verification manual because the test project does not currently use an EF database provider. Do not add a new provider package solely for these tests.
    - *Dependencies*: Depends on Step 4.

13. **Deploy the schema and seed**
    - *Details*: The SQL project is database-first. Add the new table to the `.sqlproj`, then run the existing deploy script. Do not create EF migrations.
    - Command: `powershell -ExecutionPolicy Bypass -File scripts/deploy-database.ps1`
    - Confirm the filtered unique pending index and FKs exist in LocalDB.
    - *Dependencies*: Depends on Step 1.

14. **Build, test, and restart the app**
    - *Details*: Build to the existing temp output path, run tests, stop the existing port 5199 process, and restart the app.
    - *Dependencies*: Depends on Steps 2-13.

## Relevant Files & Symbols

- `Manufacture.Database/Tables/IngredientPriceChangeRequests.sql`: New approval/history table.
- `Manufacture.Database/Manufacture.Database.sqlproj`: Add the new table as a `Build` item.
- `Manufacture.Library/Models/Entities/IngredientPriceChangeRequest.cs`: New scaffold-style entity.
- `Manufacture.Library/Data/ManufactureDbContext.cs`: Add `DbSet`, indexes, property defaults, and relationships.
- `Manufacture.Library/Models/DTOs/RecipeCostingDtos.cs`: `IngredientPriceChangeStatus`, request/history/result DTOs.
- `Manufacture.Library/Models/DTOs/ProductionDtos.cs`: `ProductionDashboardDto`, `ProductionOutputGroupDto`.
- `Manufacture.Library/Interfaces/IProductionService.cs`: New request and dashboard method signatures.
- `Manufacture.Web/Services/ProductionService.cs`: Implement dashboard data loading, request submission, approval, rejection, audit logging, and price application. Reuse `MapIngredient`, `UpdateIngredientPrice`, and the existing `AuditLogs` pattern from `SalesService.cs`.
- `Manufacture.Web/Services/ProductionDashboardMetrics.cs`: Pure aggregation helper for KPI/output-group calculations.
- `Manufacture.Tests/ProductionDashboardMetricsTests.cs`: Tests for the aggregation helper.
- `Manufacture.Web/Pages/Dashboard/Index.cshtml.cs`: Load `ProductionDashboard` and today's batches.
- `Manufacture.Web/Pages/Dashboard/Index.cshtml`: Replace Production Manager KPI cards and batch log; add output breakdown and Review Batch action.
- `Manufacture.Web/Pages/Production/Pricing/Index.cshtml.cs`: Role-aware submit/approve/reject handlers; remove immediate `ApplyPriceUpdates`.
- `Manufacture.Web/Pages/Production/Pricing/Index.cshtml`: Remove outer form; add pending approvals, submission form, pending badges, and history.
- `Manufacture.Web/Services/RequireSessionPageFilter.cs`: ProductionManager route restrictions.
- `Manufacture.Web/Pages/Inventory/Index.cshtml.cs`: `CanManageStock` and restock guard.
- `Manufacture.Web/Pages/Inventory/Index.cshtml`: Hide Add/Edit/Restock for ProductionManager.
- `Manufacture.Web/Pages/Sales/Orders/Index.cshtml.cs`: `CanCreatePosOrder`, `CanPrintSlip`.
- `Manufacture.Web/Pages/Sales/Orders/Index.cshtml`: Hide `New POS Order` and `Slip`.
- `Manufacture.Web/Pages/Sales/Orders/Details.cshtml.cs`: `CanManageSales` and POST guards.
- `Manufacture.Web/Pages/Sales/Orders/Details.cshtml`: Hide assignment/settlement sections and adjust grid.
- `Manufacture.Web/Pages/Shared/_Layout.cshtml`: Hide POS submenu for ProductionManager.
- `scripts/deploy-database.ps1`: Publish the new schema and seed data.

## Verification Plan

1. **Automated**
   - Build:
     `dotnet build Manufacture.slnx -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\`
   - Tests:
     `dotnet test Manufacture.Tests/Manufacture.Tests.csproj --no-build -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\`
   - Expected: 0 warnings, 0 errors; existing tests plus new focused tests pass.

2. **Manual: Production Manager dashboard**
   - Sign in as `production@bakery.com` / `Password123!`.
   - Confirm only today's non-cancelled batches are counted.
   - Confirm KPIs cover all product lines and show output by product type/unit instead of "loaves".
   - Confirm each batch row has a working **Review Batch** action to `/Production/Batches/Details/{id}`.
   - Create or use a non-bread batch and verify it appears in both the KPI totals and the output breakdown.

3. **Manual: Price approval**
   - As Production Manager, change one raw-material price and submit.
   - Verify the item price, recipe costing, and margin report have not changed yet.
   - Verify a pending request appears and the item shows the proposed price.
   - Attempt a second request for the same item; confirm it is blocked by the pending request.
   - Sign in as `admin@bakery.com` / `Password123!`; approve the request.
   - Verify `InventoryItems.UnitCostPrice`, the linked `RecipeIngredients.IngredientUnitCost` rows, recipe costing, and margin report update only after approval.
   - Verify history records old/new price, requester, reviewer, timestamp, and request number.
   - Submit and reject another request with a reason; verify the price does not change and the rejection appears in history.

4. **Manual: Access restrictions**
   - As Production Manager:
     - `/Inventory` shows `View` but no `Add Stock Item`, `Edit`, or `Restock`.
     - Direct `/Inventory/Items/Create` and `/Inventory/Items/Edit` redirect to `/Inventory`.
     - Direct restock POST is rejected.
     - `/Sales/Orders` has no `New POS Order` or `Slip` action.
     - `/Sales/Orders/Create` redirects to `/Sales/Orders/Index`.
     - The sidebar has `Orders & Invoices` but no `Point of Sale`.
     - `/Sales/Orders/Details` has no `Sales Rep Assignment` or `Settlement`; the remaining content uses the full width.
     - Direct POST to assign a rep or apply payment is rejected.
   - As SuperAdmin: verify POS, slip, assignment, settlement, inventory, and price approvals remain available.
   - As StoreManager: verify existing restrictions remain unchanged.
   - As SalesRep: verify POS creation and own-order visibility remain unchanged.

## Context & Decisions Log

- **2026-11-13**: "In the /Dashboard page, the KPI are not making sence, it's only for bread ... the factory produces not just bread but snaks included. Also at the `Today's Oven Batch Log` section the table dosn't have a review batch action button."
- **2026-11-13**: "i want the dashboard to read relevant data for the Production Manager User."
- **2026-11-13**: "the /Production/Pricing page, they should be a workflow such that then a new item price is enterd it dosnt save imidiately but need to be approved by the superAdmin and they shoul be price change history to track the item price taht was changed."
- **2026-11-13**: User selected **Production-wide operational KPIs**.
- **2026-11-13**: User selected **Submit → SuperAdmin approves → price applies**.
- **2026-11-13**: User selected **Raw-material unit cost** and clarified: "make sure that the raw material liksted are form the inventory and not some randome product list. the aime of the /Production/Pricing was to be able to update the price of things in the soter so easily."
- **2026-11-13**: "in the /Inventory page the Production Manager User should not see the `Add Stock Item` button and in the table the Production Manager User should not see the `Edit` and `Restock` actions."
- **2026-11-13**: "The Production Manager User should not have access to the `/Sales/Orders/Create` page in the `Sales & POS` moduel."
- **2026-11-13**: "The Production Manager User should not have access to the `New POS Order` button in the /Sales/Orders page."
- **2026-11-13**: "in the /Sales/Orders page, the Production Manager User should not see the the Slip action in the table."
- **2026-11-13**: "in the /Sales/Orders/Details page, the Production Manager User should not see the `Sales Rep Assignmen` and `Settlement` sections."

## Key Decisions & Trade-offs

- **Price source**: `InventoryItems` remains the single source of truth through `ProductionService.GetAllIngredients`. The existing exclusion of `Fuel` and `Utilities` stays. Prices are displayed per the item's base package unit (kg, litre, piece), not per package, to match recipe costing.
- **Approval timing**: No price mutation occurs during submission. `RecipeCostingEngine.ApplyPriceUpdates` is no longer used by the pricing page; only the approval handler calls the price-application path.
- **One pending request per item**: Prevents ambiguous approvals and duplicate history. The trade-off is that a Production Manager must wait for the current request to be approved or rejected before submitting a replacement.
- **History table vs audit-only**: A dedicated request table is required because the page must show pending status, rejection reasons, reviewer identity, and proposed prices. `AuditLogs` is still written for a second, system-wide audit trail.
- **Multi-unit dashboard**: Output is grouped by product type and output unit. The dashboard deliberately does not add loaves, bottles, and bags into one number, because that would repeat the original problem.
- **Read-only Sales access**: Production Manager keeps order visibility but loses management actions. This preserves cross-functional visibility while preventing accidental POS, assignment, or settlement changes.
