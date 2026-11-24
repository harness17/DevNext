# ブランチ・マージルール

## ブランチ運用

- 開発は `develop` ブランチで行う
- 機能単位でさらに分岐する場合は `feature/xxx` を `develop` から切る
- `master` への直接コミット・直接 push は禁止（GitHub のブランチ保護で強制）

## master への反映は PR 作成からマージまで完了させる

ユーザーが master（main）への反映を指示したら、`/merge-master` の手順で **PR 作成 → CI 通過確認 → マージまでを一続きで完了させる**。
PR を作っただけで止めない。途中で止まるのは、CI 失敗・レビュー差し戻し・コンフリクトなど、下のチェックリストを満たせないときだけとし、その場合は理由を報告する。

- マージは `gh pr merge --merge`（マージコミット）。PR 作成直後に `gh pr merge --auto --merge` で自動マージを予約するのを基本とする。develop ブランチは削除しない
- CI（`.github/workflows/ci.yml` の `build-and-test`）が `pass` になるまでマージしない。`fail` / `pending` のままマージしない
- ブランチ保護を管理者権限で迂回しない（`--admin` を使わない）

## master へのマージ前チェックリスト

以下をすべて完了してからマージすること。

| # | 手順 | スキル・コマンド |
|---|------|----------------|
| 1 | ビルド＋テスト通過 | `/verify` |
| 2 | agent-browser デバッグ（動作確認） | `agent-browser open/snapshot/click` |
| 3 | Evaluator レビュー（差し戻しなし） | `superpowers:code-reviewer` |
| 4 | ページ・ナビ・README 更新 | `/add-page`（トリガー条件を満たす場合） |
| 5 | **ドキュメント更新** | `/export-docs`（下記参照） |
| 6 | PR 作成 → **CI 全件 pass** → マージ | `gh pr create` → `gh pr checks` → `gh pr merge --merge` |

## ドキュメント更新ルール（手順 5）

`document-output.md` のトリガー表を参照し、今回の変更内容に対応するドキュメントを更新すること。

| 変更内容 | 更新対象ドキュメント |
|---------|-------------------|
| 機能追加・画面追加 | 基本設計書・詳細設計書・画面設計書 |
| テーブル・カラム追加・変更 | DB設計書 |
| テスト追加・変更 | テスト設計書・テストケース一覧 |
| API エンドポイント追加・変更 | API仕様書 |
| CommonLibrary の変更 | CommonLibrary利用ガイド |

手順：
1. 該当する `docs/*.md` を先に更新する
2. `/export-docs` を実行して Office ファイルを再生成する
3. ドキュメントの変更もコミットに含めてからマージする
