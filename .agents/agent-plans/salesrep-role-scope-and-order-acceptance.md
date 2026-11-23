# Plan: SalesRep Role Scope + Order Acceptance

**Status**: Implemented — verified against the local app
**Created**: 2026-11-17
**Owner**: SalesRep experience (`/Sales`, `/Sales/Orders/Details`, role guards, navigation)

## Summary (TL;DR)

Three changes to how a SalesRep experiences the app:

1. **Production is removed entirely** for SalesRep — no Production nav, no `/Production/*` or `/Recipes/*` access.
2. **The `/Sales` hub is removed** for SalesRep — direct visits redirect to `/Sales/Orders`, and the "Sales Hub" nav item is hidden. The `Dedicated Checkout Registers` section therefore never renders for them.
3. **Sales Rep Assignment becomes actionable**:
   - SalesRep sees an **Accept order** button on an open, unassigned order and self-assigns.
   - SuperAdmin keeps the dropdown, but only for an open, unassigned order.
   - Once assigned (or once the order is closed), the card shows **only the assigned rep's name** — no dropdown, no button.

## User Requirements (verbatim)

- "Take off Production enirly out for sales rep user"
- "At the /Sales page, the sales rep should not see the `Dedicated Checkout Registers` section"
- "At the /Sales/Orders/Details page, the `Sales Rep Assignment` section should have a botton saying `Accept order`, when the sales rep clik on it will assign the sign in sales rep to that order, Note: ths modification is only for sales rep it will remain a dropdown for superadmin only for order that is Open and not yet have a sales rep to assigned to it."
- "when a sales rep is asign to an order, the dropdown should change to just the assigned sales rep name"

## Decisions (verbatim answers)

- **"Open" order rule**: **"Any undelivered order (Recommended)"** — Pending, Confirmed, InProduction and ReadyToLoad are open; Dispatched and Cancelled are closed.
- **`/Sales` scope**: **"You are making a point here, ill sugest that the Salesrep User should not see the /Sales page"** — the whole hub is removed for SalesRep, not just the register section.

## Scope

**In scope**
- Hide Production navigation for SalesRep and block `/Production/*` + `/Recipes/*` routes.
- Redirect SalesRep away from `/Sales` (to `/Sales/Orders`) and hide the "Sales Hub" nav item.
- Add `Accept order` self-assignment on order details for SalesRep.
- Restrict the SuperAdmin assignment dropdown to open, unassigned orders.
- Show a read-only assigned rep name whenever assign/accept is not possible.
- Enforce the same rules server-side in the page handlers and the sales service.

**Out of scope**
- Changing the order lifecycle, payment flow, status transitions or dispatch behaviour.
- Changing the SalesRep order list filter (already shows own + unassigned orders).
- Removing SalesRep access to Orders, Point of Sale, Dispatch, Reconciliation, Supermarket Accounts, Customers or Reports.
- Hiding the `Dedicated Checkout Registers` markup itself (the page redirect makes it unreachable for SalesRep).
- Database schema changes, migrations, or editing live RBAC rows.
- Reassigning an order after it has a rep (explicitly disallowed by the requirement).

## Current State Findings

| Area | Today | Change |
|---|---|---|
| `RbacDefaults` | SalesRep: `Production = None`, `Recipes = View Only` | `Recipes = None` so a defaults reset cannot re-grant it |
| `_Layout` Production menu | Shows when `userRole != StoreManager && (Production \|\| Recipes)` → SalesRep sees it via Recipes | Explicitly exclude SalesRep |
| `RequireSessionPageFilter` | SalesRep only blocked from `/Sales/Terminals` | Add `/Production`, `/Recipes`; retarget Terminal redirect |
| `/Sales` page | Renders for everyone; section already hides Manage/Assign buttons for SalesRep | Redirect SalesRep to `/Sales/Orders` |
| Details assignment card | Dropdown + Save for every `CanManageSales` role, including SalesRep | Accept button (SalesRep) / dropdown (SuperAdmin, open + unassigned) / name only |
| `AssignOrderSalesRepAsync` | Assigns any rep, any order state, including reassignment | Reject closed orders, already-assigned orders, and missing rep |

## Implementation Steps

### Phase A — Remove Production from SalesRep

1. **Hide the Production nav**
   - *Details*: In `Manufacture.Web/Pages/Shared/_Layout.cshtml` (~L200) change the Production block condition to
     `userRole != "StoreManager" && userRole != "SalesRep" && (Rbac.HasAccess(userRole, "Production") || Rbac.HasAccess(userRole, "Recipes"))`.
     No other nav changes; SuperAdmin and ProductionManager keep the menu.
   - *Dependencies*: None.

