---
name: codex-dev
description: 機能開発をCodexに委譲するワークフロー。ブレインストーミングで設計を固め、実装計画を生成し、codex:codex-rescueサブエージェントで直接Codexに引き渡す（別ターミナル不要）。
---

ユーザーから機能開発・実装の依頼を受けた場合、以下の手順を順番に実行してください。

## 手順

### Step 1: ブレインストーミング（設計フェーズ）

`superpowers:brainstorming` スキルを起動して設計を固める。

- ユーザーの依頼内容をインプットにブレインストーミングを開始する
- スペック（`docs/superpowers/specs/YYYY-MM-DD-<topic>-design.md`）が承認されるまで進める
- **ここでユーザーの承認を得てから次のステップへ進む**

### Step 2: 実装計画の作成

`superpowers:writing-plans` スキルを起動して実装計画を生成する。

- 承認されたスペックをもとに計画ファイルを生成する
- 計画ファイルは `docs/superpowers/plans/YYYY-MM-DD-<feature>.md` に保存される
- **計画ファイルのパスを控えてから次のステップへ進む**

### Step 3: Codexへの引き渡し（プラグイン経由）

`codex:codex-rescue` サブエージェントをバックグラウンドで起動し、実装計画を渡す。

Agentツールを使って以下のように委譲する：
- `subagent_type: codex:codex-rescue`
- `run_in_background: true`
- promptには以下を含める：
  - `--background` フラグ
  - 「AGENTS.mdのルールに従って」
  - 「計画ファイル `<パス>` を読んでチェックボックス順に実装してください」
  - 「実装完了後は `git add -A && git commit` してください」

例：
```
--background AGENTS.md のルールに従って、実装計画 docs/superpowers/plans/2026-03-30-my-feature.md をチェックボックス順に実装してください。完了後は git add -A && git commit してください。
```

起動したらユーザーに伝える：
```
Codexをバックグラウンドで起動しました。
完了後は /codex:result で結果を確認できます。
```

### Step 4: レビュー（Codex完了後）

Codexの実装が完了したら（`/codex:result` で確認後）、同セッション内でレビューを実行する。

`superpowers:code-reviewer` エージェントを使って以下を評価する：
- セキュリティ（認可チェック・XSS・CSRF）
- 保守性（コメント・命名・AGENTS.mdルール準拠）
- 機能完成度（スプリントコントラクトの完成条件を満たしているか）

問題なければ `/add-page` スキルで後処理（ナビ・ドキュメント・コミット）へ進む。

---

## 注意事項

- Step 1のブレインストーミングをスキップしない（設計なしで実装させない）
- Codexはバックグラウンド実行が基本（複雑な実装タスクは時間がかかるため）
- `scripts/start-codex.ps1` は旧方式のフォールバック用として残置
- レビューはClaude Code側で行う（別セッション不要・`request-review.ps1` は不要）
