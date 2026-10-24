# Release and deployment

The player package is separate from the source and the development workspace.
Only `release/ACZeroPCremap-<version>-win-x64/` and its matching ZIP should be
distributed to players. `ACZeroPCremap.exe` is at the root of that folder/ZIP.

## Build again

On Windows with a .NET 10 SDK, run:

```powershell
powershell -File scripts/Build-Release.ps1
```

The script runs the Release build and both console check projects, then
publishes with `Properties/PublishProfiles/Portable-win-x64.pubxml`. It builds
the player ZIP, an independent source ZIP and SHA-256 checksums. It selects
the workspace SDK if present, otherwise `dotnet` from PATH. `-DotnetPath`
can select another SDK. `-OutputRoot` selects another destination. Existing
packages are never deleted or overwritten, so rebuilding the same version
requires a new output root. `-SkipChecks` is for local iteration; normal
release builds must run the checks. `-NoRestore` is only for already restored
dependencies, including the win-x64 runtime packs.

The source ZIP has an explicit allowlist. It excludes SDKs, NuGet caches,
build outputs, player packages, local settings and reference game screenshots.
The original INIs are included only as test fixtures in the source archive.
The player archive contains no INI, DLL/PDB sidecars, SDK or installer.

The runtime and libraries are bundled into the EXE. Standard .NET bundle
compression reduces download/disk size. WPF trimming is disabled to preserve
XAML, reflection and localization. Native dependencies, including WPF's
runtime components, are bundled for extraction to `%TEMP%\.net`. See
[Microsoft single-file deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/).
No WebView2, Node.js, Python or separate .NET installation is used.

The app requests `asInvoker`, so launching it does not request administrator
rights. Its language and window-placement preferences are `ACZeroPCremap.settings.json` beside the
EXE. Use a writable folder. If it is read-only, the app can still run and
switch language for that session, but cannot retain the preference. The game
settings folder must be writable to save game changes. Files disabled by
ACZeroRemap 0.5.x remain restorable. For a previous language preference,
manually copy `%LOCALAPPDATA%\ACZeroRemap\settings.json` beside the new EXE and
rename it to `ACZeroPCremap.settings.json` if desired; no migration is automatic.

## Verify before publishing

1. Run `scripts/Build-Release.ps1` without skipping checks.
2. Run `scripts/Test-Portable.ps1 -ExePath <release-folder>/ACZeroPCremap.exe`.
   This starts a copied EXE in a path with spaces and Unicode, hides the window,
   points DOTNET_ROOT at an empty folder and removes dotnet from PATH. It verifies
   the main window, self-contained host trace, bundled native WPF library and clean exit. It is a host smoke
   check and cannot substitute for an operating-system image without prerequisites.
3. On a clean Windows 11 x64 VM or Windows Sandbox, copy only the player ZIP,
   disconnect networking, extract it to a writable folder and launch the EXE
   as a standard user. Do not install .NET, Visual C++, WebView2 or development
   tools beforehand. Check first launch, all languages, open/edit/save/backup
   using a disposable INI, then move the folder and recheck the language preference.
4. Repeat on a clean Windows 10 x64 desktop if that compatibility is advertised.
   Windows 7/8.1 and 32-bit Windows are outside scope. Microsoft's current
   [.NET Windows support matrix](https://learn.microsoft.com/en-us/dotnet/core/install/windows)
   distinguishes supported Windows editions and lifecycles; do not equate a
   successful launch with Microsoft's support for an obsolete OS edition.
5. Verify physical keyboard capture and in-game keyboard/mouse changes on the intended game build.
6. Check small-screen layouts at 100%, 125% and 150% display scale, window restore, maximization and moving between monitors. Disconnect a secondary monitor and confirm that the saved window remains accessible.

The clean-VM step remains manual and unverified until its results are recorded.
Do not describe an environment-variable smoke check as a clean-install test.
The EXE is unsigned; a downloaded build can trigger Windows publisher/reputation
prompts. Code signing is a separate future release step.

## GitHub

Push the source tree (or extract the source ZIP into a repository), preserving
`.gitignore`. GitHub Actions builds/tests/packages on Windows and keeps the
archives as workflow artifacts. It does not publish a GitHub Release itself.
Create a GitHub Release for the version and attach the player ZIP, source ZIP
and `SHA256SUMS.txt`. Users should download the player ZIP, not GitHub's automatic
source download, which contains no executable. No repository or release is
created by the local packaging script.

The project uses MIT. Bundled library and runtime notices are copied alongside
the EXE, with runtime licenses under `licenses/`. The license does not grant
rights to game trademarks, assets or third-party components beyond their own terms.
