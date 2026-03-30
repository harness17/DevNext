<#
.SYNOPSIS
    Codexが実装完了後にClaude Codeのレビューを自動起動するスクリプト。
    git diff で変更内容を収集し、claude --print でレビュー指示を送る。

.DESCRIPTION
    ワークフロー:
    1. git diff HEAD でステージング前後の変更を取得
    2. 変更がなければスキップ
    3. claude --print にレビュー指示とdiff内容を渡す
    4. レビュー結果を標準出力に表示

.EXAMPLE
    ./scripts/request-review.ps1
#>

# リポジトリルートに移動（スクリプトの親ディレクトリ）
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

# git diff で直前コミットの変更ファイル一覧を取得（Codexがコミット後に実行する前提）
$diffStat = git diff HEAD~1 HEAD --stat 2>&1
$diffContent = git diff HEAD~1 HEAD 2>&1

# 変更がなければスキップ（コミット履歴を確認）
if (-not $diffStat -and -not (git log --oneline -1 2>&1)) {
    Write-Host "コミットがありません。レビューをスキップします。"
    exit 0
}

# ステージ済みの差分も取得
$stagedStat = git diff --cached --stat 2>&1
$stagedContent = git diff --cached 2>&1

Write-Host "=== 変更ファイル ==="
if ($diffStat) { Write-Host $diffStat }
if ($stagedStat) { Write-Host $stagedStat }
Write-Host ""
Write-Host "Claude Code によるレビューを開始します..."
Write-Host ""

# Claude Code に渡すレビュー指示
$reviewPrompt = @"
以下のコード変更をレビューしてください。

## 変更ファイル一覧
$diffStat
$stagedStat

## 変更内容（diff）
$diffContent
$stagedContent

## レビュー観点
AGENTS.md のコーディングルールに基づき、以下の軸で評価してください：
1. セキュリティ（認可チェック・XSS・SQLインジェクション・CSRF）
2. 保守性（コメント量・命名・単一責任・AGENTS.mdのルール準拠）
3. 機能完成度（実装計画のチェックボックスをすべて満たしているか）

問題があれば具体的な修正箇所と修正方法を指摘してください。
問題なければ「レビュー完了: 承認」と出力してください。
"@

# claude --print でレビューを実行（結果を標準出力に表示）
$reviewPrompt | claude --print
