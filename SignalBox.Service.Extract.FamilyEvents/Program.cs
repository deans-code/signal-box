using System.ComponentModel.DataAnnotations;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddRedisDistributedCache("cache-service-extract-familyevents");

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/process", ExtractFamilyEventsHandlerAsync)
    .WithName("extractfamilyevents")
    .WithSummary("Extracts events from HTML content")
    .WithDescription("Extracts events from HTML content, for HTML which adheres to a predefined format.")
    .WithOpenApi();

app.MapDefaultEndpoints();

app.Run();    

async Task<IResult> ExtractFamilyEventsHandlerAsync(
    [FromServices] IHttpClientFactory httpClientFactory,
    [FromServices] IDistributedCache cache,
    [FromBody] ExtractFamilyEventsRequest request,
    CancellationToken cancellationToken)
{
    try
    {
        string hash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes(request.Html)
            )
        );

        var cacheKey = $"extractfamilyevents:{hash}";

        var cachedData = await cache.GetAsync(cacheKey);

        if (cachedData is not null)
        {
            var cachedResult = JsonSerializer.Deserialize<ExtractFamilyEventsResponse>(cachedData);

            return Results.Ok(cachedResult);
        }

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(request.Html);

        HtmlNode eventContainer = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='eventcontainer']");
        
        if (eventContainer == null)
        {
            return Results.Problem($"No element with ID 'eventcontainer' found on the page", statusCode: 404);
        }

        HtmlNodeCollection detailsDivs = eventContainer.SelectNodes("(.//div[contains(@class,'details')])[position() <= 5]");
        
        if (detailsDivs == null || !detailsDivs.Any())
        {
            return Results.Problem($"No div elements with class 'details' found within eventcontainer on the page", statusCode: 404);
        }

        var results = new ExtractFamilyEventsResponse
        {
            Events = ExtractEvents(detailsDivs)
        };

        await cache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(results)), new()
        {
            AbsoluteExpiration = DateTime.Now.AddMinutes(30)
        });

        return Results.Ok(results);
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred while extracting: {ex.Message}", statusCode: 500);
    }
}

List<FamilyEvent> ExtractEvents(HtmlNodeCollection detailsDivs)
{
    var events = new List<FamilyEvent>();
    
    foreach (var detailsDiv in detailsDivs)
    {
        HtmlNode itemDiv = detailsDiv.ParentNode;

        if (itemDiv == null) continue;

        HtmlNode titleLink = detailsDiv.SelectSingleNode(".//a");

        if (titleLink == null) continue;

        string url = titleLink.GetAttributeValue("href", string.Empty);
        string title = System.Net.WebUtility.HtmlDecode(titleLink.GetAttributeValue("title", string.Empty));

        HtmlNode infoDiv = detailsDiv.SelectSingleNode(".//div[@style]");
        string infoText = infoDiv?.InnerHtml ?? string.Empty;
        string[] infoParts = infoText.Split(["<br>", "<br/>", "<br />"], StringSplitOptions.RemoveEmptyEntries);

        string location = infoParts.Length > 0 ? System.Net.WebUtility.HtmlDecode(infoParts[0].Trim()) : string.Empty;
        string dateRange = infoParts.Length > 1 ? System.Net.WebUtility.HtmlDecode(infoParts[1].Trim()) : string.Empty;

        events.Add(new FamilyEvent
        {
            Url = url,
            Title = title,            
            Location = location,
            DateRange = dateRange
        });
    }
    
    return events;
}

public record ExtractFamilyEventsRequest
{
    [Required]
    public string Html { get; init; } = string.Empty;
}

public record ExtractFamilyEventsResponse
{            
    public List<FamilyEvent>? Events { get; init; }
}

public record FamilyEvent
{
    public string Url { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;    
    public string Location { get; init; } = string.Empty;
    public string DateRange { get; init; } = string.Empty;
}