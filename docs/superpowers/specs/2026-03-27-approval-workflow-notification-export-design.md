# 開発プラン設計書：承認ワークフロー・通知・エクスポート

| 項目 | 内容 |
|------|------|
| ドキュメント名 | 承認ワークフロー・通知機能・エクスポート 設計書 |
| バージョン | 1.0 |
| 作成日 | 2026-03-27 |
| 対象システム | DevNext Web アプリケーション |
| 目的 | ポートフォリオ強化（業務アプリ系機能の追加） |

---

## 概要

DevNext に以下の 3 フェーズで業務アプリ機能を追加する。

| フェーズ | 内容 | 優先度 |
|---------|------|--------|
| Phase 1 | 承認ワークフロー | 高（最初に実装） |
| Phase 2 | 通知機能 | 高（Phase 1 と連動） |
| Phase 3 | CSV/Excel エクスポート | 中（Phase 1 完成後に追加） |

---

## Phase 1：承認ワークフロー

### 目的

申請者（Member）が申請を作成し、承認者（Admin）が承認・却下する業務フローを実装する。
実務でよく使われる多段階状態遷移パターンのポートフォリオ的な実装例とする。

### エンティティ設計

**テーブル名：`ApprovalRequest`**

`SiteEntityBase` を継承し、共通カラム（Id, DelFlag, CreateDate, UpdateDate, CreateApplicationUserId, UpdateApplicationUserId）を持つ。

| カラム名 | 型 | NULL | 説明 |
|---------|---|------|------|
| `Id` | bigint | NOT NULL | PK（IDENTITY） |
| `Title` | nvarchar(200) | NOT NULL | 申請タイトル |
| `Content` | nvarchar(2000) | NOT NULL | 申請内容 |
| `Status` | int | NOT NULL | 状態（ApprovalStatus Enum） |
| `RequesterUserId` | nvarchar(450) | NOT NULL | 申請者のユーザーID（ApplicationUser.Id） |
| `ApproverComment` | nvarchar(1000) | NULL | 承認者コメント（承認・却下時に入力） |
| `RequestedDate` | datetime2 | NULL | 申請（Pending 移行）日時 |
| `ApprovedDate` | datetime2 | NULL | 承認・却下が確定した日時 |
| `DelFlag` | bit | NOT NULL | 論理削除フラグ |
| `CreateDate` | datetime2 | NOT NULL | 作成日時 |
| `UpdateDate` | datetime2 | NOT NULL | 最終更新日時 |
| `CreateApplicationUserId` | nvarchar(max) | NULL | 作成者ユーザーID |
| `UpdateApplicationUserId` | nvarchar(max) | NULL | 最終更新者ユーザーID |

### Enum 定義

**ApprovalStatus（申請状態）**

| 値 | 名前 | 表示名 | 説明 |
|---|------|--------|------|
| 1 | Draft | 下書き | 作成中・未申請 |
| 2 | Pending | 申請中 | 承認者のレビュー待ち |
| 3 | Approved | 承認済み | 承認者が承認した |
| 4 | Rejected | 却下 | 承認者が却下した |

### 状態遷移

```
[Draft] ──申請──→ [Pending] ──承認──→ [Approved]
   ↑                   │
   └────再申請──── [Rejected]
```

| 遷移 | 操作者 | 条件 |
|------|-------|------|
| Draft → Pending | 申請者（Member/Admin） | 申請ボタン押下 |
| Pending → Approved | 承認者（Admin） | 承認ボタン押下 |
| Pending → Rejected | 承認者（Admin） | 却下ボタン押下 |
| Rejected → Draft | 申請者（Member/Admin） | 修正して再申請 |

### ページ構成

| URL | 説明 | アクセス権 |
|-----|------|-----------|
| `ApprovalRequest/Index` | 申請一覧 | Member: 自分の申請のみ、Admin: 全申請 |
| `ApprovalRequest/Create` | 新規作成 | Member・Admin |
| `ApprovalRequest/Edit/{id}` | 編集（Draft のみ可） | 申請者本人 |
| `ApprovalRequest/Detail/{id}` | 詳細・承認/却下操作 | Member: 閲覧のみ、Admin: 操作可 |
| `ApprovalRequest/Delete/{id}` | 論理削除（Draft のみ） | 申請者本人 |

### 実装構成

既存の DatabaseSample パターンに準拠する。

```
Entity/
  ApprovalRequestEntity.cs
Repository/
  ApprovalRequestRepository.cs
Service/
  ApprovalWorkflowService.cs   ← 状態遷移ロジックをここに集約
Models/
  ApprovalRequestViewModels.cs
Controllers/
  ApprovalRequestController.cs
Views/ApprovalRequest/
  Index.cshtml
  _IndexPartial.cshtml
  Create.cshtml
  Edit.cshtml
  Detail.cshtml
  Delete.cshtml
```

---

## Phase 2：通知機能

### 目的

Phase 1 の承認ワークフローのイベント（申請・承認・却下）に連動して、関係ユーザーに通知を届ける。
ダッシュボードと同じ Ajax ポーリング方式で実装し、ナビバーに未読バッジとして表示する。

### エンティティ設計

**テーブル名：`Notification`**

`SiteEntityBase` を継承。

