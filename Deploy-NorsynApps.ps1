# Deploys NorsynApps (Release) into the Revit Addins folder using the same
# layout convention as DevReload's Deploy-RevitAddins.ps1:
#   %APPDATA%\Autodesk\Revit\Addins\<year>\NorsynApps.addin   (manifest)
#   %APPDATA%\Autodesk\Revit\Addins\<year>\NorsynApps\*       (payload)
param(
    [int[]]$Years = @(2022, 2024, 2025),
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot

foreach ($year in $Years) {
    $project = Join-Path $repo "NorsynApps-$year\NorsynApps-$year.csproj"
    if (-not (Test-Path $project)) {
        Write-Warning "NorsynApps-$year not found, skipping."
        continue
    }

    Write-Host "Building NorsynApps-$year ($Configuration)..."
    dotnet build $project -c $Configuration -p:Platform=x64 --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "Build failed for NorsynApps-$year" }

    $binDir = Join-Path $repo "NorsynApps-$year\bin\$Configuration"
    $addinsRoot = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$year"
    $payloadDir = Join-Path $addinsRoot 'NorsynApps'

    New-Item -ItemType Directory -Force $payloadDir | Out-Null
    Copy-Item (Join-Path $binDir '*') $payloadDir -Recurse -Force
    # Manifest goes to the Addins ROOT (Revit only scans there); its
    # relative <Assembly> path points into the NorsynApps subfolder.
    Copy-Item (Join-Path $repo 'NorsynApps-SHARED\NorsynApps.addin') $addinsRoot -Force
    # Don't leave a second manifest inside the payload folder.
    Remove-Item (Join-Path $payloadDir 'NorsynApps.addin') -ErrorAction SilentlyContinue

    Write-Host "Deployed NorsynApps for Revit $year -> $addinsRoot"
}
