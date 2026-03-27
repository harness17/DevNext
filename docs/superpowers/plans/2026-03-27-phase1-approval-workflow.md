# Phase 1：承認ワークフロー 実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 申請者（Member/Admin）が申請を作成し、承認者（Admin）が承認・却下できる承認ワークフロー機能を実装する。

**Architecture:** 既存の DatabaseSample パターン（Entity → Repository → Service → Controller → View）に準拠する。状態遷移ロジックは `ApprovalWorkflowService` に集約し、Controller は薄く保つ。ロール別表示（Member: 自申請のみ / Admin: 全申請）は Service 層で制御する。

**Tech Stack:** ASP.NET Core 10 MVC, Entity Framework Core, ASP.NET Core Identity（ロール制御）, Bootstrap 5, Font Awesome

---

## ファイル一覧

| 操作 | ファイルパス | 役割 |
|------|------------|------|
| 新規 | `DevNext/Entity/ApprovalRequestEntity.cs` | エンティティ定義 |
| 修正 | `DevNext/Common/EnumDefine.cs` | ApprovalStatus enum 追加 |
| 修正 | `DevNext/Common/DBContext.cs` | DbSet 追加 |
| 修正 | `DevNext/Common/SessionKey.cs` | TempData キー追加 |
| 修正 | `DbMigrationRunner/Program.cs` | テーブル作成 SQL 追加 |
| 新規 | `DevNext/Repository/ApprovalRequestRepository.cs` | CRUD + 検索クエリ |
| 新規 | `DevNext/Models/ApprovalRequestViewModels.cs` | ViewModel 群 |
| 新規 | `DevNext/Service/ApprovalWorkflowService.cs` | 状態遷移ロジック |
| 修正 | `DevNext/Program.cs` | サービス DI 登録 |
| 新規 | `DevNext/Controllers/ApprovalRequestController.cs` | アクション定義 |
| 新規 | `DevNext/Views/ApprovalRequest/Index.cshtml` | 申請一覧（検索フォーム） |
| 新規 | `DevNext/Views/ApprovalRequest/_IndexPartial.cshtml` | 一覧テーブル（Ajax用） |
| 新規 | `DevNext/Views/ApprovalRequest/Create.cshtml` | 新規作成フォーム |
| 新規 | `DevNext/Views/ApprovalRequest/Edit.cshtml` | 編集フォーム |
| 新規 | `DevNext/Views/ApprovalRequest/Detail.cshtml` | 詳細・承認/却下フォーム |
| 新規 | `DevNext/Views/ApprovalRequest/Delete.cshtml` | 削除確認 |
| 修正 | `DevNext/Views/Shared/_Layout.cshtml` | ナビバーにリンク追加 |
| 修正 | `DevNext/Views/Home/Index.cshtml` | ホームにカード追加 |

---

## Task 1：Enum の追加

**Files:**
- Modify: `DevNext/Common/EnumDefine.cs`

- [ ] **Step 1: ApprovalStatus enum を EnumDefine.cs に追加する**

`DevNext/Common/EnumDefine.cs` の末尾（`}` の直前）に追加：

```csharp
    /// <summary>承認申請の状態</summary>
    public enum ApprovalStatus
    {
        [Display(Name = "下書き")]
        Draft = 1,
        [Display(Name = "申請中")]
        Pending = 2,
        [Display(Name = "承認済み")]
        Approved = 3,
        [Display(Name = "却下")]
        Rejected = 4,
    }
```

- [ ] **Step 2: ビルドして確認する**

```bash
cd DevNext && dotnet build
```

期待結果: `Build succeeded.`

- [ ] **Step 3: コミットする**

```bash
git add DevNext/Common/EnumDefine.cs
git commit -m "feat: ApprovalStatus enum を追加"
```

---

## Task 2：エンティティの作成

**Files:**
- Create: `DevNext/Entity/ApprovalRequestEntity.cs`

- [ ] **Step 1: ApprovalRequestEntity.cs を作成する**

```csharp
using Site.Common;
using System.ComponentModel.DataAnnotations;

namespace Site.Entity
{
    /// <summary>
    /// 承認申請エンティティ
    /// 申請者が作成し、承認者（Admin）が承認・却下する。
    /// </summary>
    public class ApprovalRequestEntity : SiteEntityBase
    {
        /// <summary>申請タイトル</summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = "";

        /// <summary>申請内容</summary>
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = "";

        /// <summary>申請状態</summary>
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Draft;

        /// <summary>申請者のユーザーID（ApplicationUser.Id）</summary>
        [Required]
        [MaxLength(450)]
        public string RequesterUserId { get; set; } = "";

        /// <summary>承認者コメント（承認・却下時に入力）</summary>
        [MaxLength(1000)]
        public string? ApproverComment { get; set; }

        /// <summary>申請（Pending 移行）日時</summary>
        public DateTime? RequestedDate { get; set; }

        /// <summary>承認・却下が確定した日時</summary>
        public DateTime? ApprovedDate { get; set; }
    }
}
```

- [ ] **Step 2: ビルドして確認する**

```bash
cd DevNext && dotnet build
```

期待結果: `Build succeeded.`

- [ ] **Step 3: コミットする**

```bash
git add DevNext/Entity/ApprovalRequestEntity.cs
git commit -m "feat: ApprovalRequestEntity を追加"
```

---

## Task 3：DBContext と SessionKey の更新

**Files:**
- Modify: `DevNext/Common/DBContext.cs`
- Modify: `DevNext/Common/SessionKey.cs`

- [ ] **Step 1: DBContext.cs に DbSet を追加する**

`DevNext/Common/DBContext.cs` の `#region DbSet` 内の末尾（`#endregion` の直前）に追加：

```csharp
        // 承認ワークフロー
        public DbSet<ApprovalRequestEntity> ApprovalRequest { get; set; }
```

- [ ] **Step 2: SessionKey.cs に TempData キーを追加する**

`DevNext/Common/SessionKey.cs` の末尾（最後の `}` の直前）に追加：

```csharp
        // 承認ワークフロー
        public static string ApprovalRequestCondViewModel = "ApprovalRequestCondViewModel";
        public static string ApprovalRequestPageModel = "ApprovalRequestPageModel";
```

