# Plan: Super Admin Dashboard DB-Backed Rebuild

**Status**: Done

## Summary

Replace the synthetic SuperAdmin dashboard with a database-backed operational overview, document the exact table and column source for each of the eight lifecycle stages, and clean up the dashboard UI.

The dashboard will read real route-sheet, production, defect, and customer-return data for Today, This Week (Sunday-Saturday), This Month, and This Year. The existing slide-over drawer will be retained but its default grouping will be context-aware: rep-attributable stages open a sales-rep breakdown, while plant-level stages open a product-category breakdown.

No schema changes are required. The database already has the tables needed for the eight stages.

## Scope

- **In-Scope**
  - Database-backed SuperAdmin lifecycle totals for all four periods.
  - Table/source mapping for every dashboard KPI.
  - New database-backed dashboard service and dashboard API wiring.
  - Product-line performance from real product/category data.
  - Sales-rep grouped drill-down for Orders Placed, Loaded, Sold, Closing Stock, Damaged, and Customer Returns.
  - Product-category grouped drill-down for Produced and Factory Defects.
  - UI cleanup of the SuperAdmin overview and slide-over drawer.
- **Out-of-Scope**
  - Rewriting `/Reports` to use the same database-backed engine. `/Reports` remains on its current engine in this phase.
  - Changing how route sheets, production batches, defects, or customer returns are captured.
  - Rep-level allocation of plant-produced or factory-defect quantities. Those are not recorded per rep.
  - New database tables, calculated columns, or migrations.
  - POS/counter revenue being merged into the route-sheet lifecycle. The eight-stage view remains a field-operations funnel.

## Current State

- `Dashboard/Index.cshtml.cs` injects `MockAnalyticsService` and calls `GetDashboardOverview()`.
- `MockAnalyticsService` synthesises the lifecycle from deterministic hashes rather than reading the operational tables.
- `Dashboard/_ExecutiveOverview.cshtml` renders the eight stage cards and product-line cards.
- `Dashboard/Index.cshtml` contains the slide-over drawer JavaScript.
- The dashboard API endpoints in `Manufacture.Web/Api/AnalyticsApi.cs` also use `MockAnalyticsService`.
- The main dashboard has no visible timeframe selector even though the JavaScript expects `input[name="exec-period"]` and `exec-period-{key}` controls.
- `Orders Placed` is labelled correctly but is not sourced from a database.
- Closing Stock is already labelled "Closing Stock" in the partial, but the underlying DTO property is still `Returned`.
- The JSON/JS stage labels already use "Closing Stock" for `Returned`; the label itself is correct.

## Database Source Matrix

| # | Stage | Primary table(s) | Key columns | Joins | Dashboard grouping |
|---|---|---|---|---|---|
| 1 | **Orders Placed** | `DailyRepRouteSheets`, `RouteSheetItems` | `RouteDate`, `SalesRepId`, `RepOrderRequestedQty` | `SalesReps`, `Recipes`, `ProductionProductTypes`, `ProductionCategories` | By sales rep + product category |
| 2 | **Produced** | `ProductionBatches` | `ProductionDate`, `ActualQuantity`, `ProductionProductTypeId` | `Recipes`, `ProductionProductTypes`, `ProductionCategories` | By product category. Plant-level, no rep attribution |
| 3 | **Loaded** | `DailyRepRouteSheets`, `RouteSheetItems` | `RouteDate`, `SalesRepId`, `VehicleRegistration`, `TodayNewlyLoadedQty` | `SalesReps`, `Vehicles`, `Recipes`, `ProductionProductTypes`, `ProductionCategories` | By sales rep + product category; vehicle as a sub-column |
| 4 | **Factory Defects** | `ProductionDefects`, `ProductionBatches` | `ProductionBatchId`, `Quantity`, `ProductionDate` | `Recipes`, `ProductionProductTypes`, `ProductionCategories` | By product category. Plant-level, no rep attribution |
| 5 | **Sold** | `DailyRepRouteSheets`, `RouteSheetItems` | `RouteDate`, `SalesRepId`, `QtySold`, `UnitSellingPrice` | `SalesReps`, `Recipes`, `ProductionProductTypes`, `ProductionCategories` | By sales rep + product category |
| 6 | **Closing Stock** | `DailyRepRouteSheets`, `RouteSheetItems` | `RouteDate`, `SalesRepId`, `ClosingStockQty` | `SalesReps`, `Recipes`, `ProductionProductTypes`, `ProductionCategories` | By sales rep + product category |
| 7 | **Damaged** | `DailyRepRouteSheets`, `RouteSheetItems` | `RouteDate`, `SalesRepId`, `QtyDamaged` | `SalesReps`, `Recipes`, `ProductionProductTypes`, `ProductionCategories` | By sales rep + product category |
| 8 | **Returns (Customer Returns)** | `CustomerReturns` | `ReturnDate`, `SalesRepId`, `RecipeId`, `Quantity`, `ReplacementQuantity` | `SalesReps`, `Recipes`, `ProductionProductTypes`, `ProductionCategories` | By sales rep + product category |

