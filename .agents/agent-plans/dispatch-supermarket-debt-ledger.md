# Plan: Dispatch Supermarket Debt Ledger

**Status**: Implemented — verified against the local app (2026-11-18)
**Created**: 2026-11-18
**Owner**: Sales dispatch (`/Sales/Dispatch`, `/Sales/Reconciliation`, `/Dashboard`, `/Production/Batches`)

## Summary (TL;DR)

Four connected changes:

1. **`/Production/Batches`**: remove the `Unit Cost` and `Notes` table columns (data, filters, KPIs and everything else stay).
2. **The debt answer**: today the system only knows a supermarket owes money through one running balance — `SupermarketAccounts.CurrentOutstandingCredit` (mirrored to `Customers.OutstandingBalance`). It is raised by `FulfillSupermarketOrderAsync` and lowered by `RecordSupermarketPaymentAsync`. **Nothing links a debt to a dispatch route or to the rep who ran it.**
3. **`/Sales/Dispatch`**: the "New Route Sheet" form gains a **multi-select of supermarket accounts**. Participants are stored in a new `RouteSheetSupermarkets` link table and carried into the next day's auto-created sheet.
4. **`/Sales/Reconciliation`**: a new panel lists every supermarket on that dispatch, each with a **checkbox (checked = settled, no debt)** and a **debt amount textbox** (used when unchecked). A dedicated **Save** button stores one row per dispatch+supermarket in a new `RouteSheetSupermarketDebts` table (FK = route sheet) and increments the supermarket account's debt by the difference only, so re-saving never double-counts. The SalesRep dashboard then shows those debtors **grouped by route, scoped to the signed-in rep's own dispatches**.

## User Requirements (verbatim)

- "update on the /Production/Batches page, remove the `Notes` column form the table including thr `Unit Cost` column"
- "how dose the system know that a particuler supermarket is owing"
- "i surgest that when creating a Dispatch Route at the /Sales/Dispatch page, the form should contain a dropdown list of supermarket and it can be multiple sellected. This is because a dispatch delivery from a factroy dosnot happen to be deliverd to a random shops out there"
- "Also at the reconsilation page add a section displaying all the invlove supermarkets and check box for each with a text box to register a dept amount for that dispatch if the checkbox is not checked . they should be a button to save that to increament the dept field in the supermarket table also they should be a table that will save the data for the supermarket section tying the dispatch route as the FK, then only display the deptors grouped by route in the sales rep dashboard for that specific sales rep that created dispatch"

## Decisions (verbatim answers)

| Question | Answer |
|---|---|
| Supermarket list source | **"Supermarket accounts (Recommended)"** — rows from `SupermarketAccounts` (the `/Sales/Supermarkets` directory). Each already owns `CurrentOutstandingCredit`, `CreditLimit` and custom prices, and is 1:1 with a `Customers` row. |
| Checkbox meaning | **"Checked = settled, no debt (Recommended)"** — checked: paid in full for this dispatch, no debt. Unchecked: owes, and the textbox amount is added to the account debt. |
| Debtor display location | **"Dashboard SalesRep section (Recommended)"** — a card in the `ActiveRole == "SalesRep"` block of `/Dashboard`, scoped to the signed-in rep, grouped by route name then dispatch. |
| Re-saving debt | **"Adjust by delta (Recommended)"** — one row per dispatch+supermarket; re-saving only increments the account by the difference. Read-only once the route sheet is Reconciled/Closed. |
| Dispatch selection rules | **"Optional, carried to next sheet (Recommended)"** — zero or more supermarkets allowed; auto-created next-day sheets inherit the same list (like closing stock). |

## How supermarket debt works today (answer to the user's question)

| Mechanism | Where | Effect on debt |
|---|---|---|
| `SupermarketAccounts.CurrentOutstandingCredit` | `Manufacture.Library/Models/Entities/SupermarketAccount.cs`, table `SupermarketAccounts` | The single authoritative "what this supermarket owes" number |
| `Customers.OutstandingBalance` | table `Customers` | Mirror kept in sync by the service |
| `FulfillSupermarketOrderAsync` | `Manufacture.Web/Services/SalesService.cs` (~L930) | `+=` supplied value; creates an `Orders` + `OrderItems` credit invoice |
| `RecordSupermarketPaymentAsync` | same file (~L835) | `-=` payment, applied to oldest open orders |
| `/Sales/Supermarkets` (`/Sales/PosAccounts/Index`) | shows `CurrentOutstandingCredit` / `CreditLimit` | Read-only visibility; payments recorded from this page |

