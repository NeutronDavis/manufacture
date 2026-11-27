# Plan: Role-ID Based Authorization

**Status**: Done

## Summary

Replace every hardcoded role **name** used for navigation, authentication, authorization and role-driven business rules with the numeric `Roles.Id`, exposed through the existing `Manufacture.Models.Domain.UserRole` enum pinned to IDs `1..6`. The session stores `UserRoleId` at login (next to the role name, which remains for display only). `RbacCache`, `RbacService`, report authorization/scoping, notification recipients, last-SuperAdmin protection and the global page filter all key on role ID instead of name. Display surfaces keep showing `Roles.DisplayName` / `Roles.Name`.

The plan also fixes the current `?? "SuperAdmin"` fallback (deny instead), aligns `RbacDefaults` with the seed SQL (SalesRep/Recipes = `None`), and seeds the live database with the full default RBAC matrix (10 modules, 9 levels, 60 role/module cells) so the Settings → RBAC screen is populated.

## Scope

- **In-Scope**
  - Pin `UserRole` enum values to database role IDs 1–6.
  - Store `UserRoleId` in session at login; add session-role helpers.
  - Convert `RbacCache`, `RbacService`, `RbacDefaults` to role IDs.
  - Convert report catalogue, report authorization and report row-scoping to role IDs.
  - Convert notification recipient rules and the notification audience selector to role IDs.
  - Convert `UserService` last-SuperAdmin protection and customer-link validation to role IDs.
  - Convert `RequireSessionPageFilter` and `SalesService.CreateOrderAsync` to role IDs.
  - Convert all Razor pages/views that gate on role names (41 files, full list below).
  - Deny when the session role ID is missing/unknown (remove the SuperAdmin fallback).
  - Make seed scripts explicit and ID-based; seed RBAC defaults into the live DB.
  - Update/add tests.

- **Out-of-Scope**
  - No database schema change: `Roles` keeps `Id`, `Name`, `DisplayName`, `IsSystemRole`.
  - Module keys and permission-level names remain strings (only roles were requested).
  - Enabling rename of system-role `Name` values (currently blocked in `UserService.SaveRoleAsync` and the Roles UI). The refactor makes such a rename safe if enabled later; `DisplayName` is already editable.
  - `RbacPermissionAudits.RoleName` snapshots and audit `*ByName` columns stay strings (historical display data).
  - Mock/demo persona data (`MockUserService`, `MockSalesService`) keeps role-name strings; it never authenticates users.
  - Adding role-based checks to POST handlers that currently only guard `OnGet` (`Settings/*`, `Production/Repositories/Create`) — separate security hardening.
  - Claims/JWT migration; the app is session-based.

## Decisions (from user)

- **2026-11-27**: "i want this system not to hardcode any role but instead use thier ID … it is generally known in the system databse that [ID 1..6] … so i want thier id to be use where it was hard coded, also you can use enum to make it easy. anywhere the role is showing, then keep showing the name but if the role is use for navigation or authentication and it's hardcoded pls use the id because the name can change but the functions remain the same."
- **Scope**: convert **all** role-based rules (nav/auth + report scoping, notifications, last-admin protection, customer-link validation), not only nav/auth.
- **Missing role**: **deny** access; remove the `?? "SuperAdmin"` fallback.
- **RBAC mismatch**: standardise SalesRep/Recipes on **`None`** (matches `RbacDefaults`) and update the seed SQL. Also **run the same `RbacDefaults` data against the live database**.

## Role contract

| Enum member | Value | DB `Roles.Id` |
|---|---:|---:|
| `UserRole.SuperAdmin` | 1 | 1 |
| `UserRole.ProductionManager` | 2 | 2 |
| `UserRole.StoreManager` | 3 | 3 |
| `UserRole.SalesRep` | 4 | 4 |
| `UserRole.Customer` | 5 | 5 |
| `UserRole.HrPayrollManager` | 6 | 6 |

Session keys (centralised in a new `SessionKeys` static class):
`UserId` (int), `UserRoleId` (int, new), `UserRole` (string, display only), `UserName`, `UserEmail`.