### Supporting data

| Dashboard element | Tables / columns |
|---|---|
| Product lines / categories | `ProductionCategories.Name`; fallback to `Recipes.OutputUnit` and "Uncategorised" where a recipe has no product type |
| SKU names and output units | `Recipes.Name`, `Recipes.Code`, `Recipes.OutputUnit` |
| Sales rep identity, route, vehicle, terminal | `SalesReps.FullName`, `SalesReps.RouteName`, `SalesReps.VehicleId`, `SalesReps.PosTerminalId`, `Vehicles.RegistrationNumber`, `PosTerminals.TerminalCode` |
| Expected sales revenue | `RouteSheetItems.QtySold * RouteSheetItems.UnitSellingPrice` |
| Cash / transfer / POS tender | `DailyRepRouteSheets.CashCollected`, `BankTransferCollected`, `PosTerminalCollected` |
| Rep outstanding balance | `DailyRepRouteSheets.PreviousAccountBalance`, `TotalExpectedSalesRevenue`, collected amounts, or `NewOutstandingAccountBalance` |
| Closing-stock rollover | `RouteSheetItems.ClosingStockQty` becomes the next day's `RouteSheetItems.YesterdayClosingStock` through the existing reconciliation service |
| Ticket count | Not required for the eight-stage view. Remove the current "Tickets" badge rather than inventing a source. |

### Important source rules

