# セットアップ手順

## 前提条件

| ツール | バージョン |
|--------|-----------|
| .NET SDK | 10.0 以上 |
| SQL Server | LocalDB / Express / Full |
| smtp4dev | メール機能を使う場合のみ |

---

## 1. リポジトリのクローン

```bash
git clone <repository-url>
cd DevNext
```

---

## 2. 接続文字列の設定

各プロジェクトの `appsettings.json` に接続文字列のプレースホルダーが入っています。
**実際の接続文字列は `appsettings.Development.json` に記載してください**（`.gitignore` 対象）。

`DevNext/appsettings.Development.json`（新規作成）:
```json
{
  "ConnectionStrings": {
    "SiteConnection": "Server=(localdb)\\mssqllocaldb;Database=DevNextDB;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

各 Sample プロジェクトも同様に `appsettings.Development.json` を作成してください。
すべて同じ `DevNextDB` を共有します。

---

## 3. DB 初期化（テーブル作成・Seed投入）

```bash
cd DbMigrationRunner
dotnet run
```

`EnsureCreatedAsync` によりテーブルが作成され、初期ユーザーが投入されます。

### 初期ユーザー

| UserName | Email | Password | Role |
|----------|-------|----------|------|
| admin1@sample.jp | admin1@sample.jp | Admin1! | Admin |
| member1@sample.jp | member1@sample.jp | Member1! | Member |

---

## 4. 開発サーバーの起動

### DevNext コア

```bash
cd DevNext
dotnet run
```

### サンプルプロジェクト（個別起動）

```bash
cd Samples/DatabaseSample
dotnet run
```

各サンプルは独立した Web アプリとして起動します。ポートが競合する場合は `launchSettings.json` で調整してください。

---

## 5. メール送信（MailSample 使用時）

smtp4dev を起動することでローカルでメール送信をキャプチャできます。

```powershell
smtp4dev-start.ps1
```

smtp4dev の管理画面: http://localhost:5000

---

## 6. ビルド・テスト確認

```bash
# DevNext コアのビルド
cd DevNext && dotnet build

# テスト実行
cd Tests && dotnet test
```