Helper signatures (new `SessionUserExtensions`):
`GetRoleId(this ISession) -> int` (0 when absent), `GetRoleName(this ISession) -> string`, `IsInRole(this ISession, UserRole) -> bool`.

When `UserRoleId` is missing or `<= 0`: `RequireSessionPageFilter` clears the session and redirects to `/Login`; RBAC returns no access; no page treats it as SuperAdmin.

## Implementation Steps

### Phase 1 — Foundation

1. **Pin the enum to DB IDs.**
   - *Details*: In `Manufacture.Library/Models/Domain/UserAndEmployee.cs`, set explicit values `SuperAdmin = 1 … HrPayrollManager = 6`; add an XML comment that the values are pinned to `dbo.Roles.Id` and must never be reordered. Keep `User.Role` as the display string.
   - *Dependencies*: None.

2. **Add the session contract and write the role ID at login.**
   - *Details*: Add `Manufacture.Web/Services/SessionKeys.cs` with the five key constants. In `Pages/Login.cshtml.cs` `SetUserSession`, call `SetInt32(SessionKeys.UserRoleId, user.RoleId)` and keep `SetString(SessionKeys.UserRole, user.Role)`. Defensive: if `user.RoleId <= 0`, abort the login with the generic invalid-credentials message.
   - *Dependencies*: Step 1.

3. **Add session helpers and enforce role presence globally.**
   - *Details*: Add `Manufacture.Web/Services/SessionUserExtensions.cs` (`GetRoleId`, `GetRoleName`, `IsInRole`). In `RequireSessionPageFilter.OnPageHandlerExecuting`, after the `UserId` check, require `Session.GetInt32(SessionKeys.UserRoleId)`; if absent, `Session.Clear()` and redirect to `/Login`. Replace the four role-name `if` blocks with `roleId == (int)UserRole.X`.
   - *Dependencies*: Steps 1, 2.

### Phase 2 — Authorization core

4. **Convert `RbacCache` to role IDs.**
   - *Details*: `Manufacture.Web/Services/RbacCache.cs` — change every public API from `string role` to `int roleId` (`HasAccess`, `GetPermissionLevel`, `CanView/Create/Edit/Delete/Approve/Print/Manage/Request/Issue/Disburse/BookOwn`). `SetSnapshot` keys on `(cell.RoleId, row.Module.ModuleKey)`. `IsSuperAdmin` compares to `(int)UserRole.SuperAdmin`. Keep the synthetic `FullAccess` level. Role ID `0`/unknown returns no access.
   - *Dependencies*: Step 1.

5. **Convert `RbacService` to role IDs.**
   - *Details*: Remove `SuperAdminRole`; use `(int)UserRole.SuperAdmin`. `GetMatrixAsync` sets `IsLocked = role.Id == (int)UserRole.SuperAdmin`; `SaveMatrixAsync` forces Full Access for that ID (it already keys roles by `Id`). Keep `FullAccessLevel` (permission-level name, not a role).
   - *Dependencies*: Steps 1, 4.

6. **Convert `RbacDefaults` and the reset path.**
   - *Details*: `RbacDefaults.Matrix` becomes `IReadOnlyList<(string ModuleKey, UserRole Role, string LevelName)>`; replace 60 role-name strings with enum members. Set `("Recipes", UserRole.SalesRep, "None")` to match the chosen value. In `RbacService.ResetToDefaultsAsync`, resolve `_db.Roles` by `Id` and look up `(int)entry.Role`.
   - *Dependencies*: Steps 1, 5.

7. **Convert reports to role IDs.**
   - *Details*:
     - `ReportDefinitionDto`: replace `List<string> AllowedRoles` with `List<int> AllowedRoleIds` (no other consumers).
     - `ReportCatalog`: remove the five role-name constants; arrays become `UserRole[]`; `Define(...)` fills `AllowedRoleIds` via `(int)`; `ForRole(int roleId)` compares IDs.
     - `ReportAuthorizationService`: `VisibleReports(int roleId)`, `CanAccess(int roleId, string key)`; SuperAdmin bypass by ID.
     - `ReportQueryContext`: `string Role` → `int RoleId`.
     - `ReportQueryService.RunAsync(..., int roleId, ...)`: SalesRep/Customer checks by ID.
     - `ReportQueries`: all 10 `ctx.Role == "SalesRep"` checks → `ctx.RoleId == (int)UserRole.SalesRep` (this also removes the current case-sensitivity inconsistency).
     - `Pages/Reports/Index.cshtml.cs`: add `ActiveRoleId` from session; keep `ActiveRole` only for the header display at `Reports/Index.cshtml:22`; use `ActiveRoleId` in both GET and export paths.
   - *Dependencies*: Steps 1, 2.

