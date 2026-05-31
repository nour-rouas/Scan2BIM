# Scan2BIM installer
# Installs Metrika.dll and Metrika.addin into the Revit Addins folder.
#
# Usage:
#   .\install.ps1              # auto-detect installed Revit versions
#   .\install.ps1 -RevitVersion 2025

param(
    [string[]]$RevitVersion
)

$ErrorActionPreference = "Stop"

function Get-TargetVersions {
    param([string[]]$Requested)

    if ($Requested -and $Requested.Count -gt 0) {
        return $Requested
    }

    $detected = @()
    $addinsRoot = Join-Path $env:ProgramData "Autodesk\Revit\Addins"
    if (Test-Path $addinsRoot) {
        $detected = Get-ChildItem $addinsRoot -Directory |
            Where-Object { $_.Name -match '^\d{4}$' } |
            ForEach-Object { $_.Name } |
            Sort-Object
    }

    if ($detected.Count -eq 0) {
        throw "No Revit Addins folders found. Pass -RevitVersion 2024 (or 2025, 2026)."
    }

    return $detected
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$dll = Join-Path $scriptDir "Metrika.dll"
$addin = Join-Path $scriptDir "Metrika.addin"

if (-not (Test-Path $dll)) {
    throw "Metrika.dll not found next to this script."
}
if (-not (Test-Path $addin)) {
    throw "Metrika.addin not found next to this script."
}

$revit = Get-Process -Name "Revit" -ErrorAction SilentlyContinue
if ($revit) {
    throw "Close Revit before installing Scan2BIM."
}

$versions = Get-TargetVersions -Requested $RevitVersion

foreach ($version in $versions) {
    $destDir = Join-Path $env:ProgramData "Autodesk\Revit\Addins\$version"
    New-Item -ItemType Directory -Force -Path $destDir | Out-Null

    Copy-Item $dll (Join-Path $destDir "Metrika.dll") -Force
    Copy-Item $addin (Join-Path $destDir "Metrika.addin") -Force

    $disabled = Join-Path $destDir "Metrika.addin.disabled"
    if (Test-Path $disabled) { Remove-Item $disabled -Force }

    Write-Host "Scan2BIM installed for Revit $version" -ForegroundColor Green
}

Write-Host ""
Write-Host "Installation complete. Open Revit and look for the Scan2BIM ribbon tab." -ForegroundColor Green
