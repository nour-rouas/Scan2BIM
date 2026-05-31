# Scan2BIM - Build release packages for all installed Revit versions.
# Output: dist/Scan2BIM-Revit20XX.zip

. "$PSScriptRoot\scripts\BuildHelpers.ps1"

Assert-RevitNotRunning

$versions = Get-RevitVersions
$packages = @()

foreach ($version in $versions) {
    if (-not (Build-Scan2Bim -Version $version)) {
        continue
    }

    $zip = New-Scan2BimReleaseZip -Version $version
    Write-Host "Created $zip" -ForegroundColor Green
    $packages += $zip
}

if ($packages.Count -eq 0) {
    Write-Host "No release packages were created." -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Release packages ready in dist\" -ForegroundColor Green
