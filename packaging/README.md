# Haystack Packaging

Builds a signed **MSIX** installer for Haystack — a real Windows install (Start menu,
Add/Remove Programs, clean uninstall) that can be safely updated without touching user data.

## One-time setup

Run in an **Administrator** PowerShell (the trust step needs admin):

```powershell
pwsh ./packaging/new-cert.ps1
```

This creates a self-signed code-signing certificate (`CN=Haystack`), exports
`haystack-signing.pfx` (used to sign) and `haystack-signing.cer`, and installs the
cert into `LocalMachine\TrustedPeople` so Windows will accept the installer.

> **Signing material lives OUTSIDE the repo**, in `DEV/.secrets/` by default
> (`../.secrets/` relative to the repo root). Override the location with the
> `HAYSTACK_CERT_DIR` environment variable. The `.pfx` is your publisher identity —
> back it up somewhere safe (password manager / encrypted drive). If lost, you can
> regenerate with `new-cert.ps1`, but you'll need to re-trust the new cert.

## Build an installer

```powershell
pwsh ./packaging/build-msix.ps1                 # uses version in AppxManifest.xml
pwsh ./packaging/build-msix.ps1 -Version 0.2.0  # bump version for an update
```

Output: `packaging/output/Haystack_<version>.msix`. Double-click to install.

## Releasing an update

1. Make your code/schema changes. **Schema changes go in `Haystack/Data/Migrations.cs`**
   as a new numbered step — never edit an already-released migration. This is what keeps
   existing user data safe across updates.
2. **Bump the version** (`-Version` flag, or edit `AppxManifest.xml`). A higher version is
   what tells Windows to update the installed app.
3. Run `build-msix.ps1` and install the new `.msix` over the old one. User data in
   `%LocalAppData%\Haystack\haystack.db` is preserved and migrated automatically.

## Future: automatic updates

MSIX supports auto-update via an `.appinstaller` file hosted at a URL (or via the
Microsoft Store). Not wired up yet — add when there's somewhere to host the feed.

## Replacing placeholder art

`Images/` currently holds generated green "H" placeholders. Swap in real logos at the
same sizes (44x44, 150x150, 50x50 store, 310x150 wide) and rebuild.
