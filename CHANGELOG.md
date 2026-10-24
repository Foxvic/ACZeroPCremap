# Changelog

## 0.8.1 — 2026-10-24

- Added a burgundy and gold winged-key icon to the EXE and application windows.
- Fit windows to the monitor's usable area, including Windows display scaling, and keep maximized windows clear of the taskbar.
- Added a compact layout for smaller windows, with secondary file actions under the ⋯ menu.
- Remember the main window's size, position and maximized state beside the portable EXE; recover saved positions from disconnected monitors.

## 0.8.0 — 2026-10-24

- Fixed a 64-bit overflow when a negative wheel delta reached the keyboard message hook.
- Added a separate mouse movement capture area with a short arming delay, jitter threshold and dominant-direction detection.
- Added wheel-up/down capture and editable recognized mouse axes, retaining compatible existing coefficients, absolute/relative mode and Alt conditions.
- Added localized input names, duplicate checks and persistence/capture regressions. Unknown expressions and all device sections remain protected.
- Both capture buttons turn into Stop during capture. Stop preserves the previous selection; left-clicks elsewhere can still assign Mouse1. Movement capture ignores the wheel and uses a taller capture area.

## 0.7.1 — 2026-10-24

- Capture input now accepts the left, right and middle mouse buttons inside the assignment window.
- The activating click is ignored, and captured clicks cannot also activate Apply or other controls. Cancel remains available during capture.
- Updated capture instructions and messages in all six languages. Side buttons and wheel expressions remain outside individual editing support.

## 0.7.0 — 2026-10-24

- Removed gamepad remapping completely: tab, capture, assignment/profile dialogs, editing APIs and DirectInput dependencies.
- Kept Flight and Menus keyboard/mouse editing, shared-input warnings, reversible flight mouse switch, backups, restore and six languages.
- Existing device sections, including names, alternatives, unknown expressions and FFB, remain preserved as opaque INI data.

## 0.6.1 — 2026-10-24

- Added a visible Steam Input warning to the Gamepad tab: disable it for DirectInput; Steam layouts are configured separately in Steam.
- Removed replay references from the menu's Unused group and its action descriptions. These are existing INI entries with an unknown purpose.
- Updated all six languages and wrapped longer notes in the main window.

## 0.6.0 — 2026-10-24

- Renamed the application and source solution to ACZeroPCremap.
- Added a compressed, self-contained Windows x64 portable release profile.
- Store the language preference beside the EXE for portable use.
- Preserve compatibility with mouse-disable comments from ACZeroRemap.
- Added MIT licensing, release/source packaging scripts and GitHub CI.

## 0.5.x

- Shared-input warnings and optional confirmation within the current tab/profile.
- Reversible flight mouse switch and visible post-mission replay descriptions.
- Pixy parody quote, Georgia typography and corrected English wording.

## 0.4.x

- Six languages with live switching and game-inspired menu styling.

## 0.2.x–0.3.x

- Separate DirectInput gamepad profiles, capture and shared menu theme.

## 0.1.x

- Byte-preserving INI edits, keyboard capture, alternatives, backups and restore.
