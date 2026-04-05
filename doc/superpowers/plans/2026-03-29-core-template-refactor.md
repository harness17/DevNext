# Core Template Refactoring 実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** DevNextをサンプル集からコアテンプレートに整理し、各サンプル機能を独立した Samples/ プロジェクトに分離する。

**Architecture:** DevNextコアは認証・ユーザー管理・基本CRUDのみに絞る。DatabaseSample / FileSample / MailSample / WizardSample はそれぞれ独立した ASP.NET Core Web アプリとして Samples/ 配下に切り出す。各Sampleは CommonLibrary を参照し、Identity entities を共有DBで動かす。

**Tech Stack:** ASP.NET Core 10, Entity Framework Core, ASP.NET Core Identity, SQL Server (DevNextDB)

---

## ファイル構成（目標）

```
DevNext/                          ← コアのみ（認証・ユーザー管理・ApprovalRequest・Schedule）
CommonLibrary/                    ← ApplicationUser/Role/SiteEntityBase を追加
Samples/
  DatabaseSample/                 ← 独立 .csproj
  FileSample/                     ← 独立 .csproj
  MailSample/                     ← 独立 .csproj（MailSampleController + MailLogController）
  WizardSample/                   ← 独立 .csproj
docs/
  setup.md
  customization.md
  recipes/
    excel-export.md
    pdf-export.md
    bulk-edit.md
    file-upload.md
    wizard.md
```

---

## Task 1: ダッシュボード削除

**Files:**
- Delete: `DevNext/Controllers/DashboardController.cs`
- Delete: `DevNext/Service/DashboardService.cs`
- Delete: `DevNext/Views/Dashboard/Index.cshtml`
- Delete: `DevNext/Models/DashboardViewModels.cs`
- Modify: `DevNext/Views/Shared/_Layout.cshtml`
- Modify: `DevNext/Program.cs`

- [ ] **Step 1: ナビゲーションリンクを削除する**

  `DevNext/Views/Shared/_Layout.cshtml` から以下の行を削除する：
  ```html
  <li class="nav-item">
      <a class="nav-link text-white" asp-controller="Dashboard" asp-action="Index">ダッシュボード</a>
  </li>
  ```

- [ ] **Step 2: DI登録を削除する**

  `DevNext/Program.cs` から以下の行を削除する：
  ```csharp
  // ダッシュボード
  builder.Services.AddScoped<Site.Service.DashboardService>();
  ```

- [ ] **Step 3: 対象ファイルを削除する**

  ```bash
  rm DevNext/Controllers/DashboardController.cs
  rm DevNext/Service/DashboardService.cs
  rm DevNext/Views/Dashboard/Index.cshtml
  rmdir DevNext/Views/Dashboard
  rm DevNext/Models/DashboardViewModels.cs
  ```

- [ ] **Step 4: ビルドして確認する**

  ```bash
  cd DevNext && dotnet build
  ```
  期待: ビルドエラーなし

- [ ] **Step 5: コミットする**

  ```bash
  git add -A
  git commit -m "refactor: ダッシュボード機能を削除"
  ```

---

## Task 2: ApplicationUser / ApplicationRole / SiteEntityBase を CommonLibrary に移動する

サンプルプロジェクトが独立して動くために、Identity エンティティと SiteEntityBase を CommonLibrary に移動する。

**Files:**
- Create: `CommonLibrary/Entity/ApplicationUser.cs`
- Create: `CommonLibrary/Entity/ApplicationRole.cs`
- Create: `CommonLibrary/Entity/SiteEntityBase.cs`
- Modify: `DevNext/Entity/ApplicationUser.cs` → 削除
- Modify: `DevNext/Entity/ApplicationRole.cs` → 削除
- Modify: `DevNext/Entity/SiteEntityBase.cs` → 削除
- Modify: `DevNext/` 配下の全ファイル（using修正）
- Modify: `CommonLibrary/CommonLibrary.csproj`（Identity パッケージ追加）

- [ ] **Step 1: CommonLibrary.csproj に Identity パッケージを追加する**

  `CommonLibrary/CommonLibrary.csproj` に追加：
  ```xml
  <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.*" />
  ```

- [ ] **Step 2: CommonLibrary に ApplicationUser.cs を作成する**

  `CommonLibrary/Entity/ApplicationUser.cs`：
  ```csharp
  using Microsoft.AspNetCore.Identity;

  namespace Dev.CommonLibrary.Entity
  {
      /// <summary>
      /// アカウント情報
      /// </summary>
      public class ApplicationUser : IdentityUser
      {
          public ApplicationUser() : base()
          {
              PreviousUserPasswords = new List<UserPreviousPassword>();
          }

          public virtual IList<UserPreviousPassword> PreviousUserPasswords { get; set; }
          public DateTime? ResetPasswordTimeOut { get; set; }
          public DateTime? PasswordAvailableEndDate { get; set; }
          public string? ApplicationRoleName { get; set; }
          public string? UpdateApplicationUserId { get; set; }
      }
  }
  ```

