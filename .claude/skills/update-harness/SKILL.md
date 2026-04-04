---
name: update-harness
description: AGENTS.md・CLAUDE.md ルールファイル・メモリを実装に追従して自動更新するセルフメンテナンススキル。3日に1回の定期実行を想定。
---

DevNext Harness（AGENTS.md・CLAUDE.md rules・メモリ）を現在の実装に追従させて更新してください。

## 作業の流れ

### Step 1: 変更差分を把握する

前回コミット以降に何が変わったかを確認する。

```bash
git log --oneline -20
git diff HEAD~5 --stat
```

追加・変更されたファイルのパターン：
- `DevNext/Controllers/` → コントローラー追加・変更
- `DevNext/Service/` → サービス追加・変更
- `DevNext/Repository/` → リポジトリ追加・変更
- `DevNext/Entity/` → エンティティ追加・変更
- `DevNext/Program.cs` → DI登録・ミドルウェア変更
- `Samples/` → Sampleプロジェクト追加
- `Tests/` → テスト追加・変更

### Step 2: 現在の実装状態をスキャンする

以下を読んで「現在どうなっているか」を把握する。

```bash
# コントローラー一覧
ls DevNext/Controllers/

# サービス一覧
ls DevNext/Service/

# リポジトリ一覧
ls DevNext/Repository/

# Samples 一覧
ls Samples/

# Program.cs の DI 登録
grep -n "AddScoped\|AddSingleton\|AddTransient\|AddDbContext" DevNext/Program.cs

# ミドルウェアパイプライン
grep -n "^app\." DevNext/Program.cs
```

### Step 3: 現在のドキュメントを読む

更新対象ドキュメントを読んで「何が古くなっているか」を確認する。

- `AGENTS.md` — AIエージェント共通ルール
- `CLAUDE.md` — Claude Code 専用設定（@rules/ 参照）
- `.claude/rules/*.md` — 各ルールファイル
- `C:/Users/harne/.claude/projects/H--ClaudeCode-DevNext/memory/MEMORY.md` — メモリインデックス

### Step 4: 更新が必要な箇所を特定する

以下の観点で差異を探す。

| 確認ポイント | 場所 |
|------------|------|
| 新しいサービス・リポジトリが DI 登録されているか | `rules/di-and-password.md` |
| 新しい Samples が追加されているか | `AGENTS.md` のプロジェクト構成 |
| コマンドが変わっていないか | `rules/commands.md` |
| テーブル・名前空間が変わっていないか | `rules/database.md` / `rules/namespaces.md` |
| スキルが増えていないか | `CLAUDE.md` や `rules/` への追記要否 |
| `rules/sprint-contract.md` などの手順が古くなっていないか | 各 rules ファイル |

### Step 5: ドキュメントを更新する

古くなっている箇所のみ最小限に修正する。**書き換えすぎない**。

- 実装に存在しないものをドキュメントから削除
- 実装に追加されたが未記載のものを追記
- ファイルパス・名前空間・コマンドのズレを修正

### Step 6: AGENTS.md を更新する

AGENTS.md は他のエージェント（Codex等）も参照する。以下を最新に保つ。

- プロジェクト構成（`Samples/` の一覧など）
- コマンドリファレンス
- エンティティ設計ルール

### Step 7: メモリを確認・更新する

`C:/Users/harne/.claude/projects/H--ClaudeCode-DevNext/memory/` の各ファイルを確認し、
古くなったプロジェクトメモリがあれば更新または `status: superseded` とする。

### Step 8: コミットする

変更があれば commit する。変更なしなら何もしない。

```bash
# CLAUDE.md はforce-trackされているので個別に追加する
git add .claude/rules/ .claude/skills/ CLAUDE.md
git commit -m "chore: harness を実装に追従して自動更新"
```

**注意**: `.claude/rules/` と `.claude/skills/` は `.gitignore` の例外として追跡済み。
`CLAUDE.md` は `git add -f` でforce-trackされているため、通常の `git add` で追加できる。
`git add -A` や `git add .` は gitignore されているファイルを意図せず含む可能性があるため使わない。

## 注意事項

- **既存の方針・意図は変えない**。実装との乖離を直すだけ。
- **ルールの追加は最小限**。「あると便利そう」なルールを勝手に増やさない。
- 変更がなければ何もしない（空コミット禁止）。
- 変更内容の概要を最後に日本語で報告する。
