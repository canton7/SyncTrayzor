$ErrorActionPreference = 'Stop'; # stop on all errors

$packageName= 'SyncTrayzor'
$url        = "https://github.com/canton7/SyncTrayzor/releases/download/v${env:chocolateyPackageVersion}/SyncTrayzorSetup-x86.exe"
$url64      = "https://github.com/canton7/SyncTrayzor/releases/download/v${env:chocolateyPackageVersion}/SyncTrayzorSetup-x64.exe"
$silentArgs = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-'
$fileType   = 'exe'
$validExitCodes = @(0)

Install-ChocolateyPackage $packageName $fileType $silentArgs $url $url64 -validExitCodes $validExitCodes
