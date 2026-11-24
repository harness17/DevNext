# Git 操作ルール（DevNext 固有追加）

基本ルール（`git add -A` 禁止・個別ファイル指定・Permission denied 対処・ブランチ運用）はグローバル `~/.claude/rules/git-ops.md` を参照。ここは DevNext 固有の追加事項のみ。

## dotnet プロセスのファイルロック

`git add` で Permission denied が出る典型原因は、`Site.exe`（または Sample 実行ファイル）が dotnet プロセスにロックされていること。

```powershell
# まず当該プロセスを止めてから再実行する
Stop-Process -Name "dotnet" -Force -ErrorAction SilentlyContinue
Stop-Process -Name "Site" -Force -ErrorAction SilentlyContinue
```

バックグラウンド起動の `dotnet run` を落とし忘れていないかも確認する（`rules/browser-test.md` の後始末ルール）。

## CLAUDE.md のコミット

`.gitignore` に `/.claude/**` パターンがあるが、`CLAUDE.md` は `git add -f` で force-track 済み。通常の `git add CLAUDE.md` で追加できる。

## マージ運用

`master` への直接コミット禁止・マージ前チェックリストは `rules/branching-and-merge.md` を参照。
