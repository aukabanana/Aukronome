@echo off
setlocal
title Aukronome - Build Setup Installer

echo ===================================================
echo   Aukronome - High-Precision Metronome Setup Build
echo ===================================================
echo.

:: 1. Locate dotnet
where dotnet >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    set "DOTNET_CMD=dotnet"
) else if exist "%USERPROFILE%\.dotnet\dotnet.exe" (
    set "DOTNET_CMD=%USERPROFILE%\.dotnet\dotnet.exe"
) else if exist "%ProgramFiles%\dotnet\dotnet.exe" (
    set "DOTNET_CMD=%ProgramFiles%\dotnet\dotnet.exe"
) else (
    echo [ERROR] .NET SDK dotnet.exe was not found.
    echo Please install .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
    goto :FAIL
)

:: 2. Locate ISCC (Inno Setup)
where iscc >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    set "ISCC_CMD=iscc"
) else if exist "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" (
    set "ISCC_CMD=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
) else if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" (
    set "ISCC_CMD=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
) else if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" (
    set "ISCC_CMD=%ProgramFiles%\Inno Setup 6\ISCC.exe"
) else (
    echo [ERROR] Inno Setup 6 compiler ISCC.exe was not found.
    echo Please install Inno Setup: https://jrsoftware.org/isinfo.php
    echo Or install via winget: winget install JRSoftware.InnoSetup
    goto :FAIL
)

echo [1/2] Publishing standalone single-file binary (win-x64)...
"%DOTNET_CMD%" publish "%~dp0src\Aukronome.App\Aukronome.App.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o "%~dp0dist\win-x64"
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Failed to publish application.
    goto :FAIL
)

echo.
echo [2/2] Compiling Windows Setup Installer with Inno Setup...
"%ISCC_CMD%" "%~dp0installer\Aukronome.iss"
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Failed to compile installer.
    goto :FAIL
)

echo.
echo ===================================================
echo   SUCCESS! Installer ready:
echo   %~dp0installer\output\Aukronome-Setup-v1.0.0.exe
echo ===================================================
echo.
if not defined CI if not "%1"=="--no-pause" pause
exit /b 0

:FAIL
if not defined CI if not "%1"=="--no-pause" pause
exit /b 1
