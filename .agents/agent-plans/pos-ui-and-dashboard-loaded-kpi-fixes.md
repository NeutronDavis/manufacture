# Plan: POS UI Fixes & Remove Loaded KPI

**Status**: Implemented (2026-11-26)
**Created**: 2026-11-26
**Owner**: Sales/POS and Dashboard

## Summary (TL;DR)

Apply five focused UI/UX fixes:

1. Remove the **LOADED** lifecycle KPI from `/Dashboard` and renumber the remaining lifecycle stages from eight to seven.
2. Fix the `/Sales/Orders/Create` **Add** button. Root cause: the page declares `products` twice (`const products` and `let products`), which throws a JavaScript `SyntaxError` and prevents every script on the page from running.
3. Make the POS terminal selection reload only the products in that terminal's assigned repository/directory.
4. Make the product search input longer than the Quantity input and keep Quantity compact.
5. Make **Payment Method** and **Remarks** full-width so they align with the combined width of the Discount + Amount Paid row.

No database or service-contract changes are required.

## Scope

**In scope**
- Dashboard lifecycle KPI removal and stage-count copy/layout cleanup.
- POS Create page JavaScript fix, terminal-to-directory product filtering, field widths, and Add flow.
- Manual and automated verification of the affected pages.

**Out of scope**
- Removing `LifecycleMetric.Loaded` from the analytics engine, DTOs, reports, or historical data.
- Changing reconciliation/business calculations that still use opening/loaded-style quantities.
- Redesigning the POS basket or settlement logic.
- Any database schema changes.

## Current Findings

| Area | Current state | Change |
|---|---|---|
| Dashboard lifecycle cards | `Index.cshtml.cs` defines 8 `LifecycleStage` items including `Loaded`; `_ExecutiveOverview.cshtml` has a parallel 8-item stage-column tuple array indexed by position | Remove `Loaded` from **both** arrays together; update 8→7 copy and grid columns |
| Dashboard drill-down maps | `Index.cshtml` JS has `metricLabels`, `metricGrouping`, `stageColumns`, and `recordColumns` entries for `Loaded` | Remove the dedicated Loaded KPI entries; keep unrelated Loaded data columns used by other tables |
| POS Add button | `Create.cshtml` declares `const products` at line 192 and `let products` at line 195 | Delete the `const` declaration; keep the mutable `let products` |
| POS terminal filtering | `OnGetProductsAsync(terminalId)` already returns only that terminal's repository products; the client change handler already calls it | Restore functionality by fixing the script error; update the visible directory label and clear the basket on terminal change |
| Search vs Quantity widths | Search is `flex-1`; Quantity is `w-28` | Make search explicitly flexible/min-width-safe; make Quantity compact (`w-20`); keep Add fixed |
| Settlement fields | Payment Method select and Remarks textarea have no `w-full` | Add `w-full` so both span the same width as the two money fields combined |

## Implementation Steps

### Phase 1 — Dashboard: remove the Loaded KPI

1. **Remove Loaded from the lifecycle stage definitions.**
   - *Details*: In `Manufacture.Web/Pages/Dashboard/Index.cshtml.cs`, delete the `LifecycleStage("Loaded", ...)` entry from `Stages`.
   - *Dependencies*: None.

2. **Remove the matching entry from the overview view's parallel stage-column array.**
   - *Details*: In `Manufacture.Web/Pages/Dashboard/_ExecutiveOverview.cshtml`, delete the `("Loaded", t => t.Loaded, ...)` tuple from the local `stageColumns` array. This array is indexed by `Model.Stages` position, so it must stay in lockstep.
   - Remove the `LifecycleMetric.Loaded => totals.Loaded` switch arm in `StageValue` if no longer referenced.
   - Update stage hints for the new seven-stage order:
     - Produced: % of orders placed.
     - Factory Defects: defect rate over produced.
     - Sold: % of produced (replaces the old loaded-based sell-through hint).
     - Closing Stock: % of produced.
     - Damaged: % of produced.
     - Returns: % of sold.
   - Remove now-unused `FillRate` helper; update `SellThrough`/`DamageRate` denominators from `Loaded` to `Produced`.
   - *Dependencies*: Depends on Step 1.