- [ ] **Step 3: CommonLibrary に ApplicationRole.cs を作成する**

  既存の `DevNext/Entity/ApplicationRole.cs` の内容を確認し、`CommonLibrary/Entity/ApplicationRole.cs` として移動する（名前空間を `Dev.CommonLibrary.Entity` に変更）。

  ```csharp
  using Microsoft.AspNetCore.Identity;

  namespace Dev.CommonLibrary.Entity
  {
      /// <summary>
      /// ロール情報
      /// </summary>
      public class ApplicationRole : IdentityRole
      {
          public ApplicationRole() : base() { }
          public ApplicationRole(string roleName) : base(roleName) { }
      }
  }
  ```

- [ ] **Step 4: CommonLibrary に SiteEntityBase.cs を作成する**

  `CommonLibrary/Entity/SiteEntityBase.cs`：
  ```csharp
  namespace Dev.CommonLibrary.Entity
  {
      /// <summary>
      /// サイトエンティティベース
      /// </summary>
      public abstract class SiteEntityBase : EntityBase, IEntity
      {
          public long Id { get; set; }
      }
  }
  ```

- [ ] **Step 5: UserPreviousPassword を CommonLibrary に移動する**

  `DevNext/Entity/UserPreviousPassword.cs` の内容を `CommonLibrary/Entity/UserPreviousPassword.cs` にコピーし、名前空間を `Dev.CommonLibrary.Entity` に変更する。

- [ ] **Step 6: DevNext の元ファイルを削除する**

  ```bash
  rm DevNext/Entity/ApplicationUser.cs
  rm DevNext/Entity/ApplicationRole.cs
  rm DevNext/Entity/SiteEntityBase.cs
  rm DevNext/Entity/UserPreviousPassword.cs
  ```

- [ ] **Step 7: DevNext 全体の using を修正する**

  `DevNext/` 配下のファイルで `using Site.Entity;` で ApplicationUser / ApplicationRole / SiteEntityBase / UserPreviousPassword を使っている箇所は、CommonLibrary からインポートされるので問題ない（既に `using Dev.CommonLibrary.Entity;` が不要または追加済みであることを確認）。

  ```bash
  # 参照が壊れていないか確認
  cd DevNext && dotnet build
  ```

- [ ] **Step 8: コミットする**

  ```bash
  git add -A
  git commit -m "refactor: ApplicationUser/Role/SiteEntityBase を CommonLibrary に移動"
  ```

---

## Task 3: Samples/DatabaseSample プロジェクト作成

**Files:**
- Create: `Samples/DatabaseSample/DatabaseSample.csproj`
- Create: `Samples/DatabaseSample/Program.cs`
- Create: `Samples/DatabaseSample/appsettings.json`
- Create: `Samples/DatabaseSample/appsettings.Development.json`
- Create: `Samples/DatabaseSample/Data/DatabaseSampleDbContext.cs`
- Move: `DevNext/Entity/SampleEntity.cs` → `Samples/DatabaseSample/Entity/SampleEntity.cs`
- Move: `DevNext/Entity/SampleEntityChild.cs` → `Samples/DatabaseSample/Entity/SampleEntityChild.cs`
- Move: `DevNext/Controllers/DatabaseSampleController.cs` → `Samples/DatabaseSample/Controllers/`
- Move: `DevNext/Service/DatabaseSampleService.cs` → `Samples/DatabaseSample/Service/`
- Move: `DevNext/Service/ExportService.cs` → `Samples/DatabaseSample/Service/`
- Move: `DevNext/Repository/SampleEntityRepository.cs` → `Samples/DatabaseSample/Repository/`
- Move: `DevNext/Repository/SampleEntityChildRepository.cs` → `Samples/DatabaseSample/Repository/`
- Move: `DevNext/Views/DatabaseSample/` → `Samples/DatabaseSample/Views/DatabaseSample/`
- Move: `DevNext/Models/DatabaseSampleViewModels.cs` → `Samples/DatabaseSample/Models/`（存在する場合）
- Modify: `DevNext.sln`

- [ ] **Step 1: ディレクトリを作成する**

  ```bash
  mkdir -p Samples/DatabaseSample/{Controllers,Service,Repository,Entity,Data,Models,Views/{DatabaseSample,Shared}}
  ```

- [ ] **Step 2: .csproj を作成する**

  `Samples/DatabaseSample/DatabaseSample.csproj`：
  ```xml
  <Project Sdk="Microsoft.NET.Sdk.Web">
    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>
      <RootNamespace>DatabaseSample</RootNamespace>
    </PropertyGroup>
    <ItemGroup>
      <ProjectReference Include="..\..\CommonLibrary\CommonLibrary.csproj" />
    </ItemGroup>
    <ItemGroup>
      <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.*" />
      <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.*" />
      <PackageReference Include="ClosedXML" Version="*" />
      <PackageReference Include="QuestPDF" Version="*" />
      <PackageReference Include="CsvHelper" Version="*" />
      <PackageReference Include="AutoMapper" Version="*" />
      <PackageReference Include="Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation" Version="10.*" />
    </ItemGroup>
  </Project>
  ```
  > **注意**: バージョン番号は DevNext.csproj の既存パッケージと合わせること。