8. **Convert notifications to role IDs.**
   - *Details*: `NotificationService` — replace `CustomerRole`/`SuperAdminRole`/`ProductionManagerRole` constants with enum IDs; recipient queries filter `u.RoleId`, not `u.Role.Name`; `GetAudienceRoles()` returns role DTOs (ID + display name) and excludes `(int)UserRole.Customer`; `SendNotificationDto.Audience` (string) → `int? AudienceRoleId` (null = everyone except Customers); `ResolveRecipients(int? roleId)` validates the role and rejects Customer by ID. Update `Pages/Notifications/Index.cshtml.cs` (`AudienceRoles` type, `IsSuperAdmin`, Customer redirect, `LoadUser` role ID) and `Pages/Notifications/Index.cshtml` (option value = `role.Id`, label = `role.DisplayName`).
   - *Dependencies*: Steps 1, 2.

9. **Convert `UserService` role business rules.**
   - *Details*: Remove `SuperAdminRole`; `SetActiveAsync`/`DeleteAsync` compare `user.RoleId` and count other active users with `RoleId == (int)UserRole.SuperAdmin`. Change `ValidateCustomerLinkAsync(int? customerId, int roleId, ...)` and its two callers. Change `UserDto.Role` default from `"SalesRep"` to `string.Empty` (display-only field).
   - *Dependencies*: Step 1.

10. **Convert `SalesService` and its caller.**
    - *Details*: `CreateOrderAsync(..., int? currentUserRoleId = null, ...)`; replace the Customer/SalesRep name checks with enum IDs. In `Pages/Sales/Orders/Create.cshtml.cs`, read `UserRoleId` and pass it.
    - *Dependencies*: Steps 1, 2.

### Phase 3 — Razor pages and views

> Pattern for every file: add/keep a display property for the role **name** only where the UI renders it; add a numeric `UserRoleId` (or `ActiveRoleId`) read via `GetRoleId()`; replace every name comparison with `roleId == (int)UserRole.X`. In `.cshtml`, add `@using Manufacture.Models.Domain` where the enum is referenced, or expose booleans (`IsCustomer`, `IsSalesRep`, `IsSuperAdmin`) from the page model.

11. **Shared layout and dashboard.**
    - `Pages/Shared/_Layout.cshtml`: replace all 23 role-name checks (notification visibility, quick actions, nav sections, Settings links) with `roleId` comparisons; keep `@userRole` at line 85 for display. Remove the `?? "Guest"` privilege ambiguity (ID 0 fails closed).
    - `Pages/Dashboard/Index.cshtml.cs`: add `ActiveRoleId`; keep `ActiveRole` only for display; use the ID for the JSON drill guard and every data-loading branch.
    - `Pages/Dashboard/Index.cshtml`: replace all `Model.ActiveRole == "..."` gates/headers with `Model.ActiveRoleId == (int)UserRole....`; keep the rendered role text.
    - *Dependencies*: Steps 1–6.

12. **Sales pages.**
    - `Pages/Sales/Index.cshtml.cs` + `Index.cshtml` (SalesRep redirect/hide actions).
    - `Pages/Sales/Orders/Index.cshtml.cs` (SalesRep/ProductionManager flags).
    - `Pages/Sales/Orders/Create.cshtml.cs` (Customer/SalesRep/ProductionManager; GET and POST) and pass the role ID to `SalesService`.
    - `Pages/Sales/Orders/Details.cshtml.cs` (ProductionManager/SalesRep/SuperAdmin capabilities) — downstream view booleans need no change.
    - `Pages/Sales/Reconciliation/Index.cshtml.cs` (SalesRep scoping + SuperAdmin manage checks in both helpers).
    - `Pages/Sales/PreOrderSchedule/Index.cshtml.cs` and `Details.cshtml.cs` (Customer-only guards).
    - *Dependencies*: Steps 1–6, 10.

