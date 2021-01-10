$ErrorActionPreference = 'Stop'; # stop on all errors
$toolsDir              = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

$packageName= 'SyncTrayzor'
$file       = (Join-Path $toolsDir 'SyncTrayzorSetup-x86.exe')
$file64     = (Join-Path $toolsDir 'SyncTrayzorSetup-x64.exe')
$silentArgs = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /SkipDotNetInstall'
$fileType   = 'exe'
$validExitCodes = @(0)

Install-ChocolateyInstallPackage $packageName $fileType $silentArgs $file $file64 -validExitCodes $validExitCodes

Remove-Item -Force -ea 0 $file, $file64