# Plan: Fleet Dispatch Form Cleanup + Route Sheet Link

**Status**: Implemented — verified against the local app (2026-11-18)
**Created**: 2026-11-18
**Owner**: Fleet delivery dispatches (`/Fleet/Dispatches/New`, `/Fleet/Dispatches/Details`) and sales route sheets (`/Sales/Dispatch`, `/Sales/Reconciliation`)

## Summary (TL;DR)

1. **`/Fleet/Dispatches/New`** (real page: `Manufacture.Web/Pages/Logistics/Dispatches/Create.cshtml`): remove **Odometer Start** entirely (form, DB column, entity/DTO, details page). Convert **Purpose** from an enum dropdown to a free-text field backed by `NVARCHAR(200)`. Re-layout the form into two clean two-column rows plus a full-width notes area.
2. **Tie a route sheet to a delivery dispatch**: `DailyRepRouteSheets` gains a nullable `FleetDispatchId` FK (one-to-one, enforced by a filtered unique index). The **New Route Sheet** dialog on `/Sales/Dispatch` gains a **required** "Delivery Dispatch" dropdown listing only **Planned** delivery dispatches that are not already linked. The route sheet's vehicle is taken from the chosen delivery dispatch.
3. **Rep dropdown cleanup**: "Select Sales Rep" shows only sales-rep names. SuperAdmin sees every rep; a signed-in SalesRep sees only themselves. The rep's assigned vehicle no longer appears there, because the vehicle now comes from the delivery dispatch.

**Answer to "the route sheet form will contain the daily dispatch"**: the delivery dispatch already defines the vehicle and driver, so the route sheet copies `VehicleId` + registration from it. The rep-master vehicle becomes irrelevant for newly created sheets.

## User Requirements (verbatim)

- "at the /Fleet/Dispatchs/New page, remove the `Odometer Start` field and rearange the form. it's not looking good"
- "the `Purpose` field should be a text field not a dropdown make sure to arange the form"
- "i think it's proper for a Delivery Dispatch to be tide to a route dispatch in the /Sales/Dispatch page"
- "in that case i like to include a dropdown of the newly created `Delivery dispatch` that are not been completed and not on transit"
- "only planneded dispatich should be in the dropdown which means, it's just createdt"
- "remove it form the form, table and anytother page displaying it" (Odometer Start)
- "in the Create Daily Route Sheet, the `Select Sales Rep` field should only list out employee that are sales rep if the loged in user is the super admin else it should show the loged in sales rep only in the dropdown and not vehicle assigned to the rout so its just the sales rep"
- "note the route sheet form will contain the daily dispatch which means that the vehicle assigned is already created in the daily dispatch"

## Decisions (verbatim answers)

| Question | Answer |
|---|---|
| Dropdown location | **"Sales Dispatch only (Recommended)"** — the picker lives in the New Route Sheet dialog. |
| Eligible dispatches | **"only planneded dispatich should be in the dropdown which means, it's just createdt"** — status exactly `Planned` (0). |
| Link cardinality | **"One-to-one (Recommended)"** — one route sheet per delivery dispatch; filtered unique index. |
| Odometer Start | **"remove it form the form, table and anytother page displaying it"** — drop the DB column and every reference. |
| Purpose storage | **"Convert column to text (Recommended)"** — `INT` enum → `NVARCHAR(200)`, existing values converted to labels, enum deleted. |
| Required link | **"Required delivery dispatch (Recommended)"** — a new route sheet cannot be generated without one. |
| Route vehicle | **"Take vehicle from dispatch (Recommended)"** — copy `VehicleId` + registration from the fleet dispatch. |
| Rep dropdown text | **"Name only (Recommended)"** — no route name, no vehicle registration. |
| Vehicle clash handling | Resolved by the required link + vehicle-from-dispatch decision; no warning needed. |

## Current State Findings

