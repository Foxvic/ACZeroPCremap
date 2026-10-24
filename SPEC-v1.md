# ACZeroPCremap version 1 specification

Status: original keyboard/mouse specification, with mouse, shared-input, gamepad and localization extensions below. These supersede the original controller exclusions, minimum window height and deferred language-selector requirements.

## Version 0.8.0: mouse axes and wheel safety

- Check native message types before converting WPARAM into a keyboard virtual key. Packed negative wheel deltas must never reach IntPtr.ToInt32.
- Capture wheel up/down in the normal input mode and provide a separate movement-capture button/area. Use a 250 ms arming delay, a 24 DIP displacement threshold and a dominant-axis check; reset the baseline when leaving the area. No global mouse hooks.
- Recognize the supplied INI's MouseX/Y, MouseAX/AY and MouseZ expressions with one zero endpoint, a positive coefficient and an optional single valid DIK condition. Unknown combinations remain read-only. Preserve compatible existing mode, coefficient, condition and comments; default new relative movements/wheel directions to the supplied INI's values.
- Make recognized axis entries individually editable/removable and include canonical axes in duplicate/shared-input checks without rewriting equivalent existing expressions. Keep all device data and unrelated lines intact, with the same explicit Apply/Save and backup rules.
- Translate all controls and names, and test the crash regression, all directions, timing/jitter, persistence, template inheritance and cancellation. This supersedes the original mouse-axis/wheel editing exclusions.

## Version 0.7.1: capture mouse buttons

- Extend input capture to left/right/middle mouse buttons inside the assignment window, mapping to the existing Mouse1/Mouse2/Mouse3 choices. Keyboard capture and list selection remain available.
- Ignore the click that starts capture and wait for held buttons to be released. Consume the captured press and release to prevent accidental control activation. A left click on Cancel must still exit without assigning anything. No global mouse hooks or capture outside the dialog.
- Keep unsupported side buttons and wheel expressions outside individual editing scope; an unsupported button must not replace the selected binding or end capture. Translate capture messages and instructions in every language. This supersedes the original requirement to select mouse buttons only from the list.

## Version 0.7.0: remove gamepad remapping

- Supersede all earlier gamepad-editing and Steam Input notice requirements. Remove the Gamepad tab, device selection/capture, profile creation, assignment dialogs, mutating device APIs and DirectInput package dependencies.
- Keep exactly the Flight and Menus keyboard/mouse tabs. Preserve all existing keyboard capture, simple editing, alternative bindings, scoped sharing warnings, search, descriptions, mouse toggle, language selection and safe persistence behavior.
- Preserve every device/unknown section as opaque INI data, including duplicate assignments, repeated device sections, metadata and FFB. Reject individual add/replace/remove operations outside the two keyboard sections. Keep whole-file restore compatibility with older device-only backups.
- Maintain translations, documentation, portable builds and regression checks. No GitHub publishing is part of this feature removal.

## Version 0.6.1 clarification: Steam Input and unknown entries

- Show a persistent translated warning in the Gamepad tab to disable Steam Input for DirectInput editing. Explain that Steam Input layouts are configured in Steam; this editor only changes dinput.ini. No Steam Input mode selector or automatic Steam setting changes.
- Exclude Unused from replay descriptions throughout the menu UI. Its group note, action descriptions and dialogs must explain that these entries exist in the INI but their purpose is unknown. This supersedes the earlier claim that every group after Selection belongs to replays.

## Version 0.6.0 extension: ACZeroPCremap portable distribution

- Rename the product, executable, solution and source projects to ACZeroPCremap. Retain support for mouse-disable comments previously written by ACZeroRemap.
- Publish Windows x64 as a compressed self-contained EXE at the root of a separate release folder/ZIP. Include native runtime components; require no separate .NET installation or elevation at startup. Keep WPF trimming disabled. Document temporary runtime extraction and perform a clean-VM check before claiming clean-Windows verification.
- Store the language preference beside the EXE as ACZeroPCremap.settings.json. This supersedes the LocalAppData location in the original localization extension. Read-only preference storage must not prevent startup or session-only switching.
- Keep player binaries separate from source, SDK, caches and build intermediates. Add an independent source ZIP, repeatable build/check/package script, MIT license, contributor notes and GitHub CI. Do not publish to GitHub as part of local packaging.

## Version 0.5.0 extension: shared inputs, flight mouse and replay context

