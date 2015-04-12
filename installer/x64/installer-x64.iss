#define AppExeName "SyncTrayzor.exe"
#define AppRoot "..\.."
#define Arch "x64"
#define AppSrc AppRoot + "\src\SyncTrayzor"
#define AppBin AppRoot +"\bin\" + Arch + "\Release"
#define AppExe AppBin + "\SyncTrayzor.exe"
#define AppName GetStringFileInfo(AppExe, "ProductName")
#define AppVersion GetFileVersion(AppExe)
#define AppPublisher "SyncTrayzor"
#define AppURL "https://github.com/canton7/SyncTrayzor"
#define AppDataFolder "SyncTrayzor"
#define RunRegKey "Software\Microsoft\Windows\CurrentVersion\Run"


[Setup]
AppId={{c004dcef-b848-46a5-9c30-4dbf736396fa}
AppName={#AppName}-{#Arch}
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
OutputBaseFilename={#AppName}Setup-{#Arch}
SetupIconFile={#AppSrc}\Icons\default.ico
Compression=lzma2/max
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
; Unintuitive - but we forcefully close SyncTrayzor ourselves (because the CEF subprocess doesn't exit when asked to)
; If we allow this, then the user gets a 'stopped programs?' prompt. If they hit 'no', then SyncTrayzor is still stopped, by us.
CloseApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Dirs]
Name: "{userappdata}\{#AppDataFolder}"

[Files]
Source: "{#AppBin}\*"; DestDir: "{app}"; Excludes: "*.xml,*.vshost.*,*.config,*.log,FluentValidation.resources.dll,System.Windows.Interactivity.resources.dll"; Flags: ignoreversion recursesubdirs
Source: "{#AppBin}\SyncTrayzor.exe.Installer.config"; DestDir: "{app}"; DestName: "SyncTrayzor.exe.config"; Flags: ignoreversion
Source: "{#AppSrc}\Icons\default.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppRoot}\*.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppRoot}\*.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "syncthing.exe"; DestDir: "{app}"
Source: "..\dotNet451Setup.exe"; DestDir: {tmp}; Flags: deleteafterinstall; Check: FrameworkIsNotInstalled

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{tmp}\dotNet451Setup.exe"; Parameters: "/passive /promptrestart"; Check: FrameworkIsNotInstalled; StatusMsg: "Microsoft .NET Framework 4.5.1 is being installed. Please wait..."
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall

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
var
  ResultCode: integer;
begin
  if CurStep = ssInstall then
  begin
    { This is really evil, but CefSharp.BrowserSubprocess.exe doesn't like to exit if we ask it nicely, so we have to kill it }
    ShellExec('open', 'taskkill.exe', '/f /t /im SyncTrayzor.exe', '', SW_HIDE, ewNoWait, ResultCode);
  end
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    { This is really evil, but CefSharp.BrowserSubprocess.exe doesn't like to exit if we ask it nicely, so we have to kill it }
    ShellExec('open', 'taskkill.exe', '/f /t /im SyncTrayzor.exe', '', SW_HIDE, ewNoWait, ResultCode);
  end
end;

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\{#AppDataFolder}"
Type: filesandordirs; Name: "{userappdata}\{#AppDataFolder}"