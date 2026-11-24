---
name: merge-master
description: develop→master マージ前の6ステップチェックリストを順番に実行。verify→ブラウザ確認→Evaluator→add-page→export-docs→merge の全手順を漏れなく実施する。
---

develop ブランチを master にマージする前に、以下のチェックリストを順番に実行してください。

## 前提確認

現在のブランチが `develop` であることを確認する：

```bash
git branch --show-current
git log --oneline -5
```

`develop` でなければ中止してユーザーに報告する。

---

## Step 1: ビルド＋テスト確認（/verify）

`/verify` スキルを実行し、ビルドとテストが通ることを確認する。

- ビルドエラー・テスト失敗があれば**中止**して修正を促す
- 全件合格を確認してから次へ進む

---

## Step 2: ブラウザ動作確認（agent-browser）

`agent-browser` で実装した機能を一通り操作し、動作確認を行う。

確認観点：
- 正常系の操作が期待通り動くか
- 認証・認可（ログイン必須ページ、Admin 限定ページ）が機能しているか
- エラー表示・バリデーションが適切か

問題があれば**中止**して修正を促す。

---

## Step 3: Evaluator レビュー

`superpowers:code-reviewer` を使って以下を評価する：

- セキュリティ（認可チェック・XSS・SQL インジェクション・CSRF）
- 保守性（コメント量・命名・単一責任・`coding-policy.md` 準拠）
- 機能完成度（スプリントコントラクトの完成条件をすべて満たしているか）

**差し戻し基準（1つでも該当すれば Step 1 からやり直し）：**
- 認可チェックの漏れがある
- `coding-policy.md` のコメントルールに著しく違反している
- スプリントコントラクトの完成条件を満たしていない

---

## Step 4: ページ・ナビ・README 更新（/add-page）

以下のいずれかに該当する変更があった場合は `/add-page` を実行する：

| 変更内容 | 例 |
|---------|-----|
| 新しいページ・機能を追加した | コントローラー・ビュー・サービスの新規作成 |
| DB テーブル・カラムを追加・変更した | エンティティクラスの追加・変更 |
| 技術スタック・依存ライブラリを変更した | NuGet パッケージの追加・変更 |
| プロジェクト構成を変更した | フォルダ追加、名前空間変更 |

上記に該当しなければスキップ可。

---

## Step 5: ドキュメント更新（/export-docs）

変更内容に応じて `docs/` 内の Markdown を先に更新し、`/export-docs` で Office ファイルを再生成する。

| 変更内容 | 更新ファイル |
|---------|------------|
| 機能追加・画面追加 | 基本設計書・詳細設計書・画面設計書 |
| テーブル・カラム追加・変更 | DB設計書 |
| テスト追加・変更 | テスト設計書・テストケース一覧 |
| API エンドポイント追加・変更 | API仕様書 |
| CommonLibrary の変更 | CommonLibrary利用ガイド |

ドキュメントの変更はマージコミットに含める。

---

## Step 6: PR 作成 → CI 通過確認 → マージ

master はブランチ保護で CI（`build-and-test`）通過が必須のため、ローカルでの `git merge` + push はできない。必ず PR 経由でマージする。

### 6-1. develop を push して PR を作成

```bash
cd H:/ClaudeCode/DevNext
git fetch origin
git status -sb   # origin/develop より behind なら rebase してから push する
git push origin develop
gh pr create --base master --head develop --title "<機能名>" --body "<Summary / Review / Test plan>"
```

### 6-2. CI の結果を確認

```bash
gh pr checks <PR番号>
```

| 結果 | 対応 |
|------|------|
| すべて `pass` | 6-3 へ進む |
| `fail` がある | **マージしない**。ログ（`gh run view <run-id> --log-failed`）で原因を特定し、develop で修正 → push して再実行させる |
| `pending` / チェックが表示されない | **マージしない**。完了を待つ。ワークフローが起動していなければ `.github/workflows/ci.yml` のトリガーを確認する |

ローカルの `/verify` が通っていても CI が通るとは限らない（OS 差・未コミットファイル・ローカル専用設定への依存）。CI の結果を正とする。

### 6-3. マージ

```bash
gh pr view <PR番号> --json mergeable,mergeStateStatus   # MERGEABLE / CLEAN を確認
gh pr merge <PR番号> --merge --subject "merge: develop → master ($(date '+%Y-%m-%d') <機能名>)"
git fetch origin master:master   # ローカル master を追従（develop のまま実行できる）
```

- `--merge`（マージコミット）を使う。squash / rebase は develop との履歴が分岐するため使わない
- `--delete-branch` は付けない（develop は継続利用する）

マージ完了後、develop ブランチにいることを確認してユーザーに報告する。

---

## 注意事項

- 各ステップで問題が見つかった場合は**必ず中止**してユーザーに報告する
- `master` への直接コミット・直接 push は禁止（PR 経由のマージのみ。ブランチ保護で強制されている）
- CI が `fail` / `pending` の PR はマージしない（ブランチ保護を管理者権限で迂回しない）
- マージ後は `develop` ブランチに戻ること
- ドキュメント更新がある場合は `/export-docs` 実行後にマージすること
