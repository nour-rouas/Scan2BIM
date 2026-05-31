# Scan2BIM - Build and deploy for all supported Revit versions.
# Usage: close Revit, then run: .\deploy.ps1

. "$PSScriptRoot\scripts\BuildHelpers.ps1"

Assert-RevitNotRunning

$versions = Get-RevitVersions
$built = @()

foreach ($version in $versions) {
    if (Build-Scan2Bim -Version $version) {
        Deploy-Scan2Bim -Version $version
        $built += $version
    }
}

if ($built.Count -eq 0) {
    Write-Host "No Revit versions were built. Install at least one supported Revit version." -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Deploy complete for Revit $($built -join ', '). Open Revit to use Scan2BIM." -ForegroundColor Green
