# ACZeroPCremap validation

Updated: October 24, 2026.

## Version 0.8.1: window placement and application icon

- Added a first-launch reminder beside the INI location in all six languages and in the GitHub/player READMEs. The no-file hint wraps in compact windows so the location and reminder remain readable. Version remains 0.8.1.

- Added an embedded multi-size winged-key ICO (16, 20, 24, 32, 40, 48, 64, 128 and 256 pixels) and a shared window icon. Its generated source and rebuild script remain in the source package.
- Added DPI-aware fitting to the monitor work area, compact main-window layout and a localized secondary-actions menu. Dialogs also fit the work area; the movement area stays scrollable with its Stop/Apply/Cancel controls accessible.
- Added portable window-placement persistence alongside the language preference, retaining normal bounds and maximized state when minimized on close. Saved locations on disconnected displays return to an available screen. Read-only settings folders do not block closing.
- Core checks cover DPI/resolution combinations, oversized and disconnected-monitor bounds, negative monitor coordinates, invalid saved bounds, legacy preferences and preserving language/window settings across writes. UI checks exercise native startup/maximization/restore and rendered small-screen layouts in all six languages.
- Release build completed with zero warnings/errors: all 61 core checks and Windows UI checks passed with no WPF binding errors. Reviewed the regular window frame, winged-key icon and compact translated previews. Checked Open/Save visibility and a scrollable list for 1366×768 and 1920×1080 display equivalents at 100%, 125% and 150% scaling. Native checks confirm work-area fitting, taskbar-aware maximization and restored placement/state.
- Published version 0.8.1.0 (player ZIP 56.90 MiB). The standalone EXE passed startup/close with empty DOTNET_ROOT, no dotnet on PATH and a path containing spaces/Cyrillic; the bundled WPF runtime loaded and placement was saved beside the EXE. Both original INI hashes remain unchanged. Physical changes between monitors with different scaling and a clean Windows VM remain manual verification tasks.

## Version 0.8.0: mouse axes and wheel overflow

- Input capture also toggles to Stop. Left-clicking Stop or its label stops without assigning Mouse1; left-clicking elsewhere still captures Mouse1. Checks cover the starting click, press/release handling, restarting, keyboard/mouse/wheel capture and translated labels. The help explains where to click for Mouse1. Version remains 0.8.0.

- Movement capture's button toggles to a localized Stop action. Stopping preserves the selection, ignores subsequent movement and restores the capture label. UI checks cover the actual mouse press/release route, restarting, successful capture and all six languages. Version remains 0.8.0.

- Layout refinement within 0.8.0: doubled the movement area's minimum height from 100 to 200 DIP. Movement capture now ignores wheel input and shows movement-only instructions; wheel assignments remain in Capture input. Updated all six translations without changing the release version.

- Reproduced the reported wheel crash by passing a packed negative wheel WPARAM to the keyboard hook; it threw OverflowException in IntPtr.ToInt32. The hook now rejects non-keyboard messages before conversion.
- Added relative/absolute movement and wheel binding recognition, a separate capture area, a short arming delay and jitter/dominant-direction filtering. Existing compatible mode/coefficient/condition templates and comments are retained. Unknown expressions and device sections remain protected.
- Added regression coverage for positive/negative wheel capture, absolute/relative template inheritance, normalized duplicates, malformed/newline input rejection, all movement directions, arming/jitter, and save/toggle/restore. Six resource sets contain 308 matching keys.
- All 57 core checks and Windows UI checks passed with zero build warnings/errors and no WPF binding errors. Verified cancellation in both capture modes and rearming the delay on entry into the movement area. Reviewed the capture area, selected-axis preview and translated dialogs.
- Published portable version 0.8.0.0 (player ZIP 56.78 MiB). The standalone EXE passed the deployment smoke check with empty DOTNET_ROOT and no dotnet on PATH, initializing and exiting normally. Both original INI fixture hashes remain unchanged. Physical mouse movement/wheel and the new assignments' in-game behavior still need manual verification; clean-VM status remains unchanged.

## Version 0.7.1: mouse-button capture

