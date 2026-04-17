using HisseFiyatlari.Data;
using HisseFiyatlari.Hubs;
using HisseFiyatlari.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HisseFiyatlari.Services;

public class MarketDataService(ApplicationDbContext context, ILogger<MarketDataService> logger, HttpClient httpClient, IConfiguration configuration, IHubContext<MarketHub> hubContext)
{
    // Hangfire servisi her tetiklediğinde izole bir şekilde kendi değişkenlerini yönetecek.
    private string? _yahooCrumb;
    private string? _yahooCookie;

    public async Task FetchAndSaveMarketDataAsync()
    {
        await EnsureYahooAuthAsync();

        string baseUrl = configuration["MarketApi:Url"] ?? throw new InvalidOperationException("API URL yapılandırma dosyasında bulunamadı!");

        string apiUrl = $"{baseUrl}&crumb={_yahooCrumb}";

        var jsonString = await FetchJsonFromApiAsync(apiUrl);

        using var doc = ParseJsonDocument(jsonString);
        var fetchedData = ExtractMarketData(doc.RootElement, logger);

        if (fetchedData.Count == 0)
        {
            throw new InvalidOperationException("JSON başarıyla okundu ancak listeye eklenecek geçerli hiçbir hisse fiyatı bulunamadı!");
        }

        await context.MarketDatas.AddRangeAsync(fetchedData);
        await context.SaveChangesAsync();

        await hubContext.Clients.All.SendAsync("ReceiveMarketUpdate");
    }

    private async Task EnsureYahooAuthAsync()
    {
        if (!string.IsNullOrEmpty(_yahooCrumb) && !string.IsNullOrEmpty(_yahooCookie))
            return;

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        try
        {
            // Hardcoded URL uyarılarını çözmek için adresleri appsettings üzerinden çekiyoruz.
            string cookieUrl = configuration["MarketApi:CookieUrl"] ?? throw new InvalidOperationException("Cookie URL bulunamadı!");
            string crumbUrl = configuration["MarketApi:CrumbUrl"] ?? throw new InvalidOperationException("Crumb URL bulunamadı!");

            var cookieReq = new HttpRequestMessage(HttpMethod.Get, cookieUrl);
            var cookieRes = await httpClient.SendAsync(cookieReq);

            if (cookieRes.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                _yahooCookie = cookies.FirstOrDefault()?.Split(';')[0];
            }

            var crumbReq = new HttpRequestMessage(HttpMethod.Get, crumbUrl);
            if (!string.IsNullOrEmpty(_yahooCookie))
            {
                crumbReq.Headers.Add("Cookie", _yahooCookie);
            }

            var crumbRes = await httpClient.SendAsync(crumbReq);
            if (crumbRes.IsSuccessStatusCode)
            {
                _yahooCrumb = await crumbRes.Content.ReadAsStringAsync();
            }
            else
            {
                logger.LogWarning("Yahoo Crumb alınamadı. Yanıt: {StatusCode}", crumbRes.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Yahoo yetkilendirme (Cookie/Crumb) işlemi sırasında hata oluştu.");
        }
    }

    private async Task<string> FetchJsonFromApiAsync(string apiUrl)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");

        if (!string.IsNullOrEmpty(_yahooCookie))
        {
            request.Headers.Add("Cookie", _yahooCookie);
        }

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            _yahooCrumb = null;
            _yahooCookie = null;
            throw new HttpRequestException($"API ulaşılamaz durumda! HTTP Kodu: {response.StatusCode}");
        }

        var jsonString = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(jsonString))
        {
            throw new InvalidDataException("API'den veri gelmedi!");
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
            throw new InvalidDataException("API JSON formatı bozuk geldi!", ex);
        }
    }

    private static List<MarketData> ExtractMarketData(JsonElement root, ILogger logger)
    {
        var fetchedData = new List<MarketData>();

        try
        {
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
                logger.LogWarning("Yahoo Finance JSON formatı değişmiş olabilir, 'quoteResponse.result' bulunamadı.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Yahoo Finance JSON verisi işlenirken hata oluştu!");
        }

        return fetchedData;
    }

    private static MarketData? ProcessMarketDataItem(JsonElement item)
    {
        try
        {
            string symbol = item.TryGetProperty("symbol", out var symProp) ? symProp.GetString()?.Replace(".IS", "") ?? "Bilinmeyen" : "Bilinmeyen";

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
                    VolumeCount = (int)(volumeCount / 1000),
                    RecordDate = DateTime.Now
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hata oluşan sembol JSON: {item} - Hata: {ex.Message}");
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