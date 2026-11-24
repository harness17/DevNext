# DevNext

ASP.NET Core 10 製の Web アプリケーション **コアテンプレート**。
新案件の出発点として使うことを目的に設計されています。

> セットアップ詳細 → [docs/setup.md](docs/setup.md)
> 新案件カスタマイズ → [docs/customization.md](docs/customization.md)
> 実装パターン集 → [docs/recipes/](docs/recipes/)

---

## 概要

- ASP.NET Core 10 MVC アーキテクチャ
- SQL Server + Entity Framework Core によるデータアクセス
- ASP.NET Core Identity による認証・認可
- 共通ライブラリ（CommonLibrary）による機能の共通化
- 独立したサンプルプロジェクト群（`Samples/`）を同梱

---

## プロジェクト構成

```
DevNext/                   # コアのみ（認証・ユーザー管理・承認・スケジュール）
CommonLibrary/             # 共通ライブラリ（RootNamespace: Dev.CommonLibrary）
BatchSample/               # バッチ処理サンプル
Tests/                     # ユニットテスト
Samples/
  DatabaseSample/          # CRUD・Excel/PDF・一括編集サンプル（独立 Web アプリ）
  FileSample/              # ファイルアップロード・ダウンロードサンプル（独立 Web アプリ）
  MailSample/              # メール送信サンプル（独立 Web アプリ）
  WizardSample/            # 多段階フォームサンプル（独立 Web アプリ）
  ExcelSample/             # Excel エクスポート・インポートサンプル（独立 Web アプリ、ClosedXML 使用）
  ApiSample/               # REST API + JWT 認証 + Swagger サンプル（独立 Web API）
  ApiClientSample/         # ApiSample を呼び出す MVC クライアントサイト
  PdfSample/               # PDF 生成サンプル（Playwright 使用）
docs/
  setup.md                 # セットアップ手順
  customization.md         # 新案件向けカスタマイズ指針
  recipes/                 # 実装パターン集
```

---

## 技術スタック

| カテゴリ | ライブラリ | バージョン |
|---|---|---|
| フレームワーク | .NET / ASP.NET Core | 10.0 |
| ORM | Entity Framework Core / SQL Server | 10.0.0 |
| 認証 | ASP.NET Core Identity | 10.0.0 |
| オブジェクトマッピング | AutoMapper | 16.1.1 |
| Excel処理（DatabaseSample） | ClosedXML | 0.104.x |
| CSV処理（DatabaseSample） | CsvHelper | 33.x |
| PDF生成（DatabaseSample / PdfSample） | Microsoft.Playwright | 1.59.x |
| テスト | xUnit / Moq | 2.9.x / 4.20.x |
| カレンダー UI | FullCalendar | 6.1.x |
| REST API ドキュメント（ApiSample） | Swashbuckle（Swagger UI） | 8.1.x |
| JWT 認証（ApiSample） | Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.0 |

---

## クイックスタート

```bash
# 1. DB マイグレーション適用（初回のみ）
cd H:/ClaudeCode/DevNext && dotnet ef database update --project DevNext

# 2. コア起動（マイグレーション適用・Seed 投入は起動時に自動実行）
cd DevNext && dotnet run

# 3. テスト
cd Tests && dotnet test
```

> **Note**: 手順 1 は初回セットアップ時のみ必要です。  
> 以降のマイグレーション追加時は `dotnet ef migrations add <名前>` → `dotnet ef database update` を実行してください。

詳細は [docs/setup.md](docs/setup.md) を参照してください。

### 初期ユーザー

| Email | Password | Role |
|---|---|---|
| admin1@sample.jp | Admin1! | Admin |
| member1@sample.jp | Member1! | Member |

---

## コア機能

### 認証・アカウント管理

- メールアドレスでのログイン / ログアウト（ユーザー名 ≠ メールでも可）
- ユーザー登録・パスワードリセット・パスワード変更
- ロールベースアクセス制御（Admin / Member）
- 5回連続失敗で5分間ロックアウト

**パスワードポリシー**: 最低6文字・大文字・小文字・数字・記号すべて必須

### ユーザー・ロール管理（Admin限定）

- ユーザー一覧（検索・ページング）・編集
- ロールの付与・剥奪
- 管理者によるパスワードリセット
- ユーザー無効化（ロックアウトベースの論理削除・既存セッションも失効）

### 承認ワークフロー

- 申請作成・編集・削除（下書き / 即時申請）
- ステータス管理（Draft → Pending → Approved / Rejected）
- Admin 全員への通知・申請者への結果通知

### スケジュール・カレンダー

- FullCalendar.js による月・週・日ビュー
- 個人予定・全体共有・招待予定の色分け表示
- 繰り返し設定・参加者招待

### 通知

- ナビバーベルアイコンによる未読バッジ表示（10秒ポーリング）

---

## サンプルプロジェクト（Samples/）