- Warn in assignment dialogs when another action in the same tab/selected gamepad profile uses the chosen input. Before applying a new shared assignment, show the affected actions and ask whether to keep it. Declining leaves the document unchanged. Sharing remains allowed and never removes another action's binding automatically. Normalize digital strengths; compare complete expressions without claiming general conditional-overlap analysis.
- Add **Use mouse in flight** only to the Flight tab. Clearing it comments out every mouse assignment in the entire `KeyboardFlight` section, including mouse buttons, motion, wheel, conditions and unknown mouse expressions. Preserve the complete original lines under a recognizable comment prefix; checking it restores those lines exactly, including after save/reopen. Leave ordinary comments, keyboard bindings, menus and device profiles untouched. Do not create default mappings when none exist. Missing/ambiguous sections remain unavailable.
- The switch is an in-memory edit until explicit Save, with the existing backup/write protections. It is the sole exception to the original exclusion of changing complex mouse expressions: those expressions still cannot be individually edited.
- In the Menus tab, explain that Navigation and Selection apply to menus and all groups after Selection apply to post-mission replays. Display the replay note in those group headers and update descriptions without inventing unconfirmed exact functions.
- Translate all new UI text into the six supported languages. Cover scoped warnings, declining/accepting sharing, reversible mouse changes and save/reopen behavior in automated checks.

## Version 0.4.0 extension: interface language selection

- Offer English, Russian, Spanish (Spain), Brazilian Portuguese, German and French in the main-window header, using native language names. Chinese is excluded as requested. English stays the first-run fallback.
- Switch app-owned text without restarting: labels, action names/descriptions, categories, input names, dialogs and errors. Preserve the session, unsaved changes, file bytes, selected tab/profile, search and category filters.
- Save the preference separately from the game's INI in `%LOCALAPPDATA%\ACZeroPCremap\settings.json`. Restore it at startup; use English for missing/invalid/unsupported settings. Failure to persist must not prevent switching for the current session.
- Keep one complete embedded resource set per culture and validate placeholder parity. Preserve technical syntax, file comments and keycap symbols. Native file pickers retain OS-owned controls.

## Version 0.2.0 extension: separate DirectInput gamepad editing

- Add a separate Gamepad tab alongside the keyboard/mouse flight and menu tabs. Keep search, groups, visible descriptions, multiple alternatives and shared-input warnings.
- Discover `[0]` and eight-digit hexadecimal VID/PID sections. Select and edit one profile at a time. Its mappings affect flight and menus. Keyboard/mouse edits and gamepad edits use distinct validated editing APIs.
- Support B1–B32, P0–P3 cardinal directions, X/Y/Z/Rx/Ry/Rz/S0/S1 axes with distinct endpoints in 0–65535, digital strength in 1–65535, and positive/negative button conditions. Unknown expressions, FFB, names and special settings stay read-only.
- Add a gamepad assignment dialog with input choices, a validated expression field and capture from a connected matching DirectInput device. Capture one input only, after release/centering; ignore drift and POV diagonals. Stop on focus loss, disconnect, cancellation or timeout. Non-centering throttle ranges remain manually editable.
- Allow copying the selected profile into a new VID/PID profile, using a detected device or manual ID/name. Reject existing IDs, invalid names and ambiguous duplicate source sections. Preserve every existing profile and original keyboard/mouse line.
- Use existing in-memory edits, explicit Save, backups, external-change protection and exact whole-file restore. Accept gamepad-only backups as well as keyboard configurations.
- Keep all new app-owned text in English resources. Minimum main window size becomes 900 by 720 device-independent units to leave room for the profile selector and action list.
- Document DirectInput compatibility and the INI's instruction to disable Steam Input. Do not claim native XInput remapping. Physical device capture and in-game application require manual confirmation.

The sections below describe the original keyboard/mouse baseline.

## 1. Purpose

Create a small Windows GUI utility for editing keyboard and mouse-button bindings in `dinput.ini`. Users open the file, change individual bindings, and save with a backup and a recovery path.

Requirements are based on `dinput.ini` and `dinputSample.ini` supplied in the project. The files were identical when this specification was written. The user subsequently confirmed that the game reads and accepts edited settings on their installed build; its exact version was not supplied.

## 2. Platform and stack

- Windows 10/11 x64.
- C# and .NET 10 LTS.
- WPF and XAML, with MVVM and a separate INI editing library.
- Portable self-contained EXE, without an installer or separate .NET installation. Runtime extraction at startup is acceptable.
- English throughout app-owned UI, regardless of the Windows language. User-facing text must be stored separately from logic to support future language selection. The selector itself is deferred.
- Fully local operation, without servers, accounts, telemetry or network requests.

## 3. First-version scope

Included:

- Open an existing INI using a file picker.
- Automatically open `dinput.ini` next to the EXE if present; otherwise prompt the user to select a file.
- Edit `KeyboardFlight` and `KeyboardUI`.
- Add, replace or remove individual simple bindings; preserve multiple bindings per action.
- Save with a backup and restore from a backup.
- Warn about shared bindings and protect unsaved changes.

