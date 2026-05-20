# Prefer .NET 8 user install first so `dotnet` is not stuck on SDK 6 from Program Files (NETSDK1045).
$ErrorActionPreference = "Stop"

$userDotnetRoot = Join-Path $env:LOCALAPPDATA "Microsoft\dotnet"
if (Test-Path (Join-Path $userDotnetRoot "dotnet.exe")) {
    $env:PATH = "$userDotnetRoot;$env:PATH"
}

$sln = Join-Path $PSScriptRoot "apps\messaging-microservice\messaging-microservice.sln"
if (-not (Test-Path $sln)) {
    Write-Error "Solution not found: $sln"
    exit 1
}

Write-Host "dotnet SDK:" (dotnet --version)
dotnet build $sln -c Release @args
