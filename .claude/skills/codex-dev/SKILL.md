---
name: codex-dev
description: 機能開発をCodexに委譲するワークフロー。ブレインストーミングで設計を固め、実装計画を生成し、codex:codex-rescueサブエージェントで直接Codexに引き渡す（別ターミナル不要）。
---

ユーザーから機能開発・実装の依頼を受けた場合、以下の手順を順番に実行してください。

## 手順

### Step 1: 設計フェーズ

`Plan` サブエージェント（`subagent_type: Plan`）を使って実装方針を固める。

- ユーザーの依頼内容・スコープ・完成条件を整理してインプットに渡す
- サブエージェントからステップバイステップの実装計画・影響ファイル・アーキテクチャ上の考慮点を受け取る
- **ここでユーザーの承認を得てから次のステップへ進む**

### Step 2: 実装計画の作成

承認された設計をもとに実装計画ファイルを作成する。

- 計画ファイルは `doc/plans/YYYY-MM-DD-<feature>.md` に保存する
- スプリントコントラクト（完成条件）を計画ファイルに明記する
- **起動前に `git log --oneline -1` で現在の最新コミットハッシュを控えておく**（完了検出に使う）
- **計画ファイルのパスを控えてから次のステップへ進む**

### Step 3: Codexへの引き渡し（プラグイン経由）

以下の手順で Codex にタスクを送信する。

**3-1. codex-companion でタスクを起動する（Bash ツールで直接実行）:**

```bash
node "C:/Users/harne/.claude/plugins/marketplaces/openai-codex/plugins/codex/scripts/codex-companion.mjs" \
  task --background --write \
  "AGENTS.md のルールに従って、実装計画 <計画ファイルパス> をチェックボックス順に実装してください。
  git add は個別ファイル指定（git add -A は使用しない）。
  実装完了後は git add で変更ファイルを個別指定してから git commit してください。"
```

> **注意**: `codex:codex-rescue` サブエージェント経由ではなく Bash で直接実行する。
> サブエージェント経由は Bash 権限が取れず失敗することがある。

出力からタスク ID（`task-xxxxxxxx-xxxxxx`）を控える。

**3-2. 完了監視ループをバックグラウンドで起動する:**

```bash
BASELINE_COMMIT=$(cd H:/ClaudeCode/DevNext && git log --oneline -1 | cut -d' ' -f1)
JOB_ID="<控えたタスクID>"
LOG_FILE="C:/Users/harne/AppData/Local/Temp/codex-companion/DevNext-57e0120711bb54b0/jobs/${JOB_ID}.log"

while true; do
  # 1. git commit による完了検出（最も信頼性が高い）
  CURRENT_COMMIT=$(cd H:/ClaudeCode/DevNext && git log --oneline -1 | cut -d' ' -f1)
  if [ "$CURRENT_COMMIT" != "$BASELINE_COMMIT" ]; then
    NEW_MSG=$(cd H:/ClaudeCode/DevNext && git log --oneline -1)
    echo "[$(date '+%H:%M:%S')] >>> git commit 検出: $NEW_MSG → Codex 完了とみなす"
    break
  fi

  # 2. status API 確認（補助的に使用）
  PHASE=$(node "C:/Users/harne/.claude/plugins/marketplaces/openai-codex/plugins/codex/scripts/codex-companion.mjs" \
    status "$JOB_ID" --json 2>/dev/null | python -c "import sys,json; print(json.load(sys.stdin)['job']['phase'])" 2>/dev/null)
  ELAPSED=$(node "C:/Users/harne/.claude/plugins/marketplaces/openai-codex/plugins/codex/scripts/codex-companion.mjs" \
    status "$JOB_ID" --json 2>/dev/null | python -c "import sys,json; print(json.load(sys.stdin)['job']['elapsed'])" 2>/dev/null)

  # 3. ログ停止 + ファイル存在チェック（スタック検出）
  LOG_AGE_MIN=$(python -c "import os,time; age=(time.time()-os.path.getmtime('$LOG_FILE'))/60 if os.path.exists('$LOG_FILE') else 0; print(int(age))" 2>/dev/null || echo 0)

  echo "[$(date '+%H:%M:%S')] phase=$PHASE elapsed=$ELAPSED log_age=${LOG_AGE_MIN}min commit=$CURRENT_COMMIT"

  if [ "$LOG_AGE_MIN" -gt 30 ] && [ "$PHASE" = "running" ]; then
    echo "[$(date '+%H:%M:%S')] >>> ログが${LOG_AGE_MIN}分停止中。スタックの可能性あり。/codex:cancel $JOB_ID を検討してください。"
  fi

  [ "$PHASE" = "completed" ] || [ "$PHASE" = "failed" ] && echo ">>> status API が完了を検出: $PHASE" && break

  sleep 300
done
```

ユーザーに伝える：
```
Codex をバックグラウンドで起動しました。タスク ID: <id>
5分ごとに監視します。完了は git commit で検出します。
進捗確認: /codex:status <id>
ログ監視: ! tail -f <ログファイルパス>
```

### Step 4: 完了確認とレビュー

監視ループが完了を検出したら（または `/codex:status` で手動確認後）、以下を実行する。

**4-1. 実装結果の確認:**

```bash
# git log で何がコミットされたか確認
cd H:/ClaudeCode/DevNext && git log --oneline -3

# 作成ファイルの確認
git show --stat HEAD
```

**4-2. コードレビュー:**

`superpowers:code-reviewer` エージェントを使って以下を評価する：
- セキュリティ（認可チェック・XSS・CSRF）
- 保守性（コメント・命名・AGENTS.md ルール準拠）
- 機能完成度（スプリントコントラクトの完成条件を満たしているか）

問題なければ `/add-page` スキルで後処理（ナビ・ドキュメント・コミット）へ進む。

---

## トラブルシューティング

### status が "running" のままで終わらない

Codex 内部のファイル書き込みはログに出ないため、job tracker が完了を検知できないことがある。
→ **git log で新しいコミットがあれば完了とみなす**（status API より信頼性が高い）

### ログが長時間（30分以上）更新されない

Codex がスタックしている可能性がある。
→ `PdfSample/` 等のターゲットフォルダにファイルが作成されているか確認する
→ ファイルがあればコミット漏れの可能性 → `/codex:cancel <id>` して手動でコミット
→ ファイルがなければスタック → `/codex:cancel <id>` してタスクを再起動

### インタラクティブ監視（別ターミナルで）

```bash
# Codex セッションに直接接続（Claude Code の ! では動かないため別ターミナルで実行）
codex resume <session-id>

# またはログをリアルタイムで流す
tail -f <ログファイルパス>
```

---

## 注意事項

- Step 1の設計フェーズをスキップしない（設計なしで実装させない）
- Codex はバックグラウンド実行が基本（複雑な実装タスクは時間がかかるため）
- レビューは Claude Code 側で行う（別セッション不要・`request-review.ps1` は不要）
- `git add -A` は使用しない。Codex へのプロンプトにも個別指定を明示すること
- `codex resume` は TUI アプリのため Claude Code の `!` では動かない。別ターミナルで実行すること
