namespace SignalBox.FrontEnd.Web;

public class WhatsOnApiClient(HttpClient httpClient)
{
    public async Task<WhatsOnResult?> GetWhatsOnAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<WhatsOnResult>("/process", cancellationToken);
    }
}

public record FamilyEvent
{
    public string Url { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;    
    public string Location { get; init; } = string.Empty;
    public string DateRange { get; init; } = string.Empty;
}

public record WhatsOnResult
{
    public required string TargetUrl { get; init; }
    public required string Summary { get; init; }
    public required List<FamilyEvent> FamilyEvents { get; init; }
}