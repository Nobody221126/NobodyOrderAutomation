param(
    [string]$GameDir = $env:NOBODY_GAME_DIR,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$testProject = Join-Path $repoRoot 'tests\NobodyOrderAutomation.Tests\NobodyOrderAutomation.Tests.csproj'
$pluginProject = Join-Path $repoRoot 'src\NobodyOrderAutomation\NobodyOrderAutomation.csproj'

if ([string]::IsNullOrWhiteSpace($GameDir)) {
    throw 'Set NOBODY_GAME_DIR or pass -GameDir with the Nobody - The Turnaround installation directory.'
}

$gameExe = Join-Path $GameDir 'Nobody.exe'
$interop = Join-Path $GameDir 'BepInEx\interop\Assembly-CSharp.dll'
if (-not (Test-Path -LiteralPath $gameExe)) {
    throw "Nobody.exe was not found: $gameExe"
}
if (-not (Test-Path -LiteralPath $interop)) {
    throw "BepInEx IL2CPP interop assemblies were not found: $interop"
}

dotnet run --project $testProject -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet build $pluginProject -c $Configuration -p:GameDir="$GameDir"
exit $LASTEXITCODE

