# Plan: Make `Product Line Performance` Cards Read-Only

**Status**: Implemented — verified against the local app
**Created**: 2026-11-16
**Owner**: SuperAdmin dashboard (`/Dashboard`), `_ExecutiveOverview` partial

## Summary (TL;DR)

The product-line cards in the SuperAdmin **Product Line Performance** section currently behave like drill-down controls: the *Units Sold* and *Gross Revenue* tiles are `<button>`s that call `openDrill(...)`, and the card footer carries a **Rep breakdown** button. All three open the side drawer.

This change removes every interaction from that section. The cards and figures become plain, sight-only displays. The eight-stage KPI cards above them keep their existing click-to-records behaviour, and the drawer itself is unchanged.

## User Decision (verbatim)

- Card footer: **"Remove footer entirely (Recommended)"** — delete the footer bar and its caption; the card ends with the SKU badges.
- Follow-up instruction: **"remove the side drawer script that opens when anywhere in the card is clicked so the application will have a clean script note the drawer should not affect any drawer for the KPI, the KPI drawer is perfect"** — remove the product-card drill entry points from the script, keep the KPI drawer exactly as-is.

## Scope

**In scope**
- Remove the `onclick="openDrill(...)"` handlers from the product-card tiles and footer.
- Convert the two clickable tiles from `<button>` to non-interactive `<div>` (no tab stop, no pointer cursor, no hover affordance).
- Remove the card footer bar entirely (`Rep breakdown` button + "Rep-grouped sales & reconciliation" caption).
- Remove hover-lift / hover-border styling from the card and its value tiles so nothing looks clickable.

**Out of scope**
- The eight lifecycle KPI cards and their `openDrill(...)` behaviour.
- The drawer, its tabs, scope/period chips and record fetching (`switchDrillTab`, `render`, `loadDetail`).
- Card content, figures, SKU badges, icons or data sources.
- Any other dashboard section.

## Current Interactive Surfaces (to remove)

| Location | Current | Why it goes |
|---|---|---|
| `_ExecutiveOverview.cshtml` ~L202 | Card has `hover:border-primary/70 hover:shadow-md transition-all duration-200` | Implies a clickable card |
| `_ExecutiveOverview.cshtml` ~L219-225 | `<button ... onclick="openDrill('@line.ScopeKey', '@period.Key', 'Sold', 'reps')">` wrapping **Units Sold**, with `hover:border-success/50` | Opens the drawer |
| `_ExecutiveOverview.cshtml` ~L226-231 | `<button ... onclick="openDrill(...)">` wrapping **Gross Revenue**, with `hover:border-primary/50` | Opens the drawer |
| `_ExecutiveOverview.cshtml` ~L268-276 | Footer bar with `Rep breakdown` button (`openDrill(...)`) | Opens the drawer |

## Implementation Steps

1. **Convert the two highlight tiles to plain containers**
   - *Details*: In `Manufacture.Web/Pages/Dashboard/_ExecutiveOverview.cshtml`, inside the `@foreach (var line in lines)` card markup:
     - Replace the **Units Sold** `<button type="button" onclick="openDrill(...)" class="... hover:border-success/50 transition-colors">…</button>` with a `<div class="text-left p-3 rounded-xl bg-success/10 border border-success/20">…</div>` (same inner labels and `@t.Sold.ToString("N0")`).
     - Replace the **Gross Revenue** `<button …>` with `<div class="text-left p-3 rounded-xl bg-base-200/60 border border-base-300">…</div>` (same inner labels and `@Money(t.ExpectedRevenue)`).
     - No markup inside either tile changes; only the wrapper element type and classes.
   - *Dependencies*: None.