- [ ] **Step 3: DBContext を作成する**

  `Samples/DatabaseSample/Data/DatabaseSampleDbContext.cs`：
  ```csharp
  using Dev.CommonLibrary.Entity;
  using DatabaseSample.Entity;
  using Microsoft.AspNetCore.Identity;
  using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
  using Microsoft.EntityFrameworkCore;

  namespace DatabaseSample.Data
  {
      /// <summary>
      /// DatabaseSample 専用 DBContext
      /// </summary>
      public class DatabaseSampleDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
      {
          public DatabaseSampleDbContext(DbContextOptions<DatabaseSampleDbContext> options) : base(options) { }

          protected override void OnModelCreating(ModelBuilder modelBuilder)
          {
              base.OnModelCreating(modelBuilder);

              // DevNextコアと同じテーブル名にすることで、同一DBを共有できる
              modelBuilder.Entity<ApplicationUser>().ToTable("ApplicationUser");
              modelBuilder.Entity<ApplicationRole>().ToTable("ApplicationRole");
              modelBuilder.Entity<IdentityUserRole<string>>().ToTable("ApplicationUserRole");
              modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("ApplicationUserClaim");
              modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("ApplicationUserLogin");
              modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("ApplicationRoleClaim");
              modelBuilder.Entity<IdentityUserToken<string>>().ToTable("ApplicationUserToken");

              foreach (var property in modelBuilder.Model.GetEntityTypes()
                  .SelectMany(e => e.GetProperties())
                  .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
              {
                  property.SetColumnType("datetime2");
              }
          }

          public DbSet<UserPreviousPassword> UserPreviousPasswords { get; set; }
          public DbSet<SampleEntity> SampleEntity { get; set; }
          public DbSet<SampleEntityHistory> SampleEntityHistory { get; set; }
          public DbSet<SampleEntityChild> SampleEntityChild { get; set; }
          public DbSet<SampleEntityChildHistory> SampleEntityChildHistory { get; set; }
      }
  }
  ```

- [ ] **Step 4: Program.cs を作成する**

  `Samples/DatabaseSample/Program.cs`：
  ```csharp
  using DatabaseSample.Data;
  using DatabaseSample.Repository;
  using DatabaseSample.Service;
  using Dev.CommonLibrary.Entity;
  using Microsoft.AspNetCore.Identity;
  using Microsoft.EntityFrameworkCore;

  var builder = WebApplication.CreateBuilder(args);

  builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

  builder.Services.AddDbContext<DatabaseSampleDbContext>(options =>
      options.UseSqlServer(builder.Configuration.GetConnectionString("SiteConnection")));

  builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
  {
      options.Password.RequiredLength = 6;
      options.Password.RequireNonAlphanumeric = true;
      options.Password.RequireDigit = true;
      options.Password.RequireLowercase = true;
      options.Password.RequireUppercase = true;
  })
  .AddEntityFrameworkStores<DatabaseSampleDbContext>()
  .AddDefaultTokenProviders();

  builder.Services.ConfigureApplicationCookie(options =>
  {
      options.LoginPath = "/Account/Login";
      options.LogoutPath = "/Account/LogOff";
  });

  builder.Services.AddSession();
  builder.Services.AddHttpContextAccessor();
  builder.Services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));

  builder.Services.AddScoped<DatabaseSampleService>();
  builder.Services.AddScoped<ExportService>();
  builder.Services.AddScoped<SampleEntityRepository>();
  builder.Services.AddScoped<SampleEntityChildRepository>();
  builder.Services.AddScoped<Dev.CommonLibrary.Attributes.AccessLogAttribute>();

  var app = builder.Build();

  var accessor = app.Services.GetRequiredService<IHttpContextAccessor>();
  EntityBase.HttpContextAccessor = accessor;

  app.UseHttpsRedirection();
  app.UseStaticFiles();
  app.UseRouting();
  app.UseSession();
  app.UseAuthentication();
  app.UseAuthorization();

  app.MapControllerRoute(
      name: "default",
      pattern: "{controller=Home}/{action=Index}/{id?}");

  app.Run();
  ```

- [ ] **Step 5: appsettings.json を作成する**

  `Samples/DatabaseSample/appsettings.json`：
  ```json
  {
    "ConnectionStrings": {
      "SiteConnection": "Server=(localdb)\\mssqllocaldb;Database=DevNextDB;Trusted_Connection=True;MultipleActiveResultSets=true"
    },
    "Logging": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "AllowedHosts": "*"
  }
  ```

