# Plan: Reconciliation Without Prior Debt

**Status**: Implemented — verified against the local app (2026-11-18)
**Created**: 2026-11-18
**Owner**: Sales reconciliation (`/Sales/Reconciliation`) and the daily route-sheet ledger

## Summary (TL;DR)

Reconciliation becomes a **single-day settlement**:

1. The right-hand sheet header loses `Route` and `Prior Debt`.
2. The `Supermarket Dispatch Debt Register` loses the `Account Balance` and `Credit Limit` columns (and the over-limit warning); it keeps only the supermarket, Settled checkbox, Debt Amount and Recorded By.
3. Prior debt is removed from every calculation: `Updated Running Balance = Gross Sales − Total Payments`. Negative values are shown as an over-collection and count as settled. The `Previous Account Debt` card and the `Prev Debt + …` formula text are removed.
4. Prior debt also stops being **stored forward**: new route sheets start at ₦0, reconciliation no longer writes a balance onto tomorrow's sheet, the persisted `NewOutstandingAccountBalance` column becomes `Gross Sales − Payments` (so reports and the SuperAdmin dashboard also exclude prior debt), and existing `PreviousAccountBalance` values are zeroed.

## User Requirements (verbatim)

- "in the /Sales/Reconciliation page, at the right side, remove the `Route` and `Prior Debt`"
- "remove the `Credit Limit field` at the `Supermarket Dispatch Debt Register` section"
- "remove all the debt in the form the existing supermarket and make user the calculation at the `Lump-Sum Payment Collection Panel (No SKU Breakdown)` section should not consider any `Prior Debt` amount"
- "`Prior Debt` field should not be considerd for another payment in any reconsilation payment, note the target for reconsilation should compare to the `Gross Sales`"
- "remove the `Previous Account Debt` form the `Daily Account Ledger Reconciliation` section"

## Decisions (verbatim answers)

| Question | Answer |
|---|---|
| Debt register trimming | **"Remove balance + limit (Recommended)"** — drop the Account Balance and Credit Limit columns and the over-limit warning. |
| Prior-debt carry | **"Stop carrying + ignore (Recommended)"** — new sheets start at ₦0, no next-day carry, no prior debt in any maths; the column stays for history. |
| Outstanding balance | **"Gross − Payments, allow overpay (Recommended)"** — negative = over-collection; status "Settled" when payments ≥ Gross Sales. |
| Reports & dashboard | **"Update DB + reports (Recommended)"** — the persisted `NewOutstandingAccountBalance` column becomes Gross − Payments. |
| Existing data | **"Zero old values"** — set every route sheet's `PreviousAccountBalance` to 0. |

## Current State Findings

| Location | Today | Change |
|---|---|---|
| Header (`Index.cshtml` ~L143-147) | Rep, Vehicle, **Route**, **Prior Debt** | Remove Route + Prior Debt |
| Debt register (~L507-544) | Supermarket, **Account Balance**, **Credit Limit**, Settled, Debt Amount, Recorded By | Remove Account Balance + Credit Limit (and `overLimit`) |
| Callout (~L371-428) | 5 cards incl. **Previous Account Debt**; formula `Prev Debt + Gross Sales - Total Payments`; status compares to `PreviousAccountBalance` | 4 cards; formula `Gross Sales - Total Payments`; status compares to ₦0 |
| Page JS (`recalcReconciliation`) | `prevBalance` from a hidden input drives `newBalance` | `newBalance = totalExpected - totalCollected`; remove hidden input |
| Domain `DailyRepRouteSheet.NewOutstandingAccountBalance` | `PreviousAccountBalance + Gross − Payments` | `Gross − Payments` |
| `DailyRepRouteSheets.NewOutstandingAccountBalance` (DB, persisted computed) | `PreviousAccountBalance + TotalExpectedSalesRevenue − payments` | `TotalExpectedSalesRevenue − payments` |
| `SalesService.CreateRouteSheetAsync` | Sets `PreviousAccountBalance` from the previous sheet | Always `0m` (still reads the previous sheet for closing stock + supermarkets) |
| `SalesService.ReconcileDailyRepAccountAsync` | Computes prior-inclusive `newBalance`, writes it onto the next sheet | Drop both; next sheet only receives closing stock |
| `SuperAdminDashboardService` (~L500) | Falls back to prior-inclusive balance | Fallback also Gross − Payments |
| `ReportQueries` (~L2094) | Reads the persisted balance | Automatically corrected by the DB column change |
| `MockSalesService` + `RouteSheetAndSalesTests` | Mock carries prior balances; one test asserts ₦20,000 kept | Mock stops carrying; test becomes "prior debt ignored" expecting ₦0 |
| Seed (`scripts/seed-database.sql`) / live rows | Seeded route sheets carry 10k-22k `PreviousAccountBalance` | Idempotent zeroing block |

