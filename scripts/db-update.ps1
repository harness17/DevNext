# db-update.ps1
# EF Core migration auto-generate & apply
# Usage:
#   ./scripts/db-update.ps1            # auto-detect changes, generate migration, apply
#   ./scripts/db-update.ps1 -Name Xxx  # specify migration name

param(
    [string]$Name = ""
)

Set-Location "H:/ClaudeCode/DevNext"

Write-Host "Checking for model changes..." -ForegroundColor Cyan
dotnet ef migrations has-pending-model-changes --project DevNext --startup-project DevNext 2>&1 | Out-Null
$hasPendingChanges = ($LASTEXITCODE -ne 0)

if ($hasPendingChanges) {
    if ($Name -ne "") {
        $migrationName = $Name
    } else {
        $migrationName = "Auto_" + (Get-Date -Format "yyyyMMddHHmmss")
    }

    Write-Host "Adding migration: $migrationName" -ForegroundColor Yellow
    dotnet ef migrations add $migrationName --project DevNext --startup-project DevNext
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to add migration." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "No model changes detected." -ForegroundColor Green
}

Write-Host "Updating database..." -ForegroundColor Cyan
dotnet ef database update --project DevNext --startup-project DevNext
if ($LASTEXITCODE -ne 0) {
    Write-Host "Database update failed." -ForegroundColor Red
    exit 1
}

Write-Host "Done." -ForegroundColor Green
