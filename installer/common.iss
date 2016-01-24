#define AppExeName "SyncTrayzor.exe"
#define AppRoot "..\.."
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
AppId={{#AppId}
AppName={#AppName} ({#Arch})
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
;Compression=lzma2/max
Compression=None
SolidCompression=yes
PrivilegesRequired=admin
CloseApplications=yes
RestartApplications=no
; If we try and close CefSharp.BrowserSubprocess.exe we'll fail - it doesn't respond well
; However if we close *just* SyncTrayzor, that will take care of shutting down CefSharp and syncthing
CloseApplicationsFilter=SyncTrayzor.exe
TouchDate=current
#if "x64" == Arch
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
#endif

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Dirs]
Name: "{userappdata}\{#AppDataFolder}"

[Files]
Source: "{#AppBin}\*"; DestDir: "{app}"; Excludes: "*.xml,*.vshost.*,*.config,*.log,FluentValidation.resources.dll,System.Windows.Interactivity.resources.dll,syncthing.exe,data,logs,ffmpegsumo.dll,d3dcompiler_43.dll,d3dcompiler_47.dll,libEGL.dll,libGLESv2.dll,pdf.dll"; Flags: ignoreversion recursesubdirs
Source: "{#AppBin}\SyncTrayzor.exe.Installer.config"; DestDir: "{app}"; DestName: "SyncTrayzor.exe.config"; Flags: ignoreversion
Source: "{#AppSrc}\Icons\default.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppRoot}\*.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppRoot}\*.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "syncthing.exe"; DestDir: "{app}"; DestName: "syncthing.exe"; Flags: ignoreversion

Source: "..\dotNet451Setup.exe"; DestDir: {tmp}; Flags: deleteafterinstall; Check: FrameworkIsNotInstalled

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

procedure BumpInstallCount;
var
  fileContents: AnsiString;
  installCount: integer;
begin
  { Increment the install count in InstallCount.txt if it exists, or create it with the contents '1' if it doesn't }
  if LoadStringFromFile(ExpandConstant('{app}\InstallCount.txt'), fileContents) then
  begin
    installCount := StrTointDef(Trim(string(fileContents)), 0) + 1;
  end
  else
  begin
    installCount := 1;
  end;

  SaveStringToFile(ExpandConstant('{app}\InstallCount.txt'), IntToStr(installCount), False);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  FindRec: TFindRec;
  FolderPath: String;
  FilePath: String;
begin
  if CurStep = ssInstall then
  begin
    BumpInstallCount();

    { We might be being run from ProcessRunner.exe, *and* we might be trying to update it. Funsies. Let's rename it (which Windows lets us do) }
    DeleteFile(ExpandConstant('{app}\ProcessRunner.exe.old'));
    RenameFile(ExpandConstant('{app}\ProcessRunner.exe'), ExpandConstant('{app}\ProcessRunner.exe.old'));

    Log(ExpandConstant('Looking for resource files in {app}\*'));
    { Remove resource files. This means that out-of-date languages will be removed, which (as a last-ditch resore) will alert maintainers that something's wrong }
    if FindFirst(ExpandConstant('{app}\*'), FindRec) then
    begin
      try
        repeat
          if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0) and (FindRec.Name <> '.') and (FindRec.Name <> '..') then
          begin
            FolderPath :=  ExpandConstant('{app}\') + FindRec.Name;
            FilePath := FolderPath + '\SyncTrayzor.resources.dll';
            if DeleteFile(FilePath) then
            begin
              Log('Deleted ' + FilePath);
              if DelTree(FolderPath, True, False, False) then
                Log('Deleted ' + FolderPath);
            end;
          end;
        until not FindNext(FindRec);
      finally
        FindClose(FindRec);
      end;
    end;
  end
end;

[UninstallDelete]
Type: files; Name: "{app}\ProcessRunner.exe.old"
Type: files; Name: "{app}\InstallCount.txt"
Type: filesandordirs; Name: "{userappdata}\{#AppDataFolder}"
Type: filesandordirs; Name: "{localappdata}\{#AppDataFolder}"