**Gap**: a dispatch route has no supermarket participants and no debt rows, so a debt cannot be attributed to a route or a rep. This plan adds exactly that attribution.

## Scope

**In scope**
- Remove the `Unit Cost` and `Notes` columns (header + cells) from the `/Production/Batches` list table; keep the `Total Material Cost` KPI, category/date/status filters, search (on remaining fields) and pagination.
- Two new tables: `RouteSheetSupermarkets` (participants) and `RouteSheetSupermarketDebts` (dispatch-level debt ledger with `RouteSheetId` FK).
- EF entities, `DbContext` mappings, domain models, DTOs, `ISalesService` contract.
- Multi-select supermarket picker on route sheet creation; participant badges and an inline edit form while the sheet is `Ordered`.
- Next-day route sheets inherit participants when created automatically.
- Reconciliation debt panel with checkbox + amount per participant, own Save button, delta-based account increment, read-only after reconcile.
- SalesRep dashboard card: debtors grouped by route → dispatch → supermarket, only for sheets where `SalesRepId` is the signed-in rep.
- Database project docs (`Manufacture.Database/README.md`) and a pure calculator with unit tests.

**Out of scope**
- Creating `Orders`/`OrderItems`/`OrderPayments` from this panel. This is a dispatch-credit ledger only; existing supermarket order/payment flow is untouched (payments already reduce the same `CurrentOutstandingCredit`).
- Changing reconciliation cash/stock maths, statuses, or the lump-sum payment drawer.
- Editing participants after the sheet is signed off (`LoadedOnRoute` or later).
- Hard-blocking debts above `CreditLimit` (UI warning only).
- Changing SalesRep navigation, RBAC defaults, or report queries.
- Seeding demo participants/debts into `MockSalesService` (it is not wired to `ISalesService`; dashboard rep data already comes from the DB service).

## Implementation Steps

### Phase 1 — Production Batches table trim

1. **Remove `Unit Cost` and `Notes` columns**
   - *Details*: In `Manufacture.Web/Pages/Production/Batches/Index.cshtml`:
     - Delete `<th class="text-right">Unit Cost</th>` and `<th>Notes</th>` from the header row.
     - Delete the unit-cost `<td>` block that renders `batch.CalculatedUnitCost` / `batch.TotalBatchCost` and the notes `<td>` (`title="@batch.Notes"`).
     - Remove `@(batch.Notes ?? "").ToLower()` from the `data-search` attribute; change the search placeholder from `"Search batch #, recipe, operator, shift..."` to the same minus any notes wording.
     - Do **not** touch `data-cost`, the `Total Material Cost` stat, category/date/status filters, or pagination JS. `batch.CalculatedUnitCost` simply stops being displayed.
   - *Dependencies*: None. Parallel with Phase 2.

### Phase 2 — Database schema and generated model

2. **Add the two tables**
   - *Details*: Create `Manufacture.Database/Tables/RouteSheetSupermarkets.sql`:
     - Columns: `Id INT IDENTITY`, `RouteSheetId INT NOT NULL`, `SupermarketAccountId INT NOT NULL`, `CreatedAt DATETIME2(3) DEFAULT SYSUTCDATETIME()`.
     - `PK_RouteSheetSupermarkets`, `UQ_RouteSheetSupermarkets_RouteSheet_Account (RouteSheetId, SupermarketAccountId)`, `FK_..._DailyRepRouteSheets` with `ON DELETE CASCADE`, `FK_..._SupermarketAccounts` (no cascade).
     - `IX_RouteSheetSupermarkets_SupermarketAccountId`.
   - Create `Manufacture.Database/Tables/RouteSheetSupermarketDebts.sql`:
     - Columns: `Id INT IDENTITY`, `RouteSheetId INT NOT NULL`, `SupermarketAccountId INT NOT NULL`, `IsSettled BIT NOT NULL DEFAULT 0`, `DebtAmount DECIMAL(18,2) NOT NULL DEFAULT 0`, `AppliedCredit DECIMAL(18,2) NOT NULL DEFAULT 0`, `RecordedBy NVARCHAR(150) NOT NULL DEFAULT N''`, `RecordedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME()`, `UpdatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME()`.
     - `PK`, `UQ (RouteSheetId, SupermarketAccountId)`, cascade FK to `DailyRepRouteSheets`, FK to `SupermarketAccounts` (no cascade), `IX` on `SupermarketAccountId`, `CHECK (DebtAmount >= 0 AND AppliedCredit >= 0)`.
   - `AppliedCredit` is the amount this row has already pushed into the account balance; it powers the delta calculation.
   - *Dependencies*: None. Parallel with Step 1.

