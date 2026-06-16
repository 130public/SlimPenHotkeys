#Requires AutoHotkey v2.0
#SingleInstance Force
Persistent
SetTitleMatchMode 2

; ============================================================
; Surface Pen 2 -> Hotkey Remapper
;   Maps the Surface Pen top-button gestures to configurable
;   hotkeys for driving Wispr Flow (or anything else).
;
;   Default key mapping (Windows sends these when "Pen button"
;   shortcuts are set to "Nothing" in Settings > Bluetooth &
;   devices > Pen & Windows Ink):
;     Single press : F20  ->  Ctrl+Shift+F9  (Wispr dictation)
;     Double press : F19  ->  (none)
;     Long press   : F18  ->  (none)
;
;   Use the "Learn" buttons if your pen sends something else.
;   Run PenSniffer.ahk first to discover your pen's key codes.
; ============================================================

AppName     := "SurfacePenRemap"
IniFile     := A_ScriptDir "\SurfacePenRemap.ini"

; ---- Load settings (with defaults) -------------------------
SingleTrigger := IniRead(IniFile, "Single", "Trigger",   "F20")
SingleHotkey  := IniRead(IniFile, "Single", "HotkeyOut", "^+{F9}")
SingleEnabled := IniRead(IniFile, "Single", "Enabled",   "1")

DoubleTrigger := IniRead(IniFile, "Double", "Trigger",   "F19")
DoubleHotkey  := IniRead(IniFile, "Double", "HotkeyOut", "")
DoubleEnabled := IniRead(IniFile, "Double", "Enabled",   "0")

LongTrigger   := IniRead(IniFile, "Long",   "Trigger",   "F18")
LongHotkey    := IniRead(IniFile, "Long",   "HotkeyOut", "")
LongEnabled   := IniRead(IniFile, "Long",   "Enabled",   "0")

HoldMs        := IniRead(IniFile, "Settings", "HoldMs",  "120")

; Track registered hooks so we can unregister cleanly
HookKeys := Map()

; ---- Build GUI ---------------------------------------------
g := Gui("+Resize +ToolWindow", AppName)
g.MarginX := 14, g.MarginY := 12
g.SetFont("s10", "Segoe UI")

; --- Single press section ---
g.SetFont("s10 Bold")
g.Add("Text", "xm", "🖊️ Single Press")
g.SetFont("s10 Norm")

g.Add("Text", "xm y+6", "Trigger key:")
edSingleTrigger := g.Add("Edit", "w200 vSingleTriggerEd", SingleTrigger)
btnLearnST := g.Add("Button", "x+6 w70", "Learn…")

g.Add("Text", "xm y+4", "Send hotkey:")
edSingleHotkey := g.Add("Edit", "w200 vSingleHotkeyEd", SingleHotkey)
btnLearnSH := g.Add("Button", "x+6 w70", "Learn…")

cbSingleEnable := g.Add("CheckBox", "xm y+4 vSingleEnableCB", "Enabled")
cbSingleEnable.Value := (SingleEnabled = "1")

; --- Double press section ---
g.SetFont("s10 Bold")
g.Add("Text", "xm y+12", "🖊️🖊️ Double Press")
g.SetFont("s10 Norm")

g.Add("Text", "xm y+6", "Trigger key:")
edDoubleTrigger := g.Add("Edit", "w200 vDoubleTriggerEd", DoubleTrigger)
btnLearnDT := g.Add("Button", "x+6 w70", "Learn…")

g.Add("Text", "xm y+4", "Send hotkey:")
edDoubleHotkey := g.Add("Edit", "w200 vDoubleHotkeyEd", DoubleHotkey)
btnLearnDH := g.Add("Button", "x+6 w70", "Learn…")

cbDoubleEnable := g.Add("CheckBox", "xm y+4 vDoubleEnableCB", "Enabled")
cbDoubleEnable.Value := (DoubleEnabled = "1")

; --- Long press section ---
g.SetFont("s10 Bold")
g.Add("Text", "xm y+12", "🖊️⏳ Long Press")
g.SetFont("s10 Norm")

g.Add("Text", "xm y+6", "Trigger key:")
edLongTrigger := g.Add("Edit", "w200 vLongTriggerEd", LongTrigger)
btnLearnLT := g.Add("Button", "x+6 w70", "Learn…")

g.Add("Text", "xm y+4", "Send hotkey:")
edLongHotkey := g.Add("Edit", "w200 vLongHotkeyEd", LongHotkey)
btnLearnLH := g.Add("Button", "x+6 w70", "Learn…")

cbLongEnable := g.Add("CheckBox", "xm y+4 vLongEnableCB", "Enabled")
cbLongEnable.Value := (LongEnabled = "1")