- [ ] **Step 3: ビルドして確認する**

```bash
cd DevNext && dotnet build
```

期待結果: `Build succeeded.`

- [ ] **Step 4: コミットする**

```bash
git add DevNext/Common/DBContext.cs DevNext/Common/SessionKey.cs
git commit -m "feat: DBContext に ApprovalRequest を追加、SessionKey を更新"
```

---

## Task 4：DbMigrationRunner の更新

**Files:**
- Modify: `DbMigrationRunner/Program.cs`

- [ ] **Step 1: ApplyMissingTablesAsync に ApprovalRequest テーブル作成ブロックを追加する**

`DbMigrationRunner/Program.cs` の `ApplyMissingTablesAsync` メソッド内、末尾の `}` 直前に追加：

```csharp
            // ─── ApprovalRequest ──────────────────────────────────────────
            Console.WriteLine("  テーブル [ApprovalRequest] を確認しています...");
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApprovalRequest')
                BEGIN
                    CREATE TABLE [ApprovalRequest] (
                        [Id]                        bigint          NOT NULL IDENTITY(1,1),
                        [Title]                     nvarchar(200)   NOT NULL,
                        [Content]                   nvarchar(2000)  NOT NULL,
                        [Status]                    int             NOT NULL,
                        [RequesterUserId]           nvarchar(450)   NOT NULL,
                        [ApproverComment]           nvarchar(1000)  NULL,
                        [RequestedDate]             datetime2(7)    NULL,
                        [ApprovedDate]              datetime2(7)    NULL,
                        [DelFlag]                   bit             NOT NULL,
                        [UpdateApplicationUserId]   nvarchar(max)   NULL,
                        [CreateApplicationUserId]   nvarchar(max)   NULL,
                        [UpdateDate]                datetime2(7)    NOT NULL,
                        [CreateDate]                datetime2(7)    NOT NULL,
                        CONSTRAINT [PK_ApprovalRequest] PRIMARY KEY ([Id])
                    )
                END");
```

- [ ] **Step 2: DbMigrationRunner を実行してテーブルを作成する**

```bash
cd DbMigrationRunner && dotnet run
```

期待結果:
```
✓ 処理が完了しました。
```

SQL Server Management Studio 等で `ApprovalRequest` テーブルが作成されていることを確認する。

- [ ] **Step 3: コミットする**

```bash
git add DbMigrationRunner/Program.cs
git commit -m "feat: DbMigrationRunner に ApprovalRequest テーブル作成を追加"
```

---

## Task 5：Repository の実装

**Files:**
- Create: `DevNext/Repository/ApprovalRequestRepository.cs`

- [ ] **Step 1: ApprovalRequestRepository.cs を作成する**

```csharp
using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;
using Site.Common;
using Site.Entity;
using Site.Models;

namespace Site.Repository
{
    /// <summary>
    /// 承認申請リポジトリ
    /// RepositoryBase を継承して共通CRUD操作を利用する。
    /// ApprovalRequestEntity に履歴テーブルは存在しないため THistory に同型を指定。
    /// </summary>
    public class ApprovalRequestRepository : RepositoryBase<ApprovalRequestEntity, ApprovalRequestEntity, ApprovalRequestCondModel>
    {
        private readonly DBContext _context;

        public ApprovalRequestRepository(DBContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// 一覧取得（ソート・ページング付き）
        /// </summary>
        public ApprovalRequestListData GetList(ApprovalRequestCondModel cond)
        {
            var model = new ApprovalRequestListData();
            IQueryable<ApprovalRequestEntity> query = GetBaseQuery(cond);

            // デフォルトソートは Id 降順
            cond.Pager.sort = string.IsNullOrEmpty(cond.Pager.sort) ? "Id" : cond.Pager.sort;
            cond.Pager.sortdir = string.IsNullOrEmpty(cond.Pager.sortdir) ? "DESC" : cond.Pager.sortdir;

            if (cond.Pager.sortdir.ToLower() == "desc")
            {
                query = cond.Pager.sort switch
                {
                    "Title" => query.OrderByDescending(x => x.Title),
                    "Status" => query.OrderByDescending(x => x.Status),
                    "RequestedDate" => query.OrderByDescending(x => x.RequestedDate),
                    _ => query.OrderByDescending(x => x.Id)
                };
            }
            else
            {
                query = cond.Pager.sort switch
                {
                    "Title" => query.OrderBy(x => x.Title),
                    "Status" => query.OrderBy(x => x.Status),
                    "RequestedDate" => query.OrderBy(x => x.RequestedDate),
                    _ => query.OrderBy(x => x.Id)
                };
            }

            int totalRecords = query.Count();
            LocalUtil.SetTakeSkip(ref query, cond);
            model.Rows = query.ToList();
            model.Summary = Util.CreateSummary(cond.Pager, totalRecords, "{0}件中 {1} - {2} を表示");
            return model;
        }

        /// <summary>
        /// 検索条件付きクエリを返す（論理削除フィルタ付き）
        /// </summary>
        public override IQueryable<ApprovalRequestEntity> GetBaseQuery(ApprovalRequestCondModel? cond = null, bool includeDelete = false)
        {
            IQueryable<ApprovalRequestEntity> query = dbSet.Where(x => includeDelete ? true : !x.DelFlag);

            if (cond != null)
            {
                // 申請者 ID による絞り込み（Member ロールは自分の申請のみ参照できる）
                if (!string.IsNullOrEmpty(cond.RequesterUserId))
                    query = query.Where(x => x.RequesterUserId == cond.RequesterUserId);

                // タイトルの部分一致検索
                if (!string.IsNullOrEmpty(cond.Title))
                    query = query.Where(x => x.Title.Contains(cond.Title));

                // 状態による絞り込み
                if (cond.Status != null)
                    query = query.Where(x => x.Status == cond.Status);
            }

            return query;
        }
    }

    /// <summary>
    /// Repository 用の検索条件モデル（View 用 ViewModel とは分離する）
    /// </summary>
    public class ApprovalRequestCondModel : IRepositoryCondModel
    {
        public string? RequesterUserId { get; set; }
        public string? Title { get; set; }
        public ApprovalStatus? Status { get; set; }
        public CommonListPagerModel Pager { get; set; } = new(1, "Id", "DESC", 10);
    }

    /// <summary>一覧取得結果</summary>
    public class ApprovalRequestListData
    {
        public List<ApprovalRequestEntity> Rows { get; set; } = new();
        public string Summary { get; set; } = "";
    }
}
```

