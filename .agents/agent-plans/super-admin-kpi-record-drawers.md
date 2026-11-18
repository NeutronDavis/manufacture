# Plan: SuperAdmin KPI Record Drawers (source-record drill-downs)

**Status**: Implemented — verified against the local database
**Created**: 2026-11-16
**Owner**: SuperAdmin dashboard (`/Dashboard`)

## Summary (TL;DR)

The eight lifecycle KPI cards on `/Dashboard` currently open a **pre-aggregated** drawer (rep × category sums). This plan keeps that aggregated view but adds a new, default **Records** tab that shows the actual source records behind each KPI:

| KPI | Drawer records | Grouped by |
|---|---|---|
| Orders Placed | `/Sales/Orders` order lines | Sales rep |
| Produced | Production batches (product + actual qty) | Product category |
| Loaded | Route-sheet load lines from signed-off dispatches | Sales rep |
| Factory Defects | Defect records per batch | Product category |
| Sold | Reconciled route-sheet sale lines | Sales rep |
| Closing Stock | Reconciled route-sheet closing lines | Sales rep |
| Damaged | Reconciled route-sheet damage lines | Sales rep |
| Returns | Customer-return records | Sales rep |

The `Orders Placed` KPI is **re-based on the order register** so the card number and the drawer total always agree. Records are fetched lazily on click from a new `/Dashboard?handler=Drill` page handler (no giant JSON payload), totals are computed server-side, and rows are capped for very wide windows.

## User Decisions (verbatim)

- Orders Placed source: **"Rebase KPI on orders"** — the KPI itself counts `/Sales/Orders` records so the KPI and drawer always match.
- Reconciled filter: **"All signed-off, badge status"** — `Loaded` includes every signed-off dispatch and shows a Reconciled/Pending badge; `Sold`, `Closing Stock` and `Damaged` stay reconciled-only.
- Factory Defects: **"Yes, include it"** — same record treatment as the other stages.
- Drawer tabs: **"New Records tab, default"** — records open first; the existing aggregated tabs stay for context.

## Scope

**In scope**
- Re-base `OrdersPlaced` facts on `Orders` + `OrderItems` (excluding `Cancelled`).
- New lazy detail endpoint (`OnGetDrill`) + `ISuperAdminDashboardService.GetKpiDetail`.
- New record DTOs and a pure, testable grouping builder.
- Records tab in the existing slide-over with per-metric columns, group headers, per-group totals and grand totals.
- Unit tests for grouping, blank actual quantities, reconciliation filtering and truncation.

**Out of scope**
- No database schema change and no EF migration.
- `/Reports` engine, exports and report columns stay untouched.
- Product-line cards, Product Orders tab and Sales Rep Allocations tab behaviour stay as-is.
- No pagination inside the drawer (cap + "Open in Full Reports" link instead).
- No backfill of `Order.SalesRepId`; existing unassigned orders stay unassigned.
- No change to `ProductionBatch.ActualQuantity` storage semantics (still `int`).

## Data Contract

### KPI → source → grouping → columns

| KPI (`LifecycleMetric`) | Source | Filter | Group | Row columns |
|---|---|---|---|---|
| `OrdersPlaced` | `Orders` + `OrderItems` + `Recipe → ProductType → Category` | `OrderDate` in window, `Status != Cancelled` | Sales rep (`SalesRepId ?? 0`) | Order # · Customer · Date · Items · Units · Value · Status |
| `Produced` | `ProductionBatches` + `Recipe → ProductType → Category` | `ProductionDate` in window, `Status != Cancelled` | Product category | Batch # · Product · Category · Target · **Actual** · Unit · Shift · Status |
| `Loaded` | `DailyRepRouteSheets` + `RouteSheetItems` | `RouteDate` in window, `Status >= LoadedOnRoute` | Sales rep | Route sheet · Product · Category · Loaded · Requested · Value · Status |
| `FactoryDefects` | `ProductionDefects` + `ProductionBatch → Recipe → …` | batch date in window, batch not cancelled | Product category | Batch # · Product · Category · Defects · Reason · Recorded by · Date |
| `Sold` | `DailyRepRouteSheets` + `RouteSheetItems` | `Status >= Reconciled`, `QtySold > 0` | Sales rep | Route sheet · Product · Category · Sold · Unit price · Value · Status |
| `Returned` (Closing Stock) | same | `Status >= Reconciled`, `ClosingStockQty > 0` | Sales rep | Route sheet · Product · Category · Closing · Loaded · Status |
| `Damaged` | same | `Status >= Reconciled`, `QtyDamaged > 0` | Sales rep | Route sheet · Product · Category · Damaged · Loaded · Value · Status |
| `CustomerReturns` | `CustomerReturns` + `Recipe → …` + `SalesRep` | `ReturnDate` in window | Sales rep (`SalesRepId ?? 0`) | Return # · Product · Customer · Qty · Refund · Reason · Date |

