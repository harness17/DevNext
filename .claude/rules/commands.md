# よく使うコマンド

## 開発

| 用途 | コマンド |
|------|---------|
| ビルド | `cd H:/ClaudeCode/DevNext && dotnet build DevNext.sln` |
| 開発サーバー起動 | `cd DevNext && dotnet run` |
| テスト実行 | `cd Tests && dotnet test` |
| DB 更新（変更検出→生成→適用） | `./scripts/db-update.ps1` |
| DB 更新（名前指定） | `./scripts/db-update.ps1 -Name AddXxx` |

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
