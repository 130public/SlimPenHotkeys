; Creates a shortcut in your Startup folder so the remapper runs at login.
; Run by double-clicking, or:  AutoHotkey64.exe Install-Startup.ahk
#Requires AutoHotkey v2.0
src := A_ScriptDir "\SlimPenHotkeys.ahk"
ahk := A_AppData "\..\Local\Programs\AutoHotkey\v2\AutoHotkey64.exe"
lnk := A_Startup "\SlimPen Hotkeys.lnk"
FileCreateShortcut(ahk, lnk, A_ScriptDir, Chr(34) src Chr(34),, ahk)
MsgBox "Startup shortcut created:`n" lnk