**Rules**
- "No actual value" on Produced means `ActualQuantity <= 0` → row `Quantity = null` and the cell renders blank (not `0`). Group and grand totals still sum real figures.
- `Loaded` shows a `Reconciled` or `Loaded · pending reconciliation` badge per row so the total can legitimately match the KPI.
- `Sold`/`Returned`/`Damaged` only read sheets with `Status >= Reconciled` (`Reconciled = 2`, `Closed = 3`); unreconciled sheets hold zeroes anyway, so totals still match the KPI.
- Cancelled orders, cancelled batches and zero-quantity lines are excluded from both totals and rows.
- Order "Units" = `OrderItem.Quantity` (bonus shown separately as `+N bonus` when present).
- Order facts contribute `OrdersPlaced` only. `ExpectedRevenue` on order facts stays `0` so revenue continues to come from verified route sales.
- Scope `category-{id}` filters every metric by `ProductionCategoryId` (`category-0` = uncategorised). Scope `all` = no filter.
- Date windows use UTC, matching the existing aggregator (`DateTime.UtcNow.Date`).

### New DTOs (`Manufacture.Library/Models/DTOs/AnalyticsDtos.cs`)

```csharp
public class KpiDetailDto
{
    public string Metric { get; set; } = "OrdersPlaced";
    public string MetricLabel { get; set; } = "Orders Placed";
    public string PeriodKey { get; set; } = "Today";
    public string PeriodLabel { get; set; } = "Today";
    public string DateLabel { get; set; } = string.Empty;
    public string ScopeKey { get; set; } = "all";
    public string ScopeLabel { get; set; } = "All Product Lines";
    public string GroupingLabel { get; set; } = "By Sales Rep";
    public string Note { get; set; } = string.Empty;
    public string Unit { get; set; } = "units";
    public int Total { get; set; }                 // metric units across all groups
    public decimal TotalValue { get; set; }        // naira, where meaningful
    public int RecordCount { get; set; }           // source records before row cap
    public bool IsTruncated { get; set; }
    public List<KpiDetailGroupDto> Groups { get; set; } = new();
}

public class KpiDetailGroupDto
{
    public string Key { get; set; } = string.Empty;        // rep-12 | category-3
    public string Label { get; set; } = string.Empty;      // "Adekunle Johnson" | "Bread"
    public string SubLabel { get; set; } = string.Empty;   // "Ikeja • KJA-001 • POS-01"
    public string Unit { get; set; } = "units";
    public int Total { get; set; }
    public decimal TotalValue { get; set; }
    public int RecordCount { get; set; }
    public List<KpiDetailRowDto> Rows { get; set; } = new();
}

/// <summary>One source record. Null quantities render blank (e.g. a batch with no recorded output).</summary>
public class KpiDetailRowDto
{
    public string Reference { get; set; } = string.Empty;      // ORD-2026-001 | BTH-0007 | RTE-… | RET-…
    public string ReferenceUrl { get; set; } = string.Empty;   // optional deep link
    public string Product { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string UnitLabel { get; set; } = "units";
    public int? Quantity { get; set; }
    public int? SecondaryQuantity { get; set; }                // target / requested / loaded
    public decimal? UnitPrice { get; set; }
    public decimal? Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusTone { get; set; } = "ghost";          // ghost | success | warning | error | info
    public string Meta { get; set; } = string.Empty;           // customer, shift, reason, vehicle
    public string DateLabel { get; set; } = string.Empty;
}
```

## Implementation Steps

### Phase A — Contract & window resolution

1. **Add the record DTOs**
   - *Details*: Append `KpiDetailDto`, `KpiDetailGroupDto`, `KpiDetailRowDto` to `AnalyticsDtos.cs` exactly as specified above. Nullable `Quantity` is deliberate: it is the "field stays empty" mechanism for Produced.
   - *Dependencies*: None.

