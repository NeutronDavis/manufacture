# Plan: Make SuperAdmin Product Line Cards Fully Category-Driven

**Status**: Done

## Summary

The SuperAdmin dashboard product-line cards are already populated from `ProductionCategories`, not from a literal Bread/Snacks/Water list. The three cards currently shown are the three seeded rows in the database.

However, the dashboard still carries a legacy `ProductType` enum (Bread/Water/Popcorn) and a few name-based presentation fallbacks. This plan removes those from the dashboard decision path so a new category created at `/Settings/Production` appears and behaves like any existing category, including before it has products.

No database schema change is required.

## What Already Works

- `SuperAdminDashboardService.BuildCategories()` reads active `ProductionCategories` from the database.
- `SuperAdminDashboardAggregator.BuildProductLines()` builds one card per category.
- Category cards use `ProductionCategoryName` and `ScopeKey` (`category-{id}`).
- The drawer has a separate product/rep scope for every category.
- The hardcoded `LineSkus` switch was already removed; cards now use actual `Recipes` names from the database.
- There is no dashboard cache: a newly saved active category appears on the next dashboard load.

Current seeded categories:

| Id | Name |
|---|---|
| 1 | Bread |
| 2 | Water |
| 3 | Snacks |

## Current Residual Hardcoding

1. `DashboardCategory.Line` and `DashboardSku.Line` still use the three-value `ProductType` enum.
2. `MapLine()` maps any unknown category to `ProductType.Bread`.
3. The dashboard product-card view still reads `LineLabel` instead of the explicit `ProductionCategoryName`.
4. The dashboard drawer JavaScript chooses badge colour by comparing `lineLabel` to `"Bread"` and `"Water"`.
5. Icon selection uses category-name heuristics (`water`, `snack`, `popcorn`, `bread`) with a `fa-box` fallback. This is presentation only but still looks like hardcoding.
6. A new category with no active product type or recipe currently appears as a zero-value card. That is safe, but the expected behaviour should be documented.

These do not prevent a new category from appearing, but they leave avoidable legacy coupling.

## Scope

- **In-Scope**
  - Make `ProductionCategoryId`/`ProductionCategoryName` the sole identity for dashboard cards, scopes, grouping and drawer labels.
  - Remove enum-based and name-based decisions from the dashboard path.
  - Ensure new categories appear immediately whether or not they have products.
  - Add regression tests for a new category.
  - Verify the Settings page remains the single place to add categories.
- **Out-of-Scope**
  - Renaming or removing the `ProductType` enum globally.
  - Migrating `/Reports` off its current Bread/Water/Popcorn model.
  - Adding custom per-category icons or colours through a new settings UI.
  - Changing the seeded Bread/Water/Snacks records.
  - Database schema changes.

## Decisions

- **Category identity**: use `ProductionCategoryId` and `ScopeKey = category-{id}`. The `ProductType` enum is legacy compatibility only.
- **New category with no products**: show an empty card with zero values and a neutral `fa-box` icon. Once an active product type and recipe exist, the card and drawer populate automatically.
- **Inactive category**: hide from the card list unless the category has historical facts for the selected period. Existing history should remain visible.
- **Icons**: keep the current presentation mapping for known names as a convenience, but never use it to decide whether a category exists. Unknown categories fall back to `fa-box`.
- **Badge colour**: use a neutral category badge instead of matching the literal labels Bread/Water.

## Implementation Steps

1. **Make the dashboard category DTOs category-first**
   - *Details*: In `Manufacture.Library/Models/DTOs/AnalyticsDtos.cs`, keep the existing `Line`/`LineLabel` fields for `/Reports` compatibility, but ensure the dashboard uses:
     - `ProductLineBreakdownDto.ProductionCategoryId`
     - `ProductLineBreakdownDto.ProductionCategoryName`
     - `ProductLineBreakdownDto.ScopeKey`
     - `ProductDrillDownRowDto.ProductionCategoryId`
     - `ProductDrillDownRowDto.ProductionCategoryName`
   - Add a comment that `Line` is a legacy display fallback, not the dashboard identity.
   - *Dependencies*: None.

2. **Remove enum mapping from the dashboard service**
   - *Details*: In `Manufacture.Web/Services/SuperAdminDashboardService.cs`:
     - Stop using `MapLine()` to decide category membership, scope, or card identity.
     - Set `DashboardCategory.Line` to a best-effort legacy value only if required by the old DTO; the dashboard must not read it.
     - Keep `DashboardCategory.IconClass` name-based for presentation only, with `fa-box` fallback.
   - In `Manufacture.Web/Services/SuperAdminDashboardAggregator.cs`:
     - Build product rows with `ProductionCategoryId`, `ProductionCategoryName`, and `LineLabel = ProductionCategoryName`.
     - Do not use `MapLine()` in `BuildProductRows` for dashboard labels.
   - *Dependencies*: Depends on Step 1.

3. **Update the product-line Razor view**
   - *Details*: In `Manufacture.Web/Pages/Dashboard/_ExecutiveOverview.cshtml`:
     - Replace `@line.LineLabel` with `@line.ProductionCategoryName`.
     - Use `@line.ScopeKey` for all `openDrill(...)` calls.
     - Keep `@line.IconClass` and fall back to `fa-box` if empty.
     - Keep the card grid responsive so a fourth or fifth category wraps cleanly.
     - Show a clear empty-state inside a category card when no active SKU exists.
   - *Dependencies*: Depends on Step 1.

