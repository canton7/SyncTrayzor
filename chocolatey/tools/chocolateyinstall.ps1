$ErrorActionPreference = 'Stop'; # stop on all errors

# Copied from https://github.com/chocolatey-community/chocolatey-coreteampackages/blob/a2371345f182da74589897055e3b3703deb0cce3/extensions/chocolatey-core.extension/extensions/Get-UninstallRegistryKey.ps1
function Get-UninstallRegistryKeySilent {
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $SoftwareName,
        [parameter(ValueFromRemainingArguments = $true)]
        [Object[]] $IgnoredArguments
    )
    Write-Debug "Running 'Get-UninstallRegistryKey' for `'$env:ChocolateyPackageName`' with SoftwareName:`'$SoftwareName`'";

    $ErrorActionPreference = 'Stop'
    $local_key       = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*'
    $machine_key     = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*'
    $machine_key6432 = 'HKLM:\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'

    Write-Verbose "Retrieving all uninstall registry keys"
    [array]$keys = Get-ChildItem -Path @($machine_key6432, $machine_key, $local_key) -ea 0
    Write-Debug "Registry uninstall keys on system: $($keys.Count)"

    Write-Debug "Error handling check: `'Get-ItemProperty`' fails if a registry key is encoded incorrectly."
    [int]$maxAttempts = $keys.Count
    for ([int]$attempt = 1; $attempt -le $maxAttempts; $attempt++)
    {
        $success = $false

        $keyPaths = $keys | Select-Object -ExpandProperty PSPath
        try {
            [array]$foundKey = Get-ItemProperty -Path $keyPaths -ea 0 | ? { $_.DisplayName -like $SoftwareName }
            $success = $true
        } catch {
            Write-Debug "Found bad key."
            foreach ($key in $keys){ try{ Get-ItemProperty $key.PsPath > $null } catch { $badKey = $key.PsPath }}
            Write-Verbose "Skipping bad key: $badKey"
            [array]$keys = $keys | ? { $badKey -NotContains $_.PsPath }
        }

        if ($success) { break; }
        if ($attempt -eq 10) {
            Write-Warning "Found more than 10 bad registry keys. Run command again with `'--verbose --debug`' for more info."
            Write-Debug "Each key searched should correspond to an installed program. It is very unlikely to have more than a few programs with incorrectly encoded keys, if any at all. This may be indicative of one or more corrupted registry branches."
        }
    }

    Write-Debug "Found $($foundKey.Count) uninstall registry key(s) with SoftwareName:`'$SoftwareName`'";
    return $foundKey
}

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
if ((Get-OSArchitectureWidth -compare 64) -and ($env:chocolateyForceX86 -ne $true) -and (Get-UninstallRegistryKeySilent -SoftwareName 'SyncTrayzor (x86)*')) {
    if (Get-UninstallRegistryKeySilent -SoftwareName 'SyncTrayzor (x64)*') {
        Write-Host -ForegroundColor green "Both the 32-bit and 64-bit versions of SyncTrayzor are installed. Upgrading the 64-bit version."
    } else {
        Write-Host -ForegroundColor green "You have the 32-bit version of SyncTrayzor installed, so upgrading this. If you intended to install the 64-bit version, please uninstall the 32-bit version first."
        # null out the 64 bit file parameter, so only the 32 bit file is available to install
        $packageArgs['file64'] = $null
    }
}

Install-ChocolateyInstallPackage @packageArgs

Remove-Item -Force -ea 0 $file, $file64
