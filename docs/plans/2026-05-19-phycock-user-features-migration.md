# 実装計画: Phycock ユーザ関連機能の移植

設計書: `docs/2026-05-19-phycock-user-features-migration-design.md`
対象ブランチ: develop

## 前提・全体方針

- 名前空間読み替え: `Phycock.*` → `Site.*`。`Dev.CommonLibrary.*` はそのまま。
- DevNext の複数ロール（`RoleNames` / checkbox）を維持。Phycock の単一ロール（`RoleName` / radio）は移植しない。
- `Const`（`SystemAdminUserId`）は `Site.Common`、`ApplicationUser`/`ApplicationRole` は `Site.Entity`、`ApplicationRoleType` は `Dev.CommonLibrary.Entity`。
- DevNext の `UserManagementService` コンストラクタは `(UserManager, RoleManager)` の2引数。`IHttpContextAccessor` は取らない。Phycock の Member選択系メソッドは移植しないため `IHttpContextAccessor` 依存は不要。
- 行 ViewModel のクラス名は `UserManagementListItemViewModel`。
- 依存順: ViewModel → Service → Controller → View → Tests。`git add` は個別ファイル指定（`git add -A` 禁止）。

### LocalUtil のシグネチャ（重要・Phycock と異なる）

`DevNext/Common/localutil.cs` の API:
- `GetAlertMessage(string template, string title)` — `string.Format(template, title, title)`
- `GetCreateAlertMessage` / `GetUpdateAlertMessage` / `GetDeleteAlertMessage(string title)`
- `GetErrorAlertMessage(string title)` — 「{title}の処理に失敗しました。」を生成する（**任意のエラー全文を渡す用途ではない**）

→ 成功メッセージは `GetAlertMessage("{1}を...しました。", "ユーザー")` を使う。
→ 初期 Admin 等の固定エラー文は **DevNext 既存コードの書き方をそのまま踏襲**（現状 `GetErrorAlertMessage("初期管理者ユーザーは削除できません")` と書かれているので、文言のみ「削除」→「無効化」に変更し、ヘルパーの使い方は変えない）。

---

## 移植対象1: 管理者パスワードリセット機能

- [x] **1-1: ViewModel 追加** — `DevNext/Models/UserManagementViewModels.cs`（namespace `Site.Models`）に `UserManagementResetPasswordViewModel` を追加。
  - `Id`（`[Required]`）/ `UserName`（`[Display(Name="ユーザー名")]` 表示専用）
  - `NewPassword`（`[Required]` / `[StringLength(100, MinimumLength=6)]` / `[DataType(DataType.Password)]` / `[Display(Name="新しいパスワード")]`）
  - `ConfirmPassword`（`[DataType(DataType.Password)]` / `[Display(...)]` / `[Compare("NewPassword", ErrorMessage="新しいパスワードと確認のパスワードが一致しません。")]`）
  - Phycock 版（`UserManagementViewModels.cs` の同クラス）をコピーし namespace を `Site.Models` に変更。

- [x] **1-2: Service にメソッド追加** — `DevNext/Service/UserManagementService.cs`（コンストラクタは変更しない）。
  - `GetUserResetPasswordAsync(string id)` — `FindByIdAsync` → null なら null、それ以外 `UserManagementResetPasswordViewModel { Id, UserName }` を返す。
  - `ResetPasswordAsync(string id, string newPassword)` — `FindByIdAsync` → null なら `IdentityResult.Failed`、それ以外 `GeneratePasswordResetTokenAsync` → `ResetPasswordAsync(user, token, newPassword)`。