- Reproduced the missing capture in a WPF routed mouse-event check: Mouse1 did not become selected after starting capture.
- Added dialog-local mouse capture for the existing Mouse1/Mouse2/Mouse3 choices. Checks cover all three buttons, the activating click, consumed releases, unsupported side buttons and cancellation without assigning a binding. Existing keyboard checks continue to run unchanged in scope.
- Updated the five capture-related strings in all six languages and wrapped the longer instructions.
- All 48 core checks and Windows UI checks passed, including keyboard capture and mouse clicks over Apply. The missing Mouse1 capture test failed before the implementation and passed afterward. Reviewed English, Russian and French dialogs plus the unsupported-input view. Release build has zero warnings/errors and no WPF binding errors.
- Published portable version 0.7.1.0 (player ZIP 56.77 MiB) and the updated source archive. Original INI fixture hashes remain unchanged. Hardware mouse behavior and clean-Windows VM testing remain manual verification items.

## Version 0.7.0: keyboard and mouse only

- Removed the Gamepad tab, capture backend, assignment/profile dialogs, mutating device APIs and Vortice/SharpGen dependencies. The release now exposes only Flight and Menus keyboard/mouse controls. Removed 45 gamepad-only resource keys from each language, leaving 291 matching keys.
- Device and unknown sections remain opaque and read-only. Added regression coverage for rejected device mutations, byte-exact preservation through keyboard/mouse edits, saving/reopening and mouse restoration, plus compatibility with whole-file device-only backups.
- Retained keyboard capture checks (including Space, Escape, Tab, modifiers and NumPad), assignment/sharing confirmations, independent filters, Unused descriptions, reversible mouse control, all six live languages and custom window commands.
- All 48 remaining core checks and Windows UI checks passed. Release build has zero warnings/errors and no WPF binding errors. Keyboard capture implementation is unchanged; UI checks now distinguish capture behavior from WPF's own message handling and simulate key release before fresh modifier presses to avoid interference from physically held keys. Reviewed the two-tab Flight/Menus views and translated Unused descriptions.
- Published portable version 0.7.0.0 (player ZIP 56.77 MiB). The standalone EXE passed the existing deployment smoke check with empty DOTNET_ROOT, no dotnet on PATH and a path containing spaces/Cyrillic text; it initialized and closed normally. No Vortice/SharpGen dependencies remain in the published dependency metadata. Both original INI fixture hashes remain unchanged. Clean-Windows VM testing remains unverified as before.

## Version 0.6.1: Steam Input notice and Unused descriptions

- Added a persistent warning banner above the DirectInput profile selector. It asks users to disable Steam Input and explains that Steam Input layouts are configured in Steam, while this app edits dinput.ini.
- Unused now has a separate group description and action/dialog hints explaining that the entries exist in the INI but their purpose is unknown. Removed the blanket replay claim from menu notes in all six languages.
- Release build and Windows UI checks passed with zero warnings/errors or WPF binding errors. Regression checks cover the Unused exception, visible Steam Input warning and live translation of both. Reviewed English and Russian/French previews and the reduced layout. All six resource sets contain 336 matching keys.
- All 61 core checks passed during release packaging. Published the portable Windows x64 folder and ZIP (57.12 MiB) plus the updated source archive; verified EXE version 0.6.1.0. Both original INI hashes remain unchanged.

## Version 0.6.0: ACZeroPCremap portable release

- Renamed the solution, projects, namespaces, product metadata, EXE, window titles and all localized app names to ACZeroPCremap. Added an asInvoker manifest and a compressed self-contained win-x64 publish profile with trimming disabled and embedded debug symbols.
- The EXE is approximately 62.34 MiB (65,368,578 bytes), including .NET and native WPF components. SDKs, NuGet caches, source and intermediate builds are excluded from the player package. Source is distributed in a separate small ZIP.
- All 61 core checks and Windows UI checks passed, with a clean Release build and no WPF binding errors. New checks verify portable preference resolution independent of the working directory and exact restoration of legacy ACZeroRemap mouse-disable comments.
- The published EXE initialized and exited with code 0 when copied alone to a path containing spaces and Cyrillic characters. DOTNET_ROOT pointed at an empty directory and PATH contained no dotnet. The host trace reported self-contained execution, and native WPF loaded from the bundle's TEMP extraction directory. Startup wrote no preference file. This verifies the deployment on this host; it is not a clean-OS test.
- Windows Sandbox is not installed on this host. Launch, editing and saving on a clean Windows VM without prerequisites remain unverified; the repeatable checklist is in docs/RELEASE.md. The EXE is unsigned.
- Added MIT, contributor/release instructions, source and player packaging, SHA-256 manifests and a GitHub Windows workflow. Original INI fixture hashes still match the recorded baseline. No repository or release has been published to GitHub.
- Final packaging completed: player ZIP 57.12 MiB; source ZIP approximately 0.14 MiB with 56 files. Archive checks confirmed a root-level EXE, complete runtime notices and no SDK/cache/source/INI in the player archive, or bin/obj/EXE in the source archive. The final EXE's SHA-256 matches the smoke-tested binary.
- Extracted the source ZIP into a new directory, restored from the local package cache and rebuilt the entire solution with zero warnings/errors. This verifies that the editable source archive does not rely on the original build intermediates.