3. **Register the tables and document them**
   - *Details*: Add both `<Build Include="Tables\...sql" />` entries to `Manufacture.Database/Manufacture.Database.sqlproj`. Update `Manufacture.Database/README.md`: add the two bullets to "Sales and route reconciliation" and extend the ER diagram with `DailyRepRouteSheets ||--o{ RouteSheetSupermarkets`, `SupermarketAccounts ||--o{ RouteSheetSupermarkets`, `DailyRepRouteSheets ||--o{ RouteSheetSupermarketDebts`, `SupermarketAccounts ||--o{ RouteSheetSupermarketDebts`.
   - *Dependencies*: Depends on Step 2.

4. **Add entities and `DbContext` mappings**
   - *Details*: New files `Manufacture.Library/Models/Entities/RouteSheetSupermarket.cs` and `RouteSheetSupermarketDebt.cs` matching scaffold style (`public partial class`, `null!` navigation properties).
     - Add navigation collections: `DailyRepRouteSheet.RouteSheetSupermarkets`, `DailyRepRouteSheet.RouteSheetSupermarketDebts`, `SupermarketAccount.RouteSheetSupermarkets`, `SupermarketAccount.RouteSheetSupermarketDebts`.
     - In `Manufacture.Library/Data/ManufactureDbContext.cs`: add `DbSet<RouteSheetSupermarket>` and `DbSet<RouteSheetSupermarketDebt>`; add `modelBuilder.Entity<...>` blocks mirroring the SQL (unique indexes, `HasPrecision(3)`, default SQL, decimal type, cascade on `RouteSheetId`, `ClientSetNull` on `SupermarketAccountId`).
   - Preferred: hand-write to avoid regenerating every entity. Optional parity check after DB deploy: `pwsh scripts/scaffold-database.ps1` (re-scaffolds all entities; review diff, keep only intended changes).
   - *Dependencies*: Depends on Step 2. Parallel with Step 3.

5. **Deploy the schema**
   - *Details*: `pwsh scripts/deploy-database.ps1` against LocalDB `ManufactureDb` (use `-Recreate` only for a dev reset). Verify both tables and constraints exist.
   - *Dependencies*: Depends on Steps 3 and 4.

### Phase 3 — Domain models, DTOs and contract

6. **Domain models and DTOs**
   - *Details*: In `Manufacture.Library/Models/Domain/RouteSheetEntities.cs`:
     - Add `RouteSheetSupermarketLink` (`SupermarketAccountId`, `AccountName`, `ContactPerson`, `PhoneNumber`, `CurrentOutstandingCredit`, `CreditLimit`, `IsActive`).
     - Add `RouteSheetSupermarketDebt` (`Id`, `RouteSheetId`, `SupermarketAccountId`, `AccountName`, `IsSettled`, `DebtAmount`, `AppliedCredit`, `RecordedBy`, `RecordedAt`).
     - Extend `DailyRepRouteSheet` with `List<RouteSheetSupermarketLink> Supermarkets` and `List<RouteSheetSupermarketDebt> SupermarketDebts`, plus `decimal TotalSupermarketDebt => SupermarketDebts.Where(d => !d.IsSettled).Sum(d => d.DebtAmount)`.
   - In `Manufacture.Library/Models/DTOs/SalesDtos.cs` add `SalesRepRouteDebtorDto` (dispatch header: `RouteSheetId`, `RouteSheetRef`, `RouteDate`, `RouteName`, `SalesRepName`, `Status`, `List<SalesRepRouteDebtorLineDto> Debtors`, `TotalDebt`) and `SalesRepRouteDebtorLineDto` (`SupermarketAccountId`, `AccountName`, `ContactPerson`, `PhoneNumber`, `DebtAmount`, `CurrentOutstandingCredit`, `CreditLimit`, `RecordedBy`, `RecordedAt`).
   - *Dependencies*: None.

