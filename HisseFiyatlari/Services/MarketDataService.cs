using HisseFiyatlari.Data;
using HisseFiyatlari.Hubs; 
using HisseFiyatlari.Models;
using Microsoft.AspNetCore.SignalR; 
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HisseFiyatlari.Services;


public class MarketDataService(ApplicationDbContext context, ILogger<MarketDataService> logger, HttpClient httpClient, IConfiguration configuration, IHubContext<MarketHub> hubContext)
{
    public async Task FetchAndSaveMarketDataAsync()
    {
        SetupHttpClient();
        // API URL'sini yapılandırma dosyasından alıyoruz
        string apiUrl = configuration["MarketApi:Url"] ?? throw new InvalidOperationException("API URL yapılandırma dosyasında (appsettings.json 'MarketApi:Url') bulunamadı!");

        var jsonString = await FetchJsonFromApiAsync(apiUrl);

        using var doc = ParseJsonDocument(jsonString);
        var fetchedData = ExtractMarketData(doc.RootElement);

        if (fetchedData.Count == 0)
        {
            throw new InvalidOperationException("JSON başarıyla okundu ancak listeye eklenecek geçerli hiçbir fiyat bulunamadı!");
        }

        await context.MarketDatas.AddRangeAsync(fetchedData);
        await context.SaveChangesAsync();
        await hubContext.Clients.All.SendAsync("ReceiveMarketUpdate");
    }

    private void SetupHttpClient()
    {
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    private async Task<string> FetchJsonFromApiAsync(string apiUrl)
    {
        var response = await httpClient.GetAsync(apiUrl);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"API ulaşılamaz durumda! HTTP Kodu: {response.StatusCode}");
        }

        var jsonString = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(jsonString) || !jsonString.TrimStart().StartsWith('{'))
        {
            throw new InvalidDataException("API'den JSON formatında veri gelmedi!");
        }

        return jsonString;
    }

    private static JsonDocument ParseJsonDocument(string jsonString)
    {
        try
        {
            return JsonDocument.Parse(jsonString);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"API veriyi yarım gönderdi! Gelen Veri Uzunluğu: {jsonString.Length}", ex);
        }
    }

    private List<MarketData> ExtractMarketData(JsonElement root)
    {
        var fetchedData = new List<MarketData>();

        foreach (var property in root.EnumerateObject())
        {
            if (property.Name == "Update_Date" || property.Name == "guncellenme_zamani")
                continue;

            try
            {
                var marketItem = ProcessMarketDataItem(property);
                if (marketItem != null)
                {
                    fetchedData.Add(marketItem);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Dönüştürülemedi: {PropertyName} - {ErrorMessage}", property.Name, ex.Message);
            }
        }

        return fetchedData;
    }

    private static MarketData? ProcessMarketDataItem(JsonProperty property)
    {
        var element = property.Value;

        string sellingStr = ExtractValue(element, "Selling", "Satış");
        string changeStr = ExtractValue(element, "Change", "Değişim");

        changeStr = changeStr.Replace("%", "").Trim();

        decimal lastPrice = ParseToDecimal(sellingStr);
        decimal changePercentage = ParseToDecimal(changeStr);

        if (lastPrice > 0)
        {
            return new MarketData
            {
                SiteType = "HisseFiyatlari",
                Name = property.Name,
                LastPrice = lastPrice,
                ChangePercentage = changePercentage,
                ChangeAmount = 0,
                VolumeAmount = null,
                VolumeCount = null,
                RecordDate = DateTime.Now
            };
        }

        return null;
    }

    private static string ExtractValue(JsonElement element, string propName1, string propName2)
    {
        if (element.TryGetProperty(propName1, out var prop1) && prop1.ValueKind != JsonValueKind.Null)
            return prop1.ToString();

        if (element.TryGetProperty(propName2, out var prop2) && prop2.ValueKind != JsonValueKind.Null)
            return prop2.ToString();

        return "0";
    }

    public async Task CleanUpOldDataAsync()
    {
        var setting = await context.SiteSettings.FirstOrDefaultAsync();
        int retentionDays = setting?.DataRetentionDays ?? 5;

        var thresholdDate = DateTime.Today.AddDays(-retentionDays);

        var oldRecords = await context.MarketDatas
            .Where(m => m.RecordDate.Date < thresholdDate)
            .ToListAsync();

        if (oldRecords.Count != 0)
        {
            context.MarketDatas.RemoveRange(oldRecords);
            await context.SaveChangesAsync();
        }
    }

    private static decimal ParseToDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "0") return 0;

        value = value.Replace(" ", "").Replace("₺", "").Replace("$", "").Replace("€", "");

        if (value.Contains(',') && value.Contains('.'))
        {
            value = value.Replace(".", "");
            value = value.Replace(',', '.');
        }
        else if (value.Contains(','))
        {
            value = value.Replace(',', '.');
        }

        if (decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal result))
        {
            return result;
        }

        return 0;
    }
}