2. **Block Production routes for SalesRep**
   - *Details*: In `Manufacture.Web/Services/RequireSessionPageFilter.cs`, inside the `SalesRep` branch, add:
     ```csharp
     if (path.StartsWithSegments("/Production", StringComparison.OrdinalIgnoreCase)
         || path.StartsWithSegments("/Recipes", StringComparison.OrdinalIgnoreCase))
     {
         context.Result = new RedirectToPageResult("/Dashboard/Index");
         return;
     }
     ```
     This covers `/Production/Index`, batches, recipes, pricing and the `/Recipes` aliases registered in `Program.cs`. Change the existing `/Sales/Terminals` redirect from `/Sales/Index` to `/Sales/Orders/Index` (step 5 removes the hub).
   - *Dependencies*: Parallel with step 1.

3. **Reflect the policy in RBAC defaults**
   - *Details*: In `Manufacture.Web/Services/RbacDefaults.cs` (~L50) change `("Recipes", "SalesRep", "View Only")` to `("Recipes", "SalesRep", "None")`. This only affects fresh seeds and "Reset to defaults"; the route guard in step 2 is authoritative for the current database.
   - *Dependencies*: Parallel with steps 1-2.

### Phase B — Remove the `/Sales` hub from SalesRep

4. **Redirect the hub**
   - *Details*: In `Manufacture.Web/Pages/Sales/Index.cshtml.cs`, change `OnGetAsync` to `Task<IActionResult>` and add at the top:
     ```csharp
     UserRole = HttpContext.Session.GetString("UserRole") ?? string.Empty;
     if (string.Equals(UserRole, "SalesRep", StringComparison.OrdinalIgnoreCase))
     {
         return RedirectToPage("/Sales/Orders/Index");
     }
     ```
     The rest of the page (stats, registers, recent orders) remains unchanged for every other role. Existing breadcrumb links to `/Sales/Index` therefore land on Orders for SalesRep instead of a dead end.
   - *Dependencies*: None.

5. **Hide the Sales Hub nav item**
   - *Details*: In `_Layout.cshtml` (~L286) change the condition to
     `Rbac.HasAccess(userRole, "Sales") && userRole != "Customer" && userRole != "SalesRep"`.
     The Sales & POS menu itself stays (Orders, Point of Sale, Dispatch, Reconciliation, Supermarket Accounts, Customers).
   - *Dependencies*: Parallel with step 4.

### Phase C — Order acceptance and assignment

6. **Enforce assignment rules in the service**
   - *Details*: In `Manufacture.Web/Services/SalesService.cs`, `AssignOrderSalesRepAsync`:
     - Return `false` when the order is `Dispatched` or `Cancelled`.
     - Return `false` when `order.SalesRepId` already has a value (no reassignment).
     - Require `salesRepId` to be a positive id and resolve an active rep (existing validation).
     - Keep the existing `InvalidOperationException` for an invalid/inactive rep.
     The contract stays `Task<bool> AssignOrderSalesRepAsync(int orderId, int? salesRepId, CancellationToken)`; no interface change.
   - *Dependencies*: None.

7. **Add role/state properties and the Accept handler**
   - *Details*: In `Manufacture.Web/Pages/Sales/Orders/Details.cshtml.cs`:
     - Properties:
       ```csharp
       public bool IsSalesRep => string.Equals(UserRole, "SalesRep", ...);
       public bool IsSuperAdmin => string.Equals(UserRole, "SuperAdmin", ...);
       public bool IsOrderOpen => Order.Status is not (OrderStatus.Dispatched or OrderStatus.Cancelled);
       public bool CanAcceptOrder => IsSalesRep && IsOrderOpen && Order.SalesRepId is null;
       public bool CanAssignRep => IsSuperAdmin && IsOrderOpen && Order.SalesRepId is null;
       ```
     - New handler `OnPostAcceptOrderAsync(int id, CancellationToken)`:
       - Reject non-SalesRep callers.
       - Load the order; reject missing, closed, or already-assigned orders with a clear TempData error.
       - Resolve the signed-in rep via `GetSalesRepByUserIdAsync(UserId)`; if null → "Your sales rep login is not linked to an active sales representative profile."
       - Call `AssignOrderSalesRepAsync(id, rep.Id)`; success message `"Order <number> accepted and assigned to you."`
       - Redirect to the same Details page.
     - Harden `OnPostAssignRepAsync`:
       - Reject when `!IsSuperAdmin`.
       - Load the order; reject closed or already-assigned orders.
       - Require `selectedSalesRepId > 0`.
       - Keep the existing success/error/message flow.
   - *Dependencies*: Depends on step 6.

