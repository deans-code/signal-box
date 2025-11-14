using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

var builder = WebApplication.CreateBuilder(args);

builder.AddRedisDistributedCache("cache-{{SERVICE_NAME}}");

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

app.MapPost("/process", ProcessHandlerAsync)
    .WithName("Process")
    .WithSummary("A summary of the service.")
    .WithDescription("A longer description of what the service does.")
    .WithOpenApi();

app.MapDefaultEndpoints();

app.Run();

async Task<IResult> ProcessHandlerAsync(
    [FromServices] IDistributedCache cache,
    [FromBody] ProcessRequest request,
    CancellationToken cancellationToken)
{
    try
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Input))
        {
            return Results.BadRequest(new { error = "Input parameter is required and cannot be empty." });
        }

        // Generate cache key from input hash
        string hash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes(request.Input)
            )
        );

        var cacheKey = $"{{SERVICE_NAME}}:{hash}";

        // Check cache
        var cachedData = await cache.GetAsync(cacheKey, cancellationToken);

        if (cachedData is not null)
        {
            var cachedResult = JsonSerializer.Deserialize<ProcessResponse>(cachedData);
            return Results.Ok(cachedResult);
        }

        // TODO: Implement your service logic here
        var result = new ProcessResponse
        {
            Output = $"Processed: {request.Input}",
            ProcessedAt = DateTimeOffset.UtcNow
        };

        // Cache the result
        await cache.SetAsync(
            cacheKey, 
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result)), 
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.Now.AddMinutes(30)
            },
            cancellationToken);

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Error processing request",
            detail: ex.Message,
            statusCode: 500
        );
    }
}

public record ProcessRequest
{
    [Required]
    public string Input { get; init; } = string.Empty;
}

public record ProcessResponse
{
    public required string Output { get; init; }
    public DateTimeOffset ProcessedAt { get; init; }
}