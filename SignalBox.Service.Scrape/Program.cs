using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

var builder = WebApplication.CreateBuilder(args);

builder.AddRedisDistributedCache("cache-service-scrape");

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

builder.Services.AddHttpClient("ScrapingClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
    {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip |
                               System.Net.DecompressionMethods.Deflate |
                               System.Net.DecompressionMethods.Brotli
    });

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/process", ScrapeHandlerAsync)
    .WithName("Scrape")
    .WithSummary("Scrape a web page")
    .WithDescription("Fetches HTML content from the provided URL.")
    .WithOpenApi();

app.MapDefaultEndpoints();

app.Run();

async Task<IResult> ScrapeHandlerAsync(
    [FromServices] IHttpClientFactory httpClientFactory,
    [FromServices] IDistributedCache cache,
    [FromBody] ScrapeRequest request,
    CancellationToken cancellationToken)
{
    try
    {
        string hash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes(request.Url)
            )
        );

        var cacheKey = $"scrape:{hash}";

        var cachedData = await cache.GetAsync(cacheKey);

        if (cachedData is not null)
        {
            var cachedResult = JsonSerializer.Deserialize<ScrapeResponse>(cachedData);

            return Results.Ok(cachedResult);
        }
            
        using var httpClient = httpClientFactory.CreateClient("ScrapingClient");
        
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var validUri) ||
            (validUri.Scheme != Uri.UriSchemeHttp && validUri.Scheme != Uri.UriSchemeHttps))
        {
            return Results.BadRequest("Invalid URL provided. Please provide a valid HTTP or HTTPS URL.");
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, validUri);
        httpRequest.Headers.Clear();
        httpRequest.Headers.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");
        httpRequest.Headers.TryAddWithoutValidation(
            "Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
        httpRequest.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
        httpRequest.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");
        httpRequest.Headers.TryAddWithoutValidation("DNT", "1");
        httpRequest.Headers.TryAddWithoutValidation("Connection", "keep-alive");
        httpRequest.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");

        HttpResponseMessage response = await httpClient.SendAsync(httpRequest);

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return Results.Problem(
                $"Access forbidden (403) for URL: {request.Url}. The website may be blocking automated requests.",
                statusCode: 403);
        }

        response.EnsureSuccessStatusCode();

        var htmlContent = await response.Content.ReadAsStringAsync();

        var results = new ScrapeResponse
        {
            Url = request.Url,
            Html = htmlContent,
            ScrapedAt = DateTimeOffset.UtcNow
        };

        await cache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(results)), new ()
        {
            AbsoluteExpiration = DateTime.Now.AddMinutes(30)
        });

        return Results.Ok(results);        
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem($"Failed to fetch content from URL: {ex.Message}", statusCode: 400);
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred while scraping: {ex.Message}", statusCode: 500);
    }    
}

public record ScrapeRequest
{
    [Required]
    public string Url { get; init; } = string.Empty;
}

public record ScrapeResponse
{
    public required string Url { get; init; }
    public required string Html { get; init; }
    public DateTimeOffset ScrapedAt { get; init; }
}