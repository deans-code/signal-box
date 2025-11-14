# SignalBox Service Template

This template can be used to create new services that follow the same structure as the existing SignalBox services.

## Features Included

- **Redis Distributed Caching**: Built-in caching support using Aspire Redis
- **POST /process Endpoint**: Standard service endpoint with caching
- **Request/Response Pattern**: Strongly-typed records with validation
- **Error Handling**: Comprehensive exception handling and problem details
- **OpenAPI/Swagger**: Auto-generated API documentation
- **Service Defaults**: Standard Aspire service configuration

## Usage

To create a new service from this template:

1. **Copy the template folder** to your desired location
2. **Rename the folder** from `SignalBox.Service.Template` to your desired project name (e.g., `SignalBox.Service.MyNewService`)
3. **Replace placeholders** in all files:
   - `{{PROJECT_NAME}}` → Your actual project name (e.g., `SignalBox.Service.MyNewService`)
   - `{{SERVICE_NAME}}` → Short service name for cache keys (e.g., `myservice`)
   - `{{PORT_NUMBER}}` → HTTP port number for your service (e.g., `5157`)
   - `{{HTTPS_PORT_NUMBER}}` → HTTPS port number for your service (e.g., `7283`)

## Placeholders to Replace

- `{{PROJECT_NAME}}` - The full name of your project/service
- `{{SERVICE_NAME}}` - Short identifier used in cache keys and configuration
- `{{PORT_NUMBER}}` - HTTP port number (typically 5xxx)
- `{{HTTPS_PORT_NUMBER}}` - HTTPS port number (typically 7xxx)

## Files Included

- `{{PROJECT_NAME}}.csproj` - Project file with Redis caching and OpenAPI packages
- `Program.cs` - ASP.NET Core web application with caching, POST endpoint, and error handling
- `appsettings.json` - Production configuration
- `appsettings.Development.json` - Development configuration
- `{{PROJECT_NAME}}.http` - HTTP request file for testing the /process endpoint
- `Properties/launchSettings.json` - Launch configuration with port settings

## Template Structure

The template follows common patterns from existing services:

1. **Caching Strategy**: SHA256 hash-based cache keys with 30-minute expiration
2. **Endpoint Pattern**: POST to `/process` with request/response DTOs
3. **Validation**: DataAnnotations on request models
4. **Error Handling**: `UseExceptionHandler()` with structured problem details
5. **Service Integration**: Redis cache, service defaults, and OpenAPI

## Example

If creating a service called `SignalBox.Service.EmailNotification`:

1. Copy template folder and rename to `SignalBox.Service.EmailNotification`
2. Replace:
   - `{{PROJECT_NAME}}` → `SignalBox.Service.EmailNotification`
   - `{{SERVICE_NAME}}` → `emailnotification`
   - `{{PORT_NUMBER}}` → `5157`
   - `{{HTTPS_PORT_NUMBER}}` → `7283`
3. The project file will be named `SignalBox.Service.EmailNotification.csproj`
4. The HTTP file will be named `SignalBox.Service.EmailNotification.http`

## Next Steps

After creating your service from the template:

1. Add your service to the solution file
2. Update the AppHost project to include your new service
3. Implement your specific business logic in `ProcessHandlerAsync`
4. Update the `ProcessRequest` and `ProcessResponse` records to match your needs
5. Customize endpoint name, summary, and description
6. Add any additional NuGet packages as needed
5. Update the weatherforecast endpoint with your actual service endpoints