- [x] **1-3: Controller にアクション追加** — `DevNext/Controllers/UserManagementController.cs` に `ResetPassword` GET / POST を追加。
  - GET: `id==null`→`BadRequest()`、`GetUserResetPasswordAsync`→null なら `_logger.Warn` + `NotFound()`、それ以外 `View(model)`。
  - POST: `[HttpPost][ValidateAntiForgeryToken]`。`ModelState.IsValid` なら `ResetPasswordAsync`。成功時 `TempData[SessionKey.Message] = LocalUtil.GetAlertMessage("{1}のパスワードを変更しました。", "ユーザー")`、`RedirectToAction("Index", new { returnList = true })`。失敗時 `result.Errors` を `ModelState.AddModelError`。エラー再表示時は `GetUserResetPasswordAsync` で `UserName` を再補完して `View(model)`。

- [x] **1-4: View 新規作成** — `DevNext/Views/UserManagement/ResetPassword.cshtml` を Phycock 版そのままで作成（Razor のみ、`@using` 追加不要）。

- [x] **1-5: Edit View にリンク追加** — `DevNext/Views/UserManagement/Edit.cshtml` のボタン行に1行追加。
  `<a asp-action="ResetPassword" asp-route-id="@Model.Id" class="btn btn-outline-primary ms-2">パスワード変更</a>`
  - ロール部分（checkbox 構成）は触らない。

---

## 移植対象2: ユーザー無効化（物理削除 → 論理削除）

- [x] **2-1: 行 ViewModel に `IsDisabled` 追加** — `UserManagementListItemViewModel` に `public bool IsDisabled { get; set; }` を追加。

- [x] **2-2 + 2-3: Service と Controller の無効化対応（同一コミットで実施）**
  - `UserManagementService.cs`:
    - 定数: `public static readonly DateTimeOffset DisabledLockoutEnd = new(new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc));`
    - ヘルパー: `public static bool IsDisabled(ApplicationUser user) => user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value >= DisabledLockoutEnd;`
    - `GetUserListAsync` のマッピングに `IsDisabled = IsDisabled(user),` を追加。
    - `DeleteUserAsync` を `DisableUserAsync(string id)` に置換: 初期 Admin は `IdentityResult.Failed`（"初期管理者ユーザーは無効化できません。"）、`FindByIdAsync`→null なら `Failed`、それ以外 `LockoutEnabled=true; LockoutEnd=DisabledLockoutEnd; AccessFailedCount=0;` → `UpdateAsync(user)`。
  - `UserManagementController.cs`:
    - `Delete` GET: 初期 Admin 禁止メッセージの文言を「削除できません」→「無効化できません」に変更（ヘルパーの使い方は現状維持）。ルート名 `Delete`/`DeleteConfirmed` は維持。
    - `DeleteConfirmed`: `DeleteUserAsync` → `DisableUserAsync` に変更。`result.Succeeded` なら `TempData[SessionKey.Message] = LocalUtil.GetAlertMessage("{1}を無効化しました。", "ユーザー")`、失敗時はエラー説明を TempData に表示（DevNext 既存のエラー表示パターンに合わせる）。
  - ※ 2-2 単独だと `DeleteUserAsync` 消失で Controller がビルド不可。必ずセットで。

- [x] **2-4: _IndexPartial View** — `DevNext/Views/UserManagement/_IndexPartial.cshtml`。
  - 状態列ヘッダ「ロック状態」→「状態」に変更。
  - 状態セルに `@if (row.IsDisabled) { <span class="badge bg-secondary">無効</span> }` を、既存のロック中/正常判定の前に追加。
  - 操作ボタン文言「削除」→「無効化」、disabled ボタンの `title` も「初期管理者ユーザーは無効化できません」に変更。
  - **`<option selected>` 整形差分は移植しない**（DevNext 側の記法を維持）。

---

## 移植対象3: ログインの email バグ修正

- [x] **3-1: AccountController.Login POST の修正** — `DevNext/Controllers/AccountController.cs`。
  - `_signInManager.PasswordSignInAsync(model.Email, ...)` を削除。
  - `var user = await _userManager.FindByEmailAsync(model.Email);`
  - `var result = user == null ? Microsoft.AspNetCore.Identity.SignInResult.Failed : await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);`
  - 成功時 `await _signInManager.SignInAsync(user!, model.RememberMe);` → `RedirectToLocal(returnUrl)`。
  - `IsLockedOut`・失敗時のセキュリティログ・メッセージは現行のまま維持。
  - **Register アクションは一切変更しない。**

