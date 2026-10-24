# Ace Combat Zero action labels

Verified against references on October 23, 2026. Labels describe gameplay actions; they are not the names of gamepad buttons.

## Sources

- Original Namco PS2 manual, US edition: [archived PDF](https://www.videogamemanual.com/PS2/Ace%20Combat%20Zero-%20The%20Belkan%20War%20%28USA%29.pdf). Printed pages 4-6, 10-11 and 19-20 were inspected. Page 5 describes controls, page 6 wingman commands and Boresight, page 11 camera views, and page 19 context-dependent display changes during weapon selection.
- [Official Namco control-scheme page](https://www.bandainamcoent.co.jp/cs/list/acecombat-zero/system/custom.php): ZERO uses Square for weapons and Select for radar; ACE5 swaps those actions. The manual's quick overview describes the other arrangement, so the explicit official scheme description resolves this distinction.
- Device-profile and keyboard comments in the supplied `dinput.ini` associate port identifiers with Gun, Missile, SwitchWeapon, SwitchTarget, ThrottleDown/Up, Yaw, Boresight, CameraMode, Pause, SwitchRadarMap, Confirm and Cancel.

## Flight mapping

Labels assume ZERO and Normal mode. The app does not change the in-game control scheme.

- `LX-` / `LX+`: roll left / right; turn in Novice mode.
- `LY-` / `LY+`: pitch down / up. Up on the original flight axis lowers the nose.
- `RX-` / `RX+` / `RY-` / `RY+`: look around. Inversion settings may change the resulting direction.
- `LT` / `RT`: decelerate / accelerate. These port identifiers correspond to PS2 L1/R1 functions, rather than a direct name match between controller triggers.
- `LB` / `RB`: yaw left / right. Together they engage autopilot. No new INI action is synthesized: shared Z and C bindings already activate both directions.
- `A`: fire gun.
- `B`: fire missile / special weapon; hold to track the missile.
- `X`: switch standard missiles and special weapons in ZERO.
- `Y`: change target; hold to track the target.
- `LSC`: Boresight mode, prioritizing targets in the aiming circle ahead.
- `RSC`: switch HUD, cockpit and third-person views.
- `Menu`: pause.
- `View`: change the radar / map display in ZERO.
- `HatU`: order the wingman to attack.
- `HatD`: order the wingman to cover.
- `HatL`: order the wingman to disperse, then select a target priority.
- `HatR`: allow / prohibit wingman special weapons.

## Menus

Navigation uses separate labels. Confirm (`A`) and cancel (`B`) follow the supplied port INI; PS2 button names cannot be copied directly onto these identifiers.

Navigation and Selection retain menu labels. Camera and Additional actions describe post-mission replay controls. Unused is separate: these entries exist in the supplied INI, but their purpose is unknown. Its group note, action descriptions and assignment dialogs do not associate them with replays.

The exact replay functions of `LB`, `RB`, `Y` and `View` remain unconfirmed. They receive neutral additional-replay-action labels and instructions to follow the game prompts. `LT`, `RT`, `LSC` and `RSC` retain the source INI's Unused designation and unknown purpose. `X` describes contextual changes to replay information, where supported. The replay context and the exclusion of Unused come from the user's clarifications, not the PS2 control reference.

The technical keys, values, comments and save rules remain unchanged. Only the program's display labels and hints are translated.
