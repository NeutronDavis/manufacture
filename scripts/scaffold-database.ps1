param(
    [string]$Server = "(localdb)\MSSQLLocalDB",
    [string]$Database = "ManufactureDb"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$connectionString = "Server=$Server;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;"

Push-Location $root
try {
    dotnet tool run dotnet-ef dbcontext scaffold `
        $connectionString `
        Microsoft.EntityFrameworkCore.SqlServer `
        --project Manufacture.Library `
        --startup-project Manufacture.Library `
        --output-dir Models/Entities `
        --context-dir Data `
        --context ManufactureDbContext `
        --namespace Manufacture.Models.Entities `
        --context-namespace Manufacture.Library.Data `
        --no-onconfiguring `
        --force
}
finally {
    Pop-Location
}