8. **Render the three assignment states**
   - *Details*: In `Manufacture.Web/Pages/Sales/Orders/Details.cshtml` (~L132-149), keep the card and title, then:
     - `@if (Model.CanAcceptOrder)` → explanatory line + `<form method="post" asp-page-handler="AcceptOrder" asp-route-id="@order.Id">` with a primary `Accept order` button.
     - `@else if (Model.CanAssignRep)` → the existing dropdown form, without the `-- Unassigned --` option, with `required` and `Save Assignment`.
     - `@else` → read-only panel:
       ```html
       <div class="p-3 rounded-xl bg-base-200 border border-base-300 text-xs">
           <div class="text-[10px] uppercase font-bold opacity-60">Assigned Sales Rep</div>
           <div class="font-bold text-base-content">@(string.IsNullOrWhiteSpace(order.SalesRepName) ? "Unassigned" : order.SalesRepName)</div>
       </div>
       ```
       plus a small note when the order is closed and still unassigned ("Order is closed; no further assignment is possible.").
     - Keep the whole card inside `@if (Model.CanManageSales)` so ProductionManager behaviour is unchanged.
   - *Dependencies*: Depends on steps 7.

## Relevant Files & Symbols

| File | Symbols / changes |
|---|---|
| `Manufacture.Web/Services/RequireSessionPageFilter.cs` | SalesRep branch: block `/Production` + `/Recipes`; retarget `/Sales/Terminals` |
| `Manufacture.Web/Pages/Shared/_Layout.cshtml` | Production menu condition (~L200), Sales Hub item (~L286) |
| `Manufacture.Web/Services/RbacDefaults.cs` | `("Recipes", "SalesRep", ...)` → `"None"` |
| `Manufacture.Web/Pages/Sales/Index.cshtml.cs` | `OnGetAsync` → `Task<IActionResult>`, early SalesRep redirect |
| `Manufacture.Web/Pages/Sales/Orders/Details.cshtml.cs` | `IsSalesRep`, `IsSuperAdmin`, `IsOrderOpen`, `CanAcceptOrder`, `CanAssignRep`, `OnPostAcceptOrderAsync`, hardened `OnPostAssignRepAsync` |
| `Manufacture.Web/Pages/Sales/Orders/Details.cshtml` | Assignment card markup: Accept / dropdown / read-only name |
| `Manufacture.Web/Services/SalesService.cs` | `AssignOrderSalesRepAsync` guard for closed/already-assigned orders |
| `Manufacture.Web/Pages/Shared/_Layout.cshtml` | No changes to the Sales & POS submenu items other than Sales Hub |

**Reuse**
- `ISalesService.GetSalesRepByUserIdAsync` for the signed-in rep.
- `ISalesService.GetOrderAsync` / `Order.SalesRepId` / `Order.SalesRepName` for state.
- Existing TempData success/error pattern and `_Layout` global alerts.
- Existing `CanManageSales` gate for ProductionManager.

## Verification

1. **Build**
   ```
   dotnet build Manufacture.slnx -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\
   ```
2. **Tests**
   ```
   dotnet test Manufacture.Tests/Manufacture.Tests.csproj --no-build -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\
   ```
   Expect 0 warnings/errors and the existing suite green (no unit-testable seams added).
3. **Role matrix (HTTP, authenticated sessions)**
   | Request | SalesRep | SuperAdmin |
   |---|---|---|
   | `GET /Sales` | `302` → `/Sales/Orders` | `200` |
   | `GET /Sales/Orders` | `200` | `200` |
   | `GET /Production/Batches` | `302` → `/Dashboard` | `200` |
   | `GET /Production/Recipes` | `302` → `/Dashboard` | `200` |
   | `GET /Recipes` (alias) | `302` → `/Dashboard` | `200` |
   | Nav HTML | no "Production", no "Sales Hub" | both present |
4. **Order assignment matrix**
   | Order state | SalesRep | SuperAdmin |
   |---|---|---|
   | Open + unassigned | `Accept order` button | dropdown + `Save Assignment` |
   | Open + assigned | assigned rep name only | assigned rep name only |
   | Dispatched/Cancelled, unassigned | "Unassigned" + closed note | same |
   | Dispatched/Cancelled, assigned | assigned rep name | assigned rep name |
5. **Accept flow**
   - As SalesRep: open an open, unassigned order, click **Accept order** → success alert, page reloads with the rep name, button gone, no `<select>`.
   - Re-post the handler → rejected with an error; `SalesRepId` unchanged.
   - Confirm the order now appears in that rep's Orders list/dashboard counts.
6. **SuperAdmin flow**
   - Open an open, unassigned order → dropdown lists active reps (no "-- Unassigned --").
   - Save → page reloads with only the rep name; the dropdown is gone.
   - Attempt to post `AssignRep` again → rejected.
