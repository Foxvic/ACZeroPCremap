ACZeroPCremap - portable Windows x64 release

QUICK START
Launch the game at least once to generate its INI configuration files.
1. Extract the complete ZIP into a writable folder, such as Documents.
2. Double-click ACZeroPCremap.exe. No installer, administrator rights or
   separate .NET installation is needed.
3. Open your game's dinput.ini from:
   ACE COMBAT ZERO THE BELKAN WAR (Steam Version)\ACZero\settings
4. Edit the bindings, then click Save. Each save creates a backup beside
   the game's INI. A game restart may be needed.

Use your own INI. No example game configuration is included in this release.
Flight / Menus keyboard and mouse controls are edited separately.
Menu Camera and Additional actions control replays.
Unused lists existing INI entries whose purpose is unknown.
Existing device sections in the INI are preserved without editing them.
On smaller screens, the extra file actions are available under the ... button.
Capture input accepts keys, left/right/middle mouse buttons and the wheel.
Capture mouse movement opens a dedicated area: pause briefly, then move in
one clear direction. Existing compatible axis settings are preserved.
Both capture buttons become Stop while active. Left-click outside Stop and
Cancel to assign the left mouse button.
Uncheck Use mouse in flight to disable flight mouse bindings; recheck it to
restore them. Shared inputs are allowed after confirmation.

PORTABLE OPERATION
Language and main-window placement are saved as ACZeroPCremap.settings.json
beside the EXE. Windows fit the available screen area and Windows scaling.
Move that file with the application to retain your preference. Nothing is
installed. To remove the app, close it and delete its folder. Edited game
files and backups are kept in the game folder and are not removed.

The bundled .NET runtime extracts native support files to %TEMP%\.net on
startup. This is normal and does not install .NET. First launch may take
longer. A writable TEMP folder is required. To save game changes, the game
settings directory must also be writable by your Windows account.

REQUIREMENTS AND LIMITS
64-bit Windows 10/11 desktop. Windows 7/8.1, 32-bit Windows and Server Core
are not supported. No WebView2, Python, Node.js or .NET SDK is required.
The release includes .NET and its native runtime dependencies. Internet
access is not needed at runtime. Clean-VM verification is documented
separately in the source repository; it has not yet been completed.

The EXE is unsigned. Windows may show an unknown-publisher warning after
download. Use the original project's release and compare its SHA-256 hash.
This is an unofficial tool and is not affiliated with the game's publisher.

MIT license: LICENSE.txt. Bundled component notices: THIRD-PARTY-NOTICES.txt
and licenses/. Source code is provided separately in the source archive.

RUSSIAN / РУССКИЙ
Запустите игру хотя бы один раз, чтобы она создала INI-файлы настроек.
Распакуйте архив в доступную для записи папку и запустите ACZeroPCremap.exe.
Установка .NET и права администратора для запуска не нужны. Выберите язык
в правом верхнем углу, откройте игровой dinput.ini и нажмите Save после
изменений. Резервные копии создаются рядом с игровым INI. Примера INI
в релизе нет: программа должна открыть ваш собственный файл.
Язык, размер и положение окна хранятся рядом с EXE. На небольшом экране
дополнительные операции с файлом доступны через кнопку «...». Файлы .NET распаковываются
во временную папку Windows. Исходники и инструменты разработки в релиз
для игроков не входят.
