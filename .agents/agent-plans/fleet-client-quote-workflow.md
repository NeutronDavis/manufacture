# Plan: Fleet Client-Request Quote Workflow

**Status**: Done

## Summary

Replace the state/city **Route Rates** pricing matrix with a **client-specific quote request** workflow for all commercial fleet and VIP bookings.

The current `/Fleet/Rates/New` page creates a fixed state-to-city rate lane, and `/Fleet/Charter/New` automatically prices every trip leg from that matrix. The client has rejected generalized VIP pricing and state/city pricing because routes within a region do not share a uniform distance.

The new source of price will be a manually prepared multi-stop quote:

- Pickup and drop-off addresses, not only states/cities.
- Distance per leg.
- Manual quoted amount per leg.
- Incidentals and net revenue.
- Manager/SuperAdmin approval before conversion to a booking.

Internal sales-distribution route sheets are **not** changing. They continue to use `RouteSheetItems.UnitSellingPrice`.

## Decisions

- **Pricing scope**: All commercial fleet and VIP pricing becomes client-request based.
- **Internal distribution**: Leave route-sheet pricing untouched.
- **Quote method**: Manual quote entry only. No per-km or per-day price formula.
- **Route detail**: Multi-stop addresses with distance and amount per leg.
- **`/Fleet/Rates/New`**: Remove state/city route-rate creation and replace it with the client quote request form.
- **Approval**: Manager or SuperAdmin approval is required before a quote can become a booking.
- **Legacy RouteRates**: Drop the `RouteRates` table and seeded lane data.
- **VIP**: Keep `VIP Escort Patrol` as a vehicle class option in quotes. It will have no pre-approved fixed price.
- **Maps**: No map/geocoding integration in this phase. Distance is entered manually by staff.

## Current State

- `/Fleet/Rates` is the Route Pricing Matrix.
- `/Fleet/Rates/New` creates `RouteRates` rows with `OriginState`, `DestinationState`, `VehicleTypeRequired`, `RateWithFuel`, `RateWithoutFuel`, and `EstimatedHours`.
- `/Fleet/Charter/New` embeds active route rates as JSON and automatically prices every leg.
- `FleetService.CreateCharterBookingAsync` re-checks `RouteRates` and rejects a leg when no active rate exists.
- `CharterBookings` and `TripLegs` store state names and leg cost only.
- `TripLegs` has no addresses or distance.
- `CharterBookings` has no quote reference or approval link.
- `VIP Escort Patrol` appears in the rate vehicle-class list, but no VIP-specific pricing rule exists.
- `RouteRates` is used only by the commercial charter/rate pages, not by internal route sheets.
- Fleet RBAC currently grants full access to `SuperAdmin`; `ProductionManager` has a Fleet row but page/route restrictions block Fleet. Approval will use `RbacCache.CanApprove(role, "Fleet")` with `SuperAdmin` as the initial approver.

## Target Workflow

1. **Staff creates a quote** at `/Fleet/Rates/New`.
   - Client name, phone, optional email.
   - Vehicle class and optional vehicle assignment.
   - Request date and quote validity date.
   - One or more legs:
     - Origin address.
     - Origin city/state label.
     - Destination address.
     - Destination city/state label.
     - Distance in km.
     - Fuel option.
     - Manual quoted amount for the leg.
   - Driver allowance, security/escort fee, tolls and road levies.
   - Notes.
   - System totals the leg amounts, total distance, incidentals and estimated net.
2. **Staff saves a draft or submits for approval.**
3. **Approver reviews the quote** at `/Fleet/Rates/Edit/{id}`.
   - Approve or reject with a reason.
   - Approved quotes become read-only.
   - Rejected quotes return to editable draft state.
4. **Approved quote is converted to a booking** at `/Fleet/Charter/New?quoteId={id}`.
   - Customer, route, addresses, distances, fuel terms, quoted amounts and incidentals are copied from the approved quote.
   - Staff assigns vehicle, driver, departure date and return date.
   - One approved quote can create only one booking.
5. **Booking details** show the quote reference, approved by/at, actual addresses and distances.
6. **Quote expiry**: an approved or pending quote past `ValidUntil` cannot be converted and is shown as `Expired`.

### Quote lifecycle

