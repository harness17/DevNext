# Sample IIS デプロイ Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Sample プロジェクト群（6本）を IIS に一括デプロイする PowerShell スクリプトと `/iis-deploy` スキルを作成する。

**Architecture:** `scripts/deploy-samples.ps1` にデプロイロジックを集約し、`.claude/skills/iis-deploy/SKILL.md` はそのスクリプトを呼び出す薄いラッパーとして機能する。各 Sample は `C:/inetpub/wwwroot/DevNext/samples/<SampleName>` に配置し、IIS 仮想アプリケーションと専用 AppPool を自動登録する。

**Tech Stack:** PowerShell (WebAdministration モジュール, appcmd.exe), .NET 10 (dotnet publish), IIS 10

---

## File Map

| 操作 | パス | 役割 |
|------|------|------|
| 新規作成 | `scripts/deploy-samples.ps1` | デプロイロジック本体 |
| 更新 | `.claude/skills/iis-deploy/SKILL.md` | Sample デプロイセクションを追加 |

---

### Task 1: deploy-samples.ps1 の作成

**Files:**
- Create: `scripts/deploy-samples.ps1`

- [ ] **Step 1: スクリプトファイルを作成する**

`scripts/deploy-samples.ps1` を以下の内容で作成する。

```powershell
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
Stop-WebAppPool -Name $AppPoolName
Start-Sleep -Seconds 2
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
            & $AppcmdPath set app "$IisSiteName$virtualPath" `
                /applicationPool:$sampleAppPool | Out-Null
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
Start-WebAppPool -Name $AppPoolName
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
```

- [ ] **Step 2: スクリプトの動作を手動確認する**

管理者権限の PowerShell で構文チェックを行う：

```bash
powershell -Command "& { \$null = [System.Management.Automation.Language.Parser]::ParseFile('H:/ClaudeCode/DevNext/scripts/deploy-samples.ps1', [ref]\$null, [ref]\$null); Write-Host 'Syntax OK' }"
```

期待結果: `Syntax OK`

- [ ] **Step 3: コミットする**

```bash
cd H:/ClaudeCode/DevNext && git add scripts/deploy-samples.ps1
git commit -m "feat: Sample IIS 一括デプロイスクリプトを追加"
```

---

### Task 2: iis-deploy スキルに Sample デプロイセクションを追加

**Files:**
- Modify: `.claude/skills/iis-deploy/SKILL.md`

既存の `iis-deploy` スキルは DevNext 本体のデプロイを担っている。そこに Sample デプロイのセクションを追記する。

- [ ] **Step 1: SKILL.md に Sample デプロイセクションを追記する**

`.claude/skills/iis-deploy/SKILL.md` の末尾（`## 注意事項` の後）に以下を追加する：

```markdown

---

## Sample プロジェクトのデプロイ

ユーザーが「Sample をデプロイ」「サンプルを発行」など Sample 向けのデプロイを指示した場合は、
DevNext 本体の手順ではなく以下の手順を実行すること。

### 前提確認

appcmd.exe の存在を確認する（管理者権限チェックを兼ねる）：

```bash
ls "C:/Windows/System32/inetsrv/appcmd.exe"
```

存在しない場合は「IIS がインストールされていないか、管理者権限で Claude Code を起動してください」と報告して中止する。

### 実行

```bash
cd H:/ClaudeCode/DevNext && powershell -ExecutionPolicy Bypass -File scripts/deploy-samples.ps1
```

### 結果報告

スクリプトの出力から結果を読み取り、以下の形式で報告する：

- デプロイ成功した Sample の一覧（✅）
- デプロイ失敗した Sample の一覧（❌）と失敗理由
- 初回デプロイ時は「IIS 仮想アプリケーションを新規登録しました」と補足する
- 失敗がある場合はエラーログを引用して原因を説明する
```

- [ ] **Step 2: SKILL.md のフォーマット確認**

ファイルを読み直して frontmatter が壊れていないこと、マークダウンのコードブロックが正しく閉じていることを確認する。

- [ ] **Step 3: コミットする**

```bash
cd H:/ClaudeCode/DevNext && git add .claude/skills/iis-deploy/SKILL.md
git commit -m "feat: /iis-deploy スキルに Sample デプロイセクションを追加"
```

---

### Task 3: .agents/skills にも反映する

**Files:**
- Modify: `.agents/skills/iis-deploy/SKILL.md`（存在する場合）

`.agents/` 配下にも同名スキルが存在する場合は同様に更新する。

- [ ] **Step 1: .agents/ 配下を確認する**

```bash
ls H:/ClaudeCode/DevNext/.agents/skills/ 2>/dev/null || echo "not found"
```

- [ ] **Step 2: 存在すれば同じ内容を反映する**

`.agents/skills/iis-deploy/SKILL.md` が存在する場合、Task 2 と同じ Sample デプロイセクションを追記する。

- [ ] **Step 3: コミットする（変更があった場合のみ）**

```bash
cd H:/ClaudeCode/DevNext && git add .agents/skills/iis-deploy/SKILL.md
git commit -m "feat: .agents/iis-deploy スキルに Sample デプロイセクションを追加"
```