7. **Add the pure delta calculator**
   - *Details*: New `Manufacture.Library/Calculations/RouteSupermarketDebtCalculator.cs` (same shape as `LoanRecoveryCalculator`):
     - `CalculateCreditDelta(decimal currentlyApplied, bool isSettled, decimal requestedDebt)` returns `(isSettled ? 0m : Math.Max(0m, requestedDebt)) - Math.Max(0m, currentlyApplied)`.
     - Document that the delta is what gets added to `CurrentOutstandingCredit`.
   - *Dependencies*: None. Parallel with Step 6.

8. **Extend `ISalesService`**
   - *Details*: In `Manufacture.Library/Interfaces/ISalesService.cs`:
     - `CreateRouteSheetAsync(int repId, DateTime date, Dictionary<int, int>? requestedQuantities = null, IReadOnlyCollection<int>? supermarketAccountIds = null, CancellationToken cancellationToken = default)` (nullable list = inherit previous sheet; non-null list, including empty, = exact selection).
     - `Task<bool> UpdateRouteSheetSupermarketsAsync(int sheetId, IReadOnlyCollection<int> accountIds, CancellationToken cancellationToken = default);`
     - `Task<bool> SaveRouteSheetSupermarketDebtsAsync(int routeSheetId, IReadOnlyDictionary<int, decimal> debtAmounts, IReadOnlyCollection<int> settledAccountIds, string recordedBy, CancellationToken cancellationToken = default);`
     - `Task<List<SalesRepRouteDebtorDto>> GetSalesRepRouteDebtorsAsync(int salesRepId, CancellationToken cancellationToken = default);`
   - *Dependencies*: Depends on Steps 6 and 7.

### Phase 4 — SalesService implementation

9. **Participants on create / inherit / edit**
   - *Details*: `Manufacture.Web/Services/SalesService.cs`:
     - `CreateRouteSheetAsync`: after adding `RouteSheetItems`, resolve ids with `_db.SupermarketAccounts.Where(a => a.IsActive && ids.Contains(a.Id))`, de-duplicated. When `supermarketAccountIds == null`, copy `previousSheet.RouteSheetSupermarkets.Select(x => x.SupermarketAccountId)` instead. Add `RouteSheetSupermarket` rows. (`previousSheet` query must `Include(s => s.RouteSheetSupermarkets)`.)
     - `UpdateRouteSheetSupermarketsAsync`: load sheet with `RouteSheetSupermarkets`; return `false` unless `Status == (int)RouteSheetStatus.Ordered`; remove existing links and add the validated active ids; set `UpdatedAt`.
     - `ReconcileDailyRepAccountAsync` already calls `CreateRouteSheetAsync(..., requestedQuantities: null, ...)` for the next day — leave the new argument omitted so the next sheet inherits participants.
   - *Dependencies*: Depends on Steps 5, 6, 8.

10. **Map participants and debts into the domain sheet**
    - *Details*: `GetRouteSheetsAsync` and `GetRouteSheetAsync` add `.Include(s => s.RouteSheetSupermarkets).ThenInclude(x => x.SupermarketAccount)` and `.Include(s => s.RouteSheetSupermarketDebts)`. Extend `MapRouteSheet` (~L1966) to fill `Supermarkets` (account name/contact/credit/limit from the nav) and `SupermarketDebts` (ordered by account name).
    - *Dependencies*: Depends on Steps 5 and 6. Parallel with Step 9.