## Scope

**In scope**
- Reconciliation header, debt register and callout markup changes.
- Client-side recalculation without prior debt, including over-collection display.
- Domain and persisted computed column changes; route-sheet creation/reconciliation no longer carry balances.
- Dashboard fallback + reports alignment via the DB column.
- One-time zeroing of existing `PreviousAccountBalance` rows (also kept idempotent in the seed script).
- Updating the affected mock service and its unit test.

**Out of scope**
- Dropping the `PreviousAccountBalance` column (kept for history, unused).
- Changing stock balancing (sold / returned / damaged / replacements), supermarket debt recording, or the supermarket credit flow.
- Changing `Net Revenue` (Gross − returns − damages); it remains an informational card.
- Changing reconciliation status transitions, RBAC, or the print layout beyond the removed fields.
- Changing any other page that does not read `NewOutstandingAccountBalance`.

## Implementation Steps

### Phase 1 — Data model and persisted column

1. **Domain calculation**
   - *Details*: `Manufacture.Library/Models/Domain/RouteSheetEntities.cs`, `DailyRepRouteSheet`:
     - `NewOutstandingAccountBalance => TotalExpectedSalesRevenue - TotalPaymentsCollectedToday;`
     - Update the XML doc: "Daily balance owed: Gross Sales − payments. Prior debt is intentionally excluded."
     - Keep the `PreviousAccountBalance` property but annotate it as legacy/unused in calculations.
   - *Dependencies*: None.

2. **Persisted computed column**
   - *Details*:
     - `Manufacture.Database/Tables/DailyRepRouteSheets.sql`: change
       `[NewOutstandingAccountBalance] AS (([PreviousAccountBalance] + [TotalExpectedSalesRevenue]) - (...)) PERSISTED`
       to `AS (([TotalExpectedSalesRevenue]) - (([CashCollected] + [BankTransferCollected]) + [PosTerminalCollected])) PERSISTED`.
     - `Manufacture.Library/Data/ManufactureDbContext.cs` (~L379): matching `HasComputedColumnSql("([TotalExpectedSalesRevenue]-(([CashCollected]+[BankTransferCollected])+[PosTerminalCollected]))", true)`. Keep `HasColumnType("decimal(21, 2)")`.
     - Keep `PreviousAccountBalance` and the `CK_DailyRepRouteSheets_Amounts` check as-is.
   - *Dependencies*: Parallel with Step 1.

3. **Zero historical prior debt**
   - *Details*: Append an idempotent block to `scripts/seed-database.sql`:
     ```sql
     UPDATE [dbo].[DailyRepRouteSheets]
     SET [PreviousAccountBalance] = 0
     WHERE [PreviousAccountBalance] <> 0;
     ```
     It runs on the next deploy and keeps the data permanently zeroed.
   - *Dependencies*: Parallel with Step 2.

### Phase 2 — Services

4. **Stop creating/carrying prior balances**
   - *Details*: `Manufacture.Web/Services/SalesService.cs`:
     - `CreateRouteSheetAsync`: set `PreviousAccountBalance = 0m` (remove `previousSheet?.NewOutstandingAccountBalance ?? 0m`). Keep the previous-sheet query for `YesterdayClosingStock` and supermarket inheritance.
     - `ReconcileDailyRepAccountAsync`: delete the `newBalance = PreviousAccountBalance + …` computation and the `nextSheet.PreviousAccountBalance = newBalance;` assignment. Keep the closing-stock roll-over and its `SaveChangesAsync`.
   - *Dependencies*: Depends on Steps 1-3.

5. **Dashboard fallback**
   - *Details*: `Manufacture.Web/Services/SuperAdminDashboardService.cs` (~L500): replace the fallback with
     `sheet.TotalExpectedSalesRevenue - sheet.CashCollected - sheet.BankTransferCollected - sheet.PosTerminalCollected`
     (no `PreviousAccountBalance`, no `Math.Max` clamp, so over-collections stay visible).
     `ReportQueries` needs no edit — it already reads the persisted column.
   - *Dependencies*: Depends on Step 2. Parallel with Step 4.