4. **Remove name-based badge logic from the drawer JavaScript**
   - *Details*: In `Manufacture.Web/Pages/Dashboard/Index.cshtml`:
     - Replace the `lineBadgeTone` conditional based on `item.lineLabel === 'Bread' / 'Water'` with a neutral category badge or a category-index palette.
     - Continue using `item.productionCategoryName` for the label.
     - Do not use category names to decide default views, grouping, or scope keys.
   - *Dependencies*: Depends on Step 2.

5. **Make scope generation explicit for every category**
   - *Details*: In `SuperAdminDashboardAggregator.Build`:
     - Build rep, product, and stage scopes for every category from `ProductionCategories`.
     - Preserve zero-value scopes for a new category so the drawer opens correctly before the first sale.
     - Ensure `ScopeKey` is stable and unique (`category-{id}`).
   - *Dependencies*: Depends on Step 2.

6. **Add regression tests**
   - *Details*: Extend `Manufacture.Tests/SuperAdminDashboardAggregatorTests.cs`:
     - Add a `Drinks` category with no facts; verify it appears as a zero-value card.
     - Add a `Drinks` fact; verify its card, product line scope, rep scope and stage rows populate.
     - Verify that a new category is not assigned Bread data and does not inherit Bread-only labels.
     - Verify `ScopeKey` is `category-{id}` and unique.
   - *Dependencies*: Depends on Step 2.

7. **Verify the Settings flow**
   - *Details*: Use `/Settings/Production` to add an active category, then add a product type and recipe under it.
   - Confirm the dashboard card appears immediately after refresh.
   - Confirm the card starts at zero and populates after a route sheet, production batch, or customer return is recorded.
   - Confirm deactivating a category hides it when it has no current facts but does not delete history.
   - *Dependencies*: Depends on Steps 2-6.

## Relevant Files & Symbols

- `Manufacture.Web/Services/SuperAdminDashboardService.cs`: `BuildCategories`, `BuildSkus`, `IconFor`, `MapLine`.
- `Manufacture.Web/Services/SuperAdminDashboardAggregator.cs`: `BuildProductLines`, `BuildProductRows`, category scope creation.
- `Manufacture.Library/Models/DTOs/AnalyticsDtos.cs`: `ProductLineBreakdownDto`, `ProductDrillDownRowDto`, `ProductSkuDto`.
- `Manufacture.Web/Pages/Dashboard/_ExecutiveOverview.cshtml`: product-line card labels, icons, scope calls.
- `Manufacture.Web/Pages/Dashboard/Index.cshtml`: drawer category badge and product-line rendering.
- `Manufacture.Web/Pages/Settings/Production.cshtml.cs`: existing category create/update/toggle flow.
- `Manufacture.Web/Pages/Settings/Production.cshtml`: category and product-type management UI.
- `Manufacture.Tests/SuperAdminDashboardAggregatorTests.cs`: new-category regression coverage.

## Verification Plan

1. **Automated**
   - Build:
     `dotnet build Manufacture.slnx -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\`
   - Tests:
     `dotnet test Manufacture.Tests/Manufacture.Tests.csproj --no-build -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\`
   - Expected: 0 warnings, 0 errors; existing tests plus new category tests pass.

2. **Manual**
   - Sign in as `admin@bakery.com` / `Password123!`.
   - Go to `/Settings/Production` and add a category, e.g. `Drinks`.
   - Refresh `/Dashboard`.
   - Confirm a **Drinks** product-line card appears, even with zero values.
   - Confirm the card uses `category-{id}` in the drawer scope, not a Bread fallback.
   - Add a product type and recipe under Drinks.
   - Add a route-sheet row or production batch for a Drinks product.
   - Refresh the dashboard and confirm the card and drawer populate from the database.
   - Deactivate the category and confirm the intended hidden/historical behaviour.

## Context & Decisions Log

- **2026-11-14**: User observed that `Product Line Performance • Today` shows Bread, Snacks and Water and asked what happens if a client adds another category at `/Settings/Production`.
- **Current code finding**: the three cards are database rows from `ProductionCategories`, not a hardcoded literal list. The remaining hardcoding is the legacy `ProductType` enum and name-based icon/color fallbacks.
- **Decision**: new active categories must appear with zero values before products are attached, then populate when their product types, recipes and operational data are created.
- **Decision**: no schema change is required for this fix.

## Key Decisions & Trade-offs

- **Category id over enum**: `ProductionCategoryId` is stable and supports unlimited categories. The enum remains only for legacy `/Reports` compatibility.
- **Neutral fallback**: new categories get a neutral box icon and badge; custom icons are deferred.
- **Empty categories visible**: showing zero-value cards makes newly configured categories immediately discoverable. If the business prefers hiding categories until their first product exists, that can be changed with a single filter in `BuildCategories`.
- **No dashboard cache**: database changes appear on refresh; this is intentional so Settings changes are immediate.