各サンプルは `CommonLibrary` を参照した **独立した Web アプリ**です。
DevNextDB を共有するため、同じユーザーアカウントでログインできます。

### Sample の DB セットアップ

各 Sample は EF Migrations ではなく `EnsureCreatedAsync` を使用しています。  
**起動時に自動でテーブルが作成されます**。手動操作は不要です。

Sample に新しいテーブル・カラムを追加する場合は、Sample 側の DBContext と `EnsureCreatedAsync` の呼び出しコードを更新してください。  
既存 DB に後からカラムを追加する場合は、SQL Server Management Studio 等で手動 ALTER TABLE が必要です（Sample は本番運用を想定しないため）。

| プロジェクト | 内容 | 参照レシピ |
|---|---|---|
| DatabaseSample | CRUD・ページング・ソート・一括編集・Excel/PDF エクスポート | [excel-export](docs/recipes/excel-export.md) / [pdf-export](docs/recipes/pdf-export.md) / [bulk-edit](docs/recipes/bulk-edit.md) |
| FileSample | ファイルアップロード・ダウンロード・削除 | [file-upload](docs/recipes/file-upload.md) |
| MailSample | テンプレートメール送信・送信ログ | — |
| WizardSample | 多段階フォーム（TempData を使ったステップ間状態保持） | [wizard](docs/recipes/wizard.md) |
| ExcelSample | Excel エクスポート・インポート（ClosedXML 使用） | [excel-export](docs/recipes/excel-export.md) |
| ApiSample | REST API（CRUD）+ JWT Bearer 認証 + Swagger UI（`/swagger` でアクセス） | — |
| ApiClientSample | ApiSample を呼び出す MVC クライアントサイト（ポート 5170） | — |
| PdfSample | PDF 生成（Playwright URL方式・HTML方式） | [pdf-export](docs/recipes/pdf-export.md) |

---

## ディレクトリ構造（DevNext コア）

```
DevNext/
├── Common/
│   ├── DBContext.cs              # EF Core コンテキスト（Identity統合）
│   ├── EnumDefine.cs             # Enum定義
│   └── LocalUtil.cs              # 共通ユーティリティ
├── Controllers/
│   ├── AccountController.cs      # 認証（ログイン・登録・パスワードリセット）
│   ├── ManageController.cs       # アカウント管理（パスワード変更）
│   ├── HomeController.cs         # ホーム画面
│   ├── UserManagementController.cs  # ユーザー・ロール管理（Admin限定）
│   ├── ApprovalRequestController.cs # 承認ワークフロー
│   ├── NotificationController.cs    # 通知 Ajax API
│   ├── ScheduleController.cs        # スケジュール・カレンダー
│   └── RootErrorController.cs       # エラーハンドリング
├── Entity/                       # エンティティ定義
├── Models/                       # ビューモデル
├── Service/                      # ビジネスロジック
├── Repository/                   # データアクセス層
├── Views/                        # Razor ビュー
└── Program.cs                    # DI・ミドルウェア設定
```

---

## 注意事項

### ServiceFilter の DI 登録

`[ServiceFilter(typeof(XxxAttribute))]` を使用する場合、対象クラスを `Program.cs` で登録する必要があります。

```csharp
builder.Services.AddScoped<AccessLogAttribute>();
```

### AutoMapper 16.x の変更点

`MapperConfiguration` コンストラクターに `ILoggerFactory` が必須になりました。DI 非使用箇所では `NullLoggerFactory.Instance` を使用します。

```csharp
new MapperConfiguration(cfg => cfg.CreateMap<A, B>(), NullLoggerFactory.Instance)
```

### テストの SignInResult 競合

`Microsoft.AspNetCore.Identity.SignInResult` と `Microsoft.AspNetCore.Mvc.SignInResult` が競合するため、エイリアスを使用します。

```csharp
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;
```

---

## 開発用ハーネス（AI エージェント向け開発ルール）

このリポジトリには、Claude Code や Codex などの AI エージェントが DevNext を出発点に新規開発するときのルールとスキルを同梱しています。
エージェントは作業開始時にこれらを読み込み、同じ手順・品質基準で実装します。

### 構成

| ファイル | 役割 |
|---------|------|
| `CLAUDE.md` | Claude Code の入口。`.claude/rules/` のルールを `@import` で読み込むインデックス |
| `AGENTS.md` | Codex・Cursor など他エージェント共通のガイドライン |
| `.claude/rules/` | トピック別のルール（コーディング方針・DB・テスト・ブランチ運用・CommonLibrary の責務境界など） |
| `.claude/skills/` | 定型作業を手順化したスキル（`/スキル名` で実行） |
| `CLAUDE_CODE_HANDOFF.md` | Claude Code と Codex の間の作業引き継ぎログ |

### 新規開発の基本ルール

新しい機能はゼロから書かず、**既存サンプルまたはコアの実装をベースに作る**ことを `.claude/rules/devnext-template-usage.md` で定めています。