```text
Draft -> PendingApproval -> Approved -> Converted
   ^            |
   |            v
   +-------- Rejected
Approved/Pending + valid date passed -> Expired
```

### Pricing rules

- `QuotedAmount = SUM(CharterQuoteLegs.QuotedLegAmount)`
- `TotalDistanceKm = SUM(CharterQuoteLegs.DistanceKm)`
- `TotalTripIncidentals = DriverAllowance + SecurityEscortFee + TollsAndRoadLevies`
- `NetEstimate = QuotedAmount - TotalTripIncidentals`
- A quote must have at least one leg.
- Every leg must have an origin address, destination address and distance greater than zero.
- Every leg must have a quoted amount greater than zero.
- A quote cannot be submitted or approved if its total or net is invalid.
- No state/city rate lookup, no distance-based formula and no VIP fixed tariff are used.

## Schema Changes

Database-first project: add SQL table files under `Manufacture.Database/Tables`, build the SQL project, then update the entities and `ManufactureDbContext`.

### 1. Add `CharterQuoteRequests`

Suggested columns:

| Column | Type | Notes |
|---|---|---|
| `Id` | INT IDENTITY | PK |
| `QuoteRef` | NVARCHAR(40) | Unique, generated like `QTE-2026-001` |
| `CustomerName` | NVARCHAR(150) | Required |
| `CustomerPhone` | NVARCHAR(30) | Required |
| `CustomerEmail` | NVARCHAR(200) | Optional |
| `RequestDate` | DATETIME2(3) | Default `SYSUTCDATETIME()` |
| `TripType` | INT | 0 OneWay, 1 Return, 2 MultiStop |
| `VehicleTypeRequired` | NVARCHAR(60) | Required; includes VIP Escort Patrol |
| `VehicleId` | INT NULL | Optional preferred vehicle |
| `TotalDistanceKm` | DECIMAL(18,2) | Sum of quote legs |
| `QuotedAmount` | DECIMAL(18,2) | Sum of quote legs |
| `DriverAllowance` | DECIMAL(18,2) | Default 0 |
| `SecurityEscortFee` | DECIMAL(18,2) | Default 0 |
| `TollsAndRoadLevies` | DECIMAL(18,2) | Default 0 |
| `TotalTripIncidentals` | DECIMAL(18,2) | Persisted computed column |
| `NetEstimate` | DECIMAL(18,2) | Persisted computed column |
| `Status` | INT | 0 Draft, 1 PendingApproval, 2 Approved, 3 Rejected, 4 Expired, 5 Converted |
| `ValidUntil` | DATE NULL | Approval validity |
| `Notes` | NVARCHAR(1000) | Default `''` |
| `RequestedByUserId` | INT NULL | FK `Users` |
| `SubmittedAt` | DATETIME2(3) NULL | |
| `ApprovedByUserId` | INT NULL | FK `Users` |
| `ApprovedAt` | DATETIME2(3) NULL | |
| `RejectionReason` | NVARCHAR(500) NULL | |
| `CreatedAt` / `UpdatedAt` | DATETIME2(3) | Audit |
| `RowVersion` | ROWVERSION | Concurrency |

Constraints and indexes:

- Unique `QuoteRef`.
- FK `VehicleId` -> `Vehicles`.
- FKs `RequestedByUserId`, `ApprovedByUserId` -> `Users`.
- Check `Status IN (0,1,2,3,4,5)`.
- Check amounts and distance `>= 0`.
- Index `(Status, RequestDate DESC)`.
- No `ConvertedBookingId` column. The booking points to the quote, avoiding a circular FK.

### 2. Add `CharterQuoteLegs`

| Column | Type | Notes |
|---|---|---|
| `Id` | INT IDENTITY | PK |
| `CharterQuoteRequestId` | INT | FK, cascade delete |
| `LegNumber` | INT | > 0 |
| `OriginAddress` | NVARCHAR(300) | Required |
| `OriginCityState` | NVARCHAR(120) | Required display label |
| `DestinationAddress` | NVARCHAR(300) | Required |
| `DestinationCityState` | NVARCHAR(120) | Required display label |
| `DistanceKm` | DECIMAL(18,2) | Required, > 0 |
| `FuelOption` | INT | 0 WithFuel, 1 WithoutFuel |
| `QuotedLegAmount` | DECIMAL(18,2) | Manual amount |
| `Notes` | NVARCHAR(300) | Optional |
| `CreatedAt` / `UpdatedAt` | DATETIME2(3) | Audit |

