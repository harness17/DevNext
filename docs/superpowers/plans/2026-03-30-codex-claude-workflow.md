# Claude Code ↔ Codex 連携ワークフロー 実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** AGENTS.md・連携スクリプト2本を追加し、「Claude Codeが設計 → Codexが実装 → Claude Codeがレビュー」を自動化する。

**Architecture:** AGENTS.mdにDevNextの共通ルールを集約しCodexが単体で参照できるようにする。start-codex.ps1でCodexに計画ファイルを渡し、request-review.ps1でClaude Codeレビューを自動起動する。

**Tech Stack:** PowerShell 5.1+, Windows Terminal (wt), Claude Code CLI (`claude --print`), Codex CLI

---

## ファイル構成

| ファイル | 操作 | 役割 |
|---------|------|------|
| `AGENTS.md` | 新規作成 | Codex向け共通ルール（コーディング・DB・コマンド） |
| `CLAUDE.md` | 先頭1行追加 | `@AGENTS.md` を参照させる |
| `scripts/start-codex.ps1` | 新規作成 | 計画ファイルを渡してCodexを新タブ起動 |
| `scripts/request-review.ps1` | 新規作成 | git差分をClaude Codeに渡してレビュー起動 |

---

## Task 1: AGENTS.md を作成する

**Files:**
- Create: `AGENTS.md`

- [ ] **Step 1: AGENTS.md を作成する**

  プロジェクトルートに以下の内容で作成する：

  ```markdown
  # DevNext — Agent Guidelines

  このファイルはClaude Code・Codex・Cursor等のAIエージェントが参照する共通ルールです。
  Claude Code固有の設定（スキル・フック）は `.claude/` を参照してください。

  ---

  ## プロジェクト概要

  - **種別**: ASP.NET Core 10 MVC Webアプリ（ポートフォリオ兼テンプレート）
  - **目的**: 新案件の出発点となるコアテンプレート（認証・ユーザー管理・基本CRUD）
  - **言語**: C# / Razor Views / JavaScript (jQuery)

  ## プロジェクト構成

  ```
  DevNext/          ← メインWebアプリ（RootNamespace: Site）
  CommonLibrary/    ← 共通ライブラリ（RootNamespace: Dev.CommonLibrary）
  DbMigrationRunner/ ← DB初期化ツール（RootNamespace: DbMigrationRunner）
  Tests/            ← xUnit テストプロジェクト
  Samples/          ← 独立したサンプルプロジェクト群（各自独立）
  docs/             ← 設計書・実装計画
  scripts/          ← 開発補助スクリプト
  ```

  ---

  ## コーディング方針

  - **メンテナンス性を最優先**とする
  - **可能な限りコメントを生成**する

  ### 名前空間ルール

  | プロジェクト | RootNamespace |
  |---|---|
  | `DevNext/` | `Site` |
  | `CommonLibrary/` | `Dev.CommonLibrary` |
  | `DbMigrationRunner/` | `DbMigrationRunner` |

  ### エンティティ設計ルール

  - すべてのエンティティは **`SiteEntityBase` を継承**すること（`Id: long` + 共通監査カラムを統一するため）
  - 更新履歴が必要なエンティティは以下のパターンで実装すること：

  ```
  XxxEntityBase (abstract) : SiteEntityBase
    ├── XxxEntity          // 本体テーブル
    └── XxxEntityHistory   // 履歴テーブル（HistoryId: long [Key], IEntityHistory）
  ```

  ### DI 登録ルール

  - `[ServiceFilter(typeof(XxxAttribute))]` を使う場合、対象クラスを `Program.cs` で `AddScoped` 登録する

  ```csharp
  builder.Services.AddScoped<AccessLogAttribute>();
  ```

  ### パスワードポリシー

  `Program.cs` で設定。以下をすべて満たすこと：最低6文字・大文字・小文字・数字・記号すべて必須

  ---

  ## データベースルール

  - **SQL Server**、DB名: `DevNextDB`、接続文字列キー: `SiteConnection`
  - 接続設定: 各プロジェクトの `appsettings.json`
  - DB作成・Seed投入は `DbMigrationRunner` を実行（`EnsureCreatedAsync` 使用）
  - **マイグレーションファイルは使用しない**
  - テーブル・カラムを追加・変更した場合は `DbMigrationRunner` を再実行すること

  ### Seed データ（初期ユーザー）

  | UserName | Email | Password | Role |
  |---|---|---|---|
  | admin1@sample.jp | admin1@sample.jp | Admin1! | Admin |
  | member1@sample.jp | member1@sample.jp | Member1! | Member |

  ---

  ## コマンドリファレンス

  | 用途 | コマンド |
  |------|---------|
  | ビルド | `cd DevNext && dotnet build` |
  | 開発サーバー起動 | `cd DevNext && dotnet run` |
  | テスト実行 | `cd Tests && dotnet test` |
  | DB初期化（作成・Seed） | `cd DbMigrationRunner && dotnet run` |

  ---

  ## 実装計画の読み方

  実装タスクは `docs/superpowers/plans/YYYY-MM-DD-<feature>.md` に保存されています。
  作業開始時は必ず該当する計画ファイルを読み、チェックボックス（`- [ ]`）を順番に実行してください。

  ---

  ## 実装完了後の必須手順

  実装が完了したら必ず以下を実行してください：

  1. ビルドが通ることを確認: `cd DevNext && dotnet build`
  2. テストが通ることを確認: `cd Tests && dotnet test`
  3. レビュー依頼スクリプトを実行: `./scripts/request-review.ps1`

  > **注意**: request-review.ps1 を実行しないとレビューフェーズに進めません。
  ```

