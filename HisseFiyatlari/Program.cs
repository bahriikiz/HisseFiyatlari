using HisseFiyatlari.Data;
using HisseFiyatlari.Services;
using HisseFiyatlari.Hubs;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Kimlik Doðrulama Ayarlarý
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.LogoutPath = "/Admin/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
    });

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// Servis Kayýtlarý
builder.Services.AddScoped<MarketDataService>();

// Veritabaný Baðlantýsý
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

// Hangfire Kurulumu
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("SqlServer")));

builder.Services.AddHangfireServer();

// SignalR Kaydý
builder.Services.AddSignalR();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire");

// ==========================================
// DÝNAMÝK GÖREV YÖNETÝMÝ (PANEL AYARLARINA GÖRE)
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var recurringJobManager = services.GetRequiredService<IRecurringJobManager>();
    var dbContext = services.GetRequiredService<ApplicationDbContext>();

    // Veritabanýndaki yönetici ayarlarýný çekiyoruz
    var settings = dbContext.SiteSettings.FirstOrDefault();

    // 1. Veri Çekme Sýklýðý Ayarý
    int fetchInterval = settings?.ApiFetchIntervalInHours ?? 2;
    string fetchCron = fetchInterval >= 24 ? "0 0 * * *" : $"0 */{fetchInterval} * * *";

    recurringJobManager.AddOrUpdate<MarketDataService>(
        "api-veri-cekme-gorevi",
        service => service.FetchAndSaveMarketDataAsync(),
        fetchCron
    );

    // 2. Veri Silme/Temizleme Ayarý
    recurringJobManager.AddOrUpdate<MarketDataService>(
        "eski-verileri-temizleme-gorevi",
        service => service.CleanUpOldDataAsync(),
        "0 1 * * *"
    );
}

app.MapHub<MarketHub>("/marketHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();