7. **Data checks**
   ```
   SELECT TOP 20 o.Id, o.OrderNumber, o.Status, o.SalesRepId, o.SalesRepName
   FROM dbo.Orders o
   WHERE o.Status NOT IN (4,5)
   ORDER BY o.Id DESC;
   ```
   Before/after the accept and admin-assign actions, `SalesRepId`/`SalesRepName` must change only once.

## Decisions & Non-Goals

- **"Open" is lifecycle-based**, not payment-based: anything not `Dispatched`/`Cancelled`.
- **Self-accept is one-way.** A rep cannot unassign or take over an order that already has a rep; the service rejects it.
- **SuperAdmin cannot reassign** an order that already has a rep, matching the "dropdown only when unassigned" requirement. If reassignment is ever needed, it must be a separate, explicit feature.
- **No status transition on accept.** Accepting only writes the rep; the order keeps its current operational status.
- **The Sales hub is redirected, not deleted.** Other roles still use it, and SalesRep breadcrumbs resolve to `/Sales/Orders`.
- **`Dedicated Checkout Registers` markup is untouched**; it is unreachable for SalesRep because the page redirects before rendering.
- **RBAC data is not rewritten.** `RbacDefaults` changes only affect resets/fresh seeds; the route guard enforces the rule on the existing database.

## Risks / Watch-outs

- **Breadcrumb hops**: Sales subpages link to `/Sales/Index`; for SalesRep these now land on `/Sales/Orders` (intended). No 404s.
- **Redirect loop**: never redirect SalesRep to `/Sales/Index` from the filter; use `/Dashboard/Index` or `/Sales/Orders/Index`.
- **Stale dropdown**: after a successful assign/accept the page must reload (POST-Redirect-GET) so the read-only name renders.
- **Concurrent accepts**: two reps could accept the same order within milliseconds; the service re-checks state on save, so the later request fails. This is acceptable for the current single-instance deployment.
- **SalesRep without a linked profile**: Accept fails with the existing "not linked to an active sales representative profile" message; verify a `SalesReps.UserId` row exists for the test login.

## Context & Decisions Log

- **2026-11-17**: User requested Production removal, `/Sales` hub removal, and Accept-order self-assignment with SuperAdmin-only dropdown for open, unassigned orders.
- **Finding**: SalesRep currently sees Production only because `Recipes` is `View Only`; the layout and route filter have no SalesRep block for Production.
- **Finding**: the Details assignment form is rendered for every `CanManageSales` role, including SalesRep, and `AssignOrderSalesRepAsync` has no state checks.
- **Decision (user)**: open = any undelivered order.
- **Decision (user)**: SalesRep must not see `/Sales` at all; the hub redirects to Orders and the nav item is hidden.

## Execution Notes (2026-11-17)

- Implemented exactly as planned; no database schema changes.
- **Role matrix verified** with the seeded `sales@bakery.com` and `admin@bakery.com` sessions:
  - SalesRep: `/Sales`, `/Sales/Index` → `302 /Sales/Orders`; `/Production`, `/Production/Pricing`, `/Production/Batches`, `/Production/Batches/Create`, `/Production/Recipes`, `/Recipes` → `302 /Dashboard`; `/Sales/Orders` → `200`.
  - SuperAdmin: all of the above return `200`.
  - SalesRep dashboard nav contains no `href="/Production..."`, no `Sales Hub`, no `/Sales/Terminals`. SuperAdmin keeps all three.
- **Accept flow verified** on the seeded open, unassigned order `ORD-20261111-002` (id 14):
  - SalesRep sees `Accept order`, no `<select name="selectedSalesRepId">`.
  - POST assigns `SalesRepId = 1` (`Babatunde Alabi`); reload shows only the name and no button; replaying the POST leaves the assignment unchanged.
  - A SalesRep POST to the admin `AssignRep` handler is rejected and leaves the order unassigned.
- **Admin flow verified** on the same order after resetting it:
  - SuperAdmin sees the dropdown (no `-- Unassigned --` option); assigning rep 2 persists `Chiamaka Nwosu`, and the reload shows only the name.
  - Replaying `AssignRep` with a different rep is rejected; the assignment stays on rep 2.
  - A SuperAdmin POST to `AcceptOrder` is rejected.
  - An open, already-assigned order (id 3) renders the read-only name with no dropdown.
- **Closed order** (id 17, Dispatched) renders `Unassigned` plus "no further assignment is possible" for both roles, with no button or dropdown.
- **Build**: 0 warnings, 0 errors. **Tests**: 255 passed, 0 failed.
- Both test orders were restored to their original values (`order 14` unassigned, `order 17` assigned to `Babatunde Alabi`), and the verification server was stopped.
- The existing database RBAC row for `Recipes/SalesRep` still reads `View Only`; the nav condition and route guard override it. A reset-to-defaults will now produce `None`.

