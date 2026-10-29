#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{9E98542C-6057-49C3-BE49-0F48377F941A}
AppName=Nekodoro
AppVersion={#AppVersion}
DefaultDirName={localappdata}\Programs\Nekodoro
DefaultGroupName=Nekodoro
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts
OutputBaseFilename=Nekodoro-Setup-{#AppVersion}-win-x64
SetupIconFile=..\src\PomodoroTaskbar.Windows\Assets\App.ico
UninstallDisplayIcon={app}\Nekodoro.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "..\artifacts\nekodoro-portable\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Nekodoro"; Filename: "{app}\Nekodoro.exe"
Name: "{autodesktop}\Nekodoro"; Filename: "{app}\Nekodoro.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Nekodoro.exe"; Description: "Launch Nekodoro"; Flags: nowait postinstall skipifsilent