6. **Mock service and tests**
   - *Details*:
     - `Manufacture.Web/Services/MockSalesService.cs`: `CreateRouteSheet` sets `PreviousAccountBalance = 0m` (delete `prevBalance`); `ReconcileDailyRepAccount` removes `nextDaySheet.PreviousAccountBalance = …`.
     - `Manufacture.Tests/RouteSheetAndSalesTests.cs`: rename `ReconcileDailyRepAccount_CalculatesExpectedRevenueAndCarriesOverBalance` to `ReconcileDailyRepAccount_IgnoresPriorDebt`; keep setting `sheet.PreviousAccountBalance = 20000m` to prove it is ignored; assert `0m` (Gross ₦63,000 − Paid ₦63,000) instead of ₦20,000.
   - *Dependencies*: Depends on Step 1. Parallel with Steps 4-5.

### Phase 3 — Reconciliation page

7. **Header + hidden input**
   - *Details*: `Manufacture.Web/Pages/Sales/Reconciliation/Index.cshtml`:
     - Delete the `<span>…Route…</span>` and `<span>…Prior Debt…</span>` entries in the sheet header.
     - Delete `<input type="hidden" id="prevBalanceHidden" …>`.
   - *Dependencies*: Depends on Step 1.

8. **Callout**
   - *Details*: Same file (~L371-428):
     - Formula badge: `Gross Sales − Total Payments`.
     - Remove the `Previous Account Debt` card; change the grid to `grid-cols-2 sm:grid-cols-4`.
     - Add at the top of the selected-sheet block:
       `var dayOutstanding = sheet.TotalExpectedSalesRevenue - sheet.TotalPaymentsCollectedToday;`
       `var isDaySettled = dayOutstanding <= 0;`
     - Updated Running Balance card: use `isDaySettled` for the border/text classes and show `-₦` for negative values, e.g. `@(dayOutstanding < 0 ? "-₦" : "₦")@Math.Abs(dayOutstanding).ToString("N0")`.
     - Status text: `@(isDaySettled ? "Settled / Over-collected ✔️" : "Outstanding ⚠️")`.
   - *Dependencies*: Depends on Step 1.

9. **Debt register**
   - *Details*: Same file (~L507-565):
     - Delete `<th class="text-right">Account Balance</th>` and `<th class="text-right">Credit Limit</th>` and their `<td>` cells.
     - Remove `overLimit`, `data-credit`, `data-limit` and the over-limit badge.
     - Keep Supermarket, Settled checkbox, Debt Amount, Recorded By, the explanatory paragraph and Save button unchanged.
   - *Dependencies*: Parallel with Step 8.

10. **Client-side recalculation**
    - *Details*: Same file, `@section Scripts`:
      - `recalcReconciliation`: delete `prevBalance` and the hidden-input read; `var newBalance = totalExpected - totalCollected;`.
      - Status branch: when `newBalance > 0` → outstanding/error classes + "Outstanding ⚠️"; otherwise success classes + "Settled / Over-collected ✔️".
      - Format the balance with a negative prefix (`-₦`) instead of `₦-…`.
      - `recalcDebtTotal`: remove the `data-credit` / `data-limit` / `overLimit` handling; keep the entered-total calculation.
    - *Dependencies*: Depends on Steps 7-9.

### Phase 4 — Deploy and verify

11. **Build, deploy, validate**
    - *Details*: Build the solution and tests, deploy the database, confirm the computed column and zeroed values, then walk the manual flows.
    - *Dependencies*: Depends on all previous steps.

## Relevant Files & Symbols

| File | Change |
|---|---|
| `Manufacture.Library/Models/Domain/RouteSheetEntities.cs` | `NewOutstandingAccountBalance` = Gross − Payments; legacy note on `PreviousAccountBalance` |
| `Manufacture.Database/Tables/DailyRepRouteSheets.sql` | Computed column formula |
| `Manufacture.Library/Data/ManufactureDbContext.cs` | Matching `HasComputedColumnSql` (~L379) |
| `scripts/seed-database.sql` | Idempotent prior-balance zeroing |
| `Manufacture.Web/Services/SalesService.cs` | `CreateRouteSheetAsync` (~L1163), `ReconcileDailyRepAccountAsync` (~L1499, ~L1517) |
| `Manufacture.Web/Services/SuperAdminDashboardService.cs` | `Outstanding` fallback (~L500) |
| `Manufacture.Web/Services/MockSalesService.cs` | Mock create/reconcile carry logic |
| `Manufacture.Tests/RouteSheetAndSalesTests.cs` | Prior-debt-ignored assertion |
| `Manufacture.Web/Pages/Sales/Reconciliation/Index.cshtml` | Header, debt register, callout, scripts |
| `Manufacture.Web/Services/Reports/ReportQueries.cs` | No edit; corrected by the column change |