- [ ] **Step 2: ビルドして確認する**

```bash
cd DevNext && dotnet build
```

期待結果: `Build succeeded.`

- [ ] **Step 3: コミットする**

```bash
git add DevNext/Repository/ApprovalRequestRepository.cs
git commit -m "feat: ApprovalRequestRepository を追加"
```

---

## Task 6：ViewModels の実装

**Files:**
- Create: `DevNext/Models/ApprovalRequestViewModels.cs`

- [ ] **Step 1: ApprovalRequestViewModels.cs を作成する**

```csharp
using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Mvc.Rendering;
using Site.Common;
using Site.Repository;
using System.ComponentModel.DataAnnotations;

namespace Site.Models
{
    // ─── 一覧ページ用 ─────────────────────────────────────────────────────────

    /// <summary>申請一覧ページの ViewModel</summary>
    public class ApprovalRequestIndexViewModel : SearchModelBase
    {
        public ApprovalRequestCondViewModel? Cond { get; set; }
        public ApprovalRequestListData? RowData { get; set; }
    }

    /// <summary>申請一覧の検索条件（View 用）</summary>
    public class ApprovalRequestCondViewModel
    {
        [Display(Name = "タイトル")]
        public string? Title { get; set; }

        [Display(Name = "状態")]
        public string? Status { get; set; }

        /// <summary>状態ドロップダウン用の選択肢リスト</summary>
        public List<SelectListItem> StatusList { get; set; } = new();
    }

    // ─── 作成・編集フォーム用 ──────────────────────────────────────────────────

    /// <summary>申請の作成・編集フォーム用 ViewModel</summary>
    public class ApprovalRequestFormViewModel
    {
        public long Id { get; set; }

        [Display(Name = "タイトル")]
        [Required(ErrorMessage = "タイトルは必須です")]
        [MaxLength(200, ErrorMessage = "200文字以内で入力してください")]
        public string Title { get; set; } = "";

        [Display(Name = "申請内容")]
        [Required(ErrorMessage = "申請内容は必須です")]
        [MaxLength(2000, ErrorMessage = "2000文字以内で入力してください")]
        public string Content { get; set; } = "";

        /// <summary>申請ボタン押下時 true。下書き保存は false。</summary>
        public bool SubmitRequest { get; set; } = false;
    }

    // ─── 詳細・承認操作用 ──────────────────────────────────────────────────────

    /// <summary>申請詳細・承認/却下操作用 ViewModel</summary>
    public class ApprovalRequestDetailViewModel
    {
        public long Id { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public ApprovalStatus Status { get; set; }
        public string RequesterUserId { get; set; } = "";
        public string RequesterName { get; set; } = "";
        public string? ApproverComment { get; set; }
        public DateTime? RequestedDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime CreateDate { get; set; }

        // 承認・却下フォーム用
        [Display(Name = "コメント")]
        [MaxLength(1000, ErrorMessage = "1000文字以内で入力してください")]
        public string? ActionComment { get; set; }
    }

    // ─── 削除確認用 ────────────────────────────────────────────────────────────

    /// <summary>削除確認ページ用 ViewModel</summary>
    public class ApprovalRequestDeleteViewModel
    {
        public long Id { get; set; }
        public string Title { get; set; } = "";
        public ApprovalStatus Status { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
```

- [ ] **Step 2: ビルドして確認する**

```bash
cd DevNext && dotnet build
```

期待結果: `Build succeeded.`

- [ ] **Step 3: コミットする**

```bash
git add DevNext/Models/ApprovalRequestViewModels.cs
git commit -m "feat: ApprovalRequestViewModels を追加"
```

---

## Task 7：Service の実装

**Files:**
- Create: `DevNext/Service/ApprovalWorkflowService.cs`

- [ ] **Step 1: ApprovalWorkflowService.cs を作成する**

