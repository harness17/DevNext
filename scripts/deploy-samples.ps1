#Requires -RunAsAdministrator
$ErrorActionPreference = "Stop"

# -- Constants -------------------------------------------------------
$IisSiteName  = "DevNext"
$AppPoolName  = "DevNext"
$DeployRoot   = "C:\inetpub\wwwroot\DevNext\samples"
$SolutionRoot = "H:\ClaudeCode\DevNext"
$Samples      = @(
    "DatabaseSample",
    "ExcelSample",
    "FileSample",
    "MailSample",
    "PdfSample",
    "WizardSample"
)

Import-Module WebAdministration -ErrorAction Stop

# -- Stop AppPool ----------------------------------------------------
Write-Host "Stopping DevNext AppPool..." -ForegroundColor Cyan
$poolState = (Get-WebAppPoolState -Name $AppPoolName).Value
if ($poolState -ne "Stopped") {
    Stop-WebAppPool -Name $AppPoolName
    Start-Sleep -Seconds 2
}
Write-Host "AppPool stopped." -ForegroundColor Green

# -- Ensure /samples virtual directory exists ------------------------
if (-not (Test-Path $DeployRoot)) {
    New-Item -ItemType Directory -Path $DeployRoot | Out-Null
}
$samplesVdir = Get-WebVirtualDirectory -Site $IisSiteName -Name "samples" -ErrorAction SilentlyContinue
if (-not $samplesVdir) {
    Write-Host "Creating /samples virtual directory..." -ForegroundColor Cyan
    New-WebVirtualDirectory -Site $IisSiteName -Name "samples" -PhysicalPath $DeployRoot | Out-Null
    Write-Host "/samples virtual directory created." -ForegroundColor Green
}

# -- Deploy each Sample ----------------------------------------------
$failed = @()

foreach ($sample in $Samples) {
    Write-Host "`n[$sample] Deploying..." -ForegroundColor Cyan
    $sampleProjectPath = Join-Path $SolutionRoot "Samples\$sample"
    $deployPath        = Join-Path $DeployRoot $sample
    $sampleAppPool     = "DevNext-$sample"

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
        if (-not (Test-Path "IIS:\AppPools\$sampleAppPool")) {
            Write-Host "  Creating AppPool '$sampleAppPool'..."
            New-WebAppPool -Name $sampleAppPool | Out-Null
            Set-ItemProperty "IIS:\AppPools\$sampleAppPool" -Name managedRuntimeVersion -Value ""
            Write-Host "  AppPool created."
        } else {
            Write-Host "  AppPool '$sampleAppPool' already exists."
        }

        # 3. Register web application (samples/<SampleName>)
        $existingApp = Get-WebApplication -Site $IisSiteName -Name "samples/$sample" -ErrorAction SilentlyContinue
        if (-not $existingApp) {
            Write-Host "  Registering web application 'samples/$sample'..."
            New-WebApplication -Site $IisSiteName -Name "samples/$sample" `
                -PhysicalPath $deployPath `
                -ApplicationPool $sampleAppPool | Out-Null
            Write-Host "  Web application registered."
        } else {
            Write-Host "  Web application 'samples/$sample' exists. Updating physicalPath..."
            Set-ItemProperty "IIS:\Sites\$IisSiteName\samples\$sample" -Name physicalPath -Value $deployPath
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
