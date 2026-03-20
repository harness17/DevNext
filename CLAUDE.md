# DevNext

ASP.NET Core 10 の Web アプリケーション。旧 .NET Framework 版（DevNet）からの移行プロジェクト。
メンテナンス性を最優先とする。
可能な限りコメントを生成

## プロジェクト構成

| プロジェクト | 概要 |
|---|---|
| `DevNext/` | メイン Web アプリ（ASP.NET Core 10、RootNamespace: `Site`） |
| `CommonLibrary/` | 共通ライブラリ（RootNamespace: `Dev.CommonLibrary`） |
| `DbMigrationRunner/` | DB 作成・Seed データ投入ツール |
| `BatchSample/` | バッチ処理サンプル |
| `Tests/` | xUnit テストプロジェクト |
| `DevNet/` | 旧 .NET Framework 版（参照用） |

## データベース

- **SQL Server**、DB名: `DevNextDB`、接続文字列キー: `SiteConnection`
- 接続設定: 各プロジェクトの `appsettings.json`
- DB 作成・Seed 投入は `DbMigrationRunner` を実行（`EnsureCreatedAsync` 使用）
- マイグレーションファイルは使用しない

### Seed データ（初期ユーザー）

| UserName | Email | Password | Role |
|---|---|---|---|
| admin1@sample.jp | admin1@sample.jp | Admin1! | Admin |
| member1@sample.jp | member1@sample.jp | Member1! | Member |

## パスワードポリシー

`Program.cs` で設定。最低6文字、大文字・小文字・数字・記号すべて必須。

## DI 登録ルール

- `[ServiceFilter(typeof(XxxAttribute))]` を使う場合、対象クラスを `Program.cs` で `AddScoped` 登録する
- 例: `builder.Services.AddScoped<AccessLogAttribute>()`

## テスト

```bash
cd Tests && dotnet test
```

- フレームワーク: xUnit + Moq
- `SignInResult` は `Microsoft.AspNetCore.Identity.SignInResult` と `Microsoft.AspNetCore.Mvc.SignInResult` が競合するため、エイリアスを使用
  ```csharp
  using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;
  ```

## 名前空間

| 場所 | 名前空間 |
|---|---|
| `DevNext/` | `Site.*` |
| `CommonLibrary/` | `Dev.CommonLibrary.*` |
| `DbMigrationRunner/` | `DbMigrationRunner` |
