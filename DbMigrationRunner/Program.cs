using Site.Common;
using Site.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DbMigrationRunner
{
    /// <summary>
    /// EF Core マイグレーション実行ツール
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

                // DB作成
                Console.WriteLine("データベースを更新しています...");
                await context.Database.EnsureCreatedAsync();
                Console.WriteLine("データベースの更新が完了しました。");

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
