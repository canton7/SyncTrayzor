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

#If only the 32 bit version is installed, upgrade that instead of installing the 64 bit version
if ((Get-OSArchitectureWidth -compare 64) -and ($env:chocolateyForceX86 -ne $true)) {
    Write-Host -ForegroundColor green "Checking for 32 bit versions of SyncTrayzor"
    if (Get-UninstallRegistryKey -SoftwareName 'SyncTrayzor (x86)*') {
        if (Get-UninstallRegistryKey -SoftwareName 'SyncTrayzor (x64)*') {
            Write-Host -ForegroundColor green "Both 32 and 64 bit SyncTrayzor found, upgrading 64 bit"
        } else {
            Write-Host -ForegroundColor green "32 bit SyncTrayzor found, upgrading"
            #null out the 64 bit file parameter, so only the 32 bit file is available to install
            $packageArgs['file64'] = $null
        }
    }
}

Install-ChocolateyInstallPackage @packageArgs

Remove-Item -Force -ea 0 $file, $file64