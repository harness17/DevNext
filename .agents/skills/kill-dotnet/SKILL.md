---
name: kill-dotnet
description: 実行中の dotnet プロセスを停止してビルドを通す。Site.exe がロックされてビルドが失敗するときに使用する。
---

実行中の dotnet プロセスを停止して、ビルドを再実行してください。

## 手順

### Step 1: ロック中のプロセスを確認する

```bash
# 実行中の dotnet プロセス一覧
Get-Process dotnet -ErrorAction SilentlyContinue | Select-Object Id, ProcessName, Path
```

### Step 2: dotnet プロセスを停止する

```bash
# 名前で一括停止（開発サーバーが起動中の場合）
Stop-Process -Name "dotnet" -Force -ErrorAction SilentlyContinue
```

特定の PID だけ停止したい場合：
```bash
Stop-Process -Id <PID> -Force
```

### Step 3: ビルドを再実行する

```bash
cd H:/ClaudeCode/DevNext && dotnet build DevNext.slnx
```

## よくあるケース

| 症状 | 原因 |
|------|------|
| `Access to the path 'Site.exe' is denied` | `dotnet run` が起動中で DLL をロックしている |
| `git add` で `Permission denied` | dotnet プロセスが git オブジェクト DB の一部をロックしている |
| ビルドは成功するが変更が反映されない | 古いプロセスがまだ旧 DLL を使用している |

## 注意

- `Stop-Process -Name "dotnet"` は **すべての** dotnet プロセスを停止する
- 他のプロジェクトの dotnet サーバーも停止するため、必要なら PID 指定で個別に止める
- 停止後は `dotnet run` で再起動が必要