| 作りたい機能 | ベース |
|------------|--------|
| CRUD・ページャー・検索つき一覧 | `Samples/DatabaseSample/` |
| 認証・ユーザー管理・所有者チェック | DevNext 本体（`AccountController` / `UserManagementController` / `ApprovalRequestController`） |
| 多段階フォーム | `Samples/WizardSample/` |
| ファイル・Excel・PDF・メール | `Samples/FileSample/` / `ExcelSample/` / `PdfSample/` / `MailSample/` |
| REST API・外部 API 呼び出し | `Samples/ApiSample/` / `ApiClientSample/` |
| バッチ処理 | `BatchSample/` |

該当するベースがない新しいパターンは、コアに入れず新しい Sample として `Samples/` に追加します（`/new-sample`）。

### 開発フロー

1. **完成条件の宣言**（`/sprint`）— 実装前に、ベースにするサンプル・正常系・認可・異常系・テストを完成条件として決める
2. **実装** — エンティティ追加は `/add-entity`、検索条件の復元は `/search-restore`
3. **検証**（`/verify`）— ビルド・xUnit・ブラウザでの動作確認
4. **マージ**（`/merge-master`）— コードレビュー、ページ・README・設計書の更新（`/add-page`・`/export-docs`）を経て develop → master

### 主なスキル

| スキル | 用途 |
|--------|------|
| `/sprint` | 完成条件（スプリントコントラクト）を宣言して実装を開始する |
| `/verify` | ビルド・テスト・ブラウザ確認を一括実行する |
| `/add-entity` | エンティティ（DB テーブル）追加とマイグレーション |
| `/add-page` | ページ追加後のホーム・ナビ・ドキュメント・README 更新チェック |
| `/new-sample` | 新しい Sample プロジェクトの追加 |
| `/search-restore` | 検索条件・ページ状態の復元パターンの実装 |
| `/merge-master` | develop → master マージ前の6ステップチェック |
| `/export-docs` | `docs/` の Markdown を Word に出力 |
| `/iis-deploy` | IIS へのデプロイ |
| `/kill-dotnet` | ファイルロックの原因になる dotnet プロセスの停止 |
| `/update-harness` | ルール・スキルを実装に合わせて更新する |
| `/codex-dev` `/codex-handoff` `/cross-review` | Codex への実装委譲・引き継ぎ・相互レビュー |

---

## 設計の意図

このプロジェクトでは、単に「動くコードを書く」だけでなく、**なぜその構造・技術を選んだか**を意識して設計しています。

### コアとサンプルを分離した理由

サンプル機能（Excel・ファイル・メール・ウィザード）はコア本体に混在させず `Samples/` 配下の独立プロジェクトに分離しています。
目的は「コアテンプレートをシンプルに保つ」こと。次の案件で使うとき、不要なサンプルコードに影響を受けずにカスタマイズできます。

### CommonLibrary に共通エンティティを集約した理由

`ApplicationUser`（Identity ユーザー）と `SiteEntityBase`（全エンティティ基底クラス）を CommonLibrary に配置しています。
これにより、コアと全サンプルが同じ認証ユーザー・監査カラム定義を共有でき、DB を分けずに済みます。

### サンプルに ClosedXML を選んだ理由

EPPlus は商用ライセンスが必要なため業務案件では制約になりやすい。ClosedXML は MIT ライセンスで制約がなく、.NET での Excel 処理として実績があります。

### Harness（CLAUDE.md）を自動更新する仕組みを作った理由

実装が進むにつれてドキュメントが古くなる問題を解決するため、`update-harness` スキルで Claude 自身が差分を確認して定期更新します。
「エージェントへの作業指示書が常に最新の実装と一致している」状態を維持するためのセルフメンテナンス設計です。

---

## このプロジェクトについて

実際の業務案件で得た知見をもとに、汎用的に再利用できる Web アプリケーション基盤として整理したものです。
「次の案件でもそのまま使える」ことを意識し、認証・CRUD・共通ライブラリなど、どの案件でも必要になる機能を一通り組み込んでいます。

---

## 関連記事

DevNext を題材にした、または DevNext の構成を参照している技術記事です。

### Zenn

- [ASP.NET MVCの自作HelperをASP.NET Coreに移植した話](https://zenn.dev/harness17/articles/devnext-mvc-helper-extensions) (2026-05-10)
- [FullCalendarでDTOの色が反映されない時に見たこと](https://zenn.dev/harness17/articles/fullcalendar-event-color-rendering) (2026-05-10)

### Qiita

- [Claude CodeのCLAUDE.mdを@importで分割してトピック別ルールに整理した](https://qiita.com/harnesswinner/items/6678320489deec25113a) (2026-05-24) — DevNext の `.claude/rules/` 構成を実例として参照

---

ClaudeCode の支援のもと作成。
