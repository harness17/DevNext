using DatabaseSample.Entity;
using Dev.CommonLibrary.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DatabaseSample.Data
{
    /// <summary>
    /// DatabaseSample 専用 DBContext
    /// DevNextDB を共有し、Identity テーブルは同じテーブル名を使用する
    /// </summary>
    public class DatabaseSampleDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public DatabaseSampleDbContext(DbContextOptions<DatabaseSampleDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // DevNextコアと同じテーブル名を使用してDBを共有する
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

        public DbSet<UserPreviousPassword> UserPreviousPasswords { get; set; }
        public DbSet<SampleEntity> SampleEntity { get; set; }
        public DbSet<SampleEntityHistory> SampleEntityHistory { get; set; }
        public DbSet<SampleEntityChild> SampleEntityChild { get; set; }
        public DbSet<SampleEntityChildHistory> SampleEntityChildHistory { get; set; }
    }
}
