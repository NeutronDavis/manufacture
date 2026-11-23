# Store Manager Access, Requisition Approval, and Stock Movement Ledger Improvements

## Summary (TL;DR)

Fix the broken Store Manager review link, restrict requisition approval actions to **StoreManager** and **SuperAdmin**, prevent StoreManager from accessing suppliers and the entire Production module, and improve the Stock Movement Ledger with full filters, requested-quantity display, and a detail modal.

User-approved decisions:

- Dashboard **Review & Issue** opens `/Inventory/Requests` and focuses/highlights the specific request.
- Only **StoreManager** and **SuperAdmin** can approve/reject requisitions.
- Movement filters include **search, date-from, date-to, movement type, and inventory item**.
- The movement **View** action opens a **detail modal**.

## Current-State Findings

- `Manufacture.Web/Pages/Dashboard/Index.cshtml` links the Store Manager “Review & Issue” button to `/Inventory/Requests/Details`, but no such Razor Page exists. The actual page is `/Inventory/Requests/Index`.
- `/Inventory/Requests` shows **Approve & Issue Stock** and **Reject** to every role and does not guard the POST handlers by role.
- `RequireSessionPageFilter` restricts SalesRep and Customer paths but has no StoreManager restrictions.
- The sidebar uses `Rbac.HasAccess`/`CanEdit`, so StoreManager currently sees the Production menu and the Approved Suppliers link because the seeded StoreManager permissions include View/Edit for Inventory and View Only for Production/Recipes.
- `/Inventory/Movements` already has item and movement-type filters, but no date range or text search.
- `StockMovement` stores only the stock-unit quantity. It does not link directly to the `StoreRequestItem` line that generated an issue, so the ledger cannot show the original requested quantity/unit.
- `StockMovementDto` does not expose requested/base/stock conversion fields, and the Movement History table has no View action.

## Implementation Phases

### Phase 1: Fix the Dashboard review link and focus the target request

1. In `Manufacture.Web/Pages/Dashboard/Index.cshtml`, change the per-row “Review & Issue” link from:
   - `asp-page="/Inventory/Requests/Details" asp-route-id="@req.Id"`
   to:
   - `asp-page="/Inventory/Requests/Index" asp-route-focusId="@req.Id" asp-fragment="req-@req.Id"`.
2. In `Manufacture.Web/Pages/Inventory/Requests/Index.cshtml.cs`:
   - add `[BindProperty(SupportsGet = true)] public int? FocusId { get; set; }`.
3. In `Manufacture.Web/Pages/Inventory/Requests/Index.cshtml`:
   - add `id="req-@req.Id"` to each request card.
   - when `Model.FocusId == req.Id`, add a temporary highlight class (`ring-2 ring-primary`) and scroll the card into view on page load.
   - keep the existing “View All Requests” link pointing to `/Inventory/Requests/Index`.

### Phase 2: Restrict requisition approval actions

1. In `Manufacture.Web/Pages/Inventory/Requests/Index.cshtml.cs`:
   - add `public string UserRole { get; set; }`.
   - add `public bool CanApproveRequests => UserRole is "StoreManager" or "SuperAdmin"` using case-insensitive comparison.
   - set `UserRole` in `OnGetAsync`.
2. Guard both POST handlers:
   - `OnPostApproveAsync`
   - `OnPostRejectAsync`
   - If the session role is not `StoreManager` or `SuperAdmin`, set an error and redirect without changing the request.
3. In `Manufacture.Web/Pages/Inventory/Requests/Index.cshtml`:
   - wrap the Approve/Reject action area in `@if (Model.CanApproveRequests && req.Status == StoreRequestStatus.Pending)`.
   - non-approvers should still see the requisition details and status, but no action buttons.

### Phase 3: Block StoreManager from Suppliers and Production

1. Update `Manufacture.Web/Services/RequireSessionPageFilter.cs`:
   - after the SalesRep branch, add a StoreManager branch.
   - redirect StoreManager away from:
     - `/Inventory/Suppliers` → `/Inventory/Index`
     - `/Production` → `/Dashboard/Index`
     - `/Recipes` (including `/Recipes/Create`, `/Recipes/Edit`, `/Recipes/Details` aliases) → `/Dashboard/Index`
   - use `StartsWithSegments(..., StringComparison.OrdinalIgnoreCase)`.
2. Update `Manufacture.Web/Pages/Shared/_Layout.cshtml`:
   - hide the Production navigation section when `userRole == "StoreManager"`.
   - hide the Approved Suppliers submenu link when `userRole == "StoreManager"`.
   - leave Store Inventory, Store Requisitions, and Stock Ledger visible.
3. In `Manufacture.Web/Pages/Inventory/Items/Details.cshtml.cs`:
   - expose `UserRole`.
   - In `Manufacture.Web/Pages/Inventory/Items/Details.cshtml`, hide the “View Supplier Directory” link for StoreManager (defence-in-depth and better UX).
4. Keep the server-side filter as the authoritative guard; navigation hiding is not a security boundary by itself.

### Phase 4: Add StoreRequestItem linkage to stock movements

1. Database:
   - add nullable `StoreRequestItemId INT NULL` to `Manufacture.Database/Tables/StockMovements.sql`.
   - add FK to `StoreRequestItems` and a filtered index.
2. EF entity:
   - add `StoreRequestItemId` and `StoreRequestItem` navigation to `Manufacture.Library/Models/Entities/StockMovement.cs`.
   - configure the relationship in `Manufacture.Library/Data/ManufactureDbContext.cs`.
3. In `Manufacture.Web/Services/InventoryService.cs`, when creating the Issue movement in `ApproveAndIssueRequestAsync`, set:
   - `StoreRequestItemId = line.Id`.
