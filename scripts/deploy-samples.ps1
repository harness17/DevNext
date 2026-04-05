#Requires -RunAsAdministrator
$ErrorActionPreference = "Stop"

# -- Constants -------------------------------------------------------
# IIS site name (from: appcmd list site)
$IisSiteName  = "Default Web Site"
# DevNext app path under the site (from: appcmd list app)
$DevNextApp   = "Default Web Site/DevNext"
$AppPoolName  = "DevNext"
$DeployRoot   = "C:\inetpub\wwwroot\DevNext\samples"
$SolutionRoot = "H:\ClaudeCode\DevNext"
$AppcmdPath   = "C:\Windows\System32\inetsrv\appcmd.exe"
$Samples      = @(
    "DatabaseSample",
    "ExcelSample",
    "FileSample",
    "MailSample",
    "PdfSample",
    "WizardSample"
)

# -- Stop AppPool ----------------------------------------------------
Write-Host "Stopping DevNext AppPool..." -ForegroundColor Cyan
Import-Module WebAdministration -ErrorAction Stop
$poolState = (Get-WebAppPoolState -Name $AppPoolName).Value
if ($poolState -ne "Stopped") {
    Stop-WebAppPool -Name $AppPoolName
    Start-Sleep -Seconds 2
}
Write-Host "AppPool stopped." -ForegroundColor Green

# -- Ensure /samples virtual directory exists under DevNext ----------
if (-not (Test-Path $DeployRoot)) {
    New-Item -ItemType Directory -Path $DeployRoot | Out-Null
}
$samplesVdir = & $AppcmdPath list vdir "$DevNextApp/samples" 2>$null
if (-not $samplesVdir) {
    Write-Host "Creating /DevNext/samples virtual directory..." -ForegroundColor Cyan
    & $AppcmdPath add vdir /app.name:"$DevNextApp" /path:/samples /physicalPath:$DeployRoot
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to create /samples virtual directory." }
    Write-Host "/DevNext/samples virtual directory created." -ForegroundColor Green
}

# -- Deploy each Sample ----------------------------------------------
$failed = @()

foreach ($sample in $Samples) {
    Write-Host "`n[$sample] Deploying..." -ForegroundColor Cyan
    $sampleProjectPath = Join-Path $SolutionRoot "Samples\$sample"
    $deployPath        = Join-Path $DeployRoot $sample
    $sampleAppPool     = "DevNext-$sample"
    $appPath           = "/DevNext/samples/$sample"
    $appFullName       = "$IisSiteName/DevNext/samples/$sample"

    try {
        # 1. dotnet publish
        Write-Host "  Publishing..."
        & dotnet publish "$sampleProjectPath\$sample.csproj" `
            -c Release `
            -o $deployPath `
            --nologo `
            2>&1 | ForEach-Object { Write-Host "  $_" }

        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish failed (exit code: $LASTEXITCODE)"
        }

        # 2. Create AppPool if not exists
        $existingPool = & $AppcmdPath list apppool $sampleAppPool 2>$null
        if (-not $existingPool) {
            Write-Host "  Creating AppPool '$sampleAppPool'..."
            & $AppcmdPath add apppool /name:$sampleAppPool /managedRuntimeVersion:""
            if ($LASTEXITCODE -ne 0) { throw "Failed to create AppPool '$sampleAppPool'." }
            Write-Host "  AppPool created."
        } else {
            Write-Host "  AppPool '$sampleAppPool' already exists."
        }

        # 3. Register web application
        $existingApp = & $AppcmdPath list app $appFullName 2>$null
        if (-not $existingApp) {
            Write-Host "  Registering app '$appPath'..."
            & $AppcmdPath add app /site.name:"$IisSiteName" /path:$appPath /physicalPath:$deployPath
            if ($LASTEXITCODE -ne 0) { throw "Failed to register app '$appPath'." }
            & $AppcmdPath set app "$appFullName" /applicationPool:$sampleAppPool
            if ($LASTEXITCODE -ne 0) { throw "Failed to set AppPool for '$appPath'." }
            Write-Host "  App registered."
        } else {
            Write-Host "  App '$appPath' exists. Updating physicalPath..."
            & $AppcmdPath set app "$appFullName" /physicalPath:$deployPath
            if ($LASTEXITCODE -ne 0) { throw "Failed to update physicalPath for '$appPath'." }
            Write-Host "  physicalPath updated."
        }

        Write-Host "[$sample] Deploy complete." -ForegroundColor Green

    } catch {
        Write-Host "[$sample] Deploy FAILED: $_" -ForegroundColor Red
        $failed += $sample
    }
}

# -- Start AppPool ---------------------------------------------------
Write-Host "`nStarting DevNext AppPool..." -ForegroundColor Cyan
$poolState = (Get-WebAppPoolState -Name $AppPoolName).Value
if ($poolState -ne "Started") {
    Start-WebAppPool -Name $AppPoolName
}
Write-Host "AppPool started." -ForegroundColor Green

# -- Result summary --------------------------------------------------
$succeeded = $Samples | Where-Object { $_ -notin $failed }

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " Deploy Result Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

foreach ($s in $succeeded) {
    Write-Host "  [OK] $s" -ForegroundColor Green
}
foreach ($s in $failed) {
    Write-Host "  [NG] $s" -ForegroundColor Red
}

if ($failed.Count -gt 0) {
    Write-Host "`nSome samples failed. Check errors above." -ForegroundColor Red
    exit 1
} else {
    Write-Host "`nAll samples deployed successfully." -ForegroundColor Green
}
