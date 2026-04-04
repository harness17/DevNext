using Dev.CommonLibrary.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PdfSample.Entity;

namespace PdfSample.Data;

public class PdfSampleDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public PdfSampleDbContext(DbContextOptions<PdfSampleDbContext> options) : base(options) { }

    public DbSet<UserPreviousPassword> UserPreviousPasswords { get; set; }
    public DbSet<InvoiceEntity> InvoiceEntity { get; set; }
    public DbSet<InvoiceEntityHistory> InvoiceEntityHistory { get; set; }
    public DbSet<InvoiceItemEntity> InvoiceItemEntity { get; set; }
    public DbSet<InvoiceItemEntityHistory> InvoiceItemEntityHistory { get; set; }

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

        modelBuilder.Entity<InvoiceEntity>()
            .HasMany(e => e.Items)
            .WithOne()
            .HasForeignKey(i => i.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InvoiceItemEntity>()
            .Property(x => x.UnitPrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<InvoiceItemEntityHistory>()
            .Property(x => x.UnitPrice)
            .HasColumnType("decimal(18,2)");

        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
        {
            property.SetColumnType("datetime2");
        }
    }
}