13. **Inventory pages.**
    - `Pages/Inventory/Index.cshtml.cs` (`CanManageStock` excludes ProductionManager).
    - `Pages/Inventory/Items/Details.cshtml.cs` and `Details.cshtml` (ProductionManager stock management; StoreManager supplier-link hiding).
    - `Pages/Inventory/Requests/Index.cshtml.cs` (StoreManager/SuperAdmin approve/issue).
    - *Dependencies*: Steps 1–6.

14. **Production pages.**
    - `Pages/Production/Pricing/Index.cshtml.cs` (ProductionManager submit, SuperAdmin approve/reject; both GET/POST) and `History.cshtml.cs`.
    - `Pages/Production/Recipes/Index.cshtml.cs` + `Index.cshtml` (SalesRep hides actions).
    - `Pages/Production/Recipes/Details.cshtml.cs` + `Details.cshtml` (`IsSuperAdmin` delete guard, SalesRep hide actions).
    - `Pages/Production/Repositories/{Index,Create,Edit,Details}.cshtml.cs` (SuperAdmin/CanEdit checks, including the `Details` POST helper).
    - *Dependencies*: Steps 1–6.

15. **Settings, UserManagement, Logistics and Notifications pages.**
    - `Pages/Settings/{Index,Finance,Production,ProductionShifts,Measurements}.cshtml.cs`: replace `?? "SuperAdmin"` with `GetRoleId()`, use `HasAccess`/`CanEdit` with the ID; `Finance` SuperAdmin-only reset by ID.
    - `Pages/UserManagement/Roles.cshtml.cs` and `Permissions.cshtml.cs`: `CanManageRoles`/`CanManageRbac` by ID; keep the display name for messages.
    - `Pages/UserManagement/Create.cshtml.cs`: default role ID = `(int)UserRole.SalesRep`.
    - `Pages/Logistics/Rates/Index.cshtml.cs` and `Edit.cshtml.cs`: `CanApprove` and the segregation-of-duties check by ID.
    - `Pages/Notifications/Index.cshtml.cs`: already covered in Step 8; verify no name checks remain.
    - *Dependencies*: Steps 1–6, 8, 9.

16. **User create/edit client-side role logic.**
    - *Details*: In `Pages/UserManagement/Create.cshtml` and `Edit.cshtml`, `showCustomerLink` compares `selectedRole?.Id == (int)UserRole.Customer`; change the option attribute `data-role-name` to `data-role-id="@role.Id"` and the JS `isCustomer` check to compare against the Customer ID (emit it from Razor). Keep the visible `@role.DisplayName (@role.Name)` label.
    - *Dependencies*: Step 1.

### Phase 4 — Seed data and live RBAC

17. **Make the full seed ID-based and consistent.**
    - *Details*: In `scripts/seed-database.sql`: insert the six roles with explicit IDs 1–6 (`SET IDENTITY_INSERT [dbo].[Roles] ON/OFF`, guarded by `Id`, not `Name`); replace user `RoleId` subqueries with literal IDs; rewrite the `RbacRolePermissions` MERGE to key on `RoleId` (roles) and module/level lookups by key/name; set SalesRep/Recipes to `None`. Add a guard that throws if IDs 1–6 are not all present.
    - *Dependencies*: Steps 1, 6.

18. **Create the live RBAC seed and update the one-shot reset script.**
    - *Details*: New `scripts/seed-rbac.sql` (idempotent, non-destructive): MERGE the 10 `RbacModules` by `ModuleKey`, the 9 `RbacPermissionLevels` by `Name`, and the 60 `RbacRolePermissions` by `(RoleId, ModuleId)` using `(RoleId, ModuleKey, LevelName)` rows; throw if role IDs 1–6 are missing. Add the same RBAC block to `scripts/manufacture-live-schema.sql` after the role seed, switch its roles to explicit IDs, and set the SuperAdmin user `RoleId = 1`. The two files must contain the same 60 cells as `RbacDefaults`.
    - *Dependencies*: Step 17.

