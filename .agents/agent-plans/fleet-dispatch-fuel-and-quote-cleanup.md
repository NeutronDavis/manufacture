# Plan: Fleet Dispatch, Fuel Linking & Quote Form Cleanup

**Status**: Draft

## Summary

Implement four related fleet changes:

1. Remove the **Distance (km)** field from the client quote form.
2. Remove the accidental **kobo/0.01 validation** on **Quoted Leg Amount (₦)**.
3. Add a **fuel record list and Record Fuel action** to `/Fleet/Charter/Details`.
4. Add a new **Fleet Delivery Dispatch** module for non-charter production/PreOrder deliveries, with multiple free-text delivery points, vehicle status tracking, dispatch details, and linked fuel records.

Fuel will become a unified fleet ledger linked to either:

- a **Charter Booking**, or
- a **Fleet Delivery Dispatch**.

General vehicle fuel logs remain valid without either link.

## Decisions

- Delivery dispatch is a **new Fleet module**, not an extension of `/Sales/Dispatch`.
- Delivery points are **free-text only**: name, address, city/state, contact name and phone.
- Vehicle status:
  - Add `InTransit` to `Vehicles.Status`.
  - Update it automatically when a dispatch starts and ends.
  - Also allow a manual vehicle status override on the dispatch details page.
- Fuel linkage:
  - Add nullable `CharterBookingId` and `FleetDispatchId` to `FuelLogs`.
  - A fuel record may link to a charter, a dispatch, or neither.
  - A fuel record must not link to both a charter and a dispatch.
- Quote distance:
  - Remove the field from the quote forms.
  - Keep the database column for legacy quotes.
  - New quotes store `0` when no distance is captured.
- Quoted leg amount:
  - Allow whole-naira values such as `20000` without forcing `.01`.
  - Allow zero per leg, but the total quote must still be greater than zero.

## Current State

- `/Fleet/Quotes/New` maps to `Manufacture.Web/Pages/Logistics/Rates/Create.cshtml`.
- The quote form currently shows `Distance (km)` and `Quoted Leg Amount (₦)`.
- `CharterQuoteLegDto.QuotedLegAmount` has `[Range(0.01, 100_000_000)]`.
- The amount input uses a `min="0.01"` / `step="1000"` combination, causing values such as `20000` to be treated as a step mismatch requiring `20000.01`.
- `/Fleet/Charter/Details` has no fuel section.
- `FuelLogs` has `VehicleId` only, with no charter or dispatch link.
- `/Sales/Dispatch` is the sales rep route-sheet loading and sign-off page. It is not a general production/PreOrder delivery dispatch.
- `Vehicles.Status` currently allows only `Active`, `InMaintenance`, and `Grounded`.
- There is no `FleetDispatches` or delivery-stop table.
- Fuel reporting is currently vehicle/date based only.

## Schema Changes

### 1. `FuelLogs` — add links

Add:

- `CharterBookingId INT NULL` FK -> `CharterBookings`.
- `FleetDispatchId INT NULL` FK -> `FleetDispatches`.
- Indexes on both nullable columns.
- Check constraint: `CharterBookingId IS NULL OR FleetDispatchId IS NULL` so one fuel record cannot belong to both.

Existing rows remain valid with both values `NULL`.

### 2. New `FleetDispatches` table

Suggested columns:

| Column | Type | Notes |
|---|---|---|
| `Id` | INT IDENTITY | PK |
| `DispatchRef` | NVARCHAR(40) | Unique, e.g. `DSP-2026-001` |
| `DispatchDate` | DATE | Required |
| `VehicleId` | INT | Required FK `Vehicles` |
| `DriverEmployeeId` | INT NULL | FK `Employees` |
| `DriverName` | NVARCHAR(150) | Snapshot text |
| `Purpose` | INT | 0 Production Delivery, 1 PreOrder Delivery, 2 Internal Transfer, 3 Other |
| `Status` | INT | 0 Planned, 1 Loading, 2 InTransit, 3 Delivered, 4 Completed, 5 Cancelled |
| `OdometerStart` | DECIMAL(18,2) NULL | Optional |
| `OdometerEnd` | DECIMAL(18,2) NULL | Optional |
| `Notes` | NVARCHAR(1000) | Default empty |
| `CreatedByUserId` | INT NULL | FK `Users` |
| `CreatedAt` / `UpdatedAt` | DATETIME2(3) | Audit |
| `RowVersion` | ROWVERSION | Concurrency |

