param([switch]$EnvironmentCheck, [switch]$Clinic)
$ErrorActionPreference = 'Stop'
$setupUnityExe = Join-Path $env:LOCALAPPDATA 'Programs\GameDev\Unity\6000.3.26f1\Editor\Unity.exe'
$setupProjectPath = Join-Path (Split-Path -Parent $PSScriptRoot) 'UnityProject'
if ($EnvironmentCheck -and $Clinic) { throw 'Choose either -EnvironmentCheck or -Clinic' }
$setupBuildLog = Join-Path $PSScriptRoot $(if ($EnvironmentCheck) { 'android-build.log' } elseif ($Clinic) { 'prototype-build.log' } else { 'kingdom-build.log' })
$setupMethod = if ($EnvironmentCheck) { 'PlagueGame.EditorTools.ProjectBootstrap.ConfigureAndBuild' } elseif ($Clinic) { 'PlagueGame.EditorTools.ProjectBootstrap.ConfigureAndBuildPrototype' } else { 'PlagueGame.EditorTools.ProjectBootstrap.ConfigureAndBuildKingdom' }
& $setupUnityExe -batchmode -nographics -projectPath $setupProjectPath -buildTarget Android -executeMethod $setupMethod -quit -logFile $setupBuildLog 2>&1 | Out-String | Write-Output
if ($LASTEXITCODE -ne 0) { throw "Unity exited with code $LASTEXITCODE. Log: $setupBuildLog" }
$setupApk = Join-Path $setupProjectPath $(if ($EnvironmentCheck) { 'Builds\Android\PlagueGame-EnvironmentCheck.apk' } elseif ($Clinic) { 'Builds\Android\PlagueGame-Clinic.apk' } else { 'Builds\Android\PlagueGame-Kingdom.apk' })
if (-not (Test-Path -LiteralPath $setupApk)) { throw 'Android APK was not created' }
Get-Item -LiteralPath $setupApk | Select-Object FullName,Length
