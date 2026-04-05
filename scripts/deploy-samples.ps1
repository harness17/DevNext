#Requires -RunAsAdministrator
$ErrorActionPreference = "Stop"

# -- Constants -------------------------------------------------------
$IisSiteName  = "DevNext"
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

# -- Prerequisites check ---------------------------------------------
if (-not (Test-Path $AppcmdPath)) {
    Write-Error "appcmd.exe not found: $AppcmdPath`nPlease verify IIS is installed."
    exit 1
}

Import-Module WebAdministration -ErrorAction Stop

# -- Stop AppPool ----------------------------------------------------
Write-Host "Stopping DevNext AppPool..." -ForegroundColor Cyan
$poolState = (Get-WebAppPoolState -Name $AppPoolName).Value
if ($poolState -ne "Stopped") {
    Stop-WebAppPool -Name $AppPoolName
    Start-Sleep -Seconds 2
}
Write-Host "AppPool stopped." -ForegroundColor Green

# -- Deploy each Sample ----------------------------------------------
$failed = @()

foreach ($sample in $Samples) {
    Write-Host "`n[$sample] Deploying..." -ForegroundColor Cyan
    $sampleProjectPath = Join-Path $SolutionRoot "Samples\$sample"
    $deployPath        = Join-Path $DeployRoot $sample
    $sampleAppPool     = "DevNext-$sample"
    $virtualPath       = "/samples/$sample"

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
            & $AppcmdPath add apppool /name:$sampleAppPool /managedRuntimeVersion:"" | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "Failed to create AppPool '$sampleAppPool'." }
            Write-Host "  AppPool created."
        } else {
            Write-Host "  AppPool '$sampleAppPool' already exists."
        }

        # 3. Register virtual app if not exists, else update physicalPath
        $existingApp = & $AppcmdPath list app "$IisSiteName$virtualPath" 2>$null
        if (-not $existingApp) {
            Write-Host "  Registering virtual app '$virtualPath'..."
            & $AppcmdPath add app `
                /site.name:$IisSiteName `
                /path:$virtualPath `
                /physicalPath:$deployPath | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "Failed to register virtual app '$virtualPath'." }
            & $AppcmdPath set app "$IisSiteName$virtualPath" `
                /applicationPool:$sampleAppPool | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "Failed to set AppPool for '$virtualPath'." }
            Write-Host "  Virtual app registered."
        } else {
            Write-Host "  Virtual app '$virtualPath' exists. Updating physicalPath..."
            & $AppcmdPath set app "$IisSiteName$virtualPath" /physicalPath:$deployPath | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "Failed to update physicalPath for '$virtualPath'." }
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
