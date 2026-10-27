@echo off
setlocal

set "PROJECT=%~dp0ElectromagneticPulseGenerator.csproj"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo ERROR: .NET SDK was not found. Install the .NET SDK and try again.
    exit /b 1
)

if not exist "%PROJECT%" (
    echo ERROR: Project file not found: "%PROJECT%"
    exit /b 1
)

echo Building Electromagnetic Pulse Scanner for RimWorld 1.7...
dotnet build "%PROJECT%" --configuration Release --no-restore
set "BUILD_EXIT_CODE=%ERRORLEVEL%"

if not "%BUILD_EXIT_CODE%"=="0" (
    echo.
    echo Build failed. If the DLL is in use, close RimWorld and run this script again.
    exit /b %BUILD_EXIT_CODE%
)

echo.
echo Build succeeded. DLL copied to:
echo "%~dp0..\..\1.7\Assemblies\ElectromagneticPulseGenerator.dll"
exit /b 0
