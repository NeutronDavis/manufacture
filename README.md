# Manufacture

Bakery and beverage manufacturing ERP.

## Repository layout

```
Manufacture/
├── Manufacture.Web/        ASP.NET Core Razor Pages web application
├── Manufacture.Database/   SQL Server database project (schema source of truth)
├── Manufacture.Library/    Shared models, DTOs, interfaces, EF Core DbContext
├── Manufacture.Tests/      xUnit test project
├── scripts/                Database deploy/scaffold helpers
└── Manufacture.slnx        Solution file
```

### Manufacture.Web

The runnable application: Razor Pages, minimal API endpoints, mock services,
`wwwroot`, and app configuration.

```powershell
dotnet run --project Manufacture.Web/Manufacture.Web.csproj
```

### Manufacture.Database

SDK-style SQL Server Data Tools project. It owns the relational schema and
produces a deployable `.dacpac`. All tables live in the `dbo` schema under
`Manufacture.Database/Tables`, with ad-hoc scripts in `Manufacture.Database/Scripts`.

```powershell
dotnet build Manufacture.Database/Manufacture.Database.sqlproj
pwsh scripts/deploy-database.ps1
```

See `Manufacture.Database/README.md` for the schema map and conventions.

### Manufacture.Library

Shared class library for:

- hand-written domain models and enums,
- DTOs,
- application interfaces,
- the EF Core `ManufactureDbContext` and scaffolded database entities.

See `Manufacture.Library/README.md` for the database-first workflow.

## Build and test

```powershell
dotnet build Manufacture.slnx
dotnet test Manufacture.Tests/Manufacture.Tests.csproj
```

## Local database

The development connection string points at SQL Server LocalDB:

```
Server=(localdb)\MSSQLLocalDB;Database=ManufactureDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;
```

Override it with the `ConnectionStrings__ManufactureDb` environment variable in
other environments.
