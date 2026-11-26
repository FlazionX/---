@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET 8 SDK is required.
  echo Install it from https://dotnet.microsoft.com/download/dotnet/8.0
  pause
  exit /b 1
)
dotnet run --project "Sector\Sector.csproj"
if errorlevel 1 (
  echo.
  echo Build or launch failed. Check the error above.
  pause
  exit /b 1
)
endlocal
