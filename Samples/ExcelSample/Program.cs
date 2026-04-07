using ExcelSample.Data;
using ExcelSample.Repository;
using ExcelSample.Service;
using Dev.CommonLibrary.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

builder.Services.AddDbContext<ExcelSampleDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SiteConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ExcelSampleDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/LogOff";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(1440);
    options.Cookie.HttpOnly = true;
    // ポイント: SecurePolicy.Always で HTTPS 接続時のみ Cookie を送信する（セキュリティベストプラクティス）
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(1440);
    options.Cookie.HttpOnly = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));

builder.Services.AddScoped<ExcelSampleService>();
builder.Services.AddScoped<ExcelItemRepository>();
builder.Services.AddScoped<Dev.CommonLibrary.Attributes.AccessLogAttribute>();

var app = builder.Build();

var accessor = app.Services.GetRequiredService<IHttpContextAccessor>();
Dev.CommonLibrary.Entity.EntityBase.HttpContextAccessor = accessor;

var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
Dev.CommonLibrary.Common.Logger.GetLogger().SetLogger(loggerFactory.CreateLogger("App"));

// DB作成・初期データ投入
await using (var scope = app.Services.CreateAsyncScope())
{
    var sp = scope.ServiceProvider;
    var context = sp.GetRequiredService<ExcelSampleDbContext>();
    await context.Database.EnsureCreatedAsync();
    await SeedAsync(sp);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/RootError/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute("default", "{controller=ExcelSample}/{action=Index}/{id?}");

app.Run();

static async Task SeedAsync(IServiceProvider sp)
{
    var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
    var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new ApplicationRole { Id = "1", Name = "Admin" });
    if (!await roleManager.RoleExistsAsync("Member"))
        await roleManager.CreateAsync(new ApplicationRole { Id = "2", Name = "Member" });

    if (await userManager.FindByNameAsync("admin1@sample.jp") == null)
    {
        var admin = new ApplicationUser { Id = "1", Email = "admin1@sample.jp", UserName = "admin1@sample.jp", ApplicationRoleName = "Admin" };
        await userManager.CreateAsync(admin, "Admin1!");
        await userManager.AddToRoleAsync(admin, "Admin");
    }
    if (await userManager.FindByNameAsync("member1@sample.jp") == null)
    {
        var member = new ApplicationUser { Id = "2", Email = "member1@sample.jp", UserName = "member1@sample.jp", ApplicationRoleName = "Member" };
        await userManager.CreateAsync(member, "Member1!");
        await userManager.AddToRoleAsync(member, "Member");
    }
}
