# SlimPen Hotkeys

Windows app that maps Surface Slim Pen top-button gestures (single press,
double press, long press) to configurable keyboard hotkeys.

Built primarily to drive [Wispr Flow](https://wisprflow.ai/) dictation
from the Surface Pen top button.

## Features

- Map all three pen button gestures (single / double / long press) to
  independent hotkeys.
- Fires the output combo on **trigger release** so the held pen key
  can't leak into the synthesized keys as a phantom modifier.
- Editable trigger / hotkey textboxes plus **Learn…** buttons for each
  gesture.
- Individual enable/disable checkboxes per gesture, plus a master toggle.
- System tray icon with quick-access menu.
- Modern WinUI 3 interface with Mica backdrop.
- Test page to visualize pen button events and hotkey triggers live.

## Install

Install from the **Microsoft Store** (recommended), or build from source:

```
cd app
dotnet run
```

Requires .NET 8 SDK and the Windows App SDK.

## Prerequisites

Before using this tool, go to **Settings → Bluetooth & devices → Pen &
Windows Ink** and set the pen button shortcuts to **Nothing** (or another
app you don't use). This ensures Windows sends the raw F-key events
(F18/F19/F20) that this app intercepts.

If you leave the defaults, Windows may launch OneNote or the Snipping
Tool instead of sending key events.

## Use

1. By default, **single press** (F20) sends **Ctrl+Shift+F9** (for Wispr).
2. Double press (F19) and long press (F18) are disabled by default —
   enable them and assign hotkeys as needed.
3. Use the **Learn…** buttons to capture keys, or type them directly.
4. Click **Save**. Settings persist locally.
5. Use the master toggle or tray icon to enable/disable all mappings.

## Test Page

The **Test** page visualizes pen activity live: the top field lights up
green with the trigger key name when a pen button is pressed, and the
field underneath flashes blue with the hotkey combo when a mapping fires.
A timestamped log lists recent events.

## Legacy AHK Scripts

The repo also contains standalone AutoHotkey v2 scripts (`SlimPenHotkeys.ahk`,
`PenSniffer.ahk`, `Run.bat`, `Install-Startup.ahk`) that predate the WinUI
app. They still work if you prefer a script-based approach:

```
winget install AutoHotkey.AutoHotkey
Run.bat
```

Use `PenSniffer.ahk` to discover which key codes your pen sends.

## Privacy

SlimPen Hotkeys does not collect, store, or transmit any data. The keyboard
hook only inspects the configured trigger keys; all other keys pass through
unmodified. No telemetry, no network, no analytics.

Full privacy policy: [130public.github.io/SlimPenHotkeys#privacy-policy](https://130public.github.io/SlimPenHotkeys/#privacy-policy)

## License

MIT