## Version 0.5.3: quote wording (October 24, 2026)

- Corrected the parody's second sentence to “What have the PC port’s limitations given us?” in all six resource sets.
- Release build and existing Windows UI checks passed with zero warnings/errors or WPF binding errors.

## Version 0.5.2: quote typography

- Set the quote in Georgia italic at 14 DIP with 19 DIP line spacing. Centered the smaller Georgia attribution directly below the quote with a 3 DIP gap.
- Release build and existing Windows UI checks passed with zero warnings/errors or WPF binding errors. Standard and reduced-size rendered previews were visually inspected.

## Version 0.5.1: header quote

- Added the requested Pixy parody in the header between the title and language selector. The English wording remains the same in all six languages. The title and quote wrap when space is limited.
- Release build passed with zero warnings/errors. Existing Windows UI checks passed without binding errors. English, French and reduced-size previews were visually inspected.

## Version 0.5.0: shared-input warnings, flight mouse and replay context

- Added warnings in keyboard/mouse and gamepad assignment dialogs, plus confirmation before keeping a new shared assignment. Checks verify cancellation leaves the document unchanged, acceptance preserves both actions, and other tabs/profiles do not cause warnings. Digital-strength variants count as the same physical key/button; unrelated modifiers in complex expressions do not create false matches.
- Added a Flight-only mouse checkbox. It comments out every mouse assignment in the section, including motion, wheel, conditions and unknown mouse entries, and restores the exact original rows. Core checks cover BOM, mixed line endings, whitespace/comments, no final newline, idempotence, missing/repeated sections, keyboard edits made while mouse is disabled, and save/reopen/restore. Menu and gamepad bindings remain unchanged.
- Added visible post-mission replay notes to all menu groups after Selection and clarified action descriptions, based on the user's explanation. All six resource sets now contain the same 334 keys.
- All 59 core checks and Windows UI checks passed. The Release solution built with zero warnings/errors. WPF checks cover the checkbox, dialog warnings, confirmation outcomes, replay group notes and existing language/capture behavior, with no binding errors. Rendered views were visually reviewed.
- Published the self-contained Windows x64 EXE and verified its file version is 0.5.0.0. Both original INI fixture hashes still match the recorded baseline. Checks use temporary configuration files and an isolated language preference. New mouse-disable behavior still needs in-game verification.

## Version 0.4.1: background and file-location hint

- Removed the static horizontal interference band and its horizontal guide from the shared background in every window.
- Updated the no-file hint in all six languages with the supplied game folder: `ACE COMBAT ZERO THE BELKAN WAR (Steam Version)\ACZero\settings`. The hint identifies `dinput.ini` as the file to open.
- Release build and existing WPF checks passed. The no-file view was rendered and visually reviewed; the hint fits and the band is absent. Portable version 0.4.1 was published.

## Version 0.4.0: six languages and live selection

- Added Russian, Spanish (Spain), Brazilian Portuguese, German and French alongside the English fallback. Each resource set contains all 325 keys. Identifiers and universal keycap symbols remain unchanged; app-owned prose, labels, descriptions, input names and errors are translated.
- The header picker switches immediately and saves only an app preference in `%LOCALAPPDATA%\ACZeroPCremap\settings.json`. Corrupt or unsupported preferences fall back to English. Settings writes use a temporary sibling file and replacement.
- All 51 core checks passed. Added checks cover the requested language set, preference round trips and corruption, full resource/placeholder coverage and byte-identical INI edits across cultures.
- WPF checks passed for all six languages with no binding errors. Switching preserves the session object, unsaved bytes, selected device profile, active tab and each tab's filters. Stable option objects keep profile/group selections visible while their names change.
- Rendered main windows and gamepad dialogs in every added language and reviewed text/layout. Existing keyboard capture, device handling and window-command checks continue to pass.
- Automated preference writes use a separate file under `artifacts/ui-checks`; no real user-language preference or original INI fixture was changed.

