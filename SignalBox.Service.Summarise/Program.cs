using OpenAI;
using OpenAI.Chat;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.AddRedisDistributedCache("cache-service-summarise");

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

builder.Services.AddSingleton<OpenAIClient>(serviceProvider =>
{
    IConfiguration? configuration = serviceProvider.GetService<IConfiguration>();

    string? baseUrl = configuration?.GetValue<string>("LanguageModel:BaseUrl");

    if (string.IsNullOrEmpty(baseUrl))
    {
        throw new InvalidOperationException("LanguageModel:BaseUrl is not configured in appsettings.");
    }

    var options = new OpenAIClientOptions();

    options.Endpoint = new Uri(baseUrl);
    
    return new OpenAIClient(new System.ClientModel.ApiKeyCredential("dummy"), options);
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/process", SummariseHandlerAsync)
    .WithName("Summarise")
    .WithSummary("Given markdown content, generate a summary")
    .WithDescription("Uses an AI model to create a concise summary of the provided markdown text.")
    .WithOpenApi();

app.MapDefaultEndpoints();

app.Run();

async Task<IResult> SummariseHandlerAsync(
    [FromServices] OpenAIClient openAIClient,
    [FromServices] IDistributedCache cache,
    [FromBody] SummariseRequest request,
    CancellationToken cancellationToken)
{
    try
    {
        if (string.IsNullOrWhiteSpace(request.Markdown))
        {
            return Results.BadRequest(new { error = "Markdown parameter is required and cannot be empty." });
        }
        
        string hash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes(request.Markdown)
            )
        );

        var cacheKey = $"summarise:{hash}";

        var cachedData = await cache.GetAsync(cacheKey);

        if (cachedData is not null)
        {
            var cachedResult = JsonSerializer.Deserialize<SummariseResponse>(cachedData);

            return Results.Ok(cachedResult);
        }

        var modelId = app.Configuration.GetValue<string>("LanguageModel:Model") ?? string.Empty;
        
        if (string.IsNullOrEmpty(modelId))
        {
            return Results.Problem(
                title: "Language model not configured",
                detail: "LanguageModel:Model is not set in configuration.",
                statusCode: 500
            );
        }

        var chatClient = openAIClient.GetChatClient(modelId);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                $"You are a helpful assistant that specializes in summarizing markdown content. " +
                $"Your task is to generate a concise summary of the data provided. " +
                $"Highlight the main themes of the data in a short paragraph, with no more than {request.CharacterLimit} characters. " +
                $"Use friendly and playful language. " +
                $"Do not mention that the summary is generated from data input. " +
                $"Do not include the raw original data in your response. " +
                "Format the summary in plain text."),
            new UserChatMessage([
                ChatMessageContentPart.CreateTextPart("Please summarize the following markdown content:"),
                ChatMessageContentPart.CreateTextPart($"```markdown\n{request.Markdown}\n```")
            ])
        };

        var response = await chatClient.CompleteChatAsync(messages);
        var completion = response.Value;
        var summaryText = completion.Content[0].Text;
            
        var results = new SummariseResponse
        {
            Summary = summaryText
        };

        await cache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(results)), new ()
        {
            AbsoluteExpiration = DateTime.Now.AddMinutes(30)
        });

        return Results.Ok(results);
    }  
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Error processing summarization request",
            detail: ex.Message,
            statusCode: 500
        );
    }
}

public record SummariseRequest
{
    [Required]
    public string Markdown { get; init; } = string.Empty;
    [Required]
    public int CharacterLimit { get; init;  } = 400;
}

public record SummariseResponse
{
    public string Summary { get; init; } = string.Empty;    
}
