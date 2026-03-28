# DevNext

ASP.NET Core 10 製の Web アプリケーションテンプレート。旧 .NET Framework 版（DevNet）からの移行・リニューアルプロジェクト。
ClaudeCodeの支援の元作成

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
| Excel処理 | ClosedXML（MIT ライセンス、商用利用無償） | 0.104.2 |
| CSV処理 | CsvHelper | 33.0.1 |
| PDF生成 | QuestPDF | 2026.2.3 |
| JSON | Newtonsoft.Json | 13.0.3 |
| テスト | xUnit / Moq | 2.9.3 / 4.20.72 |
| グラフ描画 | Chart.js | 4.4.7 |

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
- 一括登録 / 一括編集（親＋子エンティティをまとめて登録・編集、子行を動的に追加・削除）
- 親子関係 CRUD（詳細画面から子エンティティの追加・編集・削除）
- 編集・削除後の一覧復帰で検索条件・ページ位置を再現
- ファイルアップロード
- Excel インポート（XLSX）
- Excel エクスポート
- PDF エクスポート（一覧 / 単体）
  - 単体 PDF には子エンティティ一覧を含む
  - 単体 PDF では PNG・JPEG・JPG は画像として埋め込み

### ファイル管理

- ファイルアップロード（サイズ・拡張子バリデーション）
- ファイル一覧表示（検索・ページング）
- ファイルダウンロード / 削除
- ファイルメタデータの DB 管理

### メール送信サンプル

- テンプレートベースのメール送信（プレースホルダー置換）
- SMTP 設定（`appsettings.json` で管理）
- smtp4dev による開発時の擬似送受信

### ユーザー・ロール管理（Admin限定）

- ユーザー一覧表示（検索・ページング）
- ユーザー情報編集（メールアドレス・ユーザー名）
- ユーザー削除（初期 Admin ユーザーは削除不可）
- ロールの付与・剥奪（Admin / Member）

### 承認ワークフロー

- 申請の作成・編集・削除（下書き保存 / 即時申請）
- 申請ステータス管理（Draft → Pending → Approved / Rejected）
- 申請一覧（Admin: 全件表示、Member: 自分の申請のみ）
- 承認・却下（Admin 限定、コメント入力可）
- 申請時は Admin 全員に、承認・却下時は申請者にベルアイコン通知
- CSV・Excel エクスポート（検索条件を維持して全件出力）

### ダッシュボード

- サマリーカード（DBサンプル / メール / ファイル / 多段階フォームの件数）
- Chart.js による5種のグラフ（折れ線・ドーナツ・棒・横棒）
- 直近30日のメール送信数推移、送信成功/失敗比率、EnumData分布、ファイル種別分布、Wizardカテゴリ分布
- 30秒間隔の Ajax ポーリングによるリアルタイム更新

### 多段階フォーム（ウィザード）

