using Dev.CommonLibrary.Entity;
using WizardSample.Data;
using WizardSample.Repository;
using WizardSample.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

builder.Services.AddDbContext<WizardSampleDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SiteConnection")));

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
.AddEntityFrameworkStores<WizardSampleDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/LogOff";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(1440);
    options.Cookie.HttpOnly = true;
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(1440);
    options.Cookie.HttpOnly = true;
});

builder.Services.AddHttpContextAccessor();

// DI 登録
builder.Services.AddScoped<WizardSampleService>();
builder.Services.AddScoped<Dev.CommonLibrary.Attributes.AccessLogAttribute>();

var app = builder.Build();

// HttpContextAccessor を EntityBase に設定（共通ライブラリの監査カラム自動セット用）
var accessor = app.Services.GetRequiredService<IHttpContextAccessor>();
Dev.CommonLibrary.Entity.EntityBase.HttpContextAccessor = accessor;

// ロガー設定
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

app.MapControllerRoute("default", "{controller=WizardSample}/{action=Index}/{id?}");

app.Run();
