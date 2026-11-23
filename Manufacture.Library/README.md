# Manufacture.Library

Shared class library for the Manufacturing application. It is the single home
for the types used by the web project, the test project, and any future worker
or API project.

## Layout

| Folder                 | Contents                                                                 |
| ---------------------- | ------------------------------------------------------------------------ |
| `Models/Entities`      | EF Core entities scaffolded from `Manufacture.Database` (one per table).  |
| `Models/Domain`        | Hand-written domain models and enums currently used by the UI/services.  |
| `Models/DTOs`          | Request, response, view, and API DTOs.                                   |
| `Interfaces`           | Application contracts (starting with `IUnitConversionService`).          |
| `Data`                 | `ManufactureDbContext` (EF Core, database-first).                        |

## Database-first workflow

The relational schema lives in `Manufacture.Database`. After changing it:

```powershell
pwsh scripts/deploy-database.ps1      # build + publish + seed
pwsh scripts/scaffold-database.ps1    # regenerate Data/ and Models/Entities/
```

The scaffolded entities in `Models/Entities` mirror the SQL tables exactly
(column names, computed columns, rowversions, navigations). The hand-written
models in `Models/Domain` retain the computed properties and UI-friendly shape
the current mock services depend on. As modules are migrated from mock data to
the database, repositories should project `Models/Entities` rows into those
domain models or directly into `Models/DTOs`.

## Packages

- `Microsoft.EntityFrameworkCore.SqlServer` — SQL Server provider.
- `Microsoft.EntityFrameworkCore.Design` — design-time scaffolding support.
- `System.Security.Cryptography.Pkcs` — pinned to remove a vulnerable
  transitive dependency pulled in by the SQL client.