2. **Delete the card footer bar**
   - *Details*: Remove the block:
     ```html
     <!-- Interactive Card Footer CTA -->
     <div class="px-5 py-3 bg-base-200/40 border-t border-base-200 rounded-b-2xl flex items-center justify-between">
         <span class="text-[10px] opacity-60 font-medium">Rep-grouped sales &amp; reconciliation</span>
         <button type="button" onclick="openDrill(...)" class="btn btn-xs btn-primary ...">
             Rep breakdown <i class="fa-solid fa-arrow-right text-[10px]"></i>
         </button>
     </div>
     ```
     The card's closing `</div>` then follows the `card-body` directly. Because the footer no longer exists, keep `card-body` as the last child (the card's `flex flex-col justify-between` is still harmless, but may be simplified — see step 3).
   - *Dependencies*: None.

3. **Strip the interactive card styling**
   - *Details*: Change the card wrapper classes from
     `card bg-base-100 border border-base-300 shadow-xs hover:border-primary/70 hover:shadow-md transition-all duration-200 rounded-2xl flex flex-col justify-between`
     to
     `card bg-base-100 border border-base-300 shadow-xs rounded-2xl`.
     This removes the hover lift, hover border and the flex layout that existed only to pin the footer to the bottom.
   - *Dependencies*: Depends on step 2 (footer removed first).

4. **Update the section caption (optional copy tidy)**
   - *Details*: The section subtitle currently reads `Segmented multi-product yield & reconciliation`. Keep it factual but non-interactive, e.g. `Category-level sales, stock and SKU snapshot`. This is a one-line text change and can be dropped if the user prefers the existing wording.
   - *Status*: **Skipped** — the existing wording does not imply interaction, so it was left untouched to keep the diff minimal.
   - *Dependencies*: None.

5. **Clean the drawer entry point in the page script**
   - *Details*: In `Manufacture.Web/Pages/Dashboard/Index.cshtml`, the only remaining caller of `openDrill` is the lifecycle KPI card. Drop the now-unused `targetView` parameter and the product-card target-view branches:
     - Before: `window.openDrill = function (scopeKey, periodKey, stageMetric, targetView) { … viewMode = targetView || 'records'; if (viewMode === 'stage') viewMode = 'records'; }`
     - After: `window.openDrill = function (scopeKey, periodKey, stageMetric) { … viewMode = 'records'; }`
     - Update the single call site in `_ExecutiveOverview.cshtml` from `openDrill('all', '@period.Key', '@stage.Metric', 'records')` to `openDrill('all', '@period.Key', '@stage.Metric')`.
     - Leave `switchDrillTab`, `render`, `loadDetail`, `productData` and `repData` untouched: the KPI drawer's Product SKUs and Sales Rep Allocations tabs still use them.
   - *Dependencies*: Depends on steps 1-3 (all product-card callers removed first).

6. **Verify no residual drill entry points remain**
   - *Details*: Confirm the rendered page contains **32** `openDrill('all'` calls (8 lifecycle stages × 4 period panels) and **zero** `openDrill('category-` calls (the three product-card handlers all used `line.ScopeKey`). The product-card markup must contain no `onclick`, no `<button>`, no `cursor-pointer`, no `hover:border` and no `openDrill` reference. `switchDrillTab('reps')` inside the drawer tabs must remain.
   - *Dependencies*: Depends on steps 1-5.

## Relevant Files & Symbols

| File | Symbols / changes |
|---|---|
| `Manufacture.Web/Pages/Dashboard/_ExecutiveOverview.cshtml` | Product-line `@foreach (var line in lines)` card: tile wrappers (~L219-231), footer block (~L268-276), card wrapper classes (~L202), section subtitle (~L193) |
| `Manufacture.Web/Pages/Dashboard/Index.cshtml` | `openDrill` signature simplified to `(scopeKey, periodKey, stageMetric)` and hard-sets `viewMode = 'records'`; drawer tab/render logic unchanged |
| `Manufacture.Web/Pages/Dashboard/Index.cshtml.cs` | **Unchanged** |
| `Manufacture.Tests/*` | No test changes expected (no UI test harness for this partial) |

## Verification

1. **Build**
   ```
   dotnet build Manufacture.slnx -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\
   ```
   Expected: 0 warnings, 0 errors.
2. **Tests**
   ```
   dotnet test Manufacture.Tests/Manufacture.Tests.csproj --no-build -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\
   ```
   Expected: all existing tests pass (255 at the time of writing).