; --- Common settings ---
g.SetFont("s10 Bold")
g.Add("Text", "xm y+14", "⚙️ Settings")
g.SetFont("s10 Norm")

g.Add("Text", "xm y+6", "Hold duration (ms):")
edHold := g.Add("Edit", "x+6 w80 vHoldEd", HoldMs)

; --- Master toggle ---
btnToggle := g.Add("Button", "xm y+14 w290 h36", "")
UpdateToggleButton()

status := g.Add("Text", "xm y+10 w290 cGray", "")

; --- Bottom buttons ---
btnSave   := g.Add("Button", "xm y+12 w90 Default", "Save")
btnReset  := g.Add("Button", "x+6 w90", "Defaults")
btnHide   := g.Add("Button", "x+6 w90", "Hide")

; ---- Wire up events ----------------------------------------
btnLearnST.OnEvent("Click", (*) => LearnTrigger("Single", edSingleTrigger))
btnLearnSH.OnEvent("Click", (*) => LearnHotkey(edSingleHotkey))
btnLearnDT.OnEvent("Click", (*) => LearnTrigger("Double", edDoubleTrigger))
btnLearnDH.OnEvent("Click", (*) => LearnHotkey(edDoubleHotkey))
btnLearnLT.OnEvent("Click", (*) => LearnTrigger("Long", edLongTrigger))
btnLearnLH.OnEvent("Click", (*) => LearnHotkey(edLongHotkey))

btnToggle.OnEvent("Click", (*) => ToggleAll())
btnSave.OnEvent("Click",   (*) => SaveSettings())
btnReset.OnEvent("Click",  (*) => ResetDefaults())
btnHide.OnEvent("Click",   (*) => g.Hide())
g.OnEvent("Close", (*) => g.Hide())

; ---- Tray menu ---------------------------------------------
A_TrayMenu.Delete()
A_TrayMenu.Add("Show " AppName, (*) => ShowWindow())
A_TrayMenu.Add("Toggle All", (*) => ToggleAll())
A_TrayMenu.Add()
A_TrayMenu.Add("Exit", (*) => ExitApp())
A_TrayMenu.Default := "Show " AppName
UpdateTrayTip()

ApplyAllHooks()
g.Show("Hide")
g.Show()

Hotkey("^!p", (*) => ShowWindow())

ShowWindow() {
    global g
    g.Show("NoActivate")
    WinActivate("ahk_id " g.Hwnd)
}

; ============================================================
; Hook management
; ============================================================

ApplyAllHooks() {
    ClearAllHooks()
    ApplyHook("Single")
    ApplyHook("Double")
    ApplyHook("Long")
    UpdateTrayTip()
    UpdateToggleButton()
}

ClearAllHooks() {
    global HookKeys
    for name, key in HookKeys {
        try Hotkey(key, "Off")
        try Hotkey(key, (*) => 0)
        try Hotkey(key " Up", "Off")
        try Hotkey(key " Up", (*) => 0)
    }
    HookKeys := Map()
}

ApplyHook(section) {
    global HookKeys, SingleTrigger, DoubleTrigger, LongTrigger
    global SingleHotkey, DoubleHotkey, LongHotkey
    global SingleEnabled, DoubleEnabled, LongEnabled
    global cbSingleEnable, cbDoubleEnable, cbLongEnable

    if (section = "Single") {
        trigger := SingleTrigger, hotkeyOut := SingleHotkey
        enabled := cbSingleEnable.Value
        SingleEnabled := enabled ? "1" : "0"
    } else if (section = "Double") {
        trigger := DoubleTrigger, hotkeyOut := DoubleHotkey
        enabled := cbDoubleEnable.Value
        DoubleEnabled := enabled ? "1" : "0"
    } else {
        trigger := LongTrigger, hotkeyOut := LongHotkey
        enabled := cbLongEnable.Value
        LongEnabled := enabled ? "1" : "0"
    }

    if (enabled && trigger != "" && hotkeyOut != "") {
        try {
            capturedTrigger := trigger
            capturedHotkey := hotkeyOut
            Hotkey(trigger, (*) => 0, "On")
            Hotkey(trigger " Up", (*) => FireOutput(capturedHotkey, capturedTrigger), "On")
            HookKeys[section] := trigger
            SetStatus(section " press: " trigger " -> " hotkeyOut)
        } catch as e {
            SetStatus("Failed to register " trigger ": " e.Message)
        }
    }
}

; ============================================================
; Output firing (same approach as AtumtekRemap)
; ============================================================

