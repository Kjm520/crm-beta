<#
    Builds a signed MSIX installer for Haystack.

    Usage:
        pwsh ./packaging/build-msix.ps1                # uses version from AppxManifest.xml
        pwsh ./packaging/build-msix.ps1 -Version 0.2.0 # override version (bump for updates!)

    Prerequisites (one-time): run packaging/new-cert.ps1 to create & trust the signing cert.

    Output: packaging/output/Haystack_<version>.msix
#>
param(
    [string]$Version,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root       = Split-Path $PSScriptRoot -Parent
$proj       = Join-Path $root "Haystack\Haystack.csproj"
$pkgDir     = $PSScriptRoot
$stageDir   = Join-Path $pkgDir "stage"
$outDir     = Join-Path $pkgDir "output"
$manifest   = Join-Path $pkgDir "AppxManifest.xml"
$imagesDir  = Join-Path $pkgDir "Images"
# Signing cert lives outside the repo (see packaging/new-cert.ps1). Override with $env:HAYSTACK_CERT_DIR.
$certDir    = if ($env:HAYSTACK_CERT_DIR) { $env:HAYSTACK_CERT_DIR } else { Join-Path (Split-Path $root -Parent) ".secrets" }
$pfx        = Join-Path $certDir "haystack-signing.pfx"
$pfxPassword = "haystack"   # local self-signed cert only; not a secret

# --- Locate SDK tools (makeappx, signtool) ---
$sdkBin = Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin" -Directory |
    Where-Object { Test-Path (Join-Path $_.FullName "x64\makeappx.exe") } |
    Sort-Object Name -Descending | Select-Object -First 1
if (-not $sdkBin) { throw "Windows SDK (makeappx/signtool) not found." }
$makeappx = Join-Path $sdkBin.FullName "x64\makeappx.exe"
$signtool = Join-Path $sdkBin.FullName "x64\signtool.exe"

# --- Resolve version: CLI arg wins, else read from manifest ---
[xml]$manifestXml = Get-Content $manifest
if ($Version) {
    # Normalize to 4-part version (MSIX requires MAJOR.MINOR.BUILD.REVISION)
    $parts = $Version.Split('.')
    while ($parts.Count -lt 4) { $parts += "0" }
    $fullVersion = ($parts[0..3] -join '.')
    $manifestXml.Package.Identity.Version = $fullVersion
    $manifestXml.Save($manifest)
} else {
    $fullVersion = $manifestXml.Package.Identity.Version
}
$shortVersion = ($fullVersion.Split('.')[0..2] -join '.')
Write-Host "Building Haystack MSIX v$fullVersion" -ForegroundColor Cyan

# --- Clean stage ---
if (Test-Path $stageDir) { Remove-Item $stageDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $stageDir, $outDir | Out-Null

# --- Publish self-contained (no .NET runtime needed on target) ---
Write-Host "Publishing..." -ForegroundColor Cyan
dotnet publish $proj -c $Configuration -r win-x64 --self-contained true `
    -p:PublishSingleFile=false -o $stageDir --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

# --- Copy manifest + images into the stage root ---
Copy-Item $manifest (Join-Path $stageDir "AppxManifest.xml") -Force
Copy-Item $imagesDir (Join-Path $stageDir "Images") -Recurse -Force

# --- Pack ---
$msix = Join-Path $outDir "Haystack_$shortVersion.msix"
if (Test-Path $msix) { Remove-Item $msix -Force }
Write-Host "Packing..." -ForegroundColor Cyan
& $makeappx pack /d $stageDir /p $msix /o
if ($LASTEXITCODE -ne 0) { throw "makeappx pack failed." }

# --- Sign ---
if (-not (Test-Path $pfx)) {
    throw "Signing cert not found at $pfx. Run packaging/new-cert.ps1 first (it writes to $certDir)."
}
Write-Host "Signing..." -ForegroundColor Cyan
& $signtool sign /fd SHA256 /a /f $pfx /p $pfxPassword $msix
if ($LASTEXITCODE -ne 0) { throw "signtool sign failed." }

Write-Host ""
Write-Host "Done: $msix" -ForegroundColor Green
Write-Host "Double-click it to install (cert must be trusted first - see new-cert.ps1)." -ForegroundColor Green
