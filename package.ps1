param(
    [string]$GameDir = $env:NOBODY_GAME_DIR,
    [string]$Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath($PSScriptRoot)
& (Join-Path $repoRoot 'build.ps1') -GameDir $GameDir -Configuration Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$distDir = Join-Path $repoRoot 'dist'
$stagingDir = Join-Path $distDir "NobodyOrderAutomation-$Version"
$stagingFull = [IO.Path]::GetFullPath($stagingDir)
if (-not $stagingFull.StartsWith($repoRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to stage outside the repository: $stagingFull"
}

if (Test-Path -LiteralPath $stagingFull) {
    Remove-Item -LiteralPath $stagingFull -Recurse -Force
}

$pluginDir = Join-Path $stagingFull 'BepInEx\plugins'
New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'src\NobodyOrderAutomation\bin\Release\net6.0\NobodyOrderAutomation.dll') -Destination $pluginDir
Copy-Item -LiteralPath (Join-Path $repoRoot 'README.md') -Destination $stagingFull
Copy-Item -LiteralPath (Join-Path $repoRoot 'CHANGELOG.md') -Destination $stagingFull
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination $stagingFull
Copy-Item -LiteralPath (Join-Path $repoRoot 'THIRD_PARTY_NOTICES.md') -Destination $stagingFull

$archive = Join-Path $distDir "NobodyOrderAutomation-$Version.zip"
if (Test-Path -LiteralPath $archive) {
    Remove-Item -LiteralPath $archive -Force
}
Compress-Archive -Path (Join-Path $stagingFull '*') -DestinationPath $archive -CompressionLevel Optimal
Write-Output $archive

