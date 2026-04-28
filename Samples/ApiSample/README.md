# ApiSample

ASP.NET Core 10 による JWT 認証付き REST API のサンプルプロジェクト。

## 概要

- **認証方式**: JWT Bearer トークン
- **DB**: SQL Server LocalDB（`ApiSampleDB`）— 起動時に自動作成・Seed 投入
- **ポート**: `http://localhost:5042`
- **Swagger UI**: `http://localhost:5042/swagger`

## 起動方法

```powershell
cd H:\ClaudeCode\DevNext\Samples\ApiSample
dotnet run
```

既に起動中のプロセスがある場合はビルドが失敗します。PID を確認して停止してから再起動してください。

```powershell
# 起動中のプロセスを停止する場合
Stop-Process -Name "ApiSample" -Force
```

## テストユーザー

| Email | Password | Role |
|-------|----------|------|
| admin1@sample.jp | Admin1! | Admin |
| member1@sample.jp | Member1! | Member |

## API エンドポイント一覧

### 認証

| メソッド | パス | 認証 | 説明 |
|---------|------|------|------|
| POST | `/api/auth/login` | 不要 | JWT トークンを取得する |

**リクエスト例:**
```json
POST /api/auth/login
{
  "email": "admin1@sample.jp",
  "password": "Admin1!"
}
```

**レスポンス例:**
```json
{
  "token": "eyJhbGci...",
  "expiresAt": "2026-04-28T10:00:00Z",
  "email": "admin1@sample.jp",
  "roles": ["Admin"]
}
```

### 商品 (Items)

| メソッド | パス | 必要ロール | 説明 |
|---------|------|-----------|------|
| GET | `/api/items` | Admin / Member | 商品一覧を取得 |
| GET | `/api/items/{id}` | Admin / Member | 商品詳細を取得 |
| POST | `/api/items` | Admin のみ | 商品を登録 |
| PUT | `/api/items/{id}` | Admin のみ | 商品を更新 |
| DELETE | `/api/items/{id}` | Admin のみ | 商品を削除 |

**Authorization ヘッダー:**
```
Authorization: Bearer <取得したトークン>
```

**商品登録・更新のリクエスト例:**
```json
{
  "name": "新商品",
  "description": "説明文",
  "price": 9800,
  "stock": 100
}
```

## curl による動作確認

```bash
# 1. トークン取得
TOKEN=$(curl -s -X POST http://localhost:5042/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin1@sample.jp","password":"Admin1!"}' \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

# 2. 一覧取得
curl -s http://localhost:5042/api/items \
  -H "Authorization: Bearer $TOKEN" | python3 -m json.tool

# 3. 商品登録（Admin のみ）
curl -s -X POST http://localhost:5042/api/items \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"テスト商品","description":"説明","price":1000,"stock":10}' \
  | python3 -m json.tool

# 4. 商品削除（Admin のみ）
curl -s -X DELETE http://localhost:5042/api/items/1 \
  -H "Authorization: Bearer $TOKEN" -w "HTTP %{http_code}"
```

## Swagger UI の使い方

1. `http://localhost:5042/swagger` を開く
2. `POST /api/auth/login` を実行してトークンを取得
3. 画面右上の「Authorize」ボタンをクリック
4. `Bearer <取得したトークン>` を入力して「Authorize」
5. 各エンドポイントを試す

## シードデータ

起動時に `ApiItems` テーブルが空の場合、以下の商品が自動投入されます。

| 商品名 | 価格 | 在庫 |
|--------|------|------|
| ノートPC | ¥128,000 | 10 |
| ワイヤレスマウス | ¥3,980 | 50 |
| メカニカルキーボード | ¥12,800 | 25 |
| USB-C ハブ | ¥4,500 | 30 |
| 27インチモニター | ¥49,800 | 8 |
