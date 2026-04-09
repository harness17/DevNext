# 新案件向けカスタマイズ指針

DevNext を新案件のベースとして使う際に変更すべき箇所をまとめます。

---

## 1. アプリ名・名前空間の変更

### 名前空間

`DevNext/` 配下のファイルの RootNamespace は `Site` です。
新案件名（例: `MyApp`）に一括置換してください。

```
Site → MyApp
```

主な対象ファイル:
- `DevNext/Controllers/*.cs`
- `DevNext/Service/*.cs`
- `DevNext/Repository/*.cs`
- `DevNext/Models/*.cs`
- `DevNext/Common/*.cs`
- `DevNext/Entity/*.cs`
- `DevNext/Views/_ViewImports.cshtml`

### アプリ表示名

`DevNext/Views/Shared/_Layout.cshtml` のナビバーブランド名を変更:
```html
<a class="navbar-brand" asp-controller="Home" asp-action="Index">MyApp</a>
```

フッターのコピーライト表記も合わせて変更してください。

---

## 2. DB 名の変更

`appsettings.Development.json` の接続文字列中の `DevNextDB` を新 DB 名に変更:
```json
"SiteConnection": "Server=(localdb)\\mssqllocaldb;Database=MyAppDB;..."
```

`appsettings.Development.json` の変更後、`dotnet ef database update --project DevNext` を実行して新 DB にマイグレーションを適用してください。

---

## 3. Data Protection キー名の変更

`DevNext/Program.cs` の `SetApplicationName` を変更:
```csharp
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("MyApp");  // ← ここを変更
```

---

## 4. 不要な機能の削除

### ApprovalRequest（承認ワークフロー）を削除する場合

1. `DevNext/Controllers/ApprovalRequestController.cs` を削除
2. `DevNext/Service/ApprovalWorkflowService.cs` を削除
3. `DevNext/Repository/ApprovalRequestRepository.cs` を削除
4. `DevNext/Entity/ApprovalRequestEntity.cs` を削除
5. `DevNext/Models/ApprovalRequestViewModels.cs` を削除
6. `DevNext/Views/ApprovalRequest/` を削除
7. `DevNext/Common/DBContext.cs` から `ApprovalRequest` DbSet を削除
8. `DevNext/Program.cs` から `ApprovalWorkflowService` の DI 登録を削除
9. `DevNext/Views/Shared/_Layout.cshtml` から承認申請ナビリンクを削除

### Schedule（スケジュール）を削除する場合

1. `DevNext/Controllers/ScheduleController.cs` を削除
2. `DevNext/Service/ScheduleService.cs`、`ScheduleRecurrenceHelper.cs` を削除
3. `DevNext/Repository/ScheduleRepository.cs` を削除
4. `DevNext/Entity/ScheduleEventEntity.cs`、`ScheduleEventParticipantEntity.cs` を削除
5. `DevNext/Models/ScheduleViewModels.cs` を削除
6. `DevNext/Views/Schedule/` を削除
7. `DevNext/Common/DBContext.cs` から Schedule 関連 DbSet を削除
8. `DevNext/Program.cs` から `ScheduleRepository`、`ScheduleService` の DI 登録を削除
9. `DevNext/Views/Shared/_Layout.cshtml` からスケジュールナビリンクを削除

---

## 5. 初期ユーザーの変更

`DevNext/Program.cs` の `SeedAsync` メソッド内の Seed データを変更してください。

---

## 6. パスワードポリシーの変更

`DevNext/Program.cs` の Identity 設定で調整:
```csharp
options.Password.RequiredLength = 8;  // 文字数など
```
