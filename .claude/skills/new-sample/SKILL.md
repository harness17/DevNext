---
name: new-sample
description: 新規 Sample プロジェクトを Samples/ に追加する手順。独立DBContext・Program.cs・appsettings・sln登録・deploy スクリプト更新まで一括実施。
---

新しい Sample プロジェクトを `Samples/` に追加します。
以下の「XxxSample」をユーザーが指定した名前（例: `MailSample`, `ReportSample`）に置き換えて実行してください。

## 前提確認

Sample 名をユーザーに確認する。まだ決まっていなければ提案を求める。

---

## Step 1: プロジェクトの雛形作成

```bash
cd H:/ClaudeCode/DevNext/Samples
dotnet new web -n XxxSample --no-restore
```

---

## Step 2: CommonLibrary 参照を追加

`Samples/XxxSample/XxxSample.csproj` に ProjectReference を追記する：

```xml
<ItemGroup>
  <ProjectReference Include="..\..\CommonLibrary\CommonLibrary.csproj" />
</ItemGroup>
```

---

## Step 3: 独立 DBContext を作成

`Samples/XxxSample/Data/XxxSampleDbContext.cs` を作成する。

```csharp
using Microsoft.EntityFrameworkCore;

namespace XxxSample.Data;

/// <summary>
/// XxxSample 専用 DBContext。
/// コアの DBContext（DevNextDB）とは完全に分離する。
/// </summary>
public class XxxSampleDbContext(DbContextOptions<XxxSampleDbContext> options) : DbContext(options)
{
    // DbSet をここに追加する
    // 例: public DbSet<XxxEntity> XxxEntities { get; set; }
}
```

---

## Step 4: Program.cs を設定

`Samples/XxxSample/Program.cs` を以下のように設定する。

```csharp
using Microsoft.EntityFrameworkCore;
using XxxSample.Data;

var builder = WebApplication.CreateBuilder(args);

// DB（Sample 専用。EnsureCreated で自動作成するため Migration 不要）
builder.Services.AddDbContext<XxxSampleDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("XxxSampleConnection")));

builder.Services.AddControllersWithViews();

var app = builder.Build();

// DB 自動作成（起動時に EnsureCreated を実行）
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<XxxSampleDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();
```

---

## Step 5: appsettings.json に接続文字列を追加

`Samples/XxxSample/appsettings.json` に追記する：

```json
{
  "ConnectionStrings": {
    "XxxSampleConnection": "Server=(localdb)\\mssqllocaldb;Database=XxxSampleDB;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

DB 名は `XxxSampleDB` のように Sample ごとに一意にする。

---

## Step 6: ソリューションに追加

```bash
cd H:/ClaudeCode/DevNext
dotnet sln DevNext.slnx add Samples/XxxSample/XxxSample.csproj
```

---

## Step 7: deploy-samples.ps1 を更新

`scripts/deploy-samples.ps1` に XxxSample のデプロイブロックを追加する（既存 Sample のパターンを参照して追記）。

---

## Step 8: ビルド確認

```bash
cd H:/ClaudeCode/DevNext && dotnet build DevNext.slnx
```

エラーがあれば修正する。

---

## Step 9: AGENTS.md・CLAUDE.md を更新

`/update-harness` を実行して Samples 一覧への追記など harness を最新化する。

---

## 設計指針（厳守）

- **Sample 同士は依存しない**（XxxSample が YyySample を参照するなど禁止）
- 各 Sample は単独でビルド・起動できる状態にする
- エンティティ・DBContext は Sample 専用とし、コアの DBContext とは分離する
- DB は `EnsureCreated` で起動時に自動作成する（Migration 管理対象外）
- コアの `Program.cs` に Sample 依存の DI 登録を追加しない
