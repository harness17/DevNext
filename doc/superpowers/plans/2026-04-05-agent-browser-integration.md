# agent-browser 導入 実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Claude Code のブラウザ操作ルールを Playwright MCP から agent-browser CLI に切り替える。

**Architecture:** グローバル Claude Code ルール（`C:/Users/harne/.claude/rules/`）の `playwright-mcp.md` を削除し `agent-browser.md` を新規作成する。グローバル `CLAUDE.md` の参照を更新し、DevNext の `commands.md` にインストールコマンドを追記する。

**Tech Stack:** agent-browser（npm グローバルパッケージ）、PowerShell（インストールスクリプト）

---

## ファイルマップ

| 操作 | パス |
|------|------|
| 新規作成 | `C:/Users/harne/.claude/rules/agent-browser.md` |
| 削除 | `C:/Users/harne/.claude/rules/playwright-mcp.md` |
| 修正 | `C:/Users/harne/.claude/CLAUDE.md` |
| 新規作成 | `H:/ClaudeCode/DevNext/scripts/install-agent-browser.ps1` |
| 修正 | `H:/ClaudeCode/DevNext/.claude/rules/commands.md` |

---

## Task 1: agent-browser をグローバルインストールする

**Files:**
- 実行のみ（ファイル変更なし）

- [ ] **Step 1: agent-browser をインストールする**

```bash
npm install -g agent-browser
```

期待値: `added N packages` のような成功メッセージ

- [ ] **Step 2: Chrome for Testing をダウンロードする**

```bash
agent-browser install
```

期待値: Chrome for Testing のダウンロード完了メッセージ

- [ ] **Step 3: インストールを確認する**

```bash
agent-browser --version
```

期待値: バージョン番号が表示される（例: `0.x.x`）

---

## Task 2: agent-browser.md ルールファイルを作成する

**Files:**
- Create: `C:/Users/harne/.claude/rules/agent-browser.md`

- [ ] **Step 1: `agent-browser.md` を作成する**

`C:/Users/harne/.claude/rules/agent-browser.md` を以下の内容で作成する：

```markdown
## agent-browser 使用ルール

### 優先順位

ブラウザ操作は以下の優先順位で行う。

1. **agent-browser を使う（第一優先）**
   - `agent-browser open <url>`
   - `agent-browser snapshot`
   - `agent-browser screenshot <file>`
   - `agent-browser click / fill / find` など

2. **agent-browser が使えない場合は Bash で操作する（フォールバック）**
   - `curl` で HTTP レスポンスを確認する
   - ページの内容・ステータスコード・リダイレクトを検証する

### 禁止事項

- subprocess や headless ブラウザを新たにインストールしてまで操作しない
- 確認できない場合は「確認できなかった理由」を明示して報告する
```

- [ ] **Step 2: ファイルが作成されたことを確認する**

```bash
cat "C:/Users/harne/.claude/rules/agent-browser.md"
```

期待値: 上記の内容が表示される

---

## Task 3: playwright-mcp.md を削除し CLAUDE.md を更新する

**Files:**
- Delete: `C:/Users/harne/.claude/rules/playwright-mcp.md`
- Modify: `C:/Users/harne/.claude/CLAUDE.md`

- [ ] **Step 1: playwright-mcp.md を削除する**

```bash
rm "C:/Users/harne/.claude/rules/playwright-mcp.md"
```

- [ ] **Step 2: CLAUDE.md の参照を更新する**

`C:/Users/harne/.claude/CLAUDE.md` を読み、以下の行を変更する：

変更前:
```
@rules/playwright-mcp.md
```

変更後:
```
@rules/agent-browser.md
```

- [ ] **Step 3: 変更を確認する**

```bash
cat "C:/Users/harne/.claude/CLAUDE.md"
```

期待値: `@rules/agent-browser.md` が含まれ、`@rules/playwright-mcp.md` が含まれていない

---

## Task 4: インストールスクリプトと commands.md を追加する

**Files:**
- Create: `H:/ClaudeCode/DevNext/scripts/install-agent-browser.ps1`
- Modify: `H:/ClaudeCode/DevNext/.claude/rules/commands.md`

- [ ] **Step 1: `install-agent-browser.ps1` を作成する**

`H:/ClaudeCode/DevNext/scripts/install-agent-browser.ps1` を以下の内容で作成する：

```powershell
<#
.SYNOPSIS
    agent-browser をグローバルインストールするスクリプト。
    AI エージェントによるブラウザ自動操作 CLI（Vercel Labs製）を導入する。

.EXAMPLE
    ./scripts/install-agent-browser.ps1
#>

Write-Host "agent-browser をインストール中..." -ForegroundColor Cyan
npm install -g agent-browser

Write-Host "Chrome for Testing をダウンロード中..." -ForegroundColor Cyan
agent-browser install

Write-Host "インストール完了。バージョン確認:" -ForegroundColor Green
agent-browser --version
```

- [ ] **Step 2: `commands.md` のユーティリティセクションに追記する**

`H:/ClaudeCode/DevNext/.claude/rules/commands.md` の `## ユーティリティ` テーブルに以下の行を追加する：

```markdown
| agent-browser インストール | `./scripts/install-agent-browser.ps1` |
```

変更後のユーティリティセクション：

```markdown
## ユーティリティ

| 用途 | コマンド |
|------|---------|
| smtp4dev 起動 | `smtp4dev-start.ps1` |
| ビルド＋テスト一括 | `/verify` スキルを使用 |
| agent-browser インストール | `./scripts/install-agent-browser.ps1` |
```

- [ ] **Step 3: git でコミットする**

```bash
cd H:/ClaudeCode/DevNext
git add scripts/install-agent-browser.ps1 .claude/rules/commands.md
git commit -m "feat: agent-browser インストールスクリプトと commands.md を追加"
```

---

## Task 5: 動作確認する

- [ ] **Step 1: サーバーを起動する（Release ビルド）**

```bash
cd H:/ClaudeCode/DevNext/DevNext && dotnet run -c Release &
```

数秒待ってから起動確認：

```bash
sleep 6 && agent-browser open http://localhost:5232/
```

- [ ] **Step 2: スナップショットを取得してページ構造を確認する**

```bash
agent-browser snapshot
```

期待値: ページのアクセシビリティツリーが表示される（ログイン画面の要素が見える）

- [ ] **Step 3: スクリーンショットを撮影する**

```bash
agent-browser screenshot H:/ClaudeCode/DevNext/doc/agent-browser-test.png
```

期待値: PNG ファイルが生成される

- [ ] **Step 4: サーバーを停止してスクリーンショットを削除する**

```bash
agent-browser close
pkill -f "dotnet.*DevNext" 2>/dev/null
rm H:/ClaudeCode/DevNext/doc/agent-browser-test.png
```
