using System.Text;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

builder.Services.AddHttpClient<ScrapeApiClient>(client =>
    {        
        client.BaseAddress = new(builder.Configuration["Services:ScrapeUrl"] ?? string.Empty);
    });

builder.Services.AddHttpClient<ExtractFamilyEventsApiClient>(client =>
    {        
        client.BaseAddress = new(builder.Configuration["Services:ExtractFamilyEventsUrl"] ?? string.Empty);
    });

builder.Services.AddHttpClient<SummariseApiClient>(client =>
    {        
        client.BaseAddress = new(builder.Configuration["Services:SummariseUrl"] ?? string.Empty);
    });

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/process", WhatsOnHandlerAsync)
    .WithName("whatson")
    .WithSummary("Get a summary of what's on in the local area")
    .WithDescription("Gets a list of events for families in the local area and generates a summary.")
    .WithOpenApi();

app.MapDefaultEndpoints();

app.Run();

async Task<IResult> WhatsOnHandlerAsync(
    [FromServices] ScrapeApiClient scrapeClient,
    [FromServices] ExtractFamilyEventsApiClient extractFamilyEventsClient,
    [FromServices] SummariseApiClient summariseClient,
    IConfiguration configuration)
{
    try
    {
        string targetUrl = configuration
            .GetValue<string>("WhatsOn:TargetUrl")
            ?? throw new InvalidOperationException("WhatsOn:TargetUrl not configured in appsettings");

        ScrapeResponse? ScrapeResponse = await scrapeClient.GetScrapeResponseAsync(targetUrl);

        if (ScrapeResponse?.Html == null)
        {
            return Results.Problem("No HTML content received from scrape service", statusCode: 500);
        }

        ExtractFamilyEventsResponse? extractFamilyEventsResponse = await extractFamilyEventsClient.GetExtractFamilyEventsResponseAsync(ScrapeResponse.Html);

        if (extractFamilyEventsResponse?.Events == null)
        {
            return Results.Problem("No events received from extract service", statusCode: 500);
        }

        SummaryResponse? summariseResult = await summariseClient.GetSummaryResponseAsync(extractFamilyEventsResponse.Events);

        if (summariseResult?.Summary == null)
        {
            return Results.Problem("No summary received from summarise service", statusCode: 500);
        }

        return Results.Ok(new WhatsOnResult
        {
            TargetUrl = targetUrl,
            Summary = summariseResult.Summary,
            FamilyEvents = extractFamilyEventsResponse.Events,
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred processing the request: {ex.Message}", statusCode: 500);
    }
}

internal class ScrapeApiClient(HttpClient httpClient)
{
    public async Task<ScrapeResponse?> GetScrapeResponseAsync(string url, CancellationToken cancellationToken = default)
    {
        var request = new { url };

        HttpResponseMessage response = await httpClient.PostAsJsonAsync("/process", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ScrapeResponse>(cancellationToken);
    }
}

internal class ExtractFamilyEventsApiClient(HttpClient httpClient)
{
    public async Task<ExtractFamilyEventsResponse?> GetExtractFamilyEventsResponseAsync(string html, CancellationToken cancellationToken = default)
    {
        var request = new { html };

        HttpResponseMessage response = await httpClient.PostAsJsonAsync("/process", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ExtractFamilyEventsResponse>(cancellationToken);
    }
}

internal class SummariseApiClient(HttpClient httpClient)
{
    public async Task<SummaryResponse?> GetSummaryResponseAsync(List<FamilyEvent> events, CancellationToken cancellationToken = default)
    {
        string markdown = EventsToMarkdown(events);

        var request = new { Markdown = markdown, CharacterLimit = 400 };

        HttpResponseMessage response = await httpClient.PostAsJsonAsync("/process", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SummaryResponse>(cancellationToken);
    }

    private string EventsToMarkdown(List<FamilyEvent> events)
    {
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine("# Events");
        stringBuilder.AppendLine();

        foreach (FamilyEvent familyEvent in events)
        {
            stringBuilder.AppendLine($"## {EscapeMarkdown(familyEvent.Title)}");

            if (!string.IsNullOrWhiteSpace(familyEvent.DateRange))
            {
                stringBuilder.AppendLine($"- **Date:** {EscapeMarkdown(familyEvent.DateRange)}");
            }

            if (!string.IsNullOrWhiteSpace(familyEvent.Location))
            {
                stringBuilder.AppendLine($"- **Location:** {EscapeMarkdown(familyEvent.Location)}");
            }

            if (!string.IsNullOrWhiteSpace(familyEvent.Url))
            {
                stringBuilder.AppendLine($"- **URL:** {EscapeMarkdown(familyEvent.Url)}");
            }
            
            stringBuilder.AppendLine();
        }

        return stringBuilder.ToString();
    }

    string EscapeMarkdown(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        
        return input
            .Replace("\\", "\\\\")
            .Replace("*", "\\*")
            .Replace("_", "\\_")
            .Replace("`", "\\`")
            .Replace("#", "\\#")
            .Replace("-", "\\-")
            .Replace(">", "\\>");
    }
}

internal record ScrapeResponse
{
    public required string Url { get; init; }
    public required string Html { get; init; }
    public DateTimeOffset ScrapedAt { get; init; }
}

internal record ExtractFamilyEventsResponse
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

internal record SummaryResponse
{
    public required string Summary { get; init; }    
}

public record WhatsOnResult
{     
    public required string TargetUrl { get; init; }
    public required string Summary { get; init; }
    public required List<FamilyEvent> FamilyEvents { get; init; }    
}