@echo off
if "%~1"=="" (
    echo Usage: create-service.bat ^<ProjectName^> ^<HttpPort^> ^<HttpsPort^>
    echo Example: create-service.bat SignalBox.Service.EmailNotification 5157 7283
    goto :eof
)

if "%~2"=="" (
    echo Usage: create-service.bat ^<ProjectName^> ^<HttpPort^> ^<HttpsPort^>
    echo Example: create-service.bat SignalBox.Service.EmailNotification 5157 7283
    goto :eof
)

if "%~3"=="" (
    echo Usage: create-service.bat ^<ProjectName^> ^<HttpPort^> ^<HttpsPort^>
    echo Example: create-service.bat SignalBox.Service.EmailNotification 5157 7283
    goto :eof
)

powershell.exe -ExecutionPolicy Bypass -File "%~dp0New-SignalBoxService.ps1" -ProjectName "%~1" -HttpPort %~2 -HttpsPort %~3