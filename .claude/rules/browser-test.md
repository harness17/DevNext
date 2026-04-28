# ブラウザテストルール

実装完了後のデバッグは、以下の優先順位でブラウザテストを行う。

## ツール優先順位

| 優先 | ツール | 使い方 |
|------|--------|--------|
| ①第一優先 | **agent-browser** | `agent-browser open <url>` / `snapshot` / `click` / `fill` |
| ②フォールバック | **curl** | agent-browser が `spawn UNKNOWN` 等で起動できない場合のみ使用 |

> フォールバックした場合は「agent-browser が使えなかったため curl で確認」と明示すること。

---

## サーバー起動手順

```bash
# バックグラウンド起動
cd H:/ClaudeCode/DevNext/DevNext && dotnet run --no-build 2>&1 &

# Monitor で起動完了を待つ（Now listening が出るまで）
# → "Now listening on: http://localhost:5232" を確認してからテスト開始
```

**ポート**: デフォルト `http://localhost:5232`（`launchSettings.json` に従う）

### バックグラウンド起動プロセスの後始末（必須）

Claude がバックグラウンドで `dotnet run` を起動した場合、**テスト完了後に必ず停止する**。
停止しないと次回の `dotnet run` でビルドがファイルロックエラーになる。

```bash
# テスト完了後に停止する（プロセス名で指定）
taskkill //F //IM "ProjectName.exe"
```

ユーザーに操作を渡すときは必ず停止済みであることを確認すること。
停止できていない場合は以下を案内する：

```powershell
Stop-Process -Name "ProjectName" -Force -ErrorAction SilentlyContinue
```

### よくある起動失敗パターン

| 症状 | 原因 | 対処 |
|------|------|------|
| すぐに exit code 0 で終了 | 前回のプロセスがポートを掴んでいる | `Stop-Process -Name "dotnet" -Force` してから再起動 |
| `HTTP 500` on `/Account/Login` | Razor ビューのコンパイルエラー | `curl` でエラー本文を取得して原因を確認 |
| `spawn UNKNOWN` | agent-browser の実行環境エラー | curl にフォールバックする |

---

## curl テストの手順

### セッション確立

```bash
# 1. ログインページを取得（cookie ファイルに antiforgery cookie を保存）
curl -s -c /tmp/cookie.txt http://localhost:5232/Account/Login -o /tmp/login.html

# 2. antiforgery トークンを抽出
REQ_TOKEN=$(grep -o '__RequestVerificationToken" type="hidden" value="[^"]*"' /tmp/login.html \
  | head -1 | sed 's/__RequestVerificationToken" type="hidden" value="//;s/"//')

# 3. ログイン POST（cookie を保存しながら送信）
curl -s -c /tmp/cookie.txt -b /tmp/cookie.txt -X POST http://localhost:5232/Account/Login \
  -d "Email=member1%40sample.jp&Password=Member1%21&__RequestVerificationToken=${REQ_TOKEN}" \
  -L -o /tmp/after_login.html -w "HTTP %{http_code}\n"
```

**重要**: GET と POST で **同じ `-c`/`-b` cookie ファイルを使う**。  
別ファイルを使うと antiforgery cookie と form token が不一致になり HTTP 400 になる。

### CSRF トークンの取得

ログイン後のページから最新トークンを取得する（ログイン前のトークンは期限切れになることがある）。

```bash
curl -s -c /tmp/cookie.txt -b /tmp/cookie.txt \
  http://localhost:5232/Home/Index -o /tmp/home.html

CSRF_TOKEN=$(grep -o 'csrf-token" content="[^"]*"' /tmp/home.html \
  | sed 's/csrf-token" content="//;s/"//')
```

---

## セキュリティ修正の検証チェックリスト

実装したセキュリティ修正に応じて、以下を実施すること。

### IDOR（水平権限昇格）

```bash
# member1 で他ユーザーのリソースにアクセス
curl -s -b /tmp/member_cookie.txt \
  http://localhost:5232/ApprovalRequest/Detail/<他人のID> \
  -w "\nHTTP %{http_code} → %{redirect_url}" -o /dev/null

# 期待: HTTP 302 → /Account/AccessDenied
```

### CSRF 保護

```bash
# トークンなし → 拒否されること
curl -s -b /tmp/cookie.txt -X POST http://localhost:5232/Notification/MarkAllAsRead \
  -w "\nHTTP %{http_code}" -o /dev/null
# 期待: HTTP 400

# 正しいトークンあり → 許可されること
curl -s -c /tmp/cookie.txt -b /tmp/cookie.txt -X POST \
  http://localhost:5232/Notification/MarkAllAsRead \
  -H "X-CSRF-TOKEN: ${CSRF_TOKEN}" \
  -w "\nHTTP %{http_code}" -o /dev/null
# 期待: HTTP 200
```

### CSRF meta タグ出力確認

```bash
grep -o 'csrf-token" content="[^"]*"' /tmp/home.html | head -1
# 期待: csrf-token" content="CfDJ8..." のように値が入っていること
```

---

## Razor ビューの注意事項

### `<script>` ブロック内での `</script>` 禁止

コメント・文字列を問わず `<script>` タグ内に `</script>` を直書きすると、  
Razor のコンパイルエラー（`CS1056: Unexpected character`）になる。

```html
{{! NG: コメント内でも </script> は使わない }}
// ポイント: </script> タグのエスケープ処理をする

{{! OK: 「script タグの閉じタグ」と言い換える }}
// ポイント: script の閉じタグのエスケープ処理をする
```

### `@Html.Raw` によるJS文字列の安全な出力

```csharp
{{! NG: エスケープが不完全（\ と </script> が未処理）}}
'@Html.Raw(message?.Replace("'", "\\'")?.Replace("\n", "\\n") ?? "")'

{{! OK: JsonSerializer.Serialize がすべてのエスケープを行う}}
@Html.Raw(System.Text.Json.JsonSerializer.Serialize(message ?? ""))
```

`JsonSerializer.Serialize` は戻り値に `"` が含まれるため、JS の `.text()` に直接渡せる。

---

## 確認報告フォーマット

テスト完了後は以下の形式で報告する：

```
## ブラウザテスト結果

| # | テスト内容 | 期待 | 結果 |
|---|-----------|------|------|
| 1 | ログインページ表示 | HTTP 200 | ✅ |
| 2 | IDOR 拒否（他人の申請） | 302 → AccessDenied | ✅ |
| 3 | CSRF なし → 拒否 | HTTP 400 | ✅ |
| 4 | CSRF あり → 許可 | HTTP 200 | ✅ |

確認方法: curl（agent-browser が spawn UNKNOWN で起動不可のためフォールバック）
```
