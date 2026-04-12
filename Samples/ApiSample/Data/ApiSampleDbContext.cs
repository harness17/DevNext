using ApiSample.Entity;
using Dev.CommonLibrary.Entity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ApiSample.Data;

/// <summary>
/// ApiSample 専用 DBContext。
/// コアの DBContext（DevNextDB）とは完全に分離する。
/// Identity も含め、このプロジェクト単独で動作する。
/// </summary>
public class ApiSampleDbContext(DbContextOptions<ApiSampleDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    /// <summary>商品テーブル</summary>
    public DbSet<ApiItem> ApiItems { get; set; }
}