```csharp
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Site.Common;
using Site.Entity;
using Site.Models;
using Site.Repository;

namespace Site.Service
{
    /// <summary>
    /// 承認ワークフローのビジネスロジックを担うサービス。
    /// 状態遷移・権限チェック・申請者名の解決をここで行う。
    /// </summary>
    public class ApprovalWorkflowService
    {
        private readonly DBContext _context;
        private readonly ApprovalRequestRepository _repo;
        private readonly UserManager<ApplicationUser> _userManager;

        public ApprovalWorkflowService(DBContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _repo = new ApprovalRequestRepository(context);
            _userManager = userManager;
        }

        // ─── 一覧取得 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 申請一覧を取得する。
        /// Admin は全件、Member は自分の申請のみ。
        /// </summary>
        public ApprovalRequestIndexViewModel GetList(ApprovalRequestIndexViewModel model, string currentUserId, bool isAdmin)
        {
            if (model.Cond == null) model.Cond = new ApprovalRequestCondViewModel();
            LocalUtil.SetPager(model.Cond, model);

            var condModel = BuildCondModel(model.Cond, model, isAdmin ? null : currentUserId);
            model.RowData = _repo.GetList(condModel);

            // 状態ドロップダウン用リスト（先頭に「全て」を追加）
            model.Cond.StatusList = Enum.GetValues<ApprovalStatus>()
                .Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = ((int)s).ToString(),
                    Text = s.GetDisplayName()
                }).ToList();

            return model;
        }

        // ─── 詳細取得 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 指定 ID の申請詳細を取得する。
        /// 申請者名は UserManager から解決する。
        /// </summary>
        public async Task<ApprovalRequestDetailViewModel?> GetDetailAsync(long id)
        {
            var entity = _repo.SelectById(id);
            if (entity == null) return null;

            var mapper = new MapperConfiguration(cfg =>
                cfg.CreateMap<ApprovalRequestEntity, ApprovalRequestDetailViewModel>(),
                NullLoggerFactory.Instance).CreateMapper();

            var vm = mapper.Map<ApprovalRequestDetailViewModel>(entity);

            // 申請者名を解決する
            var requester = await _userManager.FindByIdAsync(entity.RequesterUserId);
            vm.RequesterName = requester?.UserName ?? entity.RequesterUserId;

            return vm;
        }

        // ─── 作成 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 申請を新規作成する。
        /// SubmitRequest=true の場合は即座に申請中（Pending）にする。
        /// </summary>
        public void Create(ApprovalRequestFormViewModel vm, string currentUserId)
        {
            var entity = new ApprovalRequestEntity
            {
                Title = vm.Title,
                Content = vm.Content,
                RequesterUserId = currentUserId,
                Status = vm.SubmitRequest ? ApprovalStatus.Pending : ApprovalStatus.Draft,
            };

            if (vm.SubmitRequest)
                entity.RequestedDate = DateTime.Now;

            entity.SetForCreate();
            _repo.Insert(entity);
        }

        // ─── 編集用データ取得 ─────────────────────────────────────────────────

        /// <summary>
        /// 編集フォーム用のデータを取得する。Draft のみ編集可能。
        /// </summary>
        public ApprovalRequestFormViewModel? GetForEdit(long id, string currentUserId)
        {
            var entity = _repo.SelectById(id);
            if (entity == null) return null;

            // 編集できるのは申請者本人かつ Draft のみ
            if (entity.RequesterUserId != currentUserId || entity.Status != ApprovalStatus.Draft)
                return null;

            return new ApprovalRequestFormViewModel
            {
                Id = entity.Id,
                Title = entity.Title,
                Content = entity.Content,
            };
        }

        // ─── 更新 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 申請を更新する（Draft → Draft / Draft → Pending）。
        /// 申請者本人かつ Draft のみ更新可能。
        /// </summary>
        public bool Update(ApprovalRequestFormViewModel vm, string currentUserId)
        {
            var entity = _repo.SelectById(vm.Id);
            if (entity == null) return false;
            if (entity.RequesterUserId != currentUserId || entity.Status != ApprovalStatus.Draft)
                return false;

            entity.Title = vm.Title;
            entity.Content = vm.Content;

            if (vm.SubmitRequest)
            {
                entity.Status = ApprovalStatus.Pending;
                entity.RequestedDate = DateTime.Now;
            }

            entity.SetForUpdate();
            _repo.Update(entity);
            return true;
        }

        // ─── 承認・却下 ────────────────────────────────────────────────────────

        /// <summary>
        /// 申請を承認する（Pending → Approved）。
        /// Admin のみ実行可能。
        /// </summary>
        public bool Approve(long id, string? comment)
        {
            var entity = _repo.SelectById(id);
            if (entity == null || entity.Status != ApprovalStatus.Pending) return false;

            entity.Status = ApprovalStatus.Approved;
            entity.ApproverComment = comment;
            entity.ApprovedDate = DateTime.Now;
            entity.SetForUpdate();
            _repo.Update(entity);
            return true;
        }

        /// <summary>
        /// 申請を却下する（Pending → Rejected）。
        /// Admin のみ実行可能。
        /// </summary>
        public bool Reject(long id, string? comment)
        {
            var entity = _repo.SelectById(id);
            if (entity == null || entity.Status != ApprovalStatus.Pending) return false;

            entity.Status = ApprovalStatus.Rejected;
            entity.ApproverComment = comment;
            entity.ApprovedDate = DateTime.Now;
            entity.SetForUpdate();
            _repo.Update(entity);
            return true;
        }

        // ─── 削除確認データ取得 ───────────────────────────────────────────────

        /// <summary>
        /// 削除確認ページ用データを取得する。
        /// 申請者本人かつ Draft のみ削除可能。
        /// </summary>
        public ApprovalRequestDeleteViewModel? GetForDelete(long id, string currentUserId)
        {
            var entity = _repo.SelectById(id);
            if (entity == null) return null;
            if (entity.RequesterUserId != currentUserId || entity.Status != ApprovalStatus.Draft)
                return null;

            return new ApprovalRequestDeleteViewModel
            {
                Id = entity.Id,
                Title = entity.Title,
                Status = entity.Status,
                CreateDate = entity.CreateDate,
            };
        }

        // ─── 削除 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 申請を論理削除する（Draft のみ）。
        /// </summary>
        public bool Delete(long id, string currentUserId)
        {
            var entity = _repo.SelectById(id);
            if (entity == null) return false;
            if (entity.RequesterUserId != currentUserId || entity.Status != ApprovalStatus.Draft)
                return false;

            _repo.LogicalDelete(entity);
            return true;
        }

        // ─── 内部ユーティリティ ───────────────────────────────────────────────

        private ApprovalRequestCondModel BuildCondModel(ApprovalRequestCondViewModel vm, ApprovalRequestIndexViewModel indexModel, string? requesterUserId)
        {
            var cond = new ApprovalRequestCondModel
            {
                Title = vm.Title,
                RequesterUserId = requesterUserId,
                // SearchModelBase のプロパティから直接 Pager を組み立てる
                Pager = new Dev.CommonLibrary.Common.CommonListPagerModel(
                    indexModel.Page ?? 1,
                    indexModel.Sort ?? "Id",
                    indexModel.SortDir ?? "DESC",
                    indexModel.RecordNum ?? 10)
            };

            if (!string.IsNullOrEmpty(vm.Status) && int.TryParse(vm.Status, out int statusVal))
                cond.Status = (ApprovalStatus)statusVal;

            return cond;
        }
    }
}
```

- [ ] **Step 2: ビルドして確認する**

```bash
cd DevNext && dotnet build
```

期待結果: `Build succeeded.`

- [ ] **Step 3: コミットする**

```bash
git add DevNext/Service/ApprovalWorkflowService.cs
git commit -m "feat: ApprovalWorkflowService を追加"
```

---

## Task 8：Program.cs への DI 登録

**Files:**
- Modify: `DevNext/Program.cs`

- [ ] **Step 1: ApprovalWorkflowService を DI 登録する**

`DevNext/Program.cs` のサービス登録ブロック（`// ダッシュボード` の行の下）に追加：

```csharp
// 承認ワークフロー
builder.Services.AddScoped<Site.Service.ApprovalWorkflowService>();
```

- [ ] **Step 2: ビルドして確認する**

```bash
cd DevNext && dotnet build
```

