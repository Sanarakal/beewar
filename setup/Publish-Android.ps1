param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version
)
$ErrorActionPreference = 'Stop'
$releaseRoot = Split-Path -Parent $PSScriptRoot
$releaseTag = "v$Version"
$releaseRepository = 'Sanarakal/beewar'
$releaseGhCommand = Get-Command gh -ErrorAction SilentlyContinue
$releaseGh = if ($releaseGhCommand) { $releaseGhCommand.Source } else { Join-Path $env:LOCALAPPDATA 'Programs\GameDev\GitHubCLI\bin\gh.exe' }
if (-not (Test-Path -LiteralPath $releaseGh)) { throw 'Install GitHub CLI and run gh auth login first.' }
$releaseApk = Join-Path $releaseRoot 'UnityProject\Builds\Android\PlagueGame-Kingdom.apk'
$releaseNotes = Join-Path $releaseRoot "release-notes\$releaseTag.md"
if (-not (Test-Path -LiteralPath $releaseApk)) { throw 'Build and test the Android APK first.' }
if (-not (Test-Path -LiteralPath $releaseNotes)) { throw "Missing release notes: $releaseNotes" }
$releaseAapt = Join-Path $env:LOCALAPPDATA 'Android\Sdk\build-tools\36.1.0\aapt2.exe'
if (-not (Test-Path -LiteralPath $releaseAapt)) { throw 'Android SDK build tools 36.1.0 are required to verify the APK version.' }
$releaseManifest = & $releaseAapt dump badging $releaseApk
if ($LASTEXITCODE -ne 0 -or -not ($releaseManifest | Select-String -SimpleMatch "versionName='$Version'")) { throw 'APK version does not match the requested release.' }
Push-Location $releaseRoot
try {
    $releaseDirty = git status --porcelain
    if ($LASTEXITCODE -ne 0 -or $releaseDirty) { throw 'Commit all source changes before publishing.' }
    $releaseHead = git rev-parse HEAD
    $releaseCommit = git rev-parse "$releaseTag^{commit}"
    if ($LASTEXITCODE -ne 0 -or $releaseHead -ne $releaseCommit) { throw 'The release tag must point to the current commit.' }
    $releaseExport = Join-Path $releaseRoot 'UnityProject\Builds\Android\Release'
    New-Item -ItemType Directory -Path $releaseExport -Force | Out-Null
    $releaseDownload = Join-Path $releaseExport 'BeeWar-Android.apk'
    Copy-Item -LiteralPath $releaseApk -Destination $releaseDownload -Force
    $releaseHash = (Get-FileHash -LiteralPath $releaseDownload -Algorithm SHA256).Hash.ToLowerInvariant()
    $releaseHashFile = Join-Path $releaseExport 'BeeWar-Android.sha256'
    Set-Content -LiteralPath $releaseHashFile -Value "$releaseHash  BeeWar-Android.apk" -Encoding ascii
    & $releaseGh release create $releaseTag $releaseDownload $releaseHashFile --repo $releaseRepository --verify-tag --title "BeeWar $Version — Янтарный улей" --notes-file $releaseNotes --latest
    if ($LASTEXITCODE -ne 0) { throw 'GitHub release upload failed.' }
    Write-Output 'https://github.com/Sanarakal/beewar/releases/latest/download/BeeWar-Android.apk'
}
finally { Pop-Location }
