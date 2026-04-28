# ApiClientSample

ApiSample（REST API）を呼び出して商品 CRUD を行う ASP.NET Core 10 MVC サンプル。
JWT 認証・HttpClient・Session を使った API クライアントの実装パターンを示す。

## 概要

- **認証**: ApiSample から取得した JWT を Session に保存して利用
- **ポート**: `http://localhost:5170`
- **前提**: ApiSample（`http://localhost:5042`）が起動済みであること

## 起動方法

**ApiSample を先に起動する（必須）**

```powershell
# ターミナル 1: ApiSample を起動
cd H:\ClaudeCode\DevNext\Samples\ApiSample
dotnet run
```

```powershell
# ターミナル 2: ApiClientSample を起動
cd H:\ClaudeCode\DevNext\Samples\ApiClientSample
dotnet run
```

起動後、`http://localhost:5170` をブラウザで開く。

既に起動中のプロセスがある場合はビルドが失敗します。

```powershell
Stop-Process -Name "ApiClientSample" -Force
```

## ログイン

ApiSample のテストユーザーでログインします。

| Email | Password | 利用できる機能 |
|-------|----------|--------------|
| admin1@sample.jp | Admin1! | 一覧・詳細・登録・編集・削除 |
| member1@sample.jp | Member1! | 一覧・詳細のみ |

## 機能一覧

| 画面 | URL | Admin | Member |
|------|-----|-------|--------|
| 商品一覧 | `/` | ✅ 登録・編集・削除ボタンあり | ✅ 閲覧のみ |
| 商品詳細 | `/Items/Detail/{id}` | ✅ 編集・削除ボタンあり | ✅ 閲覧のみ |
| 商品登録 | `/Items/Create` | ✅ | ❌ 403 |
| 商品編集 | `/Items/Edit/{id}` | ✅ | ❌ 403 |
| 商品削除 | `POST /Items/Delete/{id}` | ✅ | ❌ 403 |

## 実装のポイント

### HttpClient の設定（Program.cs）

```csharp
builder.Services.AddHttpClient<ApiSampleClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5042");
    client.Timeout = TimeSpan.FromSeconds(10);
});
```

### JWT トークンの Session 保存（AccountController）

```csharp
var result = await apiClient.LoginAsync(email, password);
HttpContext.Session.SetString("JwtToken", result.Token);
HttpContext.Session.SetString("IsAdmin", result.IsAdmin ? "1" : "0");
```

### API 呼び出し時のトークン付与（ApiSampleClient）

```csharp
var req = new HttpRequestMessage(HttpMethod.Get, "/api/items");
req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
var res = await httpClient.SendAsync(req);
```

### ロールによる表示制御（Razor ビュー）

```razor
@if (ViewBag.IsAdmin == true)
{
    <a asp-action="Create">新規登録</a>
}
```

## プロジェクト構成

```
Controllers/
  AccountController.cs   ← ログイン・ログアウト（JWT 取得 + Session 保存）
  HomeController.cs      ← 商品一覧
  ItemsController.cs     ← 商品詳細・登録・編集・削除
Models/
  LoginViewModel.cs      ← ログインフォーム
  LoginResult.cs         ← API レスポンス（Token + Roles）
  ItemViewModel.cs       ← 商品表示用
  ItemFormViewModel.cs   ← 商品登録・更新フォーム
Services/
  ApiSampleClient.cs     ← ApiSample 呼び出し（全エンドポイント対応）
Views/
  Account/Login.cshtml
  Home/Index.cshtml      ← 商品一覧（Admin 時は操作ボタン付き）
  Items/Detail.cshtml
  Items/Create.cshtml
  Items/Edit.cshtml
```