19. **Apply the RBAC defaults to the live database.**
    - *Details*: Run `sqlcmd -S 104.40.192.145 -d "Manufacture-DB" -U SBUser -P <password> -b -i scripts/seed-rbac.sql` (live currently has empty RBAC tables). Do this only after Step 18 is reviewed; it is non-destructive. Verify 10 modules, 9 levels, 60 permissions, and that `Recipes/SalesRep = None`.
    - *Dependencies*: Step 18.

### Phase 5 — Tests and verification

20. **Update and extend tests.**
    - *Details*:
      - `Manufacture.Tests/RbacTests.cs`: build `RoleDto` from enum IDs; assert capabilities by ID; add a case where role ID `0`/unknown has no access; keep 60-cell count.
      - `Manufacture.Tests/ReportCatalogTests.cs`: `ForRole((int)UserRole.X)` and `CanAccess((int)UserRole.X, key)`.
      - Add `Manufacture.Tests/RoleContractTests.cs`: enum values are exactly 1–6 with the expected names (guards accidental reordering).
      - Add `Manufacture.Tests/RbacSeedScriptTests.cs`: locate the repo root by walking up to `Manufacture.slnx`; parse the 60 `(RoleId, ModuleKey, LevelName)` tuples and the module/level lists from `scripts/seed-rbac.sql`, `scripts/seed-database.sql` and `scripts/manufacture-live-schema.sql`; assert they match `RbacDefaults` exactly (prevents future drift like the current SalesRep/Recipes mismatch).
      - `Manufacture.Tests/MockAnalyticsServiceTests.cs` unchanged (mock layer keeps names).
    - *Dependencies*: Steps 1–19.

21. **Full build, leftover audit and manual verification.** See Verification Plan.
    - *Dependencies*: Step 20.

## Relevant Files & Symbols

### Library
- `Manufacture.Library/Models/Domain/UserAndEmployee.cs`: `UserRole` enum values; `User.Role` display field.
- `Manufacture.Library/Models/DTOs/ReportDtos.cs`: `ReportDefinitionDto.AllowedRoleIds`, `ReportQueryContext.RoleId`.
- `Manufacture.Library/Models/DTOs/NotificationDtos.cs`: `SendNotificationDto.AudienceRoleId`.
- `Manufacture.Library/Models/DTOs/UserAndEmployeeDtos.cs`: `UserDto.RoleId` (already present), `UserDto.Role` default.

### Web plumbing
- `Manufacture.Web/Pages/Login.cshtml.cs`: `SetUserSession`.
- `Manufacture.Web/Services/SessionKeys.cs` (new), `SessionUserExtensions.cs` (new).
- `Manufacture.Web/Services/RequireSessionPageFilter.cs`.
- `Manufacture.Web/Services/RbacCache.cs`, `RbacService.cs`, `RbacDefaults.cs`.
- `Manufacture.Web/Services/UserService.cs` (`SetActiveAsync`, `DeleteAsync`, `ValidateCustomerLinkAsync`, `CreateAsync`, `UpdateAsync`).
- `Manufacture.Web/Services/NotificationService.cs` (`GetAudienceRoles`, `SendGeneral`, `ResolveRecipients`, `AddPriceChangeSubmitted`, `ResolveDecisionRecipients`).
- `Manufacture.Web/Services/SalesService.cs` (`CreateOrderAsync`).
- `Manufacture.Web/Services/Reports/ReportCatalog.cs`, `ReportAuthorizationService.cs`, `ReportQueryService.cs`, `ReportQueries.cs`.
- `Manufacture.Web/Program.cs`: no new registrations required; `RbacCache` stays a singleton.

