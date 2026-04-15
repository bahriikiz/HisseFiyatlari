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

        // API URL'sini yapılandırma dosyasından alıyoruz (Yahoo Finance linki olmalı)
        string apiUrl = configuration["MarketApi:Url"] ?? throw new InvalidOperationException("API URL yapılandırma dosyasında (appsettings.json 'MarketApi:Url') bulunamadı!");

        var jsonString = await FetchJsonFromApiAsync(apiUrl);

        using var doc = ParseJsonDocument(jsonString);
        var fetchedData = ExtractMarketData(doc.RootElement);

        if (fetchedData.Count == 0)
        {
            throw new InvalidOperationException("JSON başarıyla okundu ancak listeye eklenecek geçerli hiçbir hisse fiyatı bulunamadı!");
        }

        await context.MarketDatas.AddRangeAsync(fetchedData);
        await context.SaveChangesAsync();

        // Ön yüze canlı güncelleme tetiklemesi gönderiyoruz
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

        try
        {
            // Yahoo Finance JSON yapısında asıl veri quoteResponse -> result dizisinin içindedir
            if (root.TryGetProperty("quoteResponse", out var quoteResponse) &&
                quoteResponse.TryGetProperty("result", out var resultList))
            {
                foreach (var item in resultList.EnumerateArray())
                {
                    var marketItem = ProcessMarketDataItem(item);
                    if (marketItem != null)
                    {
                        fetchedData.Add(marketItem);
                    }
                }
            }
            else
            {
                logger.LogWarning("Yahoo Finance JSON formatı beklenenden farklı. 'quoteResponse.result' yolu bulunamadı.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Yahoo Finance JSON verisi işlenirken bir hata oluştu!");
        }

        return fetchedData;
    }

    private static MarketData? ProcessMarketDataItem(JsonElement item)
    {
        try
        {
            // Hisse kodunu alıyoruz ve ekranda temiz görünmesi için sonundaki ".IS" takısını atıyoruz
            string symbol = item.TryGetProperty("symbol", out var symProp) ? symProp.GetString()?.Replace(".IS", "") ?? "Bilinmeyen" : "Bilinmeyen";

            // Sayısal değerleri JSON elementinden doğrudan decimal ve long olarak çekiyoruz
            decimal lastPrice = item.TryGetProperty("regularMarketPrice", out var priceProp) ? priceProp.GetDecimal() : 0;
            decimal changePercentage = item.TryGetProperty("regularMarketChangePercent", out var changeProp) ? changeProp.GetDecimal() : 0;
            decimal changeAmount = item.TryGetProperty("regularMarketChange", out var changeAmtProp) ? changeAmtProp.GetDecimal() : 0;
            long volumeCount = item.TryGetProperty("regularMarketVolume", out var volProp) ? volProp.GetInt64() : 0;

            if (lastPrice > 0)
            {
                return new MarketData
                {
                    SiteType = "HisseFiyatlari",
                    Name = symbol,
                    LastPrice = lastPrice,
                    ChangePercentage = changePercentage,
                    ChangeAmount = changeAmount,
                    VolumeAmount = null, // Eğer Yahoo hacmi parasal değer olarak verirse buraya eklenebilir
                    VolumeCount = (int)(volumeCount / 1000), // Çok büyük sayıları önlemek için "Bin Lot" cinsinden kaydediyoruz
                    RecordDate = DateTime.Now
                };
            }
        }
        catch (Exception)
        {
            // Eğer bir hissenin verisinde bozukluk varsa sistemi durdurmaması için sadece o hisseyi atlıyoruz
            return null;
        }

        return null;
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
}