Constraints and indexes:

- Unique `(CharterQuoteRequestId, LegNumber)`.
- FK `CharterQuoteRequestId` -> `CharterQuoteRequests` with cascade delete.
- Check `LegNumber > 0`, `DistanceKm > 0`, `QuotedLegAmount > 0`, `FuelOption IN (0,1)`.

### 3. Extend `CharterBookings`

Add:

- `QuoteRequestId INT NULL` FK -> `CharterQuoteRequests`.
- Unique filtered index on `QuoteRequestId` where `QuoteRequestId IS NOT NULL` to prevent one quote creating multiple bookings.

Existing bookings keep `QuoteRequestId = NULL` and remain valid as legacy bookings.

### 4. Extend `TripLegs`

Add:

- `OriginAddress NVARCHAR(300) NULL`
- `DestinationAddress NVARCHAR(300) NULL`
- `DistanceKm DECIMAL(18,2) NULL`

Keep existing `OriginState` and `DestinationState` columns as display labels for backward compatibility. New bookings copy both the address and city/state label from the quote.

### 5. Drop `RouteRates`

- Delete `Manufacture.Database/Tables/RouteRates.sql`.
- Remove the `RouteRates` seed block from `scripts/seed-database.sql`.
- Remove `DbSet<RouteRate>` from `ManufactureDbContext`.
- Delete `Manufacture.Library/Models/Entities/RouteRate.cs`.
- Delete `RouteRateDto` from `Manufacture.Library/Models/DTOs/FleetDtos.cs`.
- Remove all RouteRate methods from `IFleetService` and `FleetService`.
- Remove RouteRate mock tests and any route-rate UI references.
- If historical rate data is needed for audit, export it before applying the database project. It will not remain in the application schema.

## DTO and Service Changes

### New DTOs and enums

Add to `Manufacture.Library/Models/DTOs/FleetDtos.cs`:

- `CharterQuoteStatus` enum: `Draft`, `PendingApproval`, `Approved`, `Rejected`, `Expired`, `Converted`.
- `CharterQuoteTripType` enum: `OneWay`, `Return`, `MultiStop`.
- `CharterQuoteDto`:
  - Identity and client fields.
  - `VehicleTypeRequired`, `VehicleId`.
  - `Status`, `ValidUntil`, approval fields.
  - `Legs`, `TotalDistanceKm`, `QuotedAmount`, incidentals and `NetEstimate`.
  - `RequestedByUserId`, `ApprovedByUserId`.
- `CharterQuoteLegDto`:
  - Leg number, addresses, city/state labels, distance, fuel option, quoted amount, notes.
- Extend `CharterBookingDto`:
  - `QuoteRequestId`
  - `QuoteRef`
  - `TotalDistanceKm`
- Extend `TripLegDto`:
  - `OriginAddress`
  - `DestinationAddress`
  - `DistanceKm`

Remove `RouteRateDto`.

### New service

Add `ICharterQuoteService` and `CharterQuoteService`, or extend `IFleetService` if a new service is not preferred. A separate service is recommended because quote approval and conversion are distinct from vehicle/fuel/maintenance management.

Required operations:

- `GetCharterQuotesAsync(status, search, cancellationToken)`
- `GetCharterQuoteAsync(id, cancellationToken)`
- `GetApprovedCharterQuotesAsync(cancellationToken)` — only `Approved`, not expired.
- `CreateCharterQuoteAsync(dto, requestedByUserId, cancellationToken)`
- `UpdateCharterQuoteAsync(dto, cancellationToken)` — allowed only for `Draft` and `Rejected`.
- `SubmitCharterQuoteForApprovalAsync(id, cancellationToken)`
- `ApproveCharterQuoteAsync(id, approvedByUserId, cancellationToken)`
- `RejectCharterQuoteAsync(id, reason, approvedByUserId, cancellationToken)`
- `CloneCharterQuoteAsync(id, requestedByUserId, cancellationToken)` — optional but useful for approved-quote revisions.
- `GetQuoteTotals`/validation helper.

