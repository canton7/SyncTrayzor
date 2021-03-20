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

# Version 1.1.28 had a bug which installed x86 on x64 machines. Forcefully transitioning someone from
# x86 to x64 is risky, as it potentially loses their config, Syncthing version, etc.
# Therefore if they've got the x86 version and not the x64 version, upgrade using the x86 version to
# avoid breaking them.
#If only the 32 bit version is installed, upgrade that instead of installing the 64 bit version
if ((Get-OSArchitectureWidth -compare 64) -and ($env:chocolateyForceX86 -ne $true) -and (Get-UninstallRegistryKey -SoftwareName 'SyncTrayzor (x86)*')) {
    if (Get-UninstallRegistryKey -SoftwareName 'SyncTrayzor (x64)*') {
        Write-Host -ForegroundColor green "Both the x86 and x64 versions of SyncTrayzor are installed. Upgrading the x64 version."
    } else {
        Write-Host -ForegroundColor green "You have the x86 version of SyncTrayzor installed, so upgrading this. If you intended to install the x64 version, please uninstall the x86 version first."
        # null out the 64 bit file parameter, so only the 32 bit file is available to install
        $packageArgs['file64'] = $null
    }
}

Install-ChocolateyInstallPackage @packageArgs

Remove-Item -Force -ea 0 $file, $file64
