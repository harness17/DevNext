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
