; Inno Setup Script for Aukronome
; High-Precision Brutalist RawBlock Metronome for Windows

#define MyAppName "Aukronome"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "aukabanana"
#define MyAppURL "https://github.com/aukabanana/Aukronome"
#define MyAppExeName "Aukronome.exe"

[Setup]
; Unique identifier for the application
AppId={{E68A8D2A-B956-4D2A-887B-A4B753A10A92}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=.\output
OutputBaseFilename=Aukronome-Setup-v1.0.1
SetupIconFile=..\src\Aukronome.App\Assets\Aukronome.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

; Allows standard user-level install without requiring admin privileges,
; or system-wide install if the user chooses.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayIcon={app}\{#MyAppExeName}
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\dist\win-x64\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\src\Aukronome.App\Assets\Aukronome.ico"; DestDir: "{app}\Assets"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\Aukronome.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\Aukronome.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