期待結果: `Build succeeded.`

- [ ] **Step 3: コミットする**

```bash
git add DevNext/Program.cs
git commit -m "feat: ApprovalWorkflowService を DI 登録"
```

---

## Task 9：Controller の実装

**Files:**
- Create: `DevNext/Controllers/ApprovalRequestController.cs`

- [ ] **Step 1: ApprovalRequestController.cs を作成する**

```csharp
using Dev.CommonLibrary.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Site.Common;
using Site.Models;
using Site.Service;
using System.Security.Claims;

namespace Site.Controllers
{
    /// <summary>
    /// 承認ワークフロー Controller
    /// [Authorize] でログイン必須。ロール別制御は Service 層・View 層で行う。
    /// </summary>
    [Authorize]
    [ServiceFilter(typeof(AccessLogAttribute))]
    public class ApprovalRequestController : Controller
    {
        private readonly ApprovalWorkflowService _service;

        public ApprovalRequestController(ApprovalWorkflowService service)
        {
            _service = service;
        }

        // ─── 一覧 ──────────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Index(SearchModelBase? pageModel = null, bool returnList = false)
        {
            var model = LocalUtil.MapPageModelTo<ApprovalRequestIndexViewModel>(pageModel);

            if (model.PageRead != null || IsAjaxRequest() || returnList)
            {
                var sessionCond = TempData.Peek(SessionKey.ApprovalRequestCondViewModel);
                if (sessionCond != null)
                    model.Cond = System.Text.Json.JsonSerializer.Deserialize<ApprovalRequestCondViewModel>(sessionCond.ToString()!)!;

                if (returnList)
                {
                    var sessionPage = TempData.Peek(SessionKey.ApprovalRequestPageModel);
                    if (sessionPage != null)
                    {
                        var savedPage = System.Text.Json.JsonSerializer.Deserialize<SearchModelBase>(sessionPage.ToString()!)!;
                        model.Page      = savedPage.Page;
                        model.Sort      = savedPage.Sort;
                        model.SortDir   = savedPage.SortDir;
                        model.RecordNum = savedPage.RecordNum;
                    }
                }

                if (IsAjaxRequest()) model.PageRead = PageRead.Paging;
            }

            model = _service.GetList(model, GetCurrentUserId(), User.IsInRole("Admin"));

            TempData[SessionKey.ApprovalRequestCondViewModel] = System.Text.Json.JsonSerializer.Serialize(model.Cond);
            TempData[SessionKey.ApprovalRequestPageModel] = System.Text.Json.JsonSerializer.Serialize(
                new SearchModelBase { Page = model.Page, Sort = model.Sort, SortDir = model.SortDir, RecordNum = model.RecordNum });

            if (IsAjaxRequest()) return PartialView("_IndexPartial", model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(ApprovalRequestIndexViewModel model)
        {
            model = _service.GetList(model, GetCurrentUserId(), User.IsInRole("Admin"));
            TempData[SessionKey.ApprovalRequestCondViewModel] = System.Text.Json.JsonSerializer.Serialize(model.Cond);
            TempData[SessionKey.ApprovalRequestPageModel] = System.Text.Json.JsonSerializer.Serialize(
                new SearchModelBase { Page = model.Page, Sort = model.Sort, SortDir = model.SortDir, RecordNum = model.RecordNum });
            return View(model);
        }

        // ─── 新規作成 ──────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Create()
        {
            return View(new ApprovalRequestFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ApprovalRequestFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            _service.Create(model, GetCurrentUserId());
            TempData[SessionKey.Message] = model.SubmitRequest ? "申請しました。" : "下書きを保存しました。";
            return RedirectToAction(nameof(Index), new { returnList = true });
        }

        // ─── 編集 ──────────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Edit(long id)
        {
            var model = _service.GetForEdit(id, GetCurrentUserId());
            if (model == null) return RedirectToAction(nameof(Index));
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ApprovalRequestFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            bool updated = _service.Update(model, GetCurrentUserId());
            if (!updated) return RedirectToAction(nameof(Index));

            TempData[SessionKey.Message] = model.SubmitRequest ? "申請しました。" : "下書きを保存しました。";
            return RedirectToAction(nameof(Index), new { returnList = true });
        }

        // ─── 詳細・承認/却下 ────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Detail(long id)
        {
            var model = await _service.GetDetailAsync(id);
            if (model == null) return RedirectToAction(nameof(Index));
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Approve(long id, string? actionComment)
        {
            _service.Approve(id, actionComment);
            TempData[SessionKey.Message] = "申請を承認しました。";
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Reject(long id, string? actionComment)
        {
            _service.Reject(id, actionComment);
            TempData[SessionKey.Message] = "申請を却下しました。";
            return RedirectToAction(nameof(Detail), new { id });
        }

        // ─── 削除 ──────────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Delete(long id)
        {
            var model = _service.GetForDelete(id, GetCurrentUserId());
            if (model == null) return RedirectToAction(nameof(Index));
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(long id)
        {
            _service.Delete(id, GetCurrentUserId());
            TempData[SessionKey.Message] = "申請を削除しました。";
            return RedirectToAction(nameof(Index), new { returnList = true });
        }

        // ─── 内部ユーティリティ ───────────────────────────────────────────────

        private string GetCurrentUserId()
            => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private bool IsAjaxRequest()
            => Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }
}
```

- [ ] **Step 2: ビルドして確認する**

```bash
cd DevNext && dotnet build
```

期待結果: `Build succeeded.`

- [ ] **Step 3: コミットする**

```bash
git add DevNext/Controllers/ApprovalRequestController.cs
git commit -m "feat: ApprovalRequestController を追加"
```

---

## Task 10：View の実装（一覧）

**Files:**
- Create: `DevNext/Views/ApprovalRequest/Index.cshtml`
- Create: `DevNext/Views/ApprovalRequest/_IndexPartial.cshtml`

- [ ] **Step 1: Index.cshtml を作成する**