2. **Expose one period-window resolver**
   - *Details*: In `SuperAdminDashboardAggregator.cs`, promote `PeriodSpec` to a public `DashboardPeriodWindow` record (`Key`, `Label`, `Start`, `End`, `EffectiveEnd`, `DateLabel`, `ReportsTimeframe`). Add `public static DashboardPeriodWindow ResolveWindow(DateTime anchor, DateTime today, string? timeframe)` accepting `today/today/this week/weekly/monthly/monthly/yearly/year` (same normalization used by `GetOverviewApi`) and falling back to `Today`. `PeriodSpecs` must build from the same code path so the KPI cards and the drawer can never disagree about a window.
   - *Dependencies*: None.

3. **Extend the service interface**
   - *Details*: In `Manufacture.Library/Interfaces/ISuperAdminDashboardService.cs` add:
     `KpiDetailDto GetKpiDetail(string? metric, string? timeframe, string? scopeKey, DateTime? date = null);`
   - *Dependencies*: Depends on step 1.

### Phase B — Re-base Orders Placed on the order register

4. **Replace route-request facts with order facts**
   - *Details*: In `SuperAdminDashboardService.BuildFacts`:
     - Remove `OrdersPlaced = item.RepOrderRequestedQty` from the route-sheet fact (leave every other route-sheet quantity untouched).
     - Add an `Orders` block before the batch block:
       ```csharp
       var orders = _db.Orders.AsNoTracking()
           .Include(o => o.SalesRep)
           .Include(o => o.OrderItems).ThenInclude(i => i.Recipe)
               .ThenInclude(r => r.ProductionProductType)
                   .ThenInclude(pt => pt!.ProductionCategory)
           .Where(o => o.OrderDate >= start && o.OrderDate < endExclusive
               && o.Status != (int)OrderStatus.Cancelled)
           .ToList();

       foreach (var order in orders)
       foreach (var item in order.OrderItems)
       {
           var category = item.Recipe?.ProductionProductType?.ProductionCategory;
           facts.Add(new DashboardFact
           {
               Date = order.OrderDate.Date,
               RepId = order.SalesRepId ?? 0,
               RepName = order.SalesRepName ?? "Unassigned Rep",
               Route = order.SalesRep?.RouteName ?? string.Empty,
               Terminal = order.PosTerminalCode ?? string.Empty,
               RecipeId = item.RecipeId,
               ProductCode = item.Recipe?.Code ?? string.Empty,
               ProductName = item.ProductName,
               ProductionCategoryId = category?.Id ?? 0,
               ProductionCategoryName = category?.Name ?? "Uncategorised",
               OutputUnit = item.Recipe?.OutputUnit ?? "units",
               UnitPrice = item.UnitPrice,
               OrdersPlaced = item.Quantity,
               // Revenue stays sales-driven: never book order value here.
               ExpectedRevenue = 0m
           });
       }
       ```
     - Keep `RequestsPlaced` nowhere; `RouteSheetItem.RepOrderRequestedQty` remains available to the Loaded detail as `SecondaryQuantity`.
   - *Dependencies*: None (independent of steps 1–3).

5. **Update the Orders Placed copy**
   - *Details*: In `_ExecutiveOverview.cshtml` change the stage subtitle `"Requested by reps"` → `"Booked in the order register"` and the help line `"Orders Placed — rep request quantities before departure."` → `"Orders Placed — sales orders booked in the register for the period."` In `SuperAdminDashboardAggregator.BuildStageDrillDown` change the `OrdersPlaced` `Note` to `"Booked in the Sales order register."` (`GroupingLabel` stays `"By Sales Rep & Product Category"`).
   - *Dependencies*: Depends on step 4.

### Phase C — Detail builder & service queries

