# Phycock ユーザ関連機能 移植 設計書

- 作成日: 2026-05-19
- 対象ブランチ: develop
- 担当: Claude Code（設計）/ Codex（実装）/ user（merge 判断）

## 背景

派生プロジェクト Phycock で追加されたユーザ関連機能を、コアテンプレートである DevNext へ移植する。
両プロジェクトは分岐が進んでおり、Phycock 側には「移植すべき新機能」と「移植すると DevNext の機能を後退させる分岐」が混在する。本設計では前者のみを対象とする。

参照: Phycock コミット `6ac4d24`（ヘッダーメニュー整理とユーザー管理へのパスワード変更機能追加）ほか。

## 移植対象

### 1. 管理者パスワードリセット機能

管理者が他ユーザーのパスワードを、現在のパスワードを知らずに再設定できる機能。

- `UserManagementController`: `ResetPassword` GET / POST の 2 アクションを追加
- `Models/UserManagementViewModels.cs`: `UserManagementResetPasswordViewModel` を追加
  - `Id`（必須・hidden）/ `UserName`（表示専用）/ `NewPassword`（必須・6文字以上）/ `ConfirmPassword`（`Compare`）
- `Service/UserManagementService.cs`:
  - `GetUserResetPasswordAsync(string id)` — 対象ユーザーを ViewModel 化
  - `ResetPasswordAsync(string id, string newPassword)` — `GeneratePasswordResetTokenAsync` でトークン発行 → `ResetPasswordAsync` で適用
- `Views/UserManagement/ResetPassword.cshtml` を新規作成
- `Views/UserManagement/Edit.cshtml` に「パスワード変更」リンクを 1 行追加

### 2. ユーザー無効化（物理削除 → 論理削除）

DevNext の現行「物理削除」を、Phycock 同様のロックアウトベース「論理削除（無効化）」へ置き換える。

- `Service/UserManagementService.cs`:
  - `DeleteUserAsync` → `DisableUserAsync` に置き換え。対象ユーザーに
    `LockoutEnabled=true` / `LockoutEnd=DisabledLockoutEnd` / `AccessFailedCount=0` を設定し `UpdateAsync`
  - `DisabledLockoutEnd` 定数（`9999-12-31 23:59:59 UTC`）を追加
  - `IsDisabled(ApplicationUser)` 静的ヘルパーを追加（`LockoutEnabled` かつ `LockoutEnd >= DisabledLockoutEnd`）
- 行 ViewModel に `IsDisabled` プロパティを追加し、`GetUserListAsync` 系のマッピングで設定
- `UserManagementController`: `Delete` / `DeleteConfirmed` のルート名は維持（Phycock 準拠で route 変更による影響を避ける）。
  内部で無効化処理を呼び、TempData メッセージを「無効化しました」に変更。初期 Admin の禁止メッセージも「無効化できません」に変更
- `Views/UserManagement/_IndexPartial.cshtml`:
  - 状態列に「無効」バッジ（`badge bg-secondary`）を追加
  - 操作ボタンの文言「削除」→「無効化」、初期 Admin の `disabled` ボタン title を変更

**既知の制約**: 再有効化機能は Phycock に存在せず、本移植にも含めない。無効化は一方向の操作であり、
元に戻すには DB 上で `LockoutEnd` をクリアする必要がある。Phycock の挙動をそのまま踏襲する。

### 3. ログインの email バグ修正

DevNext はカスタムユーザー名での登録を許可済み（コミット `4e88dc3`）だが、`AccountController.Login` は
`PasswordSignInAsync(model.Email, ...)` を使用している。`PasswordSignInAsync` の第 1 引数は **ユーザー名**として
扱われるため、ユーザー名 ≠ メールアドレスのユーザーはログイン不能になる既知バグがある。

- `AccountController.Login` (POST) を以下へ修正:
  - `FindByEmailAsync(model.Email)` でユーザー取得
  - `CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)` で認証（ロックアウト有効）
  - 成功時に `SignInAsync(user, model.RememberMe)` でサインイン
  - ユーザーが見つからない場合は `SignInResult.Failed` 相当として「無効なログイン試行です。」を表示
