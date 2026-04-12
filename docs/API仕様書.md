# DevNext API 仕様書（内部 Ajax API）

| 項目 | 内容 |
|------|------|
| ドキュメント名 | DevNext API 仕様書（内部 Ajax API） |
| バージョン | 1.0 |
| 作成日 | 2026-03-28 |
| 対象システム | DevNext Web アプリケーション |

---

## 目次

1. [概要](#1-概要)
2. [共通仕様](#2-共通仕様)
3. [通知 API（NotificationController）](#3-通知-apinotificationcontroller)
   - [未読件数取得](#31-未読件数取得)
   - [最近の通知一覧取得](#32-最近の通知一覧取得)
   - [通知を既読にする](#33-通知を既読にする)
   - [全通知を既読にする](#34-全通知を既読にする)
4. [ダッシュボード API（DashboardController）](#4-ダッシュボード-apidashboardcontroller)
   - [チャートデータ取得](#41-チャートデータ取得)

---

## 1. 概要

本システムの内部 Ajax API は、ページの動的更新に使用するサーバーサイド API である。
外部公開 API ではなく、同一オリジンのフロントエンドからのみ呼び出す。

### 用途

| API グループ | 用途 |
|-------------|------|
| 通知 API | ナビバーのベルアイコン通知機能（未読件数・一覧表示・既読処理） |
| ダッシュボード API | Chart.js グラフデータの定期ポーリング（30秒間隔） |

---

## 2. 共通仕様

### 認証

- **すべてのエンドポイントで `[Authorize]` 属性**が必要
- 未認証リクエストは HTTP 401 または ログイン画面へのリダイレクトとなる

### リクエストヘッダー

| ヘッダー | 値 | 備考 |
|---------|-----|------|
| `Content-Type` | `application/json` | POST リクエスト時 |
| `X-Requested-With` | `XMLHttpRequest` | Ajax リクエスト識別用 |

### CSRF 対策

POST リクエストには ASP.NET Core の `Antiforgery` トークンが必要。
フォームからの POST または `[ValidateAntiForgeryToken]` を使用する。

### レスポンス形式

- 成功時：JSON オブジェクト（200 OK）
- 認証エラー：HTTP 401
- サーバーエラー：HTTP 500

---

## 3. 通知 API（NotificationController）

**ベース URL**: `/Notification`

ナビバーのベルアイコンで使用する通知機能の API。
ページ読み込み時および定期的に呼び出される。

---

### 3.1 未読件数取得

ログインユーザーの未読通知件数を返す。

#### リクエスト

| 項目 | 内容 |
|------|------|
| メソッド | GET |
| URL | `/Notification/GetUnreadCount` |
| 認証 | 必須 |
| リクエストボディ | なし |

#### レスポンス

```json
{
  "count": 3
}
```

| フィールド | 型 | 説明 |
|-----------|----|------|
| `count` | `int` | 未読通知件数（0以上） |

#### 使用場面

- ページ読み込み時にベルアイコンのバッジ数を更新する
- 通知を既読にした後に件数を再取得する

---

### 3.2 最近の通知一覧取得

ログインユーザーの直近の通知一覧を返す。

#### リクエスト

| 項目 | 内容 |
|------|------|
| メソッド | GET |
| URL | `/Notification/GetRecent` |
| 認証 | 必須 |
| リクエストボディ | なし |

#### レスポンス

```json
[
  {
    "notificationId": 12,
    "message": "申請「テスト申請」が承認されました",
    "isRead": false,
    "createdAt": "2026-03-28T10:30:00",
    "relatedUrl": "/ApprovalRequest/Detail/5"
  },
  {
    "notificationId": 11,
    "message": "新しい承認申請が提出されました",
    "isRead": true,
    "createdAt": "2026-03-27T14:00:00",
    "relatedUrl": "/ApprovalRequest/Detail/4"
  }
]
```

| フィールド | 型 | 説明 |
|-----------|----|------|
| `notificationId` | `long` | 通知 ID |
| `message` | `string` | 通知メッセージ |
| `isRead` | `bool` | 既読フラグ |
| `createdAt` | `string` | 作成日時（ISO 8601形式） |
| `relatedUrl` | `string?` | 関連ページの URL（任意） |

#### 使用場面

- ベルアイコンのドロップダウンを展開したときに通知一覧を表示する

---

### 3.3 通知を既読にする

指定した通知を既読状態に更新する。

#### リクエスト

| 項目 | 内容 |
|------|------|
| メソッド | POST |
| URL | `/Notification/MarkAsRead` |
| 認証 | 必須 |
| Content-Type | `application/json` |

**リクエストボディ**

```json
{
  "notificationId": 12
}
```

| フィールド | 型 | 説明 |
|-----------|----|------|
| `notificationId` | `long` | 既読にする通知 ID |

#### レスポンス

| ステータス | 内容 |
|-----------|------|
| 200 OK | 処理成功（ボディなし） |
| 400 Bad Request | リクエスト形式エラー |

#### 使用場面

- 通知一覧から個別通知をクリックしたとき

---

### 3.4 全通知を既読にする

ログインユーザーの全通知を既読状態に更新する。

#### リクエスト

| 項目 | 内容 |
|------|------|
| メソッド | POST |
| URL | `/Notification/MarkAllAsRead` |
| 認証 | 必須 |
| リクエストボディ | なし |

#### レスポンス

| ステータス | 内容 |
|-----------|------|
| 200 OK | 処理成功（ボディなし） |

#### 使用場面

- 「すべて既読にする」ボタンをクリックしたとき

---

## 4. ダッシュボード API（DashboardController）

**ベース URL**: `/Dashboard`

ダッシュボードの Chart.js グラフを動的に更新するための API。

---

### 4.1 チャートデータ取得

ダッシュボードに表示する全グラフのデータをまとめて返す。

#### リクエスト

| 項目 | 内容 |
|------|------|
| メソッド | GET |
| URL | `/Dashboard/GetChartData` |
| 認証 | 必須 |
| リクエストボディ | なし |

#### レスポンス

ダッシュボードで使用する5種のグラフデータを含むオブジェクトを返す。

```json
{
  "mailTrend": {
    "labels": ["2026-02-27", "2026-02-28", "..."],
    "data": [3, 5, 2, 0, 1, "..."]
  },
  "mailResult": {
    "labels": ["成功", "失敗"],
    "data": [120, 5]
  },
  "enumData": {
    "labels": ["値1", "値2", "値3"],
    "data": [30, 45, 25]
  },
  "fileType": {
    "labels": [".pdf", ".xlsx", ".png", "その他"],
    "data": [10, 8, 15, 3]
  },
  "wizardCategory": {
    "labels": ["カテゴリA", "カテゴリB", "カテゴリC"],
    "data": [20, 35, 10]
  }
}
```

| フィールド | 説明 | グラフ種別 |
|-----------|------|-----------|
| `mailTrend` | 直近30日のメール送信数推移 | 折れ線グラフ |
| `mailResult` | 送信成功 / 失敗の比率 | ドーナツグラフ |
| `enumData` | EnumData の値ごとの分布 | 棒グラフ |
| `fileType` | ファイル種別（拡張子）の分布 | 横棒グラフ |
| `wizardCategory` | ウィザードフォームのカテゴリ分布 | 棒グラフ |

各グラフデータの共通構造：

| フィールド | 型 | 説明 |
|-----------|----|------|
| `labels` | `string[]` | 軸ラベルの配列 |
| `data` | `number[]` | 各ラベルに対応するデータ値の配列 |

#### ポーリング間隔

フロントエンド側でダッシュボード表示中は **30秒間隔** で自動的に本エンドポイントを呼び出し、グラフを更新する。

#### 使用場面

- ダッシュボードページ（`/Dashboard/Index`）の初期表示時
- 30秒ごとの定期ポーリング時

---

## 付録：フロントエンド実装例

### 通知件数バッジ更新

```javascript
// ナビバーの未読件数を更新する例
async function updateNotificationBadge() {
    const response = await fetch('/Notification/GetUnreadCount');
    const data = await response.json();
    const badge = document.getElementById('notification-badge');
    badge.textContent = data.count > 0 ? data.count : '';
    badge.style.display = data.count > 0 ? 'inline' : 'none';
}
```

### ダッシュボード定期ポーリング

```javascript
// 30秒ごとにチャートデータを取得して更新する例
setInterval(async () => {
    const response = await fetch('/Dashboard/GetChartData');
    const data = await response.json();
    updateCharts(data);
}, 30000);
```

---

---

## 付録：ApiSample — 外部公開 REST API サンプル

`Samples/ApiSample` は DevNext 本体とは独立した **REST API サンプルプロジェクト**です。
内部 Ajax API ではなく、JWT Bearer 認証による外部公開を想定した API のパターンを示します。

| 項目 | 内容 |
|------|------|
| ベース URL | `http://localhost:5200/api` |
| 認証方式 | JWT Bearer（`Authorization: Bearer {token}`） |
| API ドキュメント | `/swagger` — Swagger UI（JWT Authorize ボタン付き） |

### 主要エンドポイント

| メソッド | URL | 説明 | 必要権限 |
|---------|-----|------|---------|
| POST | `/api/auth/login` | JWT トークン発行 | 不要 |
| GET | `/api/items` | 商品一覧取得 | 認証済み |
| GET | `/api/items/{id}` | 商品詳細取得 | 認証済み |
| POST | `/api/items` | 商品登録 | Admin |
| PUT | `/api/items/{id}` | 商品更新 | Admin |
| DELETE | `/api/items/{id}` | 商品削除 | Admin |

詳細な仕様は起動後に Swagger UI（`/swagger`）で確認してください。

---

*本ドキュメントは DevNext プロジェクトの内部 Ajax API 仕様を記述したものです。*
