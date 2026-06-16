# SurfacePenRemap

Windows app that maps Surface Pen 2 top-button gestures (single press,
double press, long press) to configurable keyboard hotkeys.

Built primarily to drive [Wispr Flow](https://wisprflow.ai/) dictation
from the Surface Pen 2 tail button.

## Features

- Maps all three pen button gestures (single / double / long press) to
  independent hotkeys.
- Fires the output combo on **trigger release** so the held pen key
  can't leak into the synthesized keys as a phantom modifier (same
  technique as AtumtekRemap — important for Electron apps like Wispr Flow).
- Editable trigger / hotkey textboxes plus Learn… buttons for each gesture.
- Individual enable/disable checkboxes per gesture, plus a master toggle.
- Persists settings to `SurfacePenRemap.ini`.
- System tray icon with quick-access menu.
- `PenSniffer.ahk` utility to discover which key codes your pen sends.

## Prerequisites

Before using this tool, go to **Settings > Bluetooth & devices > Pen &
Windows Ink** and set the pen button shortcuts to **Nothing** (or another
app you don't use). This ensures Windows sends the raw F-key events
(F18/F19/F20) that this app intercepts.

If you leave the defaults, Windows may launch OneNote or the Snipping
Tool instead of sending key events.

## Install

1. Install AutoHotkey v2:
   ```
   winget install AutoHotkey.AutoHotkey
   ```
2. Clone this repo to `%USERPROFILE%\SurfacePenRemap` (or anywhere).
3. Double-click `Run.bat`, or run:
   ```
   "%LOCALAPPDATA%\Programs\AutoHotkey\v2\AutoHotkey64.exe" SurfacePenRemap.ahk
   ```

## Discover Your Pen's Key Codes

If you're not sure what keys your pen sends:

1. Run `PenSniffer.ahk` (double-click it).
2. Single-press, double-press, and long-press the pen button.
3. Open `penlog.txt` — it shows every key code detected.
4. Use those key names in the Trigger fields.

## Use

1. By default, **single press** (F20) sends **Ctrl+Shift+F9** (for Wispr).
2. Double press (F19) and long press (F18) are disabled by default —
   enable them and assign hotkeys as needed.
3. Use the **Learn…** buttons or type AHK hotkey syntax directly:
   - `^` Ctrl · `+` Shift · `!` Alt · `#` Win
   - `{Space}`, `{Enter}`, `{F9}` etc. for the base key
   - Example: `^+{F9}` = Ctrl+Shift+F9
4. Click **Save**. Settings persist in `SurfacePenRemap.ini`.
5. Use the master toggle or tray icon to enable/disable all mappings.

## Global Hotkey

Press **Ctrl+Alt+P** to show the window from anywhere.

## Run at Login

Double-click `Install-Startup.ahk` once to create a Startup folder
shortcut.

## Notes

- Built on AutoHotkey v2.
- The Surface Pen top button typically sends F20 (single), F19 (double),
  F18 (long press) when the Windows pen settings are set to "Nothing".
  Your pen may differ — use `PenSniffer.ahk` to check.
- `SurfacePenRemap.ini` is user-local and not committed.
