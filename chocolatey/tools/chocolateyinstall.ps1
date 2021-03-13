$ErrorActionPreference = 'Stop'; # stop on all errors
$toolsDir              = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$file                  = (Join-Path $toolsDir 'SyncTrayzorSetup-x86.exe')
$file64                = (Join-Path $toolsDir 'SyncTrayzorSetup-x64.exe')

$packageArgs = @{
    packageName   = 'SyncTrayzor'
    file          = $file
    file64        = $file64
    silentArgs    = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /SkipDotNetInstall'
    fileType      = 'exe'
    validExitCodes = @(0)
}

Install-ChocolateyInstallPackage @packageArgs

Remove-Item -Force -ea 0 $file, $file64