Add `CharterQuoteCalculator` as a pure helper so quote totals and validation can be unit tested without a database:

- Sum leg amounts.
- Sum distance.
- Compute incidentals and net.
- Validate required legs, addresses, distance and amount.
- Determine if a quote is convertible.

### Update `CreateCharterBookingAsync`

Replace the RouteRates lookup loop with quote conversion:

1. Require `dto.QuoteRequestId`.
2. Load the quote with legs.
3. Require `Status == Approved`.
4. Reject if `ValidUntil` is before today.
5. Require the quote total to equal the sum of its leg amounts.
6. Copy customer details, legs, addresses, distances, fuel options, leg amounts, incidentals and totals into the booking.
7. Set `CharterBookings.QuoteRequestId`.
8. Create booking legs with `LegCost = QuotedLegAmount`.
9. Set the quote status to `Converted`.
10. Use a database transaction so the booking and quote status update succeed together.
11. Reject a second conversion of the same quote.

### Approval rules

- Approval requires `ActiveRole == "SuperAdmin"` or `RbacCache.CanApprove(role, "Fleet")`.
- A non-SuperAdmin user cannot approve a quote they requested themselves.
- Only `PendingApproval` quotes can be approved or rejected.
- Only `Draft` and `Rejected` quotes can be edited.
- Approved quotes are read-only. To change an approved quote, clone it into a new draft.
- Record approval/rejection and conversion events in `AuditLogs` with entity name `CharterQuoteRequest`.

## Page and Route Changes

### `/Fleet/Rates` — become “Client Quotes”

Replace `Pages/Logistics/Rates/Index.cshtml` and `.cs` with a quote register:

- KPIs: Draft, Pending Approval, Approved, Rejected, Converted, total quoted value.
- Status tabs and search by quote ref, customer, phone, vehicle class and route.
- “New Client Quote” button.
- Row actions:
  - Draft/Rejected -> Edit.
  - PendingApproval -> Review.
  - Approved -> Book Charter.
  - Converted -> View booking.
- Show approval state and expiry.

### `/Fleet/Rates/New` — become “New Client Quote”

Replace `Pages/Logistics/Rates/Create.cshtml` and `.cs`:

- Client details.
- Vehicle class dropdown:
  - `30-Ton Haulage Truck`
  - `15-Ton Box Truck`
  - `Delivery Van`
  - `VIP Escort Patrol`
- Optional preferred vehicle.
- Trip type.
- Quote validity date.
- Dynamic multi-stop legs:
  - Origin address.
  - Origin city/state label.
  - Destination address.
  - Destination city/state label.
  - Distance km.
  - Fuel option.
  - Manual quoted amount.
  - Notes.
- Totals panel:
  - Total distance.
  - Total quoted amount.
  - Incidentals.
  - Estimated net.
- Actions:
  - Save Draft.
  - Submit for Approval.
- Remove all state/city rate, with-fuel/without-fuel rate, and fuel-allowance logic.

### `/Fleet/Rates/Edit/{id}` — become “Review / Edit Client Quote”

Replace `Pages/Logistics/Rates/Edit.cshtml` and `.cs`:

- Editable only for Draft/Rejected.
- Read-only for PendingApproval/Approved/Converted/Expired.
- Approver panel for PendingApproval:
  - Approve.
  - Reject with reason.
- Show requested by, submitted at, approved by/at, rejection reason.
- Show expiry.
- “Book Charter” link for Approved quotes.
- “Clone as new quote” action for Approved/Expired quotes.

### `/Fleet/Charter/New` — become “Confirm Booking from Approved Quote”

Replace the rate-matrix UI in `Pages/Logistics/Charter/Create.cshtml` and `.cs`:

- On GET, accept optional `quoteId`.
- If no quote is selected, show a dropdown of approved, non-expired quotes.
- If no approved quotes exist, show a message and link to `/Fleet/Rates/New`.
- Display the selected quote read-only:
  - Client details.
  - Multi-stop addresses and distances.
  - Fuel option per leg.
  - Per-leg quoted amount.
  - Gross quoted amount, incidentals, net estimate.
- Staff enters only booking-specific fields:
  - Vehicle.
  - Driver.
  - Departure date.
  - Return date.
  - Booking notes.