Constraints:

- Unique `DispatchRef`.
- Check `Status IN (0,1,2,3,4,5)`.
- Check odometer values `>= 0`.
- Index `(DispatchDate DESC, Status ASC)`.
- Index `VehicleId, DispatchDate`.

### 3. New `FleetDispatchStops` table

Suggested columns:

| Column | Type | Notes |
|---|---|---|
| `Id` | INT IDENTITY | PK |
| `FleetDispatchId` | INT | FK, cascade delete |
| `StopNumber` | INT | > 0 |
| `DeliveryPointName` | NVARCHAR(150) | Free-text stop name |
| `Address` | NVARCHAR(300) | Free-text address |
| `CityState` | NVARCHAR(120) | Free-text city/state |
| `ContactName` | NVARCHAR(150) NULL | |
| `ContactPhone` | NVARCHAR(30) NULL | |
| `Status` | INT | 0 Pending, 1 Delivered, 2 Failed |
| `DeliveredAt` | DATETIME2(3) NULL | |
| `Notes` | NVARCHAR(500) NULL | |
| `CreatedAt` / `UpdatedAt` | DATETIME2(3) | Audit |

Constraints:

- Unique `(FleetDispatchId, StopNumber)`.
- Check `StopNumber > 0` and `Status IN (0,1,2)`.
- Index `(FleetDispatchId, StopNumber)`.

### 4. `Vehicles.Status`

Add `InTransit` to the allowed status values:

```text
Active, InMaintenance, Grounded, InTransit
```

Update the SQL check constraint and the vehicle create/edit dropdowns to include `InTransit`.

## Quote Form Changes

### `/Fleet/Quotes/New`

Remove from the **Multi-Stop Route & Manual Pricing** section:

- `Distance (km)` label.
- Distance input.
- Distance from the per-leg summary display.
- Distance from the quote summary and total distance display when no distance is captured.

Keep:

- Origin address.
- Origin city/state.
- Destination address.
- Destination city/state.
- Fuel option.
- Quoted leg amount.
- Leg notes.

The database column remains nullable/legacy, but new quote legs store `0` distance.

### Quoted Leg Amount fix

Replace the current validation and input constraints:

- DTO: change `[Range(0.01, 100_000_000)]` to allow zero and whole-naira values.
- Server validation:
  - A leg amount below zero is invalid.
  - A leg amount of zero is allowed.
  - The total quoted amount must remain greater than zero.
- HTML:
  - `min="0"`
  - `step="any"` or `step="1"`
  - No `0.01` minimum and no step that requires `20000.01`.
- Ensure `20000` submits and saves without a kobo adjustment.

Apply the same amount fix on the quote edit form.

## Charter Fuel Records

### `/Fleet/Charter/Details`

Add a **Fuel Records** section:

- List linked `FuelLogs` for the charter’s vehicle where `CharterBookingId = booking.Id`.
- Columns:
  - Date
  - Station
  - Fuel type
  - Litres
  - Price per litre
  - Total cost
  - Recorded by
- Add **Record Fuel** button linking to:
  `/Fleet/FuelLogs/New?charterBookingId={id}&returnUrl=/Fleet/Charter/Details/{id}`.
- Show total linked fuel cost for the charter.

### `/Fleet/FuelLogs/New`

Support query parameters:

- `charterBookingId`
- `fleetDispatchId`
- `returnUrl`

Behavior:

