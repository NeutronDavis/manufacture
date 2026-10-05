```
# TASK: Build the Executive Dashboard Quick-Overview & the /reports Analytics Engine

## Objective
Give a plant manager a two-minute read on the day's operations from the dashboard,
and a drillable, exportable analytics engine at `/reports`. There is no database yet,
so the numbers are synthesised deterministically on top of the live recipe master, the
sales-rep roster and the vehicle fleet.

---

## 1. Core Business Logic & Rules

### A. The six-stage lifecycle
Every figure belongs to a stage, and the stages are a funnel:

  `Orders Placed -> Produced -> Loaded -> Sold -> Returned -> Damaged`

- **Requested** is the sum of what the reps placed; the plant never runs ahead of the
  order book, so `Produced <= OrdersPlaced`.
- **Produced** is the output of the ovens, 82%–100% of requested, so shortfalls are real.
- **Loaded** is what the loading bay released: `Loaded <= Produced`.
- **Sold** is what converted on the route; `Sold + Returned` reconciles back to `Loaded`.
- **Returned** is closing stock off the van.
- **Damaged** is the part of the returns written off: `Damaged <= Returned`.

Derived ratios (all rounded to one decimal): **Fill Rate** (Produced / Orders Placed),
**Sell-Through** (Sold / Loaded), **Damage %** (Damaged / Returned),
**Collection Rate** (Collected / Expected Revenue).

### B. Determinism
There is no database, so figures come from a stable FNV-1a hash of the natural key
`date | sku | rep` rather than `string.GetHashCode()`. Two consequences the engine must
guarantee:

- Reloading a page, toggling Table <-> Charts, or paginating **never changes a number**.
- A single day returns exactly the slice that was aggregated into the wider range.

Per-day cells are cached, so a month report is generated once per day of the month.

### C. Period resolution
The operational week runs **Sunday to Saturday**.

| Timeframe | Window |
|---|---|
| Daily | the anchor day |
| Week | Sunday–Saturday containing the anchor |
| Monthly | 1st to last day of the anchor month |
| Yearly | 1 Jan – 31 Dec |
| Custom | `start_date`/`end_date`, swapped if reversed |

The *effective* end is clamped to the last day that has data, so a report never books a
shift that has not run. A window that has not opened yet stays **empty** rather than
being dragged back to its nominal start.

### D. Product lines
`Bread`, `Water`, `Popcorn`. The sellable catalog is projected from
`MockProductionService.GetAllRecipes()` so a report can only ever describe products the
plant can actually make. Bread SKUs: Jumbo, Medium, Family Loaf, Round, 6-in-1.

### E. Money
`₦` + `N0` everywhere. Tender is split three ways — **cash**, **bank transfer**,
**POS bank** — and each channel is compared against `PlannedCashShare` (32% of
invoiced revenue) to produce the **cash variance** per rep.

### F. Roles
The `Reports` module was added to the RBAC matrix: SuperAdmin Full;
ProductionManager / StoreManager / SalesRep View Only; Vendor / HrPayrollManager None.

---

## 2. Data Models

1. `MockAnalyticsService` — the engine. Catalog, rep roster, range resolution,
   `QueryReports(ReportsQueryDto) -> ReportResultDto`, `GetDashboardOverview() -> ExecutiveOverviewDto`.
2. `ReportsQueryDto` — timeframe, nominal `StartDate`/`EndDate`, `EffectiveEndDate`,
   `IsClamped`, `RangeLabel`, `BucketLabel`, `ProductIds`, `Lines`, `RepIds`, `Route`,
   `Module`, `GroupBy`, `Sort`, `Direction`, `Page`, `Limit`.
3. `ReportResultDto` — `Rows` (the visible page), `AllRows` (every filtered row, JSON-ignored),
   `Columns`, `GrandTotals`, `Tender`, `LogisticsSummary`, `Charts`, pagination.
4. `ReportColumnCatalog` — **single source of truth** for column metadata, value
   formatting, sorting and text keys. The table, the charts, the CSV and the PDF all
   read from it, so they cannot drift apart.
5. `ReportTotals` — the closing TOTAL band as a `column key -> formatted value` map,
   shared by the CSV and the PDF so a figure can never land under the wrong heading.
6. `ReportExportService` (CSV), `SimplePdfWriter` + `ReportPdfService` (PDF).
   No NuGet packages: the PDF writer is ~370 lines of hand-rolled PDF 1.4.

---

## 3. API Endpoints

- `GET /api/dashboard/summary?date=` — executive summary for a day.
- `GET /api/reports/query?start_date&end_date&category&product_id&rep_id&module&page&limit`
  — the same engine the Razor page uses, so the two can never diverge.
- `GET /Reports?handler=ExportCsv` / `?handler=ExportPdf` — file downloads.

---

## 4. UI Requirements

### A. Executive Dashboard (`/Dashboard`)
1. Six lifecycle stage cards in flow order, with the gap from the previous stage called out.
2. Four **cumulative** KPI cards — Today / This Week / This Month / This Year.
3. A product-line table (Bread / Water / Popcorn) with share of volume.
4. Alerts, and a tender split.
5. Clicking a KPI card or a product row opens a **slide-over drawer** with the rep-by-rep
   breakdown for that scope and period (client-side; no round trip).
6. A prominent **View Full Reports** button that deep-links to `/reports` with the
   timeframe, module, product line and rep already applied.

### B. `/reports`
1. **View switcher** defaulting to **Table View**; **Charts View** renders:
   - grouped lifecycle bars (Requested / Loaded / Sold / Returned / Damaged),
   - revenue vs collections line (by day, or by month past 70 days),
   - product volume donut,
   - rep performance horizontal ranking with cash variance.
2. **Filters** applying to both views: timeframe, Bread / Water / Popcorn lines, SKUs,
   reps, route, production line, module (Operational / Financial / Logistics & Fleet).
3. **Filter state persists** across view toggling; charts re-render without losing selections.
4. **Table view** — sortable columns, red-flag rows, a totals strip, and join pagination.
5. **Exports** — CSV and formatted PDF. Both cover *every filtered row*, not just the
   visible page.
6. Accessible and mobile responsive.

---

## 5. Instructions
- Money is `₦` + `N0`; the CSV is written with a UTF-8 BOM so Excel reads the naira sign.
- Unsupported glyphs are folded to WinAnsi before they reach a PDF page (the base-14
  fonts carry no embedded glyph data), and `₦` falls back to `N`.
- Charts use Chart.js from jsdelivr, matching the existing CDN usage; `wwwroot/js/erp-charts.js`
  is the bridge.
- Registration order matters: `MockAnalyticsService` needs Production, User and Logistics.
- `dotnet run` needs `--no-launch-profile`, otherwise `launchSettings.json` overrides `ASPNETCORE_URLS`.
- Tests live in `tests/Manufacture.Tests/MockAnalyticsServiceTests.cs` and cover range
  resolution, clamping, determinism, the lifecycle invariants, allocation, pagination,
  sorting, chart shape, dashboard overview, and both exports.
```