- Remove:
  - `ActiveRouteRates`.
  - `RouteRatesJson`.
  - `matrixOrigins`.
  - `findRateMatch`.
  - `syncLegCost`.
  - automatic matrix pricing.
  - editable leg amount fields.
- On POST, quote ID is required and the service performs the approval/expiry/duplicate checks.

### `/Fleet/Charter` and `/Fleet/Charter/Details`

- Add quote reference to booking list and manifest.
- Show actual origin/destination addresses and distance on each leg.
- Show approved-by and approved-at from the linked quote.
- Keep existing legacy bookings readable when `QuoteRequestId` is null.
- Replace “Route Pricing Matrix” nav/link with “Client Quotes”.

### Navigation and aliases

Update `Manufacture.Web/Pages/Shared/_Layout.cshtml`:

- Replace “Route Rates Matrix” with “Client Quotes”.

Keep existing route aliases for compatibility:

- `/Fleet/Rates` -> quote register.
- `/Fleet/Rates/New` -> new quote.
- `/Fleet/Rates/Edit/{id}` -> quote review/edit.

Add clearer aliases in `Manufacture.Web/Program.cs`:

- `/Fleet/Quotes` -> `/Logistics/Rates/Index`
- `/Fleet/Quotes/New` -> `/Logistics/Rates/Create`
- `/Fleet/Quotes/Edit/{id:int}` -> `/Logistics/Rates/Edit`
- `/Fleet/Charter/New` remains the booking page.

## Implementation Steps

### Phase 1: Schema and Data Model

1. Add `CharterQuoteRequests.sql` and `CharterQuoteLegs.sql`.
2. Add `QuoteRequestId` to `CharterBookings.sql`.
3. Add `OriginAddress`, `DestinationAddress`, `DistanceKm` to `TripLegs.sql`.
4. Delete `RouteRates.sql` and remove the RouteRates seed block.
5. Build the database project and apply it to the local database.
6. Scaffold or hand-update:
   - `CharterQuoteRequest` entity and navigation to legs.
   - `CharterQuoteLeg` entity.
   - `CharterBooking.QuoteRequestId` navigation.
   - `TripLeg` address/distance fields.
   - `ManufactureDbContext` DbSets and relationships.
   - Remove `RouteRate` entity and DbSet.

### Phase 2: DTOs, Validation and Services

7. Add quote enums and DTOs; remove `RouteRateDto`.
8. Add `CharterQuoteCalculator` with totals/validation tests.
9. Add `ICharterQuoteService`/`CharterQuoteService`.
10. Remove RouteRate methods from `IFleetService`/`FleetService`.
11. Rewrite `CreateCharterBookingAsync` as quote conversion.
12. Add audit logging for submit, approve, reject and convert.

### Phase 3: Quote Pages

13. Replace Rates/Index with the Client Quotes register.
14. Replace Rates/Create with the New Client Quote form.
15. Replace Rates/Edit with the Review / Edit Client Quote page.
16. Add approval/rejection handlers with RBAC checks.

### Phase 4: Booking Pages and Navigation

17. Replace Charter/Create with approved-quote booking confirmation.
18. Update Charter/Index and Charter/Details for quote reference, addresses and distance.
19. Update the sidebar label and links.
20. Add `/Fleet/Quotes` aliases while retaining `/Fleet/Rates` compatibility.

### Phase 5: Cleanup and Tests

21. Remove all RouteRate UI, DTO, service, mock and seed references.
22. Replace RouteRate mock tests with quote calculation and status tests.
23. Add a manual workflow smoke test for create -> submit -> approve -> convert.

## Relevant Files

### Database

- `Manufacture.Database/Tables/CharterBookings.sql`
- `Manufacture.Database/Tables/TripLegs.sql`
- `Manufacture.Database/Tables/RouteRates.sql` — delete
- `Manufacture.Database/Tables/CharterQuoteRequests.sql` — new
- `Manufacture.Database/Tables/CharterQuoteLegs.sql` — new
- `scripts/seed-database.sql` — remove RouteRates seed; optional quote seed

### Library