3. **Manual (SuperAdmin, `/Dashboard`)**
   - Hover a product-line card: no lift, no border highlight, no pointer cursor.
   - Click **Units Sold**, **Gross Revenue**, the card background and the SKU badges: **nothing opens**.
   - Tab through the page: the product-line tiles are no longer focusable; the eight lifecycle KPI cards still are (they are clickable containers).
   - Click a lifecycle KPI card (e.g. `Sold`): the drawer still opens on the **Records** tab and loads as before.
   - Confirm the product card now ends with the SKU badge row (no footer bar).
4. **Static check**
   ```
   Select-String -Path Manufacture.Web/Pages/Dashboard/*.cshtml -Pattern "openDrill\("
   ```
   Expected: one definition in `Index.cshtml` and one call site in `_ExecutiveOverview.cshtml` (the KPI card). On the rendered page: 32 calls (8 stages × 4 period panels), 0 calls scoped to `category-*`, and 0 `, 'reps')` arguments.
5. **Rendered-page checks (automated, used during execution)**
   - Product-card markup around **Units Sold**: no `onclick`, no `<button>`, no `cursor-pointer`, no `hover:border`, no `openDrill`.
   - Inline dashboard JavaScript passes `node --check`.
   - `GET /Dashboard?handler=Drill&metric=Sold&timeframe=monthly&scope=all` returns `200` with the expected totals, proving the KPI drawer still works.

## Decisions & Non-Goals

- **Read-only means non-focusable.** The two highlight tiles become `<div>`s rather than `<button>`s, so keyboard users no longer land on controls that do nothing.
- **No card-level handler existed.** The product card never had an `onclick` on its wrapper; the drawer opened through the two tile buttons and the footer button (which cover much of the card, hence the "anywhere in the card" feel). Removing those three leaves no product entry point.
- **Footer removed, not re-captioned.** The user chose to delete the footer bar; the card ends with the SKU list.
- **KPI cards stay interactive.** Only the `Product Line Performance` section is affected; the eight-stage pipeline continues to open the records drawer.
- **Script cleanup without touching the KPI drawer.** `openDrill` no longer accepts `targetView` and always opens `records`. `switchDrillTab`, `render`, `loadDetail`, `productData` and `repData` are untouched, so the drawer's Product SKUs and Sales Rep Allocations tabs still work exactly as before.
- **Category scopes remain reachable inside the drawer.** The drawer's scope chips are rendered from `overview.DrillDownScopes`, independent of these cards, so opening the drawer from a KPI card still allows filtering by category.
- **No data or DTO changes**; `ProductLineBreakdownDto.ScopeKey` stays for the drawer scope chips.

## Context & Decisions Log

- **2026-11-16**: User asked to remove the click-to-open-drawer interaction from every category card in `Product Line Performance`; the section should be sight-only.
- **Finding**: three handlers exist there — two tile `<button>`s and one footer CTA — all calling `openDrill(scope, period, 'Sold', 'reps')`. There is no wrapper-level click handler.
- **Decision (user)**: remove the footer bar entirely; no replacement caption.
- **Decision (user)**: also clean the drawer script so no product-card entry point remains, while leaving the KPI drawer untouched.

## Execution Notes (2026-11-16)

- Product card: footer deleted, tiles converted to `<div>`, card hover styles removed.
- `openDrill` reduced to three parameters and always opens the Records view; the single KPI call site updated.
- Build: 0 warnings, 0 errors. Tests: 255 passed, 0 failed.
- Rendered-page verification:
  - `openDrill('all'` → **32** (8 stages × 4 period panels).
  - `openDrill('category-` → **0**.
  - `, 'reps')` → **0**.
  - Product-card markup → no `onclick`, `<button>`, `cursor-pointer`, `hover:border` or `openDrill`.
  - Inline JS → `node --check` **OK**.
  - KPI drawer endpoint → `200`, `Sold` monthly **288** across **2** rep groups.
- The verification server was stopped; the pre-existing development server was left untouched.