```html
@model ApprovalRequestIndexViewModel
@{
    ViewData["Title"] = "承認申請一覧";
}

<h2>@ViewData["Title"]</h2>

@if (TempData["Message"] != null)
{
    <div class="alert alert-success alert-dismissible fade show" role="alert">
        @TempData["Message"]
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}

<p>
    <a asp-action="Create" class="btn btn-success"><i class="fas fa-plus me-1"></i>新規申請</a>
</p>

<form id="SearchForm" asp-controller="ApprovalRequest" asp-action="Index" method="post">
    @Html.AntiForgeryToken()
    <input asp-for="Page" type="hidden" />
    <input asp-for="Sort" type="hidden" />
    <input asp-for="SortDir" type="hidden" />
    <input asp-for="RecordNum" type="hidden" />
    <input asp-for="PageRead" type="hidden" />

    <div class="card mb-3">
        <div class="card-header">検索条件</div>
        <div class="card-body">
            <div class="row mb-2">
                <div class="col-md-4">
                    <label asp-for="Cond.Title" class="form-label">タイトル</label>
                    <input asp-for="Cond.Title" class="form-control" placeholder="部分一致" />
                </div>
                <div class="col-md-3">
                    <label asp-for="Cond.Status" class="form-label">状態</label>
                    <select asp-for="Cond.Status" asp-items="Model.Cond!.StatusList" class="form-select">
                        <option value="">全て</option>
                    </select>
                </div>
            </div>
        </div>
        <div class="card-footer text-end">
            <button type="submit" class="btn btn-primary"><i class="fas fa-search me-1"></i>検索</button>
            <a asp-action="Index" class="btn btn-outline-secondary ms-2">クリア</a>
        </div>
    </div>
</form>

<div id="ListArea">
    @await Html.PartialAsync("_IndexPartial", Model)
</div>

@section Scripts {
    <script>
        // ページング・ソートは Ajax で取得してリストエリアを差し替える
        function doAjaxSearch(page, sort, sortdir, recordNum, pageRead) {
            var url = '@Url.Action("Index", "ApprovalRequest")';
            var params = '?Page=' + (page ?? '') + '&Sort=' + (sort ?? '') + '&SortDir=' + (sortdir ?? '') + '&RecordNum=' + (recordNum ?? '') + '&PageRead=' + (pageRead ?? '');
            $.ajax({
                url: url + params,
                type: 'GET',
                success: function (data) { $('#ListArea').html(data); }
            });
        }
    </script>
}
```

- [ ] **Step 2: _IndexPartial.cshtml を作成する**

```html
@model ApprovalRequestIndexViewModel

<p class="text-muted small">@Model.RowData?.Summary</p>

<div class="table-responsive">
    <table class="table table-bordered table-hover table-sm align-middle">
        <thead class="table-dark">
            <tr>
                <th>ID</th>
                <th>タイトル</th>
                <th>状態</th>
                @if (User.IsInRole("Admin"))
                {
                    <th>申請者</th>
                }
                <th>申請日時</th>
                <th>操作</th>
            </tr>
        </thead>
        <tbody>
            @if (Model.RowData?.Rows == null || !Model.RowData.Rows.Any())
            {
                <tr>
                    <td colspan="6" class="text-center text-muted">データがありません</td>
                </tr>
            }
            else
            {
                @foreach (var row in Model.RowData.Rows)
                {
                    <tr>
                        <td>@row.Id</td>
                        <td>@row.Title</td>
                        <td>
                            @{
                                var badgeClass = row.Status switch
                                {
                                    Site.Common.ApprovalStatus.Draft    => "bg-secondary",
                                    Site.Common.ApprovalStatus.Pending  => "bg-warning text-dark",
                                    Site.Common.ApprovalStatus.Approved => "bg-success",
                                    Site.Common.ApprovalStatus.Rejected => "bg-danger",
                                    _ => "bg-secondary"
                                };
                            }
                            <span class="badge @badgeClass">@row.Status.GetDisplayName()</span>
                        </td>
                        @if (User.IsInRole("Admin"))
                        {
                            <td>@row.RequesterUserId</td>
                        }
                        <td>@(row.RequestedDate?.ToString("yyyy/MM/dd HH:mm") ?? "-")</td>
                        <td>
                            <a asp-action="Detail" asp-route-id="@row.Id" class="btn btn-sm btn-outline-primary">詳細</a>
                            @if (row.Status == Site.Common.ApprovalStatus.Draft)
                            {
                                <a asp-action="Edit" asp-route-id="@row.Id" class="btn btn-sm btn-outline-secondary ms-1">編集</a>
                                <a asp-action="Delete" asp-route-id="@row.Id" class="btn btn-sm btn-outline-danger ms-1">削除</a>
                            }
                        </td>
                    </tr>
                }
            }
        </tbody>
    </table>
</div>
```

- [ ] **Step 3: ビルドして確認する**

```bash
cd DevNext && dotnet build
```

期待結果: `Build succeeded.`

- [ ] **Step 4: コミットする**

```bash
git add DevNext/Views/ApprovalRequest/Index.cshtml DevNext/Views/ApprovalRequest/_IndexPartial.cshtml
git commit -m "feat: 承認申請一覧ビュー を追加"
```

---

## Task 11：View の実装（作成・編集）

**Files:**
- Create: `DevNext/Views/ApprovalRequest/Create.cshtml`
- Create: `DevNext/Views/ApprovalRequest/Edit.cshtml`

- [ ] **Step 1: Create.cshtml を作成する**

```html
@model ApprovalRequestFormViewModel
@{
    ViewData["Title"] = "申請の新規作成";
}

<h2>@ViewData["Title"]</h2>

<div class="row">
    <div class="col-md-8">
        <form asp-action="Create" method="post">
            @Html.AntiForgeryToken()
            <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

            <div class="mb-3">
                <label asp-for="Title" class="form-label"></label>
                <input asp-for="Title" class="form-control" />
                <span asp-validation-for="Title" class="text-danger"></span>
            </div>

            <div class="mb-3">
                <label asp-for="Content" class="form-label"></label>
                <textarea asp-for="Content" class="form-control" rows="6"></textarea>
                <span asp-validation-for="Content" class="text-danger"></span>
            </div>

            <div class="d-flex gap-2">
                @* 下書き保存：SubmitRequest=false のまま送信 *@
                <button type="submit" name="SubmitRequest" value="false" class="btn btn-outline-secondary">
                    <i class="fas fa-save me-1"></i>下書き保存
                </button>
                @* 申請：SubmitRequest=true で送信 *@
                <button type="submit" name="SubmitRequest" value="true" class="btn btn-primary">
                    <i class="fas fa-paper-plane me-1"></i>申請する
                </button>
                <a asp-action="Index" asp-route-returnList="true" class="btn btn-outline-secondary ms-auto">キャンセル</a>
            </div>
        </form>
    </div>
</div>

@section Scripts {
    @{ await Html.RenderPartialAsync("_ValidationScriptsPartial"); }
}
```

