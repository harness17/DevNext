using ApiClientSample.Services;

var builder = WebApplication.CreateBuilder(args);

// HttpClient（ApiSample 向け）
var apiBaseUrl = builder.Configuration["ApiSample:BaseUrl"]
    ?? throw new InvalidOperationException("ApiSample:BaseUrl が設定されていません。");

builder.Services.AddHttpClient<ApiSampleClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Session（JWT トークン保持に使用）
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