- [ ] **Step 2: AGENTS.md が正しく作成されたか確認する**

  ```
  cat AGENTS.md | head -5
  ```
  期待出力: `# DevNext — Agent Guidelines` が表示される

- [ ] **Step 3: コミットする**

  ```bash
  git add AGENTS.md
  git commit -m "feat: AGENTS.md を追加（Codex向け共通ルール）"
  ```

---

## Task 2: CLAUDE.md に @AGENTS.md 参照を追加する

**Files:**
- Modify: `CLAUDE.md` (先頭に1行追加)

- [ ] **Step 1: CLAUDE.md の先頭に @AGENTS.md を追加する**

  `CLAUDE.md` の現在の先頭行 `# DevNext 開発ガイド` の**前**に以下を追加する：

  ```markdown
  @AGENTS.md

  # DevNext 開発ガイド
  ```

- [ ] **Step 2: 確認する**

  ```
  head -3 CLAUDE.md
  ```
  期待出力:
  ```
  @AGENTS.md

  # DevNext 開発ガイド
  ```

- [ ] **Step 3: コミットする**

  ```bash
  git add CLAUDE.md
  git commit -m "feat: CLAUDE.md に @AGENTS.md 参照を追加"
  ```

---

## Task 3: scripts/start-codex.ps1 を作成する

**Files:**
- Create: `scripts/start-codex.ps1`

- [ ] **Step 1: scripts/ ディレクトリを確認する**

  ```bash
  ls scripts/ 2>/dev/null || echo "scripts/ が存在しません（新規作成します）"
  ```

