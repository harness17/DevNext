using Dev.CommonLibrary.Entity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Site.Common;
using Site.Entity;
using Site.Service;

var builder = WebApplication.CreateBuilder(args);

// Data Protection キーの永続化
// IIS環境でワーカープロセス再起動後もキーを維持し、認証クッキーの復号を可能にする
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("DevNext");

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

// DBContext
builder.Services.AddDbContext<DBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SiteConnection")));

// ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = false;
})
.AddEntityFrameworkStores<DBContext>()
.AddDefaultTokenProviders();

// Cookie認証設定
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/LogOff";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(1440);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(1440);
    options.Cookie.HttpOnly = true;
});

// IHttpContextAccessor (EntityBase用)
builder.Services.AddHttpContextAccessor();

// サービス登録
builder.Services.AddScoped<DatabaseSampleService>();
builder.Services.AddScoped<CommonService>();
builder.Services.AddScoped<Dev.CommonLibrary.Attributes.AccessLogAttribute>();

// メール送信サンプル
builder.Services.AddScoped<Site.Service.MailSampleService>();
// メール送信ログ一覧
builder.Services.AddScoped<Site.Service.MailLogService>();

// ファイル管理サンプル
builder.Services.AddScoped<Site.Service.FileManagementService>();

// 多段階フォームサンプル
builder.Services.AddScoped<Site.Service.WizardSampleService>();

// ユーザー・ロール管理
builder.Services.AddScoped<Site.Service.UserManagementService>();

// ダッシュボード
builder.Services.AddScoped<Site.Service.DashboardService>();

// 承認ワークフロー
builder.Services.AddScoped<Site.Service.ApprovalWorkflowService>();

// AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));

var app = builder.Build();

// IHttpContextAccessorをEntityBaseに設定
var accessor = app.Services.GetRequiredService<IHttpContextAccessor>();
EntityBase.HttpContextAccessor = accessor;

// Logger設定
var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
Dev.CommonLibrary.Common.Logger.GetLogger().SetLogger(loggerFactory.CreateLogger("App"));

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