| Area | Today | Change |
|---|---|---|
| `FleetDispatches.Purpose` | `INT NOT NULL` default 0, `CK_FleetDispatches_Purpose IN (0,1,2,3)`, mapped to `FleetDispatchPurpose` enum | `NVARCHAR(200) NOT NULL` default `N''`; drop check constraint and enum |
| `FleetDispatches.OdometerStart` | `DECIMAL(18,2) NULL`, input on Create, display on Details | Column and all references removed; `CK_FleetDispatches_Odometer` keeps only `OdometerEnd` |
| Fleet Create form | 3-col row: Date / Purpose dropdown / Odometer, then 2-col Vehicle / Driver, then Notes | Two 2-col rows + full-width Notes |
| `DailyRepRouteSheets` | No link to fleet dispatches; vehicle comes from `SalesRep.VehicleId` | Add `FleetDispatchId INT NULL` FK + filtered unique index |
| Dispatch page modal | Reps list renders `FullName (RouteName - VehicleRegistration)` | Name only; add required Planned delivery-dispatch picker |
| Reconciliation | `ReconcileDailyRepAccountAsync` auto-creates tomorrow's sheet with no delivery dispatch | Stop auto-creating; carry balances/stock when the next sheet is created explicitly |
| `/Fleet/Dispatchs/New` URL | Typo route; actual aliases are `/Fleet/Dispatches/New` and `/Logistics/Dispatches/Create` (`Program.cs` L45-47) | Plan targets the real page files |
| Seed data | No `FleetDispatches` seed rows | No FK backfill needed; only Purpose label conversion for any existing rows |

## Scope

**In scope**
- Remove `OdometerStart` from the SQL table, EF entity, `FleetDispatchDto`, `FleetDispatchService` create/update/map, Create form and Details page. Adjust the odometer check constraint.
- Convert `Purpose` to free text across schema, entity, DTO, service and form; delete `FleetDispatchPurpose`.
- One-time, idempotent conversion of any existing `Purpose` values `'0'..'3'` to their labels after the column alter.
- Re-layout the Fleet Create form; keep the delivery-points editor and summary sidebar unchanged.
- Add `FleetDispatchId` to `DailyRepRouteSheets` (nullable, FK, filtered unique index), EF entity/nav, DbContext mapping, SQL table, DB README/ER diagram.
- Add `int? fleetDispatchId` to `ISalesService.CreateRouteSheetAsync` and enforce: required, exists, `Planned`, not already linked.
- Copy `VehicleId` and `VehicleRegistration` from the fleet dispatch onto the route sheet.
- New `IFleetDispatchService.GetAvailableForSalesRouteAsync` returning only Planned, unlinked dispatches.
- New Route Sheet dialog: required delivery-dispatch dropdown; rep dropdown shows names only (SuperAdmin → all reps, SalesRep → self).
- Show the linked delivery dispatch (ref, status, vehicle, driver, link) on the selected route sheet header.
- Remove the auto-creation of the next-day route sheet during reconciliation, while keeping the balance/closing-stock carry-forward for explicitly created sheets.
- Pure rules helper + unit tests; docs update.

**Out of scope**
- Changing the Fleet delivery-points editor, stop statuses, fuel logging, or the vehicle `InTransit` lifecycle.
- Removing `OdometerEnd` (only `OdometerStart` is in scope).
- Changing or releasing a route sheet's linked delivery dispatch after creation (delete the `Ordered` sheet and recreate if the link is wrong).
- Showing the linked route sheet on the Fleet Details page (nice-to-have, not requested).
- Changing reports, dashboards, RBAC, or the supermarket debt work delivered previously.
- Backfilling `FleetDispatchId` for existing route sheets (they remain `NULL` and render as "legacy / not linked").

## Implementation Steps

### Phase 1 — Schema

1. **Fleet table changes**
   - *Details*: In `Manufacture.Database/Tables/FleetDispatches.sql`:
     - Change `[Purpose] INT NOT NULL DEFAULT (0)` to `[Purpose] NVARCHAR (200) NOT NULL CONSTRAINT [DF_FleetDispatches_Purpose] DEFAULT (N'')`.
     - Delete `[OdometerStart] DECIMAL (18, 2) NULL`.
     - Delete `CONSTRAINT [CK_FleetDispatches_Purpose] ...`.
     - Replace `CK_FleetDispatches_Odometer` with `CHECK ([OdometerEnd] IS NULL OR [OdometerEnd] >= 0)`.
   - *Dependencies*: None.