- ロックアウト・失敗時のセキュリティログ記録は現行のまま維持

※ Seed ユーザーは `UserName == Email` のため既存テストでは検出されない。
無効化済みユーザーは `LockoutEnd` が遠未来のため `CheckPasswordSignInAsync` が `IsLockedOut` を返し、Lockout 画面に遷移する。

### 4. ヘッダーメニューのドロップダウン化

- `Views/Shared/_LoginPartial.cshtml`: ログイン中の表示を、ユーザー名をトグルにした
  Bootstrap ドロップダウンに集約する
  - Admin: 「ユーザー管理」（`UserManagement/Index`）
  - Member: 「アカウント管理」（`Manage/Index`）
  - 区切り線 + 「ログアウト」（POST フォーム + AntiForgeryToken）
- **未ログイン時の「ログイン」「新規登録」リンクは現行のまま保全する**（DevNext は自己登録テンプレートを維持）

### 5. テスト

- `Tests/UserManagement/UserManagementServiceTests.cs` を新規作成
- 移植するテスト観点:
  - `ResetPasswordAsync` — 正常系 / ユーザー不在 / Identity ポリシー違反のエラー伝播
  - `UserManagementResetPasswordViewModel` — `NewPassword` 必須 / `ConfirmPassword` 不一致
  - `DisableUserAsync` — 正常系 / 初期 Admin ユーザーは無効化不可
- `CreateService` ヘルパーは DevNext の `UserManagementService` コンストラクタ署名
  （`IHttpContextAccessor` を取らない）に合わせて調整する
- `dotnet test` が全件パスすること

## 移植しないもの（DevNext の方針を維持）

| 項目 | 理由 |
|------|------|
| 単一ロール化（radio / `RoleName`） | DevNext は意図的に複数ロール対応（checkbox / `RoleNames`） |
| Member 選択・対象利用者切替（`GetSelectedMemberUserIdAsync` 等） | Phycock 固有ドメイン機能 |
| 登録の Admin 限定化（`Register` の `[Authorize(Roles=Admin)]` 化） | DevNext テンプレートは自己登録方式 |
| `ApplicationUser.ApplicationRoleName` フィールド | 複数ロール維持のため不要 |
| `_IndexPartial` の `<option selected>` 整形差分 | DevNext 側の記述で機能上問題なし |

## 完成条件

- 管理者が UserManagement の Edit から「パスワード変更」へ遷移し、対象ユーザーのパスワードを再設定できる（正常系）
- パスワードポリシー違反時、ResetPassword 画面にエラーが赤字表示される（異常系）
- ResetPassword / Disable は Admin のみ実行可能（認可）
- UserManagement の一覧でユーザーを無効化でき、一覧に「無効」バッジが表示される（正常系）
- 初期 Admin ユーザーは無効化できない（認可・制約）
- カスタムユーザー名（ユーザー名 ≠ メール）で登録したユーザーが、メールアドレスでログインできる（バグ修正の検証）
- 無効化済みユーザーはログインできず Lockout 画面に遷移する（異常系）
- ヘッダーがロール別ドロップダウンになり、未ログイン時の「新規登録」リンクが残っている
- `Tests/` に対応するテストを追加し `dotnet test` が全件パスする
- 既存の認証・ユーザー管理・基本 CRUD を壊していない（no-regression）

## 検証

```powershell
dotnet build DevNext.slnx
dotnet test .\Tests\Tests.csproj
```

実動確認: `dotnet run --project .\DevNext\DevNext.csproj` → ブラウザで以下を確認
- admin1 でログイン → UserManagement → Edit → パスワード変更 → member1 のパスワード再設定
- 再設定したパスワードで member1 がログイン成功
- UserManagement 一覧で member の無効化 → 「無効」バッジ表示 → 当該ユーザーのログイン不可
- 新規登録（ユーザー名 ≠ メール）→ メールでログイン成功
