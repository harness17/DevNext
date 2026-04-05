# agent-browser 導入設計

> **対象:** Claude Code デバッグ用ブラウザ操作ツールの切り替え

**作成日:** 2026-04-05  
**ステータス:** active

---

## 目的

Claude Code がブラウザ操作を行う際、Playwright MCP（未接続時に使えない）に依存するのをやめ、CLI として常に使える `agent-browser` を第一優先に切り替える。

---

## 変更ファイル一覧

| 操作 | ファイル |
|------|---------|
| 新規作成 | `C:/Users/harne/.claude/rules/agent-browser.md` |
| 削除 | `C:/Users/harne/.claude/rules/playwright-mcp.md` |
| 修正 | `C:/Users/harne/.claude/CLAUDE.md`（`@rules/playwright-mcp.md` → `@rules/agent-browser.md`） |
| 新規作成 | `H:/ClaudeCode/DevNext/scripts/install-agent-browser.ps1` |
| 修正 | `H:/ClaudeCode/DevNext/.claude/rules/commands.md`（インストールコマンドを追加） |

---

## agent-browser.md の内容

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

---

## install-agent-browser.ps1 の内容

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

---

## commands.md への追記

```markdown
| agent-browser インストール | `./scripts/install-agent-browser.ps1` |
```