2. **Route-sheet link columns**
   - *Details*: In `Manufacture.Database/Tables/DailyRepRouteSheets.sql`:
     - Add `[FleetDispatchId] INT NULL` after `[VehicleRegistration]`.
     - Add `CONSTRAINT [FK_DailyRepRouteSheets_FleetDispatches] FOREIGN KEY ([FleetDispatchId]) REFERENCES [dbo].[FleetDispatches] ([Id])` (no cascade).
     - Add:
       ```sql
       CREATE UNIQUE NONCLUSTERED INDEX [UQ_DailyRepRouteSheets_FleetDispatch]
           ON [dbo].[DailyRepRouteSheets] ([FleetDispatchId] ASC)
           WHERE [FleetDispatchId] IS NOT NULL;
       ```
   - *Dependencies*: Parallel with Step 1.

3. **Purpose label migration + docs**
   - *Details*: Append an idempotent block to `scripts/seed-database.sql` (runs after every publish, same file the deploy script already applies):
     ```sql
     UPDATE [dbo].[FleetDispatches]
     SET [Purpose] = CASE [Purpose]
         WHEN N'0' THEN N'Production Delivery'
         WHEN N'1' THEN N'Pre-Order Delivery'
         WHEN N'2' THEN N'Internal Transfer'
         WHEN N'3' THEN N'Other'
         ELSE [Purpose] END
     WHERE [Purpose] IN (N'0', N'1', N'2', N'3');
     ```
     Update `Manufacture.Database/README.md`: note the text Purpose and the route-sheet ↔ delivery-dispatch link; add `FleetDispatches ||--o| DailyRepRouteSheets : "carries"` to the ER diagram.
   - *Dependencies*: Depends on Step 1.

4. **EF entities and DbContext**
   - *Details*:
     - `Manufacture.Library/Models/Entities/FleetDispatch.cs`: `Purpose` → `string`; remove `OdometerStart`; add `public virtual DailyRepRouteSheet? DailyRepRouteSheet { get; set; }`.
     - `Manufacture.Library/Models/Entities/DailyRepRouteSheet.cs`: add `public int? FleetDispatchId { get; set; }` and `public virtual FleetDispatch? FleetDispatch { get; set; }`.
     - `Manufacture.Library/Data/ManufactureDbContext.cs` (~L517-560): `Purpose` max length 200 + default `""`; remove `OdometerStart` mapping; add one-to-one mapping:
       `entity.HasOne(d => d.FleetDispatch).WithOne(p => p.DailyRepRouteSheet).HasForeignKey<DailyRepRouteSheet>(d => d.FleetDispatchId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_DailyRepRouteSheets_FleetDispatches");`
     - `DailyRepRouteSheet` mapping (~L358-410): add the filtered unique index descriptor and the relationship above (from the route-sheet side).
   - *Dependencies*: Depends on Steps 1-2. Parallel with Step 3.

