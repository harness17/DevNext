# データベースルール

- **SQL Server**、DB名: `DevNextDB`、接続文字列キー: `SiteConnection`
- 接続設定: 各プロジェクトの `appsettings.json`
- DB 作成・Seed 投入は `DbMigrationRunner` を実行（`EnsureCreatedAsync` 使用）
- **マイグレーションファイルは使用しない**
- テーブル・カラムを追加・変更した場合は `/add-entity` スキルを使用すること

## Seed データ（初期ユーザー）

| UserName | Email | Password | Role |
|---|---|---|---|
| admin1@sample.jp | admin1@sample.jp | Admin1! | Admin |
| member1@sample.jp | member1@sample.jp | Member1! | Member |
