var builder = DistributedApplication.CreateBuilder(args);

// Distributed caching

var cacheServiceScrape = builder.AddRedis("cache-service-scrape");
var cacheServiceExtractFamilyEvents = builder.AddRedis("cache-service-extract-familyevents");
var cacheServiceSummarise = builder.AddRedis("cache-service-summarise");

// Services

var scrape = builder.AddProject<Projects.SignalBox_Service_Scrape>("service-scrape")    
    .WithHttpHealthCheck("/health")
    .WithReference(cacheServiceScrape);

var extractFamilyEvents = builder.AddProject<Projects.SignalBox_Service_Extract_FamilyEvents>("service-extract-familyevents")
    .WithHttpHealthCheck("/health")
    .WithReference(cacheServiceExtractFamilyEvents);

var summarise = builder.AddProject<Projects.SignalBox_Service_Summarise>("service-summarise")    
    .WithHttpHealthCheck("/health")
    .WithReference(cacheServiceSummarise);

// Orchestration

var whatsOn = builder.AddProject<Projects.SignalBox_Orchestration_WhatsOn>("orchestration-whatson")    
    .WithHttpHealthCheck("/health")
    .WithReference(scrape)
    .WithReference(summarise)
    .WithReference(extractFamilyEvents)
    .WaitFor(scrape)
    .WaitFor(summarise)
    .WaitFor(extractFamilyEvents);

// Frontend

builder.AddProject<Projects.SignalBox_FrontEnd_Web>("frontend-web")
    .WithExternalHttpEndpoints() // Exposes the website, this is not required on services and orchestration
    .WithHttpHealthCheck("/health")
    .WithReference(whatsOn)
    .WaitFor(whatsOn);
    
// Run    

builder.Build().Run();