---

## 移植対象4: ヘッダーメニューのドロップダウン化

- [x] **4-1: _LoginPartial の修正** — `DevNext/Views/Shared/_LoginPartial.cshtml`。
  - **ログイン中ブロックのみ** を Phycock 版のドロップダウン構成に置換: ユーザー名トグル + Admin は「ユーザー管理」（`UserManagement/Index`）/ それ以外「アカウント管理」（`Manage/Index`）+ 区切り線 + ログアウト（POST フォーム + `@Html.AntiForgeryToken()`）。
  - **未ログインブロックは現行のまま保全** — DevNext の「新規登録」「ログイン」両リンクを残す。Phycock 版で丸ごと上書きしないこと。
  - `@using Site.Entity` ヘッダは維持（`@using Phycock.Entity` に書き換えない）。

---

## 移植対象5: テスト

- [x] **5-1: テストファイル新規作成** — `Tests/UserManagement/UserManagementServiceTests.cs`（namespace `Tests.UserManagement`）。
  - Phycock 版をベースに `using Phycock.*` → `using Site.*`。`Dev.CommonLibrary.Entity` はそのまま。
  - **`CreateService` ヘルパーを2引数コンストラクタに調整**: `new UserManagementService(userManager.Object, roleManager.Object)`（Phycock 版の第3引数 `IHttpContextAccessor` を削除）。不要になる `using` は整理。
  - 移植するテスト: `IsDisabled` 判定 / `ResetPasswordAsync` 正常系・ユーザー不在・ポリシー違反のエラー伝播 / `UserManagementResetPasswordViewModel` の `NewPassword` 必須・`ConfirmPassword` 不一致。
  - **追加するテスト**（Phycock 版に無い）: `DisableUserAsync_Succeeds_WhenUserExists` / `DisableUserAsync_Fails_ForSystemAdminUser`。
  - Phycock 固有メソッド（`GetMemberListAsync` 等）のテストは移植しない。

- [ ] **5-2: 検証** — `dotnet build DevNext.slnx` / `dotnet test .\Tests\Tests.csproj` が全件パス。

---

## 移植しないもの（混入防止）

`GetMemberListAsync` / `GetSelectedMemberUserIdAsync` / `SetSelectedMemberUserIdAsync` / `SelectedMemberUserIdSessionKey` / `ApplicationRoleName` への代入 / `RoleName`（単数）/ radio ボタン / `Register` の Admin 限定化。Service・ViewModel をコピーする際にこれらを取り込まないこと。

## DI 登録

`UserManagementService` は `Program.cs` で `AddScoped` 登録済み。コンストラクタ署名を変えないため **DI 登録の変更は不要**。

## 完成条件（スプリントコントラクト）

- 管理者が UserManagement の Edit から「パスワード変更」へ遷移し対象ユーザーのパスワードを再設定できる（正常系）
- パスワードポリシー違反時、ResetPassword 画面にエラーが赤字表示される（異常系）
- UserManagement 一覧でユーザーを無効化でき「無効」バッジが表示される（正常系）
- 初期 Admin ユーザーは無効化できない（認可・制約）
- カスタムユーザー名（ユーザー名≠メール）で登録したユーザーがメールでログインできる（バグ修正検証）
- 無効化済みユーザーはログインできず Lockout 画面に遷移する（異常系）
- ヘッダーがロール別ドロップダウンになり、未ログイン時の「新規登録」リンクが残る
- `Tests/` に対応するテストを追加し `dotnet test` が全件パスする
- 既存の認証・ユーザー管理・基本 CRUD を壊していない（no-regression）
