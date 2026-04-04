---
name: iis-deploy
description: DevNext を IIS にデプロイする。ビルド→パブリッシュ→AppPool停止→ファイル配置→AppPool起動 の手順を実行する。
disable-model-invocation: true
---

以下の手順で DevNext を IIS にデプロイしてください。

## デプロイ設定

- **AppPool 名**: `DevNext`
- **IIS 物理パス**: `C:/inetpub/wwwroot/DevNext`
- **パブリッシュ出力先**: `H:/ClaudeCode/DevNext/publish_output`（一時フォルダ）

## 手順

### 1. ビルド確認
```bash
cd H:/ClaudeCode/DevNext && dotnet build -c Release
```
エラーがあれば中止してユーザーに報告する。

### 2. パブリッシュ
```bash
cd H:/ClaudeCode/DevNext && dotnet publish DevNext/DevNext.csproj -c Release -o publish_output
```

### 3. AppPool 停止
```powershell
Import-Module WebAdministration; Stop-WebAppPool -Name 'DevNext'; Start-Sleep -Seconds 2; Write-Host 'Stopped'
```

### 4. ファイル配置

パブリッシュ出力（`publish_output/`）の内容を IIS パス（`C:/inetpub/wwwroot/DevNext`）にコピーする。
- `appsettings.json` など本番設定ファイルは上書きしないよう注意する
- `DataProtectionKeys/` フォルダは保持すること

### 5. AppPool 起動
```powershell
Import-Module WebAdministration; Start-WebAppPool -Name 'DevNext'; Write-Host 'Started'
```

### 6. 動作確認

AppPool が起動していることを確認し、デプロイ完了を報告する。

## 注意事項

- デプロイ前にユーザーへ確認を取ること（本番環境への変更のため）
- `appsettings.Production.json` や `DataProtectionKeys/` は上書きしない
- デプロイ後に IIS のサイトへのアクセスが正常か確認を促す
