using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Site.Entity;

namespace Site.Common
{
    /// <summary>
    /// DBコンテクスト
    /// </summary>
    public class DBContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public DBContext(DbContextOptions<DBContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // テーブル名のカスタマイズ
            modelBuilder.Entity<ApplicationUser>().ToTable("ApplicationUser");
            modelBuilder.Entity<ApplicationRole>().ToTable("ApplicationRole");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("ApplicationUserRole");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("ApplicationUserClaim");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("ApplicationUserLogin");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("ApplicationRoleClaim");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("ApplicationUserToken");

            // DateTime型の設定
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetProperties())
                .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
            {
                property.SetColumnType("datetime2");
            }
        }

        #region DbSet

        public DbSet<UserPreviousPassword> UserPreviousPasswords { get; set; }
        public DbSet<SampleEntity> SampleEntity { get; set; }
        public DbSet<SampleEntityHistory> SampleEntityHistory { get; set; }
        public DbSet<SampleEntityChild> SampleEntityChild { get; set; }
        public DbSet<SampleEntityChildHistory> SampleEntityChildHistory { get; set; }

        // ファイル管理サンプル
        public DbSet<FileEntity> FileEntity { get; set; }

        // 多段階フォームサンプル
        public DbSet<WizardEntity> WizardEntity { get; set; }

        // メール送信ログ
        public DbSet<MailLogEntity> MailLog { get; set; }

        // 承認ワークフロー
        public DbSet<ApprovalRequestEntity> ApprovalRequest { get; set; }

        #endregion
    }
}
