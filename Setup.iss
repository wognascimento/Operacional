#ifndef MyAppVersion
  #define MyAppVersion "1.0.0.0"
#endif

#define MyAppName "Operacional S.I.G."
#define MyAppPublisher "Cipolatti, Inc."
#define MyAppURL "https://www.cipolatti.com.br"
#define MyAppExeName "Operacional.exe"
#define DotNetRuntimeInstaller "redist\windowsdesktop-runtime-10.0-win-x64.exe"

[Setup]
AppId={{54439EDB-1877-4689-B1F1-127E18CC5D88}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName=C:\SIG\{#MyAppName}
DisableDirPage=yes
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
PrivilegesRequired=lowest
OutputDir=artifacts\installer
OutputBaseFilename=OperacionalSetup-{#MyAppVersion}
SetupIconFile=Operacional\icones\logo.ico
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
#ifexist DotNetRuntimeInstaller
Source: "{#DotNetRuntimeInstaller}"; DestDir: "{tmp}"; Flags: deleteafterinstall
#endif

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
#ifexist DotNetRuntimeInstaller
Filename: "{tmp}\windowsdesktop-runtime-10.0-win-x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Instalando .NET Desktop Runtime 10..."; Check: not IsDotNetDesktopRuntime10Installed
#endif
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNetDesktopRuntime10Installed: Boolean;
begin
  Result := RegKeyExists(HKLM64, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App\10.0');
end;
