# Sample IIS デプロイ設計

**日付:** 2026-04-05  
**ステータス:** 承認済み

---

## 概要

DevNext の Samples プロジェクト群を IIS に一括デプロイするための PowerShell スクリプトと Claude Code スキルを作成する。

現状、DevNext 本体の IIS デプロイ手順は `CLAUDE.local.md` に記載されているが、Sample プロジェクト向けには存在しない。本設計でその空白を埋める。

---

## デプロイ先構成

| パス | 内容 |
|------|------|
| `C:/inetpub/wwwroot/DevNext/samples/DatabaseSample` | DatabaseSample |
| `C:/inetpub/wwwroot/DevNext/samples/ExcelSample` | ExcelSample |
| `C:/inetpub/wwwroot/DevNext/samples/FileSample` | FileSample |
| `C:/inetpub/wwwroot/DevNext/samples/MailSample` | MailSample |
| `C:/inetpub/wwwroot/DevNext/samples/PdfSample` | PdfSample |
| `C:/inetpub/wwwroot/DevNext/samples/WizardSample` | WizardSample |

各 Sample は DevNext IIS サイト配下の仮想アプリケーション（`/samples/<SampleName>`）として登録する。

---

## 成果物

| 成果物 | パス |
|--------|------|
| デプロイスクリプト | `scripts/deploy-samples.ps1` |
| スキル | `.claude/skills/iis-deploy/SKILL.md` |

---

## スクリプト設計（`scripts/deploy-samples.ps1`）

### 定数

```powershell
$IisSiteName   = "DevNext"
$AppPoolName   = "DevNext"
$DeployRoot    = "C:\inetpub\wwwroot\DevNext\samples"
$SolutionRoot  = "H:\ClaudeCode\DevNext"
$Samples       = @("DatabaseSample","ExcelSample","FileSample","MailSample","PdfSample","WizardSample")
```

### 処理フロー

1. **DevNext AppPool を停止** — `Stop-WebAppPool -Name $AppPoolName`
2. **各 Sample をループ処理:**
   a. `dotnet publish -c Release -o <DeployRoot>/<SampleName>` でビルド＆発行
   b. IIS AppPool `DevNext-<SampleName>` が未作成なら `appcmd add apppool` で作成（`managedRuntimeVersion=""` = No Managed Code）
   c. IIS 仮想アプリケーション `/samples/<SampleName>` が未登録なら `appcmd add app` で登録し、AppPool をセット
3. **DevNext AppPool を起動** — `Start-WebAppPool -Name $AppPoolName`
4. **結果サマリーを出力** — 成功・失敗した Sample を色付きで表示

### AppPool 命名規則

各 Sample は独立した ASP.NET Core プロセスのため、専用 AppPool を使用する。

| Sample | AppPool 名 |
|--------|-----------|
| DatabaseSample | `DevNext-DatabaseSample` |
| ExcelSample | `DevNext-ExcelSample` |
| FileSample | `DevNext-FileSample` |
| MailSample | `DevNext-MailSample` |
| PdfSample | `DevNext-PdfSample` |
| WizardSample | `DevNext-WizardSample` |

### エラーハンドリング

- `$ErrorActionPreference = "Stop"` でエラー時に即停止
- 各 Sample のビルド失敗は catch して記録し、残りの Sample を継続処理
- 最後に失敗一覧を表示し、AppPool は必ず起動する（finally ブロック）

---

## スキル設計（`.claude/skills/iis-deploy/SKILL.md`）

### トリガー

ユーザーが `/iis-deploy` を実行したとき。

### 処理手順

1. `appcmd` の存在確認（管理者権限チェックを兼ねる）
2. `powershell -ExecutionPolicy Bypass -File scripts/deploy-samples.ps1` を Bash で実行
3. 実行結果を日本語で報告（成功した Sample・失敗した Sample を一覧表示）

### 責務境界

- スクリプトの呼び出しと結果報告のみ担当
- デプロイロジックはスクリプト側に集約する

---

## 前提条件

- 実行環境は Windows Server / Windows 11（IIS インストール済み）
- `appcmd.exe` が `PATH` または `C:\Windows\System32\inetsrv\appcmd.exe` に存在すること
- 実行ユーザーが IIS 管理者権限を持つこと
- DevNext IIS サイトが事前に作成済みであること