- [ ] **Step 6: エンティティを移動して名前空間を変更する**

  - `DevNext/Entity/SampleEntity.cs` を `Samples/DatabaseSample/Entity/SampleEntity.cs` にコピー
  - `DevNext/Entity/SampleEntityChild.cs` を `Samples/DatabaseSample/Entity/SampleEntityChild.cs` にコピー
  - 各ファイルの名前空間を `namespace DatabaseSample.Entity` に変更
  - `using Site.Common;` を `using Dev.CommonLibrary.Entity;` に変更
  - `using Site.Entity;` を不要であれば削除

- [ ] **Step 7: コントローラーを移動して名前空間を更新する**

  - `DevNext/Controllers/DatabaseSampleController.cs` を `Samples/DatabaseSample/Controllers/DatabaseSampleController.cs` にコピー
  - 名前空間を `namespace DatabaseSample.Controllers` に変更
  - `using Site.*` を `using DatabaseSample.*` / `using Dev.CommonLibrary.*` に更新

- [ ] **Step 8: サービス・リポジトリを移動して名前空間を更新する**

  - `DevNext/Service/DatabaseSampleService.cs` → `Samples/DatabaseSample/Service/`
  - `DevNext/Service/ExportService.cs` → `Samples/DatabaseSample/Service/`
  - `DevNext/Repository/SampleEntityRepository.cs` → `Samples/DatabaseSample/Repository/`
  - `DevNext/Repository/SampleEntityChildRepository.cs` → `Samples/DatabaseSample/Repository/`
  - 各ファイルの名前空間・using を更新（`Site.*` → `DatabaseSample.*`）

- [ ] **Step 9: Views を移動する**

  ```bash
  cp -r DevNext/Views/DatabaseSample Samples/DatabaseSample/Views/DatabaseSample
  ```

  - `Samples/DatabaseSample/Views/` に `_ViewStart.cshtml` / `_ViewImports.cshtml` を作成

  `_ViewStart.cshtml`：
  ```html
  @{
      Layout = "_Layout";
  }
  ```

  `_ViewImports.cshtml`：
  ```html
  @using DatabaseSample
  @using DatabaseSample.Models
  @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
  ```

  - `Samples/DatabaseSample/Views/Shared/_Layout.cshtml` を DevNext から参考に作成（DatabaseSample 用のシンプルなレイアウト）

- [ ] **Step 10: Models を移動する**

  DatabaseSample 関連の ViewModel を `Samples/DatabaseSample/Models/` にコピーし名前空間を更新する。

- [ ] **Step 11: .sln にプロジェクトを追加する**

  ```bash
  dotnet sln DevNext.sln add Samples/DatabaseSample/DatabaseSample.csproj
  ```

- [ ] **Step 12: ビルドして確認する**

  ```bash
  cd Samples/DatabaseSample && dotnet build
  ```
  期待: ビルドエラーなし

- [ ] **Step 13: コミットする**

  ```bash
  git add -A
  git commit -m "feat: Samples/DatabaseSample 独立プロジェクトを作成"
  ```

---

## Task 4: Samples/FileSample プロジェクト作成

**Files:**
- Create: `Samples/FileSample/FileSample.csproj`
- Create: `Samples/FileSample/Program.cs`
- Create: `Samples/FileSample/appsettings.json`
- Create: `Samples/FileSample/Data/FileSampleDbContext.cs`
- Move: `DevNext/Entity/FileEntity.cs` → `Samples/FileSample/Entity/`
- Move: `DevNext/Controllers/FileManagementController.cs` → `Samples/FileSample/Controllers/`
- Move: `DevNext/Service/FileManagementService.cs` → `Samples/FileSample/Service/`
- Move: `DevNext/Repository/FileEntityRepository.cs` → `Samples/FileSample/Repository/`
- Move: `DevNext/Views/FileManagement/` → `Samples/FileSample/Views/FileManagement/`

- [ ] **Step 1: ディレクトリを作成する**

  ```bash
  mkdir -p Samples/FileSample/{Controllers,Service,Repository,Entity,Data,Models,Views/{FileManagement,Shared}}
  ```

- [ ] **Step 2: .csproj を作成する**

  `Samples/FileSample/FileSample.csproj`：
  ```xml
  <Project Sdk="Microsoft.NET.Sdk.Web">
    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>
      <RootNamespace>FileSample</RootNamespace>
    </PropertyGroup>
    <ItemGroup>
      <ProjectReference Include="..\..\CommonLibrary\CommonLibrary.csproj" />
    </ItemGroup>
    <ItemGroup>
      <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.*" />
      <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.*" />
      <PackageReference Include="Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation" Version="10.*" />
    </ItemGroup>
  </Project>
  ```

