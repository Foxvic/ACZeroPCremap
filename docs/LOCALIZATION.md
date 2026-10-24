# Localization

Version 0.8.1 supports English (`en`), Russian (`ru`), Spanish from Spain (`es-ES`), Brazilian Portuguese (`pt-BR`), German (`de`) and French (`fr`). English is the first-run fallback, independent of the Windows language.

The initial selection was based on the [Steam September 2026 language survey](https://store.steampowered.com/hwsurvey/hw/). The final set follows the user's revision: omit Simplified Chinese and include German and French, resulting in six choices including English.

## Resources and live updates

- `src/ACZeroPCremap.Core/Localization/Strings.resx` is the English fallback; `Strings.<culture>.resx` supplies each translation. All six sets contain the same 309 keys and format placeholders, including movement/wheel names, capture/Stop instructions, the compact actions menu, shared-input warnings and descriptions of unknown INI entries. Gamepad-only resources have been removed. The Pixy parody quote intentionally keeps the requested English wording in every language.
- `Text.Get` and `Text.Format` resolve strings using `Text.Culture`. Changing the culture raises `CultureChanged`.
- XAML uses `{loc:Tr ResourceKey}`, a live binding to `LocalizedStrings`. This updates labels and control templates without rebuilding the window.
- The main view model refreshes translated action rows, descriptions and warnings using the same document. Group/profile option identities remain stable while their names update, preserving visible selections.
- The language dropdown uses native names, so users can find their language even when the current interface is unfamiliar.
- Application UI culture follows the selection. Native file picker buttons still follow OS behavior; app-provided picker titles and filters are translated.

## Persistence

`LanguagePreferences` reads and writes `ACZeroPCremap.settings.json` beside the EXE, using `AppContext.BaseDirectory` rather than the current working directory. Only supported language codes are accepted. The same file stores the main window's normal bounds, DPI and maximized state; language and placement updates preserve each other. Older language-only files remain supported. The file is replaced through a temporary sibling file; it is separate from the game configuration and its backups. This makes the preferences portable along with the application. There is no automatic migration from the old ACZeroRemap preference under LocalAppData.

Missing, malformed or unsupported preferences fall back to English. A preference-write failure leaves the selected language active for the session and displays a translated message. Language changes never save, discard or replace the open INI.

## Adding another language

1. Add a complete `Strings.<culture>.resx` with the same keys and placeholders.
2. Add the culture code and native display name to `LanguagePreferences.Languages`.
3. Check action descriptions, errors, picker filters and keyboard names. Technical identifiers and universal keycap symbols stay unchanged.
4. Run the core resource/format checks and the Windows UI checks; inspect rendered previews for longer labels.

The checks cover each translation, preference round trips and corruption, live XAML updates, new dialogs, and preservation of unsaved INI bytes, active tabs and filters. Automated UI checks use an isolated settings file under `artifacts/ui-checks`.
