#define AppExeName "SyncTrayzor.exe"
#define AppRoot ".."
#define AppSrc AppRoot + "\src\SyncTrayzor"
#define AppBin AppRoot +"\bin\x64\Release"
#define AppExe AppBin + "\SyncTrayzor.exe"
#define AppName GetStringFileInfo(AppExe, "ProductName")
#define AppVersion GetFileVersion(AppExe)
#define AppPublisher "SyncTrayzor"
#define AppURL "https://github.com/canton7/SyncTrayzor"
#define AppDataFolder "SyncTrayzor"
#define RunRegKey "Software\Microsoft\Windows\CurrentVersion\Run"


[Setup]
AppId={{c004dcef-b848-46a5-9c30-4dbf736396fa}
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
LicenseFile={#AppRoot}\LICENSE.txt
OutputDir="."
OutputBaseFilename={#AppName}Setup
SetupIconFile={#AppSrc}\Icons\default.ico
Compression=lzma2/max
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Dirs]
Name: "{userappdata}\{#AppDataFolder}"

[Files]
Source: "{#AppBin}\*.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppBin}\{#AppExeName}.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppBin}\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppBin}\*.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppBin}\*.pak"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppBin}\*.dat"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppBin}\locales\*"; DestDir: "{app}\locales"; Flags: ignoreversion
Source: "{#AppSrc}\Icons\default.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppRoot}\*.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppRoot}\*.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "syncthing.exe"; DestDir: "{userappdata}\{#AppDataFolder}"
Source: "dotNet451Setup.exe"; DestDir: {tmp}; Flags: deleteafterinstall; Check: FrameworkIsNotInstalled

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{tmp}\dotNet451Setup.exe"; Parameters: "/passive /promptrestart"; Check: FrameworkIsNotInstalled; StatusMsg: "Microsoft .NET Framework 4.5.1 is being installed. Please wait..."
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function FrameworkIsNotInstalled: Boolean;
var 
  exists: boolean;
  release: cardinal;
begin
  exists := RegQueryDWordValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', release);
  result := not exists or (release < 378758);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then begin
    { Since we're shifting from 32-bit to 64-bit, clean up the 32-bit installation dir }
    if DirExists(ExpandConstant('{pf32}\SyncTrayzor')) then
      DelTree(ExpandConstant('{pf32}\SyncTrayzor'), True, True, True);

    { If we mistakenly wrote to the admin user's registry before, undo that now }
    if RegValueExists(HKCU64, ExpandConstant('{#RunRegKey}'), 'SyncTrayzor') then
      RegDeleteValue(HKCU64, ExpandConstant('{#RunRegKey}'), 'SyncTrayzor');
  end;
end;

[UninstallDelete]
Type: files; Name: "{userappdata}\{#AppDataFolder}\config.xml"
Type: dirifempty; Name: "{userappdata}\{#AppDataFolder}"