#define AppExeName "SyncTrayzor.exe"
#define AppSrc "..\src"
#define AppBin "..\bin\Release"
#define AppExe AppBin + "\SyncTrayzor.exe"
#define AppName GetStringFileInfo(AppExe, "ProductName")
#define AppVersion GetFileVersion(AppExe)
#define AppPublisher "SyncTrayzor"
#define AppURL "https://github.com/canton7/SyncTrayzor"
#define AppDataFolder "SyncTrayzor"


[Setup]
AppId={{0512E32C-CF12-4633-B836-DA29E0BAB2F5}
AppName={#AppName}
AppVersion={#AppVersion}
;AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={pf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
LicenseFile={#AppSrc}\LICENSE.txt
OutputBaseFilename={#AppName}Setup
SetupIconFile={#AppSrc}\icon.ico
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Dirs]
Name: "{userappdata}\{#AppDataFolder}"

[Files]
Source: "{#AppBin}\{#AppExeName}.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppBin}\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppBin}\*.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppBin}\*.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppBin}\*.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "syncthing.exe"; DestDir: "{userappdata}\{#AppDataFolder}\syncthing.exe"
Source: "dotNet451Setup.exe"; DestDir: {tmp}; Flags: deleteafterinstall; Check: FrameworkIsNotInstalled

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{tmp}\dotNet451Setup.exe"; Parameters: "/passive /promptrestart"; Check: FrameworkIsNotInstalled; StatusMsg: Microsoft .NET Framework 4.5.1 is being installed. Please wait...
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[code]
function FrameworkIsNotInstalled: Boolean;
var 
  exists: boolean;
  release: cardinal;
begin
  exists := RegQueryDWordValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', release);
  result := not exists or (release < 378758);
end;

[UninstallDelete]
Type: files; Name: "{userappdata}\{#AppDataFolder}\config.xml"
Type: dirifempty; Name: "{userappdata}\{#AppDataFolder}"