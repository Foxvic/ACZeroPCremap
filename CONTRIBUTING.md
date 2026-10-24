# Contributing to ACZeroPCremap

The source remains a normal C#/.NET/WPF solution. No generated EXE or SDK is
needed in Git. Open `ACZeroPCremap.sln` with a .NET 10 SDK on Windows.

1. Run `dotnet restore ACZeroPCremap.sln` and build in Release.
2. Run both console check projects (commands below). `dotnet test` does not run them.
3. Keep unknown INI data and all unchanged bytes intact. Use temporary copies
   for persistence tests; never edit the root INI fixtures in tests.
4. Translate new UI text in all six resource sets. Keep technical identifiers
   and binding syntax independent of display language.
5. Check rendered previews for layout changes. Physical keyboard capture and
   in-game behavior need separate manual verification. Device sections in the
   INI are opaque data and must remain unchanged by keyboard/mouse edits.
6. Update the app version and each localized BuildLabel, then run
   `powershell -File scripts/Build-Release.ps1` to produce a new package.

Keep changes focused and explain what was tested. Do not commit `.tools`,
`bin`, `obj`, `artifacts`, `release`, user settings or backup files.

```powershell
dotnet build ACZeroPCremap.sln -c Release -m:1
dotnet run --project tests/ACZeroPCremap.Tests -c Release
dotnet run --project tests/ACZeroPCremap.UiChecks -c Release -- "C:\path\ACZeroPCremap"
```

Use the actual source directory for the final command. UI previews are written
to `artifacts/ui-checks`. See [release instructions](docs/RELEASE.md) for packaging
and [the reference guide](docs/USER-GUIDE.md) for behavior and limitations.

Mouse-disable comments from ACZeroRemap 0.5.x are part of the compatibility
surface: the renamed app must still be able to restore those original lines.

Contributions are distributed under the repository's MIT license. Game names,
game assets and third-party components retain their respective ownership.