4. Backfill existing Issue movements in `scripts/seed-database.sql` or a deployment-safe SQL update:
   - match `StockMovements.StoreRequestId` + `InventoryItemId` to the corresponding `StoreRequestItems` row.
   - if multiple lines match, leave null; the mapping fallback will still work.
5. Extend `StockMovementDto` with:
   - `int? StoreRequestItemId`
   - `decimal? RequestedQuantity`
   - `string? RequestedUnit`
   - `decimal? BaseQuantity`
   - `string? BaseUnit`
   - `decimal? StockQuantity`
   - `string? StockUnit`
   - `string? RequestNumber`
6. Update `MapStockMovement` to populate these fields from the linked `StoreRequestItem` and `StoreRequest`.

### Phase 5: Movement filters

1. In `Manufacture.Web/Pages/Inventory/Movements/Index.cshtml.cs`, add bindable GET properties:
   - `DateOnly? DateFrom`
   - `DateOnly? DateTo`
   - `string? Search`
2. Extend `IInventoryService.GetStockMovementsAsync` with optional parameters:
   - `DateOnly? fromDate = null`
   - `DateOnly? toDate = null`
   - `string? search = null`
3. Apply server-side filters in `InventoryService`:
   - date range on `CreatedAt` (use UTC day boundaries).
   - text search against item name, item code, reference number, performer, and notes.
   - retain item and movement-type filters.
4. Update the filter form in `Manufacture.Web/Pages/Inventory/Movements/Index.cshtml`:
   - add Search input.
   - add Date From and Date To controls.
   - keep Inventory Item and Movement Type.
   - keep Apply Filter and Clear.
   - preserve selected values after postback.
5. Ensure the receipt/issue/net KPI cards reflect the filtered movement list.

### Phase 6: Quantity column and View modal

1. Movement History table:
   - for Receipts, keep `+Quantity Unit`.
   - for Issues with a linked request line, show:
     - primary: requested quantity/unit (e.g. `5 litres`).
     - secondary: `Issued 0.072 Bags (50kg)` and/or `3.6 kg`.
   - for Issues without a linked request line, fall back to the existing movement quantity/unit.
2. Add a **View** action column at the end.
3. Add a `<dialog id="movementDetailModal">` to the Movements page.
4. Each View button carries `data-*` attributes for:
   - item name/code
   - movement type/date
   - requested quantity/unit
   - base quantity/unit
   - stock quantity/unit
   - balance after
   - reference number
   - performed by
   - notes
   - linked request number
5. The modal displays those details in a descriptive layout and includes a close action.
6. Optionally include links from the modal to the inventory item and the linked requisition.

## Relevant Files

### Dashboard & Requests
- `Manufacture.Web/Pages/Dashboard/Index.cshtml`
- `Manufacture.Web/Pages/Inventory/Requests/Index.cshtml`
- `Manufacture.Web/Pages/Inventory/Requests/Index.cshtml.cs`

### Access control
- `Manufacture.Web/Services/RequireSessionPageFilter.cs`
- `Manufacture.Web/Pages/Shared/_Layout.cshtml`
- `Manufacture.Web/Pages/Inventory/Items/Details.cshtml`
- `Manufacture.Web/Pages/Inventory/Items/Details.cshtml.cs`

### Movement ledger
- `Manufacture.Web/Pages/Inventory/Movements/Index.cshtml`
- `Manufacture.Web/Pages/Inventory/Movements/Index.cshtml.cs`
- `Manufacture.Web/Services/InventoryService.cs`
- `Manufacture.Library/Interfaces/IInventoryService.cs`
- `Manufacture.Library/Models/DTOs/InventoryDtos.cs`
- `Manufacture.Library/Models/Entities/StockMovement.cs`
- `Manufacture.Library/Data/ManufactureDbContext.cs`
- `Manufacture.Database/Tables/StockMovements.sql`
- `scripts/seed-database.sql`

## Verification

### Automated
```powershell
dotnet build Manufacture.slnx -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\
dotnet test Manufacture.Tests/Manufacture.Tests.csproj --no-build -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\
```

### Manual: StoreManager
1. Sign in as `store@bakery.com` / `Password123!`.
2. Dashboard → “Review & Issue” opens `/Inventory/Requests` and highlights the correct request.
3. `/Inventory/Requests` shows Approve & Issue and Reject only for pending requests.
4. Direct POST attempts by another role are rejected.
5. `/Inventory/Suppliers` redirects to `/Inventory/Index`.
6. `/Production`, `/Recipes`, and recipe create/edit/details aliases redirect to the dashboard.
7. Production and Approved Suppliers are absent from the sidebar.
8. Inventory, Requisitions, Stock Ledger, and item details remain accessible.

### Manual: SuperAdmin
1. Sign in as `admin@bakery.com`.
2. Approve/Reject controls are visible and functional.
3. Suppliers and Production remain accessible.

### Manual: Movement ledger
1. Open `/Inventory/Movements`.
2. Filter by search text, date range, item, and movement type.
3. Confirm the table and KPI cards reflect the filters.
4. Open an Issue row: Quantity shows the requested quantity/unit, with the issued stock conversion beneath.
5. Click View: the modal shows requested, base, and stock quantities, balance, reference, performer, notes, and linked request.
6. Confirm Receipt rows still show the received quantity correctly.

## Decisions / Non-Goals

- StoreManager approval is limited to **StoreManager** and **SuperAdmin**, as chosen.
- Dashboard review goes to the requisition list with a focused highlight; no new requisition details page is created.
- Movement filters are full filters: search, date-from, date-to, movement type, and item.
- Movement View is a modal, not a new page.
- No changes to partial issue workflow or production functionality beyond access restriction.