Excluded:

- Live input remapping during gameplay, background global hooks, DLL injection or game binary changes.
- Joystick/HOTAS/controller profile, analog axis or Force Feedback editing.
- Editing combinations, conditions, mouse movement, wheel input or analog coefficients.
- User profiles, cloud sync, automatic updates, game launching or installation discovery.
- Creating a configuration from scratch.

## 4. Main window

Show the full selectable/copyable file path and an unsaved-change indicator.

Commands: Open file, Save, Discard changes, Reload file and Restore backup. Save is enabled only when the document has changes.

Provide Flight (`KeyboardFlight`) and Menus (`KeyboardUI`) tabs. Each action shows its gameplay name, technical identifier and all active binding rows. Simple rows support Edit and Remove. Each known action supports Add binding.

Known identifiers: `HatL`, `HatR`, `HatU`, `HatD`, `LX-`, `LX+`, `LY-`, `LY+`, `RX-`, `RX+`, `RY-`, `RY+`, `LT`, `RT`, `LB`, `RB`, `A`, `B`, `X`, `Y`, `LSC`, `RSC`, `Menu`, `View`.

Names are separate from parser identifiers and differ by section. Flight labels follow PS2 Ace Combat Zero, ZERO scheme and Normal mode, mapped to port identifiers using the supplied INI comments. Explain ACE5/Novice differences in the UI. Do not invent exact names for unconfirmed contextual menu functions. References are in `docs/CONTROLS.md`.

The usability update groups Flight actions into Flight, Weapons, Camera and Wingman, with separate menu categories. Add case-insensitive search across action/group names, input labels, identifiers and raw expressions. All words must match, and the complete action with all its alternatives stays visible. Query and category combine and persist separately for each tab. Provide clear/reset controls, a result count, an empty state and Ctrl+F. Compact rows use clickable binding labels, confirmed per-binding removal and a separate Add binding command. Read-only/unknown values remain visible. Filters affect only the view, never the document.

## 5. Supported values

Editable bindings:

- A single DirectInput DIK hex value, such as `0x1E`.
- `Mouse1`, `Mouse2`, `Mouse3`, displayed as left, right and middle mouse buttons; confirm this mapping in the target game build.
- A semicolon starts a comment, not part of the value.

DIK values are physical key identifiers, not Unicode characters or Windows virtual-key values.

Values such as `0x47:16384`, `0x38 + MouseAX:256:0`, `!0x38 + MouseX:8:0` and `MouseZ:0:1` are read-only. Show their complete expressions. Editing a simple binding must not remove complex expressions assigned to the same action. An action with only complex expressions may still receive an additional simple binding.

Unknown keys and values remain read-only. Unknown sections are preserved.

## 6. Assignment dialog

Add/Edit opens a dialog with a key/mouse list, keyboard capture, Apply and Cancel.

- Capture only while the dialog is active; do not use global input hooks.
- Capture physical keys independently of Russian/English keyboard layout.
- Distinguish left/right Shift, Ctrl and Alt, regular/NumPad Enter, arrow keys and NumPad keys.
- Support letters, numbers, F1-F12, Space, Tab, Enter, Escape, Backspace, modifiers, navigation and NumPad. Verify special cases.
- Escape, Tab and Enter must be assignable rather than triggering normal dialog navigation while capturing.
- Capture one key; do not construct combinations.
- Ignore auto-repeat and the press that opened capture mode.
- Show the detected name and DIK code. Apply changes only when the user clicks Apply.
- Unsupported keys leave the binding unchanged and show an English explanation.
- OS-reserved shortcuts that are not delivered to the app do not need to be captured.
- Mouse buttons are selected explicitly from the list so ordinary UI clicks cannot become accidental bindings.

## 7. Alternatives and shared bindings

Every active assignment line is an independent record. Three `A=` lines must display three bindings, rather than only the last value.

Removing a row removes only that binding and explains that alternatives are retained. Do not add an identical simple binding to the same action. Preserve existing duplicate rows on open; allow manual removal.

If one simple input is assigned to different actions in the same section, show a nonblocking warning naming the actions. Do not warn across tabs. Existing shared Z/C assignments to LB/RB are intentional and must never be removed automatically. Complex-expression conflict analysis is out of scope.

Removing the last simple binding does not guarantee that an action is disabled: complex expressions or game fallback rules may still apply. Show No simple bindings rather than Action disabled.

## 8. Lossless INI editing

Represent the document as ordered lines linked to editable records. A section/key dictionary alone is insufficient.

