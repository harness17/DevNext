# データベースルール

- **SQL Server**、DB名: `DevNextDB`、接続文字列キー: `SiteConnection`
- 接続設定: 各プロジェクトの `appsettings.json`
- **EF Core Migrations** を使用してスキーマ管理する
- DB 作成・マイグレーション適用・Seed 投入はアプリ起動時に自動実行される（`Program.cs` の `MigrateAsync` + `SeedAsync`）
- テーブル・カラムを追加・変更した場合は `/add-entity` スキルを使用すること

## マイグレーション操作

| 用途 | コマンド |
|------|---------|
| マイグレーション追加 | `cd H:/ClaudeCode/DevNext && dotnet ef migrations add <名前> --project DevNext` |
| DB に適用 | `cd H:/ClaudeCode/DevNext && dotnet ef database update --project DevNext` |
| 適用済み一覧 | `cd H:/ClaudeCode/DevNext && dotnet ef migrations list --project DevNext` |
| 最後のマイグレーションを削除 | `cd H:/ClaudeCode/DevNext && dotnet ef migrations remove --project DevNext` |

## Seed データ（初期ユーザー）

`Program.cs` の `SeedAsync` で投入される。ユーザーが0件のときのみ実行。

| UserName | Email | Password | Role |
|---|---|---|---|
| admin1@sample.jp | admin1@sample.jp | Admin1! | Admin |
| member1@sample.jp | member1@sample.jp | Member1! | Member |

## Sample プロジェクトの DB

各 Sample は独自の DBContext を持ち、`EnsureCreatedAsync` で起動時にテーブルを作成する。
マイグレーション管理対象外。