5. **Deploy the schema**
   - *Details*: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/deploy-database.ps1` (the repository has no `pwsh` in this environment). The publish drops `OdometerStart`, alters `Purpose`, adds `FleetDispatchId`, and the seed block rewrites old enum values. `BlockOnPossibleDataLoss=False` is already set in the script.
   - *Dependencies*: Depends on Steps 3-4.

### Phase 2 — Fleet dispatch form

6. **Purpose text + odometer removal in the data layer**
   - *Details*:
     - `Manufacture.Library/Models/DTOs/FleetDtos.cs`: delete `enum FleetDispatchPurpose`; change `FleetDispatchDto.Purpose` to `string` with `[Required(ErrorMessage = "Enter a purpose.")]` + `[StringLength(200)]`; delete `OdometerStart`.
     - `Manufacture.Web/Services/FleetDispatchService.cs`:
       - `CreateDispatchAsync`: `Purpose = dto.Purpose?.Trim() ?? string.Empty`; remove `OdometerStart`.
       - `UpdateDispatchAsync`: same Purpose mapping; **do not** touch `OdometerStart` (property gone).
       - `MapDispatch`: `Purpose = dispatch.Purpose`; remove `OdometerStart`.
     - `Manufacture.Web/Pages/Logistics/Dispatches/Create.cshtml.cs`: default `Purpose = "Production Delivery"` (string); remove any enum import need.
   - *Dependencies*: Depends on Step 4.

7. **Re-layout the Create form**
   - *Details*: `Manufacture.Web/Pages/Logistics/Dispatches/Create.cshtml`, replace the current 3-column details row with:
     - Row A (2 columns): **Dispatch Date** (`sm:grid-cols-2`) | **Purpose** (`<input asp-for="DispatchInput.Purpose" type="text" maxlength="200" required>` with placeholder `e.g. Production delivery — Ikeja route`).
     - Row B (2 columns): **Vehicle** | **Driver** (unchanged markup and `syncDispatchDriver()` wiring).
     - Full-width **Notes** textarea.
     - Delete the `Odometer Start` input and the now-unused `Enum.GetValues<FleetDispatchPurpose>()` loop.
     - Leave the Delivery Points card and the sticky Summary card as they are.
   - *Dependencies*: Depends on Step 6.

8. **Remove Odometer Start from Details/Index**
   - *Details*:
     - `Manufacture.Web/Pages/Logistics/Dispatches/Details.cshtml`: delete the "Odometer Start" grid item; change `grid-cols-2 sm:grid-cols-4` to `sm:grid-cols-3` (Vehicle / Driver / Purpose).
     - `Manufacture.Web/Pages/Logistics/Dispatches/Index.cshtml`: Purpose is a text badge now — add `max-w-[18rem] truncate` + `title="@d.Purpose"` so long text does not break the table. No odometer there today.
     - Leave `OdometerEnd` untouched.
   - *Dependencies*: Depends on Step 6. Parallel with Step 7.

### Phase 3 — Route sheet ↔ delivery dispatch

9. **Domain model and service contract**
   - *Details*:
     - `Manufacture.Library/Models/Domain/RouteSheetEntities.cs`, `DailyRepRouteSheet`: add `FleetDispatchId`, `FleetDispatchRef`, `FleetDispatchStatus`, `FleetDispatchVehicleRegNumber`, `FleetDispatchDriverName`, `FleetDispatchDate` (all nullable/empty defaults for legacy sheets).
     - `Manufacture.Library/Interfaces/ISalesService.cs`: extend to
       `CreateRouteSheetAsync(int repId, DateTime date, Dictionary<int,int>? requestedQuantities = null, IReadOnlyCollection<int>? supermarketAccountIds = null, int? fleetDispatchId = null, CancellationToken cancellationToken = default)`.
     - `Manufacture.Library/Interfaces/IFleetDispatchService.cs`: add
       `Task<List<FleetDispatchDto>> GetAvailableForSalesRouteAsync(CancellationToken cancellationToken = default);`
   - *Dependencies*: Depends on Step 4.

10. **SalesService create + mapping**
    - *Details*: `Manufacture.Web/Services/SalesService.cs`:
      - `CreateRouteSheetAsync`: after the duplicate guard, require `fleetDispatchId`; load the `FleetDispatch` with `.Include(d => d.Vehicle)`; throw `InvalidOperationException` when:
        - `fleetDispatchId` is null/≤0 → "Select a delivery dispatch for this route sheet."
        - not found → "The selected delivery dispatch was not found."
        - `Status != Planned` → "Only planned delivery dispatches can be linked to a route sheet."
        - another sheet already has `FleetDispatchId == id` → "That delivery dispatch is already linked to another route sheet."
      - Set `FleetDispatchId = dispatch.Id`, `VehicleId = dispatch.VehicleId`, `VehicleRegistration = dispatch.Vehicle?.RegistrationNumber`.
      - Include `s.FleetDispatch` in `GetRouteSheetsAsync` and `GetRouteSheetAsync`; extend `MapRouteSheet` with the fleet fields (`DispatchRef`, status name, vehicle registration, driver, date).
      - Remove the next-day auto-create block from `ReconcileDailyRepAccountAsync` (the `if (nextSheet == null) { CreateRouteSheetAsync(...) }` section). Keep the `if (nextSheet != null)` balance/closing-stock carry-forward and its `SaveChangesAsync`.
      - `CreateRouteSheetAsync` still carries `PreviousAccountBalance` and `YesterdayClosingStock` from the most recent earlier sheet, so removing auto-create preserves carry-forward when the next sheet is eventually created.
    - *Dependencies*: Depends on Steps 5, 9.

11. **Available-delivery-dispatch query**
    - *Details*: `Manufacture.Web/Services/FleetDispatchService.cs` add `GetAvailableForSalesRouteAsync`: query `FleetDispatches.AsNoTracking()` with the same includes as `GetDispatchesAsync`, filtered by `Status == (int)FleetDispatchStatus.Planned` and `!_db.DailyRepRouteSheets.Any(s => s.FleetDispatchId == d.Id)`, ordered by `DispatchDate desc, Id desc`, then `MapDispatch`. Keep the result list small (this is a picker).
    - *Dependencies*: Depends on Step 9. Parallel with Step 10.

12. **Dispatch page model**
    - *Details*: `Manufacture.Web/Pages/Sales/Dispatch/Index.cshtml.cs`:
      - Inject `IFleetDispatchService`; add `public List<FleetDispatchDto> AvailableDispatches`.
      - `OnGetAsync`: `AvailableDispatches = await _fleetDispatchService.GetAvailableForSalesRouteAsync(cancellationToken);`
      - `OnPostCreateSheetAsync`: accept `int? fleetDispatchId` and pass it to `CreateRouteSheetAsync`.
      - Keep the existing SalesRep filter on `Reps` (SuperAdmin → all; SalesRep → self); no other role gets filtered.
    - *Dependencies*: Depends on Steps 9-11.

13. **Dispatch page markup**
    - *Details*: `Manufacture.Web/Pages/Sales/Dispatch/Index.cshtml`:
      - New Route Sheet dialog: reorder to **Sales Rep** → **Route Date** → **Delivery Dispatch** → **Supermarkets**.
        - Rep `<option>` text becomes `@rep.FullName` only (drop `(@rep.RouteName - @rep.VehicleRegistration)`).
        - Add `<select name="fleetDispatchId" required>` with `<option value="">-- Select Planned Delivery Dispatch --</option>`; each option: `@d.DispatchRef · @d.DispatchDate.ToString("yyyy-MM-dd") · @d.VehicleRegNumber — @d.Purpose`. When the list is empty, render a disabled option "No planned delivery dispatches available" plus a hint linking to `/Fleet/Dispatches/New`.
      - Sheet header: add a "Daily Dispatch" line showing the linked ref, status badge, vehicle and driver, linking to `/Logistics/Dispatches/Details` (`asp-route-id="@sheet.FleetDispatchId"`). For legacy sheets (`FleetDispatchId == null`) show "Not linked (legacy sheet)".
      - Add a short helper under the picker: "Only newly created (Planned) dispatches that are not yet linked appear here. The route vehicle is taken from this dispatch."
      - Do not add an editor for changing the link (out of scope).
    - *Dependencies*: Depends on Step 12.

### Phase 4 — Rules, tests, final validation

14. **Pure eligibility helper + tests**
    - *Details*: New `Manufacture.Library/Calculations/FleetDispatchRules.cs`:
      ```csharp
      public static bool IsAvailableForRouteSheet(FleetDispatchStatus status, bool alreadyLinked)
          => status == FleetDispatchStatus.Planned && !alreadyLinked;
      ```
      Use it in `GetAvailableForSalesRouteAsync` (or keep the DB filter and assert parity in tests). Add `Manufacture.Tests/FleetDispatchRulesTests.cs` covering Planned/unlinked = true and every other status or already-linked = false.
    - *Dependencies*: Depends on Step 9.

15. **Build, deploy, verify**
    - *Details*: Run the commands below, then walk the manual flows. Update this plan's status to Implemented with the verification record.
    - *Dependencies*: Depends on all previous steps.

## Relevant Files & Symbols

| File | Change |
|---|---|
| `Manufacture.Database/Tables/FleetDispatches.sql` | Purpose → NVARCHAR(200); drop OdometerStart; adjust checks |
| `Manufacture.Database/Tables/DailyRepRouteSheets.sql` | Add `FleetDispatchId`, FK, filtered unique index |
| `scripts/seed-database.sql` | Idempotent Purpose label conversion |
| `Manufacture.Database/README.md` | Fleet/route relationship + Purpose text note |
| `Manufacture.Library/Models/Entities/FleetDispatch.cs` | `Purpose` string, remove `OdometerStart`, inverse nav |
| `Manufacture.Library/Models/Entities/DailyRepRouteSheet.cs` | `FleetDispatchId` + nav |
| `Manufacture.Library/Data/ManufactureDbContext.cs` | Fleet + route-sheet mappings (~L358, ~L517) |
| `Manufacture.Library/Models/DTOs/FleetDtos.cs` | Remove `FleetDispatchPurpose`, `OdometerStart`; Purpose string |
| `Manufacture.Library/Models/Domain/RouteSheetEntities.cs` | Fleet dispatch summary fields on `DailyRepRouteSheet` |
| `Manufacture.Library/Interfaces/ISalesService.cs` | `CreateRouteSheetAsync` signature |
| `Manufacture.Library/Interfaces/IFleetDispatchService.cs` | `GetAvailableForSalesRouteAsync` |
| `Manufacture.Library/Calculations/FleetDispatchRules.cs` | **New** eligibility helper |
| `Manufacture.Web/Services/FleetDispatchService.cs` | Purpose mapping, odometer removal, available query |
| `Manufacture.Web/Services/SalesService.cs` | Required dispatch validation, vehicle copy, mapping, remove next-day auto-create |
| `Manufacture.Web/Pages/Logistics/Dispatches/Create.cshtml` + `.cs` | Layout, text Purpose, no odometer |
| `Manufacture.Web/Pages/Logistics/Dispatches/Details.cshtml` | Remove odometer item, 3-col grid |
| `Manufacture.Web/Pages/Logistics/Dispatches/Index.cshtml` | Truncated Purpose badge |
| `Manufacture.Web/Pages/Sales/Dispatch/Index.cshtml` + `.cs` | Rep name-only, required dispatch picker, linked dispatch display |
| `Manufacture.Tests/FleetDispatchRulesTests.cs` | **New** unit tests |

**Reuse, don't reinvent**
- `FleetDispatchService.GetDispatchesAsync` includes/`MapDispatch` for the availability query.
- `SalesService.CreateRouteSheetAsync` duplicate guard and `previousSheet` carry-forward logic.
- The required-field pattern already used by `DispatchInput.VehicleId` (`[Range]` in `FleetDispatchDto`).
- Dictionary/list form binding pattern in `OnPostCreateSheetAsync`.

## Verification Plan

1. **Build & tests**
   - `dotnet build Manufacture.slnx`
   - `dotnet test Manufacture.Tests/Manufacture.Tests.csproj`
   - `dotnet build Manufacture.Database/Manufacture.Database.sqlproj`
2. **Schema deploy**
   - `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/deploy-database.ps1`
   - SQL checks:
     - `FleetDispatches` has no `OdometerStart`; `Purpose` is `nvarchar(200)`.
     - `DailyRepRouteSheets` has `FleetDispatchId`, the FK and `UQ_DailyRepRouteSheets_FleetDispatch`.
     - Existing Purpose rows read `Production Delivery` / `Pre-Order Delivery` / `Internal Transfer` / `Other`, never `'0'..'3'`.
3. **Manual flows**
   - `/Fleet/Dispatches/New`: form shows Date + Purpose text in one row, Vehicle + Driver in the next, Notes full width; no Odometer Start. Save as planned with a free-text purpose (e.g. "Ikeja supermarket run"). Details shows the text and no odometer.
   - `/Sales/Dispatch` as SuperAdmin: rep dropdown lists names only. The delivery-dispatch dropdown lists only the newly created Planned dispatch, not completed/in-transit/cancelled ones.
   - Create a route sheet: it is refused if no delivery dispatch is chosen; on success the route sheet shows the dispatch ref/status/vehicle/driver and uses the dispatch's vehicle registration.
   - Open New Route Sheet again: the consumed dispatch no longer appears (one-to-one). Create a second Planned dispatch → it appears.
   - As `sales@bakery.com`: rep dropdown contains only that rep.
   - Reconcile a route sheet: no next-day sheet is auto-created; after creating tomorrow's sheet manually with a Planned dispatch, the previous balance and closing stock are carried into it.
   - Legacy route sheets render "Not linked (legacy sheet)" and still work.
4. **Regression checks**
   - Fleet Start/Complete/Cancel and stop-status updates still work.
   - Fuel logging still links to a fleet dispatch.
   - Supermarket debt panel and `/Production/Batches` changes from the previous plan are untouched.

## Key Decisions & Trade-offs

| Topic | Decision | Why |
|---|---|---|
| Purpose type | Free text `NVARCHAR(200)` | Matches the user's request and removes the enum bottleneck; labels keep old rows readable |
| Odometer Start | Dropped everywhere | Explicitly requested "form, table and any other page"; data is intentionally discarded |
| Link direction | Route sheet stores `FleetDispatchId` | Sales route is the dependent record; fleet dispatch stays a standalone logistics record |
| Eligibility | Exactly `Planned` and unlinked | "It's just created" — guarantees the vehicle has not left yet |
| Cardinality | One-to-one + filtered unique index | Prevents two reps claiming the same physical delivery movement |
| Required link | Enforced on create | A daily route must ride an existing delivery dispatch; vehicle comes from it |
| Rep dropdown | Names only, role-scoped | Removes vehicle noise; SuperAdmin sees all reps, reps see themselves |
| Next-day auto-create | Removed | Cannot auto-create without a Planned dispatch; carry-forward still happens in the explicit create path |
| Legacy sheets | `FleetDispatchId` stays NULL | No backfill; they remain viewable and closeable |

## Implementation Record

Completed on branch `development`, 2026-11-18.

- **Schema deployed** to LocalDB `ManufactureDb`:
  - `FleetDispatches.Purpose` is now `NVARCHAR(200) NOT NULL`; `OdometerStart` is gone; `CK_FleetDispatches_Purpose` is gone; `CK_FleetDispatches_Odometer` covers only `OdometerEnd`.
  - `DailyRepRouteSheets.FleetDispatchId INT NULL` added with `FK_DailyRepRouteSheets_FleetDispatches` and filtered unique index `UQ_DailyRepRouteSheets_FleetDispatch`.
  - Purpose label conversion block appended to `scripts/seed-database.sql` (no existing rows needed conversion).
- **Build**: `dotnet build Manufacture.slnx` — 0 warnings, 0 errors.
- **Tests**: `dotnet test` — 270 passed, 0 failed (7 new `FleetDispatchRules` tests).
- **HTTP smoke test** against the running app:
  - `/Fleet/Dispatches/New` renders Purpose as a text input and has no Odometer Start anywhere; created `DSP-2026-001` with purpose "Ikeja supermarket run" as `Planned`.
  - Fleet Details shows the free-text purpose and no odometer.
  - `/Sales/Dispatch` offered only that Planned dispatch and rendered rep names without route/vehicle text.
  - Creating a route sheet with `fleetDispatchId` stored `FleetDispatchId = 2002`, `VehicleId = 1`, `VehicleRegistration = KJA-892-XA` (from the dispatch), and the sheet header rendered the Daily Dispatch line.
  - The consumed dispatch disappeared from the picker.
  - Signed in as `sales@bakery.com`: the rep dropdown contained exactly one option (self) and hid the other reps.
  - Negative cases: posting without a dispatch returned "Select a delivery dispatch for this route sheet."; re-using `DSP-2026-001` returned "That delivery dispatch is already linked to another route sheet."
  - Cleanup: test route sheet and dispatch deleted (0 leftovers).
- The user-owned running app instance was stopped for the build/deploy and restarted afterwards with the new code.
- `doc/random_prompt.md` remains a pre-existing local modification and was left untouched.