**Reuse, don't reinvent**
- `SalesService.ReconcileDailyRepAccountAsync` already updates the next sheet's closing stock — keep that untouched and only remove the balance line.
- `Model.NetRevenue` stays as an informational value.
- The existing `formatMoney` helper and debt-total script remain; only the prefill/limit logic is removed.

## Verification Plan

1. **Build & tests**
   - `dotnet build Manufacture.slnx`
   - `dotnet test Manufacture.Tests/Manufacture.Tests.csproj`
2. **Schema deploy**
   - `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/deploy-database.ps1`
   - SQL checks:
     - `sys.computed_columns` definition for `NewOutstandingAccountBalance` no longer mentions `PreviousAccountBalance`.
     - `SELECT COUNT(*) FROM DailyRepRouteSheets WHERE PreviousAccountBalance <> 0` = 0.
3. **Manual flows** (use a `LoadedOnRoute` sheet)
   - Header shows only Rep + Vehicle.
   - Debt register shows Supermarket, Settled, Debt Amount, Recorded By — no balance or limit.
   - Gross ₦63,000, payments ₦63,000 → balance ₦0, "Settled"; payments ₦40,000 → ₦23,000 "Outstanding"; payments ₦70,000 → `-₦7,000`, "Settled / Over-collected".
   - No `Previous Account Debt` card; formula badge reads `Gross Sales − Total Payments`.
   - Create a new route sheet → DB `PreviousAccountBalance = 0`.
   - Reconcile a sheet with an existing next-day sheet → its `PreviousAccountBalance` stays 0 while closing stock still rolls over.
   - Dashboard/report "Outstanding" values match Gross − Payments for a sample sheet.
4. **Regression**
   - Stock balancing, supermarket debt save, sign-off and print still work.
   - The prior supermarket debt ledger behavior is unchanged (it still updates the supermarket's own credit).

## Key Decisions & Trade-offs

| Topic | Decision | Why |
|---|---|---|
| Prior debt | Fully excluded from display and maths | One-day settlement was explicitly requested; prior debt should never compound |
| Carry-forward | Stopped, not just hidden | Prevents invisible debt reappearing in reports later |
| Overpayments | Allowed and shown as negative | Keeps the true cash position instead of silently clamping |
| Persisted column | Rewritten in DB | Reports and the dashboard read it directly; changing only the page would leave inconsistent numbers |
| `PreviousAccountBalance` column | Kept, zeroed, unused | Avoids a destructive schema drop while guaranteeing no stale values |
| Debt register | Balance + limit removed, debt entry kept | The user asked to remove the supermarket's existing debt display, not the dispatch debt recording feature |

## Implementation Record

Completed on branch `development`, 2026-11-18.

- **Build**: `dotnet build Manufacture.slnx` — 0 warnings, 0 errors.
- **Tests**: `dotnet test` — 270 passed, 0 failed (the carry-over test is now `ReconcileDailyRepAccount_IgnoresPriorDebt` and asserts ₦0).
- **Schema deployed**:
  - `sys.computed_columns` now reads `([TotalExpectedSalesRevenue]-(([CashCollected]+[BankTransferCollected])+[PosTerminalCollected]))`.
  - `SELECT COUNT(*) FROM DailyRepRouteSheets WHERE PreviousAccountBalance <> 0` = 0.
  - Sample rows balance correctly (e.g. sheet 7: gross 64,500 − paid 64,500 = 0).
- **Page verification** on `/Sales/Reconciliation?sheetId=2011` (LoadedOnRoute, 3 supermarket stops):
  - No `Previous Account Debt` card, no `Prior Debt`, no `Route:` header span.
  - No `Credit Limit` and no `Account Balance` column in the dispatch debt register.
  - No `prevBalanceHidden` input.
  - Formula reads `Gross Sales - Total Payments`; callout uses the 4-card grid; Updated Running Balance card present.
  - Debt register renders Supermarket / Settled? / Debt Amount / Recorded By only.
- **New-sheet check**: created a Planned delivery dispatch and a route sheet for 2026-12-28; DB stored `PreviousAccountBalance = 0`. Test rows deleted afterwards.
- The dev server used for verification was stopped afterwards (the user starts it themselves).
