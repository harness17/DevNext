<#
.SYNOPSIS
    Claude Codeが設計完了後にCodexを起動するスクリプト。
    Windows Terminal の新タブでCodexを起動し、指定した計画ファイルを最初のプロンプトとして渡す。

.PARAMETER PlanFile
    実装させる計画ファイルのパス（リポジトリルートからの相対パス）。

.EXAMPLE
    ./scripts/start-codex.ps1 doc/superpowers/plans/2026-03-30-my-feature.md
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$PlanFile
)

$repoRoot = Split-Path -Parent $PSScriptRoot

$absolutePlanFile = Join-Path $repoRoot $PlanFile
if (-not (Test-Path $absolutePlanFile)) {
    Write-Error "計画ファイルが見つかりません: $absolutePlanFile"
    exit 1
}

$safePlanFile = $absolutePlanFile -replace "'", "''"
$prompt = "AGENTS.md のルールに従って、以下の実装計画をチェックボックス順に実行してください: $safePlanFile 実装完了後は必ず scripts/request-review.ps1 を実行してください。"

Write-Host "Codexを起動します..."
Write-Host "計画ファイル: $absolutePlanFile"

wt new-tab --startingDirectory $repoRoot powershell -NoExit -Command "codex '$prompt'"
