#Requires -RunAsAdministrator
$ErrorActionPreference = "Stop"

# ── 定数 ──────────────────────────────────────────────
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

# ── 前提チェック ───────────────────────────────────────
if (-not (Test-Path $AppcmdPath)) {
    Write-Error "appcmd.exe が見つかりません: $AppcmdPath`nIIS がインストールされているか確認してください。"
    exit 1
}

Import-Module WebAdministration -ErrorAction Stop

# ── AppPool 停止 ───────────────────────────────────────
Write-Host "DevNext AppPool を停止します..." -ForegroundColor Cyan
$poolState = (Get-WebAppPoolState -Name $AppPoolName).Value
if ($poolState -ne "Stopped") {
    Stop-WebAppPool -Name $AppPoolName
    Start-Sleep -Seconds 2
}
Write-Host "停止しました。" -ForegroundColor Green

# ── 各 Sample をデプロイ ──────────────────────────────
$failed = @()

foreach ($sample in $Samples) {
    Write-Host "`n[$sample] デプロイ開始..." -ForegroundColor Cyan
    $sampleProjectPath = Join-Path $SolutionRoot "Samples\$sample"
    $deployPath        = Join-Path $DeployRoot $sample
    $sampleAppPool     = "DevNext-$sample"
    $virtualPath       = "/samples/$sample"

    try {
        # 1. dotnet publish
        Write-Host "  ビルド＆パブリッシュ中..."
        & dotnet publish "$sampleProjectPath\$sample.csproj" `
            -c Release `
            -o $deployPath `
            --nologo `
            2>&1 | ForEach-Object { Write-Host "  $_" }

        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish が失敗しました (exit code: $LASTEXITCODE)"
        }

        # 2. AppPool の作成（未存在時のみ）
        $existingPool = & $AppcmdPath list apppool $sampleAppPool 2>$null
        if (-not $existingPool) {
            Write-Host "  AppPool '$sampleAppPool' を作成します..."
            & $AppcmdPath add apppool /name:$sampleAppPool /managedRuntimeVersion:"" | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "AppPool '$sampleAppPool' の作成に失敗しました。" }
            Write-Host "  AppPool を作成しました。"
        } else {
            Write-Host "  AppPool '$sampleAppPool' は既に存在します。"
        }

        # 3. 仮想アプリケーションの登録（未登録時のみ）
        $existingApp = & $AppcmdPath list app "$IisSiteName$virtualPath" 2>$null
        if (-not $existingApp) {
            Write-Host "  仮想アプリ '$virtualPath' を登録します..."
            & $AppcmdPath add app `
                /site.name:$IisSiteName `
                /path:$virtualPath `
                /physicalPath:$deployPath | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "仮想アプリ '$virtualPath' の登録に失敗しました。" }
            & $AppcmdPath set app "$IisSiteName$virtualPath" `
                /applicationPool:$sampleAppPool | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "仮想アプリ '$virtualPath' への AppPool 設定に失敗しました。" }
            Write-Host "  仮想アプリを登録しました。"
        } else {
            Write-Host "  仮想アプリ '$virtualPath' は既に存在します。"
        }

        Write-Host "[$sample] デプロイ完了" -ForegroundColor Green

    } catch {
        Write-Host "[$sample] デプロイ失敗: $_" -ForegroundColor Red
        $failed += $sample
    }
}

# ── AppPool 起動 ───────────────────────────────────────
Write-Host "`nDevNext AppPool を起動します..." -ForegroundColor Cyan
$poolState = (Get-WebAppPoolState -Name $AppPoolName).Value
if ($poolState -ne "Started") {
    Start-WebAppPool -Name $AppPoolName
}
Write-Host "起動しました。" -ForegroundColor Green

# ── 結果サマリー ───────────────────────────────────────
$succeeded = $Samples | Where-Object { $_ -notin $failed }

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " デプロイ結果サマリー" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

foreach ($s in $succeeded) {
    Write-Host "  ✅ $s" -ForegroundColor Green
}
foreach ($s in $failed) {
    Write-Host "  ❌ $s" -ForegroundColor Red
}

if ($failed.Count -gt 0) {
    Write-Host "`n失敗した Sample があります。上記のエラーを確認してください。" -ForegroundColor Red
    exit 1
} else {
    Write-Host "`nすべての Sample のデプロイが完了しました。" -ForegroundColor Green
}