FireOutput(hotkeyOut, trigger) {
    global HoldMs
    if (hotkeyOut = "")
        return
    try KeyWait(trigger, "T0.5")
    out := hotkeyOut
    if !InStr(out, "{") {
        mods := ""
        rest := out
        while (rest != "" && InStr("^+!#", SubStr(rest, 1, 1))) {
            mods .= SubStr(rest, 1, 1)
            rest := SubStr(rest, 2)
        }
        if (StrLen(rest) > 1)
            out := mods . "{" . rest . "}"
    }
    mods := "", base := out
    while (base != "" && InStr("^+!#", SubStr(base, 1, 1))) {
        mods .= SubStr(base, 1, 1)
        base := SubStr(base, 2)
    }
    if (SubStr(base, 1, 1) = "{" && SubStr(base, -1) = "}")
        baseName := SubStr(base, 2, StrLen(base) - 2)
    else
        baseName := base
    hold := HoldMs + 0
    if (hold < 1)
        hold := 1
    modMap := Map("^", "Ctrl", "+", "Shift", "!", "Alt", "#", "LWin")
    downSeq := "", upSeq := ""
    Loop Parse, mods {
        if modMap.Has(A_LoopField) {
            downSeq .= "{" modMap[A_LoopField] " down}"
            upSeq   := "{" modMap[A_LoopField] " up}" upSeq
        }
    }
    SendInput("{Blind}{LCtrl up}{RCtrl up}{LShift up}{RShift up}{LAlt up}{RAlt up}{LWin up}{RWin up}")
    SendInput(downSeq . "{" baseName " down}")
    Sleep hold
    SendInput("{" baseName " up}" upSeq)
}

; ============================================================
; Learn functions
; ============================================================

LearnTrigger(section, editCtrl) {
    global SingleTrigger, DoubleTrigger, LongTrigger
    SetStatus("Press the pen button now…")
    ClearAllHooks()
    captured := WaitForAnyKey(8000)
    if (captured = "") {
        SetStatus("No key captured (timed out).")
        ApplyAllHooks()
        return
    }
    editCtrl.Value := captured
    if (section = "Single")
        SingleTrigger := captured
    else if (section = "Double")
        DoubleTrigger := captured
    else
        LongTrigger := captured
    SetStatus("Captured " section " trigger: " captured ". Click Save.")
    ApplyAllHooks()
}

LearnHotkey(editCtrl) {
    SetStatus("Press the hotkey combo you want to SEND…")
    combo := WaitForCombo(8000)
    if (combo = "") {
        SetStatus("No hotkey captured (timed out).")
        return
    }
    editCtrl.Value := combo
    SetStatus("Output set to: " combo ". Click Save.")
}

WaitForAnyKey(timeoutMs) {
    InstallKeybdHook()
    deadline := A_TickCount + timeoutMs
    keys := ["F13","F14","F15","F16","F17","F18","F19","F20"
           ,"F21","F22","F23","F24"
           ,"Volume_Up","Volume_Down","Volume_Mute","Media_Play_Pause"
           ,"Media_Next","Media_Prev","Media_Stop","Browser_Back"
           ,"Browser_Forward","Browser_Refresh","Browser_Home"
           ,"Launch_Mail","Launch_Media","Launch_App1","Launch_App2"
           ,"Enter","Space","Escape","Tab","Up","Down","Left","Right"
           ,"PgUp","PgDn","Home","End","Insert","Delete"]
    Loop {
        for k in keys {
            if GetKeyState(k, "P")
                return k
        }
        if (A_TickCount > deadline)
            return ""
        Sleep 15
    }
}

WaitForCombo(timeoutMs) {
    deadline := A_TickCount + timeoutMs
    Loop {
        if (A_TickCount > deadline)
            return ""
        base := ""
        for k in ["a","b","c","d","e","f","g","h","i","j","k","l","m"
                 ,"n","o","p","q","r","s","t","u","v","w","x","y","z"
                 ,"0","1","2","3","4","5","6","7","8","9"
                 ,"Space","Enter","Tab","Escape","Up","Down","Left","Right"
                 ,"F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12"
                 ,"F13","F14","F15","F16","F17","F18","F19","F20"
                 ,"F21","F22","F23","F24"
                 ,"Home","End","PgUp","PgDn","Insert","Delete"] {
            if GetKeyState(k, "P") {
                base := k
                break
            }
        }
        if (base != "") {
            mods := ""
            if GetKeyState("Ctrl", "P")    mods .= "^"
            if GetKeyState("Shift", "P")   mods .= "+"
            if GetKeyState("Alt", "P")     mods .= "!"
            if GetKeyState("LWin", "P") || GetKeyState("RWin", "P")
                mods .= "#"
            KeyWait(base)
            return mods . "{" . base . "}"
        }
        Sleep 15
    }
}