1. Preserve section/line order, blank lines, comments, duplicate keys and unknown settings.
2. Leave device profiles and unrelated lines unchanged.
3. Replacing a binding changes only the selected value; write new key codes as uppercase `0xNN`.
4. If the selected line's comment begins with a `DIK_...` token, update that token for a new key or remove it for a mouse binding. Keep the remaining comment text and arbitrary comments.
5. Insert a new row after the action's last binding; if absent, insert at the section end before the next section.
6. Delete only the selected binding row, without removing adjacent comments or alternatives.
7. Support UTF-8 with/without BOM. Preserve original line endings and final-newline presence. Block writing unsupported/ambiguous encodings.
8. Opening without edits must never rewrite or normalize the file.
9. Commented lines are not active bindings.
10. Missing target sections are shown as unavailable and are not synthesized. The other tab remains usable.
11. If both target sections are missing, the file is read-only with an unsupported-config explanation.
12. Repeated target-section declarations are ambiguous; disable editing that section while preserving all data.
13. Localization must never change INI identifiers, values or DIK comment tokens. Derive tokens independently of translated display names.

## 9. Save and backups

Edits remain in memory until Save.

1. Validate changed values.
2. Compare the current file with the last opened/saved baseline. If externally changed, cancel saving and offer Reload with a local-edit loss warning. No automatic merge.
3. Create an exact copy of current file bytes beside the source: `<original-name>.YYYYMMDD-HHmmss-fff.bak`. Resolve collisions with a suffix; never overwrite a backup.
4. Write a temporary file in the same directory and verify its bytes and parsing.
5. Atomically replace the source. If safe replacement is unavailable, fail without deleting the original.
6. Update the in-memory baseline and clear the dirty indicator only after success.

Backup failure must prevent saving. Write failures retain local edits and explain the problem with file context. Do not request administrator rights automatically. Saving an unchanged file creates no backup and performs no write. Never delete backups automatically.

## 10. Restore

Let the user select a backup and confirm its path, modification time and replacement of the whole open INI.

Restore exact bytes, including device sections. Back up the current file first. Apply the same external-change and safe-replacement protections as Save.

For unsaved edits, offer Save, Don't save or Cancel before proceeding. Reload the restored file after success.

`dinputSample.ini` is not guaranteed to represent the game's factory settings. Do not provide a Factory defaults command in this version.

## 11. Errors and window behavior

- Prevent accidental edit loss when closing or opening another file; offer Save, Don't save and Cancel.
- Canceling the picker leaves the current document unchanged.
- Failure to open another file retains the existing document and edits.
- Distinguish missing files, access denial, locking/write failures, unsupported encoding and invalid format. App-owned explanations and buttons must remain English on a non-English OS.
- Mention that a game restart may be needed; do not claim live configuration reload.
- Support resizing, scrolling and Windows scaling from 100-200%. Minimum main window size: 900 by 600 device-independent units. Avoid clipping buttons and messages.
- Native Windows picker controls may follow OS language; app labels and filters use app resources.

## 12. Acceptance criteria

Automated core checks:

- All source assignments remain separate, including repeated A, RX-, LB and FFB keys.
- A supported unchanged document serializes to identical bytes.
- Replacing one simple binding changes only that value and, if needed, its initial DIK comment token.
- Add/remove operations preserve other bindings, expressions and device sections.
- Shared bindings are analyzed per section; intentional yaw sharing is retained.
- Backup/write failures and external changes do not overwrite the original.
- Restore reproduces exact backup bytes and saves the previous state.
- Validate DIK mapping for modifiers, NumPad Enter and navigation keys, independently of display language.

UI/manual checks:

1. Open the supplied INI and view both tabs with all alternatives.
2. Replace the Flight A key while retaining Mouse1.
3. Add/remove one alternative, discard edits and verify the baseline returns.
4. Assign Escape, Tab, Enter, right Ctrl and NumPad Enter; test physical Russian and English layouts.
5. Choose each mouse button without capturing ordinary UI clicks.
6. Save, reopen, verify the changed binding and backup.
7. Externally edit the INI and verify stale data cannot overwrite it.
8. Check unsaved-close choices and whole-file restore.
9. Run the portable EXE without a separately installed .NET Desktop Runtime.
10. Verify English UI, confirmations and errors under a Russian Windows/UI culture.

Before release, verify reading both target sections and actual keyboard/mouse behavior in the target game build, including alternatives and deletion. Confirm labels against the game's selected scheme. Record the tested build or explicitly state that it was unavailable.

## 13. Deliverables

- WPF application and independent INI editing library.
- Automated checks for critical persistence, capture and language behavior.
- Portable Windows x64 build.
- English README covering operation, build, recovery and limitations.
- Control-reference and localization notes.
- Validation report separating executed checks from unverified hardware/game behavior.
