using AltinZamani.Data;
using AltinZamani.Services;
using Microsoft.EntityFrameworkCore;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// HttpClient servisini sisteme tanýtýyoruz (API çaðrýlarý için kullanacaðýz)
builder.Services.AddHttpClient();

// MarketDataService'i sisteme tanýtýyoruz (veri çekme ve veritabanýna kaydetme iþlemleri için)
builder.Services.AddScoped<MarketDataService>();

// Veritabaný (DbContext) baðlantýmýzý sisteme tanýtýyoruz
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

// --- HANGFIRE SERVÝS KURULUMLARI (Burasý silinmiþti, geri ekledik) ---
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("SqlServer")));

// Hangfire arka plan iþleyicisini (Sunucusunu) baþlatýyoruz
builder.Services.AddHangfireServer();
// ---------------------------------------------------------------------

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire");

using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    // 1. Görev: API'den Veri Çekme (Her 2 saatte bir çalýþýr)
    recurringJobManager.AddOrUpdate<MarketDataService>(
        "api-veri-cekme-gorevi",
        service => service.FetchAndSaveMarketDataAsync(),
        "0 */2 * * *"
    );

    // 2. Görev: Eski Verileri Temizleme (Her gece saat 01:00'da çalýþýr)
    recurringJobManager.AddOrUpdate<MarketDataService>(
        "eski-verileri-temizleme-gorevi",
        service => service.CleanUpOldDataAsync(),
        Cron.Daily(7, 0)
    );
}
// -------------------------------------------------------------

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();