- [ ] **Step 2: Edit.cshtml を作成する**

```html
@model ApprovalRequestFormViewModel
@{
    ViewData["Title"] = "申請の編集";
}

<h2>@ViewData["Title"]</h2>

<div class="row">
    <div class="col-md-8">
        <form asp-action="Edit" method="post">
            @Html.AntiForgeryToken()
            <input asp-for="Id" type="hidden" />
            <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

            <div class="mb-3">
                <label asp-for="Title" class="form-label"></label>
                <input asp-for="Title" class="form-control" />
                <span asp-validation-for="Title" class="text-danger"></span>
            </div>

            <div class="mb-3">
                <label asp-for="Content" class="form-label"></label>
                <textarea asp-for="Content" class="form-control" rows="6"></textarea>
                <span asp-validation-for="Content" class="text-danger"></span>
            </div>

            <div class="d-flex gap-2">
                <button type="submit" name="SubmitRequest" value="false" class="btn btn-outline-secondary">
                    <i class="fas fa-save me-1"></i>下書き保存
                </button>
                <button type="submit" name="SubmitRequest" value="true" class="btn btn-primary">
                    <i class="fas fa-paper-plane me-1"></i>申請する
                </button>
                <a asp-action="Index" asp-route-returnList="true" class="btn btn-outline-secondary ms-auto">キャンセル</a>
            </div>
        </form>
    </div>
</div>

@section Scripts {
    @{ await Html.RenderPartialAsync("_ValidationScriptsPartial"); }
}
```

- [ ] **Step 3: ビルドして確認する**

```bash
cd DevNext && dotnet build
```

期待結果: `Build succeeded.`

- [ ] **Step 4: コミットする**

```bash
git add DevNext/Views/ApprovalRequest/Create.cshtml DevNext/Views/ApprovalRequest/Edit.cshtml
git commit -m "feat: 承認申請 作成・編集ビュー を追加"
```

---

## Task 12：View の実装（詳細・削除）

**Files:**
- Create: `DevNext/Views/ApprovalRequest/Detail.cshtml`
- Create: `DevNext/Views/ApprovalRequest/Delete.cshtml`

- [ ] **Step 1: Detail.cshtml を作成する**

```html
@model ApprovalRequestDetailViewModel
@{
    ViewData["Title"] = "申請詳細";
    var badgeClass = Model.Status switch
    {
        Site.Common.ApprovalStatus.Draft    => "bg-secondary",
        Site.Common.ApprovalStatus.Pending  => "bg-warning text-dark",
        Site.Common.ApprovalStatus.Approved => "bg-success",
        Site.Common.ApprovalStatus.Rejected => "bg-danger",
        _ => "bg-secondary"
    };
}

<h2>@ViewData["Title"]</h2>

@if (TempData["Message"] != null)
{
    <div class="alert alert-success alert-dismissible fade show" role="alert">
        @TempData["Message"]
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}

<div class="row">
    <div class="col-md-8">
        <div class="card mb-4">
            <div class="card-body">
                <h5 class="card-title">@Model.Title</h5>
                <p class="text-muted small mb-3">
                    申請者: @Model.RequesterName
                    作成日: @Model.CreateDate.ToString("yyyy/MM/dd HH:mm")
                    申請日時: @(Model.RequestedDate?.ToString("yyyy/MM/dd HH:mm") ?? "-")
                </p>
                <span class="badge @badgeClass fs-6 mb-3">@Model.Status.GetDisplayName()</span>
                <p class="card-text" style="white-space: pre-wrap;">@Model.Content</p>

                @if (!string.IsNullOrEmpty(Model.ApproverComment))
                {
                    <hr />
                    <h6>承認者コメント</h6>
                    <p class="card-text">@Model.ApproverComment</p>
                    <p class="text-muted small">承認・却下日時: @(Model.ApprovedDate?.ToString("yyyy/MM/dd HH:mm") ?? "-")</p>
                }
            </div>
        </div>

        @* 承認/却下フォーム（Admin 限定、申請中のみ表示） *@
        @if (User.IsInRole("Admin") && Model.Status == Site.Common.ApprovalStatus.Pending)
        {
            <div class="card border-warning mb-4">
                <div class="card-header bg-warning text-dark">承認・却下操作</div>
                <div class="card-body">
                    <div class="mb-3">
                        <label class="form-label">コメント（任意）</label>
                        <textarea id="actionComment" name="actionComment" class="form-control" rows="3"
                                  placeholder="承認・却下の理由や備考を入力してください"></textarea>
                    </div>
                    <div class="d-flex gap-2">
                        <form asp-action="Approve" method="post" class="d-inline">
                            @Html.AntiForgeryToken()
                            <input type="hidden" name="id" value="@Model.Id" />
                            <input type="hidden" name="actionComment" id="approveComment" />
                            <button type="submit" class="btn btn-success"
                                    onclick="document.getElementById('approveComment').value = document.getElementById('actionComment').value">
                                <i class="fas fa-check me-1"></i>承認する
                            </button>
                        </form>
                        <form asp-action="Reject" method="post" class="d-inline">
                            @Html.AntiForgeryToken()
                            <input type="hidden" name="id" value="@Model.Id" />
                            <input type="hidden" name="actionComment" id="rejectComment" />
                            <button type="submit" class="btn btn-danger"
                                    onclick="document.getElementById('rejectComment').value = document.getElementById('actionComment').value">
                                <i class="fas fa-times me-1"></i>却下する
                            </button>
                        </form>
                    </div>
                </div>
            </div>
        }

        <a asp-action="Index" asp-route-returnList="true" class="btn btn-outline-secondary">
            <i class="fas fa-arrow-left me-1"></i>一覧へ戻る
        </a>
    </div>
</div>
```

