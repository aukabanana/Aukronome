# Create Windows Desktop & Start Menu Shortcuts for Aukronome
# Works on ANY Windows PC where this repository is cloned!

$rootDir = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($rootDir)) {
    $rootDir = Get-Location
}

# 1. Locate or automatically build Aukronome.exe
$publishExe = Join-Path $rootDir "src\Aukronome.App\bin\Release\net8.0-windows\win-x64\publish\Aukronome.exe"
$debugExe = Join-Path $rootDir "src\Aukronome.App\bin\Debug\net8.0-windows\Aukronome.exe"

$targetExe = ""
if (Test-Path $publishExe) {
    $targetExe = $publishExe
} elseif (Test-Path $debugExe) {
    $targetExe = $debugExe
} else {
    Write-Host "Aukronome.exe not found yet. Building application..." -ForegroundColor Yellow
    & dotnet build (Join-Path $rootDir "src\Aukronome.App\Aukronome.App.csproj")
    if (Test-Path $debugExe) {
        $targetExe = $debugExe
    } else {
        Write-Error "Build failed. Please ensure .NET 8 SDK is installed on this machine."
        exit 1
    }
}

$iconPath = Join-Path $rootDir "src\Aukronome.App\Assets\Aukronome.ico"
$wsh = New-Object -ComObject WScript.Shell

# 2. Create Desktop Shortcut
$desktopPath = [Environment]::GetFolderPath('Desktop')
$desktopShortcutPath = Join-Path $desktopPath "Aukronome.lnk"
$shortcut = $wsh.CreateShortcut($desktopShortcutPath)
$shortcut.TargetPath = $targetExe
$shortcut.WorkingDirectory = Split-Path $targetExe
$shortcut.Description = "Aukronome - High Precision RawBlock Metronome"
if (Test-Path $iconPath) {
    $shortcut.IconLocation = "$iconPath,0"
}
$shortcut.Save()
Write-Host "Created Desktop Shortcut: $desktopShortcutPath" -ForegroundColor Green

# 3. Create Start Menu Shortcut (Makes Aukronome searchable in Windows Search)
$startMenuPath = [Environment]::GetFolderPath('Programs')
$startMenuShortcutPath = Join-Path $startMenuPath "Aukronome.lnk"
$startShortcut = $wsh.CreateShortcut($startMenuShortcutPath)
$startShortcut.TargetPath = $targetExe
$startShortcut.WorkingDirectory = Split-Path $targetExe
$startShortcut.Description = "Aukronome - High Precision RawBlock Metronome"
if (Test-Path $iconPath) {
    $startShortcut.IconLocation = "$iconPath,0"
}
$startShortcut.Save()
Write-Host "Created Start Menu Shortcut: $startMenuShortcutPath" -ForegroundColor Green
Write-Host "Aukronome is now searchable in Windows Start Menu! Press Windows Key and type 'Aukronome'." -ForegroundColor Cyan
