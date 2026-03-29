# Claude Code ↔ Codex 連携ワークフロー 設計書

**作成日**: 2026-03-30
**対象プロジェクト**: DevNext
**目的**: Claude Codeによる設計 → Codexによる実装 → Claude Codeによるレビューを自動化する

---

## 背景・目的

DevNextの開発効率を上げるため、各AIエージェントの強みを活かした役割分担を行う。

| フェーズ | 担当 | 理由 |
|---------|------|------|
| 設計・計画 | Claude Code | ブレインストーミング・設計書作成・スキル活用が得意 |
| 実装 | Codex | 計画ファイルを読んでコードを書くのが得意 |
| レビュー | Claude Code | セキュリティ・保守性・設計整合性のチェックが得意 |

---

## ワークフロー全体像

```
[Claude Code] 設計フェーズ
  1. brainstorming スキルで設計を固める
  2. writing-plans スキルで実装計画を生成
     → docs/superpowers/plans/YYYY-MM-DD-xxx.md

  3. scripts/start-codex.ps1 <plan-file> を実行
     → Windows Terminalの新タブでCodexが起動し、計画ファイルを渡す

[Codex] 実装フェーズ
  4. AGENTS.md を参照してプロジェクトルールを確認
  5. 指定された plan.md を読んで実装
  6. ビルド確認（dotnet build）
  7. scripts/request-review.ps1 を実行
     → Claude Code が自動起動してレビューを開始

[Claude Code] レビューフェーズ
  8. git diff で変更内容を確認
  9. superpowers:code-reviewer でレビュー
  10. 問題なければ /add-page スキルで後処理（ナビ・ドキュメント・コミット）
```

---

## ファイル構成

```
DevNext/
├── AGENTS.md                        ← 新規: Codex向け共通ルール
├── CLAUDE.md                        ← 変更: 先頭に @AGENTS.md を追加
├── scripts/
│   ├── start-codex.ps1              ← 新規: Codex起動スクリプト
│   └── request-review.ps1           ← 新規: レビュー依頼スクリプト
└── docs/
    └── superpowers/
        └── plans/                   ← 既存: Claude Codeが設計書を置く場所
```

---

## AGENTS.md の内容設計

CodexがDevNextで作業するために必要な情報をすべて含める。
`.claude/rules/` の汎用ルールをここに集約し、Claude.md からは `@AGENTS.md` で参照する。

### 含める内容

| セクション | 移植元 |
|-----------|--------|
| プロジェクト概要・構成 | CLAUDE.md |
| コーディング方針 | `.claude/rules/coding-policy.md` |
| 名前空間ルール | `.claude/rules/namespaces.md` |
| DB設定・ルール | `.claude/rules/database.md` |
| DI登録ルール | `.claude/rules/di-and-password.md` |
| ビルド・テストコマンド | `.claude/rules/commands.md` |
| 実装計画の読み方 | 新規記載 |
| 実装完了後の必須手順 | 新規記載（request-review.ps1 実行） |

---

## scripts/start-codex.ps1

```
目的: Claude Code が設計完了後に呼び出す。
     Windows Terminal の新タブでCodexを起動し、計画ファイルを最初のプロンプトとして渡す。

引数: <plan-file-path>  例: docs/superpowers/plans/2026-03-30-my-feature.md

動作:
  1. 指定された計画ファイルが存在するか確認
  2. wt（Windows Terminal）コマンドで新タブを開く
  3. 新タブで codex を起動し、計画ファイルのパスを含むプロンプトを渡す
```

---

## scripts/request-review.ps1

```
目的: Codexが実装完了後に呼び出す。
     git diff で変更ファイルを収集し、claude --print でレビューを自動起動する。

引数: なし（カレントディレクトリのgit差分を使用）

動作:
  1. git diff --stat で変更ファイル一覧を取得
  2. git diff で変更内容の詳細を取得
  3. claude --print に変更内容を渡してレビュー指示を送る
     - 評価軸: セキュリティ・保守性・スプリントコントラクト達成度
     - 参照: AGENTS.md のコーディングルール
  4. レビュー結果を標準出力（必要に応じてファイル保存も可）
```

---

## CLAUDE.md の変更内容

先頭行に以下を追加するだけ：

```markdown
@AGENTS.md
```

これにより、Claude Code も AGENTS.md のルールを参照できるようになる。
`.claude/rules/` の個別ファイルはそのまま維持（Claude Code固有の詳細設定として残す）。

---

## 制約・注意事項

- `request-review.ps1` が呼ぶ `claude --print` は**別セッション**で起動するため、会話履歴は引き継がない
- Codexに渡せるコンテキストはAGENTS.md + 計画ファイルのみ（`.claude/rules/` は読まれない）
- AGENTS.md の上限は32KiBのため、必要最小限の情報に絞る
- Windows Terminal（`wt` コマンド）がインストールされていることが前提

---

## 完成条件

- [ ] AGENTS.md が存在し、Codexが単体でDevNextの実装を開始できる情報が揃っている
- [ ] CLAUDE.md の先頭に `@AGENTS.md` が追加されている
- [ ] `start-codex.ps1` に計画ファイルパスを渡すとCodexが起動する
- [ ] `request-review.ps1` を実行すると Claude Code がレビューを開始する
- [ ] 既存の `.claude/rules/` の内容は変更しない
