param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectName,
    
    [Parameter(Mandatory=$true)]
    [int]$HttpPort,
    
    [Parameter(Mandatory=$true)]
    [int]$HttpsPort,
    
    [string]$DestinationPath = "."
)

# Validate inputs
if ($ProjectName -notmatch "^SignalBox\.Service\..+") {
    Write-Error "Project name should follow the pattern 'SignalBox.Service.{Name}'"
    exit 1
}

if ($HttpPort -eq $HttpsPort) {
    Write-Error "HTTP and HTTPS ports must be different"
    exit 1
}

# Get template path (assuming script is run from the root of the signal-box project)
$TemplatePath = Join-Path $PSScriptRoot "templates\SignalBox.Service.Template"
$NewServicePath = Join-Path $DestinationPath $ProjectName

# Check if template exists
if (-not (Test-Path $TemplatePath)) {
    Write-Error "Template not found at: $TemplatePath"
    exit 1
}

# Check if destination already exists
if (Test-Path $NewServicePath) {
    Write-Error "Destination already exists: $NewServicePath"
    exit 1
}

Write-Host "Creating new service: $ProjectName" -ForegroundColor Green
Write-Host "HTTP Port: $HttpPort" -ForegroundColor Yellow
Write-Host "HTTPS Port: $HttpsPort" -ForegroundColor Yellow
Write-Host "Destination: $NewServicePath" -ForegroundColor Yellow

# Copy template to new location
Copy-Item -Path $TemplatePath -Destination $NewServicePath -Recurse

# Get all files that need placeholder replacement
$FilesToProcess = Get-ChildItem -Path $NewServicePath -Recurse -File

foreach ($File in $FilesToProcess) {
    # Skip README.md in the root of the new service
    if ($File.Name -eq "README.md") {
        Remove-Item $File.FullName
        continue
    }
    
    # Read file content
    $Content = Get-Content -Path $File.FullName -Raw
    
    # Replace placeholders
    $Content = $Content -replace '\{\{PROJECT_NAME\}\}', $ProjectName
    $Content = $Content -replace '\{\{PORT_NUMBER\}\}', $HttpPort
    $Content = $Content -replace '\{\{HTTPS_PORT_NUMBER\}\}', $HttpsPort
    
    # Write back to file
    Set-Content -Path $File.FullName -Value $Content -NoNewline
    
    # Rename files that contain {{PROJECT_NAME}} in the filename
    if ($File.Name -like "*{{PROJECT_NAME}}*") {
        $NewFileName = $File.Name -replace '\{\{PROJECT_NAME\}\}', $ProjectName
        $NewFilePath = Join-Path $File.Directory.FullName $NewFileName
        Rename-Item -Path $File.FullName -NewName $NewFileName
        Write-Host "Renamed: $($File.Name) -> $NewFileName" -ForegroundColor Cyan
    }
}

Write-Host "`nService created successfully!" -ForegroundColor Green
Write-Host "Location: $NewServicePath" -ForegroundColor Yellow
Write-Host "`nNext steps:" -ForegroundColor White
Write-Host "1. Add the project to your solution file" -ForegroundColor Gray
Write-Host "2. Update the AppHost project to include this service" -ForegroundColor Gray
Write-Host "3. Implement your business logic" -ForegroundColor Gray