### Pages / views (role-name checks to convert)
- `Pages/Shared/_Layout.cshtml`
- `Pages/Dashboard/Index.cshtml.cs`, `Pages/Dashboard/Index.cshtml`
- `Pages/Reports/Index.cshtml.cs`
- `Pages/Sales/Index.cshtml.cs`, `Sales/Index.cshtml`
- `Pages/Sales/Orders/Index.cshtml.cs`, `Orders/Create.cshtml.cs`, `Orders/Details.cshtml.cs`
- `Pages/Sales/Reconciliation/Index.cshtml.cs`
- `Pages/Sales/PreOrderSchedule/Index.cshtml.cs`, `Details.cshtml.cs`
- `Pages/Inventory/Index.cshtml.cs`
- `Pages/Inventory/Items/Details.cshtml.cs`, `Details.cshtml`
- `Pages/Inventory/Requests/Index.cshtml.cs`
- `Pages/Production/Pricing/Index.cshtml.cs`, `History.cshtml.cs`
- `Pages/Production/Recipes/Index.cshtml.cs`, `Index.cshtml`, `Details.cshtml.cs`, `Details.cshtml`
- `Pages/Production/Repositories/Index.cshtml.cs`, `Create.cshtml.cs`, `Edit.cshtml.cs`, `Details.cshtml.cs`
- `Pages/Settings/Index.cshtml.cs`, `Finance.cshtml.cs`, `Production.cshtml.cs`, `ProductionShifts.cshtml.cs`, `Measurements.cshtml.cs`
- `Pages/Logistics/Rates/Index.cshtml.cs`, `Edit.cshtml.cs`
- `Pages/UserManagement/Roles.cshtml.cs`, `Permissions.cshtml.cs`, `Create.cshtml.cs`, `Create.cshtml`, `Edit.cshtml`
- `Pages/Notifications/Index.cshtml.cs`, `Index.cshtml`

### Seeds
- `scripts/seed-database.sql` (roles, users, RBAC MERGE at ~1579–1727).
- `scripts/seed-rbac.sql` (new).
- `scripts/manufacture-live-schema.sql` (role seed + RBAC seed + SuperAdmin).

### Tests
- `Manufacture.Tests/RbacTests.cs`, `ReportCatalogTests.cs`.
- `Manufacture.Tests/RoleContractTests.cs`, `RbacSeedScriptTests.cs` (new).
- `Manufacture.Tests/MockAnalyticsServiceTests.cs` (reference only; unchanged).

## Verification Plan

1. **Automated**
   - `dotnet build Manufacture.slnx`
   - `dotnet test Manufacture.Tests\Manufacture.Tests.csproj`
   - Leftover audit (review hits; allowed only in `Mock*`, audit/display text and docs):
     `rg -n '"(SuperAdmin|ProductionManager|StoreManager|SalesRep|Customer|HrPayrollManager)"' Manufacture.Web Manufacture.Library --glob '!**/Mock*'`
   - Confirm no `Session.GetString("UserRole")` drives a decision:
     `rg -n 'GetString\("UserRole"\)' Manufacture.Web` → only display/property assignment.

2. **Manual (local `ManufactureDb`)**
   - Log in as each seeded demo user; verify nav sections and quick actions match the previous behaviour.
   - SuperAdmin: all nav sections, Settings, Reports; open `UserManagement/Permissions` and confirm the 60-cell matrix renders and saves.
   - ProductionManager: no Fleet nav, no POS order creation, no inventory create/edit links.
   - StoreManager: no Production nav, no Approved Suppliers link, can approve/issue requisitions.
   - SalesRep: lands on Orders, no Sales hub/Production, reconciliation scoped to own rep.
   - Customer: only dashboard + pre-order schedule/create; no notifications bell.
   - Change `ProductionManager` **DisplayName** to "Ops Manager"; confirm nav/auth unchanged, then revert. (Machine `Name` remains locked; IDs are the stable key.)
   - Create a custom role, assign a user, set matrix cells; confirm login/nav behaviour uses its ID and RBAC cells.
   - Clear the `UserRoleId` session key (or simulate an old session); confirm redirect to `/Login` rather than SuperAdmin access.

3. **Live database**
   - Run `scripts/seed-rbac.sql`; verify:
     `SELECT COUNT(*) FROM RbacModules` = 10, `RbacPermissionLevels` = 9, `RbacRolePermissions` = 60.
   - `SELECT l.Name FROM RbacRolePermissions p JOIN Roles r ON r.Id=p.RoleId JOIN RbacModules m ON m.Id=p.ModuleId JOIN RbacPermissionLevels l ON l.Id=p.PermissionLevelId WHERE r.Id=4 AND m.ModuleKey='Recipes'` → `None`.
   - Deploy the updated app; log in as `admin@bakery.com` / `Password123!`; all nav sections render and Settings → RBAC is populated.

