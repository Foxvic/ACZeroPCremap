# Usage and technical reference

A local keyboard and mouse binding editor for `dinput.ini`. Built with C#, .NET 10 and WPF for Windows 10/11 x64.

Version 0.8.1 adds a winged-key icon and screen-aware window sizing, with saved placement and a compact layout. Keyboard, mouse buttons, wheel and recognized mouse axes are editable. Existing device sections in the INI are preserved without changes. The portable Windows x64 EXE includes .NET, needs no installer and supports six languages.

The theme is drawn with scalable WPF geometry; screenshots are visual references and are not used as background images. Main-window controls, assignment dialogs, confirmations, dropdowns and tooltips share the theme. Standard Windows file pickers retain their native appearance.

Windows fit the current monitor's working area and display scaling. In a small main window, Open and Save stay visible while Discard, Reload and Restore move into the **⋯** menu. Decorative header content is hidden to leave room for the scrollable action list. The main window remembers its normal size, position and maximized state, and returns to an available screen if a saved monitor is disconnected. Closing while minimized retains its previous normal/maximized state.

## Run

Download the **win-x64 player ZIP** from the project's GitHub Release, extract it to a writable folder and launch **ACZeroPCremap.exe** at its root. The locally built folder is `release/ACZeroPCremap-0.8.1-win-x64/`. GitHub's automatic source archive contains no executable.

A separate .NET installation, installer or administrator rights are not required for launch. The bundled runtime extracts native support files into `%TEMP%\.net`; first launch can take longer. Runtime operation is offline. Use Windows 10/11 x64 desktop; Windows 7/8.1 and 32-bit Windows are outside scope. Verification on a clean Windows VM is still pending; see [deployment checks](RELEASE.md).

1. Start the EXE and open `dinput.ini` from `ACE COMBAT ZERO THE BELKAN WAR (Steam Version)\ACZero\settings` inside your game installation. A file next to the EXE opens automatically.
2. Select the **Keyboard / mouse** tab for flight or menus.
3. Find an action using search or the group filter. Click a binding to edit it, use × to remove it, or click + Binding to add an alternative.
4. Choose an input from the list, or click **Capture input...** for a key, left/right/middle mouse button or wheel direction. Use **Capture mouse movement...** for motion: pause briefly, then move clearly in one direction inside the capture box. Use **Cancel** to exit capture: Escape can also be assigned in regular capture. The activating click is ignored, and capture never applies a binding automatically.
5. Click **Save**. You may need to restart the game.

You can also pass a file path:

```powershell
.\ACZeroPCremap.exe "D:\Games\Ace Combat\dinput.ini"
```

Opening or viewing a file does not write to it. Changes stay in memory until you click Save. Development and automated tests have not modified the original `dinput.ini` or `dinputSample.ini` in this repository.

## Supported features

- Flight, Weapons, Camera and Wingman groups; separate menu navigation/selection groups.
- Case-insensitive search across action names/descriptions, group names, key names, INI identifiers and raw binding expressions. All search words must match an action; its alternative bindings remain visible.
- `KeyboardFlight` and `KeyboardUI` sections.
- Simple DirectInput DIK keys and `Mouse1`, `Mouse2`, `Mouse3`.
- Multiple bindings per action, distinct left/right modifiers, and NumPad keys.
- Warnings about bindings shared by multiple actions in one keyboard/mouse tab, including confirmation before assigning a shared input.
- A reversible **Use mouse in flight** checkbox covering all flight mouse buttons, motion, wheel and conditional expressions.
- Backups, discarding unsaved changes, reloading from disk, and restoring a whole file.

Recognized relative/absolute mouse axes and wheel directions are editable, including the single-key conditions used in the supplied INI. Movement capture preserves an existing compatible axis's mode, coefficient and modifier; adding an alternative inherits the action's existing axis template when available. Without a template, movement uses relative defaults from the supplied INI. Unsupported combinations and unknown expressions stay read-only. All device sections, metadata and Force Feedback settings are preserved as opaque data.

Flight labels follow the PS2 **ZERO** control scheme in **Normal** mode. The port's identifiers are mapped using the comments in the supplied INI. ACE5 swaps weapon and radar controls; Novice mode uses turns instead of rolls. Select those settings in the game; this app does not change them.

Descriptions explain missile/target tracking on hold, Boresight mode, and autopilot through both yaw directions. In the Menus tab, Navigation and Selection control menus; Camera and Additional actions describe post-mission replay controls. Unused contains INI entries whose purpose is unknown and has its own note. Replay actions whose exact function is unconfirmed retain neutral names. See [control references](CONTROLS.md).

The app does not need to stay open during gameplay. It uses no global input hooks, drivers, DLL injection or game modifications.

## Finding and editing bindings

Press **Ctrl+F** to focus search on the active tab. For example, `gun Mouse1` finds Fire gun and keeps both its keyboard and mouse alternatives visible. `0x24` finds a DIK value; `MouseX` finds matching mouse-motion expressions.

Search combines with the group dropdown. **Clear** clears only the query; **Clear filters** resets both the query and category when no actions match. Each tab retains its own filters, including after a document refresh. Searching and grouping never change the INI or remove hidden bindings.

Click a key/mouse label to edit it, or hover to see its raw INI value. The adjacent **×** removes only that binding after confirmation. Complex bindings have read-only badges. Unknown action identifiers remain available under **Other** as read-only data.

Descriptions appear directly below each action row, including mode differences, hold behavior and special functions. Long descriptions wrap across the available row width; opening the assignment dialog is not required to read them.