6. **Create the pure grouping builder**
   - *Details*: New file `Manufacture.Web/Services/SuperAdminKpiDetailBuilder.cs` in namespace `Manufacture.Services`. Add small flattened source records (settable properties so EF can project with `Select`):
     `OrderLineSource`, `BatchSource`, `DispatchLineSource`, `ReturnSource`, `DefectSource`.
     Static methods, all pure and unit-testable:
     ```csharp
     public static KpiDetailDto BuildOrders(IEnumerable<OrderLineSource> lines, string periodKey, string dateLabel, string scopeLabel);
     public static KpiDetailDto BuildProduced(IEnumerable<BatchSource> batches, string periodKey, string dateLabel, string scopeLabel);
     public static KpiDetailDto BuildDispatch(IEnumerable<DispatchLineSource> lines, LifecycleMetric metric, string periodKey, string dateLabel, string scopeLabel);
     public static KpiDetailDto BuildDefects(IEnumerable<DefectSource> defects, string periodKey, string dateLabel, string scopeLabel);
     public static KpiDetailDto BuildReturns(IEnumerable<ReturnSource> returns, string periodKey, string dateLabel, string scopeLabel);
     ```
     Behaviour:
     - Group key/label: rep (`rep-{id}`, name, `route • vehicle • terminal`) or category (`category-{id}`, name, `Plant production`).
     - Order lines are merged per order (`OrderId`) into a single row: `Product` = `"Jumbo Bread ×20, Meat Pie ×5"`, `Quantity` = sum of in-scope quantities, `Amount` = Σ(`Quantity × UnitPrice`), `Meta` = customer, `Status` = order status.
     - Produced rows are ordered by category, then product, then batch; `Quantity = ActualQuantity > 0 ? ActualQuantity : null`.
     - Unit resolution: distinct non-empty row units; one distinct → that unit, otherwise `"units"`.
     - Group totals and grand total are computed **before** the row cap; cap at `MaxRowsPerDetail = 1000` (drop oldest rows first) and set `IsTruncated` + `RecordCount`.
     - Sort groups by their metric total desc, then label; `Unassigned Rep` always last.
     - Status tones: `success` for Reconciled/Completed, `warning` for LoadedOnRoute/Baking/InMixing/Planned/PartiallyPaid, `error` for Cancelled, `info` for ReadyToLoad/Confirmed, `ghost` otherwise.
   - *Dependencies*: Depends on step 1.