- [ ] **Step 3: DBContext を作成する**

  `Samples/FileSample/Data/FileSampleDbContext.cs`：
  ```csharp
  using Dev.CommonLibrary.Entity;
  using FileSample.Entity;
  using Microsoft.AspNetCore.Identity;
  using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
  using Microsoft.EntityFrameworkCore;

  namespace FileSample.Data
  {
      public class FileSampleDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
      {
          public FileSampleDbContext(DbContextOptions<FileSampleDbContext> options) : base(options) { }

          protected override void OnModelCreating(ModelBuilder modelBuilder)
          {
              base.OnModelCreating(modelBuilder);
              modelBuilder.Entity<ApplicationUser>().ToTable("ApplicationUser");
              modelBuilder.Entity<ApplicationRole>().ToTable("ApplicationRole");
              modelBuilder.Entity<IdentityUserRole<string>>().ToTable("ApplicationUserRole");
              modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("ApplicationUserClaim");
              modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("ApplicationUserLogin");
              modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("ApplicationRoleClaim");
              modelBuilder.Entity<IdentityUserToken<string>>().ToTable("ApplicationUserToken");

              foreach (var property in modelBuilder.Model.GetEntityTypes()
                  .SelectMany(e => e.GetProperties())
                  .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
              {
                  property.SetColumnType("datetime2");
              }
          }

          public DbSet<UserPreviousPassword> UserPreviousPasswords { get; set; }
          public DbSet<FileEntity> FileEntity { get; set; }
      }
  }
  ```

- [ ] **Step 4: Program.cs を作成する**

  `Samples/FileSample/Program.cs`：
  ```csharp
  using Dev.CommonLibrary.Entity;
  using FileSample.Data;
  using FileSample.Repository;
  using FileSample.Service;
  using Microsoft.AspNetCore.Identity;
  using Microsoft.EntityFrameworkCore;

  var builder = WebApplication.CreateBuilder(args);

  builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

  builder.Services.AddDbContext<FileSampleDbContext>(options =>
      options.UseSqlServer(builder.Configuration.GetConnectionString("SiteConnection")));

  builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
  {
      options.Password.RequiredLength = 6;
      options.Password.RequireNonAlphanumeric = true;
      options.Password.RequireDigit = true;
      options.Password.RequireLowercase = true;
      options.Password.RequireUppercase = true;
  })
  .AddEntityFrameworkStores<FileSampleDbContext>()
  .AddDefaultTokenProviders();

  builder.Services.ConfigureApplicationCookie(options =>
  {
      options.LoginPath = "/Account/Login";
  });

  builder.Services.AddSession();
  builder.Services.AddHttpContextAccessor();
  builder.Services.AddScoped<FileManagementService>();
  builder.Services.AddScoped<FileEntityRepository>();
  builder.Services.AddScoped<Dev.CommonLibrary.Attributes.AccessLogAttribute>();

  var app = builder.Build();

  var accessor = app.Services.GetRequiredService<IHttpContextAccessor>();
  EntityBase.HttpContextAccessor = accessor;

  app.UseHttpsRedirection();
  app.UseStaticFiles();
  app.UseRouting();
  app.UseSession();
  app.UseAuthentication();
  app.UseAuthorization();

  app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
  app.Run();
  ```

- [ ] **Step 5: appsettings.json を作成する**

  `Samples/FileSample/appsettings.json`（DatabaseSample と同じ内容）

- [ ] **Step 6: ファイルを移動・名前空間更新する**

  - `FileEntity.cs` → `Samples/FileSample/Entity/FileEntity.cs`（名前空間: `FileSample.Entity`）
  - `FileManagementController.cs` → `Samples/FileSample/Controllers/`（名前空間: `FileSample.Controllers`）
  - `FileManagementService.cs` → `Samples/FileSample/Service/`（名前空間: `FileSample.Service`）
  - `FileEntityRepository.cs` → `Samples/FileSample/Repository/`（名前空間: `FileSample.Repository`）
  - `Views/FileManagement/` → `Samples/FileSample/Views/FileManagement/`
  - `_ViewStart.cshtml` / `_ViewImports.cshtml` / `_Layout.cshtml` を Shared/ に作成

- [ ] **Step 7: wwwroot/Uploads/Files フォルダを作成する**

  FileManagementService はファイルを `wwwroot/Uploads/Files/` に保存する。

  ```bash
  mkdir -p Samples/FileSample/wwwroot/Uploads/Files
  ```

- [ ] **Step 8: .sln に追加、ビルド確認、コミットする**

  ```bash
  dotnet sln DevNext.sln add Samples/FileSample/FileSample.csproj
  cd Samples/FileSample && dotnet build
  git add -A
  git commit -m "feat: Samples/FileSample 独立プロジェクトを作成"
  ```

---

## Task 5: Samples/MailSample プロジェクト作成