When assigning an input already used by another action in the current tab, the dialog names those actions. Applying asks whether to keep the shared assignment. Declining leaves the document unchanged; accepting keeps both assignments. Existing intentional sharing, such as yaw/autopilot, is allowed. Other tabs do not cause warnings. Detection compares complete normalized inputs, not every possible overlap between conditional expressions.

## Mouse in flight

Clear **Use mouse in flight** on the Flight tab to disable every mouse assignment in `KeyboardFlight`, including read-only motion/wheel expressions and combinations. Keyboard assignments, menu controls and gamepad profiles remain intact. The switch works on the whole flight section regardless of search filters.

Disabled rows are retained as comments prefixed with `; ACZeroPCremap mouse disabled: `, so the game no longer sees active mouse bindings. Checking the box restores those original rows, including after saving and reopening the file. Ordinary comments are never enabled. The change stays in memory until **Save**, which creates a backup as usual. When the file has no active or restorable flight mouse bindings, the switch is unavailable; add a mouse-button binding first if needed.

## Saving and recovery

Each save first creates an exact copy next to the INI, named `dinput.ini.YYYYMMDD-HHmmss-fff.bak`. Filename collisions receive a numeric suffix. Backups are not deleted automatically.

The new document is written to a temporary file, verified, and atomically replaces the original. A backup error or external file change cancels the operation. Use **Reload file** after an external edit; discarding local edits requires confirmation.

**Restore backup...** replaces the **entire INI**, including device profiles, after backing up the current version. `dinputSample.ini` is not presented as a guaranteed factory configuration.

UTF-8 with and without BOM is supported. Unchanged lines are not normalized, and repeated keys remain separate. Replacing a simple key updates only its value and an initial `DIK_...` comment token if present. Repeated declarations of a target section are read-only because their meaning is ambiguous.

## Language

Choose the interface language from the selector at the upper right. The six choices are shown in their native names: English, Русский, Español, Português (Brasil), Deutsch and Français. English is the first-run default regardless of Windows language. The selection takes effect immediately, including action names, descriptions, groups, dialogs and messages.

The selected language is saved to `ACZeroPCremap.settings.json` beside the EXE and restored on the next launch. Move it with the application to retain the preference. Missing, invalid or unsupported settings fall back to English. If the folder is read-only, the language still changes for the current session and the app reports that it could not save the preference. Switching preserves the open INI, unsaved edits, selected profile/tab and search filters; it never writes to the game's configuration.

All app-owned text uses embedded resources. Native Windows file pickers keep OS-owned button labels, while their app-provided titles and filters use the selected language. Game data, identifiers, keycap letters, DIK tokens and existing comments remain unchanged. See [localization notes](LOCALIZATION.md).

## Build

Requires .NET 10 SDK on Windows. The application uses WPF and the standard .NET libraries without third-party input packages. Publishing may download standard .NET runtime packs. Runtime operation is local. Runtime notices are included beside the portable EXE.

```powershell
dotnet build ACZeroPCremap.sln -c Release -m:1
dotnet run --project src/ACZeroPCremap.App -c Release -- "C:\path\dinput.ini"
```

Build the portable player ZIP and a separate source ZIP, including checks:

```powershell
powershell -File scripts/Build-Release.ps1
```

The script uses the workspace SDK if available, otherwise `dotnet` from PATH. The checked-in publish profile enables self-contained single-file deployment, native library bundling and compression, and disables trimming. `.tools`, `bin`, `obj`, `artifacts`, `release` and personal settings are excluded from Git and source archives. See [release instructions](RELEASE.md) and [contributing](../CONTRIBUTING.md) for subsequent changes and builds.

## Checks

Checks are console projects without a test-framework dependency. They return a nonzero exit code on failure; `dotnet test` does not execute them.

```powershell
dotnet run --project tests/ACZeroPCremap.Tests -c Release
dotnet run --project tests/ACZeroPCremap.UiChecks -c Release -- "C:\path\ACZeroPCremap"
```

Core checks cover byte-preserving round trips, individual edits, alternatives, duplicates, unknown expressions, backups, restore, external edits, write failures, scan codes, and language-independent DIK tokens.

UI checks run on Windows. They validate the two keyboard/mouse tabs, WPF bindings, keyboard message processing, assignment warnings, mouse toggle, menu descriptions and all six languages. They also check that gamepad windows and dependencies are absent. Previews are rendered into `artifacts/ui-checks`. These checks do not replace physical keyboard or in-game testing.

Persistence checks use temporary files. The original INI is only read as a fixture.

## Layout

- `src/ACZeroPCremap.Core`: INI model, bindings, DIK mapping and safe persistence.
- `src/ACZeroPCremap.Core/Localization`: shared English resources and resource lookup.
- `src/ACZeroPCremap.App`: WPF windows, view models and localized dialogs.
- `tests/ACZeroPCremap.Tests`: core and file-operation checks.
- `tests/ACZeroPCremap.UiChecks`: WPF checks and previews.
- `SPEC-v1.md`: version 1 specification.
- `VALIDATION.md`: completed checks and remaining manual verification.
- `scripts`: repeatable packaging and published-EXE smoke checks.
- `.github/workflows`: Windows build/check/package workflow; no automatic public release.

## License

MIT; see [LICENSE](../LICENSE). Bundled dependencies retain their own notices. ACZeroPCremap is an unofficial utility and is not affiliated with the game's publisher. No game assets or example game INI are included in the player release.
