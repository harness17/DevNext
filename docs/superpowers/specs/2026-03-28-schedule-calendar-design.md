# スケジュール・カレンダー機能 設計書

| 項目 | 内容 |
|------|------|
| 作成日 | 2026-03-28 |
| ステータス | 承認済み |
| 対象機能 | スケジュール・カレンダー |

---

## 1. 概要

FullCalendar.js を使ったスケジュール・カレンダー機能を追加する。
個人の予定と全体共有の予定を色分けして1画面で管理できる。
繰り返し予定・参加者招待にも対応する。

---

## 2. データモデル

### `ScheduleEventEntity` / `ScheduleEventEntityHistory`

`SampleEntity` と同じパターンで `Base` クラスを介して本体と履歴を共有する。

```
ScheduleEventEntityBase (abstract) : SiteEntityBase
  ├── ScheduleEventEntity          // 本体テーブル
  └── ScheduleEventEntityHistory   // 履歴テーブル（HistoryId: long [Key]）
```

`ScheduleEventEntityBase` のカラム（`SiteEntityBase` 共通カラムに加えて）：

| カラム | 型 | 概要 |
|---|---|---|
| Title | string (200) | 件名 |
| Description | string? (2000) | 詳細 |
| StartDate | DateTime | 開始日時 |
| EndDate | DateTime | 終了日時 |
| IsAllDay | bool | 終日フラグ |
| IsShared | bool | false=個人 / true=共有 |
| OwnerId | string (450) | 作成者 UserId |
| RecurrenceType | enum | None / Daily / Weekly / Monthly |
| RecurrenceInterval | int | 繰り返し間隔（デフォルト1） |
| RecurrenceEndDate | DateTime? | 繰り返し終了日 |
| RecurrenceDaysOfWeek | string? | 週次の対象曜日（例: "1,3,5"） |

### `ScheduleEventParticipantEntity`

参加者テーブル。更新性質が異なるため履歴なし。`SiteEntityBase` を継承。

| カラム | 型 | 概要 |
|---|---|---|
| EventId | long | FK → ScheduleEventEntity |
| UserId | string (450) | FK → ApplicationUser |
| Status | enum | Invited / Accepted / Declined |

### Enum 定義

**`RecurrenceType`**

| 値 | 概要 |
|---|---|
| None | 繰り返しなし |
| Daily | 毎日 |
| Weekly | 毎週（RecurrenceDaysOfWeek で曜日指定） |
| Monthly | 毎月（開始日と同じ日） |

**`ParticipantStatus`**

| 値 | 概要 |
|---|---|
| Invited | 招待済み（未回答） |
| Accepted | 承諾 |
| Declined | 辞退 |

---

## 3. アーキテクチャ・コンポーネント構成

### 新規作成ファイル

```
DevNext/
├── Entity/
│   ├── ScheduleEventEntity.cs             // エンティティ（本体・履歴・Base）
│   └── ScheduleEventParticipantEntity.cs  // 参加者エンティティ
├── Common/
│   └── ScheduleEnum.cs                    // RecurrenceType / ParticipantStatus
├── Repository/
│   ├── IScheduleRepository.cs
│   └── ScheduleRepository.cs
├── Service/
│   ├── IScheduleService.cs
│   └── ScheduleService.cs                 // 繰り返し展開ロジックを含む
├── Controllers/
│   └── ScheduleController.cs
└── Views/Schedule/
    └── Index.cshtml                       // FullCalendar メイン画面

DbMigrationRunner/
└── ScheduleTableCreator.cs               // テーブル作成
```

### コントローラーのエンドポイント

| メソッド | URL | 概要 |
|---|---|---|
| GET | /Schedule/Index | カレンダー画面（認証済み） |
| GET | /Schedule/GetEvents | FullCalendar 用 JSON（`start` / `end` パラメータ） |
| GET | /Schedule/Detail/{id} | 詳細 JSON（モーダル用） |
| POST | /Schedule/Create | 予定作成 |
| POST | /Schedule/Edit/{id} | 予定編集 |
| POST | /Schedule/Delete/{id} | 予定論理削除 |
| POST | /Schedule/UpdateParticipantStatus | 参加ステータス更新（Accepted / Declined） |

### フロントエンド

- **FullCalendar.js** を CDN で読み込み（既存の Chart.js と同様の方式）
- 月・週・日ビューの切り替えをサポート
- カレンダー上の色分け：
  - 個人イベント（自分作成） = 青
  - 共有イベント（自分作成） = 緑
  - 招待された共有イベント = 橙
- 予定クリック → 詳細モーダル（参加者一覧・ステータス表示）
- 日付クリック / 「新規作成」ボタン → 作成モーダル（終日フラグ・繰り返し・参加者追加）

---

## 4. 権限設計

| 操作 | 条件 |
|---|---|
| 閲覧（個人予定） | 自分が作成した予定のみ |
| 閲覧（共有予定） | 全認証ユーザー |
| 作成 | 全認証ユーザー |
| 編集・削除 | 作成者本人のみ（Admin も他人の予定は変更不可） |
| 参加ステータス変更 | 招待されたユーザー本人のみ |

---

## 5. エラー処理

| ケース | レスポンス |
|---|---|
| 他人の予定を編集・削除しようとした | 403 |
| 存在しない予定 ID | 404 |
| 終了日時が開始日時より前 | バリデーションエラー |
| 繰り返し終了日が開始日より前 | バリデーションエラー |
| GetEvents / Detail（JSON API） | エラーも JSON で返却 |

---

## 6. 繰り返し展開方針

- 繰り返しルールはマスターレコードに保持する（`RecurrenceType` / `RecurrenceInterval` / `RecurrenceEndDate` / `RecurrenceDaysOfWeek`）
- カレンダー取得時（`GetEvents`）に、サービス層で指定期間内の発生日を動的に計算して返す
- 個別発生の編集はなし（編集 = 全発生を一括変更）
- 繰り返しイベントは FullCalendar に複数のイベントオブジェクトとして渡す（同じ `Id` + 発生日で区別）

---

## 7. テスト方針

`Tests/` プロジェクト（xUnit + Moq）に以下を追加する。

| テスト対象 | 観点 |
|---|---|
| `ScheduleService.GetEventsForRange` | 繰り返し展開が指定期間内に正しく生成されるか |
| `ScheduleService.GetEventsForRange` | `IsShared=false` の他人の予定が含まれないか |
| `ScheduleService.Create` | 終了日時 < 開始日時 のバリデーション |
| `ScheduleService.Delete` | 作成者以外が削除しようとした場合に例外 |
| `ScheduleService.UpdateParticipantStatus` | 招待されていないユーザーが変更できないか |

---

## 8. 画面 ID

| 画面 ID | 画面名 | URL | 権限 |
|--------|--------|-----|------|
| SCH-001 | スケジュール（カレンダー） | /Schedule/Index | 認証済み |