| カラム名 | 型 | NULL | 説明 |
|---------|---|------|------|
| `Id` | bigint | NOT NULL | PK（IDENTITY） |
| `RecipientUserId` | nvarchar(450) | NOT NULL | 通知先ユーザーID（ApplicationUser.Id） |
| `Message` | nvarchar(500) | NOT NULL | 通知メッセージ本文 |
| `IsRead` | bit | NOT NULL | 既読フラグ（false: 未読、true: 既読） |
| `RelatedUrl` | nvarchar(500) | NULL | クリック時の遷移先URL（Detail ページなど） |
| `DelFlag` | bit | NOT NULL | 論理削除フラグ |
| `CreateDate` | datetime2 | NOT NULL | 作成日時（通知発生日時） |
| `UpdateDate` | datetime2 | NOT NULL | 最終更新日時 |
| `CreateApplicationUserId` | nvarchar(max) | NULL | 作成者ユーザーID |
| `UpdateApplicationUserId` | nvarchar(max) | NULL | 最終更新者ユーザーID |

### 通知トリガー

`ApprovalWorkflowService` 内の状態遷移処理から `NotificationService.CreateAsync()` を呼び出す。

| イベント | 通知先 | メッセージ例 |
|---------|-------|------------|
| Draft → Pending（申請） | Admin ロール全ユーザー | 「{申請者名} さんから申請「{タイトル}」が届きました」 |
| Pending → Approved（承認） | 申請者 | 「申請「{タイトル}」が承認されました」 |
| Pending → Rejected（却下） | 申請者 | 「申請「{タイトル}」が却下されました」 |

### UI 設計

- **ナビバー：** ベルアイコン（🔔）＋未読件数バッジ
  - 未読 0 件のときはバッジ非表示
  - Ajax ポーリング（5〜10 秒間隔）で未読件数を取得
- **ドロップダウン：** 最新 10 件の通知を一覧表示
  - 未読は強調表示
  - クリックで既読にしつつ `RelatedUrl` に遷移
- **通知一覧ページ（任意）：** `Notification/Index` で全通知履歴を確認

### 実装構成

```
Entity/
  NotificationEntity.cs
Repository/
  NotificationRepository.cs
Service/
  NotificationService.cs
Controllers/
  NotificationController.cs   ← Ajax エンドポイント（未読件数取得・既読更新）
Views/Shared/
  _NotificationPartial.cshtml ← ナビバーに埋め込むパーシャル
```

---

## Phase 3：CSV/Excel エクスポート

### 目的

承認一覧ページの検索結果を CSV・Excel ファイルとしてダウンロードできるようにする。
現在の検索条件を維持したままエクスポートする（全件ではなく絞り込み結果が対象）。

### 対象ページ

`ApprovalRequest/Index`（承認一覧）

### エクスポート形式・ライブラリ

| 形式 | ライブラリ | 備考 |
|------|----------|------|
| CSV | 標準ライブラリのみ（System.Text） | 外部依存なし |
| Excel (.xlsx) | EPPlus | 既存の DatabaseSample の Excel 出力実装に準拠する |

> **注意：** `DatabaseSampleService.cs` に EPPlus による Excel 出力実装があるため、それを踏襲すること。ClosedXML は使用しない。

### エクスポート項目

| # | 項目名 | 説明 |
|---|-------|------|
| 1 | 申請ID | ApprovalRequest.Id |
| 2 | タイトル | ApprovalRequest.Title |
| 3 | 申請者 | RequesterUserId → ユーザー名に変換 |
| 4 | 状態 | ApprovalStatus の表示名 |
| 5 | 申請日時 | RequestedDate（申請ボタン押下時点） |
| 6 | 承認・却下日時 | ApprovedDate |
| 7 | 承認者コメント | ApproverComment |

### UI

`ApprovalRequest/Index` の検索フォーム横に 2 つのボタンを追加：

- `CSV ダウンロード` — 現在の検索条件でエクスポート
- `Excel ダウンロード` — 現在の検索条件でエクスポート

### 実装構成

```
Service/
  ExportService.cs   ← CSV・Excel 生成ロジック（汎用的に設計）
Controllers/
  ApprovalRequestController.cs に ExportCsv / ExportExcel アクションを追加
```

---

## 実装順序

```
Phase 1: ApprovalRequestEntity 追加
       → DbMigrationRunner 更新
       → Repository / Service / Controller / View 実装
       → /add-entity スキル・/add-page スキルで確認

Phase 2: NotificationEntity 追加
       → DbMigrationRunner 更新
       → NotificationService（ApprovalWorkflowService と連携）
       → ナビバー通知 UI（Ajax ポーリング）

Phase 3: ClosedXML パッケージ追加
       → ExportService 実装
       → Index ページにボタン追加
```

---

## 参考：既存の実装パターン

新機能の実装時は以下の既存実装を参考にすること。

| 参考対象 | 参照先 |
|---------|-------|
| CRUD パターン全般 | `DatabaseSampleController` / `DatabaseSampleService` |
| Ajax ポーリング | `DashboardController` / `Dashboard/Index.cshtml` |
| 状態管理（Enum） | `EnumDefine.cs` |
| ロールによるアクセス制御 | `UserManagementController`（`[Authorize(Roles = "Admin")]`） |
| 一覧の検索復帰パターン | `/search-restore` スキル |
| エンティティ追加手順 | `/add-entity` スキル |
| ページ追加後チェック | `/add-page` スキル |
