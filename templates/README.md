# SignalBox Templates

This directory contains templates for creating new SignalBox services.

## Available Templates

### SignalBox.Service.Template

A template for creating new ASP.NET Core web services that follow the SignalBox architecture pattern.

**Features:**
- ASP.NET Core 9.0 web application
- ServiceDefaults integration
- OpenAPI/Swagger support
- Standard logging configuration
- HTTP client testing file
- Proper launch settings

## Quick Start

### Using PowerShell Script (Recommended)

```powershell
.\New-SignalBoxService.ps1 -ProjectName "SignalBox.Service.YourServiceName" -HttpPort 5157 -HttpsPort 7283
```

### Using Batch File

```cmd
create-service.bat SignalBox.Service.YourServiceName 5157 7283
```

### Manual Process

1. Copy the `SignalBox.Service.Template` folder
2. Rename it to your desired service name
3. Replace all placeholders in the files:
   - `{{PROJECT_NAME}}` → Your project name
   - `{{PORT_NUMBER}}` → HTTP port number
   - `{{HTTPS_PORT_NUMBER}}` → HTTPS port number
4. Rename files that contain `{{PROJECT_NAME}}` in their filename

## Port Number Guidelines

Use the following port ranges for new services:

- **HTTP Ports**: 5150-5199 (next available: check existing services)
- **HTTPS Ports**: 7280-7299 (next available: check existing services)

## Existing Service Ports

Check existing projects for used ports, or ask Copilot to help.

## After Creating a Service

1. Add your new project to `SignalBox.slnx`
2. Update `SignalBox.AppHost/AppHost.cs` to include your service
3. Update any relevant documentation
4. Implement your specific business logic
5. Replace the sample WeatherForecast endpoint with your actual endpoints