**Files:**
- Create: `Samples/MailSample/MailSample.csproj`
- Create: `Samples/MailSample/Program.cs`
- Create: `Samples/MailSample/appsettings.json`
- Create: `Samples/MailSample/Data/MailSampleDbContext.cs`
- Move: `DevNext/Entity/MailLogEntity.cs` → `Samples/MailSample/Entity/`
- Move: `DevNext/Controllers/MailSampleController.cs` → `Samples/MailSample/Controllers/`
- Move: `DevNext/Controllers/MailLogController.cs` → `Samples/MailSample/Controllers/`
- Move: `DevNext/Service/MailSampleService.cs` → `Samples/MailSample/Service/`
- Move: `DevNext/Service/MailLogService.cs` → `Samples/MailSample/Service/`
- Move: `DevNext/Repository/MailLogRepository.cs` → `Samples/MailSample/Repository/`
- Move: `DevNext/Views/MailSample/` → `Samples/MailSample/Views/MailSample/`
- Move: `DevNext/Views/MailLog/` → `Samples/MailSample/Views/MailLog/`
- Move: `DevNext/Common/Email.cs` → `Samples/MailSample/Common/`（またはコア共通化を検討）

- [ ] **Step 1: ディレクトリを作成する**

  ```bash
  mkdir -p Samples/MailSample/{Controllers,Service,Repository,Entity,Data,Common,Models,Views/{MailSample,MailLog,Shared}}
  ```

- [ ] **Step 2: .csproj を作成する**

  `Samples/MailSample/MailSample.csproj`：
  ```xml
  <Project Sdk="Microsoft.NET.Sdk.Web">
    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>
      <RootNamespace>MailSample</RootNamespace>
    </PropertyGroup>
    <ItemGroup>
      <ProjectReference Include="..\..\CommonLibrary\CommonLibrary.csproj" />
    </ItemGroup>
    <ItemGroup>
      <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.*" />
      <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.*" />
      <PackageReference Include="Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation" Version="10.*" />
    </ItemGroup>
  </Project>
  ```

- [ ] **Step 3: DBContext を作成する**

  `Samples/MailSample/Data/MailSampleDbContext.cs`：DatabaseSample と同パターンで `MailLogEntity` の DbSet を持つ DBContext を作成する。

  ```csharp
  // DbSet のみ異なる。Identity テーブル名設定は他プロジェクトと同一。
  public DbSet<UserPreviousPassword> UserPreviousPasswords { get; set; }
  public DbSet<MailLogEntity> MailLog { get; set; }
  ```

- [ ] **Step 4: Program.cs を作成する**

  メール設定（smtp4dev / SMTP）は appsettings.json で設定する。DI登録：
  - `MailSampleService`
  - `MailLogService`
  - `MailLogRepository`

  ```csharp
  builder.Services.AddScoped<MailSampleService>();
  builder.Services.AddScoped<MailLogService>();
  builder.Services.AddScoped<MailLogRepository>();
  ```

- [ ] **Step 5: appsettings.json を作成する**

  メール設定を含める：
  ```json
  {
    "ConnectionStrings": {
      "SiteConnection": "Server=(localdb)\\mssqllocaldb;Database=DevNextDB;Trusted_Connection=True;MultipleActiveResultSets=true"
    },
    "SmtpSettings": {
      "Host": "localhost",
      "Port": 1025,
      "FromAddress": "noreply@devnext.local",
      "FromName": "DevNext MailSample"
    },
    "Logging": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "AllowedHosts": "*"
  }
  ```

- [ ] **Step 6: ファイルを移動・名前空間更新する**

  - `MailLogEntity.cs` → `Samples/MailSample/Entity/`（名前空間: `MailSample.Entity`）
  - `MailSampleController.cs` / `MailLogController.cs` → `Samples/MailSample/Controllers/`
  - `MailSampleService.cs` / `MailLogService.cs` → `Samples/MailSample/Service/`
  - `MailLogRepository.cs` → `Samples/MailSample/Repository/`
  - `Email.cs`（メール送信ヘルパー）→ `Samples/MailSample/Common/Email.cs`
  - `Views/MailSample/` / `Views/MailLog/` → `Samples/MailSample/Views/`

- [ ] **Step 7: .sln に追加、ビルド確認、コミットする**

  ```bash
  dotnet sln DevNext.sln add Samples/MailSample/MailSample.csproj
  cd Samples/MailSample && dotnet build
  git add -A
  git commit -m "feat: Samples/MailSample 独立プロジェクトを作成"
  ```

---

## Task 6: Samples/WizardSample プロジェクト作成

**Files:**
- Create: `Samples/WizardSample/WizardSample.csproj`
- Create: `Samples/WizardSample/Program.cs`
- Create: `Samples/WizardSample/appsettings.json`
- Create: `Samples/WizardSample/Data/WizardSampleDbContext.cs`
- Move: `DevNext/Entity/WizardEntity.cs` → `Samples/WizardSample/Entity/`
- Move: `DevNext/Controllers/WizardSampleController.cs` → `Samples/WizardSample/Controllers/`
- Move: `DevNext/Service/WizardSampleService.cs` → `Samples/WizardSample/Service/`
- Move: `DevNext/Repository/WizardEntityRepository.cs` → `Samples/WizardSample/Repository/`
- Move: `DevNext/Views/WizardSample/` → `Samples/WizardSample/Views/WizardSample/`