- [ ] **Step 2: Delete.cshtml を作成する**

```html
@model ApprovalRequestDeleteViewModel
@{
    ViewData["Title"] = "申請の削除";
}

<h2>@ViewData["Title"]</h2>

<div class="alert alert-danger">
    以下の申請を削除します。この操作は取り消せません。
</div>

<div class="row">
    <div class="col-md-6">
        <dl class="row">
            <dt class="col-sm-4">ID</dt>
            <dd class="col-sm-8">@Model.Id</dd>
            <dt class="col-sm-4">タイトル</dt>
            <dd class="col-sm-8">@Model.Title</dd>
            <dt class="col-sm-4">作成日</dt>
            <dd class="col-sm-8">@Model.CreateDate.ToString("yyyy/MM/dd HH:mm")</dd>
        </dl>

        <form asp-action="DeleteConfirmed" method="post">
            @Html.AntiForgeryToken()
            <input type="hidden" name="id" value="@Model.Id" />
            <button type="submit" class="btn btn-danger">
                <i class="fas fa-trash me-1"></i>削除する
            </button>
            <a asp-action="Index" asp-route-returnList="true" class="btn btn-outline-secondary ms-2">キャンセル</a>
        </form>
    </div>
</div>
```

- [ ] **Step 3: ビルドして確認する**

```bash
cd DevNext && dotnet build
```

期待結果: `Build succeeded.`

- [ ] **Step 4: コミットする**

```bash
git add DevNext/Views/ApprovalRequest/Detail.cshtml DevNext/Views/ApprovalRequest/Delete.cshtml
git commit -m "feat: 承認申請 詳細・削除ビュー を追加"
```

---

## Task 13：ナビバーとホーム画面への追加

**Files:**
- Modify: `DevNext/Views/Shared/_Layout.cshtml`
- Modify: `DevNext/Views/Home/Index.cshtml`

- [ ] **Step 1: _Layout.cshtml にナビリンクを追加する**

`DevNext/Views/Shared/_Layout.cshtml` のナビバーの `</ul>` 直前（末尾のナビアイテムの後）に追加：

```html
                        <li class="nav-item">
                            <a class="nav-link text-white" asp-controller="ApprovalRequest" asp-action="Index">承認申請</a>
                        </li>
```

- [ ] **Step 2: Home/Index.cshtml にカードを追加する**

`DevNext/Views/Home/Index.cshtml` の最後の `</div>` （`@if (User.IsInRole("Admin"))` ブロックの後）に追加：

```html
    <div class="col-md-6 mb-3">
        <div class="card h-100">
            <div class="card-body">
                <h5 class="card-title"><i class="fas fa-tasks me-2"></i>承認ワークフロー</h5>
                <p class="card-text">申請の作成・承認・却下ができます。Admin は全申請を管理できます。</p>
                <a asp-controller="ApprovalRequest" asp-action="Index" class="btn btn-primary">申請一覧へ</a>
                <a asp-controller="ApprovalRequest" asp-action="Create" class="btn btn-outline-success ms-2">新規申請</a>
            </div>
        </div>
    </div>
```

- [ ] **Step 3: ビルドして確認する**

```bash
cd DevNext && dotnet build
```

期待結果: `Build succeeded.`

- [ ] **Step 4: コミットする**

```bash
git add DevNext/Views/Shared/_Layout.cshtml DevNext/Views/Home/Index.cshtml
git commit -m "feat: ナビバーとホーム画面に承認申請リンクを追加"
```

---

## Task 14：動作確認

- [ ] **Step 1: アプリを起動する**

```bash
cd DevNext && dotnet run
```

- [ ] **Step 2: Member アカウントで動作確認する**

1. `member1@sample.jp` / `Member1!` でログイン
2. ホーム画面の「承認ワークフロー」カードを確認
3. 「新規申請」で申請を作成（下書き保存）
4. 一覧で Draft ステータスの申請を確認
5. 「編集」で内容を修正し「申請する」でステータスが Pending になることを確認
6. 詳細ページで申請内容を確認
7. Draft の申請を削除できることを確認
8. Pending の申請は編集・削除ボタンが表示されないことを確認

- [ ] **Step 3: Admin アカウントで動作確認する**

1. `admin1@sample.jp` / `Admin1!` でログイン
2. 一覧で全申請が表示されること（申請者カラムが表示されること）を確認
3. Pending の申請の詳細を開き、承認/却下フォームが表示されることを確認
4. 「承認する」ボタンでステータスが Approved になることを確認
5. 別の Pending 申請を「却下する」でステータスが Rejected になることを確認
6. Member 画面では承認/却下フォームが表示されないことを確認

- [ ] **Step 4: 最終コミット**

```bash
git add -A
git commit -m "feat: Phase 1 承認ワークフロー 完成"
```

---

## 補足：共通ライブラリのメソッド参照

| メソッド / クラス | 場所 | 説明 |
|---|---|---|
| `SiteEntityBase` | `CommonLibrary/Entity/SiteEntityBase.cs` | Id, DelFlag, CreateDate 等の共通フィールド |
| `SetForCreate()` | `CommonLibrary/Entity/EntityBase.cs` | 新規作成時の共通フィールド初期化 |
| `SetForUpdate()` | `CommonLibrary/Entity/EntityBase.cs` | 更新時の UpdateDate 更新 |
| `RepositoryBase<T,TH,TC>` | `CommonLibrary/Repository/RepositoryBase.cs` | Insert, Update, LogicalDelete, SelectById |
| `LocalUtil.SetPager()` | `DevNext/Common/localutil.cs` | ViewModel → Pager モデルへのページ情報コピー |
| `LocalUtil.SetTakeSkip()` | `DevNext/Common/localutil.cs` | IQueryable にページング（Take/Skip）を適用 |
| `LocalUtil.MapPageModelTo<T>()` | `DevNext/Common/localutil.cs` | SearchModelBase → T へプロパティコピー |
| `Util.CreateSummary()` | `CommonLibrary/Common/Util.cs` | 「N件中 X - Y を表示」文字列生成 |
| `GetDisplayName()` | `CommonLibrary/Attributes` 拡張メソッド | Enum の `[Display(Name="")]` 値を取得 |
