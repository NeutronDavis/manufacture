# Manufacture.Database

SQL Server Data Tools (SDK-style) database project that is the source of truth
for the Manufacturing application's relational schema.

The project is built with `Microsoft.Build.Sql` and produces a `.dacpac` that can
be published to SQL Server, Azure SQL, or a local SQL Server LocalDB instance.

## Organisation

All tables live in the **`dbo`** schema and in a single `Tables` folder. This
keeps joins simple (no cross-schema prefixes) and matches the layout used by the
other database projects in the workspace.

```
Manufacture.Database/
├── Tables/
│   ├── Users.sql
│   ├── Orders.sql
│   ├── OrderItems.sql
│   └── ...
├── Scripts/
│   └── README.md
├── README.md
└── Manufacture.Database.sqlproj
```

## Table map

### Identity and access

- `Users` — application logins, roles, PIN/password hashes.
- `AuditLogs` — append-only change history.

### Human resources

- `Employees` — staff master record with emergency and guarantor contacts.
- `SalaryAdvances` — staff loans and repayment balances.
- `PayrollRecords` — one row per employee per payroll month.

### Product catalogue and inventory

- `Suppliers`
- `InventoryItems`
- `Ingredients` — raw materials priced per unit.
- `Recipes` — sellable products and their baseline-yield costing header.
- `RecipeIngredients` — ingredient lines for one baseline batch.

### Production

- `ProductionBatches` — logged production runs with costing snapshots.
- `ProductionDefects` — factory rejects removed before loading.
- `StoreRequests`
- `StoreRequestItems`

### Sales and route reconciliation

- `Customers`
- `PosTerminals`
- `SalesReps` — field roster, route, POS terminal, and vehicle assignment.
- `Orders`
- `OrderItems`
- `SupermarketAccounts`
- `SupermarketAccountPrices` — agreed per-product prices for an account.
- `CustomerReturns` — prior-sale items returned by customers.
- `DailyRepRouteSheets` — daily rep reconciliation header.
- `RouteSheetItems` — per-SKU loaded, sold, closing-stock, and damaged quantities.

### Fleet and logistics

- `Drivers`
- `Vehicles`
- `FuelLogs`
- `RouteRates`
- `CharterBookings`
- `TripLegs`
- `MaintenanceLogs`
- `WeeklyFleetExpenses`

### Finance

- `TreasuryPolicySettings` — single-row cash/digital tender policy.

## Core relationships

```mermaid
erDiagram
    Users ||--o{ PosTerminals : "operates"
    Users ||--o{ SalesReps : "signs in as"
    Vehicles ||--o{ SalesReps : "is assigned"
    Customers ||--o{ Orders : "places"
    SalesReps ||--o{ Orders : "takes"
    Orders ||--|{ OrderItems : "contains"
    Recipes ||--o{ OrderItems : "sells as"
    Recipes ||--o{ RecipeIngredients : "has"
    Ingredients ||--o{ RecipeIngredients : "used in"
    Recipes ||--o{ ProductionBatches : "produced as"
    ProductionBatches ||--o{ ProductionDefects : "yields"
    SalesReps ||--o{ DailyRepRouteSheets : "reconciles"
    DailyRepRouteSheets ||--|{ RouteSheetItems : "contains"
    Recipes ||--o{ RouteSheetItems : "loaded/sold as"
    Recipes ||--o{ CustomerReturns : "returned as"
    Customers ||--o{ SupermarketAccounts : "has"
    SupermarketAccounts ||--o{ SupermarketAccountPrices : "agrees"
    Vehicles ||--o{ FuelLogs : "consumes"
    Vehicles ||--o{ MaintenanceLogs : "serviced by"
    Vehicles ||--o{ CharterBookings : "chartered as"
    CharterBookings ||--|{ TripLegs : "contains"
    Employees ||--o{ PayrollRecords : "paid via"
    Employees ||--o{ SalaryAdvances : "advances"
```

## Conventions

- Tables are plural and PascalCase and all live in `dbo`.
- Every table has an `Id` surrogate key (`INT IDENTITY`), except audit rows
  (`BIGINT`) and the single-row settings table.
- Business keys are protected by unique constraints or filtered unique indexes
  (for example `Users.Email`, `SalesReps.UserId`, `Ingredients.ItemCode`).
- Enum-like columns are `INT` with a named `CHECK` constraint so the numeric
  values match the C# enums in `Manufacture.Library/Models/Entities`.
- Money uses `DECIMAL(18,2)`; quantities use `DECIMAL(18,3)`; unit costs use
  `DECIMAL(18,4)`.
- Derived values that are safe to persist (`LineTotal`, `Variance`,
  `RemainingBalance`, `NetSalary`, and similar) are persisted computed columns.
- Child line tables cascade from their parent header; reference/master tables use
  `NO ACTION` to avoid accidental deletes and multiple cascade paths.
- `CreatedAt` / `UpdatedAt` default to `SYSUTCDATETIME()`; the application is
  responsible for refreshing `UpdatedAt` on updates.
- Aggregate roots that benefit from optimistic concurrency carry `ROWVERSION`.

## Visual Studio

The project targets **.NET 10.0** (`<TargetFramework>net10.0</TargetFramework>`)
so it matches the web and library projects and does not require a .NET Framework
targeting pack.

Every table and README file is listed explicitly in
`Manufacture.Database.sqlproj`. `EnableDefaultSqlItems` is turned off so
Solution Explorer shows the full `Tables/*.sql` tree instead of relying on the
SDK's implicit `**/*.sql` glob.

The project also pins the SQL build tasks to the copies shipped inside the
`Microsoft.Build.Sql` package:

```xml
<BuildFromSSDT>false</BuildFromSSDT>
<SSDTPath>$(NETFrameworkTargetsPath)</SSDTPath>
```

Without this, Visual Studio's SQLDB extension can load its older DacFx task
assembly and fail to initialize `SqlScriptDependenciesTask` (`04063` / `04064`).

`Microsoft.Build.Sql` is pinned to **2.2.0** for Visual Studio 2026
compatibility. Version 2.3.0 passes a `PreplanItem` parameter that the DacFx
task assembly bundled with the Visual Studio SQLDB extension does not expose.
The newer-SDK version check is suppressed with
`SkipCheckForNewSDKVersion`.

## Build

```powershell
dotnet build Manufacture.Database/Manufacture.Database.sqlproj
```

The compiled model is written to `bin/Debug/Manufacture.Database.dacpac`.

## Deploy to LocalDB

The repository includes a helper that builds, creates the database, publishes the
schema, and applies the baseline seed rows:

```powershell
pwsh scripts/deploy-database.ps1
```

Defaults:

- Server: `(localdb)\MSSQLLocalDB`
- Database: `ManufactureDb`

Override them when needed:

```powershell
pwsh scripts/deploy-database.ps1 -Server "localhost" -Database "ManufactureDb_Dev"
```

For a clean development reset (drops and recreates the database):

```powershell
pwsh scripts/deploy-database.ps1 -Recreate
```

`Microsoft.SqlPackage` and `dotnet-ef` are pinned as local tools in
`dotnet-tools.json`; run `dotnet tool restore` after cloning.

## Re-scaffold the EF Core models

After changing the schema, regenerate the DbContext and entities in the class
library:

```powershell
pwsh scripts/scaffold-database.ps1
```

Generated files:

- `Manufacture.Library/Data/ManufactureDbContext.cs`
- `Manufacture.Library/Models/Entities/*.cs`

The hand-written domain models and DTOs remain in `Manufacture.Library/Models`
(`Models/Domain` and `Models/DTOs`).