- If `charterBookingId` is supplied:
  - Load the charter booking.
  - Prefill and lock the vehicle to the charter vehicle.
  - Prefill driver and route description where available.
  - Save the fuel log with `CharterBookingId`.
  - Return to the charter details page.
- If `fleetDispatchId` is supplied:
  - Load the dispatch.
  - Prefill and lock the vehicle to the dispatch vehicle.
  - Prefill driver and route description where available.
  - Save the fuel log with `FleetDispatchId`.
  - Return to the dispatch details page.
- Normal fuel logs remain unchanged with both links `NULL`.

## Fleet Delivery Dispatch Module

### Routes and navigation

Add under Fleet:

- `/Fleet/Dispatches` — dispatch list.
- `/Fleet/Dispatches/New` — create dispatch.
- `/Fleet/Dispatches/Details/{id}` — dispatch details.

Add a sidebar item **Delivery Dispatches** under Fleet.

### Dispatch list

Show:

- Dispatch reference.
- Date.
- Vehicle registration.
- Driver.
- Purpose.
- Number of stops.
- Delivered / pending / failed stop counts.
- Status badge.
- Linked fuel total.
- Actions: View, Start/Continue, Complete, Cancel.

Filters:

- Status.
- Date range.
- Vehicle.
- Driver.
- Search by dispatch ref, vehicle, driver or delivery point.

KPIs:

- Planned dispatches.
- In transit.
- Completed.
- Total fuel cost for displayed dispatches.

### Create dispatch

Fields:

- Dispatch date.
- Vehicle.
- Driver dropdown from Logistics staff.
- Selecting a vehicle auto-selects its assigned driver; another logistics driver can be selected manually.
- Purpose.
- Odometer start.
- Notes.
- Dynamic multiple delivery points:
  - Delivery point name.
  - Address.
  - City/state.
  - Contact name.
  - Contact phone.
  - Notes.

Actions:

- Save as Planned.
- Save and start dispatch.

Validation:

- At least one delivery point is required.
- Delivery point name and address are required.
- Vehicle is required.
- Vehicle must not be `InMaintenance` or `Grounded` when starting a dispatch.

### Dispatch details

Show:

- Dispatch reference and status.
- Vehicle and driver.
- Purpose and dates.
- Odometer start/end.
- Delivery points with status controls:
  - Pending.
  - Delivered.
  - Failed.
- Stop delivered timestamp.
- Dispatch notes.
- Vehicle status panel:
  - Current vehicle status.
  - Manual status selector: Active, InMaintenance, Grounded, InTransit.
  - Update button.
- Fuel records section:
  - List fuel logs linked with `FleetDispatchId`.
  - Record Fuel button.
  - Total linked fuel cost.
- Status actions:
  - Start Dispatch -> sets dispatch InTransit and vehicle InTransit.
  - Mark Delivered -> dispatch Delivered.
  - Complete -> dispatch Completed and vehicle Active if it was InTransit.
  - Cancel -> dispatch Cancelled and vehicle Active if it was InTransit.

### Vehicle status rules

- Starting a dispatch sets `Vehicles.Status = 'InTransit'`.
- Completing or cancelling a dispatch sets the vehicle back to `Active` only when its current status is `InTransit`.
- Manual status updates are allowed from dispatch details.
- A vehicle in `InMaintenance` or `Grounded` cannot be started on a dispatch until manually changed to `Active`.
- The existing Vehicles page must show and allow the new `InTransit` status.

## Service Changes

### `FuelLogDto`

Add:

- `CharterBookingId`
- `CharterBookingRef`
- `FleetDispatchId`
- `FleetDispatchRef`

### `IFleetService` / `FleetService`

Add:

- `GetFuelLogsForCharterAsync(int charterBookingId, ...)`
- `GetFuelLogsForDispatchAsync(int fleetDispatchId, ...)`
- `CreateFuelLogForCharterAsync(...)` or extend `CreateFuelLogAsync` with validation context.
- `CreateFuelLogForDispatchAsync(...)` or extend `CreateFuelLogAsync` with validation context.