- [ ] **Step 1: ディレクトリを作成する**

  ```bash
  mkdir -p Samples/WizardSample/{Controllers,Service,Repository,Entity,Data,Models,Views/{WizardSample,Shared}}
  ```

- [ ] **Step 2: .csproj を作成する**

  `Samples/WizardSample/WizardSample.csproj`（DatabaseSample と同パターン、ClosedXML/QuestPDF 不要）

- [ ] **Step 3: DBContext を作成する**

  `WizardSampleDbContext`：Identity テーブル + `WizardEntity` DbSet のみ

  ```csharp
  public DbSet<UserPreviousPassword> UserPreviousPasswords { get; set; }
  public DbSet<WizardEntity> WizardEntity { get; set; }
  ```

- [ ] **Step 4: Program.cs を作成する**

  DI登録：`WizardSampleService`、`WizardEntityRepository`

- [ ] **Step 5: appsettings.json を作成する**（DatabaseSample と同内容）

- [ ] **Step 6: ファイルを移動・名前空間更新する**

  - `WizardEntity.cs` → `Samples/WizardSample/Entity/`（名前空間: `WizardSample.Entity`）
  - `WizardSampleController.cs` → `Samples/WizardSample/Controllers/`
  - `WizardSampleService.cs` → `Samples/WizardSample/Service/`
  - `WizardEntityRepository.cs` → `Samples/WizardSample/Repository/`
  - `Views/WizardSample/` → `Samples/WizardSample/Views/WizardSample/`
  - `EnumDefine.cs` の `WizardCategory` など WizardSample 専用の Enum を `Samples/WizardSample/Common/EnumDefine.cs` に移動

- [ ] **Step 7: .sln に追加、ビルド確認、コミットする**

  ```bash
  dotnet sln DevNext.sln add Samples/WizardSample/WizardSample.csproj
  cd Samples/WizardSample && dotnet build
  git add -A
  git commit -m "feat: Samples/WizardSample 独立プロジェクトを作成"
  ```

---

## Task 7: DevNext コアのクリーンアップ

サンプルを移動した後、DevNext 本体から不要なファイルを削除し、Program.cs / DBContext / ナビゲーションをコア専用にする。

**Files:**
- Delete: 移動済みのサンプルファイル群（Entity / Controllers / Service / Repository / Views）
- Modify: `DevNext/Common/DBContext.cs`（サンプル DbSet を削除）
- Modify: `DevNext/Program.cs`（サンプル DI 登録を削除）
- Modify: `DevNext/Views/Shared/_Layout.cshtml`（サンプルナビを削除）
- Delete: `DevNext/Controllers/ViewSampleController.cs`（CLAUDE.md で未定義のため、削除して整理）
- Delete: `DevNext/Views/ViewSample/`

- [ ] **Step 1: 移動済みのサンプルファイルを削除する**

  削除するコントローラー：
  ```bash
  rm DevNext/Controllers/DatabaseSampleController.cs
  rm DevNext/Controllers/FileManagementController.cs
  rm DevNext/Controllers/MailSampleController.cs
  rm DevNext/Controllers/MailLogController.cs
  rm DevNext/Controllers/WizardSampleController.cs
  rm DevNext/Controllers/ViewSampleController.cs
  ```

  削除するサービス：
  ```bash
  rm DevNext/Service/DatabaseSampleService.cs
  rm DevNext/Service/ExportService.cs
  rm DevNext/Service/FileManagementService.cs
  rm DevNext/Service/MailSampleService.cs
  rm DevNext/Service/MailLogService.cs
  rm DevNext/Service/WizardSampleService.cs
  ```

  削除するリポジトリ：
  ```bash
  rm DevNext/Repository/SampleEntityRepository.cs
  rm DevNext/Repository/SampleEntityChildRepository.cs
  rm DevNext/Repository/FileEntityRepository.cs
  rm DevNext/Repository/MailLogRepository.cs
  rm DevNext/Repository/WizardEntityRepository.cs
  ```

  削除するビュー：
  ```bash
  rm -rf DevNext/Views/DatabaseSample
  rm -rf DevNext/Views/FileManagement
  rm -rf DevNext/Views/MailSample
  rm -rf DevNext/Views/MailLog
  rm -rf DevNext/Views/WizardSample
  rm -rf DevNext/Views/ViewSample
  ```

- [ ] **Step 2: DBContext.cs からサンプル DbSet を削除する**

  `DevNext/Common/DBContext.cs` を開き、以下を削除する：
  ```csharp
  public DbSet<SampleEntity> SampleEntity { get; set; }
  public DbSet<SampleEntityHistory> SampleEntityHistory { get; set; }
  public DbSet<SampleEntityChild> SampleEntityChild { get; set; }
  public DbSet<SampleEntityChildHistory> SampleEntityChildHistory { get; set; }
  public DbSet<FileEntity> FileEntity { get; set; }
  public DbSet<WizardEntity> WizardEntity { get; set; }
  public DbSet<MailLogEntity> MailLog { get; set; }
  ```

