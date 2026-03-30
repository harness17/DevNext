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
DbMigrationRunner/         # DB作成・Seedデータ投入ツール
BatchSample/               # バッチ処理サンプル
Tests/                     # ユニットテスト
Samples/
  DatabaseSample/          # CRUD・Excel/PDF・一括編集サンプル（独立 Web アプリ）
  FileSample/              # ファイルアップロード・ダウンロードサンプル（独立 Web アプリ）
  MailSample/              # メール送信サンプル（独立 Web アプリ）
  WizardSample/            # 多段階フォームサンプル（独立 Web アプリ）
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
| PDF生成（DatabaseSample） | QuestPDF | 2026.x |
| テスト | xUnit / Moq | 2.9.x / 4.20.x |
| カレンダー UI | FullCalendar | 6.1.x |

---

## クイックスタート

```bash
# 1. DB 初期化（テーブル作成・Seed 投入）
cd DbMigrationRunner && dotnet run

# 2. コア起動
cd DevNext && dotnet run

# 3. テスト
cd Tests && dotnet test
```

詳細は [docs/setup.md](docs/setup.md) を参照してください。

### 初期ユーザー

| Email | Password | Role |
|---|---|---|
| admin1@sample.jp | Admin1! | Admin |
| member1@sample.jp | Member1! | Member |

---

## コア機能

### 認証・アカウント管理

- ログイン / ログアウト
- ユーザー登録・パスワードリセット・パスワード変更
- ロールベースアクセス制御（Admin / Member）
- 5回連続失敗で5分間ロックアウト

**パスワードポリシー**: 最低6文字・大文字・小文字・数字・記号すべて必須

### ユーザー・ロール管理（Admin限定）

- ユーザー一覧（検索・ページング）・編集・削除
- ロールの付与・剥奪

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

| プロジェクト | 内容 | 参照レシピ |
|---|---|---|
| DatabaseSample | CRUD・ページング・ソート・一括編集・Excel/PDF エクスポート | [excel-export](docs/recipes/excel-export.md) / [pdf-export](docs/recipes/pdf-export.md) / [bulk-edit](docs/recipes/bulk-edit.md) |
| FileSample | ファイルアップロード・ダウンロード・削除 | [file-upload](docs/recipes/file-upload.md) |
| MailSample | テンプレートメール送信・送信ログ | — |
| WizardSample | 多段階フォーム（TempData を使ったステップ間状態保持） | [wizard](docs/recipes/wizard.md) |

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

## このプロジェクトについて

実際の業務案件で得た知見をもとに、汎用的に再利用できる Web アプリケーション基盤として整理したものです。
「次の案件でもそのまま使える」ことを意識し、認証・CRUD・共通ライブラリなど、どの案件でも必要になる機能を一通り組み込んでいます。

ClaudeCode の支援のもと作成。
