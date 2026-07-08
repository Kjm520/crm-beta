<#
    One-time setup: creates a self-signed code-signing certificate for Haystack,
    exports it as a .pfx (used by build-msix.ps1 to sign), and installs it into
    the machine's "Trusted People" store so Windows will allow the MSIX to install.

    Run this ONCE (in an elevated / Administrator PowerShell for the trust step).

        pwsh ./packaging/new-cert.ps1

    The certificate Subject (CN=Haystack) MUST match the Publisher in AppxManifest.xml.
#>
$ErrorActionPreference = "Stop"
$subject     = "CN=Haystack"
# Store signing material OUTSIDE the repo. Default: ..\.secrets relative to repo root.
# Override with $env:HAYSTACK_CERT_DIR.
$repoRoot    = Split-Path $PSScriptRoot -Parent
$certDir     = if ($env:HAYSTACK_CERT_DIR) { $env:HAYSTACK_CERT_DIR } else { Join-Path (Split-Path $repoRoot -Parent) ".secrets" }
New-Item -ItemType Directory -Force -Path $certDir | Out-Null
$pfx         = Join-Path $certDir "haystack-signing.pfx"
$cer         = Join-Path $certDir "haystack-signing.cer"
$pfxPassword = ConvertTo-SecureString "haystack" -AsPlainText -Force

Write-Host "Creating self-signed code-signing certificate ($subject)..." -ForegroundColor Cyan
$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject $subject `
    -KeyUsage DigitalSignature `
    -FriendlyName "Haystack Self-Signed" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}") `
    -NotAfter (Get-Date).AddYears(5)

Export-PfxCertificate -Cert $cert -FilePath $pfx -Password $pfxPassword | Out-Null
Export-Certificate    -Cert $cert -FilePath $cer | Out-Null
Write-Host "Exported: $pfx" -ForegroundColor Green
Write-Host "Exported: $cer" -ForegroundColor Green

# Trust the cert so the MSIX will install. Requires admin.
Write-Host "Installing cert into LocalMachine\TrustedPeople (needs admin)..." -ForegroundColor Cyan
try {
    Import-Certificate -FilePath $cer -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" | Out-Null
    Write-Host "Certificate trusted. You can now install signed Haystack MSIX packages." -ForegroundColor Green
} catch {
    Write-Warning "Could not write to LocalMachine store (are you elevated?)."
    Write-Warning "Re-run this script in an Administrator PowerShell, or import $cer manually:"
    Write-Warning "  Import-Certificate -FilePath '$cer' -CertStoreLocation Cert:\LocalMachine\TrustedPeople"
}
