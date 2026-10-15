$ErrorActionPreference = 'Stop'
$setupInstallLog = 'C:\Users\dlepe\Documents\PlagueGame\setup\unity-install.log'
$setupUnityDestination = 'C:\Users\dlepe\AppData\Local\Programs\GameDev\Unity\6000.3.26f1'
$setupUnityInstallers = @(
    'C:\Users\dlepe\AppData\Local\PlagueGameSetup\downloads\UnitySetup64-6000.3.26f1.exe',
    'C:\Users\dlepe\AppData\Local\PlagueGameSetup\downloads\UnitySetup-Android-Support-for-Editor-6000.3.26f1.exe'
)
try {
    foreach ($setupUnityInstaller in $setupUnityInstallers) {
        $setupSignature = Get-AuthenticodeSignature -LiteralPath $setupUnityInstaller
        if ($setupSignature.Status -ne 'Valid' -or $setupSignature.SignerCertificate.Subject -notmatch 'Unity Technologies') {
            throw "Installer signature verification failed: $setupUnityInstaller"
        }
    }
    'START ' + (Get-Date -Format o) | Set-Content -LiteralPath $setupInstallLog -Encoding utf8
    foreach ($setupUnityInstaller in $setupUnityInstallers) {
        'INSTALL ' + [IO.Path]::GetFileName($setupUnityInstaller) | Add-Content -LiteralPath $setupInstallLog
        $setupInstallerProcess = Start-Process -FilePath $setupUnityInstaller -ArgumentList @('/S', ('/D=' + $setupUnityDestination)) -WindowStyle Hidden -PassThru -Wait
        'EXIT ' + $setupInstallerProcess.ExitCode | Add-Content -LiteralPath $setupInstallLog
        if ($setupInstallerProcess.ExitCode -ne 0) { throw "Installer failed: $($setupInstallerProcess.ExitCode)" }
    }
    if (-not (Test-Path -LiteralPath (Join-Path $setupUnityDestination 'Editor\Unity.exe'))) { throw 'Unity.exe was not installed' }
    'SUCCESS' | Add-Content -LiteralPath $setupInstallLog
} catch {
    ('ERROR ' + $_.Exception.Message) | Add-Content -LiteralPath $setupInstallLog
    exit 1
}