## Context & Decisions Log

- **2026-11-27**: Role IDs are a fixed application contract: 1 SuperAdmin, 2 ProductionManager, 3 StoreManager, 4 SalesRep, 5 Customer, 6 HrPayrollManager.
- **2026-11-27**: Display surfaces keep showing role names; authorization/navigation/business rules use IDs.
- **2026-11-27**: Convert all role-based rules, not just nav/auth.
- **2026-11-27**: Missing role → deny; no SuperAdmin fallback.
- **2026-11-27**: SalesRep/Recipes standardised to `None`; seed the live DB with `RbacDefaults`.

## Key Decisions & Trade-offs

- **Enum over raw integer constants**: one named source of truth (`UserRole`) with values pinned to DB IDs; casting at the boundary is trivial and compile-time checked.
- **Session stores both ID and name**: ID for decisions, name for display; avoids a DB lookup on every request while preserving current UI text.
- **`RbacCache` keys on `(RoleId, ModuleKey)`**: minimal change to the existing cache; module keys remain stable identifiers. `RbacRolePermissions` was already fully numeric.
- **Reports use `AllowedRoleIds`**: removes the name arrays and the `ForRole` name comparison; report keys/groups stay strings because they are not roles.
- **Notification audience becomes a role ID**: the dropdown still shows display names, but recipients are resolved by ID, so a rename cannot silently change the audience.
- **Deny on missing role**: closes the current privilege-escalation path; deploying invalidates in-memory sessions anyway, so the practical impact is a re-login.
- **Seed drift guard**: the new SQL-vs-`RbacDefaults` test makes the live seed and the "Reset Defaults" action provably identical.

## Execution Result (2026-11-27)

- All phases implemented. `dotnet build Manufacture.slnx` succeeds with 0 warnings / 0 errors; `dotnet test` passes 275/275.
- New: `Manufacture.Web/Services/SessionKeys.cs`, `SessionUserExtensions.cs`, `scripts/seed-rbac.sql`,
  `Manufacture.Tests/RoleContractTests.cs`, `Manufacture.Tests/RbacSeedScriptTests.cs`.
- Seed scripts now use explicit role IDs 1-6 and an ID-keyed RBAC matrix; `scripts/manufacture-live-schema.sql`
  resets and rebuilds the database with roles, RBAC and the SuperAdmin user.
- Validated on a scratch LocalDB: 57 tables, 6 roles, 1 SuperAdmin user, 10 modules, 9 levels, 60 permissions;
  the reset script is repeatable and `seed-database.sql` / `seed-rbac.sql` are idempotent.
- Live `Manufacture-DB`: ran `scripts/seed-rbac.sql`; now has 10 modules, 9 levels, 60 permissions, with
  SalesRep/Recipes = `None`.
- Local `ManufactureDb`: RBAC aligned to `RbacDefaults`.
- Manual HTTP checks against the running app: SuperAdmin sees all nav and the 60-cell RBAC page; SalesRep is routed
  from `/Production` to `/Dashboard` and from `/Sales/Terminals` to `/Sales/Orders`; ProductionManager is routed
  from `/Logistics` to `/Dashboard` and cannot access stock creation; StoreManager is routed from
  `/Inventory/Suppliers` to `/Inventory`; Customer is routed from `/Inventory` to `/Sales/PreOrderSchedule`.

## Follow-up: New-role RBAC initialisation (2026-11-27)

- `IRbacService.InitializeRolePermissionsAsync(roleId, updatedBy)` creates a `None` row for every active module
  that is missing a permission row; `InitializeMissingRolePermissionsAsync(updatedBy)` runs it for all roles.
- `Pages/UserManagement/Roles.cshtml.cs` calls `InitializeRolePermissionsAsync` when a new role is saved, then
  refreshes the RBAC cache.
- `Program.cs` runs the idempotent backfill at startup (failure is logged without blocking the app) so roles
  created before this change, or outside the app, receive their default rows.
- The admin can change any of these rows from Settings → RBAC Matrix; `SaveMatrixAsync` updates them by
  `(RoleId, ModuleId)`.
