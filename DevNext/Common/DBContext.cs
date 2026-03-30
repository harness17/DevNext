using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Dev.CommonLibrary.Entity;
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

        // 承認ワークフロー
        public DbSet<ApprovalRequestEntity> ApprovalRequest { get; set; }

        // 通知
        public DbSet<NotificationEntity> Notification { get; set; }

        // スケジュール
        public DbSet<ScheduleEventEntity> ScheduleEvent { get; set; }
        public DbSet<ScheduleEventEntityHistory> ScheduleEventHistory { get; set; }
        public DbSet<ScheduleEventParticipantEntity> ScheduleEventParticipant { get; set; }

        #endregion
    }
}