- `Orders Placed` uses route-sheet request quantities, not `Orders` / `OrderItems`. This matches the agreed meaning: quantities requested by reps before route departure.
- `Loaded` uses `TodayNewlyLoadedQty`, not `TotalActiveInventory` (which includes yesterday's closing stock).
- `Closing Stock` uses `ClosingStockQty` only. It is good unsold stock and is already rolled into the next sheet by the existing reconciliation logic.
- `Damaged` uses `QtyDamaged` only. It is separate from customer returns.
- `Returns` uses `CustomerReturns.Quantity`. These are prior-sale customer returns/replacements and are not part of the same day's closing stock.
- `Factory Defects` uses `ProductionDefects.Quantity` joined to its batch. The period is attributed to `ProductionBatches.ProductionDate` so a defect stays with the run that produced it, even if the defect is entered later in the day.
- `ProductionBatches.Status = Cancelled (4)` is excluded from produced and defect totals.
- Use `Recipes.ProductionProductTypeId -> ProductionProductTypes.ProductionCategoryId -> ProductionCategories.Name` as the category source. Do not use the legacy `RouteSheetItems.Category` integer for dynamic categories.
- Date filtering is UTC-based, matching the rest of the dashboard.
- "This Week" is Sunday-Saturday.
- If a rep is missing from a customer return, group it under "Unassigned Rep"; do not drop the quantity.

## Decisions

- **Orders Placed source**: route-sheet requests.
- **Migration scope**: SuperAdmin Dashboard plus `/api/dashboard/summary` and `/api/dashboard/overview`. `/Reports` stays on the current engine for now.
- **Plant-level stages**: Produced and Factory Defects group by product category, not by an estimated rep allocation.
- **Internal naming**: Keep `LifecycleTotalsDto.Returned` for compatibility, but continue displaying it as **Closing Stock**. Add a computed `ClosingStock` alias if useful.
- **UI cleanup**: Remove duplicate/empty widgets, remove the fake ticket counter, add the missing visible timeframe selector, use actual database SKU names, and make the stage cards and drawer less dense.
- **No schema changes**: all required data already exists.

## Implementation Steps

### Phase 1: Database-Backed Dashboard Service

1. **Create a database-backed dashboard service contract**
   - *Details*: Add `ISuperAdminDashboardService` in `Manufacture.Library/Interfaces/`.
   - Interface:
     - `ExecutiveOverviewDto GetOverview(DateTime? date = null)`
     - `DashboardOverviewApiResponse GetOverviewApi(string? timeframe = null, DateTime? date = null)`
   - Keep the existing `ExecutiveOverviewDto` shape so the Razor partial and drawer JavaScript do not need a complete rewrite.
   - *Dependencies*: None.

2. **Add a source-fact model and period resolver**
   - *Details*: Add `Manufacture.Web/Services/SuperAdminDashboardService.cs` and a private/internal `DashboardFact` record:
     - `Date`, `RepId`, `RepName`, `Route`, `VehicleRegistration`, `Terminal`
     - `RecipeId`, `ProductCode`, `ProductName`, `CategoryId`, `CategoryName`, `OutputUnit`
     - `OrdersPlaced`, `Produced`, `Loaded`, `FactoryDefects`, `Sold`, `ClosingStock`, `Damaged`, `CustomerReturns`
     - `ExpectedRevenue`, `Cash`, `Transfer`, `PosBank`, `Outstanding`
   - Use the existing operational-week rule (Sunday-Saturday) and period windows:
     - Today
     - This Week
     - This Month
     - This Year
   - Fetch year-to-date facts once and filter into each period in memory to avoid four round trips.
   - *Dependencies*: Depends on Step 1.

3. **Implement route-sheet fact queries**
   - *Details*: Query `DailyRepRouteSheets` and `RouteSheetItems` for the anchor year through the anchor date.
   - Project each SKU row to a fact using:
     - `RepOrderRequestedQty` -> OrdersPlaced
     - `TodayNewlyLoadedQty` -> Loaded
     - `QtySold` -> Sold
     - `ClosingStockQty` -> ClosingStock
     - `QtyDamaged` -> Damaged
     - `QtySold * UnitSellingPrice` -> ExpectedRevenue
     - sheet tender amounts and `NewOutstandingAccountBalance`
   - Include `SalesReps.FullName`, `RouteName`, `VehicleRegistration`, and terminal code.
   - Join category through `Recipes -> ProductionProductTypes -> ProductionCategories`.
   - *Dependencies*: Depends on Step 2.

4. **Implement production and defect fact queries**
   - *Details*: Query `ProductionBatches` for non-cancelled batches in the range and project `ActualQuantity` as Produced.
   - Query `ProductionDefects` joined to `ProductionBatches` and `Recipes`, projecting `Quantity` as FactoryDefects and attributing it to the batch production date.
   - These facts have no rep and must remain plant-level category facts.
   - *Dependencies*: Depends on Step 2.

5. **Implement customer-return facts**
   - *Details*: Query `CustomerReturns` in the range and project `Quantity` as CustomerReturns.
   - Use `SalesRepId`, `ReturnDate`, `RecipeId`, and the recipe category. Use "Unassigned Rep" when `SalesRepId` is null.
   - *Dependencies*: Depends on Step 2.

6. **Build period totals, product lines, and drill-down scopes**
   - *Details*: Add a pure `SuperAdminDashboardAggregator` helper used by the service and unit tests.
   - It must build:
     - `ExecutiveOverviewDto.Periods`
     - `Today`
     - `ProductLinesByPeriod` and `ProductLines` using real `ProductionCategories` names and actual SKU names
     - `TenderByPeriod` and `Tender`
     - `AlertsByPeriod` and `Alerts` from real figures
     - `DrillDownScopes` (rep-by-rep)
     - `ProductDrillDownScopes` (SKU-by-SKU)
     - `StageDrillDownScopes` (eight stages)
   - Add `ProductionCategoryId`, `ProductionCategoryName`, and `ScopeKey` (`category-{id}`) to `ProductSkuDto` and `ProductLineBreakdownDto`. Keep the existing `ProductType`/`Line` fields as fallback for `/Reports` compatibility.
   - Build product-line cards from active `ProductionCategories`, not from the three-value `ProductType` enum. Dynamic categories such as Snacks or Drinks must appear as their own cards.
   - Stage grouping rules:
     - `OrdersPlaced`: by sales rep + product category
     - `Produced`: by product category
     - `Loaded`: by sales rep + product category; vehicle as a sub-column
     - `FactoryDefects`: by product category; produced column
     - `Sold`: by sales rep + product category
     - `Returned` / Closing Stock: by sales rep + product category
     - `Damaged`: by sales rep + product category; loaded column
     - `CustomerReturns`: by sales rep + product category
   - Compute `sharePercent` against the metric total.
   - For plant-level stages, set grouping labels to "By Product Category" and include a note that rep attribution is unavailable.
   - *Dependencies*: Depends on Steps 3-5.

### Phase 2: Page and API Wiring

7. **Wire the dashboard page to the new service**
   - *Details*: Modify `Dashboard/Index.cshtml.cs`.
   - Replace `MockAnalyticsService` injection with `ISuperAdminDashboardService` for `Overview`.
   - Keep `MockAnalyticsService` available for other pages if they still use it.
   - Keep the current `Stages` metadata and `LifecycleStage` record; only the data source changes.
   - *Dependencies*: Depends on Step 6.

8. **Wire the dashboard APIs**
   - *Details*: Modify `Manufacture.Web/Api/AnalyticsApi.cs`.
   - `/api/dashboard/overview` and `/api/dashboard/summary` use `ISuperAdminDashboardService`.
   - Preserve the existing JSON field names, especially `returned` for Closing Stock, so external consumers do not break. If adding a clearer alias, add `closing_stock` without removing `returned`.
   - Leave `/api/reports/query` on `MockAnalyticsService` for this phase.
   - *Dependencies*: Depends on Step 7.

9. **Register the service**
   - *Details*: In `Program.cs`, register `ISuperAdminDashboardService` as scoped because it uses `ManufactureDbContext`.
   - Remove the old dashboard dependency from the page; do not change the `MockAnalyticsService` registration used by `/Reports`.
   - *Dependencies*: Depends on Step 7.

### Phase 3: SuperAdmin UI Cleanup

10. **Add a visible timeframe and context toolbar**
    - *Details*: In `Dashboard/_ExecutiveOverview.cshtml`, add a top toolbar visible for SuperAdmin:
      - Segmented control: **Today / This Week / This Month / This Year**
      - As-of date label
      - Short definitions/legend for the eight stages
    - Wire the required `input[name="exec-period"]` and `exec-period-{key}` controls, or simplify the JavaScript to use the new buttons directly. Remove the current mismatch where JS expects controls that are not rendered.
    - *Dependencies*: Depends on Step 7.

11. **Simplify the eight-stage pipeline cards**
    - *Details*: Keep the strict order:
      `Orders Placed -> Produced -> Loaded -> Factory Defects -> Sold -> Closing Stock -> Damaged -> Returns`
    - Use a responsive horizontal rail with arrows between cards, or a 4x2 grid with a clear reading order. Avoid squeezing eight cards into one narrow row on small screens.
    - Each card shows:
      - Stage number, label, value, and unit
      - A one-line definition from the agreed rules
      - Conversion/health hint where useful, e.g. loaded/requested, sold/loaded, defects/produced
    - Add explicit tooltips for the three non-obvious stages:
      - Factory Defects: production losses before leaving the factory
      - Closing Stock: good unsold stock carried into tomorrow
      - Damaged: loaded stock spoiled before sale and written off
      - Returns: prior-sale customer returns replaced with fresh stock
    - Remove the `Tickets` badge; it has no source in this lifecycle.
    - *Dependencies*: Depends on Step 10.

12. **Simplify product-line cards**
    - *Details*: Replace the dense micro-lifecycle block with a cleaner card:
      - Product line / category name
      - Active SKU count
      - Units sold
      - Gross revenue
      - Closing stock
      - Damaged
      - Customer returns
      - **Rep breakdown** button
    - Use actual `SkuNames` from the database instead of the hardcoded `LineSkus` switch.
    - Make the Sold / Revenue block open the drawer in rep mode for that product line:
      `openDrill('@line.ScopeKey', period, 'Sold', 'reps')`.
    - Keep the full stage breakdown available through the drawer, not on the card.
    - *Dependencies*: Depends on Step 10.

13. **Update the slide-over drawer grouping and defaults**
    - *Details*: In `Dashboard/Index.cshtml` JavaScript and `_ExecutiveOverview.cshtml` drawer copy:
      - Orders Placed, Loaded, Sold, Closing Stock, Damaged, Customer Returns: default to **Sales Rep Breakdown**.
      - Produced and Factory Defects: default to **Category Breakdown** and show a "Plant-level data, no rep attribution" note.
      - Product-line sales totals: open **Sales Rep Breakdown** for that category.
      - Update `metricGrouping` and `stageColumns` to match the server-side grouping and columns.
      - Keep the existing **Product Breakdown** tab for SKU details.
      - Remove or rename the redundant generic "Stage Breakdown" tab if the rep/category table now carries the stage columns.
      - Ensure the drawer footer totals match the visible grouped rows.
    - *Dependencies*: Depends on Steps 11-12.

### Phase 4: Tests and Verification

14. **Add pure aggregation tests**
    - *Details*: Add `Manufacture.Tests/SuperAdminDashboardAggregatorTests.cs`.
    - Cover:
      - Today/Week/Month/Year windows.
      - Cancelled production batches excluded.
      - Route-sheet requested/loaded/sold/closing/damaged values.
      - Customer-return quantities.
      - Factory-defect attribution by production date.
      - Rep grouping for rep-attributable stages.
      - Category grouping for Produced and Factory Defects.
      - Product-line totals and share calculations.
      - Empty data returns zeroes, not divide-by-zero or fabricated values.
    - *Dependencies*: Depends on Step 6.

15. **Build, test, and restart**
    - *Details*: Stop the app on port 5199, build, run the full test suite, and restart.
    - *Dependencies*: Depends on Steps 7-14.

## Relevant Files & Symbols

- `Manufacture.Library/Interfaces/ISuperAdminDashboardService.cs`: New interface for DB-backed overview.
- `Manufacture.Web/Services/SuperAdminDashboardService.cs`: New EF Core queries and API mapping.
- `Manufacture.Web/Services/SuperAdminDashboardAggregator.cs`: Pure period/scope/grouping calculations.
- `Manufacture.Library/Models/DTOs/AnalyticsDtos.cs`: Existing `ExecutiveOverviewDto`, `LifecycleTotalsDto`, `StageDrillDownScopeDto`, `RepDrillDownRowDto`, `ProductLineBreakdownDto`.
- `Manufacture.Web/Pages/Dashboard/Index.cshtml.cs`: Replace `MockAnalyticsService` usage for `Overview`.
- `Manufacture.Web/Pages/Dashboard/Index.cshtml`: Drawer rendering, grouping metadata, default view selection.
- `Manufacture.Web/Pages/Dashboard/_ExecutiveOverview.cshtml`: Toolbar, stage cards, product-line cards, drawer shell.
- `Manufacture.Web/Api/AnalyticsApi.cs`: Dashboard API endpoints.
- `Manufacture.Web/Program.cs`: Register `ISuperAdminDashboardService`.
- `Manufacture.Database/Tables/DailyRepRouteSheets.sql`: Date, rep, vehicle, tender, outstanding balance.
- `Manufacture.Database/Tables/RouteSheetItems.sql`: Requested, loaded, sold, closing, damaged quantities.
- `Manufacture.Database/Tables/ProductionBatches.sql`: Produced quantity and production date.
- `Manufacture.Database/Tables/ProductionDefects.sql`: Factory defect quantities.
- `Manufacture.Database/Tables/CustomerReturns.sql`: Customer-return quantities and rep.
- `Manufacture.Database/Tables/Recipes.sql`, `ProductionProductTypes.sql`, `ProductionCategories.sql`: Product/category dimension.
- `Manufacture.Database/Tables/SalesReps.sql`, `Vehicles.sql`, `PosTerminals.sql`: Rep/route/vehicle labels.
- `Manufacture.Tests/SuperAdminDashboardAggregatorTests.cs`: New aggregation tests.

## Verification Plan

1. **Automated**
   - Build:
     `dotnet build Manufacture.slnx -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\`
   - Tests:
     `dotnet test Manufacture.Tests/Manufacture.Tests.csproj --no-build -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\`
   - Expected: 0 warnings, 0 errors; existing tests plus the new aggregation tests pass.

2. **Database cross-checks**
   - Compare each stage against direct SQL for a known date/range:
     - Route-sheet requested, loaded, sold, closing, damaged
     - Production actual output
     - Production defect sum
     - Customer returns sum
   - Confirm `Orders Placed`, `Loaded`, `Sold`, `Closing Stock`, `Damaged`, and `Returns` reconcile to the same route-sheet/return totals.
   - Confirm Produced and Factory Defects group by category and are labelled plant-level.

3. **UI and drawer**
   - Sign in as `admin@bakery.com` / `Password123!`.
   - Verify the timeframe selector switches Today / This Week / This Month / This Year and updates all cards.
   - Click every stage card and confirm:
     - Orders Placed opens a rep-grouped table.
     - Closing Stock opens a rep-grouped table.
     - Returns opens a rep-grouped table.
     - Loaded, Sold, and Damaged open rep-grouped tables.
     - Produced and Factory Defects open category-grouped tables.
   - Click a product-line Sold/Revenue block and confirm the drawer opens on the rep breakdown for that line.
   - Confirm the drawer search, period, product scope, totals, and "Open in Full Reports" link still work.
   - Confirm actual SKU names appear instead of hardcoded names.

4. **API**
   - `GET /api/dashboard/summary` returns database-backed totals.
   - `GET /api/dashboard/overview?timeframe=today` returns the same lifecycle values as the page.
   - Ensure the existing `returned` JSON field remains for Closing Stock compatibility.

## Context & Decisions Log

- **2026-11-13**: User requested a table-level review of the SuperAdmin Dashboard before any changes and allowed UI adjustments.
- **2026-11-13**: The eight stages are strictly ordered: `Orders Placed → Produced → Loaded → Factory Defects → Sold → Closing Stock → Damaged → Returns`.
- **2026-11-13**: "Factory Defects ... tracked under production analysis."
- **2026-11-13**: "Closing Stock ... Good, unsold products a sales rep brings back at the end of the day. These remain in the reps custody and automatically become the next day's opening stock."
- **2026-11-13**: "Damaged: Products loaded onto the truck in good condition that later became spoiled or unusable before sale ... and are returned for write-off."
- **2026-11-13**: "Returns (Customer Returns): Previously sold goods ... returned by supermarkets from prior sales; the rep collects the bad bread and replaces it with fresh stock."
- **2026-11-13**: "Clicking on summary cards ... must open a side bar, showing the data grouped by individual sales reps rather than only aggregated totals table."
- **2026-11-13**: User selected **route sheet requests** for Orders Placed.
- **2026-11-13**: User selected **Dashboard + dashboard APIs** for the database-backed scope; `/Reports` remains unchanged for now.
- **2026-11-13**: User selected **product-category grouping** for Produced and Factory Defects because they have no rep attribution.

## Key Decisions & Trade-offs

- **Route sheets as the operational source**: `DailyRepRouteSheets` and `RouteSheetItems` are the only tables that record requested, loaded, sold, closing, and damaged quantities by rep in one reconciled flow. This avoids reconciling unrelated POS orders against truck stock.
- **Customer returns remain separate**: `CustomerReturns` is a prior-sale replacement ledger, not same-day closing stock. Keeping it as stage eight preserves the agreed distinction.
- **Plant-level stages stay plant-level**: Producing a fake per-rep allocation of ProductionBatches or ProductionDefects would create numbers the business did not record. Category grouping is honest and auditable.
- **Reuse the existing DTO/view shell**: The current `ExecutiveOverviewDto` and drawer already support periods, scopes, stage drill-downs, and rep rows. Replacing the data source is lower risk than redesigning the contract and JavaScript from scratch.
- **Dynamic product categories**: The dashboard product-line cards must read `ProductionCategories` rather than hardcode Bread/Water/Popcorn. This supports future Snacks or Drinks categories without another code change.
- **UI simplification**: The current page has no visible main timeframe selector, a hardcoded SKU list, a fake ticket count, and a dense product-card micro-grid. Removing those reduces confusion without removing operational information.
- **No schema change**: Every stage has a home in the existing tables. The required work is query, aggregation, grouping, and presentation.