Validation:

- If `CharterBookingId` is supplied, the selected vehicle must equal the charter vehicle.
- If `FleetDispatchId` is supplied, the selected vehicle must equal the dispatch vehicle.
- A fuel log cannot carry both links.
- The vehicle and fuel values must still pass existing fuel-log validation.

### New `IFleetDispatchService` / `FleetDispatchService`

Add:

- `GetFleetDispatchesAsync(status, search, fromDate, toDate, vehicleId, driverId, ...)`
- `GetFleetDispatchAsync(id, ...)`
- `CreateFleetDispatchAsync(dto, ...)`
- `UpdateFleetDispatchAsync(dto, ...)`
- `StartFleetDispatchAsync(id, ...)`
- `UpdateDispatchStopStatusAsync(stopId, status, ...)`
- `CompleteFleetDispatchAsync(id, ...)`
- `CancelFleetDispatchAsync(id, ...)`
- `UpdateVehicleStatusAsync(vehicleId, status, ...)`
- `GetFuelLogsForDispatchAsync(...)`

Dispatch status transitions:

```text
Planned -> Loading -> InTransit -> Delivered -> Completed
       \-> Cancelled at any active stage
```

Vehicle updates:

- Start -> `InTransit`.
- Complete/Cancel -> `Active` only if currently `InTransit`.
- Manual override is always available subject to Fleet edit permission.

## Relevant Files

### Database

- `Manufacture.Database/Tables/FuelLogs.sql`
- `Manufacture.Database/Tables/Vehicles.sql`
- `Manufacture.Database/Tables/FleetDispatches.sql` — new
- `Manufacture.Database/Tables/FleetDispatchStops.sql` — new
- `Manufacture.Database/Manufacture.Database.sqlproj`
- `scripts/seed-database.sql` — optional sample dispatch and linked fuel records

### Library

- `Manufacture.Library/Data/ManufactureDbContext.cs`
- `Manufacture.Library/Models/Entities/FuelLog.cs`
- `Manufacture.Library/Models/Entities/FleetDispatch.cs` — new
- `Manufacture.Library/Models/Entities/FleetDispatchStop.cs` — new
- `Manufacture.Library/Models/DTOs/FleetDtos.cs`
- `Manufacture.Library/Interfaces/IFleetService.cs`
- `Manufacture.Library/Interfaces/IFleetDispatchService.cs` — new

### Web services

- `Manufacture.Web/Services/FleetService.cs`
- `Manufacture.Web/Services/FleetDispatchService.cs` — new
- `Manufacture.Web/Program.cs` — register service and routes

### Quote pages

- `Manufacture.Web/Pages/Logistics/Rates/Create.cshtml`
- `Manufacture.Web/Pages/Logistics/Rates/Edit.cshtml`
- `Manufacture.Library/Models/DTOs/FleetDtos.cs`
- `Manufacture.Web/Services/CharterQuoteCalculator.cs`

### Charter and fuel pages

- `Manufacture.Web/Pages/Logistics/Charter/Details.cshtml`
- `Manufacture.Web/Pages/Logistics/Charter/Details.cshtml.cs`
- `Manufacture.Web/Pages/Logistics/FuelLogs/Create.cshtml`
- `Manufacture.Web/Pages/Logistics/FuelLogs/Create.cshtml.cs`
- `Manufacture.Web/Pages/Logistics/FuelLogs/Index.cshtml`
- `Manufacture.Web/Pages/Logistics/FuelLogs/Index.cshtml.cs`

### Dispatch pages

- `Manufacture.Web/Pages/Logistics/Dispatches/Index.cshtml` + `.cs` — new
- `Manufacture.Web/Pages/Logistics/Dispatches/Create.cshtml` + `.cs` — new
- `Manufacture.Web/Pages/Logistics/Dispatches/Details.cshtml` + `.cs` — new
- `Manufacture.Web/Pages/Shared/_Layout.cshtml`
- `Manufacture.Web/Program.cs`

