# Shared helpers for Scan2BIM build and deploy scripts.

function Get-RevitVersions {
    param(
        [string]$VersionsFile = (Join-Path $PSScriptRoot "..\RevitVersions.txt")
    )

    if (-not (Test-Path $VersionsFile)) {
        throw "Missing RevitVersions.txt at $VersionsFile"
    }

    return Get-Content $VersionsFile |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ -and -not $_.StartsWith("#") }
}

function Get-MsBuildPath {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" |
            Select-Object -First 1
        if ($msbuild) { return $msbuild }
    }

    $fallbacks = @(
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
    )

    foreach ($path in $fallbacks) {
        if (Test-Path $path) { return $path }
    }

    throw "MSBuild not found. Install Visual Studio with the .NET desktop development workload."
}

function Get-RevitAddinsDir {
    param([string]$Version)

    return Join-Path $env:ProgramData "Autodesk\Revit\Addins\$Version"
}

function Test-RevitInstalled {
    param([string]$Version)

    $installDir = "C:\Program Files\Autodesk\Revit $Version"
    return Test-Path (Join-Path $installDir "Revit.exe")
}

function Assert-RevitNotRunning {
    $revit = Get-Process -Name "Revit" -ErrorAction SilentlyContinue
    if ($revit) {
        throw "Close Revit before building or deploying Scan2BIM."
    }
}

function Build-Scan2Bim {
    param(
        [Parameter(Mandatory = $true)][string]$Version,
        [string]$Configuration = "Release",
        [string]$SolutionPath = (Join-Path $PSScriptRoot "..\FloorToPointCloud.sln")
    )

    if (-not (Test-RevitInstalled -Version $Version)) {
        Write-Warning "Revit $Version is not installed. Skipping build for this version."
        return $false
    }

    $msbuild = Get-MsBuildPath
    Write-Host "Building Scan2BIM for Revit $Version..." -ForegroundColor Cyan

    & $msbuild /t:Rebuild /p:Configuration=$Configuration /p:RevitVersion=$Version $SolutionPath /verbosity:minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for Revit $Version."
    }

    return $true
}

function Deploy-Scan2Bim {
    param(
        [Parameter(Mandatory = $true)][string]$Version,
        [string]$ProjectRoot = (Join-Path $PSScriptRoot "..")
    )

    $sourceDll = Join-Path $ProjectRoot "bin\ReleaseBuild\$Version\Metrika.dll"
    $addinSrc = Join-Path $ProjectRoot "Metrika.$Version.addin"

    if (-not (Test-Path $sourceDll)) {
        throw "Compiled DLL not found for Revit $Version at $sourceDll"
    }
    if (-not (Test-Path $addinSrc)) {
        throw "Add-in manifest not found at $addinSrc"
    }

    $destDir = Get-RevitAddinsDir -Version $Version
    New-Item -ItemType Directory -Force -Path $destDir | Out-Null

    Copy-Item $sourceDll (Join-Path $destDir "Metrika.dll") -Force

    $disabledPath = Join-Path $destDir "Metrika.addin.disabled"
    if (Test-Path $disabledPath) { Remove-Item $disabledPath -Force }

    Copy-Item $addinSrc (Join-Path $destDir "Metrika.addin") -Force
    Write-Host "Deployed to $destDir" -ForegroundColor Green
}

function New-Scan2BimReleaseZip {
    param(
        [Parameter(Mandatory = $true)][string]$Version,
        [string]$ProjectRoot = (Join-Path $PSScriptRoot ".."),
        [string]$OutputDir = (Join-Path $PSScriptRoot "..\dist")
    )

    $sourceDll = Join-Path $ProjectRoot "bin\ReleaseBuild\$Version\Metrika.dll"
    $addinSrc = Join-Path $ProjectRoot "Metrika.$Version.addin"
    $installScript = Join-Path $PSScriptRoot "install.ps1"

    if (-not (Test-Path $sourceDll)) {
        throw "Compiled DLL not found for Revit $Version."
    }

    New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

    $staging = Join-Path $env:TEMP "Scan2BIM-$Version"
    if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $staging | Out-Null

    Copy-Item $sourceDll (Join-Path $staging "Metrika.dll")
    Copy-Item $addinSrc (Join-Path $staging "Metrika.addin")
    Copy-Item $installScript (Join-Path $staging "install.ps1")

    $zipPath = Join-Path $OutputDir "Scan2BIM-Revit$Version.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zipPath

    Remove-Item $staging -Recurse -Force
    return $zipPath
}
