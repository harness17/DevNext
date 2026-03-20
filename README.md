# DevNext

ASP.NET Core 10 製の Web アプリケーションテンプレート。旧 .NET Framework 版（DevNet）からの移行・リニューアルプロジェクト。

## 概要

- ASP.NET Core 10 MVC アーキテクチャ
- SQL Server + Entity Framework Core によるデータアクセス
- ASP.NET Core Identity による認証・認可
- 共通ライブラリ（CommonLibrary）による機能の共通化
- バッチ処理・DB初期化ツールを同梱

---

## プロジェクト構成

```
DevNext/
├── DevNext/            # メイン Web アプリ（RootNamespace: Site）
├── CommonLibrary/      # 共通ライブラリ（RootNamespace: Dev.CommonLibrary）
├── DbMigrationRunner/  # DB作成・Seedデータ投入ツール
├── BatchSample/        # バッチ処理サンプル
├── Tests/              # ユニットテスト
└── DevNet/             # 旧 .NET Framework 版（参照用）
```

---

## 技術スタック

| カテゴリ | ライブラリ | バージョン |
|---|---|---|
| フレームワーク | .NET / ASP.NET Core | 10.0 |
| ORM | Entity Framework Core / SQL Server | 10.0.0 |
| 認証 | ASP.NET Core Identity | 10.0.0 |
| オブジェクトマッピング | AutoMapper | 16.1.1 |
| Excel処理 | EPPlus | 7.5.2 |
| CSV処理 | CsvHelper | 33.0.1 |
| PDF生成 | QuestPDF | 2026.2.3 |
| JSON | Newtonsoft.Json | 13.0.3 |
| テスト | xUnit / Moq | 2.9.3 / 4.20.72 |

---

## セットアップ

### 前提条件

- .NET 10.0 SDK
- SQL Server（localhost で起動済み）
- `admin` ユーザーが SQL Server 認証で作成済み

### 1. データベース作成・Seed投入

```bash
cd DbMigrationRunner
dotnet run
```

`DbMigrationRunner` は以下を自動実行します。

- `DevNextDB` データベースの作成（`EnsureCreatedAsync`）
- 初期ロール・ユーザーの投入

**初期ユーザー**

| UserName | Password | Role |
|---|---|---|
| admin1@sample.jp | `Admin1!` | Admin |
| member1@sample.jp | `Member1!` | Member |

接続文字列は `DbMigrationRunner/appsettings.json` で設定します。

```json
{
  "ConnectionStrings": {
    "SiteConnection": "Server=localhost;Database=DevNextDB;Integrated Security=False;User ID=admin;Password=admin;TrustServerCertificate=True;"
  }
}
```

### 2. Web アプリ起動

```bash
cd DevNext
dotnet run
```

### 3. テスト実行

```bash
cd Tests
dotnet test
```

---

## 主な機能

### 認証・アカウント管理

- ログイン / ログアウト
- ユーザー登録
- パスワードリセット（Forgot Password）
- アカウント管理（パスワード変更）
- ロールベースアクセス制御（Admin / Member）
- 5回連続失敗で5分間ロックアウト

**パスワードポリシー**

| 条件 | 設定値 |
|---|---|
| 最低文字数 | 6文字 |
| 大文字 | 必須 |
| 小文字 | 必須 |
| 数字 | 必須 |
| 記号 | 必須 |

### DBサンプル（CRUD）

- 一覧表示（検索・ページング・ソート）
- 新規作成 / 編集 / 削除（論理削除）
- ファイルアップロード
- Excel インポート（XLSX）
- Excel エクスポート
- PDF エクスポート（一覧 / 単体）
  - 単体PDFでは PNG・JPEG・JPG は画像として埋め込み

### 共通機能（CommonLibrary）

- ジェネリックリポジトリ（CRUD・バッチ挿入・論理削除・履歴管理）
- アクセスログ属性（`[ServiceFilter(typeof(AccessLogAttribute))]`）
- バッチサービス（Mutex による多重起動防止）
- ページング・ソートユーティリティ
- ファイル検証属性（サイズ・拡張子）
- Enum ユーティリティ（表示名・ドロップダウン生成）

---

## ディレクトリ構造

```
DevNext/
├── Common/
│   ├── DBContext.cs          # EF Core コンテキスト（Identity統合）
│   ├── EnumDefine.cs         # Enum定義
│   ├── ConstDefine.cs        # 定数定義（ファイルパス等）
│   └── localutil.cs          # 共通ユーティリティ
├── Controllers/
│   ├── AccountController.cs  # 認証
│   ├── ManageController.cs   # アカウント管理
│   ├── DatabaseSampleController.cs  # DBサンプルCRUD
│   ├── ViewSampleController.cs      # UIパターンサンプル
│   └── HomeController.cs
├── Entity/                   # エンティティ定義
├── Models/                   # ビューモデル
├── Service/                  # ビジネスロジック
├── Repository/               # データアクセス層
├── Views/                    # Razor ビュー
├── Migrations/               # EF Core マイグレーション
└── Program.cs                # DI・ミドルウェア設定

CommonLibrary/
├── Attributes/               # カスタム属性（AccessLog・FileValidation）
├── Batch/                    # バッチ処理基盤
├── Common/                   # ユーティリティ・ロガー・ページング
├── Entity/                   # エンティティ基底クラス
├── Extensions/               # 拡張メソッド
└── Repository/               # リポジトリ基底クラス
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

`AddAutoMapper` のアセンブリスキャンも変更されています。

```csharp
// Before (13.x)
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// After (16.x)
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));
```

### テストの SignInResult 競合

`Microsoft.AspNetCore.Identity.SignInResult` と `Microsoft.AspNetCore.Mvc.SignInResult` が競合するため、エイリアスを使用します。

```csharp
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;
```