11. **Save debts with delta semantics**
    - *Details*: `SaveRouteSheetSupermarketDebtsAsync`:
      - Load the sheet with `RouteSheetSupermarkets`, `RouteSheetSupermarketDebts`, and `SalesRep`. Return `false` if missing or `Status != (int)RouteSheetStatus.LoadedOnRoute` (so it can only be written between gate sign-off and reconciliation).
      - Only accept account ids present in `RouteSheetSupermarkets`; ignore unknown/foreign ids.
      - Open a transaction. Load affected accounts with `.Include(a => a.Customer)`.
      - For each participant: existing row lookup by account; `settled = settledAccountIds.Contains(id)`; `amount = Math.Max(0m, requested)`; `delta = RouteSupermarketDebtCalculator.CalculateCreditDelta(existing?.AppliedCredit ?? 0m, settled, amount)`.
        - Skip creating a row when there is no existing row, not settled, and amount is `0`.
        - Upsert row: `IsSettled`, `DebtAmount = settled ? 0m : amount`, `AppliedCredit += delta`, `RecordedBy` (last editor), `RecordedAt`(first) / `UpdatedAt`(always).
        - `account.CurrentOutstandingCredit = Math.Max(0m, account.CurrentOutstandingCredit + delta)` and `account.Customer.OutstandingBalance = Math.Max(0m, account.Customer.OutstandingBalance + delta)`.
      - `SaveChangesAsync` + commit.
      - Never call `FulfillSupermarketOrderAsync` here (no order/invoice is created).
    - *Dependencies*: Depends on Steps 5, 7, 8, 9.

12. **Debtor query for the dashboard**
    - *Details*: `GetSalesRepRouteDebtorsAsync` queries `RouteSheetSupermarketDebts` joined to `DailyRepRouteSheets` and `SupermarketAccounts`, filtered by `sheet.SalesRepId == salesRepId`, `!d.IsSettled`, `d.DebtAmount > 0`. Order by `RouteName`, `RouteDate` desc, account name. Project into `SalesRepRouteDebtorDto` with current account credit/limit/contact. Uses `AsNoTracking`.
    - *Dependencies*: Depends on Steps 5, 6, 8. Parallel with Step 11.

### Phase 5 — Dispatch page multi-select

13. **Dispatch creation form and participant display**
    - *Details*:
      - `Manufacture.Web/Pages/Sales/Dispatch/Index.cshtml.cs`: add `List<SupermarketAccount> Supermarkets` loaded via `GetSupermarketAccountsAsync()` in `OnGetAsync`. Change `OnPostCreateSheetAsync` to accept `int[]? supermarketAccountIds` and pass `(IReadOnlyCollection<int>?)(supermarketAccountIds ?? Array.Empty<int>())` (explicit empty means "none"). Add `OnPostUpdateSupermarketsAsync(int sheetId, int[]? supermarketAccountIds, CancellationToken)` guarded by the existing `CanManageSheetAsync`, setting a success/error TempData message.
      - `Manufacture.Web/Pages/Sales/Dispatch/Index.cshtml`: inside `#newSheetModal`, after "Route Date", add a required-label-free multi-select: `<select name="supermarketAccountIds" multiple size="6" class="select select-sm select-bordered w-full rounded-xl text-xs">` over active accounts (`@acct.AccountName · ₦@acct.CurrentOutstandingCredit`), with helper text "Ctrl/Cmd-click to select multiple. Optional."
      - In the selected sheet header, render participant badges (`@sup.AccountName`). When `sheet.Status == Ordered && Model.CanEdit`, render a compact inline form posting to `UpdateSupermarkets` with the same multi-select pre-selected via `selected="@(...)"`, plus a "Save supermarkets" button. When the list is empty, show "No supermarkets selected for this dispatch."
      - Keep the sign-off table and handler unchanged.
    - *Dependencies*: Depends on Steps 8, 9, 10.

### Phase 6 — Reconciliation debt panel

14. **Page model handler**
    - *Details*: `Manufacture.Web/Pages/Sales/Reconciliation/Index.cshtml.cs`:
      - Add `OnPostSaveSupermarketDebtsAsync(int routeSheetId, Dictionary<int, decimal>? debtAmounts, List<int>? settledSupermarketIds, CancellationToken)`:
        - reuse `CanManageSheetAsync(routeSheetId, ...)`; deny with `TempData["ErrorMessage"]`;
        - `recordedBy = HttpContext.Session.GetString("UserName") ?? "Chief Cashier"`;
        - call `SaveRouteSheetSupermarketDebtsAsync(routeSheetId, debtAmounts ?? new(), settledSupermarketIds ?? new(), recordedBy, ...)`;
        - set success/error TempData and redirect to `new { sheetId = routeSheetId, filterDate = FilterDate }`.
      - Add `public bool CanSaveDebts { get; set; }` computed in `OnGetAsync` as `CanEdit && SelectedSheet?.Status == RouteSheetStatus.LoadedOnRoute`.
      - Optionally expose `public decimal TotalRecordedSupermarketDebt => SelectedSheet?.TotalSupermarketDebt ?? 0m`.
    - *Dependencies*: Depends on Steps 10, 11.