3. **Update the Dashboard copy and layout for seven stages.**
   - *Details*: In `_ExecutiveOverview.cshtml`:
     - “All eight stages” → “All seven stages”.
     - “How to read the eight-stage pipeline” → “seven-stage”.
     - Remove the `<strong>Loaded</strong> — stock physically released to the trucks.` explanation row.
     - “Strict 8-stage…” → “Strict 7-stage…” and remove the loading wording.
     - “8 Stages” badge → “7 Stages”.
     - Lifecycle grid `xl:grid-cols-8` → `xl:grid-cols-7`.
     - Update “8-Stage Sequential Pipeline Cards” comments.
   - *Dependencies*: Parallel with Step 2.

4. **Remove the dedicated Loaded KPI drill-down maps.**
   - *Details*: In `Manufacture.Web/Pages/Dashboard/Index.cshtml` JS:
     - Remove `'Loaded'` from `metricLabels` and `metricGrouping`.
     - Remove the `stageColumns['Loaded']` entry.
     - Remove the `recordColumns['Loaded']` entry.
     - Update the “eight lifecycle KPI cards” comment to seven.
     - Leave unrelated `loaded` data columns used by other drill-down tables unchanged (for example the Damaged-stage context column and rep allocation tables).
   - *Dependencies*: Depends on Step 2.

### Phase 2 — POS Create page fixes

5. **Fix the Add button script failure.**
   - *Details*: In `Manufacture.Web/Pages/Sales/Orders/Create.cshtml`, delete the duplicate `const products = @Html.Raw(productsJson);` declaration and keep `let products = @Html.Raw(productsJson);`.
   - Confirm `rebuildProductIndex()` still runs before the first `render()`.
   - *Dependencies*: None.

6. **Make the selected terminal drive the product directory.**
   - *Details*: In `Create.cshtml`:
     - Keep the existing `terminalSelect` change handler calling `reloadProductsForTerminal()`.
     - Add a stable `data-repository` attribute (repository name/code) to each terminal `<option>`.
     - Add an element id for the “Directory: {name}” label and update it when the terminal changes.
     - Clear the basket when switching terminals so products from the previous directory cannot be submitted.
     - Verify `OnGetProductsAsync(int terminalId)` in `Create.cshtml.cs` returns only products from `terminal.ProductRepositoryId`.
   - *Dependencies*: Depends on Step 5.

7. **Adjust product search, quantity, and Add button widths.**
   - *Details*: In `Create.cshtml`:
     - Product search label/input: `flex-1 min-w-0` so it consumes the remaining row width.
     - Quantity label: compact fixed width (`w-20`), input remains right-aligned and numeric.
     - Add button: `shrink-0` so it keeps its natural size.
   - *Dependencies*: Parallel with Step 6.

8. **Normalize the Order Settlement Summary widths.**
   - *Details*: In `Create.cshtml`:
     - Add `w-full` to the Payment Method `<select>`.
     - Add `w-full` to the Remarks `<textarea>`.
     - Keep the existing two-column Discount / Amount Paid grid unchanged so both new fields span the same combined width as that row.
     - Apply the same full-width treatment to the customer-credit replacement box if it is not already full width.
   - *Dependencies*: Parallel with Step 7.

### Phase 3 — Verification

9. **Build and test.**
   - *Details*: `dotnet build Manufacture.slnx`; `dotnet test Manufacture.Tests/Manufacture.Tests.csproj`.
   - *Dependencies*: Depends on all UI steps.

10. **Manual browser verification.**
    - *Details*:
      - `/Dashboard`: exactly seven lifecycle cards; no Loaded card; Orders Placed / Produced / Factory Defects / Sold / Closing Stock / Damaged / Returns still open their drawers correctly; no JS console errors.
      - `/Sales/Orders/Create`: browser console has no `Identifier 'products' has already been declared` error.
      - Search Product → select a product → Add: a basket row is appended with quantity and bonus; repeated Add increments the row.
      - Change POS Register Terminal: product suggestions, directory label, and basket refresh to the selected terminal's repository only. No product from another repository appears.
      - Search field is visibly longer than Quantity; Quantity stays compact.
      - Payment Method and Remarks span the full settlement card width, matching the combined Discount + Amount Paid row.
    - *Dependencies*: Depends on Step 9.

## Relevant Files & Symbols

- `Manufacture.Web/Pages/Dashboard/Index.cshtml.cs`
  - `Stages` (remove `LifecycleMetric.Loaded` entry).
- `Manufacture.Web/Pages/Dashboard/_ExecutiveOverview.cshtml`
  - Local `stageColumns` tuple array (remove Loaded tuple).
  - `StageValue` switch (remove Loaded arm).
  - `FillRate`, `SellThrough`, `DamageRate` helpers.
  - Pipeline copy, “8 Stages” badge, `xl:grid-cols-8`, eight-stage comments.
