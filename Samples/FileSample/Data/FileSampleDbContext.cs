using Dev.CommonLibrary.Entity;
using FileSample.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FileSample.Data
{
    /// <summary>
    /// FileSample 専用 DBContext
    /// DevNextDB を共有し、Identity テーブルは同じテーブル名を使用する
    /// </summary>
    public class FileSampleDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public FileSampleDbContext(DbContextOptions<FileSampleDbContext> options) : base(options) { }

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
        public DbSet<FileEntity> FileEntity { get; set; }
    }
}
