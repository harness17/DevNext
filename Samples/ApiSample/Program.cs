using System.Text;
using ApiSample.Data;
using ApiSample.Repository;
using ApiSample.Service;
using Dev.CommonLibrary.Entity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────
// DB（ApiSample 専用。EnsureCreated で自動作成するため Migration 不要）
// ─────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApiSampleDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SiteConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

// ─────────────────────────────────────────────────────────────
// Identity（認証用ユーザー・ロール管理）
// ─────────────────────────────────────────────────────────────
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
.AddEntityFrameworkStores<ApiSampleDbContext>()
.AddDefaultTokenProviders();

// ─────────────────────────────────────────────────────────────
// JWT Bearer 認証
// ─────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JwtSettings:SecretKey が設定されていません。");

builder.Services.AddAuthentication(options =>
{
    // デフォルトスキームを JWT に設定（Cookie 認証は使わない）
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "ApiSample",
        ValidAudience = jwtSettings["Audience"] ?? "ApiSampleClient",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
    };
});

builder.Services.AddAuthorization();

// ─────────────────────────────────────────────────────────────
// コントローラー
// ─────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ─────────────────────────────────────────────────────────────
// Swagger / OpenAPI（JWT Bearer 対応）
// ─────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ApiSample API",
        Version = "v1",
        Description = "ASP.NET Core 10 REST API + JWT 認証サンプル",
    });

    // Swagger UI で JWT トークンを入力できるよう Bearer 認証を定義
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT トークンを入力してください。例: Bearer {token}",
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            []
        },
    });
});

// ─────────────────────────────────────────────────────────────
// DI 登録
// ─────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ApiItemRepository>();
builder.Services.AddScoped<ApiItemService>();

var app = builder.Build();

// HttpContextAccessor を CommonLibrary の EntityBase に注入（監査カラム用）
var accessor = app.Services.GetRequiredService<IHttpContextAccessor>();
EntityBase.HttpContextAccessor = accessor;

// ─────────────────────────────────────────────────────────────
// DB 自動作成・シードデータ投入
// ─────────────────────────────────────────────────────────────
await using (var scope = app.Services.CreateAsyncScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<ApiSampleDbContext>();
    await db.Database.EnsureCreatedAsync();
    await SeedAsync(sp);
}

// ─────────────────────────────────────────────────────────────
// ミドルウェアパイプライン
// ─────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiSample v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// ─────────────────────────────────────────────────────────────
// シードデータ（ロール・初期ユーザー・商品サンプル）
// ─────────────────────────────────────────────────────────────
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
        var admin = new ApplicationUser
        {
            Id = "1",
            Email = "admin1@sample.jp",
            UserName = "admin1@sample.jp",
            ApplicationRoleName = "Admin",
        };
        await userManager.CreateAsync(admin, "Admin1!");
        await userManager.AddToRoleAsync(admin, "Admin");
    }

    if (await userManager.FindByNameAsync("member1@sample.jp") == null)
    {
        var member = new ApplicationUser
        {
            Id = "2",
            Email = "member1@sample.jp",
            UserName = "member1@sample.jp",
            ApplicationRoleName = "Member",
        };
        await userManager.CreateAsync(member, "Member1!");
        await userManager.AddToRoleAsync(member, "Member");
    }

    // 商品サンプルデータ（0件のときのみ投入）
    var db = sp.GetRequiredService<ApiSampleDbContext>();
    if (!db.ApiItems.Any())
    {
        db.ApiItems.AddRange(
            new ApiSample.Entity.ApiItem { Name = "ノートPC",      Description = "軽量で高性能なモバイルノート", Price = 128000m, Stock = 10 },
            new ApiSample.Entity.ApiItem { Name = "ワイヤレスマウス", Description = "静音クリックの人間工学デザイン", Price = 3980m,  Stock = 50 },
            new ApiSample.Entity.ApiItem { Name = "メカニカルキーボード", Description = "青軸採用の打鍵感に優れたキーボード", Price = 12800m, Stock = 25 },
            new ApiSample.Entity.ApiItem { Name = "USB-C ハブ",    Description = "7ポート対応の多機能ハブ",       Price = 4500m,  Stock = 30 },
            new ApiSample.Entity.ApiItem { Name = "27インチモニター", Description = "4K対応・IPS パネル・60Hz",      Price = 49800m, Stock = 8  }
        );
        await db.SaveChangesAsync();
    }
}