- `Manufacture.Library/Data/ManufactureDbContext.cs`
- `Manufacture.Library/Models/Entities/CharterBooking.cs`
- `Manufacture.Library/Models/Entities/TripLeg.cs`
- `Manufacture.Library/Models/Entities/RouteRate.cs` — delete
- `Manufacture.Library/Models/Entities/CharterQuoteRequest.cs` — new
- `Manufacture.Library/Models/Entities/CharterQuoteLeg.cs` — new
- `Manufacture.Library/Models/DTOs/FleetDtos.cs`
- `Manufacture.Library/Interfaces/IFleetService.cs`
- `Manufacture.Library/Interfaces/ICharterQuoteService.cs` — new

### Web services

- `Manufacture.Web/Services/FleetService.cs`
- `Manufacture.Web/Services/CharterQuoteService.cs` — new
- `Manufacture.Web/Services/CharterQuoteCalculator.cs` — new
- `Manufacture.Web/Program.cs` — register quote service and route aliases

### Pages

- `Manufacture.Web/Pages/Logistics/Rates/Index.cshtml` + `.cs`
- `Manufacture.Web/Pages/Logistics/Rates/Create.cshtml` + `.cs`
- `Manufacture.Web/Pages/Logistics/Rates/Edit.cshtml` + `.cs`
- `Manufacture.Web/Pages/Logistics/Charter/Create.cshtml` + `.cs`
- `Manufacture.Web/Pages/Logistics/Charter/Index.cshtml` + `.cs`
- `Manufacture.Web/Pages/Logistics/Charter/Details.cshtml` + `.cs`
- `Manufacture.Web/Pages/Shared/_Layout.cshtml`

### Tests

- `Manufacture.Tests/MockFleetServiceTests.cs` — remove route-rate tests; add quote workflow tests
- `Manufacture.Tests/CharterQuoteCalculatorTests.cs` — new

## Verification

### Automated

```powershell
dotnet build Manufacture.slnx -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\
dotnet test Manufacture.Tests/Manufacture.Tests.csproj --no-build -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\
```

Add tests for:

- Quote total equals sum of leg amounts.
- Total distance equals sum of leg distances.
- Incidentals and net calculation.
- Quote with no legs fails validation.
- Leg with zero distance or zero amount fails validation.
- Draft -> PendingApproval -> Approved -> Converted transitions.
- Rejected quote can be edited and resubmitted.
- Approved quote cannot be edited directly.
- Expired quote cannot be converted.
- Same quote cannot create two bookings.
- Non-approver cannot approve.
- Self-approval is blocked for non-SuperAdmin.

### Manual workflow

1. Sign in as SuperAdmin.
2. Open `/Fleet/Rates/New`.
3. Create a quote with two legs, different addresses and distances, manual amounts and incidentals.
4. Save draft; confirm totals.
5. Submit for approval.
6. Approve the quote.
7. Open `/Fleet/Charter/New?quoteId={id}`.
8. Assign vehicle, driver and dates; confirm booking.
9. Confirm:
   - Booking legs show both addresses and distances.
   - Gross amount and incidentals match the quote.
   - Quote status is Converted.
   - The quote can no longer be converted again.
10. Verify a rejected quote can be edited and resubmitted.
11. Verify an expired quote cannot be booked.
12. Verify existing legacy bookings still render.
13. Verify internal `/Sales/Dispatch` and route-sheet pricing are unchanged.
14. Confirm `/Fleet/Rates` no longer shows state/city rate lanes or VIP fixed pricing.

## Non-Goals

- No map, geocoding or automatic distance calculation.
- No per-km or per-day pricing formula.
- No customer-facing self-service quote portal in this phase.
- No changes to internal sales-distribution route-sheet pricing.
- No VIP fixed tariff.
- No retained `RouteRates` data in the application after the drop.

## Risks and Mitigations

- **Dropping `RouteRates` is destructive.** Export the table to CSV/SQL before deployment if the client may need historical rates. The user approved the drop.
- **Existing bookings have no quote.** Keep `QuoteRequestId` nullable and treat old bookings as legacy.
- **Approval permissions are currently SuperAdmin-only.** Use `RbacCache.CanApprove(role, "Fleet")` so a future Fleet Manager role can approve without page rewrites.
- **Manual distances can be wrong.** Required distance and amount fields, plus approval, are the control. A map integration can be added later.
- **Charter/Create currently has significant matrix JavaScript.** Remove it entirely rather than leaving a hidden fallback that could silently price from old logic.