7. **Implement `GetKpiDetail` in the dashboard service**
   - *Details*: In `SuperAdminDashboardService.cs`:
     - Resolve `anchor` exactly like `GetOverview` (clamp to today) and call `SuperAdminDashboardAggregator.ResolveWindow(anchor, today, timeframe)`.
     - Resolve scope: `all` → `categoryId = null`, label `"All Product Lines"`; `category-{id}` → `categoryId = id`; unknown → `all`.
     - Metric switch (all queries `AsNoTracking()` and projected to the builder's source records; category filter applied inside SQL where possible, otherwise on the projected list):
       - Orders: `OrderDate` in window, not cancelled, `Include(SalesRep)` + items/recipe/category.
       - Produced: `ProductionDate` in window, `Status != Cancelled`, batch → recipe → product type → category.
       - Loaded / Sold / Returned / Damaged: `RouteDate` between `Start` and `EffectiveEnd`; `Status >= LoadedOnRoute` for Loaded, `Status >= Reconciled` for the other three; items → recipe → product type → category, plus `SalesRep` and `Vehicle`.
       - FactoryDefects: defects whose batch is in the window and not cancelled.
       - CustomerReturns: `ReturnDate` in window, `Include(Recipe.ProductionProductType.ProductionCategory)` + `SalesRep` + `Customer`.
     - Default unknown metric to `OrdersPlaced`.
     - Fill the returned DTO's window metadata (`PeriodKey`, `PeriodLabel`, `DateLabel`, `ScopeKey`, `ScopeLabel`, `GroupingLabel`, `Note`, `Unit`).
   - *Dependencies*: Depends on steps 2, 3, 6.

8. **Add the page handler**
   - *Details*: In `Pages/Dashboard/Index.cshtml.cs` add:
     ```csharp
     public IActionResult OnGetDrill(string? metric, string? timeframe, string? scope, DateTime? date)
     {
         var role = HttpContext.Session.GetString("UserRole");
         if (!string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
             return Forbid();

         return new JsonResult(_analytics.GetKpiDetail(metric, timeframe, scope, date));
     }
     ```
     MVC's `JsonOptions` already emit camelCase (`JsonSerializerDefaults.Web`), matching the existing pre-serialized payloads in `Index.cshtml`. `RequireSessionPageFilter` still runs for handlers, so anonymous calls redirect to `/Login`.
   - *Dependencies*: Depends on step 7.

### Phase D — Drawer UI

9. **Add the Records tab and container**
   - *Details*: In `_ExecutiveOverview.cshtml`:
     - Add a `tab-btn-records` button (default `tab-active`) labelled `Records` with `fa-list-ul`, placed before the Stage Breakdown tab.
     - Add `#drill-records-container` (visible by default) containing:
       - a summary strip with `#drill-records-grouping-badge`, `#drill-records-total-badge`, `#drill-records-value-badge`, `#drill-records-note`;
       - `#drill-records-loading` (daisyUI `loading loading-spinner` + "Loading records…");
       - `#drill-records-error` (`alert alert-error` with a `Retry` button);
       - `#drill-records-groups` (server-rendered group blocks via JS);
       - `#drill-records-empty` (existing empty-state copy pattern).
     - Hide `#drill-stage-container` by default and remove `tab-active` from `tab-btn-stage`.
     - Keep the Product Orders and Sales Rep Allocations tabs untouched.
   - *Dependencies*: Depends on step 8 (markup only needs the container ids; behaviour lives in step 10).

10. **Wire lazy loading, rendering and search**
    - *Details*: In `Pages/Dashboard/Index.cshtml` `<script>`:
      - State: `let detail = null, detailKey = '', detailLoading = false;`
      - `window.openDrill(scopeKey, periodKey, stageMetric, targetView)`: default `viewMode = 'records'` when called from a KPI card (`'stage'` from the KPI cards must map to `'records'`; product-card CTAs keep `'reps'`).
      - `loadDetail()`: guard against a stale key; `fetch('/Dashboard?handler=Drill&metric=' + encodeURIComponent(metric) + '&timeframe=' + encodeURIComponent(periodOf(period).timeframe) + '&scope=' + encodeURIComponent(scope) + '&date=' + anchorDate)` with `credentials: 'same-origin'`; toggle loading/error/empty states; store `detail` and `detailKey`.
      - `renderRecordsView()`: header badges (`groupingLabel`, `total + unit`, `TotalValue` when > 0, `note`), then one block per group:
        - group header row: label, `SubLabel`, `RecordCount record(s)`, `Total unit`, `TotalValue`;
        - rows table using a per-metric column recipe (`recordColumns`), quantities right-aligned in `font-mono`, `null` quantity rendered as an empty cell, status as a daisyUI badge, `Reference` linking to `ReferenceUrl` when present;
        - grand-total row from `detail.total` / `detail.totalValue`;
        - truncation caption when `detail.isTruncated`.
      - `recordColumns` recipes (few, descriptive):
        `OrdersPlaced`: Reference · Product · Date · Units · Value · Status (customer in `Meta` under Reference)
        `Produced`: Reference · Product · Category · Target · Actual · Unit · Status (shift in `Meta`)
        `Loaded`: Reference · Product · Category · Loaded · Requested · Value · Status
        `FactoryDefects`: Reference · Product · Category · Defects · Reason (`Meta`) · Date
        `Sold`: Reference · Product · Category · Sold · Unit price · Value · Status
        `Returned`: Reference · Product · Category · Closing · Loaded · Status
        `Damaged`: Reference · Product · Category · Damaged · Loaded · Value · Status
        `CustomerReturns`: Reference · Product · Customer (`Meta`) · Qty · Refund · Reason (`Meta`) · Date
      - `switchDrillTab(mode)`: show/hide the new container; when `mode === 'records'` call `loadDetail()` if `detailKey !== currentKey()`.
      - `applyExecPeriod`, `selectDrillScope` and `selectDrillPeriod`: after updating state, if `viewMode === 'records'` call `loadDetail()`.
      - Search input: when in records mode, filter groups/rows client-side from the cached `detail` (match reference, product, category, meta, status) and re-render; never refetch.
      - Footer summary: `Showing N group(s) · M records · T total <metric>`; keep the Reports deep link unchanged.
      - Leave the existing `stageData`, `repData` and `productData` paths intact for the other three tabs.
    - *Dependencies*: Depends on steps 8, 9.

### Phase E — Tests & verification

11. **Builder unit tests**
    - *Details*: New `Manufacture.Tests/SuperAdminKpiDetailBuilderTests.cs` covering:
      - Produced: zero/`null`-actual batches produce `Quantity == null`, blank cells, and are excluded from group/grand totals; groups sorted by category; `Unit` resolution.
      - Loaded: `LoadedOnRoute` rows included with `pending reconciliation` tone, `Reconciled` rows included with `success`, `Ordered` rows excluded, group totals equal the sum of loaded quantities.
      - Sold/Closing/Damaged: only reconciled lines appear; totals equal the sums.
      - Orders: multiple item lines on one order merge into one row; unassigned rep group sorts last; units/value arithmetic.
      - Returns and Factory Defects: grouping, sorting, and "Unassigned Rep" fallback.
      - Truncation: >1000 rows sets `IsTruncated`, preserves `Total`/`RecordCount`.
    - *Dependencies*: Depends on step 6.

12. **Window resolver test**
    - *Details*: Extend `SuperAdminDashboardAggregatorTests.cs` with `ResolveWindow_MatchesTheKpiPeriods` for `today`, `weekly`, `monthly`, `yearly`, verifying `Start`/`EffectiveEnd` equal the corresponding `KpiPeriodCardDto` window (Sunday–Saturday week, month start, year start).
    - *Dependencies*: Depends on step 2.

## Relevant Files & Symbols

| File | Symbols / changes |
|---|---|
| `Manufacture.Library/Models/DTOs/AnalyticsDtos.cs` | New `KpiDetailDto`, `KpiDetailGroupDto`, `KpiDetailRowDto` |
| `Manufacture.Library/Interfaces/ISuperAdminDashboardService.cs` | New `GetKpiDetail(...)` |
| `Manufacture.Web/Services/SuperAdminDashboardService.cs` | `BuildFacts` order-fact block (remove route-request `OrdersPlaced`), new `GetKpiDetail`, scope resolver |
| `Manufacture.Web/Services/SuperAdminDashboardAggregator.cs` | `DashboardPeriodWindow`, `ResolveWindow`, `PeriodSpecs` reuse, `OrdersPlaced` note text |
| `Manufacture.Web/Services/SuperAdminKpiDetailBuilder.cs` | **New** pure builder + source records + `MaxRowsPerDetail` |
| `Manufacture.Web/Pages/Dashboard/Index.cshtml.cs` | `OnGetDrill` handler + SuperAdmin guard |
| `Manufacture.Web/Pages/Dashboard/_ExecutiveOverview.cshtml` | Records tab, records container, Orders Placed copy |
| `Manufacture.Web/Pages/Dashboard/Index.cshtml` | Drawer JS: `loadDetail`, `renderRecordsView`, `recordColumns`, tab/period/scope wiring, search |
| `Manufacture.Tests/SuperAdminKpiDetailBuilderTests.cs` | **New** builder tests |
| `Manufacture.Tests/SuperAdminDashboardAggregatorTests.cs` | `ResolveWindow` regression test |

**Reuse, don't re-implement**
- Period bucketing: `SuperAdminDashboardAggregator.PeriodSpecs` / `ResolveWindow`.
- Existing drawer open/close, tab, scope and period helpers (`openDrill`, `closeDrillDrawer`, `switchDrillTab`, `paintStrip`, `periodOf`, `reportsUrl`).
- Formatting: `ReportColumnCatalog.FormatMoney` on the server, `money()`/`num()` in the drawer JS.
- Status enums: `OrderStatus`, `BatchStatus`, `RouteSheetStatus` (never compare raw ints in the view).

## Verification

1. **Build**
   ```
   dotnet build Manufacture.slnx -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\
   ```
2. **Tests**
   ```
   dotnet test Manufacture.Tests/Manufacture.Tests.csproj --no-build -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\
   ```
   Expected: 0 warnings, 0 errors; existing suite plus the new builder/window tests pass.
3. **Manual (SuperAdmin)**
   - Sign in (`admin@bakery.com` / `Password123!`) → `/Dashboard`.
   - `Orders Placed` card and drawer total **match**; drawer groups by rep, rows mirror `/Sales/Orders` (cancelled orders absent); unassigned orders fall under "Unassigned Rep".
   - `Produced`: groups by category; a planned batch shows an **empty** Actual cell while completed batches show values; batch totals equal the KPI.
   - `Loaded`: includes signed-off dispatches with `Reconciled` and `pending reconciliation` badges; total equals the KPI.
   - `Sold`, `Closing Stock`, `Damaged`: only reconciled dispatches; totals equal their KPIs.
   - `Returns`: customer-return rows grouped by rep, with reason and refund.
   - `Factory Defects`: rows grouped by category with batch, reason and quantity.
   - Switch **period** (Today → This Week → This Month → This Year) and **scope** (All → a product category) inside the drawer: records reload and totals change; search filters without a network call; Escape/backdrop closes.
   - Trigger a category with no records: clean empty state, not a spinner.
4. **Role guard**
   - Hit `/Dashboard?handler=Drill&metric=Sold` in a non-SuperAdmin session → `403`; anonymous → redirect to `/Login`.

## Decisions & Non-Goals

- **Orders Placed is order-book units.** `OrderItem.Quantity` only (bonus shown separately), `OrderDate` buckets the period, cancelled orders excluded. This intentionally changes the KPI number from route-sheet requests.
- **Revenue is untouched.** Order facts never carry `ExpectedRevenue`; revenue remains `QtySold × UnitSellingPrice` from reconciled route sheets.
- **Loaded is signed-off-inclusive.** It mirrors the KPI and badges reconciliation state. Closing/Sold/Damaged are reconciliation-scoped.
- **Actual output can be blank.** `ActualQuantity <= 0` renders an empty cell; totals only sum recorded output.
- **Lazy fetch over pre-computed JSON.** Eight metrics × four periods × every category scope of row-level data would bloat the page into megabytes; the handler fetches one metric/window/scope on demand and keeps the existing aggregated tabs instant.
- **Rows are capped, totals are not.** Totals and `RecordCount` are computed from the full query; only rendered rows are capped at 1,000 with a truncation caption and the existing "Open in Full Reports" deep link.
- **No schema change.** Everything is derived from existing tables.

## Risks / Watch-outs

- **EF include depth**: `Orders → OrderItems → Recipe → ProductionProductType → ProductionCategory` is a 5-level include. Project only the columns the builder needs; avoid tracking (`AsNoTracking`).
- **Year windows**: `This Year` can span tens of thousands of order lines. The row cap plus a lightweight aggregate for totals keeps the handler responsive; if a year view still feels slow, add `.AsSplitQuery()` rather than changing the contract.
- **KPI drift**: The detail window *must* come from `ResolveWindow`, not a second implementation, or the card and drawer will disagree at month/week boundaries.
- **Mixed units**: a group can mix `loaves`, `bottles` and `packs`; the badge falls back to `"units"` while each row shows its own unit.
- **Session filter on handlers**: `OnGetDrill` must stay on `Index.cshtml` (not a minimal API route) so `RequireSessionPageFilter` and the SuperAdmin guard both apply.

## Context & Decisions Log

- **2026-11-16**: User asked for KPI record drawers with rep/category grouping, descriptive rows, batch actuals left blank when unrecorded, and reconciliation-scoped dispatch detail.
- **Finding**: the current drawer is pre-aggregated (`StageDrillDownScopes`); `OrdersPlaced` was sourced from `RouteSheetItem.RepOrderRequestedQty`; `Loaded` ignored route-sheet status.
- **Decision**: rebase `OrdersPlaced` on `Orders` so KPI and drawer can never disagree; include signed-off dispatches in `Loaded` with a status badge; include Factory Defects; add the Records tab as the default while keeping the aggregated tabs.

## Execution Notes (2026-11-16)

- **Orders Placed** now reads `Orders`/`OrderItems` in `BuildFacts`. Verified against the local database: 382 units for the current month equals the KPI card and the drawer.
- **Reconciliation alignment finding**: the seed database contains one `LoadedOnRoute` sheet (`RTE-2026-001`) that already carries sold/closing/damaged quantities. The KPI used to count them (Sold 399, Closing 34, Damaged 6 for the month) while the reconciled-only drawer showed 288 / 11 / 1. `BuildFacts` now books `Sold`, `ClosingStock`, `Damaged` and `ExpectedRevenue` only for sheets at `Reconciled` or later, so every card equals its drawer:
  - Monthly: Orders 382, Produced 201, Loaded 428, Sold 288, Closing 11, Damaged 1, Returns 8 — all `MATCH`.
  - Yearly: identical `MATCH` across all seven metrics.
  - Category scopes partition the `all` total (Produced: Bread 201 + Snacks 0 + Water 0 = 201).
- **Verification**: `dotnet build` and `dotnet test` pass (255 tests, 0 warnings). The rendered drawer JavaScript parses under `node --check`. `/Dashboard?handler=Drill` returns `200` for the SuperAdmin session and `302 → /Login` for anonymous callers.
- **Not covered by live data**: no batch currently has an empty actual quantity, so the blank-actual field is covered by `SuperAdminKpiDetailBuilderTests` rather than the seed data.
