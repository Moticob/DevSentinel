#Requires -Version 5.1
<#
.SYNOPSIS
    Builds DevSentinel as a single, portable, self-contained .exe for Windows x64.

.DESCRIPTION
    Runs `dotnet publish` with the settings already configured in DevSentinel.csproj
    (SelfContained + PublishSingleFile) so the result is one .exe that runs on a
    machine with no .NET runtime installed — nothing else to copy alongside it.

.EXAMPLE
    ./build.ps1
    ./build.ps1 -Configuration Debug
#>

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

Write-Host "Publishing DevSentinel ($Configuration, $Runtime)..." -ForegroundColor Cyan

dotnet publish DevSentinel.csproj `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o "publish"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

$exePath = Join-Path $root "publish\DevSentinel.exe"
if (Test-Path $exePath) {
    $size = (Get-Item $exePath).Length / 1MB
    Write-Host ""
    Write-Host "Build succeeded: $exePath ($([math]::Round($size, 1)) MB)" -ForegroundColor Green
    Write-Host "This single .exe is portable - copy it anywhere and run it, no install needed." -ForegroundColor Green
} else {
    Write-Host "Expected output not found at $exePath" -ForegroundColor Yellow
}