; ============================================================
; Settings
; ============================================================

SaveSettings() {
    global SingleTrigger, SingleHotkey, SingleEnabled
    global DoubleTrigger, DoubleHotkey, DoubleEnabled
    global LongTrigger, LongHotkey, LongEnabled
    global HoldMs, IniFile
    global edSingleTrigger, edSingleHotkey, cbSingleEnable
    global edDoubleTrigger, edDoubleHotkey, cbDoubleEnable
    global edLongTrigger, edLongHotkey, cbLongEnable
    global edHold

    SingleTrigger := edSingleTrigger.Value
    SingleHotkey  := edSingleHotkey.Value
    SingleEnabled := cbSingleEnable.Value ? "1" : "0"

    DoubleTrigger := edDoubleTrigger.Value
    DoubleHotkey  := edDoubleHotkey.Value
    DoubleEnabled := cbDoubleEnable.Value ? "1" : "0"

    LongTrigger   := edLongTrigger.Value
    LongHotkey    := edLongHotkey.Value
    LongEnabled   := cbLongEnable.Value ? "1" : "0"

    HoldMs := edHold.Value
    if !(HoldMs ~= "^\d+$")
        HoldMs := "120"

    IniWrite(SingleTrigger,  IniFile, "Single", "Trigger")
    IniWrite(SingleHotkey,   IniFile, "Single", "HotkeyOut")
    IniWrite(SingleEnabled,  IniFile, "Single", "Enabled")

    IniWrite(DoubleTrigger,  IniFile, "Double", "Trigger")
    IniWrite(DoubleHotkey,   IniFile, "Double", "HotkeyOut")
    IniWrite(DoubleEnabled,  IniFile, "Double", "Enabled")

    IniWrite(LongTrigger,    IniFile, "Long", "Trigger")
    IniWrite(LongHotkey,     IniFile, "Long", "HotkeyOut")
    IniWrite(LongEnabled,    IniFile, "Long", "Enabled")

    IniWrite(HoldMs,         IniFile, "Settings", "HoldMs")

    SetStatus("Saved.")
    ApplyAllHooks()
}

ResetDefaults() {
    global SingleTrigger, SingleHotkey, DoubleTrigger, DoubleHotkey
    global LongTrigger, LongHotkey, HoldMs
    global edSingleTrigger, edSingleHotkey, cbSingleEnable
    global edDoubleTrigger, edDoubleHotkey, cbDoubleEnable
    global edLongTrigger, edLongHotkey, cbLongEnable
    global edHold

    SingleTrigger := "F20",  SingleHotkey := "^+{F9}"
    DoubleTrigger := "F19",  DoubleHotkey := ""
    LongTrigger   := "F18",  LongHotkey   := ""
    HoldMs := "120"

    edSingleTrigger.Value := SingleTrigger
    edSingleHotkey.Value  := SingleHotkey
    cbSingleEnable.Value  := 1

    edDoubleTrigger.Value := DoubleTrigger
    edDoubleHotkey.Value  := DoubleHotkey
    cbDoubleEnable.Value  := 0

    edLongTrigger.Value   := LongTrigger
    edLongHotkey.Value    := LongHotkey
    cbLongEnable.Value    := 0

    edHold.Value := HoldMs

    SetStatus("Defaults restored. Click Save to persist.")
    ApplyAllHooks()
}

ToggleAll() {
    global cbSingleEnable, cbDoubleEnable, cbLongEnable
    ; If any are enabled, disable all. Otherwise enable all that have hotkeys.
    anyOn := cbSingleEnable.Value || cbDoubleEnable.Value || cbLongEnable.Value
    if (anyOn) {
        cbSingleEnable.Value := 0
        cbDoubleEnable.Value := 0
        cbLongEnable.Value   := 0
    } else {
        cbSingleEnable.Value := 1
        cbDoubleEnable.Value := 1
        cbLongEnable.Value   := 1
    }
    ApplyAllHooks()
}

UpdateToggleButton() {
    global btnToggle, cbSingleEnable, cbDoubleEnable, cbLongEnable
    anyOn := cbSingleEnable.Value || cbDoubleEnable.Value || cbLongEnable.Value
    btnToggle.Text := anyOn
        ? "✅  ACTIVE  —  click to disable all"
        : "⛔  ALL DISABLED  —  click to enable"
}

UpdateTrayTip() {
    global AppName, cbSingleEnable, cbDoubleEnable, cbLongEnable
    anyOn := cbSingleEnable.Value || cbDoubleEnable.Value || cbLongEnable.Value
    A_IconTip := AppName " - " (anyOn ? "ON" : "OFF")
}

SetStatus(msg) {
    global status
    status.Value := msg
}
