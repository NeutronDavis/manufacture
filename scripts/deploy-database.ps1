param(
    [string]$Server = "(localdb)\MSSQLLocalDB",
    [string]$Database = "ManufactureDb",
    [switch]$Recreate
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "Manufacture.Database\Manufacture.Database.sqlproj"
$dacpac = Join-Path $root "Manufacture.Database\bin\Debug\Manufacture.Database.dacpac"
$seed = Join-Path $PSScriptRoot "seed-database.sql"

Write-Host "Building $project..." -ForegroundColor Cyan
dotnet build $project -v:minimal

if ($Recreate) {
    Write-Host "Dropping database [$Database]..." -ForegroundColor Yellow
    sqlcmd -S $Server -b -Q @"
IF DB_ID(N'$Database') IS NOT NULL
BEGIN
    ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$Database];
END
"@
}

Write-Host "Ensuring database [$Database] exists on [$Server]..." -ForegroundColor Cyan
sqlcmd -S $Server -b -Q "IF DB_ID(N'$Database') IS NULL CREATE DATABASE [$Database];"

Write-Host "Publishing schema..." -ForegroundColor Cyan
Push-Location $root
try {
    dotnet tool run sqlpackage `
        /Action:Publish `
        /SourceFile:$dacpac `
        /TargetConnectionString:"Server=$Server;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;" `
        /p:BlockOnPossibleDataLoss=False
}
finally {
    Pop-Location
}

if (Test-Path $seed) {
    Write-Host "Applying seed data..." -ForegroundColor Cyan
    sqlcmd -S $Server -d $Database -b -i $seed
}

Write-Host "Database [$Database] is ready." -ForegroundColor Green
