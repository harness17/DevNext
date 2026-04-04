# よく使うコマンド

## 開発

| 用途 | コマンド |
|------|---------|
| ビルド | `cd DevNext && dotnet build` |
| 開発サーバー起動 | `cd DevNext && dotnet run` |
| テスト実行 | `cd Tests && dotnet test` |
| DB 初期化（作成・Seed） | `cd DbMigrationRunner && dotnet run` |

## デプロイ

| 用途 | コマンド |
|------|---------|
| 発行（Release） | `cd DevNext && dotnet publish -c Release -o <出力パス>` |
| AppPool 停止 | `Stop-WebAppPool -Name 'DevNext'` |
| AppPool 起動 | `Start-WebAppPool -Name 'DevNext'` |

## ユーティリティ

| 用途 | コマンド |
|------|---------|
| smtp4dev 起動 | `smtp4dev-start.ps1` |
| ビルド＋テスト一括 | `/verify` スキルを使用 |