## Version 0.3.0: Ace Combat Zero menu styling

- Used the three screenshots in `UIREF` as visual references for the crimson background, diagonal texture, amber frames, yellow selection and rectangular menu layout. The background is scalable WPF geometry; screenshots are not embedded into the UI.
- Applied a shared theme to every app-owned window and control, including caption buttons, dropdowns, text fields, scrolling, assignment/profile dialogs and confirmations. Native Windows file pickers retain OS styling.
- Kept keyboard/mouse and gamepad sections separate, with action groups, search, all alternatives and descriptions in the main list. INI parsing, input capture and persistence logic are unchanged.
- Added UI checks for dropdown display names/popup contents and the custom window caption/close command. Offscreen preview checks use software rendering to avoid depending on a GPU render channel.
- Release build and UI checks passed without warnings or WPF binding errors. Previews of the complete window, reduced layout and all dialogs were visually reviewed. Both original INI SHA-256 hashes remain unchanged.
- Startup window creation is explicit so UI checks can disable it; this avoids a second startup window interpreting the test runner's directory argument as an INI. Normal startup still creates the main window and uses the same argument/adjacent-file discovery.

## Version 0.2.0: separate DirectInput gamepad support

- Added the Gamepad tab, default/device profile selection, profile cloning, manual button/POV/axis/condition editing and matching-device capture. Keyboard/mouse sections use separate editing APIs.
- All 47 core checks passed, including profile isolation, normalized duplicate detection, byte-preserving replacements, conditional expressions, unknown/FFB preservation, profile cloning, capture release/centering, and gamepad-only save/restore. Atomic replacement tests ran outside the sandbox because its filesystem restrictions block replacement.
- WPF checks cover profile switching without dirtying the file, conditional HOTAS rows, repeated read-only FFB settings, expression validation, simulated capture, disconnection cleanup, offline editing and duplicate profile protection. Existing keyboard and English-language checks also pass with no WPF binding errors.
- The checked native DirectInput state call was exercised with a controlled COM object returning a failed HRESULT. This verifies failed reads are reported rather than interpreted as input.
- Native DirectInput enumeration completed and reported zero connected devices. Physical capture, device-specific axis layout and application of gamepad mappings in the game remain unverified.
- Main/profile/assignment views were rendered for visual inspection. The main window minimum height was increased to keep the Gamepad action list usable with its additional profile controls.
- Original root INI fixtures remain unchanged. Version 0.2.0 is a self-contained Windows x64 build; third-party notices accompany it.

## Version 0.1.5: descriptions in the main list

- Action descriptions are always shown below their action row rather than only in tooltips or the assignment dialog.
- Descriptions span the row and wrap to fit the available width, preserving space for bindings and commands.

## Version 0.1.4: groups, search and compact rows

- Flight actions are organized into Flight, Weapons, Camera and Wingman. Menus use Navigation, Selection, Camera, Additional actions and Unused. Unknown identifiers appear under Other.
- Added case-insensitive multi-word search over visible labels, groups, key names, technical identifiers and raw expressions. Search and group filters combine; matching actions keep every alternative binding.
- Each tab retains its own filters. Clear query, empty-state reset and refresh behavior were checked. Filtering does not mark the document as changed.
- Compact rows use clickable binding labels, separate remove buttons and read-only expression badges. Input hover hints retain raw INI values. Ctrl+F focuses the active search field.
- WPF checks cover grouping completeness, bound search-box updates, multiple search terms, raw hex/complex expressions, group intersections, empty results, reset, independent tabs, document refresh, unknown actions and missing sections.
- Tests explicitly disable startup-file discovery so offscreen template initialization cannot open the test runner's directory argument as a game INI.
- Release build completed without warnings or errors. Grouped, filtered, empty and reduced-size views were rendered for visual review.