- [ ] **Step 3: Program.cs からサンプル DI 登録を削除する**

  以下を削除する：
  ```csharp
  builder.Services.AddScoped<DatabaseSampleService>();
  builder.Services.AddScoped<Site.Service.MailSampleService>();
  builder.Services.AddScoped<Site.Service.MailLogService>();
  builder.Services.AddScoped<Site.Service.FileManagementService>();
  builder.Services.AddScoped<Site.Service.WizardSampleService>();
  builder.Services.AddScoped<Site.Service.ExportService>();
  ```

- [ ] **Step 4: _Layout.cshtml のナビゲーションをコア専用に整理する**

  サンプルドロップダウン全体を削除し、コア機能のみ残す：
  ```html
  <ul class="navbar-nav flex-grow-1">
      <li class="nav-item">
          <a class="nav-link text-white" asp-controller="Home" asp-action="Index">ホーム</a>
      </li>
      <li class="nav-item">
          <a class="nav-link text-white" asp-controller="ApprovalRequest" asp-action="Index">承認申請</a>
      </li>
      <li class="nav-item">
          <a class="nav-link text-white" asp-controller="Schedule" asp-action="Index">スケジュール</a>
      </li>
      @if (User.IsInRole("Admin"))
      {
          <li class="nav-item">
              <a class="nav-link text-white" asp-controller="UserManagement" asp-action="Index">ユーザー管理</a>
          </li>
      }
  </ul>
  ```

- [ ] **Step 5: ビルドして確認する**

  ```bash
  cd DevNext && dotnet build
  ```

- [ ] **Step 6: テストを実行する**

  ```bash
  cd Tests && dotnet test
  ```

- [ ] **Step 7: コミットする**

  ```bash
  git add -A
  git commit -m "refactor: サンプル機能を DevNext コアから削除してクリーンアップ"
  ```

---

## Task 8: ドキュメント整備

**Files:**
- Create: `docs/setup.md`
- Create: `docs/customization.md`
- Create: `docs/recipes/excel-export.md`
- Create: `docs/recipes/pdf-export.md`
- Create: `docs/recipes/bulk-edit.md`
- Create: `docs/recipes/file-upload.md`
- Create: `docs/recipes/wizard.md`
- Modify: `README.md`

- [ ] **Step 1: docs/setup.md を作成する**

  内容：
  - 前提条件（.NET 10, SQL Server, smtp4dev）
  - リポジトリのクローン
  - 接続文字列設定（appsettings.json）
  - DB初期化（DbMigrationRunner）
  - 開発サーバー起動
  - 初期ユーザー（admin1@sample.jp / Admin1!）

- [ ] **Step 2: docs/customization.md を作成する**

  内容：
  - 名前空間の変更箇所（`Site` → プロジェクト名）
  - DB名の変更箇所
  - アプリ名の変更箇所（`DevNext` → プロジェクト名）
  - 削除すべき機能（ApprovalRequest/Schedule を削除する手順）

- [ ] **Step 3: docs/recipes/ の各 Markdown を作成する**

  - `excel-export.md`：DatabaseSampleService の Excel エクスポート実装パターン（ClosedXML 使用）
  - `pdf-export.md`：QuestPDF による PDF 生成パターン
  - `bulk-edit.md`：親子エンティティの一括編集パターン（SampleEntity + SampleEntityChild）
  - `file-upload.md`：FileSample のファイルアップロード・ダウンロード・削除パターン
  - `wizard.md`：WizardSample の多段階フォームパターン（Session 利用）

- [ ] **Step 4: README.md を更新する**

  新構造（Samples/ / docs/）に合わせて記載を整理する。

- [ ] **Step 5: コミットする**

  ```bash
  git add docs/ README.md
  git commit -m "docs: setup.md / customization.md / recipes を追加"
  ```

---

## 注意事項

1. **各タスクはビルド確認後にコミットする**（壊れた状態でコミットしない）
2. **Email.cs の扱い**：DevNext/Common/Email.cs は MailSample に移動する。DevNext コア側でメール機能が不要なら削除、必要なら CommonLibrary に移動する
3. **EnumDefine.cs**：DevNext/Common/EnumDefine.cs には複数の Enum が混在している。サンプル専用の Enum（SampleEnum, WizardCategory 等）は各 Sample プロジェクトに移動し、コア用の Enum のみ残す
4. **ViewSample**：CLAUDE.md に明示されていないが、コア設計指針「サンプル機能をコア本体に追加しない」に従い削除する
5. **DbMigrationRunner**：コアの DBContext 変更後、Seeder の参照を更新して再実行確認が必要
