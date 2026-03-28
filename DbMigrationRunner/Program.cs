using Site.Common;
using Site.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DbMigrationRunner
{
    /// <summary>
    /// DB作成・スキーマ更新ツール。
    /// - DBが存在しない場合: EnsureCreatedAsync でテーブルをすべて作成する。
    /// - DBが既に存在する場合: 不足テーブルのみ追加する（既存データは削除しない）。
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== DevNext DB マイグレーションランナー ===");

            try
            {
                // 設定ファイル読み込み
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var connectionString = config.GetConnectionString("SiteConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine("エラー: appsettings.json に SiteConnection が設定されていません。");
                    Environment.Exit(1);
                }

                // サービスプロバイダー構築
                var services = new ServiceCollection();
                services.AddLogging();
                services.AddDbContext<DBContext>(options => options.UseSqlServer(connectionString));
                services.AddIdentityCore<ApplicationUser>()
                    .AddRoles<ApplicationRole>()
                    .AddEntityFrameworkStores<DBContext>();

                await using var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var sp = scope.ServiceProvider;

                var context = sp.GetRequiredService<DBContext>();

                // DBが存在しない場合は EnsureCreatedAsync で全テーブルを作成する。
                // DBが既に存在する場合は EnsureCreatedAsync は何もしないため、
                // 不足テーブルのみ個別に追加する（既存データは保持される）。
                Console.WriteLine("データベースを確認しています...");
                bool created = await context.Database.EnsureCreatedAsync();
                if (created)
                {
                    Console.WriteLine("データベースを新規作成しました。");
                }
                else
                {
                    Console.WriteLine("既存データベースを検出しました。不足テーブルを追加します...");
                    await ApplyMissingTablesAsync(context);
                    Console.WriteLine("スキーマの更新が完了しました。");
                }

                // Seedデータ投入
                Console.WriteLine("Seedデータを投入しています...");
                var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
                var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
                await SeedAsync(context, roleManager, userManager);
                Console.WriteLine("Seedデータの投入が完了しました。");

                Console.WriteLine("\n✓ 処理が完了しました。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nエラーが発生しました: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"詳細: {ex.InnerException.Message}");
                }
                Environment.Exit(1);
            }

            Console.WriteLine("\n任意のキーを押して終了してください...");
            Console.ReadKey();
        }

        /// <summary>
        /// 不足しているテーブルを追加する。
        /// 新しいエンティティを追加した場合は、このメソッドに対応するブロックを追記すること。
        /// EF Core が生成する DDL と型を合わせるため、各カラムの型は EF Core の規約に従う。
        ///   - string（[Required][MaxLength(N)]）→ nvarchar(N) NOT NULL
        ///   - string?（[MaxLength(N)]）         → nvarchar(N) NULL
        ///   - string?（MaxLength指定なし）       → nvarchar(max) NULL
        ///   - long                              → bigint NOT NULL
        ///   - bool                              → bit NOT NULL
        ///   - DateTime                          → datetime2(7) NOT NULL
        ///   - DateTime?                         → datetime2(7) NULL
        ///   - enum (int)                        → int NOT NULL
        /// </summary>
        static async Task ApplyMissingTablesAsync(DBContext context)
        {
            // ─── FileEntity ──────────────────────────────────────────────
            Console.WriteLine("  テーブル [FileEntity] を確認しています...");
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FileEntity')
                BEGIN
                    CREATE TABLE [FileEntity] (
                        [Id]                        bigint          NOT NULL IDENTITY(1,1),
                        [OriginalFileName]          nvarchar(260)   NOT NULL,
                        [SavedFileName]             nvarchar(260)   NOT NULL,
                        [FileSize]                  bigint          NOT NULL,
                        [ContentType]               nvarchar(100)   NOT NULL,
                        [Description]               nvarchar(500)   NULL,
                        [DelFlag]                   bit             NOT NULL,
                        [UpdateApplicationUserId]   nvarchar(max)   NULL,
                        [CreateApplicationUserId]   nvarchar(max)   NULL,
                        [UpdateDate]                datetime2(7)    NOT NULL,
                        [CreateDate]                datetime2(7)    NOT NULL,
                        CONSTRAINT [PK_FileEntity] PRIMARY KEY ([Id])
                    )
                END");

            // ─── WizardEntity ─────────────────────────────────────────────
            Console.WriteLine("  テーブル [WizardEntity] を確認しています...");
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WizardEntity')
                BEGIN
                    CREATE TABLE [WizardEntity] (
                        [Id]                        bigint          NOT NULL IDENTITY(1,1),
                        [Name]                      nvarchar(100)   NOT NULL,
                        [Email]                     nvarchar(256)   NOT NULL,
                        [Phone]                     nvarchar(20)    NULL,
                        [Subject]                   nvarchar(200)   NOT NULL,
                        [Content]                   nvarchar(2000)  NOT NULL,
                        [Category]                  int             NOT NULL,
                        [DesiredDate]               datetime2(7)    NULL,
                        [DelFlag]                   bit             NOT NULL,
                        [UpdateApplicationUserId]   nvarchar(max)   NULL,
                        [CreateApplicationUserId]   nvarchar(max)   NULL,
                        [UpdateDate]                datetime2(7)    NOT NULL,
                        [CreateDate]                datetime2(7)    NOT NULL,
                        CONSTRAINT [PK_WizardEntity] PRIMARY KEY ([Id])
                    )
                END");

            // ─── MailLog ──────────────────────────────────────────────────
            Console.WriteLine("  テーブル [MailLog] を確認しています...");
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MailLog')
                BEGIN
                    CREATE TABLE [MailLog] (
                        [Id]                        bigint          NOT NULL IDENTITY(1,1),
                        [SenderName]                nvarchar(100)   NOT NULL,
                        [SenderEmail]               nvarchar(256)   NOT NULL,
                        [Subject]                   nvarchar(200)   NOT NULL,
                        [Body]                      nvarchar(2000)  NOT NULL,
                        [IsSuccess]                 bit             NOT NULL,
                        [ErrorMessage]              nvarchar(1000)  NULL,
                        [DelFlag]                   bit             NOT NULL,
                        [UpdateApplicationUserId]   nvarchar(max)   NULL,
                        [CreateApplicationUserId]   nvarchar(max)   NULL,
                        [UpdateDate]                datetime2(7)    NOT NULL,
                        [CreateDate]                datetime2(7)    NOT NULL,
                        CONSTRAINT [PK_MailLog] PRIMARY KEY ([Id])
                    )
                END");

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

            // ─── Notification ─────────────────────────────────────────────
            Console.WriteLine("  テーブル [Notification] を確認しています...");
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Notification')
                BEGIN
                    CREATE TABLE [Notification] (
                        [Id]                        bigint          NOT NULL IDENTITY(1,1),
                        [RecipientUserId]           nvarchar(450)   NOT NULL,
                        [Message]                   nvarchar(500)   NOT NULL,
                        [IsRead]                    bit             NOT NULL,
                        [RelatedUrl]                nvarchar(500)   NULL,
                        [DelFlag]                   bit             NOT NULL,
                        [UpdateApplicationUserId]   nvarchar(max)   NULL,
                        [CreateApplicationUserId]   nvarchar(max)   NULL,
                        [UpdateDate]                datetime2(7)    NOT NULL,
                        [CreateDate]                datetime2(7)    NOT NULL,
                        CONSTRAINT [PK_Notification] PRIMARY KEY ([Id])
                    )
                END");

            // ─── ScheduleEvent ────────────────────────────────────────────────
            Console.WriteLine("  テーブル [ScheduleEvent] を確認しています...");
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ScheduleEvent')
                BEGIN
                    CREATE TABLE [ScheduleEvent] (
                        [Id]                        bigint          NOT NULL IDENTITY(1,1),
                        [Title]                     nvarchar(200)   NOT NULL,
                        [Description]               nvarchar(2000)  NULL,
                        [StartDate]                 datetime2(7)    NOT NULL,
                        [EndDate]                   datetime2(7)    NOT NULL,
                        [IsAllDay]                  bit             NOT NULL,
                        [IsShared]                  bit             NOT NULL,
                        [OwnerId]                   nvarchar(450)   NOT NULL,
                        [RecurrenceType]            int             NOT NULL,
                        [RecurrenceInterval]        int             NOT NULL,
                        [RecurrenceEndDate]         datetime2(7)    NULL,
                        [RecurrenceDaysOfWeek]      nvarchar(20)    NULL,
                        [DelFlag]                   bit             NOT NULL,
                        [UpdateApplicationUserId]   nvarchar(max)   NULL,
                        [CreateApplicationUserId]   nvarchar(max)   NULL,
                        [UpdateDate]                datetime2(7)    NOT NULL,
                        [CreateDate]                datetime2(7)    NOT NULL,
                        CONSTRAINT [PK_ScheduleEvent] PRIMARY KEY ([Id])
                    )
                END");

            // ─── ScheduleEventHistory ─────────────────────────────────────────
            Console.WriteLine("  テーブル [ScheduleEventHistory] を確認しています...");
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ScheduleEventHistory')
                BEGIN
                    CREATE TABLE [ScheduleEventHistory] (
                        [HistoryId]                 bigint          NOT NULL IDENTITY(1,1),
                        [Id]                        bigint          NOT NULL,
                        [Title]                     nvarchar(200)   NOT NULL,
                        [Description]               nvarchar(2000)  NULL,
                        [StartDate]                 datetime2(7)    NOT NULL,
                        [EndDate]                   datetime2(7)    NOT NULL,
                        [IsAllDay]                  bit             NOT NULL,
                        [IsShared]                  bit             NOT NULL,
                        [OwnerId]                   nvarchar(450)   NOT NULL,
                        [RecurrenceType]            int             NOT NULL,
                        [RecurrenceInterval]        int             NOT NULL,
                        [RecurrenceEndDate]         datetime2(7)    NULL,
                        [RecurrenceDaysOfWeek]      nvarchar(20)    NULL,
                        [DelFlag]                   bit             NOT NULL,
                        [UpdateApplicationUserId]   nvarchar(max)   NULL,
                        [CreateApplicationUserId]   nvarchar(max)   NULL,
                        [UpdateDate]                datetime2(7)    NOT NULL,
                        [CreateDate]                datetime2(7)    NOT NULL,
                        CONSTRAINT [PK_ScheduleEventHistory] PRIMARY KEY ([HistoryId])
                    )
                END");

            // ─── ScheduleEventParticipant ─────────────────────────────────────
            Console.WriteLine("  テーブル [ScheduleEventParticipant] を確認しています...");
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ScheduleEventParticipant')
                BEGIN
                    CREATE TABLE [ScheduleEventParticipant] (
                        [Id]                        bigint          NOT NULL IDENTITY(1,1),
                        [EventId]                   bigint          NOT NULL,
                        [UserId]                    nvarchar(450)   NOT NULL,
                        [Status]                    int             NOT NULL,
                        [DelFlag]                   bit             NOT NULL,
                        [UpdateApplicationUserId]   nvarchar(max)   NULL,
                        [CreateApplicationUserId]   nvarchar(max)   NULL,
                        [UpdateDate]                datetime2(7)    NOT NULL,
                        [CreateDate]                datetime2(7)    NOT NULL,
                        CONSTRAINT [PK_ScheduleEventParticipant] PRIMARY KEY ([Id])
                    )
                END");
        }

        static async Task SeedAsync(DBContext context, RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            // ロール作成
            if (!await roleManager.RoleExistsAsync(ApplicationRoleType.Admin.ToString()))
                await roleManager.CreateAsync(new ApplicationRole { Id = "1", Name = ApplicationRoleType.Admin.ToString() });

            if (!await roleManager.RoleExistsAsync(ApplicationRoleType.Member.ToString()))
                await roleManager.CreateAsync(new ApplicationRole { Id = "2", Name = ApplicationRoleType.Member.ToString() });

            // 初期ユーザー作成
            if (!context.Users.Any())
            {
                var adminUser = new ApplicationUser
                {
                    Id = "1",
                    Email = "admin1@sample.jp",
                    UserName = "admin1@sample.jp",
                    ApplicationRoleName = ApplicationRoleType.Admin.ToString()
                };
                await userManager.CreateAsync(adminUser, "Admin1!");
                await userManager.AddToRoleAsync(adminUser, ApplicationRoleType.Admin.ToString());

                var memberUser = new ApplicationUser
                {
                    Id = "2",
                    Email = "member1@sample.jp",
                    UserName = "member1@sample.jp",
                    ApplicationRoleName = ApplicationRoleType.Member.ToString()
                };
                await userManager.CreateAsync(memberUser, "Member1!");
                await userManager.AddToRoleAsync(memberUser, ApplicationRoleType.Member.ToString());
            }
        }
    }
}