## Version 0.1.3: English default and resource extraction

- All app-owned UI labels, actions, tooltips, capture messages, confirmations and error explanations were translated to English.
- English resources are embedded in `Strings.resx`. `Text.Culture` explicitly defaults to English; XAML resolves text with `TrExtension`.
- App-owned confirmation windows keep their buttons in English even on Russian Windows. File pickers remain native Windows dialogs.
- DIK comment tokens no longer depend on localized display names. A core check covers every supported keyboard token.
- UI checks run under a Russian host UI culture and verify English action names and Save / Don't save / Cancel buttons.
- A language selector is intentionally deferred; the resource structure is ready for additional cultures.
- All 31 core checks and WPF checks passed. The build completed without warnings or errors, and the English self-contained EXE was published.
- Application startup sets WPF's UI culture to English, including built-in commands such as Copy. Rendered main, assignment and confirmation windows were visually reviewed.

## In-game confirmation by the user

On October 23, 2026 the user reported that the game reads and accepts the changes.

This confirms reading the edited INI and applying changes on the user's installed build. The build number, exact bindings tested and need for a restart were not specified. This report does not verify every input type or other game builds.

## Version 0.1.2: gameplay action labels

- Flight labels were checked against the original PS2 manual, printed pages 5-6 and 11. ZERO/ACE5 differences were checked against Namco's official page.
- Port identifiers were mapped using the supplied INI comments: LT/RT are throttle down/up, LB/RB are yaw, and X/View are weapons/radar under ZERO.
- Flight and menu sections have separate labels. Shared-binding warnings and assignment dialogs use gameplay names.
- WPF checks passed, including different meanings of A in each section and pitch direction. Rendered previews were visually inspected.
- No INI data was changed. The new labels have not been individually verified in the game.

## Version 0.1.1: Space capture fix

- Reproduced the missed Space key when input passes through ComponentDispatcher before the HwndSource hook. The original SendMessage-only checks bypassed this WPF stage and missed the issue.
- Added a ThreadFilterMessage handler limited to the capture dialog and capture mode. It is removed when the dialog closes.
- The regression failed before the fix and passed afterward: Space selects DIK_SPACE (0x39), and Apply becomes available.
- Message-queue checks passed for Escape, Tab, Enter, NumPad Enter, right modifiers and an arrow key, including repeat suppression and filtering messages for other windows.
- Build completed with no warnings or errors. Physical keyboard checks still require manual confirmation.

## Baseline version 0.1.0

- Release solution built with .NET SDK 10.0.402, with no warnings or errors.
- The initial 30 core checks passed.
- WPF checks passed for both sections, alternative bindings, disabled saving without edits, theme and data bindings.
- The dialog was checked using native messages for Escape, Tab, Enter, NumPad Enter, right Ctrl, right Shift, right Alt and Up, plus an unsupported key.
- WPF previews were rendered in `artifacts/ui-checks` and visually reviewed, including a reduced content area.
- A Windows x64 self-contained EXE was published.
- The published EXE initialized its window and closed normally, with exit code 0.

## Original file integrity

The original root-level fixtures were not changed during development. Their SHA-256 values before and after the implementation:

```text
dinput.ini
BCE70762F9E9B6714890FAB01BF10F684BEF07585CF131D230E6B51680AE20D8

dinputSample.ini
BCE70762F9E9B6714890FAB01BF10F684BEF07585CF131D230E6B51680AE20D8
```

Persistence checks use temporary files and verify exact round trips, individual binding edits, preservation of alternatives and unknown expressions, backup bytes, exact restore, external-edit protection, and preservation of the original on backup/replacement failures.

The sandbox blocked atomic file replacement in temporary-file tests. Those checks were rerun outside the sandbox and passed without changing the persistence implementation.

## Remaining manual checks

- Match action labels to the selected game scheme; verify context-specific menu functions and physical mouse-button mapping.
- Capture from physical Russian/English keyboards, including hardware-specific NumPad and modifier behavior. Native-message tests are not hardware testing.
- Windows scaling at 150-200% and deployment on another Windows 10/11 machine.

References and mapping limitations are documented in `docs/CONTROLS.md`. In-game application is confirmed on the user's build; other builds are unverified.
