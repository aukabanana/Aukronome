# Create Windows Desktop & Start Menu Shortcuts for Aukronome
$targetExe = "C:\Users\ACER\.gemini\antigravity\scratch\Aukronome\src\Aukronome.App\bin\Release\net8.0-windows\win-x64\publish\Aukronome.exe"
if (-not (Test-Path $targetExe)) {
    # Fallback to Debug exe if single-file publish is not yet built
    $targetExe = "C:\Users\ACER\.gemini\antigravity\scratch\Aukronome\src\Aukronome.App\bin\Debug\net8.0-windows\Aukronome.exe"
}

$iconPath = "C:\Users\ACER\.gemini\antigravity\scratch\Aukronome\src\Aukronome.App\Assets\Aukronome.ico"

$wsh = New-Object -ComObject WScript.Shell

# 1. Desktop Shortcut
$desktopPath = [Environment]::GetFolderPath('Desktop')
$desktopShortcutPath = Join-Path $desktopPath "Aukronome.lnk"
$shortcut = $wsh.CreateShortcut($desktopShortcutPath)
$shortcut.TargetPath = $targetExe
$shortcut.WorkingDirectory = Split-Path $targetExe
$shortcut.Description = "Aukronome - High Precision RawBlock Metronome"
$shortcut.IconLocation = "$iconPath,0"
$shortcut.Save()
Write-Output "Created Desktop Shortcut: $desktopShortcutPath"

# 2. Start Menu Shortcut
$startMenuPath = [Environment]::GetFolderPath('Programs')
$startMenuShortcutPath = Join-Path $startMenuPath "Aukronome.lnk"
$startShortcut = $wsh.CreateShortcut($startMenuShortcutPath)
$startShortcut.TargetPath = $targetExe
$startShortcut.WorkingDirectory = Split-Path $targetExe
$startShortcut.Description = "Aukronome - High Precision RawBlock Metronome"
$startShortcut.IconLocation = "$iconPath,0"
$startShortcut.Save()
Write-Output "Created Start Menu Shortcut: $startMenuShortcutPath"
