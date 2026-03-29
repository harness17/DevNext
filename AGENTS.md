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