- `Manufacture.Web/Pages/Dashboard/Index.cshtml`
  - JS `metricLabels`, `metricGrouping`, `stageColumns`, `recordColumns`.
  - “eight lifecycle KPI cards” comment.
- `Manufacture.Web/Pages/Sales/Orders/Create.cshtml`
  - Duplicate `products` declaration (line ~192/195).
  - Terminal `<option>` markup, directory label, search/quantity/Add row.
  - Payment Method select, Discount/Amount Paid grid, Remarks textarea.
  - JS `rebuildProductIndex`, `renderProductOptions`, `reloadProductsForTerminal`, `addProduct`.
- `Manufacture.Web/Pages/Sales/Orders/Create.cshtml.cs`
  - `OnGetProductsAsync(int terminalId)` — reuse; confirm repository filtering.
  - `LoadProductsForTerminalAsync` — initial directory load.

## Verification Plan

1. **Automated**
   - `dotnet build Manufacture.slnx`
   - `dotnet test Manufacture.Tests/Manufacture.Tests.csproj`
   - If output files are locked, use `-p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\`.

2. **Manual**
   - Dashboard seven-stage lifecycle and drawer behavior.
   - POS Add button and basket rendering.
   - POS terminal change reloads only that terminal's repository directory.
   - Field width alignment in the settlement summary.
   - Browser console shows no JavaScript errors.

## Context & Decisions Log

- **2026-11-26**: User requested removal of the `LOADED` KPI because the app no longer performs dispatch; only orders and reconciliation remain.
- **2026-11-26**: User requested the POS search product field be longer than Quantity, the terminal selection to scope products to its assigned directory, and the Add button to work.
- **2026-11-26**: User requested Payment Method and Remarks to use the full combined width of the Discount + Amount Paid fields for visual uniformity.
- **Root cause found**: `Create.cshtml` declares `products` twice, causing a page-wide JavaScript syntax error; this explains why Add and terminal-driven product reloading both fail.

## Key Decisions & Trade-offs

| Topic | Decision | Why |
|---|---|---|
| Loaded KPI | Remove from Dashboard lifecycle UI and its dedicated drill-down maps only | Matches the request without deleting historical analytics data or breaking reports that still consume it |
| Stage array alignment | Remove Loaded from both `Model.Stages` and `_ExecutiveOverview.stageColumns` in the same change | The view indexes the tuple array by stage position; removing one side alone would mislabel every later KPI |
| Hints after removal | Use Produced/Sold denominators instead of Loaded | Keeps the remaining KPI hints meaningful without a dispatch stage |
| Add button | Fix the duplicate declaration rather than rewriting the basket | Smallest, safest fix for the actual runtime failure |
| Terminal filtering | Keep the existing JSON endpoint and change handler; fix the script and refresh the directory label/basket | Reuses the already-correct repository-filtering service call |
| Quantity width | Compact fixed width (`w-20`) while Search flexes | Implements the requested visual hierarchy and keeps the row responsive |
| Settlement widths | `w-full` on Payment Method and Remarks | Aligns them with the combined Discount + Amount Paid row without changing the two-column money grid |

## Implementation Record (2026-11-26)

- **Build**: `dotnet build Manufacture.slnx` — passed.
- **Tests**: `dotnet test Manufacture.Tests/Manufacture.Tests.csproj` — 263 passed, 0 failed.
- **Dashboard verification**:
  - Lifecycle labels rendered exactly: Orders Placed, Produced, Factory Defects, Sold, Closing Stock, Damaged, Returns.
  - 28 lifecycle stage-card clicks (7 stages × 4 periods).
  - “7 Stages” badge present; “8 Stages” badge absent.
  - Dashboard JavaScript passed `node --check`.
- **POS verification**:
  - Removed the duplicate `products` declaration; the rendered page has no duplicate `const products`.
  - POS page script passed `node --check`.
  - Terminal options expose `data-id` and `data-repository`.
  - Directory label, search flex width, compact Quantity width, and Add button shrink behavior rendered correctly.
  - Payment Method and Remarks both render with `w-full`.
  - Product endpoint returned 10 factory-directory products for POS-01 and 1 mall-directory product for a temporary mall terminal, confirming the terminal-to-repository filtering. The temporary terminal was deleted afterwards.
- **Server**: The verification server was stopped.

