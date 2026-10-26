; Chord Search — Inno Setup installer
; Build: ISCC.exe packaging\ChordSearch.iss
; Expects published files in artifacts\publish-win-x64\

#define MyAppName "Chord Search"
#define MyAppVersion "1.0.2"
#define MyAppPublisher "Chord Search"
#define MyAppExeName "ChordSearch.App.exe"
#define MyAppURL "https://github.com/dominikrose887/chordSearch"
#define PublishDir "..\artifacts\publish-win-x64"

[Setup]
AppId={{A7C3E9F1-4B2D-4E8A-9C1F-6D5A8B0E2F34}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; User can pick any folder; elevate only when installing under Program Files
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=..\artifacts
OutputBaseFilename=ChordSearch-Setup-{#MyAppVersion}
SetupIconFile=..\ChordSearch.App\Assets\AppIcon.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=no
InfoBeforeFile=INSTALL-WIZARD.txt
LicenseFile=
CloseApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Leave user settings alone under %LocalAppData%\ChordSearch
Type: files; Name: "{app}\*.log"
