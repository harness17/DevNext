<#
.SYNOPSIS
    Codexが実装完了後にClaude Codeのレビューを自動起動するスクリプト。
    git diff で変更内容を収集し、claude --print でレビュー指示を送る。

.DESCRIPTION
    ワークフロー:
    1. git diff で変更内容を取得
    2. 変更がなければスキップ
    3. claude --print にレビュー指示とdiff内容を渡す
    4. レビュー結果を標準出力に表示

.EXAMPLE
    ./scripts/request-review.ps1
#>

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$stagedStat = git diff --cached --stat 2>&1
$stagedContent = git diff --cached 2>&1

if (-not [string]::IsNullOrWhiteSpace(($stagedStat | Out-String))) {
    $diffStat = $stagedStat
    $diffContent = $stagedContent
} else {
    $headExists = $true
    git rev-parse --verify HEAD *> $null
    if ($LASTEXITCODE -ne 0) {
        $headExists = $false
    }

    if ($headExists) {
        $diffStat = git diff HEAD~1 HEAD --stat 2>&1
        $diffContent = git diff HEAD~1 HEAD 2>&1
    } else {
        $diffStat = git diff --stat 2>&1
        $diffContent = git diff 2>&1
    }
}

if ([string]::IsNullOrWhiteSpace(($diffStat | Out-String))) {
    Write-Host "差分がありません。レビューをスキップします。"
    exit 0
}

Write-Host "=== 変更ファイル ==="
if ($diffStat) { Write-Host $diffStat }
Write-Host ""
Write-Host "Claude Code によるレビューを開始します..."
Write-Host ""

$reviewPrompt = @"
以下のコード変更をレビューしてください。

## 変更ファイル一覧
$diffStat

## 変更内容（diff）
$diffContent

## レビュー観点
AGENTS.md のコーディングルールに基づき、以下の軸で評価してください：
1. セキュリティ（認可チェック・XSS・SQLインジェクション・CSRF）
2. 保守性（コメント量・命名・単一責任・AGENTS.mdのルール準拠）
3. 機能完成度（実装計画のチェックボックスをすべて満たしているか）

問題があれば具体的な修正箇所と修正方法を指摘してください。
問題なければ「レビュー完了: 承認」と出力してください。
"@

$reviewPrompt | claude --print