15. **Debt panel UI**
    - *Details*: `Manufacture.Web/Pages/Sales/Reconciliation/Index.cshtml`, **outside** the existing `#reconcileForm` (it closes ~L471) and before the Customer Returns card, add a separate card with its own `<form method="post" asp-page-handler="SaveSupermarketDebts">` (nested forms are invalid HTML, hence the placement).
      - Header: "Supermarket Dispatch Debt Register" with count badge and explanation: "Checked = paid in full for this dispatch. Unchecked = owes the amount entered. Amounts are added to the supermarket's running credit balance."
      - If `sheet.Supermarkets` is empty: empty-state text plus a link to `/Sales/Dispatch` (`asp-page="/Sales/Dispatch/Index" asp-route-sheetId="@sheet.Id"`).
      - Table columns: Supermarket (name + contact/phone), Account Balance (`₦CurrentOutstandingCredit`), Credit Limit, `Settled` checkbox (`name="settledSupermarketIds" value="@sup.SupermarketAccountId"`), `Debt Amount (₦)` `<input type="number" min="0" step="500" name="debtAmounts[@sup.SupermarketAccountId]">`.
      - Pre-fill from `sheet.SupermarketDebts` by `SupermarketAccountId`: checkbox checked when `IsSettled`; amount value = `DebtAmount` (blank when 0). Rows without a debt record default unchecked and blank.
      - Hide inputs and the Save button when `!Model.CanSaveDebts`; show a read-only table with recorded amounts + `RecordedBy`/`RecordedAt` instead. Show "Sign off the dispatch first / already reconciled" note accordingly.
      - Add a small inline script `toggleDebtSettled(checkbox)` that clears and disables the sibling amount input when checked (and re-enables when unchecked), plus a client-side total of entered debts.
      - Non-blocking warning badge when `CurrentOutstandingCredit + entered amount > CreditLimit` (label "Over credit limit", `text-warning`), no server rejection.
    - *Dependencies*: Depends on Steps 11 and 14.

### Phase 7 — SalesRep dashboard debtors

16. **Load and render the debtors card**
    - *Details*:
      - `Manufacture.Web/Pages/Dashboard/Index.cshtml.cs`: add `public List<SalesRepRouteDebtorDto> MyRouteDebtors { get; set; } = new();`. Inside the existing `if (ActiveRole == "SalesRep" && currentUserId.HasValue)` block, after `currentSalesRep` resolves: `MyRouteDebtors = await _dbSalesService.GetSalesRepRouteDebtorsAsync(currentSalesRep.Id, cancellationToken);`.
      - `Manufacture.Web/Pages/Dashboard/Index.cshtml`, inside the `@if (Model.ActiveRole == "SalesRep")` block (after the quick-action buttons): a card headed "Supermarket Debtors by Route" with a total badge `₦@Model.MyRouteDebtors.Sum(d => d.TotalDebt).ToString("N0")`.
      - Group with `@foreach (var routeGroup in Model.MyRouteDebtors.GroupBy(d => d.RouteName))`; inside, per dispatch (`d`): ref, date, status badge; then a compact table of `d.Debtors` (Supermarket, Contact, Dispatch Debt, Account Balance, Recorded By/At).
      - Empty state: "No outstanding supermarket debts on your routes."
      - Rep scoping is enforced server-side by `SalesRepId` from the session, never from a query-string id.
    - *Dependencies*: Depends on Step 12.

### Phase 8 — Tests, docs, verification

17. **Tests**
    - *Details*: Add `Manufacture.Tests/RouteSupermarketDebtCalculatorTests.cs` covering: unchecked with amount adds; checked reverses previously applied; re-save same amount yields `0` delta; re-save lower/higher yields the exact difference; negative amounts clamp to 0.
    - *Dependencies*: Depends on Step 7.

18. **Final validation**
    - *Details*: run the build/test/DB commands below and walk the manual flows before marking the plan Done.
    - *Dependencies*: Depends on all previous steps.

