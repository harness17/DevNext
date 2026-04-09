# よく使うコマンド

## 開発

| 用途 | コマンド |
|------|---------|
| ビルド | `cd H:/ClaudeCode/DevNext && dotnet build DevNext.slnx` |
| 開発サーバー起動 | `cd DevNext && dotnet run` |
| テスト実行 | `cd Tests && dotnet test` |
| マイグレーション追加 | `cd H:/ClaudeCode/DevNext && dotnet ef migrations add <名前> --project DevNext` |
| DB に適用（手動） | `cd H:/ClaudeCode/DevNext && dotnet ef database update --project DevNext` |

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
| agent-browser インストール | `./scripts/install-agent-browser.ps1` |