### Tests

- `Manufacture.Tests/CharterQuoteCalculatorTests.cs`
- New `FleetDispatchServiceTests` or pure dispatch-status tests
- Fuel-link validation tests

## Implementation Steps

### Phase 1: Quote cleanup

1. Remove the Distance field from the New Quote form.
2. Remove the Distance field from the Edit Quote form.
3. Remove distance from the quote summary/total display where it would show `0`.
4. Fix Quoted Leg Amount DTO validation, server validation and HTML `min`/`step`.
5. Verify `20000` saves without becoming `20000.01`.

### Phase 2: Fuel schema and charter fuel

6. Add `CharterBookingId` and `FleetDispatchId` to `FuelLogs`.
7. Add FKs, indexes and the one-link-only check constraint.
8. Add `CharterBookingId` to `FuelLogDto` and mapping.
9. Add fuel list and Record Fuel action to Charter Details.
10. Extend Fuel Logs New page with charter query parameter, prefilled/locked vehicle and return URL.
11. Enforce charter-vehicle matching in the service.

### Phase 3: Dispatch schema and service

12. Add `FleetDispatches` and `FleetDispatchStops`.
13. Add `InTransit` to the Vehicles status constraint and UI.
14. Add dispatch entities, DTOs, DbSets and service methods.
15. Implement dispatch status transitions and vehicle status updates.
16. Implement stop status updates and free-text multi-stop validation.

### Phase 4: Dispatch pages

17. Build Dispatch List page with filters and KPIs.
18. Build Create Dispatch page with dynamic delivery points.
19. Build Dispatch Details page with stops, vehicle status and fuel records.
20. Add Record Fuel from dispatch to Fuel Logs New.
21. Add Fleet navigation and route aliases.

### Phase 5: Verification

22. Build and apply the database project.
23. Run the full test suite.
24. Create a test dispatch with multiple stops, start it, complete it and verify vehicle status changes.
25. Record fuel against the dispatch and charter and verify each list only shows its own records.
26. Verify quote amount `20000` saves exactly as `20000`.

## Verification

### Automated

```powershell
dotnet build Manufacture.slnx -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\
dotnet test Manufacture.Tests/Manufacture.Tests.csproj --no-build -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\
```

Add tests for:

- Leg amount `20000` accepted without kobo adjustment.
- Zero leg amount allowed; zero total quote rejected.
- Distance omitted and saved as `0`.
- Fuel log linked to charter validates the charter vehicle.
- Fuel log linked to dispatch validates the dispatch vehicle.
- Fuel log cannot link to both charter and dispatch.
- Dispatch start sets vehicle to `InTransit`.
- Dispatch completion/cancel restores `Active` only from `InTransit`.
- Manual vehicle status override works.
- Dispatch stop status transitions work.

### Manual

1. Open `/Fleet/Quotes/New`.
2. Confirm no Distance field appears.
3. Enter a quoted leg amount of `20000`; confirm it saves as `20000`, not `20000.01`.
4. Create a charter booking and open `/Fleet/Charter/Details/{id}`.
5. Confirm Fuel Records section and Record Fuel button appear.
6. Record fuel and confirm it appears only under that charter.
7. Create a fleet dispatch with three free-text delivery points.
8. Start the dispatch; confirm the vehicle becomes `InTransit`.
9. Mark stops Delivered/Failed.
10. Complete the dispatch; confirm the vehicle returns to `Active`.
11. Manually set the vehicle status from the dispatch details page.
12. Record fuel against the dispatch; confirm it appears only under that dispatch.
13. Confirm `/Sales/Dispatch` remains unchanged.

## Non-Goals

- No map/geocoding or automatic route distance.
- No automatic link between dispatch stops and PreOrder orders in this phase; stops are free-text.
- No replacement of the existing `/Sales/Dispatch` route-sheet workflow.
- No separate fuel tables for charter and dispatch; use the unified `FuelLogs` ledger.
- No customer-facing dispatch tracking page.