- [ ] **Step 2: start-codex.ps1 を作成する**

  `scripts/start-codex.ps1` を以下の内容で作成する：

  ```powershell
  <#
  .SYNOPSIS
      Claude Codeが設計完了後にCodexを起動するスクリプト。
      Windows Terminal の新タブでCodexを起動し、指定した計画ファイルを最初のプロンプトとして渡す。

  .PARAMETER PlanFile
      実装させる計画ファイルのパス。例: docs/superpowers/plans/2026-03-30-my-feature.md

  .EXAMPLE
      ./scripts/start-codex.ps1 docs/superpowers/plans/2026-03-30-my-feature.md
  #>
  param(
      [Parameter(Mandatory = $true)]
      [string]$PlanFile
  )

  # 計画ファイルの存在確認
  if (-not (Test-Path $PlanFile)) {
      Write-Error "計画ファイルが見つかりません: $PlanFile"
      exit 1
  }

  # リポジトリルートを取得（このスクリプトの親ディレクトリ）
  $repoRoot = Split-Path -Parent $PSScriptRoot
  $absolutePlanFile = Join-Path $repoRoot $PlanFile

  # Codexに渡すプロンプト
  # AGENTS.md を参照した上で計画ファイルを読んで実装するよう指示する
  $prompt = "AGENTS.md のルールに従って、以下の実装計画を実行してください: $absolutePlanFile`n実装完了後は必ず scripts/request-review.ps1 を実行してください。"

  Write-Host "Codexを起動します..."
  Write-Host "計画ファイル: $absolutePlanFile"

  # Windows Terminal の新タブでCodexを起動
  # --pos を使ってプロンプトをCodexに渡す
  wt new-tab --startingDirectory $repoRoot powershell -NoExit -Command "codex '$prompt'"
  ```

- [ ] **Step 3: スクリプトの動作確認（ドライラン）**

  実際にCodexを起動せず構文チェックのみ行う：

  ```powershell
  powershell -Command "& { . './scripts/start-codex.ps1'; Write-Host 'syntax OK' }" 2>&1
  ```

  エラーが出なければOK（引数未指定のエラーは正常）

- [ ] **Step 4: コミットする**

  ```bash
  git add scripts/start-codex.ps1
  git commit -m "feat: start-codex.ps1 を追加（Codex起動スクリプト）"
  ```

---

## Task 4: scripts/request-review.ps1 を作成する

**Files:**
- Create: `scripts/request-review.ps1`

- [ ] **Step 1: request-review.ps1 を作成する**

  `scripts/request-review.ps1` を以下の内容で作成する：

  ```powershell
  <#
  .SYNOPSIS
      Codexが実装完了後にClaude Codeのレビューを自動起動するスクリプト。
      git diff で変更内容を収集し、claude --print でレビュー指示を送る。

  .EXAMPLE
      ./scripts/request-review.ps1
  #>

  # リポジトリルートに移動
  $repoRoot = Split-Path -Parent $PSScriptRoot
  Set-Location $repoRoot

  # git diff でステージング済み＋未ステージの変更ファイル一覧を取得
  $diffStat = git diff HEAD --stat 2>&1
  $diffContent = git diff HEAD 2>&1

  # 変更がなければ終了
  if (-not $diffStat) {
      Write-Host "変更がありません。レビューをスキップします。"
      exit 0
  }

  Write-Host "変更ファイル:"
  Write-Host $diffStat
  Write-Host ""
  Write-Host "Claude Code によるレビューを開始します..."

  # Claude Code に渡すレビュー指示
  $reviewPrompt = @"
  以下のコード変更をレビューしてください。

  ## 変更ファイル一覧
  $diffStat

  ## 変更内容（diff）
  $diffContent

  ## レビュー観点
  AGENTS.md のコーディングルールに基づき、以下の軸で評価してください：
  1. セキュリティ（認可チェック・XSS・SQLインジェクション・CSRF）
  2. 保守性（コメント量・命名・単一責任・AGENTS.mdのルール準拠）
  3. 機能完成度（実装計画のチェックボックスをすべて満たしているか）

  問題があれば具体的な修正箇所を指摘してください。
  問題なければ「レビュー完了：承認」と出力してください。
  "@

  # claude --print でレビューを実行（結果を標準出力に表示）
  $reviewPrompt | claude --print
  ```

- [ ] **Step 2: スクリプトの構文確認**

  ```powershell
  powershell -Command "Get-Content scripts/request-review.ps1 | Out-Null; Write-Host 'syntax check passed'"
  ```

  期待出力: `syntax check passed`

- [ ] **Step 3: コミットする**

  ```bash
  git add scripts/request-review.ps1
  git commit -m "feat: request-review.ps1 を追加（Claude Codeレビュー自動起動）"
  ```

---

## Task 5: 動作確認

**Files:**
- 変更なし（確認のみ）

- [ ] **Step 1: AGENTS.md が Codex の上限（32KiB）以内か確認する**

  ```bash
  wc -c AGENTS.md
  ```

  32768バイト（32KiB）以下であればOK

- [ ] **Step 2: CLAUDE.md の先頭が正しいか確認する**

  ```bash
  head -3 CLAUDE.md
  ```

  期待出力:
  ```
  @AGENTS.md

  # DevNext 開発ガイド
  ```

- [ ] **Step 3: scripts/ の内容を確認する**

  ```bash
  ls scripts/
  ```

  期待出力: `request-review.ps1` と `start-codex.ps1` が存在する

- [ ] **Step 4: ビルドが通ることを確認する（既存コードへの影響がないことを検証）**

  ```bash
  cd DevNext && dotnet build
  ```

  期待出力: `Build succeeded.`
