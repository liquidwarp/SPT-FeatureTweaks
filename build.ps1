param(
    [string]$SPTPath = 'D:\SPT\4.1 Fika',

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$InstallMod,

    [switch]$InstallClientMod,

    [switch]$InstallServerMod,

    [switch]$PackageRelease,

    [switch]$Test
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath "$SPTPath\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll")) {
    throw "SPT not found at '$SPTPath'"
}
$SPTPath = (Resolve-Path -LiteralPath $SPTPath).Path

$buildArgs = @(
    "$PSScriptRoot\FeatureTweaks.slnx"
    "-c", $Configuration
    "-p:SPTPath=$SPTPath"
    "-p:InstallClientMod=$($InstallMod -or $InstallClientMod)"
    "-p:InstallServerMod=$($InstallMod -or $InstallServerMod)"
    "-p:PackageRelease=$($PackageRelease.IsPresent)"
    "-tlp:verbosity=detailed"
)
dotnet build @buildArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed ($LASTEXITCODE)" }

if ($Test) {
    $testArgs = @(
        "$PSScriptRoot\Tests\FeatureTweaks.Tests.slnx"
        "-c", $Configuration
        "-p:SPTPath=$SPTPath"
    )
    dotnet test @testArgs
    if ($LASTEXITCODE -ne 0) { throw "FeatureTweaks tests failed ($LASTEXITCODE)" }
}