## Relevant Files & Symbols

| File | Change |
|---|---|
| `Manufacture.Web/Pages/Production/Batches/Index.cshtml` | Remove `Unit Cost` / `Notes` `<th>` + `<td>`; trim `data-search` and placeholder |
| `Manufacture.Database/Tables/RouteSheetSupermarkets.sql` | **New** participant link table |
| `Manufacture.Database/Tables/RouteSheetSupermarketDebts.sql` | **New** dispatch debt ledger |
| `Manufacture.Database/Manufacture.Database.sqlproj` | Register both new SQL files |
| `Manufacture.Database/README.md` | Table map + ER diagram |
| `Manufacture.Library/Models/Entities/RouteSheetSupermarket.cs` | **New** entity |
| `Manufacture.Library/Models/Entities/RouteSheetSupermarketDebt.cs` | **New** entity |
| `Manufacture.Library/Models/Entities/DailyRepRouteSheet.cs` | Navigation collections |
| `Manufacture.Library/Models/Entities/SupermarketAccount.cs` | Navigation collections |
| `Manufacture.Library/Data/ManufactureDbContext.cs` | DbSets + `modelBuilder` config (~L103, ~L1678) |
| `Manufacture.Library/Models/Domain/RouteSheetEntities.cs` | `RouteSheetSupermarketLink`, `RouteSheetSupermarketDebt`, extend `DailyRepRouteSheet` |
| `Manufacture.Library/Models/DTOs/SalesDtos.cs` | `SalesRepRouteDebtorDto`, `SalesRepRouteDebtorLineDto` |
| `Manufacture.Library/Calculations/RouteSupermarketDebtCalculator.cs` | **New** pure delta maths |
| `Manufacture.Library/Interfaces/ISalesService.cs` | 1 signature change + 3 new methods (~L77-87) |
| `Manufacture.Web/Services/SalesService.cs` | `CreateRouteSheetAsync` (~L1078), `GetRouteSheetsAsync` (~L1029), `GetRouteSheetAsync` (~L1066), new save/update/debtor methods, `MapRouteSheet` (~L1966) |
| `Manufacture.Web/Pages/Sales/Dispatch/Index.cshtml.cs` | Supermarket list, create/update handlers |
| `Manufacture.Web/Pages/Sales/Dispatch/Index.cshtml` | Multi-select in `#newSheetModal` (~L300-335), participant badges + inline edit |
| `Manufacture.Web/Pages/Sales/Reconciliation/Index.cshtml.cs` | `CanSaveDebts`, debt save handler |
| `Manufacture.Web/Pages/Sales/Reconciliation/Index.cshtml` | Debt register card after the reconcile form (~L471) |
| `Manufacture.Web/Pages/Dashboard/Index.cshtml.cs` | `MyRouteDebtors` loaded for `SalesRep` (~L157-176) |
| `Manufacture.Web/Pages/Dashboard/Index.cshtml` | Debtors card in the SalesRep block (~L400-435) |
| `Manufacture.Tests/RouteSupermarketDebtCalculatorTests.cs` | **New** unit tests |

**Reuse, don't reinvent**
- `SalesService.RecordSupermarketPaymentAsync` for how account + customer balances are kept in sync.
- `SalesService.FulfillSupermarketOrderAsync` for account lookup and credit increment conventions.
- `SalesService.CanManageSheetAsync`-style rep ownership checks in page models.
- Dictionary-style form binding already used by `loadedQuantities[@it.ProductId]` (Dispatch) and `SoldQuantities[@it.ProductId]` (Reconciliation) for `Dictionary<int, decimal>`.
- `LoanRecoveryCalculator` as the pattern for a pure, testable calculation helper.

## Verification Plan

1. **Build & tests**
   - `dotnet build Manufacture.slnx`
   - `dotnet test Manufacture.Tests/Manufacture.Tests.csproj`
   - `dotnet build Manufacture.Database/Manufacture.Database.sqlproj`
2. **Schema deploy**
   - `pwsh scripts/deploy-database.ps1` (or `-Recreate` on a disposable dev DB)
   - Confirm indexes/constraints: unique `(RouteSheetId, SupermarketAccountId)` on both tables, cascade delete from `DailyRepRouteSheets`.