- セッションを利用したステップ間データ保持
- 確認画面 → 完了画面の一連フロー

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
│   ├── DBContext.cs              # EF Core コンテキスト（Identity統合）
│   ├── EnumDefine.cs             # Enum定義
│   ├── ConstDefine.cs            # 定数定義（ファイルパス等）
│   ├── Email.cs                  # SMTP メール送信
│   └── localutil.cs              # 共通ユーティリティ
├── Controllers/
│   ├── HomeController.cs         # ホーム画面
│   ├── AccountController.cs      # 認証（ログイン・登録・パスワードリセット）
│   ├── ManageController.cs       # アカウント管理（パスワード変更）
│   ├── DatabaseSampleController.cs  # DBサンプル CRUD
│   ├── FileManagementController.cs  # ファイル管理
│   ├── MailSampleController.cs      # メール送信サンプル
│   ├── WizardSampleController.cs    # 多段階フォーム
│   ├── ViewSampleController.cs      # UIパターンサンプル
│   ├── DashboardController.cs        # ダッシュボード・グラフデータ API
│   ├── UserManagementController.cs  # ユーザー・ロール管理（Admin限定）
│   ├── ApprovalRequestController.cs # 承認ワークフロー
│   ├── NotificationController.cs    # 通知 Ajax API
│   └── RootErrorController.cs       # エラーハンドリング
├── Entity/                       # エンティティ定義
├── Models/                       # ビューモデル
├── Service/                      # ビジネスロジック
│   ├── DatabaseSampleService.cs  # CRUD・Excel/PDF 出力
│   ├── FileManagementService.cs  # ファイル管理
│   ├── MailSampleService.cs      # メール送信
│   ├── WizardSampleService.cs    # 多段階フォーム
│   ├── DashboardService.cs        # ダッシュボード集計
│   ├── UserManagementService.cs  # ユーザー・ロール管理
│   ├── CommonService.cs          # 共通サービス
│   ├── ApprovalWorkflowService.cs # 承認ワークフロー（状態遷移・通知トリガー）
│   ├── NotificationService.cs    # 通知の作成・取得・既読更新
│   └── ExportService.cs          # 承認申請 CSV / Excel エクスポート
├── Repository/                   # データアクセス層
│   ├── SampleEntityRepository.cs
│   ├── SampleEntityChildRepository.cs
│   ├── FileEntityRepository.cs
│   ├── WizardEntityRepository.cs
│   ├── ApprovalRequestRepository.cs
│   └── NotificationRepository.cs
├── Views/                        # Razor ビュー
└── Program.cs                    # DI・ミドルウェア設定

CommonLibrary/
├── Attributes/               # カスタム属性（AccessLog・FileValidation）
├── Batch/                    # バッチ処理基盤
├── Common/                   # ユーティリティ・ロガー・ページング
├── Entity/                   # エンティティ基底クラス
├── Extensions/               # 拡張メソッド
└── Repository/               # リポジトリ基底クラス
```

---

## ビルドパフォーマンス

Debug ビルドでは以下の最適化が適用されており、ビルド時間を大幅に短縮しています。

| 設定 | 効果 |
|---|---|
| `RunAnalyzers=false` | Roslyn アナライザー・ソースジェネレーター無効化（最大の効果） |
| `StaticWebAssetsEnabled=false` | Static Web Assets マニフェスト生成スキップ |
| `RazorCompileOnBuild=false` | Razor ビルドタスクスキップ（RuntimeCompilation が実行時コンパイル） |
| `SatelliteResourceLanguages=en` | 多言語サテライトアセンブリ生成を英語のみに限定 |

**Release ビルドはすべての最適化が無効** になり、通常のビルドが実行されます。

| ビルド種別 | 所要時間（目安） |
|---|---|
| Debug クリーンビルド | 約 1.7 秒 |
| Debug 差分ビルド（変更なし） | 約 1.1 秒 |
| ベースライン（最適化前） | 約 11 秒 |

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

---

## このプロジェクトについて

### 背景・目的

実際の業務案件で得た知見をもとに、汎用的に再利用できる Web アプリケーション基盤として整理したものです。
「次の案件でもそのまま使える」ことを意識し、認証・CRUD・共通ライブラリなど、どの案件でも必要になる機能を一通り組み込んでいます。

案件ごとに継ぎ足して育てることを前提に設計しており、特定のビジネスロジックには依存しない構造にしています。

### 設計上のこだわり

- **CommonLibrary の切り出し**: アプリ本体と共通機能を分離し、別プロジェクトへの流用を容易にしています
- **保守性の優先**: 追加・変更が発生しやすい箇所（リポジトリ・サービス層・ViewModel）を明確に分離し、影響範囲を局所化しています
- **旧バージョン（DevNet）との比較**: .NET Framework 版を参照用として同梱しており、移行前後の差分を確認できます

### 学んだこと・意識したこと

- .NET Framework から ASP.NET Core への移行における非互換点の洗い出しと対応
- テスト可能な設計（DI・モック）を最初から意識した構造化
- 実務で繰り返し発生するパターン（ページング・論理削除・アクセスログ等）の共通化と再利用
