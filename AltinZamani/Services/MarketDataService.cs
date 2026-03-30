using AltinZamani.Data;
using AltinZamani.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AltinZamani.Services;

public class MarketDataService(ApplicationDbContext context, ILogger<MarketDataService> logger, HttpClient httpClient)
{
    public async Task FetchAndSaveMarketDataAsync()
    {
        // 1. Bot engeline takılmamak için tarayıcı kimliği
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        // 2. V4 yerine EN STABİL ana sürümü kullanıyoruz!
        string apiUrl = "https://finans.truncgil.com/today.json";

        var response = await httpClient.GetAsync(apiUrl);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"API ulaşılamaz durumda! HTTP Kodu: {response.StatusCode}");
        }

        var jsonString = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(jsonString) || !jsonString.TrimStart().StartsWith("{"))
        {
            throw new Exception("API'den JSON formatında veri gelmedi!");
        }

        JsonDocument doc;
        try
        {
            // 3. Eksik/Bozuk JSON gelirse sistemi çökertmeden hatayı yakalıyoruz
            doc = JsonDocument.Parse(jsonString);
        }
        catch (JsonException ex)
        {
            // Hata verirse Hangfire'a düşecek ve verinin ne kadarının geldiğini göreceğiz
            throw new Exception($"API veriyi yarım gönderdi! Hata: {ex.Message} | Gelen Veri Uzunluğu: {jsonString.Length}");
        }

        using (doc)
        {
            var root = doc.RootElement;
            var fetchedData = new List<MarketData>();
            var today = DateTime.Today;

            foreach (var property in root.EnumerateObject())
            {
                if (property.Name == "Update_Date" || property.Name == "guncellenme_zamani")
                    continue;

                try
                {
                    var element = property.Value;

                    // ToString() ile her türlü veriyi (sayı, metin vs.) string'e güvenle çeviriyoruz
                    string sellingStr = "0";
                    if (element.TryGetProperty("Selling", out var sellProp))
                        sellingStr = sellProp.ToString();
                    else if (element.TryGetProperty("Satış", out var satisProp))
                        sellingStr = satisProp.ToString();

                    string changeStr = "0";
                    if (element.TryGetProperty("Change", out var changeProp))
                        changeStr = changeProp.ToString();
                    else if (element.TryGetProperty("Değişim", out var degisimProp))
                        changeStr = degisimProp.ToString();

                    changeStr = changeStr.Replace("%", "").Trim();

                    decimal lastPrice = ParseToDecimal(sellingStr);
                    decimal changePercentage = ParseToDecimal(changeStr);

                    if (lastPrice > 0)
                    {
                        fetchedData.Add(new MarketData
                        {
                            SiteType = "altinzamani",
                            Name = property.Name,
                            LastPrice = lastPrice,
                            ChangePercentage = changePercentage,
                            ChangeAmount = 0,
                            VolumeAmount = null,
                            VolumeCount = null,
                            RecordDate = DateTime.Now
                        });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"Dönüştürülemedi: {property.Name} - {ex.Message}");
                }
            }

            if (!fetchedData.Any())
            {
                throw new Exception("JSON başarıyla okundu ancak listeye eklenecek geçerli hiçbir fiyat bulunamadı!");
            }

            var todaysExistingData = await context.MarketDatas
                .Where(m => m.RecordDate.Date == today && m.SiteType == "altinzamani")
                .ToListAsync();

            if (todaysExistingData.Any())
            {
                context.MarketDatas.RemoveRange(todaysExistingData);
            }

            await context.MarketDatas.AddRangeAsync(fetchedData);
            await context.SaveChangesAsync();
        }
    }

    public async Task CleanUpOldDataAsync()
    {
        var setting = await context.SiteSettings.FirstOrDefaultAsync();
        int retentionDays = setting?.DataRetentionDays ?? 5;

        var thresholdDate = DateTime.Today.AddDays(-retentionDays);

        var oldRecords = await context.MarketDatas
            .Where(m => m.RecordDate.Date < thresholdDate)
            .ToListAsync();

        if (oldRecords.Any())
        {
            context.MarketDatas.RemoveRange(oldRecords);
            await context.SaveChangesAsync();
        }
    }

    // Her türlü formattaki (1.500,55 veya 1500.55 vb.) sayıyı güvenle Decimal'e çevirir
    private decimal ParseToDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "0") return 0;

        // Boşlukları ve para birimi sembollerini temizle
        value = value.Replace(" ", "").Replace("₺", "").Replace("$", "").Replace("€", "");

        if (value.Contains(",") && value.Contains("."))
        {
            value = value.Replace(".", "");
            value = value.Replace(",", ".");
        }
        else if (value.Contains(","))
        {
            value = value.Replace(",", ".");
        }

        if (decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal result))
        {
            return result;
        }

        return 0;
    }
}