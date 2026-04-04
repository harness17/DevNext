# Git 操作ルール

## git add は個別ファイル指定

`git add -A` や `git add .` は使用しない。

**理由:**
- `.gitignore` 済みのファイルを誤って含める可能性がある
- git オブジェクト DB がロック中の場合に `Permission denied` エラーになりやすい
- 意図しないファイル（`appsettings.Development.json` など）がコミットに混入するリスク

**正しいやり方:**

```bash
# ファイルを個別に指定する
git add CLAUDE.md .claude/rules/di-and-password.md DevNext/Program.cs

# .claude/ 配下を一括追加する場合は安全なパスを明示する
git add .claude/rules/ .claude/skills/
```

## git add で Permission denied が出たとき

git オブジェクト DB がロックされている場合。dotnet プロセスが `Site.exe` を掴んでいることが多い。

```bash
# 方法1: 実行中の dotnet プロセスを停止してから再試行
Stop-Process -Name "dotnet" -Force -ErrorAction SilentlyContinue

# 方法2: 特定の PID を停止
Stop-Process -Id <PID> -Force
```

## CLAUDE.md のコミット

`CLAUDE.md` は `.gitignore` に `/.claude/**` パターンが設定されているが、`git add -f` でforce-track済み。
通常の `git add CLAUDE.md` で追加できる。

## ブランチルール

- 開発は `develop` ブランチで行う
- `master` への直接コミット禁止
- 詳細は `branching-and-merge.md` を参照