3. **Manual flows**
   - `/Production/Batches`: `Unit Cost` and `Notes` columns gone; `Total Material Cost` KPI, category pills, date/status filters, search and pagination still work.
   - `/Sales/Dispatch` → New Route Sheet → select two supermarkets → Generate. Participant badges appear. Confirm inline edit works only while `Ordered`.
   - Sign off the dispatch → `/Sales/Reconciliation` shows both participants. Enter ₦50,000 for one, tick the other → Save.
   - `/Sales/Supermarkets`: first account's credit increased by exactly ₦50,000; second unchanged. `Customers.OutstandingBalance` mirrors it.
   - Re-save the same panel without changes → balances do not move. Change to ₦70,000 → only +₦20,000 applied. Tick the first → reverses ₦70,000 (clamped at 0). Un-tick with ₦30,000 → +₦30,000.
   - Reconcile the route → the debt panel becomes read-only and the saved values persist.
   - Log in as `sales@bakery.com` → `/Dashboard`: debtors grouped by route, only sheets for that rep. Another rep's debt is not visible. Confirm no unauthenticated/query-string rep override is possible.
   - Delete an `Ordered` route sheet → participant rows removed by cascade (no orphans).
   - Create an auto next-day sheet by reconciling → it inherits the same participants.
4. **Regression checks**
   - `/Sales/PosAccounts/Index` payment flow still reduces the same credit balance.
   - `/Sales/PosAccounts/Supply` still increments credit and creates an order.
   - `RouteSheetAndSalesTests` still pass (the mock service is untouched).

## Key Decisions & Trade-offs

| Topic | Decision | Why |
|---|---|---|
| Where debt lives | `SupermarketAccounts.CurrentOutstandingCredit` (+ customer mirror) | Matches the existing single source of truth, so payments and credit limits continue to work unchanged |
| Two tables vs one | `RouteSheetSupermarkets` (participants) + `RouteSheetSupermarketDebts` (money) | Participants are chosen at dispatch time; debts are only recorded at reconciliation. Keeps the ledger auditable and avoids "amount 0" noise for every participant |
| Re-save safety | Store `AppliedCredit` per row and apply `target - applied` delta | Idempotent; never double-counts; supports un-checking and corrections |
| Write window | `LoadedOnRoute` only | Debt belongs to a dispatched route; `Ordered` has no deliveries yet and `Reconciled` must be immutable |
| Inherit participants | `null` argument = copy previous sheet's participants | Auto-created next-day sheets keep the same supermarket round |
| Over-limit debt | UI warning only | Blocking would stop real deliveries; the operator still needs to record reality |
| No `Order` creation | Debt panel only adjusts credit | Reuses the existing credit concept without duplicating invoices or sales commission |
| Dashboard scoping | Server-side `SalesRepId` from session lookup | Prevents cross-rep data leaks |
| Batches columns | Header + cells only | The user asked for columns, not the cost KPIs or filters |

## Implementation Record

Completed on branch `development`, 2026-11-18.

- Schema published to LocalDB `ManufactureDb` — both new tables created with their unique keys, indexes, checks and cascade FKs.
- `dotnet build Manufacture.slnx` — 0 warnings, 0 errors.
- `dotnet test Manufacture.Tests/Manufacture.Tests.csproj` — 263 passed, 0 failed (8 new calculator tests included).
- HTTP smoke test against the running app:
  - Created a route sheet with two supermarket stops → both `RouteSheetSupermarkets` rows written.
  - Signed off the dispatch → reconciliation panel rendered both stores and the Save button.
  - Saved ₦12,345 debt for store 1 and checked store 2 settled → store 1 credit +12,345, store 2 unchanged.
  - Re-saved the same values → balance unchanged (delta 0); raised to ₦20,000 → only +7,555 applied.
  - Rep dashboard showed "Supermarket Debtors by Route", the dispatch ref, store name and ₦20,000 for the owning rep.
  - Set the sheet to Reconciled → panel rendered read-only with no Save button.
  - `/Production/Batches` rendered without the `Unit Cost` and `Notes` headers while keeping the `Total Material Cost` KPI.
  - Cleanup: test sheets deleted (cascade left 0 orphan stops/debts) and account/customer balances restored.
- Note: `doc/random_prompt.md` was already modified in the working tree before this work and was left untouched.
